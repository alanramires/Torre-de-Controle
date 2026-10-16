using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TorreDeControle.Core.Armazenamento;
using TorreDeControle.Core.Personagens;

namespace TorreDeControle.Core.Salas
{
    /// <summary>
    /// A mesa de uma sala: a rodada corrente e o log de eventos. Cada comando do mestre (dar potencial,
    /// marcar condição, mudar Rótulos) valida pela regra, muda a ficha da instância e vira um evento
    /// com rodada e motivo. Corrigir não apaga: lança o inverso.
    /// Grava em "mesa_&lt;sala&gt;" (separado de salas.json, que fica pequeno).
    /// </summary>
    public sealed class Mesa
    {
        const int VersaoArquivo = 1;

        public const string CampoPotencial = "potencial";
        /// <summary>A pilha de Equipe: é da mesa, não de uma ficha (o evento pode não ter personagem).</summary>
        public const string CampoEquipe = "equipe";
        public static string CampoCondicao(string condicao) => "condicao:" + condicao;
        public static string CampoRotulo(string rotulo) => "rotulo:" + rotulo;

        readonly IArmazenamento armazenamento;
        readonly CadastroPersonagens personagens;
        readonly string sala;
        readonly Arquivo arquivo;

        Mesa(IArmazenamento armazenamento, CadastroPersonagens personagens, string sala, Arquivo arquivo)
        {
            this.armazenamento = armazenamento;
            this.personagens = personagens;
            this.sala = sala;
            this.arquivo = arquivo;
        }

        static string Chave(string sala) => "mesa_" + sala;

        public static Mesa Carregar(IArmazenamento armazenamento, CadastroPersonagens personagens, string sala)
        {
            var texto = armazenamento.Ler(Chave(sala));
            var arquivo = string.IsNullOrWhiteSpace(texto)
                ? new Arquivo()
                : JsonConvert.DeserializeObject<Arquivo>(texto) ?? new Arquivo();
            arquivo.Eventos ??= new List<Evento>();
            arquivo.EmCena ??= new List<string>();
            arquivo.Bonus ??= new List<BonusDeMesa>();
            arquivo.CondicoesDeVilao ??= new Dictionary<string, List<string>>();
            if (arquivo.Rodada < 1)
                arquivo.Rodada = 1; // sempre há uma rodada corrente
            return new Mesa(armazenamento, personagens, sala, arquivo);
        }

        public int Rodada => arquivo.Rodada;

        /// <summary>Pontos na pilha de Equipe agora.</summary>
        public int Equipe => arquivo.Equipe;

        /// <summary>Do mais antigo ao mais novo.</summary>
        public IReadOnlyList<Evento> Eventos => arquivo.Eventos;

        public bool Corrigido(Evento e) => arquivo.Eventos.Any(x => x.Corrige == e.Id);

        /// <summary>Fecha a rodada corrente e abre a próxima.</summary>
        public void NovaRodada()
        {
            arquivo.Rodada++;
            Gravar();
        }

        // ---------- comandos ----------

        /// <summary>Soma (ou tira) potencial. Não fica negativo nem desfaz potencial que já virou avanço.</summary>
        public Resultado DarPotencial(string personagem, int delta, string motivo)
        {
            if (!EmJogo(personagem, out var p, out var erro))
                return erro;
            var novo = p.Potencial + delta;
            if (novo < 0)
                return Resultado.Falha("Potencial não fica negativo.");
            if (novo / CadastroPersonagens.PotencialPorAvanco < p.AvancosMarcados.Count)
                return Resultado.Falha("Esse potencial já virou avanço marcado.");
            var texto = delta >= 0 ? $"+{delta} potencial" : $"{delta} potencial";
            return Registrar(p, texto, motivo, new Mudanca { Campo = CampoPotencial, Antes = p.Potencial, Depois = novo });
        }

        /// <summary>
        /// Marca ou desmarca uma condição. Marcar com todas já marcadas não muda nada: a personagem
        /// sai de cena (o resultado diz isso).
        /// </summary>
        public Resultado AlternarCondicao(string personagem, string condicao, string motivo)
        {
            if (!EmJogo(personagem, out var p, out var erro))
                return erro;
            if (!Condicoes.Todas.Contains(condicao))
                return Resultado.Falha("Condição desconhecida.");
            var marcada = p.CondicoesMarcadas.Contains(condicao);
            var texto = (marcada ? "desmarcou " : "marcou ") + Condicoes.Nome(condicao);
            return Registrar(p, texto, motivo,
                new Mudanca { Campo = CampoCondicao(condicao), Antes = marcada ? 1 : 0, Depois = marcada ? 0 : 1 });
        }

        /// <summary>
        /// Mudar Rótulos: <paramref name="sobe"/> +1 e <paramref name="desce"/> −1 (livro p.46). Se algum
        /// sairia de [−2, +3], nenhum muda e a personagem marca uma condição, que o mestre escolhe:
        /// sem <paramref name="condicaoSeEstourar"/>, o resultado pede a escolha e nada é feito.
        /// </summary>
        public Resultado MudarRotulos(string personagem, string sobe, string desce, string motivo,
            string condicaoSeEstourar = null)
        {
            if (!EmJogo(personagem, out var p, out var erro))
                return erro;
            if (!Rotulos.Todos.Contains(sobe) || !Rotulos.Todos.Contains(desce))
                return Resultado.Falha("Rótulo desconhecido.");
            if (sobe == desce)
                return Resultado.Falha("Um Rótulo sobe e outro desce: escolha dois diferentes.");

            var atuais = personagens.RotulosAtuais(p);
            var estoura = atuais[sobe] + 1 > Rotulos.Maximo || atuais[desce] - 1 < Rotulos.Minimo;
            if (!estoura)
                return Registrar(p, $"{Rotulos.Nome(sobe)} +1, {Rotulos.Nome(desce)} −1", motivo,
                    new Mudanca { Campo = CampoRotulo(sobe), Antes = atuais[sobe], Depois = atuais[sobe] + 1 },
                    new Mudanca { Campo = CampoRotulo(desce), Antes = atuais[desce], Depois = atuais[desce] - 1 });

            if (condicaoSeEstourar == null)
                return Resultado.Falha(MensagemDeEstouro);
            if (!Condicoes.Todas.Contains(condicaoSeEstourar))
                return Resultado.Falha("Condição desconhecida.");
            if (p.CondicoesMarcadas.Contains(condicaoSeEstourar))
                return Resultado.Falha("Essa condição já está marcada: escolha outra.");
            return Registrar(p,
                $"{Rotulos.Nome(sobe)} +1 / {Rotulos.Nome(desce)} −1 estourou o limite: nenhum Rótulo muda; marcou {Condicoes.Nome(condicaoSeEstourar)}",
                motivo, new Mudanca { Campo = CampoCondicao(condicaoSeEstourar), Antes = 0, Depois = 1 });
        }

        /// <summary>
        /// Marca (ou tira) pontos numa trilha do arquétipo, com o que aconteceu (ex.: perdição por Visões
        /// sombrias). A cada 5 a ficha rende uma escolha (um sinal novo): a tela avisa.
        /// </summary>
        public Resultado MarcarTrilha(string personagem, string extra, string campo, int delta, string motivo)
        {
            if (!EmJogo(personagem, out var p, out var erro))
                return erro;
            var r = personagens.ValidarContador(p, extra, campo, delta, out var novo);
            if (!r.Ok)
                return r;
            var nome = personagens.TrilhasDoArquetipo(p).FirstOrDefault(t => t.extra == extra && t.campo == campo).nome ?? campo;
            return Registrar(p, $"{(delta > 0 ? "+" : string.Empty)}{delta} {nome}", motivo,
                new Mudanca { Campo = "contador:" + extra + "|" + campo, Antes = novo - delta, Depois = novo });
        }

        // ---------- pilha de Equipe (livro pp.84–88) ----------

        /// <summary>Soma ou tira pontos da pilha (gastar, ganhar por movimento, ajuste do mestre). Não fica negativa.</summary>
        public Resultado MudarEquipe(int delta, string descricao, string motivo)
        {
            if (delta == 0)
                return Resultado.Sucesso();
            var novo = arquivo.Equipe + delta;
            if (novo < 0)
                return Resultado.Falha("A pilha de Equipe está vazia.");
            var texto = string.IsNullOrWhiteSpace(descricao) ? (delta > 0 ? $"+{delta} Equipe" : $"{delta} Equipe") : descricao;
            return RegistrarDaMesa(null, texto, motivo, new Mudanca { Campo = CampoEquipe, Antes = arquivo.Equipe, Depois = novo });
        }

        /// <summary>
        /// Usar pontos da pilha (ajudar um colega, agir de forma egoísta, movimentos que gastam mais de um, como
        /// os do Farol). Quem usa é opcional; a quantidade não passa do que há na pilha.
        /// </summary>
        public Resultado UsarEquipe(string personagem, int quantidade, string motivo)
        {
            if (quantidade < 1)
                return Resultado.Falha("Use pelo menos 1 ponto.");
            if (quantidade > arquivo.Equipe)
                return Resultado.Falha($"A pilha tem {arquivo.Equipe} ponto{(arquivo.Equipe == 1 ? "" : "s")}.");
            Personagem p = null;
            if (personagem != null && !EmJogo(personagem, out p, out var erro))
                return erro;
            return RegistrarDaMesa(p, $"usou {quantidade} da pilha de Equipe", motivo,
                new Mudanca { Campo = CampoEquipe, Antes = arquivo.Equipe, Depois = arquivo.Equipe - quantidade });
        }

        /// <summary>Início de sessão: +1 na pilha (p.92).</summary>
        public Resultado InicioDeSessao(string motivo = null) => MudarEquipe(1, "início de sessão: +1 Equipe", motivo);

        /// <summary>Quando o tempo passa (salto na história, p.88): a pilha volta para 1.</summary>
        public Resultado OTempoPassa(string motivo) =>
            RegistrarDaMesa(null, "o tempo passa: pilha volta para 1", motivo,
                new Mudanca { Campo = CampoEquipe, Antes = arquivo.Equipe, Depois = 1 });

        /// <summary>As respostas do questionário de "Entrar em batalha" (p.86–87).</summary>
        public sealed class EntradaEmBatalha
        {
            public string Lider { get; set; }
            /// <summary>As outras heroínas na luta (as que podem gastar da pilha).</summary>
            public IReadOnlyList<string> Colegas { get; set; } = new List<string>();
            public bool MesmoObjetivo { get; set; }
            public bool AlguemNaoConfia { get; set; }
            public bool MalPreparada { get; set; }
            /// <summary>Condições que a líder marca para não perder pontos (uma por ponto evitado).</summary>
            public IReadOnlyList<string> CondicoesParaEvitar { get; set; } = new List<string>();
        }

        /// <summary>
        /// A conta do questionário, sem aplicar: +2; +1 se a líder tem Influência sobre cada colega; +1 se
        /// todas têm o mesmo objetivo; −1 se alguém não confia; −1 se mal preparada. Cada condição que a
        /// líder marca evita um dos −1. Devolve o total e as linhas, para a tela mostrar antes de confirmar.
        /// </summary>
        public (int total, IReadOnlyList<string> linhas, IReadOnlyList<string> semInfluencia) ContarBatalha(EntradaEmBatalha e)
        {
            var lider = personagens.Buscar(e.Lider);
            var colegas = e.Colegas.Where(id => id != e.Lider).Select(personagens.Buscar).Where(c => c != null).ToList();
            var semInfluencia = lider == null ? new List<string>()
                : colegas.Where(c => !personagens.TemInfluencia(lider, c)).Select(c => c.NomeExibicao).ToList();
            var linhas = new List<string> { "+2 entrando como equipe" };
            var total = 2;
            if (lider != null && colegas.Count > 0 && semInfluencia.Count == 0)
            {
                total++;
                linhas.Add("+1 quem lidera tem Influência sobre cada colega");
            }
            if (e.MesmoObjetivo)
            {
                total++;
                linhas.Add("+1 todos têm o mesmo objetivo");
            }
            var perdas = (e.AlguemNaoConfia ? 1 : 0) + (e.MalPreparada ? 1 : 0);
            var evitadas = Math.Min(perdas, e.CondicoesParaEvitar.Count);
            if (e.AlguemNaoConfia)
                linhas.Add("−1 alguém não confia em quem lidera ou na equipe");
            if (e.MalPreparada)
                linhas.Add("−1 equipe mal preparada ou desequilibrada");
            if (evitadas > 0)
                linhas.Add($"+{evitadas} evitado por quem lidera (marca {string.Join(", ", e.CondicoesParaEvitar.Take(evitadas).Select(Condicoes.Nome))})");
            total -= perdas - evitadas;
            return (total, linhas, semInfluencia);
        }

        /// <summary>
        /// Entrar em batalha como equipe: aplica a conta na pilha e as condições que a líder marcou para evitar
        /// perdas, num evento só (desfazer volta tudo).
        /// </summary>
        public Resultado EntrarEmBatalha(EntradaEmBatalha e, string motivo)
        {
            if (!EmJogo(e.Lider, out var lider, out var erro))
                return erro;
            if (e.Colegas.Count(id => id != e.Lider) == 0)
                return Resultado.Falha("Uma pessoa só não é equipe: o movimento pede mais de um personagem.");
            var perdas = (e.AlguemNaoConfia ? 1 : 0) + (e.MalPreparada ? 1 : 0);
            var condicoes = e.CondicoesParaEvitar.Take(perdas).ToList();
            if (condicoes.Any(c => !Condicoes.Todas.Contains(c)) || condicoes.Distinct().Count() != condicoes.Count)
                return Resultado.Falha("Condições para evitar: escolha condições diferentes.");
            if (condicoes.Any(c => lider.CondicoesMarcadas.Contains(c)))
                return Resultado.Falha("Quem lidera já tem essa condição marcada: escolha outra.");
            var (total, linhas, _) = ContarBatalha(e);
            var novo = Math.Max(0, arquivo.Equipe + total);
            var mudancas = new List<Mudanca> { new Mudanca { Campo = CampoEquipe, Antes = arquivo.Equipe, Depois = novo } };
            mudancas.AddRange(condicoes.Select(c => new Mudanca { Campo = CampoCondicao(c), Antes = 0, Depois = 1 }));
            var sinal = total >= 0 ? "+" : string.Empty;
            return RegistrarDaMesa(lider, $"entrar em batalha (lidera: {lider.NomeExibicao}): {sinal}{total} Equipe · {string.Join("; ", linhas)}",
                motivo, mudancas.ToArray());
        }

        // ---------- bônus (adiante, constante, controle) ----------

        public const string BonusAdiante = "adiante";
        public const string BonusConstante = "constante";
        public const string BonusControle = "controle";
        /// <summary>Lembrete: uma tag temporária sem valor (ex.: "abertura: Aurora cedeu terreno").</summary>
        public const string BonusLembrete = "lembrete";

        public static string NomeDoBonus(string tipo) => tipo switch
        {
            BonusConstante => "Constante",
            BonusControle => "Controle",
            BonusLembrete => "Lembrete",
            _ => "Adiante",
        };

        /// <summary>Os bônus ativos de uma heroína, na ordem em que foram dados (viram tokens no assento).</summary>
        public IReadOnlyList<BonusDeMesa> BonusDe(string personagem) =>
            arquivo.Bonus.Where(b => b.Ativo && b.Personagem == personagem).ToList();

        /// <summary>
        /// O mestre dá um bônus: adiante (vale na próxima rolagem que bate com o contexto), constante (vale
        /// enquanto o contexto durar) ou controle (um recurso a gastar). O contexto diz quando vale (ex.:
        /// "agindo pelas respostas da avaliação").
        /// </summary>
        public Resultado DarBonus(string personagem, string tipo, int valor, string contexto, string motivo)
        {
            var lembrete = tipo == BonusLembrete;
            Personagem p;
            if (lembrete && EstaEmCena(personagem))
                p = personagens.Buscar(personagem); // lembrete também vai no vilão em cena
            else if (!EmJogo(personagem, out p, out var erro))
                return erro;
            if (tipo != BonusAdiante && tipo != BonusConstante && tipo != BonusControle && !lembrete)
                return Resultado.Falha("Tipo de bônus desconhecido.");
            if (lembrete)
            {
                valor = 0;
                if (string.IsNullOrWhiteSpace(contexto))
                    return Resultado.Falha("Lembrete precisa de texto (ex.: \"abertura: Aurora cedeu terreno\").");
            }
            else if (valor == 0)
                return Resultado.Falha("Bônus de zero não muda nada.");
            var bonus = new BonusDeMesa
            {
                Id = Guid.NewGuid().ToString("N"), Personagem = p.Id, Tipo = tipo, Valor = valor,
                Contexto = (contexto ?? string.Empty).Trim(),
            };
            arquivo.Bonus.Add(bonus);
            return RegistrarDaMesa(p, lembrete ? $"lembrete: {bonus.Contexto}" : $"ganhou {NomeDoBonus(tipo)} {Sinal(valor)}" +
                                      (bonus.Contexto.Length > 0 ? $" ({bonus.Contexto})" : string.Empty), motivo,
                new Mudanca { Campo = CampoDeBonus(bonus.Id), Antes = 0, Depois = 1 });
        }

        /// <summary>Usou ou acabou: o bônus sai do assento (o evento guarda qual era).</summary>
        public Resultado RetirarBonus(string bonusId, string motivo)
        {
            var bonus = arquivo.Bonus.FirstOrDefault(b => b.Id == bonusId && b.Ativo);
            if (bonus == null)
                return Resultado.Falha("Esse bônus já não está na mesa.");
            var p = personagens.Buscar(bonus.Personagem);
            var verbo = bonus.Tipo == BonusConstante ? "acabou" : "usou";
            if (bonus.Tipo == BonusLembrete)
                return RegistrarDaMesa(p, $"resolveu o lembrete: {bonus.Contexto}", motivo,
                    new Mudanca { Campo = CampoDeBonus(bonus.Id), Antes = 1, Depois = 0 });
            return RegistrarDaMesa(p, $"{verbo} {NomeDoBonus(bonus.Tipo)} {Sinal(bonus.Valor)}" +
                                      (bonus.Contexto.Length > 0 ? $" ({bonus.Contexto})" : string.Empty), motivo,
                new Mudanca { Campo = CampoDeBonus(bonus.Id), Antes = 1, Depois = 0 });
        }

        static string CampoDeBonus(string id) => "bonus:" + id;
        static string Sinal(int v) => v > 0 ? "+" + v : v.ToString();

        // ---------- vilões em cena (livro pp.186–196) ----------

        public static string CampoDeVilao(string vilao, string condicao) => $"vilao:{vilao}:{condicao}";

        /// <summary>Os NPCs em cena, na ordem em que entraram (os que sumiram do cadastro saem da lista).</summary>
        public IReadOnlyList<Personagem> EmCena() =>
            arquivo.EmCena.Select(personagens.Buscar).Where(n => n != null).ToList();

        public bool EstaEmCena(string npc) => arquivo.EmCena.Contains(npc);

        /// <summary>
        /// Põe ou tira um NPC da mesa (preparo do mestre, pela Área do mestre). Não é evento de jogo; as condições
        /// marcadas na luta ficam guardadas se ele sair e voltar.
        /// </summary>
        public Resultado AlternarEmCena(string npc)
        {
            var n = personagens.Buscar(npc);
            if (n == null || n.Controle != Controle.Mestre || n.Sala != sala)
                return Resultado.Falha("NPC fora desta campanha.");
            if (!arquivo.EmCena.Remove(npc))
                arquivo.EmCena.Add(npc);
            Gravar();
            return Resultado.Sucesso();
        }

        public IReadOnlyList<string> CondicoesMarcadasDoVilao(string vilao) =>
            arquivo.CondicoesDeVilao.TryGetValue(vilao, out var c) ? c : (IReadOnlyList<string>)new List<string>();

        /// <summary>
        /// Marca ou desmarca, na luta, uma das condições que o vilão tem (ficha de vilão). Marcar é o golpe que
        /// pegou: o livro manda fazer um movimento daquela condição na hora (a tela mostra a carta).
        /// </summary>
        public Resultado AlternarCondicaoDoVilao(string vilao, string condicao, string motivo)
        {
            var n = personagens.Buscar(vilao);
            if (n == null || !EstaEmCena(vilao))
                return Resultado.Falha("Esse NPC não está em cena.");
            if (n.Vilao == null || !n.Vilao.Condicoes.Contains(condicao))
                return Resultado.Falha("Ele não tem essa condição na ficha de vilão.");
            var marcada = CondicoesMarcadasDoVilao(vilao).Contains(condicao);
            return RegistrarDaMesa(n, (marcada ? "desmarcou " : "marcou ") + Condicoes.Nome(condicao), motivo,
                new Mudanca { Campo = CampoDeVilao(vilao, condicao), Antes = marcada ? 1 : 0, Depois = marcada ? 0 : 1 });
        }

        /// <summary>Todas as condições dele marcadas: o próximo golpe que pedir condição o derruba (p.196).</summary>
        public bool NoLimite(Personagem vilao) =>
            vilao.Vilao != null && vilao.Vilao.Condicoes.Count > 0 &&
            vilao.Vilao.Condicoes.All(c => CondicoesMarcadasDoVilao(vilao.Id).Contains(c));

        /// <summary>Registra no log qual movimento de vilão o mestre usou (lembra a mesa e vira post).</summary>
        public Resultado UsarMovimentoDeVilao(string vilao, string movimento, string motivo)
        {
            var n = personagens.Buscar(vilao);
            if (n == null || !EstaEmCena(vilao))
                return Resultado.Falha("Esse NPC não está em cena.");
            if (string.IsNullOrWhiteSpace(movimento))
                return Resultado.Falha("Movimento vazio.");
            return RegistrarDaMesa(n, "movimento de vilão: " + movimento.Trim(), motivo);
        }

        /// <summary>A tela reconhece este texto para pedir a condição do estouro.</summary>
        public const string MensagemDeEstouro = "Estoura o limite dos Rótulos: nenhum muda e a personagem marca uma condição. Qual?";

        /// <summary>
        /// Desfaz um evento lançando o inverso (o original fica no log, marcado como corrigido).
        /// É ajuste do mestre: aplica o inverso do que mudou, sem as regras do comando.
        /// </summary>
        public Resultado Corrigir(string eventoId, string motivo)
        {
            var original = arquivo.Eventos.FirstOrDefault(e => e.Id == eventoId);
            if (original == null)
                return Resultado.Falha("Evento não encontrado.");
            if (original.Corrige != null)
                return Resultado.Falha("Uma correção não se corrige: lance o comando de novo.");
            if (Corrigido(original))
                return Resultado.Falha("Esse evento já foi corrigido.");
            var p = original.Personagem == null ? null : personagens.Buscar(original.Personagem);
            if (p == null && original.Mudancas.Any(m => !DaMesa(m.Campo)))
                return Resultado.Falha("Personagem não encontrada.");

            var inversas = new List<Mudanca>();
            foreach (var m in original.Mudancas)
            {
                var agora = DaMesa(m.Campo) ? ValorDaMesa(m.Campo) : personagens.ValorDeJogo(p, m.Campo);
                var depois = Limitar(m.Campo, agora - (m.Depois - m.Antes));
                if (depois != agora)
                    inversas.Add(new Mudanca { Campo = m.Campo, Antes = agora, Depois = depois });
            }
            var evento = NovoEvento(p, "corrigiu: " + original.Descricao, motivo);
            evento.Corrige = original.Id;
            evento.Mudancas = inversas;
            Aplicar(p, inversas);
            arquivo.Eventos.Add(evento);
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// Refaz o que um desfazer (correção) tirou: lança de novo as mudanças do evento original, a partir dos
        /// valores de agora. É outro evento ("refez: ..."), que também se desfaz.
        /// </summary>
        public Resultado Refazer(string correcaoId, string motivo)
        {
            var correcao = arquivo.Eventos.FirstOrDefault(e => e.Id == correcaoId);
            if (correcao?.Corrige == null)
                return Resultado.Falha("Só se refaz um desfazer.");
            if (arquivo.Eventos.Any(e => e.Refaz == correcao.Id))
                return Resultado.Falha("Esse desfazer já foi refeito.");
            var original = arquivo.Eventos.FirstOrDefault(e => e.Id == correcao.Corrige);
            if (original == null)
                return Resultado.Falha("Evento original não encontrado.");
            var p = original.Personagem == null ? null : personagens.Buscar(original.Personagem);
            if (p == null && original.Mudancas.Any(m => !DaMesa(m.Campo)))
                return Resultado.Falha("Personagem não encontrada.");
            var mudancas = new List<Mudanca>();
            foreach (var m in original.Mudancas)
            {
                var agora = DaMesa(m.Campo) ? ValorDaMesa(m.Campo) : personagens.ValorDeJogo(p, m.Campo);
                var depois = Limitar(m.Campo, agora + (m.Depois - m.Antes));
                if (depois != agora)
                    mudancas.Add(new Mudanca { Campo = m.Campo, Antes = agora, Depois = depois });
            }
            var evento = NovoEvento(p, "refez: " + original.Descricao, motivo ?? original.Motivo);
            evento.Refaz = correcao.Id;
            evento.Mudancas = mudancas;
            Aplicar(p, mudancas);
            arquivo.Eventos.Add(evento);
            Gravar();
            return Resultado.Sucesso();
        }

        // ---------- por dentro ----------

        /// <summary>Só as instâncias desta sala, em carreira e numa vaga, recebem comando de jogo.</summary>
        bool EmJogo(string id, out Personagem p, out Resultado erro)
        {
            p = personagens.Buscar(id);
            erro = Resultado.Sucesso();
            if (p == null || p.Sala != sala)
                erro = Resultado.Falha("Personagem fora desta sala.");
            else if (!p.Ativo)
                erro = Resultado.Falha("Ficha do arquivo da sala: só consulta.");
            else if (p.Status != StatusCriacao.EmCarreira)
                erro = Resultado.Falha("Ficha em revisão (novo): conclua a criação antes de jogar.");
            return erro.Ok;
        }

        Resultado Registrar(Personagem p, string descricao, string motivo, params Mudanca[] mudancas) =>
            RegistrarDaMesa(p, descricao, motivo, mudancas);

        /// <summary>Lança o evento (personagem pode ser null: só a pilha) e aplica as mudanças.</summary>
        Resultado RegistrarDaMesa(Personagem p, string descricao, string motivo, params Mudanca[] mudancas)
        {
            var evento = NovoEvento(p, descricao, motivo);
            evento.Mudancas = mudancas.ToList();
            Aplicar(p, evento.Mudancas);
            arquivo.Eventos.Add(evento);
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>A pilha e as condições dos vilões em cena mudam na mesa; o resto, na ficha da heroína.</summary>
        void Aplicar(Personagem p, List<Mudanca> mudancas)
        {
            foreach (var m in mudancas.Where(m => DaMesa(m.Campo)))
            {
                if (m.Campo == CampoEquipe)
                {
                    arquivo.Equipe = Math.Max(0, m.Depois);
                    continue;
                }
                if (m.Campo.StartsWith("bonus:"))
                {
                    var b = arquivo.Bonus.FirstOrDefault(x => "bonus:" + x.Id == m.Campo);
                    if (b != null)
                        b.Ativo = m.Depois > 0;
                    continue;
                }
                var (vilao, condicao) = PartesDoCampoDeVilao(m.Campo);
                if (!arquivo.CondicoesDeVilao.TryGetValue(vilao, out var marcadas))
                    arquivo.CondicoesDeVilao[vilao] = marcadas = new List<string>();
                marcadas.Remove(condicao);
                if (m.Depois > 0)
                    marcadas.Add(condicao);
            }
            var daFicha = mudancas.Where(m => !DaMesa(m.Campo)).ToList();
            if (p != null && daFicha.Count > 0)
                personagens.AplicarDeJogo(p, daFicha);
        }

        static bool DaMesa(string campo) => campo == CampoEquipe || campo.StartsWith("vilao:") || campo.StartsWith("bonus:");

        int ValorDaMesa(string campo)
        {
            if (campo == CampoEquipe)
                return arquivo.Equipe;
            if (campo.StartsWith("bonus:"))
                return arquivo.Bonus.FirstOrDefault(b => "bonus:" + b.Id == campo)?.Ativo == true ? 1 : 0;
            var (vilao, condicao) = PartesDoCampoDeVilao(campo);
            return CondicoesMarcadasDoVilao(vilao).Contains(condicao) ? 1 : 0;
        }

        static (string vilao, string condicao) PartesDoCampoDeVilao(string campo)
        {
            var partes = campo.Split(':');
            return (partes[1], partes[2]);
        }

        Evento NovoEvento(Personagem p, string descricao, string motivo) => new Evento
        {
            Id = Guid.NewGuid().ToString("N"),
            Rodada = arquivo.Rodada,
            Quando = DateTime.Now.ToString("s"),
            Personagem = p?.Id,
            Descricao = descricao,
            Motivo = (motivo ?? string.Empty).Trim(),
        };

        static int Limitar(string campo, int valor) =>
            campo.StartsWith("condicao:") || campo.StartsWith("vilao:") || campo.StartsWith("bonus:") ? Math.Max(0, Math.Min(1, valor))
            : campo == CampoPotencial || campo == CampoEquipe || campo.StartsWith("contador:") ? Math.Max(0, valor)
            : valor;

        void Gravar()
        {
            arquivo.Versao = VersaoArquivo;
            armazenamento.Gravar(Chave(sala), JsonConvert.SerializeObject(arquivo, Formatting.Indented));
        }

        sealed class Arquivo
        {
            [JsonProperty("versao")] public int Versao { get; set; } = VersaoArquivo;
            [JsonProperty("rodada")] public int Rodada { get; set; } = 1;
            [JsonProperty("equipe")] public int Equipe { get; set; }
            /// <summary>NPCs (vilões, oponentes) em cena nesta mesa, na ordem em que entraram.</summary>
            [JsonProperty("em_cena")] public List<string> EmCena { get; set; } = new List<string>();
            /// <summary>Bônus dados na mesa (ativos e já usados: o log aponta para eles).</summary>
            [JsonProperty("bonus")] public List<BonusDeMesa> Bonus { get; set; } = new List<BonusDeMesa>();
            /// <summary>Condições marcadas na luta, por vilão (as que ele tem estão na ficha dele).</summary>
            [JsonProperty("condicoes_de_vilao")] public Dictionary<string, List<string>> CondicoesDeVilao { get; set; } =
                new Dictionary<string, List<string>>();
            [JsonProperty("eventos")] public List<Evento> Eventos { get; set; } = new List<Evento>();
        }
    }
}
