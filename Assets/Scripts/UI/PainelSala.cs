using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TorreDeControle.Core;
using TorreDeControle.Core.Jogadores;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Salas;
using UnityEngine;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Dentro da sala: os slots. Cada slot = um jogador + uma personagem da sala. Adicionar personagem
    /// copia a do laboratório (a original fica intacta); tirar inativa a cópia (não exclui).
    /// </summary>
    public sealed class PainelSala : MonoBehaviour
    {
        const float LarguraSlot = 110;
        const float LarguraJogador = 320;
        const float LarguraBotao = 170;
        const float LarguraPerfil = 200;
        const string Nenhum = "";

        Sala sala;
        CadastroSalas salas;
        CadastroPersonagens personagens;
        CadastroJogadores jogadores;
        Action aoFechar;
        PainelFicha painelFicha;
        PainelNpc painelNpc;

        string npcNome = string.Empty;
        Maturidade npcMaturidade = Maturidade.Adulto;
        Papel npcPapel = Papel.Neutro;
        TMP_InputField campoNpc;

        /// <summary>Personagem do laboratório escolhida na lista de cada slot vazio (antes de "Adicionar").</summary>
        readonly Dictionary<int, string> escolhida = new Dictionary<int, string>();

        enum Aba { Vagas, Npcs }
        Aba abaAtual = Aba.Vagas;

        TextMeshProUGUI titulo;
        TextMeshProUGUI contagem;
        TextMeshProUGUI mensagem;
        RectTransform abas;
        RectTransform cabecalhoVagas;
        RectTransform lista;

        public static PainelSala Criar(Transform pai)
        {
            var veu = Estilo.Rect("PainelSala", pai);
            Estilo.Esticar(veu);
            veu.gameObject.AddComponent<Image>().color = Estilo.Veu;
            veu.gameObject.AddComponent<NavegacaoTab>();
            var painel = veu.gameObject.AddComponent<PainelSala>();
            painel.Montar(veu);
            return painel;
        }

        public void Abrir(Sala salaAberta, CadastroSalas cadastroSalas, CadastroPersonagens cadastroPersonagens,
            CadastroJogadores cadastroJogadores, Action aoFecharSala)
        {
            sala = salaAberta;
            salas = cadastroSalas;
            personagens = cadastroPersonagens;
            jogadores = cadastroJogadores;
            aoFechar = aoFecharSala;
            abaAtual = Aba.Vagas;
            escolhida.Clear();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Mostrar(null);
            Redesenhar();
        }

        /// <summary>O painel de controle do mestre (rodadas, potencial, condições, Rótulos, log). Em construção.</summary>
        void AreaDoMestre() =>
            mensagem.text = "Área do mestre: em construção (rodadas, potencial, condições, Rótulos e o log da campanha).";

        void Fechar()
        {
            gameObject.SetActive(false);
            aoFechar?.Invoke();
        }

        void Mostrar(Resultado? resultado)
        {
            var erro = resultado.HasValue && !resultado.Value.Ok ? resultado.Value.Erro : null;
            mensagem.text = erro ?? string.Empty;
        }

        // ---------- opções ----------

        /// <summary>Jogadores ativos que não estão em outro slot; o atual entra mesmo se inativo.</summary>
        IReadOnlyList<(string id, string nome)> OpcoesJogador(Slot slot)
        {
            var opcoes = new List<(string id, string nome)> { (Nenhum, "sem jogador") };
            foreach (var j in jogadores.Listar())
            {
                var atual = j.Id == slot.Jogador;
                if (atual || (j.Ativo && sala.Slots.All(s => s.Jogador != j.Id)))
                    opcoes.Add((j.Id, j.Ativo ? j.Nome : j.Nome + " (inativo)"));
            }
            return opcoes;
        }

        /// <summary>Fichas do laboratório que podem entrar: criadas, ativas e ainda fora desta sala.</summary>
        List<Personagem> Candidatas()
        {
            var naSala = new HashSet<string>(sala.Slots
                .Where(s => s.Personagem != null)
                .Select(s => personagens.Buscar(s.Personagem)?.Origem)
                .Where(o => o != null));
            return personagens.Listar()
                .Where(p => p.Ativo && p.Controle == Controle.Jogador && p.Status == StatusCriacao.EmAprovacao && !naSala.Contains(p.Id))
                .ToList();
        }

        static string Descrever(Personagem p) =>
            $"{p.NomeExibicao} ({Arquetipos.Buscar(p.Foto?.Arquetipo ?? p.Identidade.Arquetipo)?.Nome ?? "?"})";

        // ---------- ações ----------

        void Por(int indice)
        {
            if (!escolhida.TryGetValue(indice, out var origem) || string.IsNullOrEmpty(origem))
            {
                Mostrar(Resultado.Falha("Escolha uma personagem do laboratório."));
                return;
            }
            var p = personagens.Buscar(origem);
            Estilo.Perguntar(transform,
                $"Adicionar {p?.NomeExibicao} à vaga {indice + 1}? A sala recebe uma cópia; a do laboratório fica intacta.\n" +
                "Só a criação: a cópia nasce da foto da criação. Criação + jogo: traz também o que o laboratório marcou " +
                "(potencial, avanços, trilhas).",
                new (string, UnityEngine.Events.UnityAction)[]
                {
                    ("Criação + jogo", () => Aplicar(salas.PorPersonagem(sala.Id, indice, origem, comJogo: true))),
                    ("Só a criação", () => Aplicar(salas.PorPersonagem(sala.Id, indice, origem, comJogo: false))),
                });
        }

        void Tirar(int indice, Personagem p)
        {
            Estilo.Confirmar(transform,
                $"Tirar {p.NomeExibicao} da vaga {indice + 1}? A cópia não é apagada: vai para as vagas liberadas (arquivo da sala). " +
                "O jogador continua na vaga.",
                "Tirar da vaga",
                () => Aplicar(salas.TirarPersonagem(sala.Id, indice)));
        }

        void Aplicar(Resultado resultado)
        {
            escolhida.Clear();
            Mostrar(resultado);
            Redesenhar();
        }

        void AbrirFicha(Personagem p)
        {
            if (painelFicha == null)
                painelFicha = PainelFicha.Criar(transform.parent);
            painelFicha.Abrir(p, personagens, jogadores, Redesenhar, sala.Nome, abertaPelaSala: true);
        }

        // ---------- montagem ----------

        void Montar(RectTransform veu)
        {
            var cartao = Estilo.Rect("Cartao", veu);
            cartao.anchorMin = new Vector2(0.5f, 0.5f);
            cartao.anchorMax = new Vector2(0.5f, 0.5f);
            cartao.sizeDelta = new Vector2(1680, 900);
            Estilo.Moldura(cartao.gameObject, Estilo.Cartao, Estilo.Borda);

            var coluna = cartao.gameObject.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(56, 56, 44, 44);
            coluna.spacing = 12;
            coluna.childControlWidth = true;
            coluna.childControlHeight = true;
            coluna.childForceExpandHeight = false;

            var cabecalho = Estilo.Fila(cartao, 84);
            var titulos = Estilo.Rect("Titulos", cabecalho);
            titulos.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var pilha = titulos.gameObject.AddComponent<VerticalLayoutGroup>();
            pilha.childControlWidth = true;
            pilha.childControlHeight = true;
            pilha.childForceExpandHeight = false;
            pilha.childAlignment = TextAnchor.MiddleLeft;
            titulo = Estilo.Rotulo(titulos, string.Empty, 44, Estilo.Dourado, FontStyles.Bold);
            titulo.characterSpacing = 6;
            contagem = Estilo.Rotulo(titulos, string.Empty, 24, Estilo.TextoSuave);
            Estilo.BotaoPrincipal(cabecalho, "Área do mestre", 300, AreaDoMestre);
            Estilo.BotaoContorno(cabecalho, "Voltar", 190, Estilo.TextoSuave, Fechar);

            var divisoria = Estilo.Rect("Divisoria", cartao);
            divisoria.gameObject.AddComponent<Image>().color =
                new Color(Estilo.Dourado.r, Estilo.Dourado.g, Estilo.Dourado.b, 0.35f);
            var ld = divisoria.gameObject.AddComponent<LayoutElement>();
            ld.minHeight = 2;
            ld.preferredHeight = 2;

            abas = Estilo.Fila(cartao, 52, 10);

            cabecalhoVagas = Estilo.Fila(cartao, 30);
            cabecalhoVagas.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 0, 0);
            Coluna(cabecalhoVagas, "VAGA", LarguraSlot);
            Coluna(cabecalhoVagas, "JOGADOR", LarguraJogador);
            Coluna(cabecalhoVagas, "PERSONAGEM", -1);
            Coluna(cabecalhoVagas, string.Empty, LarguraBotao);
            Coluna(cabecalhoVagas, string.Empty, LarguraBotao);

            mensagem = Estilo.Rotulo(cartao, string.Empty, 22, Estilo.Erro);
            mensagem.gameObject.AddComponent<LayoutElement>().minHeight = 28;

            lista = Estilo.ListaRolavel(cartao);
        }

        static void Coluna(Transform pai, string texto, float largura)
        {
            var rotulo = Estilo.Rotulo(pai, texto, 18, Estilo.TextoSuave, FontStyles.Bold);
            rotulo.characterSpacing = 4;
            if (largura < 0)
                Estilo.ColunaElastica(rotulo);
            else
                rotulo.gameObject.AddComponent<LayoutElement>().preferredWidth = largura;
        }

        void Redesenhar()
        {
            for (var i = lista.childCount - 1; i >= 0; i--)
                Destroy(lista.GetChild(i).gameObject);

            titulo.text = sala.Nome.ToUpperInvariant();
            var comJogador = sala.Slots.Count(s => s.Jogador != null);
            var comPersonagem = sala.Slots.Count(s => s.Personagem != null);
            contagem.text = $"{sala.Slots.Count} vagas · {comJogador} com jogador · {comPersonagem} com personagem" +
                            (sala.Ativo ? string.Empty : " · sala inativa");

            MontarAbas();
            cabecalhoVagas.gameObject.SetActive(abaAtual == Aba.Vagas);
            if (abaAtual == Aba.Npcs)
            {
                SecaoNpcs();
                return;
            }

            var candidatas = Candidatas();
            for (var k = 0; k < sala.Slots.Count; k++)
                Linha(k, sala.Slots[k], candidatas);
            Arquivo();
        }

        void MontarAbas()
        {
            for (var i = abas.childCount - 1; i >= 0; i--)
                Destroy(abas.GetChild(i).gameObject);
            var npcs = personagens.NpcsDaSala(sala.Id).Count(p => p.Ativo);
            foreach (var (aba, nome) in new[] { (Aba.Vagas, "Vagas"), (Aba.Npcs, $"NPCs · {npcs}") })
            {
                var alvo = aba;
                if (aba == abaAtual)
                    Estilo.BotaoPrincipal(abas, nome, 260, () => { });
                else
                    Estilo.BotaoContorno(abas, nome, 260, Estilo.Dourado, () =>
                    {
                        abaAtual = alvo;
                        Mostrar(null);
                        Redesenhar();
                    });
            }
            Estilo.Rect("Vao", abas).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        /// <summary>
        /// Vagas liberadas: as cópias tiradas das vagas (ficha errada, jogador que saiu). Não se apagam;
        /// ficam aqui, no arquivo da sala, com a ficha para consulta.
        /// </summary>
        void Arquivo()
        {
            var tiradas = personagens.HeroisDaSala(sala.Id).Where(p => !p.Ativo).ToList();
            if (tiradas.Count == 0)
                return;

            var vao = Estilo.Rect("Vao", lista);
            vao.gameObject.AddComponent<LayoutElement>().minHeight = 14;
            var bloco = Estilo.Bloco(lista, "VAGAS LIBERADAS · ARQUIVO DA SALA", tiradas.Count);
            foreach (var p in tiradas)
            {
                var linha = Estilo.Fila(bloco, 60);
                linha.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 8, 8);
                Estilo.Moldura(linha.gameObject, Estilo.Linha, Estilo.BordaApagada);

                Estilo.Rect("Vao", linha).gameObject.AddComponent<LayoutElement>().preferredWidth = LarguraSlot;
                var jogador = p.Identidade.Jogador == null ? "sem jogador" : jogadores.Buscar(p.Identidade.Jogador)?.Nome ?? "?";
                var j = Estilo.Rotulo(linha, jogador, 22, Estilo.TextoApagado, FontStyles.Italic);
                j.gameObject.AddComponent<LayoutElement>().preferredWidth = LarguraJogador;
                var nome = Estilo.Rotulo(linha, Descrever(p), 24, Estilo.TextoApagado);
                Estilo.ColunaElastica(nome);
                Estilo.Rect("Vao", linha).gameObject.AddComponent<LayoutElement>().preferredWidth = LarguraBotao;
                var copia = p;
                Estilo.BotaoContorno(linha, "Ficha", LarguraBotao, Estilo.TextoSuave, () => AbrirFicha(copia));
            }
        }

        // ---------- NPCs da sala ----------

        static IReadOnlyList<(string id, string nome)> OpcoesMaturidade() =>
            new List<(string, string)> { (Maturidade.Adulto.ToString(), "Adulto"), (Maturidade.Jovem.ToString(), "Jovem") };

        static IReadOnlyList<(string id, string nome)> OpcoesPapel() =>
            new List<(string, string)>
            {
                (Papel.Neutro.ToString(), "Neutro"), (Papel.Heroi.ToString(), "Herói"), (Papel.Vilao.ToString(), "Vilão"),
            };

        void SecaoNpcs()
        {
            var titulos = Estilo.Fila(lista, 30);
            titulos.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 0, 0);
            Estilo.VaoDaAlca(titulos);
            Coluna(titulos, "NPC", -1);
            Coluna(titulos, "MATURIDADE", LarguraPerfil);
            Coluna(titulos, "PAPEL", LarguraPerfil);
            Coluna(titulos, string.Empty, LarguraBotao);
            Coluna(titulos, string.Empty, LarguraBotao);

            var novo = Estilo.Fila(lista, 62);
            novo.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 0, 0);
            Estilo.VaoDaAlca(novo);
            campoNpc = Estilo.CampoTexto(novo, "Nome do novo NPC e Enter");
            campoNpc.text = npcNome;
            Estilo.ColunaElastica(campoNpc);
            campoNpc.onValueChanged.AddListener(t => npcNome = t);
            campoNpc.onSubmit.AddListener(_ => CriarNpc());
            Estilo.ListaSuspensa(novo, LarguraPerfil, OpcoesMaturidade(), npcMaturidade.ToString(),
                id => npcMaturidade = (Maturidade)Enum.Parse(typeof(Maturidade), id));
            Estilo.ListaSuspensa(novo, LarguraPerfil, OpcoesPapel(), npcPapel.ToString(),
                id => npcPapel = (Papel)Enum.Parse(typeof(Papel), id));
            Estilo.BotaoPrincipal(novo, "Criar NPC", LarguraBotao * 2 + 14, CriarNpc);

            var npcs = personagens.NpcsDaSala(sala.Id);
            foreach (var (ativo, titulo) in new[] { (true, "NPCS"), (false, "NPCS INATIVOS") })
            {
                var doBloco = npcs.Where(p => p.Ativo == ativo).ToList();
                if (doBloco.Count == 0)
                    continue;
                var bloco = Estilo.Bloco(lista, titulo, doBloco.Count);
                foreach (var npc in doBloco)
                    LinhaNpc(npc, bloco);
            }
        }

        void CriarNpc()
        {
            var resultado = personagens.CriarNpc(sala.Id, npcNome, npcMaturidade, npcPapel);
            Mostrar(resultado);
            if (!resultado.Ok)
                return;
            npcNome = string.Empty;
            Redesenhar();
            campoNpc.ActivateInputField();
        }

        void LinhaNpc(Personagem npc, RectTransform bloco)
        {
            var linha = Estilo.Fila(bloco, 64);
            linha.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 8, 8);
            Estilo.Moldura(linha.gameObject, Estilo.Linha, npc.Ativo ? Estilo.Borda : Estilo.BordaApagada);
            Estilo.Alca(linha, indice => Mostrar(personagens.Mover(npc.Id, indice)));

            var motivacao = npc.Pnj?.Motivacao;
            var texto = $"<b>{npc.NomeExibicao}</b>" +
                        (string.IsNullOrWhiteSpace(motivacao) ? string.Empty : $"  <i><color=#9AA3B5>quer {motivacao}</color></i>");
            var nome = Estilo.Rotulo(linha, texto, 24, npc.Ativo ? Estilo.Texto : Estilo.TextoApagado);
            nome.textWrappingMode = TextWrappingModes.NoWrap;
            nome.overflowMode = TextOverflowModes.Ellipsis;
            Estilo.ColunaElastica(nome);

            Estilo.ListaSuspensa(linha, LarguraPerfil, OpcoesMaturidade(), npc.Maturidade.ToString(), id =>
            {
                Mostrar(personagens.DefinirMaturidade(npc.Id, (Maturidade)Enum.Parse(typeof(Maturidade), id)));
                Redesenhar();
            });
            Estilo.ListaSuspensa(linha, LarguraPerfil, OpcoesPapel(), npc.Papel.ToString(), id =>
            {
                Mostrar(personagens.DefinirPapel(npc.Id, (Papel)Enum.Parse(typeof(Papel), id)));
                Redesenhar();
            });

            if (npc.Ativo)
                Estilo.BotaoContorno(linha, "Ativo", LarguraBotao, Estilo.Dourado, () => AlternarNpc(npc));
            else
                Estilo.BotaoContorno(linha, "Inativo", LarguraBotao, Estilo.TextoApagado, () => AlternarNpc(npc));
            Estilo.BotaoPrincipal(linha, "Ficha", LarguraBotao, () => AbrirNpc(npc));
        }

        void AlternarNpc(Personagem npc)
        {
            Mostrar(personagens.DefinirAtivo(npc.Id, !npc.Ativo));
            Redesenhar();
        }

        void AbrirNpc(Personagem npc)
        {
            if (painelNpc == null)
                painelNpc = PainelNpc.Criar(transform.parent);
            painelNpc.Abrir(npc, personagens, Redesenhar);
        }

        void Linha(int indice, Slot slot, List<Personagem> candidatas)
        {
            var linha = Estilo.Fila(lista, 64);
            linha.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 8, 8);
            var ocupado = slot.Jogador != null || slot.Personagem != null;
            Estilo.Moldura(linha.gameObject, Estilo.Linha, ocupado ? Estilo.Borda : Estilo.BordaApagada);

            var numero = Estilo.Rotulo(linha, $"{indice + 1}", 26, Estilo.Dourado, FontStyles.Bold);
            numero.alignment = TextAlignmentOptions.Center;
            numero.gameObject.AddComponent<LayoutElement>().preferredWidth = LarguraSlot;

            Estilo.ListaSuspensa(linha, LarguraJogador, OpcoesJogador(slot), slot.Jogador ?? Nenhum, id =>
            {
                Mostrar(salas.DefinirJogador(sala.Id, indice, id));
                Redesenhar();
            });

            var p = slot.Personagem != null ? personagens.Buscar(slot.Personagem) : null;
            if (p != null)
            {
                var situacao = p.Status switch
                {
                    StatusCriacao.EmAprovacao => "em aprovação",
                    StatusCriacao.EmRevisao => "em revisão",
                    _ => null,
                };
                var texto = situacao == null
                    ? Descrever(p)
                    : $"{Descrever(p)}  <size=80%><color=#{ColorUtility.ToHtmlStringRGB(Estilo.Dourado)}>· {situacao}</color></size>";
                var nome = Estilo.Rotulo(linha, texto, 26, Estilo.Texto, FontStyles.Bold);
                Estilo.ColunaElastica(nome);
                Estilo.BotaoContorno(linha, "Tirar", LarguraBotao, Estilo.TextoSuave, () => Tirar(indice, p));
                Estilo.BotaoPrincipal(linha, "Ficha", LarguraBotao, () => AbrirFicha(p));
                return;
            }

            // Vaga com jogador: só as fichas dele. Sem jogador: todas.
            if (slot.Jogador != null)
                candidatas = candidatas.Where(c => c.Identidade.Jogador == slot.Jogador).ToList();
            if (candidatas.Count == 0)
            {
                var semFicha = slot.Jogador == null
                    ? "nenhuma ficha criada disponível no laboratório"
                    : $"{jogadores.Buscar(slot.Jogador)?.Nome ?? "o jogador"} não tem ficha criada disponível no laboratório";
                var vazio = Estilo.Rotulo(linha, semFicha, 22,
                    Estilo.TextoApagado, FontStyles.Italic);
                Estilo.ColunaElastica(vazio);
                Estilo.Rect("Vao", linha).gameObject.AddComponent<LayoutElement>().preferredWidth = LarguraBotao * 2 + 14;
                return;
            }

            var opcoes = new List<(string id, string nome)> { (Nenhum, "escolher do laboratório...") };
            opcoes.AddRange(candidatas.Select(c => (c.Id, Descrever(c))));
            escolhida.TryGetValue(indice, out var atual);
            var seletor = Estilo.ListaSuspensa(linha, 0, opcoes, atual ?? Nenhum, id => escolhida[indice] = id);
            Estilo.ColunaElastica(seletor);
            Estilo.BotaoPrincipal(linha, "Adicionar personagem", LarguraBotao * 2 + 14, () => Por(indice));
        }
    }
}
