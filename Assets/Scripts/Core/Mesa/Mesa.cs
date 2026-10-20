using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TorreDeControle.Core.Armazenamento;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Regras;

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
        readonly RegrasDeCondicoes regrasDeCondicoes;

        Mesa(IArmazenamento armazenamento, CadastroPersonagens personagens, string sala, Arquivo arquivo,
            RegrasDeCondicoes regrasDeCondicoes)
        {
            this.armazenamento = armazenamento;
            this.personagens = personagens;
            this.sala = sala;
            this.arquivo = arquivo;
            this.regrasDeCondicoes = regrasDeCondicoes;
        }

        static string Chave(string sala) => "mesa_" + sala;

        /// <param name="regrasDeCondicoes">As penalidades das condições (nucleo.json); sem elas, a rolagem soma só o Rótulo.</param>
        public static Mesa Carregar(IArmazenamento armazenamento, CadastroPersonagens personagens, string sala,
            RegrasDeCondicoes regrasDeCondicoes = null)
        {
            var texto = armazenamento.Ler(Chave(sala));
            var arquivo = string.IsNullOrWhiteSpace(texto)
                ? new Arquivo()
                : JsonConvert.DeserializeObject<Arquivo>(texto) ?? new Arquivo();
            arquivo.Eventos ??= new List<Evento>();
            arquivo.EmCena ??= new List<string>();
            arquivo.Bonus ??= new List<BonusDeMesa>();
            arquivo.CondicoesDeVilao ??= new Dictionary<string, List<string>>();
            arquivo.Cenas ??= new List<Cena>();
            arquivo.HeroisEmCena ??= new List<string>();
            if (arquivo.Rodada < 1)
                arquivo.Rodada = 1; // sempre há uma rodada corrente
            return new Mesa(armazenamento, personagens, sala, arquivo, regrasDeCondicoes);
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
            if (ForaDeCena(personagem) is string fora)
                return Resultado.Falha(fora);
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
            // Só quem está na cena entra na batalha (e conta para o +1 de Influência sobre cada colega).
            if (new[] { e.Lider }.Concat(e.Colegas).Select(ForaDeCena).FirstOrDefault(x => x != null) is string fora)
                return Resultado.Falha(fora);
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

        public static string CampoDeNpcEmCena(string npc) => "npc_em_cena:" + npc;

        /// <summary>
        /// Põe ou tira um NPC da cena (vilão, aliado, neutro), pela gestão da cena ou pela Área do mestre. Vai para o log
        /// ("entrou em cena" / "saiu de cena", com o motivo) e se desfaz como os outros; as condições marcadas na luta
        /// ficam guardadas se ele sair e voltar.
        /// </summary>
        public Resultado AlternarEmCena(string npc, string motivo = null)
        {
            var n = personagens.Buscar(npc);
            if (n == null || n.Controle != Controle.Mestre || n.Sala != sala)
                return Resultado.Falha("NPC fora desta campanha.");
            var dentro = arquivo.EmCena.Contains(npc);
            return RegistrarDaMesa(n, dentro ? "saiu de cena" : "entrou em cena", motivo,
                new Mudanca { Campo = CampoDeNpcEmCena(npc), Antes = dentro ? 1 : 0, Depois = dentro ? 0 : 1 });
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

        /// <summary>
        /// A condição que a troca desta rolagem marcou num vilão e cujo movimento ainda não foi resolvido (livro: quando o
        /// vilão marca uma condição, ele faz o movimento dela). Null se não há o que resolver. Não abre nada sozinho: o
        /// MJ resolve na ordem que couber.
        /// </summary>
        public (string vilao, string condicao)? CondicaoPedida(Evento e)
        {
            var r = e?.Rolagem;
            if (r?.Troca?.CondicaoDoAlvo == null || r.Troca.AlvoGolpePoderoso || r.Alvo == null || !r.Aceita || e.Oculto || Corrigido(e))
                return null;
            if (arquivo.Eventos.Any(x => x.Resolve == e.Id && !x.Oculto && !Corrigido(x)))
                return null;
            return (r.Alvo, r.Troca.CondicaoDoAlvo);
        }

        /// <summary>
        /// O MJ fez o movimento da condição que a troca marcou no vilão: entra no log ("movimento de condição (Irritado):
        /// ..."), e a pendência some. Desfazer este evento devolve a pendência.
        /// </summary>
        public Resultado ResolverMovimentoDeCondicao(string eventoId, string oQueFez)
        {
            var e = arquivo.Eventos.FirstOrDefault(x => x.Id == eventoId);
            var pedida = CondicaoPedida(e);
            if (pedida == null)
                return Resultado.Falha("Não há movimento de condição pendente nessa rolagem.");
            var (vilao, condicao) = pedida.Value;
            var texto = (oQueFez ?? string.Empty).Trim();
            var evento = NovoEvento(personagens.Buscar(vilao),
                $"movimento de condição ({Condicoes.Nome(condicao)})" + (texto.Length > 0 ? ": " + texto : string.Empty), null);
            evento.Resolve = e.Id;
            arquivo.Eventos.Add(evento);
            Gravar();
            return Resultado.Sucesso();
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
            // Devolve o que a correção tirou de fato (ela pode ter sido parcial, por limite), não o delta original.
            var mudancas = new List<Mudanca>();
            foreach (var m in correcao.Mudancas)
            {
                var agora = DaMesa(m.Campo) ? ValorDaMesa(m.Campo) : personagens.ValorDeJogo(p, m.Campo);
                var depois = Limitar(m.Campo, agora - (m.Depois - m.Antes));
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

        // ---------- debug e marcações do mestre ----------

        /// <summary>
        /// Debug: a mesa volta ao início do jogo, para testar de novo. Esconde o log (fica no arquivo), volta à rodada 1 e
        /// lança um evento por heroína (potencial 0, sem condições, Rótulos e trilhas da ficha aprovada) e um da mesa
        /// (pilha 0, bônus e condições de vilão apagados). Cada evento se desfaz como os outros. Avanços marcados e os
        /// vilões em cena ficam como estão.
        /// </summary>
        public Resultado ResetarMesa()
        {
            EsconderTudo();
            arquivo.Rodada = 1;
            var herois = personagens.HeroisDaSala(sala).Where(p => p.Ativo && p.Status == StatusCriacao.EmCarreira).ToList();
            foreach (var p in herois)
            {
                var mudancas = new List<Mudanca>();
                if (p.Potencial != 0)
                    mudancas.Add(new Mudanca { Campo = CampoPotencial, Antes = p.Potencial, Depois = 0 });
                mudancas.AddRange(p.CondicoesMarcadas.ToList().Select(c => new Mudanca { Campo = CampoCondicao(c), Antes = 1, Depois = 0 }));
                if (p.Foto != null)
                {
                    var atuais = personagens.RotulosAtuais(p);
                    foreach (var r in Rotulos.Todos)
                        if (p.Foto.Rotulos.TryGetValue(r, out var inicio) && atuais.TryGetValue(r, out var agora) && agora != inicio)
                            mudancas.Add(new Mudanca { Campo = CampoRotulo(r), Antes = agora, Depois = inicio });
                }
                mudancas.AddRange(p.Contadores.Where(c => c.Value != 0).ToList()
                    .Select(c => new Mudanca { Campo = "contador:" + c.Key, Antes = c.Value, Depois = 0 }));
                if (mudancas.Count > 0)
                    RegistrarDaMesa(p, "reset (debug): a ficha volta ao início do jogo", null, mudancas.ToArray());
            }
            var daMesa = new List<Mudanca>();
            if (arquivo.Equipe != 0)
                daMesa.Add(new Mudanca { Campo = CampoEquipe, Antes = arquivo.Equipe, Depois = 0 });
            daMesa.AddRange(arquivo.Bonus.Where(b => b.Ativo).Select(b => new Mudanca { Campo = CampoDeBonus(b.Id), Antes = 1, Depois = 0 }));
            daMesa.AddRange(arquivo.CondicoesDeVilao.SelectMany(v => v.Value.Select(c => new Mudanca { Campo = CampoDeVilao(v.Key, c), Antes = 1, Depois = 0 })).ToList());
            daMesa.AddRange(arquivo.HeroisEmCena.ToList().Select(h => new Mudanca { Campo = CampoDeCena(h), Antes = 1, Depois = 0 }));
            arquivo.CenaAtual = null; // a mesa volta vazia, esperando a primeira cena
            RegistrarDaMesa(null, "reset (debug): a mesa volta ao início do jogo (rodada 1, pilha 0, sem cena)", null, daMesa.ToArray());
            return Resultado.Sucesso();
        }

        // ---------- a cena: quem está nela e quem assiste ----------

        public static string CampoDeCena(string heroi) => "cena:" + heroi;

        /// <summary>Quantas heroínas cabem numa cena: o jogo foi pensado para até 6 (a mesa senta 3 em cima e 3 embaixo).</summary>
        public const int MaximoEmCena = 6;

        /// <summary>Há uma cena aberta. Sem ela, a mesa está vazia (esperando "criar cena").</summary>
        public bool TemCena => arquivo.CenaAtual != null;

        /// <summary>A cena aberta, ou null.</summary>
        public Cena CenaAtual => arquivo.Cenas.FirstOrDefault(c => c.Id == arquivo.CenaAtual);

        /// <summary>Todas as cenas da campanha, da primeira à atual (nada se exclui).</summary>
        public IReadOnlyList<Cena> Cenas => arquivo.Cenas;

        /// <summary>A heroína está na cena aberta (as outras assistem: estão em outra cena, saíram ou foram nocauteadas).</summary>
        public bool HeroiEmCena(string heroi) => TemCena && arquivo.HeroisEmCena.Contains(heroi);

        /// <summary>
        /// Com cena aberta, quem age na mesa precisa estar nela: heroína em cena ou NPC em cena. Devolve o erro, ou null
        /// (também null sem cena aberta, ou sem ninguém: o herói genérico).
        /// </summary>
        string ForaDeCena(string id)
        {
            if (id == null || !TemCena)
                return null;
            var p = personagens.Buscar(id);
            if (p == null)
                return null;
            if (p.Controle == Controle.Mestre)
                return EstaEmCena(id) ? null : $"{p.NomeExibicao} não está em cena.";
            return HeroiEmCena(id) ? null : $"{p.NomeExibicao} está assistindo: ponha em cena antes.";
        }

        /// <summary>
        /// Abre uma cena nova com quem está nela (o resto assiste). Um evento só: desfazer volta quem estava em cena
        /// antes. Não mexe na pilha: ela só volta para 1 quando o tempo passa (p.88, <see cref="OTempoPassa"/>); as
        /// condições também ficam.
        /// </summary>
        public Resultado CriarCena(string nome, string local, IEnumerable<string> participantes, string motivo)
        {
            var ids = (participantes ?? Enumerable.Empty<string>()).Distinct().ToList();
            foreach (var id in ids)
                if (!EmJogo(id, out _, out var erro))
                    return erro;
            if (ids.Count > MaximoEmCena)
                return Resultado.Falha($"Cabem {MaximoEmCena} heroínas numa cena (marcou {ids.Count}): as outras assistem.");
            var cena = new Cena
            {
                Id = Guid.NewGuid().ToString("N"),
                Nome = string.IsNullOrWhiteSpace(nome) ? $"cena {arquivo.Cenas.Count + 1}" : nome.Trim(),
                Local = string.IsNullOrWhiteSpace(local) ? null : local,
                Rodada = arquivo.Rodada,
                Quando = DateTime.Now.ToString("s"),
            };
            arquivo.Cenas.Add(cena);
            arquivo.CenaAtual = cena.Id;
            var mudancas = new List<Mudanca>();
            foreach (var h in arquivo.HeroisEmCena.Where(h => !ids.Contains(h)).ToList())
                mudancas.Add(new Mudanca { Campo = CampoDeCena(h), Antes = 1, Depois = 0 });
            foreach (var h in ids.Where(h => !arquivo.HeroisEmCena.Contains(h)))
                mudancas.Add(new Mudanca { Campo = CampoDeCena(h), Antes = 0, Depois = 1 });
            var nomeDoLocal = cena.Local == null ? null : personagens.CenarioDaSala(sala).FirstOrDefault(e => e.Id == cena.Local)?.Nome;
            var quem = ids.Count == 0 ? "ninguém em cena"
                : "em cena: " + string.Join(", ", ids.Select(h => personagens.Buscar(h)?.NomeExibicao ?? "?"));
            return RegistrarDaMesa(null, $"nova cena: {cena.Nome}" + (nomeDoLocal != null ? $" ({nomeDoLocal})" : string.Empty) + $" · {quem}",
                motivo, mudancas.ToArray());
        }

        /// <summary>
        /// A heroína entra na cena aberta (chegou pela narração, ou gastou um Controle, como o "Chegar na hora certa" da
        /// Bruta). O Controle gasto sai do assento no mesmo evento: desfazer devolve os dois.
        /// </summary>
        public Resultado EntrarEmCena(string heroi, string motivo, string bonusControle = null)
        {
            if (!EmJogo(heroi, out var p, out var erro))
                return erro;
            if (!TemCena)
                return Resultado.Falha("Ainda não há cena: crie uma.");
            if (HeroiEmCena(heroi))
                return Resultado.Falha($"{p.NomeExibicao} já está em cena.");
            if (arquivo.HeroisEmCena.Count >= MaximoEmCena)
                return Resultado.Falha($"A cena já tem {MaximoEmCena} heroínas: tire alguém antes.");
            var mudancas = new List<Mudanca> { new Mudanca { Campo = CampoDeCena(heroi), Antes = 0, Depois = 1 } };
            var texto = "entrou em cena";
            if (bonusControle != null)
            {
                var b = arquivo.Bonus.FirstOrDefault(x => x.Id == bonusControle && x.Ativo && x.Personagem == heroi && x.Tipo == BonusControle);
                if (b == null)
                    return Resultado.Falha("Esse Controle não está no assento dela.");
                mudancas.Add(new Mudanca { Campo = CampoDeBonus(b.Id), Antes = 1, Depois = 0 });
                texto += " gastando Controle" + (b.Contexto.Length > 0 ? $" ({b.Contexto})" : string.Empty);
            }
            return RegistrarDaMesa(p, texto, motivo, mudancas.ToArray());
        }

        /// <summary>A heroína sai da cena e passa a assistir (fugiu, afastou-se, nocauteada, foi para outra cena).</summary>
        public Resultado SairDeCena(string heroi, string motivo)
        {
            if (!EmJogo(heroi, out var p, out var erro))
                return erro;
            if (!HeroiEmCena(heroi))
                return Resultado.Falha($"{p.NomeExibicao} não está em cena.");
            return RegistrarDaMesa(p, "saiu de cena", motivo, new Mudanca { Campo = CampoDeCena(heroi), Antes = 1, Depois = 0 });
        }

        /// <summary>
        /// Corrige o nome e o local da cena aberta (o registro da cena; a entrada "nova cena" do log fica como foi
        /// escrita). Local null = sem local.
        /// </summary>
        public Resultado EditarCena(string nome, string local)
        {
            var cena = CenaAtual;
            if (cena == null)
                return Resultado.Falha("Ainda não há cena: crie uma.");
            if (string.IsNullOrWhiteSpace(nome))
                return Resultado.Falha("A cena precisa de um nome.");
            if (local != null && personagens.CenarioDaSala(sala).All(e => e.Id != local))
                return Resultado.Falha("Esse local não está no Cenário da campanha.");
            cena.Nome = nome.Trim();
            cena.Local = local;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// Por que ela está fora da cena: o motivo (ou o texto) do último evento que a tirou e ainda vale. "não entrou
        /// nesta cena" quando a cena nova começou sem ela; null se está em cena.
        /// </summary>
        public string MotivoFora(string heroi)
        {
            if (HeroiEmCena(heroi))
                return null;
            var campo = CampoDeCena(heroi);
            var saiu = arquivo.Eventos.LastOrDefault(e => !e.Oculto && TemEfeito(e) && e.Mudancas.Any(m => m.Campo == campo && m.Depois == 0));
            if (saiu == null || saiu.Descricao.StartsWith("nova cena"))
                return TemCena ? "não entrou nesta cena" : null;
            return string.IsNullOrWhiteSpace(saiu.Motivo) ? saiu.Descricao : saiu.Motivo;
        }


        /// <summary>O log visível em texto, do mais antigo ao mais novo ("r2  Aurora: ... — motivo"), para salvar.</summary>
        public string LogEmTexto()
        {
            var linhas = Visiveis.Select(e =>
            {
                var quem = e.Personagem != null ? personagens.Buscar(e.Personagem)?.NomeExibicao ?? "?"
                    : e.Rolagem?.Movimento != null ? "Herói genérico" : "Mesa";
                var motivo = string.IsNullOrWhiteSpace(e.Motivo) ? string.Empty : $"  — {e.Motivo}";
                return $"r{e.Rodada}  {quem}: {e.Descricao}{motivo}";
            });
            return string.Join("\n", linhas);
        }

        readonly Queue<int> dadosFixos = new Queue<int>();

        /// <summary>
        /// Debug: as próximas rolagens dão estas somas de 2d6 (2 a 12), na ordem, sem jogar os dados. O modificador soma
        /// por cima, como numa rolagem normal. Não fica gravado: fechar a mesa limpa. Lista vazia limpa a fila.
        /// </summary>
        public Resultado FixarDados(IEnumerable<int> somas)
        {
            var lista = somas.ToList();
            if (lista.Any(s => s < 2 || s > 12))
                return Resultado.Falha("2d6 vai de 2 a 12.");
            dadosFixos.Clear();
            foreach (var s in lista)
                dadosFixos.Enqueue(s);
            return Resultado.Sucesso();
        }

        /// <summary>As somas ainda na fila de dados fixos.</summary>
        public IReadOnlyList<int> DadosFixos => dadosFixos.ToList();

        /// <summary>Tira a próxima soma fixa, em dois dados (8 = 4 + 4; 11 = 6 + 5). False se a fila está vazia.</summary>
        public bool TirarDadoFixo(out int dado1, out int dado2)
        {
            dado1 = dado2 = 0;
            if (dadosFixos.Count == 0)
                return false;
            var soma = dadosFixos.Dequeue();
            dado1 = (soma + 1) / 2;
            dado2 = soma - dado1;
            return true;
        }

        // ---------- o log editado pelo mestre ----------

        /// <summary>Os eventos que o log mostra: os não apagados, do mais antigo ao mais novo.</summary>
        public IReadOnlyList<Evento> Visiveis => arquivo.Eventos.Where(e => !e.Oculto).ToList();

        /// <summary>Uma anotação no log (rolagem do console, nota do mestre): sem efeito na mesa.</summary>
        public void Anotar(string descricao, string motivo) => RegistrarDaMesa(null, descricao, motivo);

        /// <summary>Esconde todas as entradas visíveis (o efeito delas na mesa continua). Devolve quantas.</summary>
        public int EsconderTudo()
        {
            var visiveis = arquivo.Eventos.Where(e => !e.Oculto).ToList();
            foreach (var e in visiveis)
                e.Oculto = true;
            if (visiveis.Count > 0)
                Gravar();
            return visiveis.Count;
        }

        /// <summary>A entrada ainda tem efeito na mesa (mudou algo e não foi desfeita).</summary>
        public bool TemEfeito(Evento e) => e.Corrige == null && e.Mudancas.Count > 0 && !Corrigido(e);

        /// <summary>Corrige o texto de uma entrada (descrição e motivo). O efeito não muda.</summary>
        public Resultado EditarEvento(string id, string descricao, string motivo)
        {
            var e = arquivo.Eventos.FirstOrDefault(x => x.Id == id);
            if (e == null)
                return Resultado.Falha("Entrada não encontrada.");
            var d = (descricao ?? string.Empty).Trim();
            if (d.Length == 0)
                return Resultado.Falha("A descrição não fica vazia (para tirar a entrada, apague).");
            var m = (motivo ?? string.Empty).Trim();
            if (d == e.Descricao && m == e.Motivo)
                return Resultado.Sucesso();
            (e.Descricao, e.Motivo, e.Editado) = (d, m, true);
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// Apaga uma entrada do log: some da lista, mas fica no arquivo. Se ela ainda tem efeito, só apaga junto
        /// com o desfazer (<paramref name="desfazerEfeito"/>): o desfazer também fica oculto, e a mesa volta como
        /// se a entrada não tivesse acontecido.
        /// </summary>
        public Resultado ApagarEvento(string id, bool desfazerEfeito)
        {
            var e = arquivo.Eventos.FirstOrDefault(x => x.Id == id);
            if (e == null)
                return Resultado.Falha("Entrada não encontrada.");
            if (TemEfeito(e))
            {
                if (!desfazerEfeito)
                    return Resultado.Falha("Essa entrada ainda mudou algo na mesa: apagar também desfaz o efeito.");
                var r = Corrigir(e.Id, "apagado do log");
                if (!r.Ok)
                    return r;
                arquivo.Eventos.Last().Oculto = true; // o desfazer que acabou de entrar
            }
            e.Oculto = true;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// Ida e volta: <paramref name="volta"/> desfaz exatamente <paramref name="ida"/> (mesma pessoa, mesmos
        /// campos, sentidos opostos), nenhum dos dois tem motivo e nenhum é desfazer/refazer. Os dois se anulam:
        /// esconder o par não muda a mesa.
        /// </summary>
        static bool IdaEVolta(Evento ida, Evento volta) =>
            ida.Personagem == volta.Personagem &&
            string.IsNullOrWhiteSpace(ida.Motivo) && string.IsNullOrWhiteSpace(volta.Motivo) &&
            ida.Corrige == null && volta.Corrige == null && ida.Refaz == null && volta.Refaz == null &&
            ida.Mudancas.Count > 0 && ida.Mudancas.Count == volta.Mudancas.Count &&
            ida.Mudancas.All(m => volta.Mudancas.Any(v => v.Campo == m.Campo && v.Antes == m.Depois && v.Depois == m.Antes));

        /// <summary>
        /// Esconde os pares de ida e volta já no log (cliques de marcar e desmarcar seguidos). Não muda a mesa:
        /// os dois se anulam. Devolve quantos pares saíram.
        /// </summary>
        public int EsconderIdasEVoltas()
        {
            var pares = 0;
            var visiveis = Visiveis.ToList();
            for (var k = 1; k < visiveis.Count; k++)
            {
                if (visiveis[k - 1].Oculto || !IdaEVolta(visiveis[k - 1], visiveis[k]))
                    continue;
                visiveis[k - 1].Oculto = visiveis[k].Oculto = true;
                pares++;
                k++; // o par saiu inteiro; o próximo começa depois dele
            }
            if (pares > 0)
                Gravar();
            return pares;
        }

        // ---------- a rolagem e as intervenções depois do dado ----------

        /// <summary>O teto do modificador na rolagem (as intervenções depois do dado não respeitam).</summary>
        public const int TetoMinimo = -3, TetoMaximo = 4;
        /// <summary>Uma intervenção mexe de −2 a +2 (ajuda +1, Farol +2, renúncia +1/−2, Defender −2, custom).</summary>
        public const int IntervencaoMinima = -2, IntervencaoMaxima = 2;

        /// <summary>
        /// Lança uma rolagem de 2d6 no log, pendente: o resultado só vale quando o mestre aceita
        /// (<see cref="AceitarRolagem"/>). Quem rola é opcional (o console rola pela mesa).
        /// </summary>
        public Resultado Rolar(string personagem, int dado1, int dado2, int mod, string motivo)
        {
            if (dado1 < 1 || dado1 > 6 || dado2 < 1 || dado2 > 6)
                return Resultado.Falha($"Dados impossíveis: {dado1} e {dado2}.");
            Personagem p = null;
            if (personagem != null && !EmJogo(personagem, out p, out var erro))
                return erro;
            return Lancar(p, new RolagemDaMesa { Dados = new[] { dado1, dado2 }, Mod = mod }, motivo);
        }

        /// <summary>
        /// Rola um movimento básico: 2d6 + o Rótulo dele na ficha de quem rola. Sem personagem, é o herói
        /// genérico (+0). O alvo (personagem ou NPC) é opcional. Fica pendente como qualquer rolagem.
        /// Por enquanto só dado + Rótulo: condições, adiante, constante e Influência ainda não entram.
        /// O golpe poderoso soma as condições marcadas (teto +4), não tem alvo e pode vir com a
        /// <paramref name="origem"/>: a rolagem que o pediu, que precisa ter pedido golpe para quem rola.
        /// </summary>
        public Resultado RolarMovimento(string personagem, MovimentosBasicos.Movimento movimento, string alvo,
            int dado1, int dado2, string motivo, string origem = null)
        {
            if (movimento == null || (movimento.Rotulo == null && !movimento.SomaCondicoes))
                return Resultado.Falha("Esse movimento não diz o que a rolagem soma.");
            if (dado1 < 1 || dado1 > 6 || dado2 < 1 || dado2 > 6)
                return Resultado.Falha($"Dados impossíveis: {dado1} e {dado2}.");
            Personagem p = null;
            if (personagem != null && !EmJogo(personagem, out p, out var erro))
                return erro;
            if (alvo != null && alvo == personagem)
                return Resultado.Falha("O alvo não pode ser quem rola.");
            if (movimento.PedeAlvo && alvo == null)
                return Resultado.Falha(MensagemPedeAlvo(movimento));
            if (movimento.SomaCondicoes && alvo != null)
                return Resultado.Falha($"{movimento.Nome} não tem contra quem: rola quem toma o golpe.");
            var foraDeCena = ForaDeCena(personagem) ?? ForaDeCena(alvo);
            if (foraDeCena != null)
                return Resultado.Falha(foraDeCena);
            if (origem != null)
            {
                var pediu = arquivo.Eventos.FirstOrDefault(x => x.Id == origem);
                if (pediu?.Pedido != null)
                {
                    if (!PedidoAberto(pediu))
                        return Resultado.Falha("Esse pedido já foi rolado (ou desfeito).");
                    if (pediu.Personagem != personagem || pediu.Pedido.Movimento != movimento.Id || pediu.Pedido.Alvo != alvo)
                        return Resultado.Falha("A rolagem não bate com o pedido (quem, movimento ou contra quem).");
                }
                else if (pediu?.Rolagem == null)
                    return Resultado.Falha("A rolagem que pediu esta não está no log.");
                else if (!GolpesPedidos(pediu).Contains(personagem))
                    return Resultado.Falha($"Essa rolagem não pediu golpe poderoso para {p?.NomeExibicao ?? "o herói genérico"} (ou ele já foi rolado).");
            }
            var partes = PartesDoModificador(p, movimento);
            return Lancar(p, new RolagemDaMesa
            {
                Dados = new[] { dado1, dado2 },
                Mod = partes.Sum(x => x.Valor),
                Partes = partes.ToList(),
                Movimento = movimento.Id,
                NomeDoMovimento = movimento.Nome,
                Rotulo = movimento.Rotulo,
                Alvo = alvo,
                Origem = origem,
            }, motivo);
        }

        /// <summary>
        /// De onde vem o modificador de quem rola este movimento, antes do teto: o Rótulo (ou as condições marcadas, no
        /// golpe poderoso) e −2 por condição marcada que pesa no movimento (nucleo.json, p.89–90). O herói genérico rola
        /// +0, sem condições. Adiante, constante e Influência ainda não entram.
        /// </summary>
        public IReadOnlyList<ParteDoModificador> PartesDoModificador(Personagem p, MovimentosBasicos.Movimento movimento)
        {
            var partes = new List<ParteDoModificador>();
            if (movimento.SomaCondicoes)
                partes.Add(new ParteDoModificador { Origem = "condicoes", Texto = "condições", Valor = p?.CondicoesMarcadas.Count ?? 0 });
            else
                partes.Add(new ParteDoModificador
                {
                    Origem = CampoRotulo(movimento.Rotulo),
                    Texto = Rotulos.Nome(movimento.Rotulo),
                    Valor = p == null ? 0 : personagens.ValorDeJogo(p, CampoRotulo(movimento.Rotulo)),
                });
            if (p == null || regrasDeCondicoes == null)
                return partes;
            foreach (var c in Condicoes.Todas.Where(c => p.CondicoesMarcadas.Contains(c) && regrasDeCondicoes.MovimentosAfetados(c).Contains(movimento.Id)))
                partes.Add(new ParteDoModificador { Origem = CampoCondicao(c), Texto = Condicoes.Nome(c), Valor = regrasDeCondicoes.Penalidade });
            return partes;
        }

        /// <summary>"Perigoso +1, Assustado −2" (com "teto" quando a soma passou do limite).</summary>
        public static string TextoDasPartes(RolagemDaMesa r)
        {
            static string Sinal(int n) => n < 0 ? $"−{-n}" : $"+{n}";
            var texto = string.Join(", ", r.Partes.Select(x => $"{x.Texto} {Sinal(x.Valor)}"));
            return r.Partes.Sum(x => x.Valor) != r.Mod ? texto + $", teto {Sinal(r.Mod)}" : texto;
        }

        /// <summary>
        /// Anota uma rolagem para depois (/eda, /tgp): fica no log como pedido, com botão, até alguém rolar com a origem
        /// nele. Confere o que a rolagem conferiria: quem está em jogo, contra quem (o Enfrentar pede; o golpe não tem).
        /// </summary>
        public Resultado PedirRolagem(string personagem, MovimentosBasicos.Movimento movimento, string alvo, string motivo)
        {
            if (movimento == null)
                return Resultado.Falha("Movimento desconhecido.");
            if (!EmJogo(personagem, out var p, out var erro))
                return erro;
            if (alvo != null && alvo == personagem)
                return Resultado.Falha("O alvo não pode ser quem rola.");
            if (movimento.PedeAlvo && alvo == null)
                return Resultado.Falha(MensagemPedeAlvo(movimento));
            if (movimento.SomaCondicoes && alvo != null)
                return Resultado.Falha($"{movimento.Nome} não tem contra quem: rola quem toma o golpe.");
            var nomeDoAlvo = alvo == null ? string.Empty : " → " + (personagens.Buscar(alvo)?.NomeExibicao ?? alvo);
            var evento = NovoEvento(p, $"pendente: {movimento.Nome}{nomeDoAlvo}", motivo);
            evento.Pedido = new PedidoDeRolagem { Movimento = movimento.Id, NomeDoMovimento = movimento.Nome, Alvo = alvo };
            arquivo.Eventos.Add(evento);
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>O pedido ainda espera a rolagem (não foi rolado, desfeito nem escondido).</summary>
        public bool PedidoAberto(Evento e) =>
            e?.Pedido != null && !e.Oculto && !Corrigido(e) &&
            !arquivo.Eventos.Any(x => x.Rolagem?.Origem == e.Id && !x.Oculto && !Corrigido(x));

        /// <summary>
        /// Quem esta rolagem aceita mandou tomar um golpe poderoso e ainda não rolou: o retorno da troca para quem rolou
        /// e, contra PJ, o alvo. Null na lista é o herói genérico. Vazia se a rolagem foi desfeita ou escondida.
        /// </summary>
        public IReadOnlyList<string> GolpesPedidos(Evento e)
        {
            var r = e?.Rolagem;
            if (r?.Troca == null || !r.Aceita || e.Oculto || Corrigido(e))
                return new List<string>();
            var quem = new List<string>();
            if (r.Troca.Retorno == TrocaDeGolpes.RetornoGolpePoderoso)
                quem.Add(e.Personagem);
            if (r.Troca.AlvoGolpePoderoso && r.Alvo != null)
                quem.Add(r.Alvo);
            return quem.Where(id => !arquivo.Eventos.Any(x =>
                x.Rolagem?.Origem == e.Id && x.Personagem == id && !x.Oculto && !Corrigido(x))).ToList();
        }

        /// <summary>A recusa de um movimento que pede alvo, rolado contra ninguém.</summary>
        public static string MensagemPedeAlvo(MovimentosBasicos.Movimento movimento) =>
            $"{movimento.Nome} pede contra quem: uma ameaça em cena. Contra o nada (obstáculo, ambiente, sentidos), é liberar os poderes.";

        Resultado Lancar(Personagem p, RolagemDaMesa rolagem, string motivo)
        {
            rolagem.Mod = Math.Max(TetoMinimo, Math.Min(TetoMaximo, rolagem.Mod)); // o teto vale na rolagem
            var evento = NovoEvento(p, null, motivo);
            evento.Rolagem = rolagem;
            evento.Descricao = TextoDaRolagem(rolagem);
            arquivo.Eventos.Add(evento);
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>A rolagem ainda espera o mestre aceitar (e não foi desfeita nem escondida).</summary>
        public bool Pendente(Evento e) => e.Rolagem != null && !e.Rolagem.Aceita && !e.Oculto && !Corrigido(e);

        /// <summary>
        /// Alguém intervém numa rolagem pendente. O custo na pilha sai na hora e fica no mesmo evento: desfazer
        /// a rolagem devolve os pontos.
        /// </summary>
        public Resultado Intervir(string eventoId, Intervencao intervencao, MovimentosBasicos.Movimento movimento = null)
        {
            var e = arquivo.Eventos.FirstOrDefault(x => x.Id == eventoId);
            if (e?.Rolagem == null)
                return Resultado.Falha("Rolagem não encontrada.");
            if (movimento != null && movimento.Id == e.Rolagem.Movimento && movimento.SemIntervencao)
                return Resultado.Falha($"{movimento.Nome} não aceita intervenção.");
            if (!Pendente(e))
                return Resultado.Falha("Essa rolagem já foi aceita (ou desfeita): não aceita mais intervenção.");
            if (intervencao == null || string.IsNullOrWhiteSpace(intervencao.Quem))
                return Resultado.Falha("Quem intervém?");
            if (ForaDeCena(intervencao.Quem) is string quemFora)
                return Resultado.Falha(quemFora + " Quem intervém precisa poder ajudar na ficção (p.84).");
            if (string.IsNullOrWhiteSpace(intervencao.DeOnde))
                return Resultado.Falha("De onde vem a intervenção?");
            if (intervencao.Mod < IntervencaoMinima || intervencao.Mod > IntervencaoMaxima)
                return Resultado.Falha($"Uma intervenção vai de {IntervencaoMinima} a +{IntervencaoMaxima}.");
            if (intervencao.CustoEquipe < 0)
                return Resultado.Falha("Custo negativo na pilha.");
            // Pilha de Equipe (p.84): quem gasta decide o que é. Outra pessoa = ajuda; quem rola = egoísta. Cada
            // pessoa gasta 1 ponto por jogada (vários colegas podem ajudar a mesma rolagem).
            bool DaPilha(string deOnde) => deOnde == Intervencao.DaPilha || deOnde == Intervencao.AjudaDaPilha || deOnde == Intervencao.EgoistaDaPilha;
            if (DaPilha(intervencao.DeOnde))
            {
                if (e.Rolagem.Intervencoes.Any(x => DaPilha(x.DeOnde) && x.Quem == intervencao.Quem))
                    return Resultado.Falha($"{personagens.Buscar(intervencao.Quem)?.NomeExibicao ?? "Essa pessoa"} já gastou da pilha nesta rolagem: " +
                                           "cada pessoa gasta 1 ponto por jogada.");
                intervencao.DeOnde = e.Personagem != null && intervencao.Quem == e.Personagem
                    ? Intervencao.EgoistaDaPilha : Intervencao.AjudaDaPilha;
            }
            if (intervencao.CustoEquipe > arquivo.Equipe)
                return Resultado.Falha($"A pilha tem {arquivo.Equipe} ponto{(arquivo.Equipe == 1 ? "" : "s")}.");
            intervencao.Detalhe = (intervencao.Detalhe ?? string.Empty).Trim();
            if (intervencao.CustoEquipe > 0)
                MudarEquipeDaRolagem(e, -intervencao.CustoEquipe);
            e.Rolagem.Intervencoes.Add(intervencao);
            e.Descricao = TextoDaRolagem(e.Rolagem, e);
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Tira uma intervenção da rolagem pendente (engano): devolve o que ela gastou da pilha.</summary>
        public Resultado RetirarIntervencao(string eventoId, int indice)
        {
            var e = arquivo.Eventos.FirstOrDefault(x => x.Id == eventoId);
            if (e?.Rolagem == null || !Pendente(e))
                return Resultado.Falha("Só se retira intervenção de rolagem pendente.");
            if (indice < 0 || indice >= e.Rolagem.Intervencoes.Count)
                return Resultado.Falha("Intervenção não encontrada.");
            var i = e.Rolagem.Intervencoes[indice];
            if (i.CustoEquipe > 0)
                MudarEquipeDaRolagem(e, i.CustoEquipe);
            e.Rolagem.Intervencoes.RemoveAt(indice);
            e.Descricao = TextoDaRolagem(e.Rolagem, e);
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// O mestre aceita: grava o resultado (sucesso/falha), o tipo (total/drawback/falha), a hora e, com o
        /// movimento, as opções escolhidas. O movimento confere a conta: 10+ escolhe tantas, 7–9 tantas, falha nenhuma,
        /// e opção com alternativas dentro pede uma marcada. Sem movimento (rolagem livre), não há escolhas.
        /// </summary>
        public Resultado AceitarRolagem(string eventoId, MovimentosBasicos.Movimento movimento = null,
            IReadOnlyList<EscolhaDaRolagem> escolhas = null, TrocaDeGolpes troca = null)
        {
            var e = arquivo.Eventos.FirstOrDefault(x => x.Id == eventoId);
            if (e?.Rolagem == null)
                return Resultado.Falha("Rolagem não encontrada.");
            if (!Pendente(e))
                return Resultado.Falha("Essa rolagem já foi aceita (ou desfeita).");
            var r = e.Rolagem;
            var tipo = RolagemDaMesa.TipoDe(r.Total);
            escolhas ??= new List<EscolhaDaRolagem>();
            var gravadas = new List<EscolhaDaRolagem>();
            if (movimento != null)
            {
                if (movimento.Id != r.Movimento)
                    return Resultado.Falha("Essa rolagem é de outro movimento.");
                var faixa = tipo == RolagemDaMesa.TipoTotal ? MovimentosBasicos.Faixa.Forte
                    : tipo == RolagemDaMesa.TipoDrawback ? MovimentosBasicos.Faixa.Parcial : MovimentosBasicos.Faixa.Falha;
                var opcoes = movimento.OpcoesNo(faixa);
                var pede = Math.Min(movimento.EscolhasNo(faixa), opcoes.Count);
                if (escolhas.Count != pede)
                    return Resultado.Falha(pede == 0
                        ? $"No resultado {RolagemDaMesa.NomeDoTipo(tipo)} não há o que escolher."
                        : $"No resultado {RolagemDaMesa.NomeDoTipo(tipo)}, escolha {pede} (marcou {escolhas.Count}).");
                var rolou = e.Personagem == null ? null : personagens.Buscar(e.Personagem);
                var usadas = new List<string>(); // condições já tomadas por outra escolha desta rolagem
                var erroDaEscolha = ConferirEscolhas(r, movimento, opcoes, escolhas, rolou, usadas, gravadas);
                if (erroDaEscolha != null)
                    return Resultado.Falha(erroDaEscolha);
            }
            else if (escolhas.Count > 0)
                return Resultado.Falha("Escolhas pedem o movimento da rolagem.");

            TrocaDeGolpes trocaGravada = null;
            if (movimento != null && movimento.TrocaGolpes && tipo != RolagemDaMesa.TipoFalha)
            {
                var erroDaTroca = ConferirTroca(e, movimento, gravadas, troca ?? new TrocaDeGolpes(), out trocaGravada);
                if (erroDaTroca != null)
                    return Resultado.Falha(erroDaTroca);
            }

            r.Resultado = RolagemDaMesa.ResultadoDe(r.Total);
            r.Tipo = tipo;
            r.Escolhas = gravadas;
            r.Troca = trocaGravada;
            r.AceitaEm = DateTime.Now.ToString("s");
            r.Aceita = true;
            AplicarAceite(e, movimento);
            e.Descricao = TextoDaRolagem(r, e);
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// Confere as escolhas marcadas contra as opções que valem e grava cada uma com o texto de agora. Opção com
        /// alternativas pede uma marcada; opção que marca condições pede quais (entre as livres de quem rolou, sem
        /// repetir as de outra escolha); opção com escolha dentro ("duas da lista de 7–9") confere as de dentro.
        /// Devolve o erro ou null.
        /// </summary>
        string ConferirEscolhas(RolagemDaMesa r, MovimentosBasicos.Movimento movimento,
            IReadOnlyList<MovimentosBasicos.OpcaoDeEscolha> opcoes, IEnumerable<EscolhaDaRolagem> escolhas, Personagem rolou,
            List<string> usadas, List<EscolhaDaRolagem> gravadas)
        {
            foreach (var escolha in escolhas)
            {
                var o = opcoes.FirstOrDefault(x => x.Id == escolha.Opcao);
                if (o == null)
                    return "Essa opção não vale neste resultado.";
                if (gravadas.Any(g => g.Opcao == o.Id))
                    return $"\"{o.Texto}\" marcada duas vezes.";
                var temAlternativas = o.Alternativas.Count > 0;
                if (temAlternativas && (escolha.Subopcao == null || escolha.Subopcao < 0 || escolha.Subopcao >= o.Alternativas.Count))
                    return $"Em \"{o.Texto}\", marque uma das alternativas.";
                if (temAlternativas && AlvoEhPj(r) && !movimento.AlternativaValeContraPj(o.Id, escolha.Subopcao.Value))
                    return $"Contra PJ, \"{o.Alternativas[escolha.Subopcao.Value]}\" não vale: brigar com colega não enche a pilha.";
                var gravada = new EscolhaDaRolagem
                {
                    Opcao = o.Id,
                    Texto = o.Texto,
                    Subopcao = temAlternativas ? escolha.Subopcao : null,
                    Subtexto = temAlternativas ? o.Alternativas[escolha.Subopcao.Value] : null,
                };

                if (escolha.Para != null && movimento.LembreteNaOposicao(o.Id) != null)
                {
                    var para = personagens.Buscar(escolha.Para);
                    if (para == null || para.Sala != sala || (para.Controle == Controle.Mestre && !EstaEmCena(para.Id)))
                        return $"Em \"{o.Texto}\": quem ganha precisa estar na mesa (heroína ou NPC em cena).";
                    if (rolou != null && para.Id == rolou.Id)
                        return $"Em \"{o.Texto}\": quem ganha não é quem rolou.";
                    gravada.Para = para.Id;
                }

                var n = movimento.CondicoesQueMarca(o.Id);
                if (n > 0)
                {
                    var livres = Condicoes.Todas.Where(c => rolou?.CondicoesMarcadas.Contains(c) != true && !usadas.Contains(c)).ToList();
                    var marca = (escolha.Condicoes ?? new List<string>()).ToList();
                    var cabe = Math.Min(n, livres.Count);
                    if (marca.Distinct().Count() != marca.Count || marca.Any(c => !livres.Contains(c)))
                        return $"Em \"{o.Texto}\": escolha condições diferentes, entre as que ainda não estão marcadas.";
                    if (marca.Count != cabe)
                        return $"Em \"{o.Texto}\", marque {cabe} condiç{(cabe == 1 ? "ão" : "ões")} (marcou {marca.Count}).";
                    usadas.AddRange(marca);
                    gravada.Condicoes = marca;
                    gravada.FaltaramCondicoes = n - cabe;
                }

                var (nDentro, dentro) = movimento.OpcoesDentro(o.Id);
                if (dentro.Count > 0)
                {
                    var marcadasDentro = escolha.Dentro ?? new List<EscolhaDaRolagem>();
                    var pedeDentro = Math.Min(nDentro, dentro.Count);
                    if (marcadasDentro.Count != pedeDentro)
                        return $"Em \"{o.Texto}\", escolha {pedeDentro} (marcou {marcadasDentro.Count}).";
                    gravada.Dentro = new List<EscolhaDaRolagem>();
                    var erro = ConferirEscolhas(r, movimento, dentro, marcadasDentro, rolou, usadas, gravada.Dentro);
                    if (erro != null)
                        return erro;
                }
                gravadas.Add(gravada);
            }
            return null;
        }

        /// <summary>
        /// O que o aceite muda na mesa, dentro do próprio evento da rolagem (desfazer a rolagem desfaz tudo):
        /// na falha, +1 potencial para quem rolou; na troca, a condição do alvo NPC e a condição do retorno;
        /// nas escolhas, as condições que elas marcam e o que a alternativa marcada dá à pilha (+1 Equipe da
        /// oportunidade). O golpe poderoso é outra rolagem, com origem nesta (<see cref="GolpesPedidos"/>).
        /// O herói genérico não tem ficha: o que seria dele fica só gravado.
        /// </summary>
        void AplicarAceite(Evento e, MovimentosBasicos.Movimento movimento)
        {
            var r = e.Rolagem;
            var p = e.Personagem == null ? null : personagens.Buscar(e.Personagem);
            var novas = new List<Mudanca>();
            if (r.Tipo == RolagemDaMesa.TipoFalha && p != null)
                novas.Add(new Mudanca { Campo = CampoPotencial, Antes = p.Potencial, Depois = p.Potencial + 1 });
            if (r.Troca?.CondicaoDoAlvo != null && r.Alvo != null && !r.Troca.AlvoGolpePoderoso)
                novas.Add(new Mudanca { Campo = CampoDeVilao(r.Alvo, r.Troca.CondicaoDoAlvo), Antes = 0, Depois = 1 });
            if (r.Troca?.Retorno == TrocaDeGolpes.RetornoCondicao && p != null)
                novas.Add(new Mudanca { Campo = CampoCondicao(r.Troca.CondicaoDeQuemRolou), Antes = 0, Depois = 1 });
            // As condições que as escolhas mandam marcar ("aguentar a dor", também dentro de "duas da lista de 7–9").
            if (p != null)
                novas.AddRange(r.Escolhas.SelectMany(x => x.ComAsDeDentro).SelectMany(x => x.Condicoes ?? new List<string>())
                    .Select(c => new Mudanca { Campo = CampoCondicao(c), Antes = 0, Depois = 1 }));
            // O lembrete no cartão de quem ganha com a opção ("abertura: Aurora cedeu terreno"), no mesmo evento.
            foreach (var x in r.Escolhas.SelectMany(x => x.ComAsDeDentro).Where(x => x.Para != null))
            {
                var modelo = movimento?.LembreteNaOposicao(x.Opcao);
                if (modelo == null)
                    continue;
                var bonus = new BonusDeMesa
                {
                    Id = Guid.NewGuid().ToString("N"), Personagem = x.Para, Tipo = BonusLembrete,
                    Contexto = modelo.Replace("{quem}", p?.NomeExibicao ?? HeroiGenerico),
                };
                arquivo.Bonus.Add(bonus);
                novas.Add(new Mudanca { Campo = CampoDeBonus(bonus.Id), Antes = 0, Depois = 1 });
            }
            if (novas.Count > 0)
            {
                Aplicar(p, novas);
                e.Mudancas.AddRange(novas);
            }
            // A condição do impressionar num alvo PJ é da ficha dele, não de quem rolou: entra como entrada própria
            // no log, logo depois da rolagem (e se desfaz à parte).
            if (r.Troca?.AlvoGolpePoderoso == true && r.Troca.CondicaoDoAlvo != null)
                AlternarCondicao(r.Alvo, r.Troca.CondicaoDoAlvo, $"impressionar ({p?.NomeExibicao ?? "herói genérico"})");
            if (movimento == null)
                return;
            var equipe = r.Escolhas.Where(x => x.Subopcao != null).Sum(x => movimento.EquipeDaAlternativa(x.Opcao, x.Subopcao.Value));
            if (equipe != 0)
                MudarEquipeDaRolagem(e, equipe);
        }

        bool AlvoEhPj(RolagemDaMesa r) => r.Alvo != null && personagens.Buscar(r.Alvo)?.Papel == Papel.Heroi;

        /// <summary>
        /// Confere a troca de golpes marcada pelo mestre. Contra NPC: a condição do alvo (uma que ele tem e ainda não
        /// marcou; se já marcou todas, nenhuma) e o retorno de quem rolou (condição ou golpe poderoso; ileso com
        /// "resistir"). Contra PJ: os dois tomam golpe poderoso, e "resistir" livra quem escolheu. Devolve o erro ou null.
        /// </summary>
        string ConferirTroca(Evento e, MovimentosBasicos.Movimento movimento, List<EscolhaDaRolagem> escolhas,
            TrocaDeGolpes troca, out TrocaDeGolpes gravada)
        {
            gravada = new TrocaDeGolpes();
            var alvo = e.Rolagem.Alvo == null ? null : personagens.Buscar(e.Rolagem.Alvo);
            var alvoPj = alvo != null && alvo.Papel == Papel.Heroi;
            if (alvoPj)
            {
                gravada.AlvoGolpePoderoso = true;
                // Contra PJ, a troca não dá condição; o "impressionar" deixa o MJ dar uma (opcional).
                if (troca.CondicaoDoAlvo != null)
                {
                    if (!escolhas.Any(x => movimento.ReflectePercepcao(x.Opcao)))
                        return "Condição no alvo PJ só com \"impressionar, surpreender ou assustar\" escolhido.";
                    if (!Condicoes.Todas.Contains(troca.CondicaoDoAlvo))
                        return "Condição desconhecida.";
                    if (alvo.CondicoesMarcadas.Contains(troca.CondicaoDoAlvo))
                        return $"{alvo.NomeExibicao} já está com {Condicoes.Nome(troca.CondicaoDoAlvo)} marcada.";
                    gravada.CondicaoDoAlvo = troca.CondicaoDoAlvo;
                }
            }
            else if (alvo != null)
            {
                var marcadas = CondicoesMarcadasDoVilao(alvo.Id);
                var livres = (alvo.Vilao?.Condicoes ?? new List<string>()).Where(c => !marcadas.Contains(c)).ToList();
                if (livres.Count > 0)
                {
                    if (troca.CondicaoDoAlvo == null || !livres.Contains(troca.CondicaoDoAlvo))
                        return $"Troca de golpes: marque a condição de {alvo.NomeExibicao} (o MJ escolhe).";
                    gravada.CondicaoDoAlvo = troca.CondicaoDoAlvo;
                }
            }

            if (escolhas.Any(x => movimento.AnulaRetorno(x.Opcao)))
                gravada.Retorno = TrocaDeGolpes.RetornoIleso;
            else if (alvoPj)
                gravada.Retorno = TrocaDeGolpes.RetornoGolpePoderoso;
            else if (troca.Retorno == TrocaDeGolpes.RetornoGolpePoderoso)
                gravada.Retorno = TrocaDeGolpes.RetornoGolpePoderoso;
            else if (troca.Retorno == TrocaDeGolpes.RetornoCondicao)
            {
                var rolou = e.Personagem == null ? null : personagens.Buscar(e.Personagem);
                if (troca.CondicaoDeQuemRolou == null || !Condicoes.Todas.Contains(troca.CondicaoDeQuemRolou))
                    return "Troca de golpes: qual condição quem rolou marca?";
                if (rolou != null && rolou.CondicoesMarcadas.Contains(troca.CondicaoDeQuemRolou))
                    return $"{rolou.NomeExibicao} já está com {Condicoes.Nome(troca.CondicaoDeQuemRolou)} marcada.";
                gravada.Retorno = TrocaDeGolpes.RetornoCondicao;
                gravada.CondicaoDeQuemRolou = troca.CondicaoDeQuemRolou;
            }
            else
                return "Troca de golpes: o retorno é uma condição ou tomar um golpe poderoso (o MJ escolhe um).";
            return null;
        }

        /// <summary>
        /// Muda a pilha por conta da rolagem, numa única mudança de "equipe" no evento (duas mudanças do mesmo
        /// campo no evento confundiriam o desfazer, que calcula o inverso de cada uma a partir do valor de agora).
        /// </summary>
        void MudarEquipeDaRolagem(Evento e, int delta)
        {
            var novo = Math.Max(0, arquivo.Equipe + delta);
            var m = e.Mudancas.FirstOrDefault(x => x.Campo == CampoEquipe);
            if (m == null)
                e.Mudancas.Add(m = new Mudanca { Campo = CampoEquipe, Antes = arquivo.Equipe, Depois = arquivo.Equipe });
            m.Depois += novo - arquivo.Equipe;
            Aplicar(null, new List<Mudanca> { new Mudanca { Campo = CampoEquipe, Antes = arquivo.Equipe, Depois = novo } });
            if (m.Antes == m.Depois)
                e.Mudancas.Remove(m);
        }

        /// <summary>
        /// O texto da rolagem no log: a linha do dado ("rolou 2d6+1: 4 + 5 + 1 = 10 (10+)"), uma linha por
        /// intervenção e o resultado final quando houve intervenção.
        /// </summary>
        string TextoDaRolagem(RolagemDaMesa r, Evento dono = null)
        {
            static string Sinal(int n) => n > 0 ? $"+{n}" : n < 0 ? $"−{-n}" : "±0";
            var sinal = r.Mod == 0 ? string.Empty : r.Mod > 0 ? $"+{r.Mod}" : r.Mod.ToString();
            var modTexto = r.Mod == 0 ? string.Empty : $" {(r.Mod > 0 ? "+" : "−")} {Math.Abs(r.Mod)}";
            var texto = $"rolou 2d6{sinal}: {r.Dados[0]} + {r.Dados[1]}{modTexto} = {r.Inicial} ({RolagemDaMesa.Faixa(r.Inicial)})";
            if (r.Movimento != null)
            {
                var alvo = r.Alvo == null ? string.Empty : " → " + (personagens.Buscar(r.Alvo)?.NomeExibicao ?? r.Alvo);
                var soma = r.Partes.Count > 0 ? TextoDasPartes(r)
                    : $"{(r.Rotulo != null ? Rotulos.Nome(r.Rotulo) : "condições")} {(r.Mod < 0 ? "−" : "+")}{Math.Abs(r.Mod)}";
                texto = $"{r.NomeDoMovimento}{alvo} ({soma}): " +
                        $"{r.Dados[0]} + {r.Dados[1]}{modTexto} = {r.Inicial} ({RolagemDaMesa.Faixa(r.Inicial)})";
            }
            var pediu = r.Origem == null ? null : arquivo.Eventos.FirstOrDefault(x => x.Id == r.Origem);
            if (pediu?.Rolagem != null)
            {
                var alvoDeLa = pediu.Rolagem.Alvo == null ? string.Empty : " → " + (personagens.Buscar(pediu.Rolagem.Alvo)?.NomeExibicao ?? pediu.Rolagem.Alvo);
                var deQuem = pediu.Personagem == null ? HeroiGenerico : personagens.Buscar(pediu.Personagem)?.NomeExibicao ?? "?";
                texto += $"\n  pedido por: {pediu.Rolagem.NomeDoMovimento ?? "rolagem"} de {deQuem}{alvoDeLa} (r{pediu.Rodada})";
            }
            else if (pediu?.Pedido != null)
                texto += $"\n  pedido no log (r{pediu.Rodada})" + (string.IsNullOrWhiteSpace(pediu.Motivo) ? string.Empty : ": " + pediu.Motivo);
            foreach (var i in r.Intervencoes)
            {
                var quem = personagens.Buscar(i.Quem)?.NomeExibicao ?? i.Quem;
                var detalhe = string.IsNullOrWhiteSpace(i.Detalhe) ? string.Empty : ": " + i.Detalhe;
                var custo = i.CustoEquipe > 0 ? $", −{i.CustoEquipe} Equipe" : string.Empty;
                var segunda = string.IsNullOrWhiteSpace(i.Rolagem) ? string.Empty : $" [{i.Rolagem}]";
                texto += $"\n  {Sinal(i.Mod)} {quem} ({i.DeOnde}{detalhe}{custo}){segunda}";
            }
            if (r.Intervencoes.Count > 0)
                texto += $"\n  = {r.Total} ({RolagemDaMesa.Faixa(r.Total)})";
            if (r.Aceita)
            {
                texto += r.Tipo == null ? " · aceito" : " · " + RolagemDaMesa.NomeDoTipo(r.Tipo);
                if (r.Escolhas.Count > 0)
                    texto += ": " + string.Join(", ", r.Escolhas.Select(TextoDaEscolha));
                if (r.Escolhas.SelectMany(x => x.ComAsDeDentro).Any(x => x.FaltaramCondicoes > 0))
                    texto += "\n  precisa marcar condição e não tem livre: sai de cena";
                if (r.Troca != null)
                {
                    var alvo = r.Alvo == null ? null : personagens.Buscar(r.Alvo)?.NomeExibicao ?? r.Alvo;
                    var doAlvo = alvo == null ? null
                        : r.Troca.AlvoGolpePoderoso ? $"{alvo} toma golpe poderoso" +
                          (r.Troca.CondicaoDoAlvo != null ? $" e marca {Condicoes.Nome(r.Troca.CondicaoDoAlvo)}" : "")
                        : r.Troca.CondicaoDoAlvo != null ? $"{alvo} marca {Condicoes.Nome(r.Troca.CondicaoDoAlvo)}"
                        : $"{alvo} sem condição livre";
                    // Com o nome de quem rolou: "retorno: ileso" sozinho parecia falar do alvo.
                    var quemRolou = dono?.Personagem == null ? HeroiGenerico : personagens.Buscar(dono.Personagem)?.NomeExibicao ?? "quem rolou";
                    var retorno = r.Troca.Retorno == TrocaDeGolpes.RetornoIleso ? $"{quemRolou} não recebe retorno (resistiu)"
                        : r.Troca.Retorno == TrocaDeGolpes.RetornoGolpePoderoso ? $"{quemRolou} toma golpe poderoso (role à parte)"
                        : $"{quemRolou} marca {Condicoes.Nome(r.Troca.CondicaoDeQuemRolou)}";
                    texto += "\n  troca de golpes: " + (doAlvo == null ? retorno : doAlvo + " · " + retorno);
                }
                if (dono != null && dono.Mudancas.Any(m => m.Campo == CampoPotencial))
                    texto += "\n  falha: +1 potencial";
            }
            return texto;
        }

        const string HeroiGenerico = "herói genérico";

        /// <summary>"Aguentar a dor [Irritado, Inseguro]", "Duas opções da lista de 7-9: Ceder terreno, ...".</summary>
        string TextoDaEscolha(EscolhaDaRolagem x)
        {
            var texto = x.Subtexto == null ? x.Texto : $"{x.Texto} ({x.Subtexto})";
            if (x.Condicoes?.Count > 0)
                texto += $" [{string.Join(", ", x.Condicoes.Select(Condicoes.Nome))}]";
            if (x.Para != null)
                texto += $" [lembrete em {personagens.Buscar(x.Para)?.NomeExibicao ?? "?"}]";
            if (x.Dentro?.Count > 0)
                texto += " → " + string.Join(" + ", x.Dentro.Select(TextoDaEscolha));
            return texto;
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
            // Clique que desfaz exatamente o clique anterior (marcou/desmarcou): o par some do log na hora.
            var anterior = arquivo.Eventos.LastOrDefault(e => !e.Oculto);
            arquivo.Eventos.Add(evento);
            if (anterior != null && IdaEVolta(anterior, evento))
                anterior.Oculto = evento.Oculto = true;
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
                if (m.Campo.StartsWith("npc_em_cena:"))
                {
                    var npc = m.Campo.Substring("npc_em_cena:".Length);
                    arquivo.EmCena.Remove(npc);
                    if (m.Depois > 0)
                        arquivo.EmCena.Add(npc); // entra no fim: a ordem da mesa é a ordem de entrada
                    continue;
                }
                if (m.Campo.StartsWith("cena:"))
                {
                    var heroi = m.Campo.Substring("cena:".Length);
                    arquivo.HeroisEmCena.Remove(heroi);
                    if (m.Depois > 0)
                        arquivo.HeroisEmCena.Add(heroi);
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

        static bool DaMesa(string campo) =>
            campo == CampoEquipe || campo.StartsWith("vilao:") || campo.StartsWith("bonus:") || campo.StartsWith("cena:") ||
            campo.StartsWith("npc_em_cena:");

        int ValorDaMesa(string campo)
        {
            if (campo == CampoEquipe)
                return arquivo.Equipe;
            if (campo.StartsWith("bonus:"))
                return arquivo.Bonus.FirstOrDefault(b => "bonus:" + b.Id == campo)?.Ativo == true ? 1 : 0;
            if (campo.StartsWith("cena:"))
                return arquivo.HeroisEmCena.Contains(campo.Substring("cena:".Length)) ? 1 : 0;
            if (campo.StartsWith("npc_em_cena:"))
                return arquivo.EmCena.Contains(campo.Substring("npc_em_cena:".Length)) ? 1 : 0;
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
            campo.StartsWith("condicao:") || campo.StartsWith("vilao:") || campo.StartsWith("bonus:") || campo.StartsWith("cena:") ||
            campo.StartsWith("npc_em_cena:")
                ? Math.Max(0, Math.Min(1, valor))
            : campo == CampoPotencial || campo == CampoEquipe || campo.StartsWith("contador:") ? Math.Max(0, valor)
            : campo.StartsWith("rotulo:") ? Math.Max(Rotulos.Minimo, Math.Min(Rotulos.Maximo, valor))
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
            /// <summary>As cenas da campanha, na ordem (nada se exclui).</summary>
            [JsonProperty("cenas")] public List<Cena> Cenas { get; set; } = new List<Cena>();
            /// <summary>A cena aberta; null = mesa vazia, esperando "criar cena".</summary>
            [JsonProperty("cena_atual", NullValueHandling = NullValueHandling.Ignore)] public string CenaAtual { get; set; }
            /// <summary>As heroínas na cena aberta; as outras assistem.</summary>
            [JsonProperty("herois_em_cena")] public List<string> HeroisEmCena { get; set; } = new List<string>();
            [JsonProperty("eventos")] public List<Evento> Eventos { get; set; } = new List<Evento>();
        }
    }
}
