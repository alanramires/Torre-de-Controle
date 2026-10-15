using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// NPCs (controle do mestre). Mesma entidade da personagem: sem arquétipo nem ciclo de criação;
    /// no lugar, o perfil PNJ e, se for vilão, a ficha de vilão. Nascem numa sala (ou da ficha de uma
    /// heroína na sala) ou no laboratório de NPCs (sala null), de onde se copiam para as salas.
    /// </summary>
    public sealed partial class CadastroPersonagens
    {
        /// <summary>NPCs da sala: ativos primeiro, na ordem que o mestre arrastou.</summary>
        public IReadOnlyList<Personagem> NpcsDaSala(string sala) =>
            Ordenacao.Ordenar(personagens.Where(p => p.Sala == sala && p.Controle == Controle.Mestre), p => p.NomeExibicao);

        /// <summary>Heróis da sala (as cópias), inclusive os tirados das vagas.</summary>
        public IReadOnlyList<Personagem> HeroisDaSala(string sala) =>
            personagens.Where(p => p.Sala == sala && p.Controle == Controle.Jogador).ToList();

        /// <summary>Laboratório de NPCs: os originais, fora de qualquer sala (docs/npc_lab.md).</summary>
        public IReadOnlyList<Personagem> NpcsDoLaboratorio() => NpcsDaSala(null);

        /// <summary>
        /// Cria o NPC na sala, ou no laboratório de NPCs com <paramref name="sala"/> null. Basta um dos dois
        /// nomes: o nome ou o codinome (nome de vilão, se vilão).
        /// </summary>
        public Resultado CriarNpc(string sala, string nome, Maturidade maturidade, Papel papel, string codinome = null)
        {
            var limpo = (nome ?? string.Empty).Trim();
            var limpoCodinome = (codinome ?? string.Empty).Trim();
            if (limpo.Length == 0 && limpoCodinome.Length == 0)
                return Resultado.Falha("Dê um nome ao NPC.");

            var npc = new Personagem
            {
                Id = Guid.NewGuid().ToString("N"),
                Identidade = new Identidade { Nome = limpo, Codinome = limpoCodinome },
                Controle = Controle.Mestre,
                Maturidade = maturidade,
                Sala = sala,
                Pnj = new PerfilPnj(),
                Ordem = Ordenacao.Fim(personagens.Where(p => p.Sala == sala && p.Controle == Controle.Mestre)),
            };
            DefinirPapelEm(npc, papel);
            personagens.Add(npc);
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Nome (o real, ou o único) e codinome / nome de vilão (opcional).</summary>
        public Resultado RenomearNpc(string id, string nome, string codinome)
        {
            if (!AcharNpc(id, out var p, out var erro))
                return erro;
            var n = (nome ?? string.Empty).Trim();
            var c = (codinome ?? string.Empty).Trim();
            if (n.Length == 0 && c.Length == 0)
                return Resultado.Falha("Dê pelo menos um nome ou um codinome.");
            if (n == p.Identidade.Nome && c == p.Identidade.Codinome)
                return Resultado.Sucesso();
            p.Identidade.Nome = n;
            p.Identidade.Codinome = c;
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado DefinirPerfilPnj(string id, string modoDeAgir, string motivacao)
        {
            if (!AcharNpc(id, out var p, out var erro))
                return erro;
            var m = (modoDeAgir ?? string.Empty).Trim();
            var mot = (motivacao ?? string.Empty).Trim();
            if (m == p.Pnj.ModoDeAgir && mot == p.Pnj.Motivacao)
                return Resultado.Sucesso();
            p.Pnj.ModoDeAgir = m;
            p.Pnj.Motivacao = mot;
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado DefinirMaturidade(string id, Maturidade maturidade)
        {
            if (!AcharNpc(id, out var p, out var erro))
                return erro;
            p.Maturidade = maturidade;
            p.Pnj.MaturidadeADefinir = false;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Papel vilão dá a ficha de vilão (promover). Voltar o papel guarda a ficha, não apaga.</summary>
        public Resultado DefinirPapel(string id, Papel papel)
        {
            if (!AcharNpc(id, out var p, out var erro))
                return erro;
            DefinirPapelEm(p, papel);
            Gravar();
            return Resultado.Sucesso();
        }

        static void DefinirPapelEm(Personagem p, Papel papel)
        {
            if (papel == Papel.Heroi)
                papel = Papel.Aliado; // Heroi é das PJs
            p.Papel = papel;
            if (papel == Papel.Vilao)
                p.Vilao ??= new FichaVilao();
        }

        public Resultado DefinirGeracao(string id, string geracao)
        {
            if (!AcharVilao(id, out var p, out var erro))
                return erro;
            if (geracao != null && Geracoes.Buscar(geracao) == null)
                return Resultado.Falha("Geração desconhecida.");
            p.Vilao.Geracao = geracao;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// Escreve o movimento de vilão de número <paramref name="indice"/>; o índice logo depois do
        /// último acrescenta um novo. Texto vazio tira o movimento (é texto do mestre, não histórico).
        /// </summary>
        public Resultado DefinirMovimentoDeVilao(string id, int indice, string texto)
        {
            if (!AcharVilao(id, out var p, out var erro))
                return erro;
            var movimentos = p.Vilao.Movimentos;
            if (indice < 0 || indice > movimentos.Count)
                return Resultado.Falha("Movimento inexistente.");
            var limpo = (texto ?? string.Empty).Trim();
            if (indice == movimentos.Count)
            {
                if (limpo.Length == 0)
                    return Resultado.Sucesso();
                movimentos.Add(limpo);
            }
            else if (limpo.Length == 0)
                movimentos.RemoveAt(indice);
            else if (movimentos[indice] == limpo)
                return Resultado.Sucesso();
            else
                movimentos[indice] = limpo;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Liga/desliga uma das condições que o vilão tem (de 1 a 5).</summary>
        public Resultado AlternarCondicaoDeVilao(string id, string condicao)
        {
            if (!AcharVilao(id, out var p, out var erro))
                return erro;
            if (!Condicoes.Todas.Contains(condicao))
                return Resultado.Falha("Condição desconhecida.");
            var lista = p.Vilao.Condicoes;
            if (lista.Contains(condicao))
                lista.Remove(condicao);
            else
            {
                lista.Add(condicao);
                // Na ordem da ficha, não na ordem do clique.
                lista.Sort((a, b) => Condicoes.Todas.ToList().IndexOf(a).CompareTo(Condicoes.Todas.ToList().IndexOf(b)));
            }
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>O que falta na ficha do NPC. Só aviso: NPC não tem ciclo de criação, nada trava.</summary>
        public IReadOnlyList<string> AvisosDoNpc(Personagem p)
        {
            var avisos = new List<string>();
            if (p.Pnj == null)
                return avisos;
            if (p.Pnj.MaturidadeADefinir)
                avisos.Add("Veio da ficha de um herói: defina se é adulto ou jovem.");
            if (string.IsNullOrWhiteSpace(p.Pnj.Motivacao))
                avisos.Add("Sem motivação: é ela que diz o que o NPC faz.");
            if (p.Papel == Papel.Vilao && p.Vilao != null)
            {
                // Geração é sabor (época e tom do nome), não falta: não vira aviso.
                if (p.Vilao.Movimentos.Count == 0)
                    avisos.Add("Vilão sem movimentos de vilão (o livro sugere três ou mais).");
                if (p.Vilao.Condicoes.Count == 0)
                    avisos.Add("Vilão sem condições: escolha de 1 a 5.");
            }
            return avisos;
        }

        // ---------- copiar NPC: laboratório → sala, sala → sala (reboot), sala → laboratório ----------

        /// <summary>
        /// Copia o NPC para a sala <paramref name="destino"/> (null = laboratório de NPCs). A cópia é
        /// independente e guarda de onde veio (<see cref="Personagem.Origem"/>). Leva o que o NPC é (nome,
        /// foto, aparência, perfil, papel, maturidade, ficha de vilão, notas); deixa o que aconteceu com ele
        /// (Influências, vínculo com a heroína; o log da mesa é da sala de origem). Ver docs/npc_lab.md.
        /// </summary>
        public Personagem CopiarNpc(string id, string destino, out Resultado resultado)
        {
            if (!AcharNpc(id, out var npc, out resultado))
                return null;
            var copia = JsonConvert.DeserializeObject<Personagem>(JsonConvert.SerializeObject(npc));
            Normalizar(copia);
            copia.Id = Guid.NewGuid().ToString("N");
            copia.Origem = npc.Id;
            copia.Sala = destino;
            copia.Ativo = true;
            copia.Favorito = false;
            copia.Ordem = Ordenacao.Fim(personagens.Where(n => n.Sala == destino && n.Controle == Controle.Mestre));
            copia.Pnj.Vinculo = null;
            copia.Influencias = new Dictionary<string, EstadoInfluencia>();
            copia.InfluenciaPorque = new Dictionary<string, string>();
            copia.InfluenciaPorqueNinguem = string.Empty;
            personagens.Add(copia);
            Gravar();
            resultado = Resultado.Sucesso();
            return copia;
        }

        /// <summary>Já há, no destino (null = laboratório), um NPC ativo com o mesmo nome? Só aviso: copiar pode.</summary>
        public bool NomeRepetidoNoDestino(Personagem npc, string destino) =>
            personagens.Any(n => n.Id != npc.Id && n.Ativo && n.Sala == destino && n.Controle == Controle.Mestre &&
                                 string.Equals(n.NomeExibicao, npc.NomeExibicao, StringComparison.CurrentCultureIgnoreCase));

        /// <summary>O NPC de onde esta cópia veio (null se nasceu aqui, ou se a origem não existe mais).</summary>
        public Personagem OrigemDaCopia(Personagem npc) =>
            npc?.Origem == null ? null : Buscar(npc.Origem);

        // ---------- NPCs que nascem da ficha (nêmesis, Legado, perguntas de histórico) ----------

        /// <summary>A "fonte" das perguntas de histórico no vínculo do NPC (o campo é o número da pergunta).</summary>
        public const string FonteHistorico = "historico";

        /// <summary>
        /// Cada lugar da ficha que dá um NPC, com o que o jogador escreveu: os extras com "npcs" (nome no
        /// campo/vaga, descrição no campo de descrição ou na descrição da vaga) e as perguntas de
        /// histórico com "npcs_historico" (nome à parte, a resposta é a descrição).
        /// </summary>
        IEnumerable<(string fonte, string campo, string vaga, Regras.RegrasArquetipos.NpcDoExtra regra, string nome, string descricao, string vinculo, string papel, string maturidade)>
            FontesDeNpc(Personagem p)
        {
            foreach (var extra in ExtrasDoArquetipo(p))
            foreach (var regra in extra.Npcs)
            {
                if (regra.PorEscolha)
                {
                    var opcoes = extra.Campos.FirstOrDefault(c => c.Id == regra.Campo)?.Opcoes;
                    foreach (var o in Escolhas(p, extra.Id, regra.Campo).Where(regra.DaPessoa))
                        yield return (extra.Id, regra.Campo, o, regra,
                            TextoDoExtra(p, extra.Id, regra.Campo, Regras.RegrasArquetipos.CampoExtra.PessoaDaOpcao(o)),
                            TextoDoExtra(p, extra.Id, regra.Campo, o),
                            opcoes?.FirstOrDefault(x => x.Id == o)?.Nome ?? regra.Vinculo,
                            regra.PapelDa(o), regra.MaturidadeDa(o));
                    continue;
                }
                var descricao = regra.CampoDescricao != null ? TextoDoExtra(p, extra.Id, regra.CampoDescricao)
                    : regra.Vaga != null ? TextoDoExtra(p, extra.Id, regra.Campo, Regras.RegrasArquetipos.CampoExtra.DescricaoDaVaga(regra.Vaga))
                    : string.Empty;
                yield return (extra.Id, regra.Campo, regra.Vaga, regra, TextoDoExtra(p, extra.Id, regra.Campo, regra.Vaga), descricao, regra.Vinculo, regra.Papel, regra.Maturidade);
            }
            var arquetipo = RegraDoArquetipo(p);
            if (arquetipo == null)
                yield break;
            foreach (var regra in arquetipo.NpcsHistorico)
            {
                var k = regra.Pergunta ?? -1;
                var respostas = p.Identidade.RespostasHistorico;
                yield return (FonteHistorico, k.ToString(), null, regra, NomeNoHistorico(p, k),
                    k >= 0 && k < respostas.Count ? respostas[k] : string.Empty, regra.Vinculo, regra.Papel, regra.Maturidade);
            }
        }

        /// <summary>
        /// Acerta os NPCs que a ficha cria na sala: nome preenchido cria (ou reativa e renomeia); nome
        /// apagado inativa (nada se exclui). Sem sala (laboratório), nada: o texto fica na ficha e o NPC
        /// nasce quando ela for para uma sala. Não grava: quem chama grava.
        /// </summary>
        void SincronizarNpcsDaFicha(Personagem p)
        {
            if (p == null || p.Sala == null || p.Controle != Controle.Jogador)
                return;
            var fontes = FontesDeNpc(p).ToList();
            // O que deixou de ser fonte (opção desmarcada, troca de arquétipo) inativa o NPC; nada se exclui.
            foreach (var orfao in personagens.Where(n => n.Ativo && n.Pnj?.Vinculo is { } v && v.Personagem == p.Id &&
                                                        !fontes.Any(f => f.fonte == v.Extra && f.campo == v.Campo && f.vaga == v.Vaga)))
                orfao.Ativo = false;
            foreach (var (fonte, campo, vaga, _, texto, _, _, papelDaFonte, maturidade) in fontes)
            {
                var nome = (texto ?? string.Empty).Trim();
                var npc = NpcDaFicha(p, fonte, campo, vaga);
                if (nome.Length == 0)
                {
                    if (npc != null)
                        npc.Ativo = false;
                    continue;
                }
                if (npc == null)
                {
                    npc = new Personagem
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Identidade = new Identidade(),
                        Controle = Controle.Mestre,
                        Maturidade = maturidade == "jovem" ? Maturidade.Jovem : Maturidade.Adulto,
                        Sala = p.Sala,
                        Pnj = new PerfilPnj
                        {
                            Vinculo = new VinculoNpc { Personagem = p.Id, Extra = fonte, Campo = campo, Vaga = vaga },
                            // "mestre": nasce adulto, mas fica o aviso até o mestre decidir.
                            MaturidadeADefinir = maturidade == "mestre",
                        },
                        Ordem = Ordenacao.Fim(personagens.Where(n => n.Sala == p.Sala && n.Controle == Controle.Mestre)),
                    };
                    DefinirPapelEm(npc, PapelDoExtra(papelDaFonte));
                    personagens.Add(npc);
                }
                npc.Ativo = true;
                // O nome que o jogador dá é o nome pelo qual a mesa o conhece (nome de vilão, se vilão).
                if (npc.Papel == Papel.Vilao)
                    npc.Identidade.Codinome = nome;
                else
                    npc.Identidade.Nome = nome;
            }
        }

        // ---------- nome do NPC numa pergunta de histórico ----------

        /// <summary>A pergunta de histórico dá um NPC (ex.: a Nova: quem ela feriu, quem a ajuda)?</summary>
        public bool PerguntaDaNpc(Personagem p, int pergunta) =>
            RegraDoArquetipo(p)?.NpcsHistorico.Any(r => r.Pergunta == pergunta) ?? false;

        public string NomeNoHistorico(Personagem p, int pergunta) =>
            p.Identidade.NomesHistorico.TryGetValue(pergunta.ToString(), out var n) ? n : string.Empty;

        /// <summary>O nome do NPC da pergunta (a resposta é a descrição dele). Grava e acerta o NPC da sala.</summary>
        public Resultado DefinirNomeNoHistorico(string id, int pergunta, string nome)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (!PerguntaDaNpc(p, pergunta))
                return Resultado.Falha("Essa pergunta não dá NPC.");
            var limpo = (nome ?? string.Empty).Trim();
            if (NomeNoHistorico(p, pergunta) == limpo)
                return Resultado.Sucesso();
            if (limpo.Length == 0)
                p.Identidade.NomesHistorico.Remove(pergunta.ToString());
            else
                p.Identidade.NomesHistorico[pergunta.ToString()] = limpo;
            SincronizarNpcsDaFicha(p);
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// O nome e a descrição de um NPC da ficha continuam editáveis na sala enquanto ela está em
        /// aprovação: é ali que o NPC nasce, junto com o resto do que se acerta com a equipe. O resto do
        /// extra (ex.: os Rótulos da mentora) é da criação. Em carreira, trava.
        /// </summary>
        public bool ExtraEditavelNaSala(Personagem p, string extra, string campo) =>
            p.Sala != null && p.Status == StatusCriacao.EmAprovacao &&
            ExtrasDoArquetipo(p).Any(e => e.Id == extra &&
                                          e.Npcs.Any(r => !r.PorEscolha && (r.Campo == campo || r.CampoDescricao == campo)));

        /// <summary>
        /// A caixa de pessoa das opções escolhidas (ex.: obrigações do Janus): na criação ou, na sala,
        /// em aprovação. As escolhas em si continuam da criação.
        /// </summary>
        public bool PessoaEditavel(Personagem p) =>
            p.EmCriacao || (p.Sala != null && p.Status == StatusCriacao.EmAprovacao);

        /// <summary>A opção deste campo (lista), escolhida, ganha caixa de pessoa (que vira NPC)?</summary>
        public bool PessoaPorEscolha(Personagem p, string extra, string campo, string opcao) =>
            RegraDePessoa(p, extra, campo, opcao) != null;

        /// <summary>O que a caixa de pessoa da opção diz quando vazia (ex.: "vazio: qualquer herói adulto").</summary>
        public string DicaDaPessoa(Personagem p, string extra, string campo, string opcao) =>
            RegraDePessoa(p, extra, campo, opcao)?.DicaDa(opcao);

        Regras.RegrasArquetipos.NpcDoExtra RegraDePessoa(Personagem p, string extra, string campo, string opcao) =>
            ExtrasDoArquetipo(p).FirstOrDefault(e => e.Id == extra)?.Npcs
                .FirstOrDefault(r => r.PorEscolha && r.Campo == campo && r.DaPessoa(opcao));

        /// <summary>Acerta os NPCs de todas as heroínas nas salas (ex.: fichas que foram para a sala antes do nêmesis virar NPC).</summary>
        void SincronizarTodosOsNpcsDaFicha()
        {
            var mudou = AcertarVinculosSemCampo();
            var antes = personagens.Count;
            var nomes = personagens.Where(n => n.Pnj?.Vinculo != null).Select(n => (n.Id, n.NomeExibicao, n.Ativo)).ToList();
            foreach (var p in personagens.Where(p => p.Sala != null && p.Controle == Controle.Jogador).ToList())
                SincronizarNpcsDaFicha(p);
            var depois = personagens.Where(n => n.Pnj?.Vinculo != null).Select(n => (n.Id, n.NomeExibicao, n.Ativo)).ToList();
            if (mudou || personagens.Count != antes || !nomes.SequenceEqual(depois))
                Gravar();
        }

        /// <summary>
        /// O primeiro nêmesis nasceu com o vínculo sem campo (antes de um extra poder dar vários NPCs) e,
        /// sem achar esse NPC, a sincronização criou outro. Aqui o antigo ganha o campo e volta a ser o
        /// NPC do extra; o repetido (o mais novo) fica inativo, apontando para a mesma ficha.
        /// </summary>
        bool AcertarVinculosSemCampo()
        {
            var mudou = false;
            foreach (var npc in personagens.Where(n => n.Pnj?.Vinculo is { Campo: null }))
            {
                var dono = Buscar(npc.Pnj.Vinculo.Personagem);
                var regras = dono == null ? null : ExtrasDoArquetipo(dono).FirstOrDefault(e => e.Id == npc.Pnj.Vinculo.Extra)?.Npcs;
                if (regras == null || regras.Count != 1)
                    continue;
                (npc.Pnj.Vinculo.Campo, npc.Pnj.Vinculo.Vaga) = (regras[0].Campo, regras[0].Vaga);
                mudou = true;
            }
            var repetidos = personagens.Where(n => n.Pnj?.Vinculo != null)
                .GroupBy(n => (n.Pnj.Vinculo.Personagem, n.Pnj.Vinculo.Extra, n.Pnj.Vinculo.Campo, n.Pnj.Vinculo.Vaga))
                .SelectMany(g => g.Skip(1))
                .Where(n => n.Ativo);
            foreach (var npc in repetidos.ToList())
            {
                npc.Ativo = false;
                mudou = true;
            }
            return mudou;
        }

        static Papel PapelDoExtra(string papel) =>
            papel == "vilao" ? Papel.Vilao : papel == "aliado" ? Papel.Aliado : Papel.Neutro;

        /// <summary>
        /// O NPC que esta parte da ficha criou (ativo ou não). <paramref name="fonte"/> é o id do extra ou
        /// <see cref="FonteHistorico"/>. Sem campo, o primeiro da fonte (extra com um NPC só, como o nêmesis).
        /// </summary>
        public Personagem NpcDaFicha(Personagem p, string fonte, string campo = null, string vaga = null) =>
            personagens.FirstOrDefault(n => n.Pnj?.Vinculo is { } v && v.Personagem == p.Id && v.Extra == fonte &&
                                            (campo == null || (v.Campo == campo && v.Vaga == vaga)));

        /// <summary>
        /// De quem é o NPC, como a sala chama o laço e o que o jogador escreveu sobre ele.
        /// Null se o mestre o criou.
        /// </summary>
        public (Personagem dono, string vinculo, string descricao)? OrigemDoNpc(Personagem npc)
        {
            var v = npc?.Pnj?.Vinculo;
            var dono = v == null ? null : Buscar(v.Personagem);
            if (dono == null)
                return null;
            foreach (var f in FontesDeNpc(dono))
                if (f.fonte == v.Extra && f.campo == v.Campo && f.vaga == v.Vaga)
                    return (dono, f.vinculo ?? "NPC", f.descricao);
            return (dono, "NPC", string.Empty);
        }

        bool AcharNpc(string id, out Personagem p, out Resultado erro)
        {
            p = Buscar(id);
            if (p == null || p.Controle != Controle.Mestre)
            {
                erro = Resultado.Falha("NPC não encontrado.");
                return false;
            }
            p.Pnj ??= new PerfilPnj();
            erro = Resultado.Sucesso();
            return true;
        }

        bool AcharVilao(string id, out Personagem p, out Resultado erro)
        {
            if (!AcharNpc(id, out p, out erro))
                return false;
            if (p.Papel != Papel.Vilao || p.Vilao == null)
            {
                erro = Resultado.Falha("Só vilão tem ficha de vilão.");
                return false;
            }
            return true;
        }
    }
}
