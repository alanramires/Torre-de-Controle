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
        PainelMestre painelMestre;

        string npcNome = string.Empty;
        Maturidade npcMaturidade = Maturidade.Adulto;
        Papel npcPapel = Papel.Neutro;
        TMP_InputField campoNpc;

        string cenarioNome = string.Empty;
        string cenarioTipo = ElementoDeCenario.Local;
        TMP_InputField campoCenario;

        /// <summary>Personagem do laboratório escolhida na lista de cada slot vazio (antes de "Adicionar").</summary>
        readonly Dictionary<int, string> escolhida = new Dictionary<int, string>();

        enum Aba { Vagas, Npcs, Cenario, Equipe, Influencia, Relacionamentos }
        Aba abaAtual = Aba.Vagas;

        TextMeshProUGUI titulo;
        TextMeshProUGUI contagem;
        TextMeshProUGUI mensagem;
        RectTransform abas;
        RectTransform cabecalhoVagas;
        RectTransform lista;
        GameObject rolagemDaLista;
        RectTransform areaEquipe;

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
            CadastroJogadores cadastroJogadores, Action aoFecharSala, bool abertaPelaMesa = false)
        {
            sala = salaAberta;
            salas = cadastroSalas;
            personagens = cadastroPersonagens;
            jogadores = cadastroJogadores;
            aoFechar = aoFecharSala;
            pelaMesa = abertaPelaMesa;
            abaAtual = Aba.Vagas;
            escolhida.Clear();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Mostrar(null);
            Redesenhar();
        }

        /// <summary>O painel de controle do mestre (rodadas, potencial, condições, Rótulos, log). Em construção.</summary>
        void AreaDoMestre()
        {
            if (painelMestre == null)
                painelMestre = PainelMestre.Criar(transform.parent);
            // A sala sai de cena enquanto a área do mestre está aberta (senão aparece por trás).
            gameObject.SetActive(false);
            painelMestre.Abrir(sala, salas, personagens, () =>
            {
                gameObject.SetActive(true);
                Redesenhar();
            });
        }

        bool pelaMesa;
        PainelMesa painelMesa;

        /// <summary>
        /// Da sala para a mesa: aberta pela mesa, é só voltar; senão abre a mesa desta campanha por cima, e o
        /// Voltar da mesa traz de volta à sala.
        /// </summary>
        void IrParaMesa()
        {
            if (pelaMesa)
            {
                Fechar();
                return;
            }
            if (PainelMesa.Armazenamento == null)
            {
                mensagem.text = "Mesa indisponível (armazenamento não ligado).";
                return;
            }
            if (painelMesa == null)
                painelMesa = PainelMesa.Criar(transform.parent);
            gameObject.SetActive(false);
            painelMesa.Abrir(sala, salas, personagens, jogadores, PainelMesa.Armazenamento, () =>
            {
                gameObject.SetActive(true);
                transform.SetAsLastSibling();
                Redesenhar();
            });
        }

        void Fechar()
        {
            gameObject.SetActive(false);
            aoFechar?.Invoke();
        }

        void Mostrar(Resultado? resultado, string sucesso = null)
        {
            var erro = resultado.HasValue && !resultado.Value.Ok ? resultado.Value.Erro : null;
            mensagem.color = erro != null ? Estilo.Erro : Estilo.TextoSuave;
            mensagem.text = erro ?? sucesso ?? string.Empty;
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

        /// <summary>Abre a ficha de uma instância desta sala, como se clicada na vaga (o atalho do laboratório usa).</summary>
        public void MostrarFicha(Personagem p) => AbrirFicha(p);

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
            Estilo.BotaoContorno(cabecalho, "Mesa", 160, Estilo.Dourado, IrParaMesa)
                .gameObject.AddComponent<Dica>().Configurar("Abrir a mesa de jogo desta campanha.");
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
            rolagemDaLista = lista.parent.parent.gameObject;

            areaEquipe = Estilo.Rect("AreaEquipe", cartao);
            areaEquipe.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var h = areaEquipe.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            areaEquipe.gameObject.SetActive(false);
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
            for (var i = areaEquipe.childCount - 1; i >= 0; i--)
                Destroy(areaEquipe.GetChild(i).gameObject);

            titulo.text = sala.Nome.ToUpperInvariant();
            var comJogador = sala.Slots.Count(s => s.Jogador != null);
            var comPersonagem = sala.Slots.Count(s => s.Personagem != null);
            contagem.text = $"{sala.Slots.Count} vagas · {comJogador} com jogador · {comPersonagem} com personagem" +
                            (sala.Ativo ? string.Empty : " · sala inativa");

            var daAprovacao = abaAtual == Aba.Equipe || abaAtual == Aba.Influencia || abaAtual == Aba.Relacionamentos;
            if (daAprovacao && NaEquipe().Count == 0)
            {
                abaAtual = Aba.Vagas; // essas abas só existem com alguém em aprovação ou em carreira
                daAprovacao = false;
            }
            MontarAbas();
            cabecalhoVagas.gameObject.SetActive(abaAtual == Aba.Vagas);
            // Formação e Influência têm área própria (fixa); as outras abas usam a lista rolável.
            rolagemDaLista.SetActive(!daAprovacao);
            areaEquipe.gameObject.SetActive(daAprovacao);
            if (abaAtual == Aba.Influencia)
            {
                SecaoInfluencia();
                return;
            }
            if (abaAtual == Aba.Relacionamentos)
            {
                SecaoRelacionamentos();
                return;
            }
            if (abaAtual == Aba.Npcs)
            {
                SecaoNpcs();
                return;
            }
            if (abaAtual == Aba.Cenario)
            {
                SecaoCenario();
                return;
            }
            if (abaAtual == Aba.Equipe)
            {
                SecaoEquipe();
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
            var lugares = personagens.CenarioDaSala(sala.Id).Count(e => e.Ativo);
            var lista = new List<(Aba, string)>
            {
                (Aba.Vagas, "Vagas"), (Aba.Npcs, $"NPCs · {npcs}"), (Aba.Cenario, $"Cenário · {lugares}"),
            };
            // Formação da equipe: aparece com ao menos uma personagem em aprovação ou em carreira nas vagas.
            var emAprovacao = NaEquipe().Count;
            if (emAprovacao > 0)
            {
                lista.Add((Aba.Equipe, $"Formação da equipe · {emAprovacao}"));
                lista.Add((Aba.Influencia, "Influência"));
                lista.Add((Aba.Relacionamentos, "Relacionamentos"));
            }
            foreach (var (aba, nome) in lista)
            {
                var alvo = aba;
                var largura = aba == Aba.Equipe ? 400 : 260;
                var botao = aba == abaAtual
                    ? Estilo.BotaoPrincipal(abas, nome, largura, () => { })
                    : Estilo.BotaoContorno(abas, nome, largura, Estilo.Dourado, () =>
                    {
                        abaAtual = alvo;
                        Mostrar(null);
                        Redesenhar();
                    });
                // Nome comprido ("Relacionamentos"): numa linha só, a fonte encolhe até caber no botão.
                var rotulo = botao.GetComponentInChildren<TextMeshProUGUI>();
                rotulo.textWrappingMode = TextWrappingModes.NoWrap;
                rotulo.enableAutoSizing = true;
                rotulo.fontSizeMin = 14;
                rotulo.fontSizeMax = 24;
                rotulo.rectTransform.offsetMin = new Vector2(10, 0);
                rotulo.rectTransform.offsetMax = new Vector2(-10, 0);
            }
            Estilo.Rect("Vao", abas).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        // ---------- formação da equipe (segunda etapa, na campanha) ----------

        /// <summary>As personagens das vagas em aprovação, na ordem das vagas.</summary>
        /// <summary>
        /// As personagens das vagas em aprovação ou em carreira, na ordem das vagas: as abas da equipe
        /// (formação, Influência, relacionamentos) continuam para leitura depois de aprovar; editar a
        /// formação é só em aprovação.
        /// </summary>
        List<Personagem> NaEquipe() =>
            sala.Slots
                .Where(s => s.Personagem != null)
                .Select(s => personagens.Buscar(s.Personagem))
                .Where(p => p != null && p.Ativo &&
                            (p.Status == StatusCriacao.EmAprovacao || p.Status == StatusCriacao.EmCarreira ||
                             p.Status == StatusCriacao.EmRevisao))
                .ToList();

        /// <summary>Personagem selecionada na formação da equipe (a que vai responder).</summary>
        string equipeSelecionada;

        /// <summary>
        /// Formação da equipe (desenho do Alan): à esquerda, as personagens em aprovação; em cima, a pergunta
        /// do arquétipo da selecionada, a resposta e o enviar; embaixo, só leitura, as respostas na ordem em
        /// que chegaram (quem envia primeiro dita a narrativa). Reenviar edita e mantém a posição.
        /// </summary>
        void SecaoEquipe()
        {
            var candidatas = NaEquipe();
            if (candidatas.All(c => c.Id != equipeSelecionada))
                equipeSelecionada = (candidatas.FirstOrDefault(c => salas.PosicaoNaFormacao(sala, c.Id) == 0)
                                     ?? candidatas.First()).Id;
            var selecionada = candidatas.First(c => c.Id == equipeSelecionada);

            var bloco = areaEquipe;

            // Esquerda: quem está em aprovação (em qualquer ordem; o enviar decide a fila).
            var coluna = Coluna(bloco, 340, 8);
            foreach (var c in candidatas)
            {
                var alvo = c;
                var posicao = salas.PosicaoNaFormacao(sala, c.Id);
                var situacao = posicao > 0 ? $"enviou · {posicao}º" : "ainda não enviou";
                var texto = $"{c.NomeExibicao}\n<size=65%>{situacao}</size>";
                var botao = c.Id == equipeSelecionada
                    ? Estilo.BotaoPrincipal(coluna, texto, 0, () => { })
                    : Estilo.BotaoContorno(coluna, texto, 0, posicao > 0 ? Estilo.TextoSuave : Estilo.Dourado, () =>
                    {
                        equipeSelecionada = alvo.Id;
                        Mostrar(null);
                        Redesenhar();
                    });
                var le = botao.GetComponent<LayoutElement>();
                le.minHeight = 64;
                le.preferredHeight = 64;
                var rotulo = botao.GetComponentInChildren<TextMeshProUGUI>();
                rotulo.fontSize = 22;
                rotulo.characterSpacing = 2;
                rotulo.textWrappingMode = TextWrappingModes.NoWrap;
                rotulo.overflowMode = TextOverflowModes.Ellipsis;
                rotulo.rectTransform.offsetMin = new Vector2(10, 0);
                rotulo.rectTransform.offsetMax = new Vector2(-10, 0);
            }


            // Direita: a pergunta e o enviar (em cima); a formação (embaixo).
            var direita = Coluna(bloco, -1, 12);

            var pergunta = Caixa(direita, Estilo.Azul);
            var regra = personagens.RegraDoArquetipo(selecionada);
            var titulo = Estilo.Rotulo(pergunta, $"{selecionada.NomeExibicao} — quando a equipe se reuniu pela primeira vez...",
                22, Estilo.Azul, FontStyles.Bold);
            titulo.textWrappingMode = TextWrappingModes.Normal;
            var textoPergunta = Estilo.Rotulo(pergunta, regra?.Equipe.Pergunta ?? string.Empty, 22, Estilo.Texto, FontStyles.Italic);
            textoPergunta.textWrappingMode = TextWrappingModes.Normal;
            var fila = Estilo.Fila(pergunta, 110, 12);
            var resposta = Estilo.AreaTexto(fila, "a resposta (reenviar edita e mantém a posição na fila)", 110);
            resposta.text = sala.FormacaoEquipe.FirstOrDefault(e => e.Personagem == selecionada.Id)?.Texto
                            ?? selecionada.EquipeResposta;
            Estilo.ColunaElastica(resposta);
            // Em carreira a formação é história: só leitura (o ••• ainda abre o texto inteiro).
            // Em revisão também: a resposta continua na formação, e volta a ser editável quando ela voltar a "em aprovação".
            var emCarreira = selecionada.Status != StatusCriacao.EmAprovacao;
            if (emCarreira)
                resposta.interactable = false;
            TMP_InputField nomeNpc = null;
            // Formação que dá NPC (ex.: o herói que o Legado impressionou): nome opcional; a resposta é a descrição.
            if (personagens.FormacaoDaNpc(selecionada))
            {
                var filaNpc = Estilo.Fila(pergunta, 50, 12);
                var cria = personagens.OQueAFormacaoCria(selecionada);
                var r = Estilo.Rotulo(filaNpc, $"{cria} · {personagens.VinculoDaFormacao(selecionada)}", 18, Estilo.TextoSuave, FontStyles.Italic);
                r.gameObject.AddComponent<LayoutElement>().preferredWidth = 420;
                nomeNpc = Estilo.CampoTexto(filaNpc, $"nome (opcional): vira {(cria == "NPC" ? "NPC" : "cenário")} da sala");
                nomeNpc.text = selecionada.EquipeNpcNome;
                Estilo.ColunaElastica(nomeNpc);
                if (emCarreira)
                    nomeNpc.interactable = false;
                var alvoNpc = selecionada;
                nomeNpc.onEndEdit.AddListener(t =>
                {
                    var r = personagens.DefinirNomeNpcDaFormacao(alvoNpc.Id, t);
                    var aviso = personagens.AvisoDeNpcRepetido(alvoNpc, CadastroPersonagens.FonteFormacao, "resposta", t);
                    Mostrar(aviso != null ? Resultado.Falha(aviso) : r);
                });
            }
            // O ••• leva junto o nome do NPC (quando a formação dá um).
            Estilo.ComExpandir(resposta, $"{selecionada.NomeExibicao}: formação da equipe", nomeNpc,
                nomeNpc == null ? null : $"{personagens.OQueAFormacaoCria(selecionada)} · {personagens.VinculoDaFormacao(selecionada)}");
            // O nome no botão evita responder pela personagem errada.
            var jaEnviou = salas.PosicaoNaFormacao(sala, selecionada.Id) > 0;
            var botoes = Coluna(fila, 260, 8);
            if (emCarreira)
                Nota(botoes, selecionada.Status == StatusCriacao.EmRevisao
                    ? "Em revisão: só leitura até voltar a \"em aprovação\"."
                    : "Em carreira: só leitura.");
            else
            {
            var enviar = Estilo.BotaoPrincipal(botoes, $"{(jaEnviou ? "Reenviar" : "Enviar")} como\n{selecionada.NomeExibicao}",
                0, () =>
                {
                    var r = salas.EnviarFormacao(sala.Id, selecionada.Id, resposta.text);
                    Mostrar(r);
                    Redesenhar();
                });
            enviar.GetComponent<LayoutElement>().preferredHeight = jaEnviou ? 60 : 110;
            // Nome comprido ("Reenviar como Garça Escarlate"): a fonte encolhe até caber.
            var rotuloEnviar = enviar.GetComponentInChildren<TextMeshProUGUI>();
            rotuloEnviar.characterSpacing = 1;
            rotuloEnviar.enableAutoSizing = true;
            rotuloEnviar.fontSizeMin = 14;
            rotuloEnviar.fontSizeMax = 22;
            rotuloEnviar.rectTransform.offsetMin = new Vector2(8, 4);
            rotuloEnviar.rectTransform.offsetMax = new Vector2(-8, -4);
            if (jaEnviou)
            {
                var envio = sala.FormacaoEquipe.First(e => e.Personagem == selecionada.Id);
                // Quem respondeu depois pode ter construído em cima: tirar este envio muda a ordem da história.
                var depois = sala.FormacaoEquipe
                    .Skip(sala.FormacaoEquipe.IndexOf(envio) + 1)
                    .Select(e => personagens.Buscar(e.Personagem)?.NomeExibicao ?? "?")
                    .ToList();
                var aviso = depois.Count == 0
                    ? string.Empty
                    : $"\n\nAtenção: {depois.Count} resposta{(depois.Count == 1 ? " foi enviada" : "s foram enviadas")} depois " +
                      $"desta e pode{(depois.Count == 1 ? "" : "m")} se apoiar nela ({string.Join(", ", depois)}). " +
                      "Desfazer muda a ordem da história. Para só corrigir o texto, use Reenviar.";
                var desfazer = Estilo.BotaoContorno(botoes, "Desfazer envio", 0, Estilo.TextoSuave, () =>
                    Estilo.Confirmar(transform,
                        envio.Anterior != null
                            ? $"Desfazer o último reenvio de {selecionada.NomeExibicao}? Volta o texto anterior; a posição na fila fica."
                            : $"Desfazer o envio de {selecionada.NomeExibicao}? Ele sai da formação, e quem vinha depois sobe uma posição." + aviso,
                        "Desfazer",
                        () =>
                        {
                            Mostrar(salas.DesfazerFormacao(sala.Id, selecionada.Id));
                            Redesenhar();
                        }));
                desfazer.GetComponent<LayoutElement>().preferredHeight = 42;
            }
            }

            // A formação da equipe: só leitura, na ordem de chegada.
            var formacao = Caixa(direita, Estilo.Dourado);
            formacao.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var filaCabeca = Estilo.Fila(formacao, 34);
            var cabeca = Estilo.Rotulo(filaCabeca, "FORMAÇÃO DA EQUIPE", 22, Estilo.Dourado, FontStyles.Bold);
            cabeca.characterSpacing = 5;
            Estilo.ColunaElastica(cabeca);
            // ••• abre a formação inteira numa tela grande, só leitura.
            if (sala.FormacaoEquipe.Count > 0)
            {
                var abrir = Estilo.BotaoContorno(filaCabeca, "•••", 70, Estilo.Dourado,
                    () => Estilo.LeitorDeTexto(transform, $"FORMAÇÃO DA EQUIPE · {sala.Nome}", TextoDaFormacao(),
                        ("Copiar para o fórum", () => FormacaoParaColar("[b]", "[/b]")),
                        ("Copiar para o WhatsApp", () => FormacaoParaColar("*", "*"))));
                abrir.GetComponent<LayoutElement>().preferredHeight = 34;
            }
            var textoDaFormacao = Estilo.ListaRolavel(formacao, 10); // só aqui rola: o resto da aba fica parado
            if (sala.FormacaoEquipe.Count == 0)
            {
                Estilo.Rotulo(textoDaFormacao, "Ninguém enviou ainda: quem enviar primeiro abre a história.", 22,
                    Estilo.TextoApagado, FontStyles.Italic).textWrappingMode = TextWrappingModes.Normal;
                RodapeDaFormacao(formacao);
                return;
            }
            var suave = ColorUtility.ToHtmlStringRGB(Estilo.TextoSuave);
            for (var k = 0; k < sala.FormacaoEquipe.Count; k++)
            {
                var envio = sala.FormacaoEquipe[k];
                var quem = personagens.Buscar(envio.Personagem);
                var jogador = quem?.Identidade.Jogador == null ? "sem jogador" : jogadores.Buscar(quem.Identidade.Jogador)?.Nome ?? "?";
                var perguntaDele = quem == null ? null : personagens.RegraDoArquetipo(quem)?.Equipe.Pergunta;
                // "1. a pergunta" (dourado), "(personagem, jogador)" pequeno, e a resposta num tom que cansa
                // menos que o branco puro na leitura longa.
                var dourado = ColorUtility.ToHtmlStringRGB(Estilo.Dourado);
                var trecho = Estilo.Rotulo(textoDaFormacao,
                    $"<b><color=#{dourado}>{k + 1}. <noparse>{perguntaDele ?? string.Empty}</noparse></color></b>\n" +
                    $"<size=80%><color=#{suave}>({quem?.NomeExibicao ?? "?"}, {jogador})</color></size>\n" +
                    $"<noparse>{envio.Texto}</noparse>", 22, Leitura);
                trecho.textWrappingMode = TextWrappingModes.Normal;
                if (k < sala.FormacaoEquipe.Count - 1)
                {
                    var traco = Estilo.Rect("Traco", textoDaFormacao);
                    traco.gameObject.AddComponent<Image>().color =
                        new Color(Estilo.Dourado.r, Estilo.Dourado.g, Estilo.Dourado.b, 0.35f);
                    var lt = traco.gameObject.AddComponent<LayoutElement>();
                    lt.minHeight = 2;
                    lt.preferredHeight = 2;
                }
            }
            RodapeDaFormacao(formacao);
        }

        // ---------- Relacionamentos (segunda etapa, na campanha): visão do mestre, só leitura ----------

        /// <summary>
        /// Relacionamentos da equipe (o jogador escolhe na ficha, aba História): à esquerda, quem está em
        /// aprovação; no meio, as frases da selecionada com o colega, o porquê e quem ocupou antes; à direita,
        /// a leitura reversa (em quais frases dos colegas ela aparece).
        /// </summary>
        void SecaoRelacionamentos()
        {
            var candidatas = NaEquipe();
            if (candidatas.All(c => c.Id != equipeSelecionada))
                equipeSelecionada = candidatas.First().Id;
            var p = candidatas.First(c => c.Id == equipeSelecionada);
            var bloco = areaEquipe;

            var coluna = Coluna(bloco, 300, 8);
            foreach (var c in candidatas)
            {
                var alvo = c;
                var frases = personagens.RegraDoArquetipo(c)?.Equipe.Relacionamentos.Count ?? 0;
                var feitas = Enumerable.Range(0, frases).Count(k => personagens.ColegaDoRelacionamento(c, k) != null);
                var texto = $"{c.NomeExibicao}\n<size=65%>{(feitas == frases ? "escolheu" : $"falta{(frases - feitas == 1 ? "" : "m")} {frases - feitas}")}</size>";
                var botao = c.Id == equipeSelecionada
                    ? Estilo.BotaoPrincipal(coluna, texto, 0, () => { })
                    : Estilo.BotaoContorno(coluna, texto, 0, feitas == frases ? Estilo.TextoSuave : Estilo.Dourado, () =>
                    {
                        equipeSelecionada = alvo.Id;
                        Mostrar(null);
                        Redesenhar();
                    });
                AjustarBotaoDeLista(botao);
            }
            TrocaDeVinculos(coluna);

            // Meio: as frases dela.
            var centro = Caixa(Coluna(bloco, -1, 0), Estilo.Azul);
            centro.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            Estilo.Rotulo(centro, $"{p.NomeExibicao} — relacionamentos", 22, Estilo.Azul, FontStyles.Bold);
            var regra = personagens.RegraDoArquetipo(p);
            var suave = ColorUtility.ToHtmlStringRGB(Estilo.TextoSuave);
            var dourado = ColorUtility.ToHtmlStringRGB(Estilo.Dourado);
            var frasesDela = Estilo.ListaRolavel(centro, 14); // só as frases rolam: título e nota ficam parados
            for (var k = 0; k < (regra?.Equipe.Relacionamentos.Count ?? 0); k++)
            {
                var colega = personagens.ColegaDoRelacionamento(p, k);
                var r = k < p.Relacoes.Count ? p.Relacoes[k] : null;
                // O nome entra no lugar do "___" (dourado); sem colega, o "___" fica em cinza.
                var lacuna = colega != null
                    ? $"</i><b><color=#{dourado}>{colega.NomeExibicao}</color></b><i>"
                    : $"<color=#{ColorUtility.ToHtmlStringRGB(Estilo.TextoApagado)}>___</color>";
                var linha = $"<i>{regra.Equipe.Relacionamentos[k].Replace("___", lacuna)}</i>";
                if (!string.IsNullOrEmpty(r?.Porque))
                    linha += $"\n<size=85%><color=#{suave}><noparse>{r.Porque}</noparse></color></size>";
                if (r != null && r.Anteriores.Count > 0)
                    linha += $"\n<size=80%><color=#{suave}>antes: {string.Join(", ", r.Anteriores.Select(id => personagens.Buscar(id)?.NomeExibicao ?? "?"))}</color></size>";
                var rotulo = Estilo.Rotulo(frasesDela, linha, 22, Estilo.Texto);
                rotulo.textWrappingMode = TextWrappingModes.Normal;
            }
            foreach (var obs in personagens.ObservacoesDaEquipe(p))
                Nota(frasesDela, obs);
            Nota(centro, "O jogador escolhe na ficha (aba História).");

            // Direita: a leitura reversa.
            var rosa = new Color(0.95f, 0.55f, 0.85f);
            var reversa = Caixa(Coluna(bloco, 380, 0), rosa);
            reversa.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var tituloReversa = Estilo.Rotulo(reversa, $"{p.NomeExibicao.ToUpperInvariant()} APARECE NAS FRASES DE", 20, rosa, FontStyles.Bold);
            tituloReversa.textWrappingMode = TextWrappingModes.Normal;
            var citacoes = personagens.CitacoesEmRelacionamentos(p);
            var citadaEm = Estilo.ListaRolavel(reversa, 10);
            if (citacoes.Count == 0)
                Nota(citadaEm, "Ninguém ainda.");
            foreach (var (quem, frase) in citacoes)
            {
                var textoFrase = personagens.RegraDoArquetipo(quem)?.Equipe.Relacionamentos[frase] ?? string.Empty;
                var c = Estilo.Rotulo(citadaEm, $"<b>{quem.NomeExibicao}</b>\n<size=85%><i>{textoFrase.Replace("___", $"</i><b><color=#{dourado}>{p.NomeExibicao}</color></b><i>")}</i></size>", 22, Estilo.Texto);
                c.textWrappingMode = TextWrappingModes.Normal;
            }
            Nota(reversa, "Lido das fichas dos colegas.");
        }

        // ---------- Influência inicial (segunda etapa, na campanha) ----------


        /// <summary>
        /// Influência (desenho do Alan): à esquerda, quem está em aprovação; em azul, a regra do arquétipo
        /// (e o comportamento, quando há); em vermelho, os colegas para marcar e o confirmar ("Dar
        /// Influência a todos" quando a regra é todos); em verde, a quem deu; em rosa, a leitura reversa
        /// (quem deu Influência a ela, lido das outras fichas).
        /// </summary>
        void SecaoInfluencia()
        {
            var candidatas = NaEquipe();
            if (candidatas.All(c => c.Id != equipeSelecionada))
                equipeSelecionada = (candidatas.FirstOrDefault(c => !personagens.InfluenciaConfirmada(c)) ?? candidatas.First()).Id;
            var p = candidatas.First(c => c.Id == equipeSelecionada);
            var bloco = areaEquipe;

            // Esquerda: quem está em aprovação.
            var coluna = Coluna(bloco, 300, 8);
            foreach (var c in candidatas)
            {
                var alvo = c;
                var feito = personagens.InfluenciaConfirmada(c);
                var texto = $"{c.NomeExibicao}\n<size=65%>{(feito ? "deu Influência" : "ainda não deu")}</size>";
                var botao = c.Id == equipeSelecionada
                    ? Estilo.BotaoPrincipal(coluna, texto, 0, () => { })
                    : Estilo.BotaoContorno(coluna, texto, 0, feito ? Estilo.TextoSuave : Estilo.Dourado, () =>
                    {
                        equipeSelecionada = alvo.Id;
                        Mostrar(null);
                        Redesenhar();
                    });
                AjustarBotaoDeLista(botao);
            }
            TrocaDeVinculos(coluna);

            // Centro: a regra (azul) e, embaixo, escolher (vermelho) | deu (verde).
            var centro = Coluna(bloco, -1, 12);
            var regra = personagens.RegraDoArquetipo(p)?.Influencia;
            var caixaRegra = Caixa(centro, Estilo.Azul);
            Estilo.Rotulo(caixaRegra, $"{p.NomeExibicao} — Influência inicial", 22, Estilo.Azul, FontStyles.Bold);
            var textoRegra = Estilo.Rotulo(caixaRegra, regra?.Texto ?? string.Empty, 22, Estilo.Texto, FontStyles.Italic);
            textoRegra.textWrappingMode = TextWrappingModes.Normal;
            // A escolha é do jogador, na ficha (Núcleo): aqui o mestre só lê.
            if (regra?.Regra == Core.Regras.RegrasArquetipos.RegraInfluencia.Comportamento)
            {
                var escolhido = regra.Opcoes.FirstOrDefault(o => o.Id == p.InfluenciaComportamento);
                Nota(caixaRegra, escolhido == null
                    ? "Comportamento: ainda não escolhido (na ficha)."
                    : $"Comportamento escolhido: {escolhido.Nome}.");
            }

            var baixo = Estilo.Rect("Escolha", centro);
            baixo.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var hb = baixo.gameObject.AddComponent<HorizontalLayoutGroup>();
            hb.spacing = 12;
            hb.childControlWidth = true;
            hb.childControlHeight = true;
            hb.childForceExpandWidth = false;
            hb.childForceExpandHeight = true;

            // Verde: a quem deu.
            var deu = Caixa(baixo, new Color(0.45f, 0.80f, 0.50f));
            deu.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Estilo.Rotulo(deu, "DEU INFLUÊNCIA A", 20, new Color(0.45f, 0.80f, 0.50f), FontStyles.Bold).characterSpacing = 4;
            var dados = personagens.InfluenciaDada(p);
            if (dados.Count == 0)
                Nota(deu, personagens.InfluenciaConfirmada(p) ? "Ninguém (pela regra)." : "Ainda não deu (o jogador marca na ficha, Núcleo).");
            var suaveInfluencia = ColorUtility.ToHtmlStringRGB(Estilo.TextoSuave);
            foreach (var id in personagens.InfluenciaPerdida(p))
                Estilo.Rotulo(deu, "• " + (personagens.Buscar(id)?.NomeExibicao ?? "?") + " (perdida)", 24, Estilo.TextoApagado);
            foreach (var id in dados)
            {
                // O nome e, se houver, o porquê (escrito pelo jogador na ficha).
                var porque = personagens.PorqueDaInfluencia(p, id);
                var linha = Estilo.Rotulo(deu, "• " + (personagens.Buscar(id)?.NomeExibicao ?? "?") +
                    (string.IsNullOrEmpty(porque) ? string.Empty
                        : $"\n<size=80%><i><color=#{suaveInfluencia}><noparse>{porque}</noparse></color></i></size>"),
                    24, Estilo.Texto);
                linha.textWrappingMode = TextWrappingModes.Normal;
            }
            // NPCs (marcados pelo mestre na ficha do NPC) e os adultos pelo padrão do livro.
            foreach (var n in personagens.NpcsAQuemDeuInfluencia(p))
                Estilo.Rotulo(deu, $"• {n.NomeExibicao} <size=75%><color=#{suaveInfluencia}>(NPC)</color></size>", 24, Estilo.Texto);
            foreach (var n in personagens.NpcsQuePerderamInfluenciaSobre(p))
                Estilo.Rotulo(deu, $"• {n.NomeExibicao} (NPC, perdida)", 24, Estilo.TextoApagado);
            var adultos = personagens.AdultosComInfluenciaPorPadrao(p);
            if (adultos > 0)
                Nota(deu, $"E todo adulto da campanha, por padrão ({adultos}).");

            // Rosa: a leitura reversa.
            var rosa = new Color(0.95f, 0.55f, 0.85f);
            var reversa = Caixa(Coluna(bloco, 340, 0), rosa);
            reversa.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var tituloReversa = Estilo.Rotulo(reversa, $"{p.NomeExibicao.ToUpperInvariant()} EXERCE INFLUÊNCIA SOBRE", 20, rosa, FontStyles.Bold);
            tituloReversa.textWrappingMode = TextWrappingModes.Normal;
            var deram = personagens.QuemDeuInfluencia(p);
            var npcsSob = personagens.NpcsSobQuemExerce(p);
            if (deram.Count == 0 && npcsSob.Count == 0)
                Nota(reversa, "Ninguém ainda (nenhum colega deu Influência a ela).");
            foreach (var c in deram)
                Estilo.Rotulo(reversa, "• " + c.NomeExibicao, 24, Estilo.Texto);
            foreach (var n in npcsSob)
                Estilo.Rotulo(reversa, $"• {n.NomeExibicao} <size=75%><color=#{suaveInfluencia}>(NPC)</color></size>", 24, Estilo.Texto);
            // Pelo estado nas fichas dos colegas: quem deu Influência a ela e, no jogo, tirou.
            foreach (var c in personagens.QuemPerdeuInfluencia(p))
                Estilo.Rotulo(reversa, "• " + c.NomeExibicao + " (perdida)", 24, Estilo.TextoApagado);
            foreach (var n in personagens.NpcsSobQuemPerdeu(p))
                Estilo.Rotulo(reversa, $"• {n.NomeExibicao} (NPC, perdida)", 24, Estilo.TextoApagado);
            Nota(reversa, "Lido das fichas dos colegas e dos NPCs.");
        }


        static void Nota(Transform pai, string texto)
        {
            var n = Estilo.Rotulo(pai, texto, 20, Estilo.TextoSuave, FontStyles.Italic);
            n.textWrappingMode = TextWrappingModes.Normal;
        }

        /// <summary>Botão da coluna de personagens: nome numa linha, situação menor embaixo.</summary>
        static void AjustarBotaoDeLista(UnityEngine.UI.Button botao)
        {
            var le = botao.GetComponent<LayoutElement>();
            le.minHeight = 64;
            le.preferredHeight = 64;
            var rotulo = botao.GetComponentInChildren<TextMeshProUGUI>();
            rotulo.fontSize = 22;
            rotulo.characterSpacing = 2;
            rotulo.textWrappingMode = TextWrappingModes.NoWrap;
            rotulo.overflowMode = TextOverflowModes.Ellipsis;
            rotulo.rectTransform.offsetMin = new Vector2(10, 0);
            rotulo.rectTransform.offsetMax = new Vector2(-10, 0);
        }

        /// <summary>A formação inteira num texto só (para o leitor do •••), no mesmo formato da caixa.</summary>
        string TextoDaFormacao()
        {
            var dourado = ColorUtility.ToHtmlStringRGB(Estilo.Dourado);
            var suave = ColorUtility.ToHtmlStringRGB(Estilo.TextoSuave);
            var leitura = ColorUtility.ToHtmlStringRGB(Leitura);
            var partes = new List<string>();
            for (var k = 0; k < sala.FormacaoEquipe.Count; k++)
            {
                var envio = sala.FormacaoEquipe[k];
                var quem = personagens.Buscar(envio.Personagem);
                var jogador = quem?.Identidade.Jogador == null ? "sem jogador" : jogadores.Buscar(quem.Identidade.Jogador)?.Nome ?? "?";
                var pergunta = quem == null ? null : personagens.RegraDoArquetipo(quem)?.Equipe.Pergunta;
                partes.Add($"<b><color=#{dourado}>{k + 1}. <noparse>{pergunta ?? string.Empty}</noparse></color></b>\n" +
                           $"<size=80%><color=#{suave}>({quem?.NomeExibicao ?? "?"}, {jogador})</color></size>\n" +
                           $"<color=#{leitura}><noparse>{envio.Texto}</noparse></color>");
            }
            return string.Join("\n\n", partes);
        }

        /// <summary>
        /// A formação em texto simples para colar fora da Torre: a pergunta numerada em negrito (BBCode no
        /// fórum, *asteriscos* no WhatsApp), quem respondeu, a resposta. A regra da fila vai no fim.
        /// </summary>
        string FormacaoParaColar(string abreNegrito, string fechaNegrito)
        {
            var partes = new List<string> { $"{abreNegrito}Formação da equipe · {sala.Nome}{fechaNegrito}" };
            for (var k = 0; k < sala.FormacaoEquipe.Count; k++)
            {
                var envio = sala.FormacaoEquipe[k];
                var quem = personagens.Buscar(envio.Personagem);
                var jogador = quem?.Identidade.Jogador == null ? "sem jogador" : jogadores.Buscar(quem.Identidade.Jogador)?.Nome ?? "?";
                var pergunta = quem == null ? null : personagens.RegraDoArquetipo(quem)?.Equipe.Pergunta;
                partes.Add($"{abreNegrito}{k + 1}. {pergunta}{fechaNegrito}\n({quem?.NomeExibicao ?? "?"}, {jogador})\n{envio.Texto}");
            }
            partes.Add("Quem responde primeiro estabelece a verdade; os demais constroem sobre ela.");
            return string.Join("\n\n", partes);
        }

        /// <summary>A regra da fila, sempre à vista no pé da formação.</summary>
        static void RodapeDaFormacao(Transform formacao)
        {
            var vao = Estilo.Rect("Vao", formacao);
            vao.gameObject.AddComponent<LayoutElement>().minHeight = 6;
            var regra = Estilo.Rotulo(formacao,
                "Quem responde primeiro estabelece a verdade; os demais constroem sobre ela. " +
                "Não se volta atrás para deixar a história mais conveniente.",
                20, Estilo.TextoSuave, FontStyles.Italic);
            regra.textWrappingMode = TextWrappingModes.Normal;
        }

        /// <summary>Cor do texto corrido da formação: mais suave que o branco, para leitura longa.</summary>
        static readonly Color Leitura = new Color(0.80f, 0.83f, 0.90f);

        static RectTransform Coluna(Transform pai, float largura, float espaco)
        {
            var coluna = Estilo.Rect("Coluna", pai);
            var le = coluna.gameObject.AddComponent<LayoutElement>();
            if (largura < 0)
                le.flexibleWidth = 1;
            else
            {
                // Largura fixa: sem isto a coluna herda "esticável" de dentro (o texto do botão) e rouba
                // o espaço de quem está ao lado.
                le.layoutPriority = 2;
                le.minWidth = largura;
                le.preferredWidth = largura;
                le.flexibleWidth = 0;
            }
            var v = coluna.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = espaco;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;
            return coluna;
        }

        static RectTransform Caixa(Transform pai, Color borda)
        {
            var caixa = Estilo.Rect("Caixa", pai);
            Estilo.Moldura(caixa.gameObject, Estilo.Linha, borda);
            var v = caixa.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(18, 18, 14, 16);
            v.spacing = 10;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;
            return caixa;
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

        static IReadOnlyList<(string id, string nome)> OpcoesPapel() => PainelNpc.OpcoesPapel();

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

            // Os NPCs desta campanha vão e voltam em JSON (o mesmo id volta por cima, como no laboratório).
            var troca = Estilo.Fila(lista, 50);
            troca.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 0, 0);
            var dica = Estilo.Rotulo(troca, "Revisar fora ou levar a outra Torre: exporte aqui, importe de volta (o mesmo NPC atualiza por cima).",
                18, Estilo.TextoApagado, FontStyles.Italic);
            Estilo.ColunaElastica(dica);
            Estilo.BotaoContorno(troca, "Exportar", LarguraBotao, Estilo.Dourado, ExportarNpcs);
            Estilo.BotaoContorno(troca, "Importar", LarguraBotao, Estilo.Dourado, ImportarNpcs);

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
            var origem = personagens.OrigemDoNpc(npc);
            // O laço: da ficha (dona · vínculo) ou do mestre (Ligado a: heroína · como).
            var laco = origem is { } o ? $"{o.dono.NomeExibicao} · {o.vinculo}"
                : string.Join(", ", personagens.LigacoesDoNpc(npc).Select(l =>
                    string.IsNullOrWhiteSpace(l.como) ? l.heroi.NomeExibicao : $"{l.heroi.NomeExibicao} · {l.como}"));
            var texto = $"<b>{npc.NomeCompleto}</b>" +
                        (laco.Length > 0 ? $"  <size=80%><color=#{ColorUtility.ToHtmlStringRGB(Estilo.Dourado)}>{laco}</color></size>" : string.Empty) +
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

        // ---------- Cenário da sala: locais, organizações, outros (não é gente, então não é NPC) ----------

        static IReadOnlyList<(string id, string nome)> OpcoesTipoCenario() =>
            new[] { ElementoDeCenario.Local, ElementoDeCenario.Organizacao, ElementoDeCenario.Outro }
                .Select(t => (t, ElementoDeCenario.NomeDoTipo(t))).ToList();

        void SecaoCenario()
        {
            var novo = Estilo.Fila(lista, 62);
            novo.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 0, 0);
            campoCenario = Estilo.CampoTexto(novo, "Nome do lugar, organização ou outro e Enter");
            campoCenario.text = cenarioNome;
            Estilo.ColunaElastica(campoCenario);
            campoCenario.onValueChanged.AddListener(t => cenarioNome = t);
            campoCenario.onSubmit.AddListener(_ => CriarElemento());
            Estilo.ListaSuspensa(novo, LarguraPerfil, OpcoesTipoCenario(), cenarioTipo, id => cenarioTipo = id);
            Estilo.BotaoPrincipal(novo, "Criar", LarguraBotao * 2 + 14, CriarElemento);

            // A cidade inteira (cada campanha é uma cidade, por enquanto) vai e vem em JSON.
            var troca = Estilo.Fila(lista, 50);
            troca.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 0, 0);
            var dica = Estilo.Rotulo(troca, "Levar a cidade para outra campanha ou outra Torre: exporte aqui, importe lá.",
                18, Estilo.TextoApagado, FontStyles.Italic);
            Estilo.ColunaElastica(dica);
            Estilo.BotaoContorno(troca, "Exportar", LarguraBotao, Estilo.Dourado, ExportarCenario);
            Estilo.BotaoContorno(troca, "Importar", LarguraBotao, Estilo.Dourado, ImportarCenario);

            var elementos = personagens.CenarioDaSala(sala.Id);
            foreach (var (ativo, titulo) in new[] { (true, "CENÁRIO"), (false, "CENÁRIO INATIVO") })
            {
                var doBloco = elementos.Where(e => e.Ativo == ativo).ToList();
                if (doBloco.Count == 0)
                    continue;
                var bloco = Estilo.Bloco(lista, titulo, doBloco.Count);
                foreach (var e in doBloco)
                    LinhaCenario(e, bloco);
            }
        }

        void CriarElemento()
        {
            var resultado = personagens.CriarElemento(sala.Id, cenarioNome, cenarioTipo);
            Mostrar(resultado);
            if (!resultado.Ok)
                return;
            cenarioNome = string.Empty;
            Redesenhar();
            campoCenario.ActivateInputField();
        }

        /// <summary>Um elemento: nome, tipo e Ativo na primeira linha; a descrição (texto livre) embaixo.</summary>
        void LinhaCenario(ElementoDeCenario e, RectTransform bloco)
        {
            var caixa = Estilo.Rect("Elemento", bloco);
            var v = caixa.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(10, 10, 8, 10);
            v.spacing = 8;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandHeight = false;
            Estilo.Moldura(caixa.gameObject, Estilo.Linha, e.Ativo ? Estilo.Borda : Estilo.BordaApagada);

            var linha = Estilo.Fila(caixa, 48);
            var nome = Estilo.CampoTexto(linha, "nome");
            nome.text = e.Nome;
            Estilo.ColunaElastica(nome);
            var origem = personagens.OrigemDoElemento(e);
            if (origem is { } o)
            {
                var laco = Estilo.Rotulo(linha, $"{o.dono.NomeExibicao} · {o.vinculo}", 18, Estilo.Dourado);
                laco.textWrappingMode = TextWrappingModes.NoWrap;
                laco.overflowMode = TextOverflowModes.Ellipsis;
                laco.gameObject.AddComponent<LayoutElement>().preferredWidth = 300;
                // O nome vem da ficha dela (muda lá, muda aqui); a descrição foi copiada uma vez e é do mestre.
                // Nome automático (ex.: "Santuário da Crepúsculo") o mestre renomeia.
                nome.interactable = !personagens.NomeDoElementoVemDaFicha(e);
            }
            Estilo.ListaSuspensa(linha, LarguraPerfil, OpcoesTipoCenario(), e.Tipo, id =>
            {
                Mostrar(personagens.AlterarElemento(e.Id, e.Nome, id, e.Descricao));
                Redesenhar();
            });
            if (e.Ativo)
                Estilo.BotaoContorno(linha, "Ativo", LarguraBotao, Estilo.Dourado, () => AlternarElemento(e));
            else
                Estilo.BotaoContorno(linha, "Inativo", LarguraBotao, Estilo.TextoApagado, () => AlternarElemento(e));

            var descricao = Estilo.AreaTexto(caixa, "descrição: o que é, onde fica, quem manda, o que aconteceu lá...", 96);
            descricao.text = e.Descricao;
            Estilo.ComExpandir(descricao, e.Nome);

            // Grava ao sair do campo (nome ou descrição), como as outras caixas da sala.
            void Gravar(string _)
            {
                if (nome.text.Trim() == e.Nome && descricao.text.Trim() == e.Descricao)
                    return;
                Mostrar(personagens.AlterarElemento(e.Id, nome.text, e.Tipo, descricao.text));
                Redesenhar();
            }
            nome.onEndEdit.AddListener(Gravar);
            descricao.onDeselect.AddListener(Gravar);
        }

        /// <summary>
        /// Embaixo da lista (abas Influência e Relacionamentos): exportar e importar os dois juntos, para revisar
        /// fora e trazer de volta. Importar só mexe nesses campos das heroínas desta campanha.
        /// </summary>
        void TrocaDeVinculos(Transform coluna)
        {
            Estilo.Rect("Vao", coluna).gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var r = Estilo.Rotulo(coluna, "Influência e relacionamentos", 16, Estilo.TextoSuave, FontStyles.Italic);
            r.alignment = TextAlignmentOptions.Center;
            var fila = Estilo.Fila(coluna, 44, 8);
            Estilo.BotaoContorno(fila, "Exportar", 0, Estilo.Dourado, ExportarVinculos)
                .GetComponent<LayoutElement>().flexibleWidth = 1;
            Estilo.BotaoContorno(fila, "Importar", 0, Estilo.Dourado, ImportarVinculos)
                .GetComponent<LayoutElement>().flexibleWidth = 1;
        }

        void ExportarVinculos()
        {
            var origem = "Campanha " + sala.Nome;
            var json = personagens.ExportarVinculos(NaEquipe(), origem);
            if (Troca.Arquivos == null)
                return;
            string caminho;
            try
            {
                caminho = Troca.Arquivos.Exportar("Influência e relacionamentos - " + origem, json);
            }
            catch (Exception e)
            {
                Mostrar(Resultado.Falha("Não deu para exportar: " + e.Message));
                return;
            }
            GUIUtility.systemCopyBuffer = json;
            Estilo.Confirmar(transform,
                $"Influência e relacionamentos exportados ({origem}) em\n<size=80%>{caminho}</size>\nO JSON também foi copiado (Ctrl+V para colar).",
                "Abrir a pasta",
                () => Application.OpenURL("file:///" + Troca.Arquivos.PastaExportacoes.Replace('\\', '/')));
        }

        void ImportarVinculos()
        {
            if (Troca.Arquivos == null)
                return;
            var opcoes = new List<(string, UnityEngine.Events.UnityAction, bool)>();
            foreach (var caminho in Troca.Importaveis(CadastroPersonagens.FormatoDeTrocaVinculos))
            {
                var arquivo = caminho;
                opcoes.Add((System.IO.Path.GetFileName(arquivo), () => ImportarVinculosDe(Troca.Arquivos.Ler(arquivo)), false));
            }
            opcoes.Add(("Colar da área de transferência", () => ImportarVinculosDe(GUIUtility.systemCopyBuffer), false));
            opcoes.Add(("Abrir a pasta de importar", () =>
                Application.OpenURL("file:///" + Troca.Arquivos.PastaImportar.Replace('\\', '/')), true));
            Estilo.Escolher(transform, $"Importar Influência e relacionamentos para {sala.Nome}", opcoes.ToArray());
        }

        void ImportarVinculosDe(string json) =>
            Estilo.Confirmar(transform, $"Substituir a Influência e os relacionamentos das heroínas de {sala.Nome} pelos do arquivo?",
                "Substituir", () =>
                {
                    var r = personagens.ImportarVinculos(json, NaEquipe(), out var n);
                    Redesenhar();
                    Mostrar(r, $"{n} heroína{(n == 1 ? "" : "s")} atualizada{(n == 1 ? "" : "s")}.");
                });

        void ExportarNpcs()
        {
            var origem = "Campanha " + sala.Nome;
            var json = personagens.ExportarNpcs(sala.Id, origem);
            if (json == null)
            {
                Mostrar(Resultado.Falha("Nenhum NPC para exportar."));
                return;
            }
            if (Troca.Arquivos == null)
                return;
            string caminho;
            try
            {
                caminho = Troca.Arquivos.Exportar("NPCs - " + origem, json);
            }
            catch (Exception e)
            {
                Mostrar(Resultado.Falha("Não deu para exportar: " + e.Message));
                return;
            }
            GUIUtility.systemCopyBuffer = json;
            Estilo.Confirmar(transform,
                $"NPCs exportados ({origem}) em\n<size=80%>{caminho}</size>\nO JSON também foi copiado (Ctrl+V para colar).",
                "Abrir a pasta",
                () => Application.OpenURL("file:///" + Troca.Arquivos.PastaExportacoes.Replace('\\', '/')));
        }

        /// <summary>Um dos .json de NPCs em dados/importar, ou a área de transferência; entra nesta campanha.</summary>
        void ImportarNpcs()
        {
            if (Troca.Arquivos == null)
                return;
            var opcoes = new List<(string, UnityEngine.Events.UnityAction, bool)>();
            foreach (var caminho in Troca.Importaveis(CadastroPersonagens.FormatoDeTrocaNpcs))
            {
                var arquivo = caminho;
                opcoes.Add((System.IO.Path.GetFileName(arquivo), () => ImportarNpcsDe(Troca.Arquivos.Ler(arquivo)), false));
            }
            opcoes.Add(("Colar da área de transferência", () => ImportarNpcsDe(GUIUtility.systemCopyBuffer), false));
            opcoes.Add(("Abrir a pasta de importar", () =>
                Application.OpenURL("file:///" + Troca.Arquivos.PastaImportar.Replace('\\', '/')), true));
            Estilo.Escolher(transform, $"Importar NPCs para {sala.Nome}", opcoes.ToArray());
        }

        void ImportarNpcsDe(string json)
        {
            var resultado = personagens.ImportarNpcs(json, sala.Id, out var novos, out var atualizados);
            Redesenhar();
            var partes = new List<string>();
            if (atualizados.Count > 0)
                partes.Add($"{atualizados.Count} atualizado{(atualizados.Count == 1 ? "" : "s")} por cima");
            if (novos.Count > 0 || partes.Count == 0)
                partes.Add($"{novos.Count} novo{(novos.Count == 1 ? "" : "s")}");
            Mostrar(resultado, $"NPCs: {string.Join(", ", partes)}.");
        }

        void ExportarCenario()
        {
            var origem = "Campanha " + sala.Nome;
            var json = personagens.ExportarCenario(sala.Id, origem);
            if (json == null)
            {
                Mostrar(Resultado.Falha("Nenhum elemento de cenário para exportar."));
                return;
            }
            if (Troca.Arquivos == null)
                return;
            string caminho;
            try
            {
                caminho = Troca.Arquivos.Exportar("Cenário - " + origem, json);
            }
            catch (Exception e)
            {
                Mostrar(Resultado.Falha("Não deu para exportar: " + e.Message));
                return;
            }
            GUIUtility.systemCopyBuffer = json;
            Estilo.Confirmar(transform,
                $"Cenário exportado ({origem}) em\n<size=80%>{caminho}</size>\nO JSON também foi copiado (Ctrl+V para colar).",
                "Abrir a pasta",
                () => Application.OpenURL("file:///" + Troca.Arquivos.PastaExportacoes.Replace('\\', '/')));
        }

        /// <summary>Um dos .json de cenário em dados/importar, ou a área de transferência; entra nesta campanha.</summary>
        void ImportarCenario()
        {
            if (Troca.Arquivos == null)
                return;
            var opcoes = new List<(string, UnityEngine.Events.UnityAction, bool)>();
            foreach (var caminho in Troca.Importaveis(CadastroPersonagens.FormatoDeTrocaCenario))
            {
                var arquivo = caminho;
                opcoes.Add((System.IO.Path.GetFileName(arquivo), () => ImportarCenarioDe(Troca.Arquivos.Ler(arquivo)), false));
            }
            opcoes.Add(("Colar da área de transferência", () => ImportarCenarioDe(GUIUtility.systemCopyBuffer), false));
            opcoes.Add(("Abrir a pasta de importar", () =>
                Application.OpenURL("file:///" + Troca.Arquivos.PastaImportar.Replace('\\', '/')), true));
            Estilo.Escolher(transform, $"Importar cenário para {sala.Nome}", opcoes.ToArray());
        }

        void ImportarCenarioDe(string json)
        {
            var resultado = personagens.ImportarCenario(json, sala.Id, out var novos, out var repetidos);
            Redesenhar();
            Mostrar(resultado, $"{novos} elemento{(novos == 1 ? "" : "s")} de cenário importado{(novos == 1 ? "" : "s")}" +
                               (repetidos > 0 ? $"; {repetidos} já existia{(repetidos == 1 ? "" : "m")} (mesmo nome) e ficou como estava." : "."));
        }

        void AlternarElemento(ElementoDeCenario e)
        {
            Mostrar(personagens.DefinirAtivoElemento(e.Id, !e.Ativo));
            Redesenhar();
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
