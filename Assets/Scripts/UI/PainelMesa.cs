using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using TorreDeControle.Core;
using TorreDeControle.Core.Armazenamento;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Salas;
using UnityEngine;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// A mesa de jogo de uma campanha (primeiro corte, "quadrada e feia" por decisão do Alan): as heroínas em
    /// carreira sentadas nas quatro arestas, a pilha de Equipe no centro e o log da mesa. Tudo o que muda vira
    /// evento (Core/Mesa), com desfazer. Rolador, vilão na mesa e post do fórum entram nos próximos cortes.
    /// </summary>
    public sealed class PainelMesa : MonoBehaviour
    {
        Sala sala;
        CadastroSalas salas;
        CadastroPersonagens personagens;
        Mesa mesa;
        Action aoFechar;

        TextMeshProUGUI titulo;
        TextMeshProUGUI subtitulo;
        TextMeshProUGUI mensagem;
        RectTransform tabuleiro;
        RectTransform dialogo;
        PainelMestre painelMestre;

        static readonly string[] Abrev = { "Irr", "Ass", "Cul", "Des", "Ins" };
        static readonly string[] AbrevRotulo = { "Per", "Abe", "Sal", "Sup", "Mun" };

        public static PainelMesa Criar(Transform pai)
        {
            var veu = Estilo.Rect("PainelMesa", pai);
            Estilo.Esticar(veu);
            veu.gameObject.AddComponent<Image>().color = Estilo.Veu;
            var painel = veu.gameObject.AddComponent<PainelMesa>();
            painel.Montar(veu);
            return painel;
        }

        Core.Jogadores.CadastroJogadores jogadores;

        /// <summary>O armazenamento dos dados (ligado pela Tela de Entrada): a sala abre a mesa sem passar por ela.</summary>
        public static IArmazenamento Armazenamento { get; set; }

        static readonly HashSet<string> copiadas = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimparEstaticos()
        {
            Armazenamento = null; // Play sem domain reload
            copiadas.Clear();
        }

        public void Abrir(Sala salaAberta, CadastroSalas cadastroSalas, CadastroPersonagens cadastroPersonagens,
            Core.Jogadores.CadastroJogadores cadastroJogadores, IArmazenamento armazenamento, Action aoFecharMesa)
        {
            jogadores = cadastroJogadores;
            sala = salaAberta;
            salas = cadastroSalas;
            personagens = cadastroPersonagens;
            aoFechar = aoFecharMesa;
            // Antes de qualquer comando de jogo: uma cópia de segurança da campanha (uma por sala por sessão).
            string falhaDaCopia = null;
            if (armazenamento is ICopiaDeSeguranca copia && !copiadas.Contains(sala.Id))
            {
                try
                {
                    copia.CopiaDeSeguranca("mesa " + sala.Nome);
                    copiadas.Add(sala.Id); // só depois do sucesso: se falhar, a próxima abertura tenta de novo
                }
                catch (Exception e)
                {
                    falhaDaCopia = "Cópia de segurança falhou (" + e.Message + "): reabra a mesa para tentar de novo.";
                    Debug.LogWarning("[Torre] " + falhaDaCopia);
                }
            }
            regrasDeCondicoes ??= Core.Regras.RegrasDeCondicoes.Carregar(new FonteDeRegrasResources());
            mesa = Mesa.Carregar(armazenamento, personagens, sala.Id, regrasDeCondicoes);
            salas.MarcarMesaAberta(sala.Id);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Mostrar(falhaDaCopia == null ? (Resultado?)null : Resultado.Falha(falhaDaCopia));
            Redesenhar();
        }

        void Fechar()
        {
            gameObject.SetActive(false);
            aoFechar?.Invoke();
        }

        void Mostrar(Resultado? r, string sucesso = null)
        {
            var erro = r.HasValue && !r.Value.Ok ? r.Value.Erro : null;
            mensagem.color = erro != null ? Estilo.Erro : Estilo.TextoSuave;
            mensagem.text = erro ?? sucesso ?? string.Empty;
        }

        // ---------- montagem ----------

        void Montar(RectTransform veu)
        {
            var cartao = Estilo.Rect("Cartao", veu);
            cartao.anchorMin = cartao.anchorMax = new Vector2(0.5f, 0.5f);
            cartao.sizeDelta = new Vector2(1820, 1000);
            Estilo.Moldura(cartao.gameObject, Estilo.Cartao, Estilo.Borda);
            var coluna = cartao.gameObject.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(40, 40, 28, 28);
            coluna.spacing = 10;
            coluna.childControlWidth = coluna.childControlHeight = true;
            coluna.childForceExpandHeight = false;

            var cabecalho = Estilo.Fila(cartao, 72);
            var titulos = Estilo.Rect("Titulos", cabecalho);
            titulos.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var pilha = titulos.gameObject.AddComponent<VerticalLayoutGroup>();
            pilha.childControlWidth = pilha.childControlHeight = true;
            pilha.childForceExpandHeight = false;
            titulo = Estilo.Rotulo(titulos, string.Empty, 38, Estilo.Dourado, FontStyles.Bold);
            titulo.characterSpacing = 6;
            subtitulo = Estilo.Rotulo(titulos, string.Empty, 22, Estilo.TextoSuave);
            Estilo.BotaoContorno(cabecalho, "Sala", 140, Estilo.Dourado, AbrirSala)
                .gameObject.AddComponent<Dica>().Configurar("Abrir a sala (vagas, NPCs, cenário, formação). O Voltar de lá traz de volta à mesa.");
            Estilo.BotaoContorno(cabecalho, "Ajuda", 150, Estilo.Dourado, AbrirAjuda)
                .gameObject.AddComponent<Dica>().Configurar("Consultar as cartas (movimentos, Equipe, vilões, primeira sessão...).");
            Estilo.BotaoContorno(cabecalho, "Bônus", 170, Estilo.Dourado, () => AbrirBonus(null));
            Estilo.BotaoPrincipal(cabecalho, "Área do mestre", 280, AbrirAreaDoMestre);
            Estilo.BotaoContorno(cabecalho, "Voltar", 190, Estilo.TextoSuave, Fechar);

            mensagem = Estilo.Rotulo(cartao, string.Empty, 20, Estilo.TextoSuave);
            mensagem.gameObject.AddComponent<LayoutElement>().minHeight = 26;

            tabuleiro = Estilo.Rect("Tabuleiro", cartao);
            tabuleiro.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            Estilo.Moldura(tabuleiro.gameObject, Estilo.Linha, Estilo.BordaApagada);
        }

        PainelFicha painelFicha;
        PainelSala painelSala;

        /// <summary>A sala desta campanha por cima da mesa; o Voltar da sala volta à mesa (redesenhada).</summary>
        void AbrirSala()
        {
            if (painelSala == null)
                painelSala = PainelSala.Criar(transform.parent);
            gameObject.SetActive(false);
            painelSala.Abrir(sala, salas, personagens, jogadores, () =>
            {
                gameObject.SetActive(true);
                transform.SetAsLastSibling();
                Redesenhar();
            }, abertaPelaMesa: true);
        }
        PainelNpc painelNpc;

        void AbrirFichaDoNpc(Personagem npc)
        {
            if (painelNpc == null)
                painelNpc = PainelNpc.Criar(transform.parent);
            painelNpc.Abrir(npc, personagens, Redesenhar);
        }

        void AbrirFicha(Personagem p)
        {
            if (painelFicha == null)
                painelFicha = PainelFicha.Criar(transform.parent);
            painelFicha.Abrir(p, personagens, jogadores, Redesenhar, sala.Nome, abertaPelaSala: true);
        }

        void AbrirAreaDoMestre()
        {
            if (painelMestre == null)
                painelMestre = PainelMestre.Criar(transform.parent);
            gameObject.SetActive(false);
            painelMestre.Abrir(sala, salas, personagens, () =>
            {
                gameObject.SetActive(true);
                Redesenhar();
            }, mesa);
        }

        /// <summary>As heroínas da campanha: as das vagas, ativas e em carreira (só elas recebem comando de jogo).</summary>
        List<Personagem> DaSala() =>
            sala.Slots.Where(v => v.Personagem != null).Select(v => personagens.Buscar(v.Personagem))
                .Where(p => p != null && p.Ativo && p.Status == StatusCriacao.EmCarreira).ToList();

        /// <summary>
        /// As heroínas na cena aberta: só elas sentam, rolam, ajudam e entram em batalha (p.84: quem ajuda precisa poder
        /// ajudar na ficção). As outras assistem. Sem cena, ninguém.
        /// </summary>
        List<Personagem> NaMesa() => DaSala().Where(p => mesa.HeroiEmCena(p.Id)).ToList();

        void Redesenhar()
        {
            for (var i = tabuleiro.childCount - 1; i >= 0; i--)
                Destroy(tabuleiro.GetChild(i).gameObject);
            titulo.text = $"MESA · {sala.Nome.ToUpperInvariant()}";
            var herois = NaMesa();
            var assistindo = DaSala().Where(h => !mesa.HeroiEmCena(h.Id)).ToList();
            var foraDaMesa = sala.Slots.Count(v => v.Personagem != null) - DaSala().Count;
            var cena = mesa.CenaAtual;
            subtitulo.text = $"rodada {mesa.Rodada} · " + (cena == null ? "sem cena" : $"cena: {cena.Nome} · {herois.Count} em cena") +
                             (assistindo.Count > 0 ? $" · {assistindo.Count} assistindo" : string.Empty) +
                             (foraDaMesa > 0 ? $" · {foraDaMesa} fora (não estão em carreira)" : string.Empty);

            // O lado direito é do log (desenho do Alan). As heroínas sentam em cima, embaixo e à esquerda.
            const float Alto = 236, Lado = 318, Log = 340;
            var cima = Aresta("Cima", new Vector2(0, 1), new Vector2(1, 1), new Vector2(Lado, -Alto), new Vector2(-Log - 10, 0), true);
            var baixo = Aresta("Baixo", new Vector2(0, 0), new Vector2(1, 0), new Vector2(Lado, 0), new Vector2(-Log - 10, Alto), true);
            // A esquerda (vilões e NPCs) desce até o fim da mesa: o canto de baixo é livre (os assentos de baixo começam
            // depois dela); o de cima é do "Assistindo".
            var esquerda = Aresta("Esquerda", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 10), new Vector2(Lado, -Alto), false);
            // Cima e baixo alternando até 3 em cada (4 → 2/2, 6 → 3/3); depois a esquerda (7 e 8).
            // Mais de 8 não cabe com folga neste tamanho de assento: volta a encher em cima e embaixo.
            var ordem = new[] { cima, baixo, cima, baixo, cima, baixo, esquerda, esquerda };
            for (var k = 0; k < herois.Count; k++)
                Assento(k < ordem.Length ? ordem[k] : (k % 2 == 0 ? cima : baixo), herois[k]);

            var centro = Estilo.Rect("Centro", tabuleiro);
            centro.anchorMin = Vector2.zero;
            centro.anchorMax = Vector2.one;
            // Os vilões em cena sentam à esquerda (o outro lado da mesa), depois das heroínas que estiverem lá.
            // A coluna da esquerda rola quando não cabe (dois vilões e dois aliados já passam da altura).
            var ladoEsquerdo = Estilo.ListaRolavel(esquerda, 8);
            // ListaRolavel devolve o conteúdo; o LayoutElement é da lista (conteúdo → viewport → lista).
            ladoEsquerdo.parent.parent.GetComponent<LayoutElement>().flexibleWidth = 1;
            foreach (var v in mesa.EmCena().Where(n => n.Papel == Papel.Vilao))
                AssentoDeVilao(ladoEsquerdo, v);
            // Os outros NPCs em cena (aliados, neutros) sentam abaixo dos vilões: podem ajudar (p.84: um ponto da pilha por NPC).
            foreach (var n in mesa.EmCena().Where(n => n.Papel != Papel.Vilao))
                AssentoDeNpc(ladoEsquerdo, n);
            // Sem ninguém à esquerda, o centro ocupa esse lado também.
            var esquerdaVazia = herois.Count <= 6 && mesa.EmCena().Count == 0;
            esquerda.gameObject.SetActive(!esquerdaVazia);
            if (esquerdaVazia)
                // Assentos centralizados com o centro; em cima, só se o canto não for de quem assiste.
                foreach (var aresta in assistindo.Count > 0 ? new[] { baixo } : new[] { cima, baixo })
                    aresta.offsetMin = new Vector2(10, aresta.offsetMin.y);
            if (assistindo.Count > 0)
                CaixaAssistindo(assistindo, Lado, Alto);
            centro.offsetMin = new Vector2(esquerdaVazia ? 10 : Lado + 10, Alto + 10);
            centro.offsetMax = new Vector2(-Log - 20, -Alto - 10);
            Centro(centro);

            var log = Estilo.Rect("Log", tabuleiro);
            log.anchorMin = new Vector2(1, 0);
            log.anchorMax = new Vector2(1, 1);
            log.offsetMin = new Vector2(-Log, 10);
            log.offsetMax = new Vector2(-10, -10);
            LogDaMesa(log);
            ajustarTextos = true;
        }

        RectTransform Aresta(string nome, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax, bool horizontal)
        {
            var r = Estilo.Rect(nome, tabuleiro);
            r.anchorMin = min;
            r.anchorMax = max;
            r.offsetMin = offMin;
            r.offsetMax = offMax;
            HorizontalOrVerticalLayoutGroup g = horizontal
                ? r.gameObject.AddComponent<HorizontalLayoutGroup>()
                : r.gameObject.AddComponent<VerticalLayoutGroup>();
            g.padding = new RectOffset(6, 6, 10, 10);
            g.spacing = 8;
            g.childAlignment = TextAnchor.MiddleCenter;
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = g.childForceExpandHeight = false;
            return r;
        }

        /// <summary>O assento: nome e potencial, os cinco Rótulos e as cinco condições (clicar marca/desmarca).</summary>
        void Assento(Transform aresta, Personagem p)
        {
            // O assento é o cartão (300) e, à direita, as moedas de Influência que ela guarda (36).
            var raiz = Estilo.Rect("Assento " + p.NomeExibicao, aresta);
            var lr = raiz.gameObject.AddComponent<LayoutElement>();
            lr.minWidth = lr.preferredWidth = 342;
            lr.minHeight = lr.preferredHeight = 212;
            lr.flexibleWidth = lr.flexibleHeight = 0;
            lr.layoutPriority = 2;
            var hr = raiz.gameObject.AddComponent<HorizontalLayoutGroup>();
            hr.spacing = 6;
            hr.childControlWidth = hr.childControlHeight = true;
            hr.childForceExpandWidth = false;
            hr.childForceExpandHeight = true;

            var c = Estilo.Rect("Cartao", raiz);
            // Tamanho fixo: sem prioridade 2 o assento herda o "esticável" dos rótulos de dentro e um assento
            // sozinho numa aresta se espalha por ela inteira.
            var le = c.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = 300;
            le.minHeight = le.preferredHeight = 212;
            le.flexibleWidth = le.flexibleHeight = 0;
            le.layoutPriority = 2;
            MoedasDeInfluencia(raiz, p);
            Estilo.Moldura(c.gameObject, Estilo.Cartao, Estilo.Dourado);
            var v = c.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(12, 12, 8, 8);
            v.spacing = 6;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandHeight = false;

            // Retrato e nome abrem a ficha (consulta). O potencial só se lê aqui: dar potencial pede o que
            // aconteceu, e fica na Área do mestre (aba Potencial).
            var topo = Estilo.Fila(c, 52, 8);
            var abrir = Estilo.Rect("AbrirFicha", topo);
            Estilo.ColunaElastica(abrir);
            var h = abrir.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            Retrato(abrir, p);
            var nome = Estilo.Rotulo(abrir, p.NomeExibicao, 22, Estilo.Dourado, FontStyles.Bold);
            nome.textWrappingMode = TextWrappingModes.NoWrap;
            nome.overflowMode = TextOverflowModes.Ellipsis;
            Estilo.ColunaElastica(nome);
            var fundoAbrir = abrir.gameObject.AddComponent<Image>();
            fundoAbrir.color = new Color(1, 1, 1, 0);
            var botaoAbrir = abrir.gameObject.AddComponent<Button>();
            botaoAbrir.targetGraphic = fundoAbrir;
            botaoAbrir.name = TextoLivre;
            botaoAbrir.onClick.AddListener(() => AbrirFicha(p));
            abrir.gameObject.AddComponent<Dica>().Configurar("Abrir a ficha de " + p.NomeExibicao + ".");
            var pot = Estilo.Rotulo(topo, $"<size=70%>POT</size>\n{p.Potencial}", 20, Estilo.Dourado, FontStyles.Bold);
            pot.alignment = TextAlignmentOptions.Center;
            var lp = pot.gameObject.AddComponent<LayoutElement>();
            lp.minWidth = lp.preferredWidth = 46;
            lp.flexibleWidth = 0;
            lp.layoutPriority = 2;
            pot.raycastTarget = true;
            pot.gameObject.AddComponent<Dica>().Configurar("Potencial (dar potencial: Área do mestre → Potencial).");

            var rotulos = personagens.RotulosAtuais(p);
            var filaR = Estilo.Fila(c, 40, 4);
            for (var k = 0; k < Rotulos.Todos.Count; k++)
            {
                var id = Rotulos.Todos[k];
                var valor = rotulos[id];
                var t = Estilo.Rotulo(filaR, $"<size=70%>{AbrevRotulo[k]}</size>\n{(valor > 0 ? "+" : string.Empty)}{valor}", 20,
                    Estilo.Texto, FontStyles.Bold);
                t.alignment = TextAlignmentOptions.Center;
                t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                t.raycastTarget = true;
                t.gameObject.AddComponent<Dica>().Configurar(Rotulos.Nome(id));
            }

            var filaC = Estilo.Fila(c, 40, 4);
            for (var k = 0; k < Condicoes.Todas.Count; k++)
            {
                var id = Condicoes.Todas[k];
                var marcada = p.CondicoesMarcadas.Contains(id);
                var b = marcada
                    ? Estilo.BotaoPrincipal(filaC, Abrev[k], 0, () => Aplicar(mesa.AlternarCondicao(p.Id, id, null)))
                    : Estilo.BotaoContorno(filaC, Abrev[k], 0, Estilo.TextoSuave, () => Aplicar(mesa.AlternarCondicao(p.Id, id, null)));
                b.GetComponent<LayoutElement>().flexibleWidth = 1;
                var r = b.GetComponentInChildren<TextMeshProUGUI>();
                r.fontSize = 16;
                r.characterSpacing = 0;
                b.gameObject.AddComponent<Dica>().Configurar(DicaDaCondicao(id, marcada));
            }

            // Os bônus dela, como tokens: o mouse mostra o contexto; clicar usa (ou acaba).
            var filaB = Estilo.Fila(c, 30, 6);
            filaB.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            foreach (var bonus in mesa.BonusDe(p.Id))
                TokenDeBonus(filaB, bonus);
            var dar = Estilo.BotaoContorno(filaB, "+", 30, Estilo.TextoSuave, () => AbrirBonus(p.Id));
            dar.gameObject.AddComponent<Dica>().Configurar("Dar um bônus (adiante, constante, controle).");
            var mover = Estilo.BotaoContorno(filaB, "M", 30, Estilo.TextoSuave, () => AbrirRolagem(p.Id));
            mover.gameObject.AddComponent<Dica>().Configurar("Rolar movimento já com " + p.NomeExibicao + " (e o vilão, se houver um só em cena).");
            Estilo.Rect("Vao", filaB).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var sair = Estilo.BotaoContorno(filaB, "X", 30, Estilo.TextoSuave, () => AbrirSairDeCena(p));
            sair.gameObject.AddComponent<Dica>().Configurar("Tirar " + p.NomeExibicao + " da cena: passa a assistir (fugiu, afastou-se, nocauteada, outra cena).");
        }

        // ---------- a cena: quem está nela e quem assiste (desenho do Alan) ----------

        /// <summary>
        /// Quem assiste, no canto de cima à esquerda: nome, por que está fora e o "Entrar" (com cena aberta). A lista
        /// rola quando são muitas.
        /// </summary>
        void CaixaAssistindo(List<Personagem> assistindo, float lado, float alto)
        {
            var caixa = Estilo.Rect("Assistindo", tabuleiro);
            caixa.anchorMin = caixa.anchorMax = new Vector2(0, 1);
            caixa.pivot = new Vector2(0, 1);
            caixa.anchoredPosition = new Vector2(10, -10);
            caixa.sizeDelta = new Vector2(lado - 20, alto - 20);
            Estilo.Moldura(caixa.gameObject, Estilo.Campo, Estilo.BordaApagada);
            var v = caixa.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(10, 10, 8, 8);
            v.spacing = 6;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandHeight = false;
            var t = Estilo.Rotulo(caixa, $"ASSISTINDO · {assistindo.Count}", 17, Estilo.TextoSuave, FontStyles.Bold);
            t.characterSpacing = 3;
            t.gameObject.AddComponent<LayoutElement>().minHeight = 24;
            var lista = Estilo.ListaRolavel(caixa, 4);
            var suave = ColorUtility.ToHtmlStringRGB(Estilo.TextoApagado);
            foreach (var h in assistindo)
            {
                var heroi = h;
                var fila = Estilo.Fila(lista, 40, 6);
                var motivo = mesa.MotivoFora(h.Id);
                var nome = Estilo.Rotulo(fila, $"<b>{h.NomeExibicao}</b>" + (motivo == null ? "" : $"\n<size=75%><color=#{suave}>{motivo}</color></size>"),
                    16, Estilo.Texto);
                nome.textWrappingMode = TextWrappingModes.NoWrap;
                nome.overflowMode = TextOverflowModes.Ellipsis;
                Estilo.ColunaElastica(nome);
                if (mesa.TemCena)
                {
                    var entrar = Estilo.BotaoContorno(fila, "Entrar", 78, Estilo.Dourado, () => AbrirEntrarEmCena(heroi));
                    entrar.gameObject.AddComponent<Dica>().Configurar("Pôr " + h.NomeExibicao + " na cena (pela narração, ou gastando um Controle).");
                }
            }
        }

        string cenaNome = string.Empty;
        string cenaLocal;
        readonly HashSet<string> cenaQuem = new HashSet<string>();
        const string LocalNenhum = "_nenhum";

        /// <summary>
        /// Criar cena: nome, local (opcional, do Cenário) e quem está nela. Já vem marcado quem está na cena aberta; na
        /// primeira, todas (até caberem).
        /// </summary>
        void AbrirCriarCena()
        {
            cenaNome = string.Empty;
            cenaLocal = null;
            cenaQuem.Clear();
            var sugestao = mesa.TemCena ? NaMesa() : DaSala();
            foreach (var h in sugestao.Take(Mesa.MaximoEmCena))
                cenaQuem.Add(h.Id);
            DesenharCriarCena();
        }

        void DesenharCriarCena()
        {
            var herois = DaSala();
            var caixa = Caixa(mesa.TemCena ? "NOVA CENA" : "CRIAR CENA", 360 + (herois.Count + 2) / 3 * 58, 1000);
            Pergunta(caixa, "Nome da cena");
            var campo = CampoMotivo(caixa, "ex.: Parque Arcaico, a nave pousou");
            campo.text = cenaNome;
            campo.onValueChanged.AddListener(t => cenaNome = t);
            Pergunta(caixa, "Local (opcional, do Cenário)");
            var locais = new List<(string, string)> { (LocalNenhum, "—") };
            locais.AddRange(personagens.CenarioDaSala(sala.Id).Where(e => e.Tipo == ElementoDeCenario.Local).Select(e => (e.Id, e.Nome)));
            var filaL = Estilo.Fila(caixa, 52, 8);
            Estilo.ColunaElastica(Estilo.ListaSuspensa(filaL, 420, locais, cenaLocal ?? LocalNenhum, id =>
            {
                cenaLocal = id == LocalNenhum ? null : id;
                DesenharCriarCena();
            }));
            Pergunta(caixa, $"Quem está em cena? ({cenaQuem.Count} de até {Mesa.MaximoEmCena}; as outras assistem)");
            for (var k = 0; k < herois.Count; k += 3)
            {
                var fila = Estilo.Fila(caixa, 50, 8);
                foreach (var h in herois.Skip(k).Take(3))
                {
                    var id = h.Id;
                    UnityEngine.Events.UnityAction alternar = () =>
                    {
                        if (!cenaQuem.Remove(id))
                        {
                            if (cenaQuem.Count >= Mesa.MaximoEmCena)
                            {
                                Mostrar(Resultado.Falha($"Cabem {Mesa.MaximoEmCena} numa cena: desmarque alguém antes."));
                                return;
                            }
                            cenaQuem.Add(id);
                        }
                        DesenharCriarCena();
                    };
                    var b = cenaQuem.Contains(id)
                        ? Estilo.BotaoPrincipal(fila, h.NomeExibicao, 0, alternar)
                        : Estilo.BotaoContorno(fila, h.NomeExibicao, 0, Estilo.Dourado, alternar);
                    b.GetComponent<LayoutElement>().flexibleWidth = 1;
                }
                for (var vazio = herois.Skip(k).Take(3).Count(); vazio < 3; vazio++)
                    Estilo.Rect("Vao", fila).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            }
            Nota(caixa, "A pilha e as condições continuam (a pilha só volta para 1 quando o tempo passa). Quem assiste entra depois pelo \"Entrar\".");
            Rodape(caixa, mesa.TemCena ? "Abrir cena" : "Criar cena", () =>
            {
                var r = mesa.CriarCena(cenaNome, cenaLocal, cenaQuem.ToList(), null);
                if (!r.Ok)
                {
                    Mostrar(r);
                    return;
                }
                FecharBatalha();
                Aplicar(r);
            });
        }

        string gestaoNome = string.Empty, gestaoMotivo = string.Empty, gestaoLocal;

        /// <summary>
        /// A gestão da cena aberta (clicar no nome dela): nome e local; as heroínas, pôr ou tirar à vontade (entra no log,
        /// com o motivo); os vilões e oponentes da campanha, pôr ou tirar da mesa (como na Área do mestre).
        /// </summary>
        void AbrirGestaoDaCena()
        {
            var cena = mesa.CenaAtual;
            if (cena == null)
                return;
            gestaoNome = cena.Nome;
            gestaoLocal = cena.Local;
            gestaoMotivo = string.Empty;
            DesenharGestaoDaCena();
        }

        void DesenharGestaoDaCena()
        {
            var cena = mesa.CenaAtual;
            if (cena == null)
            {
                FecharBatalha();
                return;
            }
            var caixa = Caixa("GESTÃO DA CENA", 900, 1100);

            // Cenas paralelas: ir para outra (quem está em cada uma fica onde está; o log da mesa acompanha a cena).
            if (mesa.Cenas.Count > 1)
            {
                var filaIr = Estilo.Fila(caixa, 52, 10);
                Estilo.Rotulo(filaIr, "IR PARA A CENA", 18, Estilo.TextoSuave, FontStyles.Bold).gameObject.AddComponent<LayoutElement>().preferredWidth = 190;
                var cenas = mesa.Cenas.Reverse().Select(c => (c.Id, $"{c.Nome}  ·  {c.Herois.Count} heroína{(c.Herois.Count == 1 ? "" : "s")}" +
                                                                    (c.Id == cena.Id ? "  (aberta)" : ""))).ToList();
                Estilo.ColunaElastica(Estilo.ListaSuspensa(filaIr, 520, cenas, cena.Id, id =>
                {
                    Mostrar(mesa.IrParaCena(id));
                    Redesenhar();
                    AbrirGestaoDaCena();
                }));
            }

            // Nome e local.
            var filaN = Estilo.Fila(caixa, 52, 10);
            var nome = Estilo.CampoTexto(filaN, "nome da cena");
            nome.text = gestaoNome;
            nome.onValueChanged.AddListener(t => gestaoNome = t);
            Estilo.ColunaElastica(nome);
            var locais = new List<(string, string)> { (LocalNenhum, "sem local") };
            locais.AddRange(personagens.CenarioDaSala(sala.Id).Where(e => e.Tipo == ElementoDeCenario.Local).Select(e => (e.Id, e.Nome)));
            Estilo.ListaSuspensa(filaN, 300, locais, gestaoLocal ?? LocalNenhum, id =>
            {
                gestaoLocal = id == LocalNenhum ? null : id;
                DesenharGestaoDaCena();
            });
            Estilo.BotaoContorno(filaN, "Salvar", 140, Estilo.Dourado, () =>
            {
                var r = mesa.EditarCena(gestaoNome, gestaoLocal);
                Mostrar(r.Ok ? (Resultado?)null : r, r.Ok ? "Cena atualizada." : null);
                Redesenhar();
                DesenharGestaoDaCena();
            });
            var motivo = CampoMotivo(caixa, "motivo de quem entra ou sai (opcional, vai para o log)");
            motivo.text = gestaoMotivo;
            motivo.onValueChanged.AddListener(t => gestaoMotivo = t);

            var lista = Estilo.ListaRolavel(caixa, 6);
            var suave = ColorUtility.ToHtmlStringRGB(Estilo.TextoApagado);

            // Heroínas: em cena ou assistindo.
            var herois = DaSala();
            var emCena = herois.Count(h => mesa.HeroiEmCena(h.Id));
            var th = Estilo.Rotulo(lista, $"HEROÍNAS · {emCena} em cena (até {Mesa.MaximoEmCena}) · {herois.Count - emCena} assistindo", 18,
                Estilo.Azul, FontStyles.Bold);
            th.gameObject.AddComponent<LayoutElement>().minHeight = 30;
            foreach (var h in herois)
            {
                var heroi = h;
                var dentro = mesa.HeroiEmCena(h.Id);
                var fila = Estilo.Fila(lista, 48, 10);
                Estilo.Moldura(fila.gameObject, Estilo.Linha, dentro ? Estilo.Dourado : Estilo.BordaApagada);
                fila.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(12, 8, 4, 4);
                var estado = dentro ? "em cena" : mesa.MotivoFora(h.Id) ?? "assistindo";
                var r = Estilo.Rotulo(fila, $"<b>{h.NomeExibicao}</b>   <size=80%><color=#{suave}>{estado}</color></size>", 19,
                    dentro ? Estilo.Texto : Estilo.TextoSuave);
                r.textWrappingMode = TextWrappingModes.NoWrap;
                r.overflowMode = TextOverflowModes.Ellipsis;
                Estilo.ColunaElastica(r);
                UnityEngine.Events.UnityAction alternar = () =>
                {
                    var res = dentro ? mesa.SairDeCena(heroi.Id, gestaoMotivo) : mesa.EntrarEmCena(heroi.Id, gestaoMotivo);
                    Mostrar(res.Ok ? (Resultado?)null : res);
                    Redesenhar();
                    DesenharGestaoDaCena();
                };
                if (dentro)
                    Estilo.BotaoPrincipal(fila, "Tirar", 150, alternar);
                else
                    Estilo.BotaoContorno(fila, "Pôr", 150, Estilo.Dourado, alternar);
            }

            // Vilões e oponentes da campanha (o mesmo da Área do mestre); entrar e sair vai para o log, com o motivo.
            var viloes = personagens.NpcsDaSala(sala.Id).Where(n => n.Ativo && n.Papel == Papel.Vilao).ToList();
            var tv = Estilo.Rotulo(lista, $"VILÕES E OPONENTES · {mesa.EmCena().Count} em cena", 18, Estilo.Erro, FontStyles.Bold);
            tv.gameObject.AddComponent<LayoutElement>().minHeight = 34;
            if (viloes.Count == 0)
                Estilo.Rotulo(lista, "Nenhum NPC com papel Vilão / Oponente nesta campanha (aba NPCs da sala).", 17,
                    Estilo.TextoApagado, FontStyles.Italic).textWrappingMode = TextWrappingModes.Normal;
            foreach (var v in viloes)
            {
                var id = v.Id;
                var dentro = mesa.EstaEmCena(v.Id);
                var fila = Estilo.Fila(lista, 48, 10);
                Estilo.Moldura(fila.gameObject, Estilo.Linha, dentro ? Estilo.Erro : Estilo.BordaApagada);
                fila.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(12, 8, 4, 4);
                var marcadas = mesa.CondicoesMarcadasDoVilao(v.Id).Count;
                var total = v.Vilao?.Condicoes.Count ?? 0;
                var r = Estilo.Rotulo(fila, $"<b>{v.NomeExibicao}</b>   <size=80%><color=#{suave}>{(dentro ? "em cena" : "fora")} · {marcadas} de {total} condições</color></size>",
                    19, dentro ? Estilo.Texto : Estilo.TextoSuave);
                r.textWrappingMode = TextWrappingModes.NoWrap;
                r.overflowMode = TextOverflowModes.Ellipsis;
                Estilo.ColunaElastica(r);
                UnityEngine.Events.UnityAction alternar = () =>
                {
                    Mostrar(mesa.AlternarEmCena(id, gestaoMotivo));
                    Redesenhar();
                    DesenharGestaoDaCena();
                };
                if (dentro)
                    Estilo.BotaoPrincipal(fila, "Tirar", 150, alternar);
                else
                    Estilo.BotaoContorno(fila, "Pôr", 150, Estilo.Dourado, alternar);
            }

            // Os outros NPCs da campanha (aliados, neutros): também podem estar em cena e ajudar com a pilha (p.84).
            var outros = personagens.NpcsDaSala(sala.Id).Where(n => n.Ativo && n.Papel != Papel.Vilao).ToList();
            var tn = Estilo.Rotulo(lista, $"OUTROS NPCs · {outros.Count(n => mesa.EstaEmCena(n.Id))} em cena", 18, Estilo.Azul, FontStyles.Bold);
            tn.gameObject.AddComponent<LayoutElement>().minHeight = 34;
            if (outros.Count == 0)
                Estilo.Rotulo(lista, "Nenhum outro NPC nesta campanha.", 17, Estilo.TextoApagado, FontStyles.Italic);
            foreach (var n in outros)
            {
                var id = n.Id;
                var dentro = mesa.EstaEmCena(n.Id);
                var fila = Estilo.Fila(lista, 48, 10);
                Estilo.Moldura(fila.gameObject, Estilo.Linha, dentro ? Estilo.Azul : Estilo.BordaApagada);
                fila.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(12, 8, 4, 4);
                var papel = n.Papel == Papel.Aliado ? "aliado" : n.Papel == Papel.Neutro ? "neutro" : n.Papel.ToString().ToLowerInvariant();
                var r = Estilo.Rotulo(fila, $"<b>{n.NomeExibicao}</b>   <size=80%><color=#{suave}>{papel} · {(dentro ? "em cena" : "fora")}</color></size>",
                    19, dentro ? Estilo.Texto : Estilo.TextoSuave);
                r.textWrappingMode = TextWrappingModes.NoWrap;
                r.overflowMode = TextOverflowModes.Ellipsis;
                Estilo.ColunaElastica(r);
                UnityEngine.Events.UnityAction alternar = () =>
                {
                    Mostrar(mesa.AlternarEmCena(id, gestaoMotivo));
                    Redesenhar();
                    DesenharGestaoDaCena();
                };
                if (dentro)
                    Estilo.BotaoPrincipal(fila, "Tirar", 150, alternar);
                else
                    Estilo.BotaoContorno(fila, "Pôr", 150, Estilo.Dourado, alternar);
            }

            var fim = Estilo.Fila(caixa, 56, 12);
            Estilo.BotaoContorno(fim, "Nova cena", 220, Estilo.Dourado, AbrirCriarCena);
            Estilo.Rect("Vao", fim).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Estilo.BotaoContorno(fim, "Fechar", 200, Estilo.TextoSuave, FecharBatalha);
        }

        Core.Regras.MovimentosDoMj movimentosDoMj;
        string mjMovimento, mjPeso, mjAlvo, mjCondicao, mjTexto = string.Empty, mjPendencia;
        PainelAjuda ajudaDoMj;
        const string MjNinguem = "_ninguem";

        /// <summary>
        /// O deck do MJ: a mão com os movimentos do livro (p.177-184), e à direita o escolhido. O peso (leve ou pesado) é
        /// escolhido na hora, com a tendência do livro pré-marcada quando há; contra quem (em cena, ou ninguém); o que
        /// acontece. "Infligir uma condição" marca a condição junto. Com <paramref name="pendencia"/>, resolve a falha.
        /// </summary>
        void AbrirMovimentoDoMj(string pendencia, string alvoPadrao)
        {
            movimentosDoMj ??= Core.Regras.MovimentosDoMj.Carregar(new FonteDeRegrasResources());
            mjMovimento = mjPeso = mjCondicao = null;
            mjTexto = string.Empty;
            mjPendencia = pendencia;
            mjAlvo = alvoPadrao != null && (mesa.HeroiEmCena(alvoPadrao) || mesa.EstaEmCena(alvoPadrao)) ? alvoPadrao : null;
            DesenharMovimentoDoMj();
        }

        void DesenharMovimentoDoMj()
        {
            var escolhido = mjMovimento == null ? null : movimentosDoMj.Buscar(mjMovimento);
            var caixa = Caixa("MOVIMENTO DO MJ", 900, 1400);
            var (leves, pesados) = mesa.PesosNoLog();
            var pediu = mjPendencia == null ? null : mesa.Eventos.FirstOrDefault(x => x.Id == mjPendencia);
            var quem = pediu?.Personagem == null ? null : personagens.Buscar(pediu.Personagem)?.NomeExibicao;
            Nota(caixa, (pediu != null ? $"Pedido pela falha{(quem != null ? " de " + quem : "")} (r{pediu.Rodada}). " : "") +
                        $"No log: leves {leves} · pesados {pesados}. O peso é seu: a mesma carta pode chegar leve ou pesada.");

            var corpo = Estilo.Fila(caixa, 700, 18);
            corpo.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
            // Esquerda: a mão.
            var esquerda = Coluna(corpo, 0, false, 6);
            esquerda.GetComponent<LayoutElement>().flexibleWidth = 1;
            Estilo.Rotulo(esquerda, "A MÃO DO MJ", 20, Estilo.Erro, FontStyles.Bold).characterSpacing = 3;
            var mao = Estilo.ListaRolavel(esquerda, 6);
            foreach (var m in movimentosDoMj.Todos)
            {
                var mov = m;
                var fila = Estilo.Fila(mao, 46, 6);
                UnityEngine.Events.UnityAction escolher = () =>
                {
                    if (mjMovimento != mov.Id)
                    {
                        mjMovimento = mov.Id;
                        mjPeso = mov.Tendencia; // a tendência do livro, quando há; o MJ troca
                        mjCondicao = null;
                    }
                    DesenharMovimentoDoMj();
                };
                var b = mjMovimento == mov.Id
                    ? Estilo.BotaoPrincipal(fila, mov.Nome, 0, escolher)
                    : Estilo.BotaoContorno(fila, mov.Nome, 0, Estilo.Dourado, escolher);
                b.GetComponent<LayoutElement>().flexibleWidth = 1;
                b.name = TextoLivre;
                TextoDeOpcao(b, 17);
                var ver = Estilo.BotaoContorno(fila, "?", 46, Estilo.TextoSuave, () =>
                {
                    if (ajudaDoMj == null)
                        ajudaDoMj = PainelAjuda.Criar(transform.parent);
                    ajudaDoMj.AbrirCategoria("movimentos-mj", mov.Ajuda);
                });
                ver.gameObject.AddComponent<Dica>().Configurar("Ver a carta no help.");
            }

            // Direita: o movimento escolhido.
            var direita = Coluna(corpo, 20, true, 10);
            direita.GetComponent<LayoutElement>().preferredWidth = 560;
            if (escolhido == null)
            {
                Estilo.Rotulo(direita, "Escolha um movimento na mão.", 20, Estilo.TextoApagado, FontStyles.Italic);
                Estilo.Rect("Vao", direita).gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            }
            else
            {
                var titulo = Estilo.Rotulo(direita, escolhido.Nome, 22, Estilo.Texto, FontStyles.Bold);
                titulo.textWrappingMode = TextWrappingModes.Normal;
                if (!string.IsNullOrWhiteSpace(escolhido.Nota))
                    Estilo.Rotulo(direita, escolhido.Nota, 15, Estilo.TextoSuave, FontStyles.Italic).textWrappingMode = TextWrappingModes.Normal;

                Pergunta(direita, escolhido.Tendencia != null ? $"Peso (o livro tende a {escolhido.Tendencia})" : "Peso");
                var filaP = Estilo.Fila(direita, 46, 8);
                Escolha(filaP, "Leve", mjPeso == Core.Regras.MovimentosDoMj.Leve, () => { mjPeso = Core.Regras.MovimentosDoMj.Leve; DesenharMovimentoDoMj(); })
                    .gameObject.AddComponent<Dica>().Configurar("Prepara, anuncia, ameaça: os heróis ainda podem reagir.");
                Escolha(filaP, "Pesado", mjPeso == Core.Regras.MovimentosDoMj.Pesado, () => { mjPeso = Core.Regras.MovimentosDoMj.Pesado; DesenharMovimentoDoMj(); })
                    .gameObject.AddComponent<Dica>().Configurar("Acontece agora e muda a cena; não dá para interceptar.");

                Pergunta(direita, "Contra quem?");
                var alvos = new List<(string, string)> { (MjNinguem, "ninguém em particular") };
                alvos.AddRange(NaMesa().Concat(mesa.EmCena()).Select(a => (a.Id, a.NomeExibicao)));
                var filaA = Estilo.Fila(direita, 52, 8);
                Estilo.ColunaElastica(Estilo.ListaSuspensa(filaA, 420, alvos, mjAlvo ?? MjNinguem, id =>
                {
                    mjAlvo = id == MjNinguem ? null : id;
                    mjCondicao = null;
                    DesenharMovimentoDoMj();
                }));

                var alvo = mjAlvo == null ? null : personagens.Buscar(mjAlvo);
                if (escolhido.MarcaCondicao)
                {
                    if (alvo == null || alvo.Controle == Controle.Mestre)
                        Estilo.Rotulo(direita, "Escolha uma heroína em cena.", 16, Estilo.Dourado, FontStyles.Italic);
                    else
                    {
                        Pergunta(direita, "Qual condição? (só se a ficção apontar uma; senão, quem recebe escolhe)");
                        var filaC = Estilo.Fila(direita, 40, 6);
                        Escolha(filaC, "à escolha", mjCondicao == null, () => { mjCondicao = null; DesenharMovimentoDoMj(); });
                        foreach (var cond in Condicoes.Todas.Where(c => !alvo.CondicoesMarcadas.Contains(c)))
                        {
                            var id = cond;
                            Escolha(filaC, Condicoes.Nome(cond), mjCondicao == id, () => { mjCondicao = id; DesenharMovimentoDoMj(); });
                        }
                    }
                }
                if (escolhido.Efeito == Core.Regras.MovimentosDoMj.EfeitoMovimentoDeVilao)
                {
                    var viloes = mesa.EmCena().Where(v => v.Papel == Papel.Vilao).ToList();
                    if (viloes.Count == 0)
                        Estilo.Rotulo(direita, "Nenhum vilão em cena.", 16, Estilo.TextoApagado, FontStyles.Italic);
                    foreach (var v in viloes)
                    {
                        var vilao = v;
                        Estilo.BotaoContorno(direita, $"Movimentos de {v.NomeExibicao}", 0, Estilo.Erro, () => AbrirMovimentos(vilao))
                            .GetComponent<LayoutElement>().preferredHeight = 40;
                    }
                }

                Pergunta(direita, "O que acontece? (vai para o log)");
                var campo = Estilo.AreaTexto(direita, "ex.: eram seus irmãos de laboratório", 90);
                campo.text = mjTexto;
                campo.onValueChanged.AddListener(t => mjTexto = t);
                Estilo.Rect("Vao", direita).gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            }

            var fim = Estilo.Fila(caixa, 56, 12);
            Estilo.Rect("Vao", fim).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Estilo.BotaoContorno(fim, "Cancelar", 200, Estilo.TextoSuave, FecharBatalha);
            Estilo.BotaoPrincipal(fim, "Fazer movimento", 280, () =>
            {
                if (escolhido == null)
                {
                    Mostrar(Resultado.Falha("Escolha um movimento na mão."));
                    return;
                }
                var r = mesa.FazerMovimentoDoMj(escolhido, mjPeso, mjAlvo, mjCondicao, mjTexto, mjPendencia);
                if (!r.Ok)
                {
                    Mostrar(r);
                    return;
                }
                FecharBatalha();
                Aplicar(r);
            });
        }

        string entrarControle;

        /// <summary>Entrar em cena: pela narração, ou gastando um Controle do assento dela (ex.: Chegar na hora certa).</summary>
        void AbrirEntrarEmCena(Personagem h)
        {
            entrarControle = null;
            DesenharEntrarEmCena(h, string.Empty);
        }

        void DesenharEntrarEmCena(Personagem h, string motivo)
        {
            var controles = mesa.BonusDe(h.Id).Where(b => b.Tipo == Mesa.BonusControle).ToList();
            var caixa = Caixa($"ENTRAR EM CENA · {h.NomeExibicao.ToUpperInvariant()}", 430 + controles.Count * 56);
            Pergunta(caixa, "Como ela chega?");
            TMP_InputField campo = null;
            var filaN = Estilo.Fila(caixa, 48, 8);
            Escolha(filaN, "pela narração", entrarControle == null, () => { entrarControle = null; DesenharEntrarEmCena(h, campo.text); });
            foreach (var b in controles)
            {
                var id = b.Id;
                var fila = Estilo.Fila(caixa, 48, 8);
                Escolha(fila, $"gastando Controle {b.Valor}" + (b.Contexto.Length > 0 ? $": {b.Contexto}" : ""), entrarControle == id,
                    () => { entrarControle = id; DesenharEntrarEmCena(h, campo.text); });
            }
            campo = CampoMotivo(caixa, "motivo (opcional; ex.: ouviu o pedido de socorro pelo rádio)");
            campo.text = motivo;
            Rodape(caixa, "Entrar", () =>
            {
                var r = mesa.EntrarEmCena(h.Id, campo.text, entrarControle);
                if (!r.Ok)
                {
                    Mostrar(r);
                    return;
                }
                FecharBatalha();
                Aplicar(r);
            });
        }

        /// <summary>Sair de cena (à vontade do mestre): ela passa a assistir, com o motivo no log.</summary>
        void AbrirSairDeCena(Personagem p)
        {
            var caixa = Caixa($"SAIR DE CENA · {p.NomeExibicao.ToUpperInvariant()}", 430);
            Nota(caixa, "Ela passa a assistir (o canto de cima à esquerda) e volta pelo \"Entrar\". Desfazer pelo log também devolve.");
            TMP_InputField campo = null;
            var fila = Estilo.Fila(caixa, 46, 8);
            foreach (var motivo in new[] { "fugiu", "afastou-se (golpe poderoso)", "nocauteada", "foi para outra cena" })
            {
                var texto = motivo;
                var b = Estilo.BotaoContorno(fila, texto, 0, Estilo.Dourado, () => campo.text = texto);
                b.GetComponent<LayoutElement>().flexibleWidth = 1;
            }
            campo = CampoMotivo(caixa, "motivo (clique num acima ou escreva)");
            Rodape(caixa, "Sair de cena", () =>
            {
                var r = mesa.SairDeCena(p.Id, campo.text);
                if (!r.Ok)
                {
                    Mostrar(r);
                    return;
                }
                FecharBatalha();
                Aplicar(r);
            });
        }

        /// <summary>
        /// As moedas que ela guarda: uma por colega da mesa sobre quem ela tem Influência (quem recebe a
        /// Influência tem o poder), com a inicial dele. Mútua (as duas têm) usa a moeda de mão dupla. Só PJ↔PJ:
        /// NPCs (e o padrão dos adultos) ficam fora da mesa.
        /// </summary>
        void MoedasDeInfluencia(Transform raiz, Personagem p)
        {
            var coluna = Estilo.Rect("Influencia", raiz);
            var lc = coluna.gameObject.AddComponent<LayoutElement>();
            lc.minWidth = lc.preferredWidth = 36;
            lc.flexibleWidth = 0;
            var v = coluna.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4;
            v.padding = new RectOffset(0, 0, 4, 4);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandHeight = false;
            var moeda = Resources.Load<Texture2D>("Icones/Tokens/influencia");
            var mutua = Resources.Load<Texture2D>("Icones/Tokens/influencia_mutua");
            foreach (var colega in NaMesa().Where(o => o != p && personagens.TemInfluencia(p, o)))
            {
                var dupla = personagens.TemInfluencia(colega, p);
                var r = Estilo.Rect("Moeda", coluna);
                var le = r.gameObject.AddComponent<LayoutElement>();
                le.minHeight = le.preferredHeight = 34;
                // O retrato de quem ela influencia no miolo transparente da moeda (o quadrado inscrito no círculo,
                // ~70% do diâmetro, fica escondido pelo aro); sem foto nem arte, a inicial.
                // Janela de 22×22 com máscara; a foto preenche a janela (o ajuste de proporção é relativo a ela).
                var janela = Estilo.Rect("Janela", r);
                janela.anchorMin = janela.anchorMax = new Vector2(0.5f, 0.5f);
                janela.sizeDelta = new Vector2(22, 22);
                janela.gameObject.AddComponent<RectMask2D>();
                var miolo = Estilo.Rect("Retrato", janela);
                Estilo.Esticar(miolo);
                var foto = miolo.gameObject.AddComponent<RawImage>();
                foto.raycastTarget = false;
                var ajuste = miolo.gameObject.AddComponent<AspectRatioFitter>();
                ajuste.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                var arte = Retratos.Buscar(personagens.RegraDoArquetipo(colega)?.Retrato);
                var temFoto = !string.IsNullOrWhiteSpace(colega.Identidade.FotoHeroica) &&
                              colega.Identidade.FotoHeroicaAprovacao != AprovacaoFoto.Recusada;
                if (temFoto || arte != null)
                {
                    var aviso = Estilo.Rotulo(r, string.Empty, 8, Estilo.TextoSuave);
                    aviso.gameObject.SetActive(false);
                    r.gameObject.AddComponent<FotoDaWeb>().Mostrar(colega.Identidade.FotoHeroica, foto, ajuste, aviso, temFoto ? null : arte);
                }
                else
                {
                    foto.enabled = false;
                    var inicial = Estilo.Rotulo(miolo, colega.NomeExibicao.Substring(0, 1).ToUpperInvariant(), 14, Color.white, FontStyles.Bold);
                    inicial.alignment = TextAlignmentOptions.Center;
                    Estilo.Esticar(inicial.rectTransform);
                    inicial.raycastTarget = false;
                }
                var aro = Estilo.Rect("Moeda", r).gameObject.AddComponent<RawImage>();
                Estilo.Esticar((RectTransform)aro.transform);
                aro.texture = dupla ? mutua : moeda;
                aro.raycastTarget = true;
                r.gameObject.AddComponent<Dica>().Configurar(
                    $"{p.NomeExibicao} tem Influência sobre {colega.NomeExibicao}" + (dupla ? " (recíproco)." : "."));
            }
        }

        void TokenDeBonus(Transform pai, BonusDeMesa bonus)
        {
            if (bonus.Tipo == Mesa.BonusLembrete)
            {
                EtiquetaDeLembrete(pai, bonus);
                return;
            }
            var r = Estilo.Rect("Bonus", pai);
            var le = r.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = 52;
            le.flexibleWidth = 0;
            var img = Estilo.Rect("Token", r).gameObject.AddComponent<RawImage>();
            img.texture = Resources.Load<Texture2D>("Icones/Tokens/" + bonus.Tipo);
            var it = (RectTransform)img.transform;
            it.anchorMin = new Vector2(0, 0);
            it.anchorMax = new Vector2(0, 1);
            it.pivot = new Vector2(0, 0.5f);
            it.sizeDelta = new Vector2(28, 0);
            var v = Estilo.Rotulo(r, (bonus.Valor > 0 ? "+" : string.Empty) + bonus.Valor, 18, Estilo.Texto, FontStyles.Bold);
            v.alignment = TextAlignmentOptions.MidlineRight;
            Estilo.Esticar(v.rectTransform);
            var botao = r.gameObject.AddComponent<Button>();
            var fundo = r.gameObject.AddComponent<Image>();
            fundo.color = new Color(1, 1, 1, 0);
            botao.targetGraphic = fundo;
            botao.name = TextoLivre;
            var nome = Mesa.NomeDoBonus(bonus.Tipo);
            r.gameObject.AddComponent<Dica>().Configurar($"{nome} {v.text}" +
                (bonus.Contexto.Length > 0 ? $": {bonus.Contexto}" : string.Empty) + "\nClique para usar (ou quando acabar).");
            botao.onClick.AddListener(() =>
                Estilo.Confirmar(transform, $"{nome} {v.text}{(bonus.Contexto.Length > 0 ? $" ({bonus.Contexto})" : string.Empty)}:\n" +
                                            (bonus.Tipo == Mesa.BonusConstante ? "acabou?" : "usar agora?"),
                    bonus.Tipo == Mesa.BonusConstante ? "Acabou" : "Usar",
                    () => Aplicar(mesa.RetirarBonus(bonus.Id, null))));
        }

        /// <summary>O lembrete: etiqueta curta (o texto inteiro no mouse); clicar resolve.</summary>
        void EtiquetaDeLembrete(Transform pai, BonusDeMesa bonus)
        {
            var curto = bonus.Contexto.Length > 16 ? bonus.Contexto.Substring(0, 15) + "…" : bonus.Contexto;
            var b = Estilo.BotaoContorno(pai, curto, 0, Estilo.Azul, () =>
                Estilo.Confirmar(transform, $"Lembrete: {bonus.Contexto}\nResolver (sai da mesa)?", "Resolver",
                    () => Aplicar(mesa.RetirarBonus(bonus.Id, null))));
            b.name = TextoLivre;
            var le = b.GetComponent<LayoutElement>();
            le.preferredWidth = 130;
            le.flexibleWidth = 0;
            var r = b.GetComponentInChildren<TextMeshProUGUI>();
            r.fontSize = 13;
            r.characterSpacing = 0;
            r.fontStyle = FontStyles.Normal;
            r.textWrappingMode = TextWrappingModes.NoWrap;
            r.overflowMode = TextOverflowModes.Ellipsis;
            b.gameObject.AddComponent<Dica>().Configurar("Lembrete: " + bonus.Contexto + "\nClique para resolver.");
        }

        // ---------- dar bônus ----------

        string bonusPara;
        string bonusTipo = Mesa.BonusAdiante;
        int bonusValor = 1;

        /// <summary>Dar um bônus: para quem, qual (adiante, constante, controle), quanto e em que contexto.</summary>
        void AbrirBonus(string para)
        {
            var herois = NaMesa();
            if (herois.Count == 0)
            {
                Mostrar(Resultado.Falha("Ninguém na mesa."));
                return;
            }
            bonusPara = para ?? (herois.Any(h => h.Id == bonusPara) ? bonusPara : herois[0].Id);
            DesenharBonus(string.Empty);
        }

        void DesenharBonus(string contextoAtual)
        {
            var herois = NaMesa();
            var lembrete = bonusTipo == Mesa.BonusLembrete;
            if (lembrete)
                herois = herois.Concat(mesa.EmCena()).ToList(); // lembrete também vai no vilão
            else if (!herois.Any(h => h.Id == bonusPara))
                bonusPara = herois.FirstOrDefault()?.Id;
            var caixa = Caixa(lembrete ? "LEMBRETE" : "DAR BÔNUS", lembrete ? 560 : 640, 1050);
            Pergunta(caixa, "Para quem?");
            var filaP = Estilo.Fila(caixa, 48, 8);
            TMP_InputField contexto = null;
            foreach (var h in herois)
            {
                var id = h.Id;
                var b = id == bonusPara
                    ? Estilo.BotaoPrincipal(filaP, h.NomeExibicao, 0, () => { })
                    : Estilo.BotaoContorno(filaP, h.NomeExibicao, 0, Estilo.Dourado, () => { bonusPara = id; DesenharBonus(contexto.text); });
                b.GetComponent<LayoutElement>().flexibleWidth = 1;
            }
            Pergunta(caixa, "Qual?");
            var filaT = Estilo.Fila(caixa, 48, 8);
            foreach (var (tipo, dica) in new[]
                     {
                         (Mesa.BonusAdiante, "vale na próxima rolagem que bate com o contexto, e some"),
                         (Mesa.BonusConstante, "vale enquanto o contexto durar"),
                         (Mesa.BonusControle, "um recurso para gastar (ex.: movimentos que pedem controle)"),
                         (Mesa.BonusLembrete, "tag temporária, sem valor: para não esquecer (ex.: abertura: Aurora cedeu terreno)"),
                     })
            {
                var t = tipo;
                var b = t == bonusTipo
                    ? Estilo.BotaoPrincipal(filaT, Mesa.NomeDoBonus(t), 0, () => { })
                    : Estilo.BotaoContorno(filaT, Mesa.NomeDoBonus(t), 0, Estilo.Dourado, () => { bonusTipo = t; DesenharBonus(contexto.text); });
                b.GetComponent<LayoutElement>().flexibleWidth = 1;
                b.gameObject.AddComponent<Dica>().Configurar(dica);
            }
            if (!lembrete)
            {
            Pergunta(caixa, "Quanto?");
            var filaV = Estilo.Fila(caixa, 48, 8);
            foreach (var v in new[] { -2, -1, 1, 2, 3 })
            {
                var valor = v;
                var texto = v > 0 ? "+" + v : v.ToString();
                var b = v == bonusValor
                    ? Estilo.BotaoPrincipal(filaV, texto, 90, () => { })
                    : Estilo.BotaoContorno(filaV, texto, 90, Estilo.Dourado, () => { bonusValor = valor; DesenharBonus(contexto.text); });
            }
            }
            Pergunta(caixa, lembrete ? "O que lembrar" : "Gatilho ou contexto (quando vale)");
            contexto = CampoMotivo(caixa, lembrete ? "ex.: abertura: Aurora cedeu terreno"
                : "ex.: agindo pelas respostas da avaliação; contra o Doutor Ocaso");
            contexto.text = contextoAtual;
            Rodape(caixa, lembrete ? "Pôr lembrete" : "Dar bônus", () =>
            {
                var r = mesa.DarBonus(bonusPara, bonusTipo, bonusValor, contexto.text, null);
                if (!r.Ok)
                {
                    Mostrar(r);
                    return;
                }
                FecharBatalha();
                Aplicar(r);
            });
        }

        /// <summary>
        /// O vilão em cena: retrato e nome, as condições que ele tem (clicar marca na luta e abre a carta da
        /// condição, porque o livro manda fazer o movimento dela na hora) e os movimentos de vilão.
        /// </summary>
        /// <summary>
        /// O assento de um NPC em cena que não é vilão (aliado, neutro): nome (abre a ficha), o papel, os lembretes e o
        /// "+" para pôr outro. Menor que o do vilão: não tem condições de luta.
        /// </summary>
        void AssentoDeNpc(Transform aresta, Personagem n)
        {
            var c = Estilo.Rect("Npc " + n.NomeExibicao, aresta);
            var le = c.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = 300;
            le.minHeight = le.preferredHeight = 120;
            le.flexibleWidth = le.flexibleHeight = 0;
            le.layoutPriority = 2;
            Estilo.Moldura(c.gameObject, Estilo.Cartao, Estilo.Azul);
            var vl = c.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(12, 12, 8, 8);
            vl.spacing = 6;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandHeight = false;

            var topo = Estilo.Fila(c, 52, 8);
            var abrir = Estilo.Rect("AbrirFicha", topo);
            Estilo.ColunaElastica(abrir);
            var h = abrir.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            Retrato(abrir, n);
            var papel = n.Papel == Papel.Aliado ? "aliado" : n.Papel == Papel.Neutro ? "neutro" : n.Papel.ToString().ToLowerInvariant();
            var nome = Estilo.Rotulo(abrir, $"{n.NomeExibicao}\n<size=70%>{papel} · em cena</size>", 20, Estilo.Azul, FontStyles.Bold);
            nome.textWrappingMode = TextWrappingModes.NoWrap;
            nome.overflowMode = TextOverflowModes.Ellipsis;
            Estilo.ColunaElastica(nome);
            var fundoAbrir = abrir.gameObject.AddComponent<Image>();
            fundoAbrir.color = new Color(1, 1, 1, 0);
            var botaoAbrir = abrir.gameObject.AddComponent<Button>();
            botaoAbrir.targetGraphic = fundoAbrir;
            botaoAbrir.name = TextoLivre;
            botaoAbrir.onClick.AddListener(() => AbrirFichaDoNpc(n));
            abrir.gameObject.AddComponent<Dica>().Configurar("Abrir a ficha de " + n.NomeExibicao + ".");

            var filaL = Estilo.Fila(c, 30, 6);
            filaL.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            foreach (var b in mesa.BonusDe(n.Id))
                TokenDeBonus(filaL, b);
            var mais = Estilo.BotaoContorno(filaL, "+", 30, Estilo.TextoSuave, () =>
            {
                bonusTipo = Mesa.BonusLembrete;
                AbrirBonus(n.Id);
            });
            mais.gameObject.AddComponent<Dica>().Configurar("Pôr um lembrete.");
            Estilo.Rect("Vao", filaL).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        void AssentoDeVilao(Transform aresta, Personagem v)
        {
            var c = Estilo.Rect("Vilao " + v.NomeExibicao, aresta);
            var le = c.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = 300;
            le.minHeight = le.preferredHeight = 212;
            le.flexibleWidth = le.flexibleHeight = 0;
            le.layoutPriority = 2;
            Estilo.Moldura(c.gameObject, Estilo.Cartao, Estilo.Erro);
            var vl = c.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(12, 12, 8, 8);
            vl.spacing = 6;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandHeight = false;

            // Retrato e nome abrem a ficha do NPC (movimentos, condições, notas).
            var topo = Estilo.Fila(c, 52, 8);
            var abrir = Estilo.Rect("AbrirFicha", topo);
            Estilo.ColunaElastica(abrir);
            var h = abrir.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            Retrato(abrir, v);
            var nome = Estilo.Rotulo(abrir, v.NomeExibicao, 22, Estilo.Erro, FontStyles.Bold);
            nome.textWrappingMode = TextWrappingModes.NoWrap;
            nome.overflowMode = TextOverflowModes.Ellipsis;
            Estilo.ColunaElastica(nome);
            var fundoAbrir = abrir.gameObject.AddComponent<Image>();
            fundoAbrir.color = new Color(1, 1, 1, 0);
            var botaoAbrir = abrir.gameObject.AddComponent<Button>();
            botaoAbrir.targetGraphic = fundoAbrir;
            botaoAbrir.name = TextoLivre;
            botaoAbrir.onClick.AddListener(() => AbrirFichaDoNpc(v));
            abrir.gameObject.AddComponent<Dica>().Configurar("Abrir a ficha de " + v.NomeExibicao + ".");

            var tem = v.Vilao?.Condicoes ?? new List<string>();
            var marcadas = mesa.CondicoesMarcadasDoVilao(v.Id);
            var estado = Estilo.Rotulo(c, mesa.NoLimite(v)
                    ? "no limite: o próximo golpe o derruba"
                    : $"{marcadas.Count} de {tem.Count} condições", 16,
                mesa.NoLimite(v) ? Estilo.Erro : Estilo.TextoSuave, FontStyles.Italic);
            estado.alignment = TextAlignmentOptions.Center;

            var filaC = Estilo.Fila(c, 36, 4);
            foreach (var cond in tem)
            {
                var id = cond;
                var marcada = marcadas.Contains(cond);
                var k = Condicoes.Todas.ToList().IndexOf(cond);
                var rotulo = k >= 0 ? Abrev[k] : cond;
                var b = marcada
                    ? Estilo.BotaoPrincipal(filaC, rotulo, 0, () => Aplicar(mesa.AlternarCondicaoDoVilao(v.Id, id, null)))
                    : Estilo.BotaoContorno(filaC, rotulo, 0, Estilo.TextoSuave, () =>
                    {
                        var r = mesa.AlternarCondicaoDoVilao(v.Id, id, null);
                        Aplicar(r);
                        if (r.Ok)
                            AbrirCartaDeCondicao(); // marcou: faça o movimento da condição agora
                    });
                b.GetComponent<LayoutElement>().flexibleWidth = 1;
                b.gameObject.AddComponent<Dica>().Configurar(Condicoes.Nome(cond) + (marcada ? " (marcada na luta)" : string.Empty));
            }
            var mov = Estilo.BotaoContorno(c, "Movimentos de vilão", 0, Estilo.Erro, () => AbrirMovimentos(v));
            mov.GetComponent<LayoutElement>().preferredHeight = 36;
            // Lembretes no vilão (ex.: "abertura: Aurora cedeu terreno"): etiqueta, e o "+" para pôr outro.
            var filaL = Estilo.Fila(c, 30, 6);
            filaL.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            foreach (var b in mesa.BonusDe(v.Id))
                TokenDeBonus(filaL, b);
            var mais = Estilo.BotaoContorno(filaL, "+", 30, Estilo.TextoSuave, () =>
            {
                bonusTipo = Mesa.BonusLembrete;
                AbrirBonus(v.Id);
            });
            mais.gameObject.AddComponent<Dica>().Configurar("Pôr um lembrete (ex.: abertura: Aurora cedeu terreno).");
            Estilo.Rect("Vao", filaL).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        PainelAjuda ajudaCondicoes;
        PainelAjuda ajuda;

        /// <summary>O help inteiro (todas as categorias), por cima da mesa.</summary>
        void AbrirAjuda()
        {
            if (ajuda == null)
                ajuda = PainelAjuda.Criar(transform.parent);
            ajuda.Abrir(null);
        }

        void AbrirCartaDeCondicao(string condicao = null)
        {
            if (ajudaCondicoes == null)
                ajudaCondicoes = PainelAjuda.Criar(transform.parent);
            ajudaCondicoes.AbrirCondicoesVilao(condicao);
        }

        /// <summary>
        /// A pendência do movimento de condição do vilão (a troca marcou a condição): ver a carta da condição e marcar como
        /// resolvido, com o que ele fez (opcional). O resolvido entra no log e se desfaz como os outros.
        /// </summary>
        void AbrirMovimentoDeCondicao(Evento rolagem, string nomeDoVilao, string condicao)
        {
            var caixa = Caixa($"MOVIMENTO DE CONDIÇÃO · {nomeDoVilao.ToUpperInvariant()}", 420);
            Nota(caixa, $"{nomeDoVilao} marcou {Condicoes.Nome(condicao)} na troca de golpes: ele faz o movimento dessa condição.");
            var ver = Estilo.BotaoContorno(caixa, $"Ver a carta: {Condicoes.Nome(condicao)}", 0, Estilo.Dourado, () => AbrirCartaDeCondicao(condicao));
            ver.GetComponent<LayoutElement>().preferredHeight = 48;
            var campo = CampoMotivo(caixa, "o que ele fez (opcional)");
            Rodape(caixa, "Resolvido", () =>
            {
                var r = mesa.ResolverMovimentoDeCondicao(rolagem.Id, campo.text);
                if (!r.Ok)
                {
                    Mostrar(r);
                    return;
                }
                FecharBatalha();
                Aplicar(r);
            });
        }

        /// <summary>Os movimentos de vilão dele: clicar registra no log qual foi usado.</summary>
        void AbrirMovimentos(Personagem v)
        {
            var movimentos = (v.Vilao?.Movimentos ?? new List<string>()).Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
            var caixa = Caixa($"{v.NomeExibicao.ToUpperInvariant()} · MOVIMENTOS DE VILÃO", Math.Min(980, 230 + movimentos.Count * 92), 1150);
            if (movimentos.Count == 0)
                Nota(caixa, "Sem movimentos na ficha de vilão (escreva na ficha do NPC).");
            foreach (var m in movimentos)
            {
                var texto = m;
                var b = Estilo.BotaoContorno(caixa, texto, 0, Estilo.Dourado, () =>
                {
                    FecharBatalha();
                    Aplicar(mesa.UsarMovimentoDeVilao(v.Id, texto, null));
                });
                b.gameObject.name = TextoLivre; // texto longo: quebra linha, não encolhe
                var le = b.GetComponent<LayoutElement>();
                le.minHeight = le.preferredHeight = 80;
                // Frase longa: quebra em até três linhas, sem caixa alta nem espaçamento, e a fonte encolhe se precisar.
                var r = b.GetComponentInChildren<TextMeshProUGUI>();
                r.textWrappingMode = TextWrappingModes.Normal;
                r.alignment = TextAlignmentOptions.MidlineLeft;
                r.fontStyle = FontStyles.Normal;
                r.characterSpacing = 0;
                r.enableAutoSizing = true;
                r.fontSizeMin = 14;
                r.fontSizeMax = 22;
                r.rectTransform.offsetMin = new Vector2(16, 6);
                r.rectTransform.offsetMax = new Vector2(-16, -6);
            }
            var fim = Estilo.Fila(caixa, 56, 12);
            Estilo.Rect("Vao", fim).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Estilo.BotaoContorno(fim, "Fechar", 200, Estilo.TextoSuave, FecharBatalha);
        }

        /// <summary>O centro: a pilha de Equipe (número e tokens), os comandos dela e o fim do log.</summary>
        void Centro(RectTransform centro)
        {
            Estilo.Moldura(centro.gameObject, Estilo.Campo, Estilo.Azul);
            var v = centro.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(20, 20, 14, 14);
            v.spacing = 10;
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandHeight = false;

            // Sem cena, a mesa está vazia: tudo começa por criar a cena (quem está nela, quem assiste).
            if (!mesa.TemCena)
            {
                Estilo.Rotulo(centro, "Nenhuma cena aberta", 26, Estilo.Azul, FontStyles.Bold).alignment = TextAlignmentOptions.Center;
                var aviso = Estilo.Rotulo(centro, "Crie a cena e marque quem está nela; as outras assistem.", 18, Estilo.TextoSuave, FontStyles.Italic);
                aviso.alignment = TextAlignmentOptions.Center;
                var filaC = Estilo.Fila(centro, 64, 10);
                filaC.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
                Encolher(Estilo.BotaoPrincipal(filaC, "Criar cena", 300, AbrirCriarCena));
                return;
            }

            // A pilha fica à vista na mesa; mexer nela é pelo "Gerir pilha" (uma telinha).
            var topo = Estilo.Fila(centro, 60, 14);
            var t = Estilo.Rotulo(topo, "PILHA DE EQUIPE", 24, Estilo.Azul, FontStyles.Bold);
            t.characterSpacing = 5;
            t.gameObject.AddComponent<LayoutElement>().preferredWidth = 260;
            var n = Estilo.Rotulo(topo, mesa.Equipe.ToString(), 56, Estilo.Texto, FontStyles.Bold);
            n.gameObject.AddComponent<LayoutElement>().preferredWidth = 90;
            var gerir = Estilo.BotaoPrincipal(topo, "Gerir pilha", 220, AbrirGestaoDaPilha);
            Encolher(gerir);
            Estilo.Rect("Vao", topo).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var token = Resources.Load<Texture2D>("Icones/Tokens/equipe");
            var fichas = Estilo.Fila(centro, 52, 6);
            fichas.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            for (var k = 0; k < Math.Min(mesa.Equipe, 12); k++)
            {
                var img = Estilo.Rect("Token", fichas).gameObject.AddComponent<RawImage>();
                img.texture = token;
                var li = img.gameObject.AddComponent<LayoutElement>();
                li.preferredWidth = li.preferredHeight = 48;
            }
            if (mesa.Equipe == 0)
                Estilo.Rotulo(fichas, "pilha vazia", 20, Estilo.TextoApagado, FontStyles.Italic);

            // As jogadas: a dos jogadores (rolar) e a do MJ (o deck), com o termômetro de leves e pesados do log.
            var acoes = Estilo.Fila(centro, 56, 12);
            acoes.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            var rolar = Estilo.BotaoPrincipal(acoes, "Rolar movimento", 260, AbrirRolagem);
            Encolher(rolar);
            rolar.gameObject.AddComponent<Dica>().Configurar("Quem rola, o alvo e o movimento básico: 2d6 + Rótulo. O resultado entra no log pendente (Intervir / Aceitar).");
            var movMj = Estilo.BotaoContorno(acoes, "Movimento do MJ", 260, Estilo.Erro, () => AbrirMovimentoDoMj(null, null));
            Encolher(movMj);
            movMj.gameObject.AddComponent<Dica>().Configurar("O deck do MJ: escolha o movimento, o peso (leve ou pesado) e contra quem. Vai para o log.");
            var (leves, pesados) = mesa.PesosNoLog();
            var termometro = Estilo.Rotulo(acoes, $"leves {leves} · pesados {pesados}", 16, Estilo.TextoSuave);
            termometro.gameObject.AddComponent<LayoutElement>().preferredWidth = 170;
            termometro.raycastTarget = true;
            termometro.gameObject.AddComponent<Dica>().Configurar("Movimentos do MJ no log. Só pesados: você deixou de ser fã dos heróis; só leves: as ameaças não assustam (p.177).");

            var filaCena = Estilo.Fila(centro, 44, 12);
            filaCena.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            var cena = mesa.CenaAtual;
            var nomeDoLocal = cena.Local == null ? null : personagens.CenarioDaSala(sala.Id).FirstOrDefault(e => e.Id == cena.Local)?.Nome;
            var gerirCena = Estilo.BotaoContorno(filaCena, $"CENA: {cena.Nome}" + (nomeDoLocal != null ? $" · {nomeDoLocal}" : "") + "  ›", 0,
                Estilo.TextoSuave, AbrirGestaoDaCena);
            gerirCena.GetComponent<LayoutElement>().flexibleWidth = 1;
            Encolher(gerirCena);
            gerirCena.gameObject.AddComponent<Dica>().Configurar("Gerir a cena: nome e local, quem está nela, os vilões e NPCs em cena.");
            var nova = Estilo.BotaoContorno(filaCena, "Nova cena", 180, Estilo.Dourado, AbrirCriarCena);
            Encolher(nova);
            nova.gameObject.AddComponent<Dica>().Configurar("Abrir a próxima cena e marcar quem está nela (a pilha e as condições continuam).");
        }

        /// <summary>O log da mesa, no lado direito: do mais novo ao mais antigo, com desfazer o último.</summary>
        void LogDaMesa(RectTransform log)
        {
            Estilo.Moldura(log.gameObject, Estilo.Campo, Estilo.BordaApagada);
            var v = log.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(14, 14, 12, 12);
            v.spacing = 8;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandHeight = false;
            // O título numa linha e os botões embaixo: no log estreito, os três botões espremiam o título até
            // virar uma coluna de letras.
            var lt = Estilo.Rotulo(log, "LOG DA MESA", 20, Estilo.TextoSuave, FontStyles.Bold);
            lt.characterSpacing = 4;
            lt.textWrappingMode = TextWrappingModes.NoWrap;
            lt.gameObject.AddComponent<LayoutElement>().minHeight = 28;
            var cabeca = Estilo.Fila(log, 36, 6);
            // Desfeito por engano: se o último evento é um desfazer (ainda não refeito), dá para refazer.
            var fim = mesa.VisiveisDaCena.LastOrDefault();
            if (fim?.Corrige != null && !mesa.Eventos.Any(e => e.Refaz == fim.Id))
                Encolher(Elastico(Estilo.BotaoContorno(cabeca, "Refazer", 0, Estilo.Dourado, () =>
                    Aplicar(mesa.Refazer(fim.Id, null)))));
            var limpar = Elastico(Estilo.BotaoContorno(cabeca, "Limpar", 0, Estilo.TextoSuave, () =>
            {
                var n = mesa.EsconderIdasEVoltas();
                Redesenhar();
                Mostrar(null, n == 0 ? "Nenhuma ida e volta no log." : $"{n} par{(n == 1 ? "" : "es")} de ida e volta escondido{(n == 1 ? "" : "s")}.");
            }));
            Encolher(limpar);
            limpar.gameObject.AddComponent<Dica>().Configurar("Limpar idas e voltas: esconde cliques que se anularam (marcou e desmarcou seguidos). A mesa não muda.");
            var ultimo = mesa.VisiveisDaCena.LastOrDefault(e => e.Corrige == null && !mesa.Corrigido(e));
            if (ultimo != null)
            {
                // "Desfazer" curto: com Refazer e Limpar ao lado, "Desfazer último" não cabia no log estreito.
                var desfazer = Elastico(Estilo.BotaoContorno(cabeca, "Desfazer", 0, Estilo.TextoSuave, () =>
                    Aplicar(mesa.Corrigir(ultimo.Id, "desfeito na mesa"))));
                Encolher(desfazer);
                desfazer.gameObject.AddComponent<Dica>().Configurar("Desfazer a última entrada desta cena (ela fica riscada no log; o efeito volta).");
            }
            var lista = Estilo.ListaRolavel(log, 6);
            var suave = ColorUtility.ToHtmlStringRGB(Estilo.TextoApagado);
            // Cada entrada é clicável: abre editar/apagar.
            foreach (var e in mesa.VisiveisDaCena.Reverse())
            {
                var quem = e.Personagem != null ? personagens.Buscar(e.Personagem)?.NomeExibicao ?? "?"
                    : e.Rolagem?.Movimento != null ? HeroiGenerico : "Mesa";
                var riscado = mesa.Corrigido(e);
                var motivo = string.IsNullOrWhiteSpace(e.Motivo) ? string.Empty : $"\n<size=85%><i><color=#{suave}>{e.Motivo}</color></i></size>";
                var editado = e.Editado ? $" <size=75%><color=#{suave}>(editado)</color></size>" : string.Empty;
                var linha = Estilo.Rotulo(lista,
                    $"<color=#{suave}>r{e.Rodada}</color>  <b>{quem}</b>: " + (riscado ? $"<s>{e.Descricao}</s>" : e.Descricao) + editado + motivo,
                    17, riscado ? Estilo.TextoApagado : Estilo.Texto);
                linha.textWrappingMode = TextWrappingModes.Normal;
                linha.raycastTarget = true;
                var evento = e;
                var b = linha.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.name = TextoLivre;
                b.onClick.AddListener(() => AbrirEntrada(evento));
                linha.gameObject.AddComponent<Dica>().Configurar("Clique para editar ou esconder esta entrada.");
                // Rolagem pendente: o resultado só vale quando o mestre aceita; antes, alguém pode intervir.
                if (mesa.Pendente(e))
                {
                    var acoes = Estilo.Fila(lista, 36, 6);
                    basicos ??= Core.Regras.MovimentosBasicos.Carregar(new FonteDeRegrasResources());
                    // O golpe poderoso não aceita intervenção: só o Aceitar.
                    var semIntervencao = basicos.Todos.FirstOrDefault(m => m.Id == e.Rolagem.Movimento)?.SemIntervencao == true;
                    var intervir = semIntervencao ? null : Elastico(Estilo.BotaoContorno(acoes, "Intervir", 0, Estilo.Dourado, () =>
                    {
                        if (evento.Rolagem.Movimento != null)
                            AbrirRolagemDoEvento(evento, intervir: true);
                        else
                            AbrirIntervencao(evento);
                    }));
                    if (intervir != null)
                    {
                        Encolher(intervir);
                        intervir.gameObject.AddComponent<Dica>().Configurar("Alguém mexe no resultado depois do dado: ajuda da pilha, egoísta, renúncia de Influência, Defender, carta custom.");
                    }
                    // Rolagem de movimento: o Aceitar abre o painel (as escolhas do resultado são marcadas lá).
                    var aceitar = Elastico(Estilo.BotaoPrincipal(acoes, "Aceitar", 0, () =>
                    {
                        if (evento.Rolagem.Movimento != null)
                            AbrirRolagemDoEvento(evento);
                        else
                            Aplicar(mesa.AceitarRolagem(evento.Id));
                    }));
                    Encolher(aceitar);
                    aceitar.gameObject.AddComponent<Dica>().Configurar("Ninguém mais intervém: o resultado vale.");
                }
                // Rolagem anotada para depois (/eda, /tgp): o botão abre o painel já preenchido.
                if (mesa.PedidoAberto(e))
                {
                    BotaoDePendencia(lista, $"Rolar: {evento.Pedido.NomeDoMovimento}", Estilo.Dourado, () => AbrirPedido(evento),
                        "Pedido no log: abre o painel com quem rola, o movimento e contra quem.");
                }
                // Falha: o MJ faz um movimento (p.177). Fica a pendência, para fazer na ordem que couber.
                if (mesa.MovimentoDoMjPedido(e))
                {
                    BotaoDePendencia(lista, "Movimento do MJ (falha)", Estilo.Erro, () => AbrirMovimentoDoMj(evento.Id, evento.Personagem),
                        "A rolagem falhou: o MJ faz um movimento (leve ou pesado, você decide).");
                }
                // A troca marcou condição no vilão: o movimento dela fica pendente, para o MJ resolver na ordem que couber.
                var condicaoPedida = mesa.CondicaoPedida(e);
                if (condicaoPedida != null)
                {
                    var (vilaoId, condicao) = condicaoPedida.Value;
                    var nomeDoVilao = personagens.Buscar(vilaoId)?.NomeExibicao ?? "vilão";
                    BotaoDePendencia(lista, $"Movimento de condição: {nomeDoVilao} ({Condicoes.Nome(condicao)})", Estilo.Erro,
                        () => AbrirMovimentoDeCondicao(evento, nomeDoVilao, condicao),
                        "A troca marcou essa condição: o vilão faz o movimento dela. Abre a carta e marca como resolvido.");
                }
                // Golpe poderoso pedido por esta rolagem e ainda não rolado: um botão por quem toma.
                foreach (var tomaId in mesa.GolpesPedidos(e))
                {
                    var toma = tomaId;
                    var nome = toma == null ? HeroiGenerico : personagens.Buscar(toma)?.NomeExibicao ?? "?";
                    BotaoDePendencia(lista, $"Golpe poderoso: {nome}", Estilo.Dourado, () => AbrirGolpePoderoso(evento.Id, toma),
                        "A troca de golpes pediu: rolar + condições marcadas (faixas invertidas).");
                }
            }
            if (mesa.VisiveisDaCena.Count == 0)
                Estilo.Rotulo(lista, "Nada ainda. Início de sessão: +1 na pilha (Gerir pilha → Passagem do tempo).", 17,
                    Estilo.TextoApagado, FontStyles.Italic).textWrappingMode = TextWrappingModes.Normal;

            // O console do mestre, embaixo do log: comandos com barra (/help lista todos).
            var comando = Estilo.CampoTexto(log, "/comando  (ex.: /roll +2, /help)");
            var lc = comando.gameObject.AddComponent<LayoutElement>();
            lc.minHeight = lc.preferredHeight = 44;
            ((TextMeshProUGUI)comando.textComponent).fontSize = 18;
            ((TextMeshProUGUI)comando.placeholder).fontSize = 18;
            comando.onSubmit.AddListener(async t =>
            {
                if (string.IsNullOrWhiteSpace(t))
                    return;
                var palavra = t.Trim().TrimStart('/').Split(' ')[0].ToLowerInvariant();
                if (palavra == "help" || palavra == "ajuda")
                {
                    consoleAtual.text = string.Empty;
                    AbrirAjudaDoConsole();
                    return;
                }
                basicos ??= Core.Regras.MovimentosBasicos.Carregar(new FonteDeRegrasResources());
                var console = new ConsoleDaMesa(mesa, personagens, sala, basicos: basicos);
                historico.Remove(t);
                historico.Add(t);
                // /reset é debug e mexe em tudo: pede confirmação antes.
                if (palavra == "reset" || palavra == "resetar")
                {
                    consoleAtual.text = string.Empty;
                    Estilo.Confirmar(transform,
                        "Voltar a mesa ao início do jogo? (debug)\nRodada 1, pilha 0; as fichas da sala ficam sem potencial nem condições, " +
                        "com os Rótulos e as trilhas da ficha aprovada. O log atual some da tela (fica no arquivo) e cada parte se desfaz pelo log.",
                        "Resetar", () =>
                        {
                            var (okReset, msgReset) = console.Executar(t);
                            Redesenhar();
                            Mostrar(okReset ? (Resultado?)null : Resultado.Falha(msgReset), okReset ? msgReset : null);
                        });
                    return;
                }
                // /salvar grava o log em texto na pasta de exportações (o Core só monta o texto) e copia para colar.
                if (palavra == "salvar" || palavra == "salvarlog")
                {
                    consoleAtual.text = string.Empty;
                    var (okTexto, texto) = console.Executar(t);
                    if (!okTexto || Troca.Arquivos == null)
                    {
                        Mostrar(Resultado.Falha(okTexto ? "Pasta de exportações indisponível." : texto));
                        return;
                    }
                    string caminho;
                    try
                    {
                        caminho = Troca.Arquivos.Exportar($"log_{sala.Nome}", $"{sala.Nome} · log da mesa\n\n{texto}\n", ".txt");
                    }
                    catch (Exception e)
                    {
                        Mostrar(Resultado.Falha("Não deu para salvar: " + e.Message));
                        return;
                    }
                    GUIUtility.systemCopyBuffer = texto;
                    Estilo.Confirmar(transform, $"Log salvo em\n<size=80%>{caminho}</size>\nO texto também foi copiado (Ctrl+V para colar).",
                        "Abrir a pasta", () => Application.OpenURL("file:///" + Troca.Arquivos.PastaExportacoes.Replace('\\', '/')));
                    return;
                }
                (bool ok, string msg) resposta;
                // /fixo keeps the requested pair but now shows its directed debug animation.
                var dadosFisicos = ConsoleDaMesa.EhRolagem(t) ? FindAnyObjectByType<SobreposicaoDados>() : null;
                if (dadosFisicos != null && dadosFisicos.isActiveAndEnabled)
                {
                    if (dadosFisicos.EmRolagem)
                    {
                        Mostrar(Resultado.Falha("Os dados ainda estão rolando."));
                        return;
                    }
                    Mostrar(null, "Rolando 2d6...");
                    var dados = await JogarDados();
                    if (this == null || mesa == null)
                        return; // a mesa fechou enquanto os dados rolavam
                    if (!dados.HasValue)
                        return;
                    resposta = console.RolarComDados(t, dados.Value.Item1, dados.Value.Item2);
                }
                else
                    resposta = console.Executar(t);
                var (ok, msg) = resposta;
                Redesenhar();
                Mostrar(ok ? (Resultado?)null : Resultado.Falha(msg), ok ? msg : null);
                focarConsole = true; // o campo novo (depois do redesenho) volta a receber o foco
            });
            consoleAtual = comando;
            if (focarConsole)
            {
                focarConsole = false;
                comando.ActivateInputField();
            }
        }

        /// <summary>
        /// Um botão de pendência no log (pedido, falha, condição do vilão, golpe): até duas linhas, a fonte encolhe até
        /// caber. Numa linha só, "Movimento de condição: Ursa Metálica (Irritado)" não cabia no log estreito.
        /// </summary>
        void BotaoDePendencia(Transform lista, string texto, Color cor, UnityEngine.Events.UnityAction acao, string dica)
        {
            var fila = Estilo.Fila(lista, 44, 6);
            var b = Elastico(Estilo.BotaoContorno(fila, texto, 0, cor, acao));
            b.name = TextoLivre;
            TextoDeOpcao(b, 16);
            b.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            b.gameObject.AddComponent<Dica>().Configurar(dica);
        }

        /// <summary>
        /// A janela dos comandos do console (como as outras caixas da mesa). Clicar num exemplo põe ele no
        /// console para editar e mandar.
        /// </summary>
        void AbrirAjudaDoConsole()
        {
            // Altura fixa e a lista rola dentro: com muitos comandos, a caixa crescia além da tela e espremia título e nota.
            var caixa = Caixa("CONSOLE DO MESTRE", 820, 1250);
            Nota(caixa, "Nomes aceitam o começo, sem acento (\"aur\" acha a Aurora). Aspas juntam palavras. ↑/↓ no console repete comandos. Clique num exemplo para usá-lo.");
            var lista = Estilo.ListaRolavel(caixa, 10);
            foreach (var (exemplo, oQueFaz) in ConsoleDaMesa.Comandos)
            {
                var fila = Estilo.Fila(lista, 48, 12);
                var texto = exemplo;
                var b = Estilo.BotaoContorno(fila, exemplo, 520, Estilo.Dourado, () =>
                {
                    FecharBatalha();
                    if (consoleAtual == null)
                        return;
                    consoleAtual.text = texto;
                    consoleAtual.ActivateInputField();
                    consoleAtual.caretPosition = texto.Length;
                });
                b.name = TextoLivre;
                var r = b.GetComponentInChildren<TextMeshProUGUI>();
                r.fontStyle = FontStyles.Normal;
                r.characterSpacing = 0;
                r.fontSize = 17;
                r.alignment = TextAlignmentOptions.MidlineLeft;
                r.textWrappingMode = TextWrappingModes.NoWrap;
                r.overflowMode = TextOverflowModes.Ellipsis;
                r.rectTransform.offsetMin = new Vector2(12, 0);
                var d = Estilo.Rotulo(fila, oQueFaz, 18, Estilo.TextoSuave);
                d.textWrappingMode = TextWrappingModes.Normal;
                Estilo.ColunaElastica(d);
            }
            var fim = Estilo.Fila(caixa, 56, 12);
            Estilo.Rect("Vao", fim).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Estilo.BotaoContorno(fim, "Fechar", 200, Estilo.TextoSuave, FecharBatalha);
        }

        readonly List<string> historico = new List<string>();
        int posicaoNoHistorico = -1;
        bool focarConsole;
        TMP_InputField consoleAtual;

        /// <summary>No console: seta para cima/baixo navega pelos comandos já digitados.</summary>
        void Update()
        {
            if (consoleAtual == null || !consoleAtual.isFocused || historico.Count == 0)
                return;
            var cima = UnityEngine.InputSystem.Keyboard.current?.upArrowKey.wasPressedThisFrame == true;
            var baixo = UnityEngine.InputSystem.Keyboard.current?.downArrowKey.wasPressedThisFrame == true;
            if (!cima && !baixo)
                return;
            if (posicaoNoHistorico < 0)
                posicaoNoHistorico = historico.Count;
            posicaoNoHistorico = Math.Max(0, Math.Min(historico.Count, posicaoNoHistorico + (cima ? -1 : 1)));
            consoleAtual.text = posicaoNoHistorico < historico.Count ? historico[posicaoNoHistorico] : string.Empty;
            consoleAtual.caretPosition = consoleAtual.text.Length;
        }

        /// <summary>
        /// Uma entrada do log: editar o texto (descrição e motivo) ou apagar. Apagar some do log mas fica no
        /// arquivo; se a entrada ainda mudou algo na mesa, apagar também desfaz o efeito (a Torre avisa).
        /// </summary>
        void AbrirEntrada(Evento e)
        {
            var caixa = Caixa("ENTRADA DO LOG", 520, 1000);
            Pergunta(caixa, "Descrição");
            var descricao = CampoMotivo(caixa, "o que aconteceu");
            descricao.text = e.Descricao;
            Pergunta(caixa, "Motivo (opcional)");
            var motivo = CampoMotivo(caixa, "por quê / em que cena");
            motivo.text = e.Motivo;
            if (mesa.TemEfeito(e))
                Nota(caixa, "Esta entrada mudou algo na mesa. Editar muda só o texto; esconder também desfaz o efeito.");
            var fim = Estilo.Fila(caixa, 56, 12);
            Estilo.BotaoContorno(fim, "Esconder", 190, Estilo.Erro, () =>
            {
                var texto = mesa.TemEfeito(e)
                    ? $"Esconder \"{e.Descricao}\" do log?\nEla mudou algo na mesa: o efeito também é desfeito."
                    : $"Esconder \"{e.Descricao}\" do log? (fica guardada no arquivo)";
                Estilo.Confirmar(transform, texto, "Esconder", () =>
                {
                    FecharBatalha();
                    Aplicar(mesa.ApagarEvento(e.Id, desfazerEfeito: true));
                });
            });
            Estilo.Rect("Vao", fim).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Estilo.BotaoContorno(fim, "Cancelar", 200, Estilo.TextoSuave, FecharBatalha);
            Estilo.BotaoPrincipal(fim, "Salvar", 220, () =>
            {
                var r = mesa.EditarEvento(e.Id, descricao.text, motivo.text);
                if (!r.Ok)
                {
                    Mostrar(r);
                    return;
                }
                FecharBatalha();
                Aplicar(r);
            });
        }

        // ---------- rolar movimento (por enquanto só 2d6 + Rótulo) ----------

        const string HeroiGenerico = "Herói genérico";
        Core.Regras.MovimentosBasicos basicos;
        string rolQuem; // null: o herói genérico (todos os Rótulos +0)
        string rolAlvo; // null: ninguém
        int rolMovimento;

        const string QuemGenerico = "_generico", AlvoNinguem = "_ninguem";
        string rolEvento; // a rolagem que esta caixa lançou (pendente no log), ou null antes de rolar
        string rolOrigem; // a rolagem que pediu esta (o golpe poderoso da troca), ou null
        readonly List<EscolhaDaRolagem> rolEscolhas = new List<EscolhaDaRolagem>();
        string rolCondAlvo, rolRetorno, rolCondRolou; // a troca de golpes, como o mestre marca

        bool rolIntervindo; // a seção 2 está aberta: o total ainda pode mudar, as opções esperam
        TMP_InputField campoModDefensorDoPainel;

        void LimparEscolhas()
        {
            rolEscolhas.Clear();
            rolCondAlvo = rolRetorno = rolCondRolou = null;
            rolIntervindo = false;
        }

        void ComecarIntervencao()
        {
            LimparEscolhas(); // as escolhas eram do total de antes
            rolIntervindo = true;
            intervOrigem = 0;
            intervMod = Origens[0].mod;
            intervQuem = null;
            intervRolagem = null;
        }

        /// <summary>Reabre o painel de uma rolagem de movimento pendente no log, para escolher e aceitar.</summary>
        void AbrirRolagemDoEvento(Evento e, bool intervir = false)
        {
            basicos ??= Core.Regras.MovimentosBasicos.Carregar(new FonteDeRegrasResources());
            var indice = basicos.Todos.ToList().FindIndex(m => m.Id == e.Rolagem.Movimento);
            if (indice < 0)
            {
                Aplicar(mesa.AceitarRolagem(e.Id));
                return;
            }
            rolMovimento = indice;
            rolQuem = e.Personagem;
            rolAlvo = e.Rolagem.Alvo;
            rolEvento = e.Id;
            rolOrigem = e.Rolagem.Origem;
            LimparEscolhas();
            if (intervir && !basicos.Todos[indice].SemIntervencao)
                ComecarIntervencao();
            DesenharRolagem();
        }

        void AbrirRolagem() => AbrirRolagem(null);

        /// <summary>
        /// Abre o painel de rolagem. Com <paramref name="heroi"/> (o "M" do assento), ela já vem como quem rola e,
        /// com um vilão só em cena, ele já vem como contra quem.
        /// </summary>
        void AbrirRolagem(string heroi)
        {
            basicos ??= Core.Regras.MovimentosBasicos.Carregar(new FonteDeRegrasResources());
            if (heroi != null)
                rolQuem = heroi;
            if (rolQuem != null && NaMesa().All(h => h.Id != rolQuem))
                rolQuem = null;
            var viloes = mesa.EmCena();
            rolAlvo = heroi != null && viloes.Count == 1 ? viloes[0].Id : null;
            rolEvento = null;
            rolOrigem = null;
            if (basicos.Todos.ElementAtOrDefault(rolMovimento)?.SomaCondicoes == true)
                rolMovimento = 0; // o golpe poderoso não fica escolhido de uma rolagem para a outra
            LimparEscolhas();
            DesenharRolagem();
        }

        Core.Regras.RegrasDeCondicoes regrasDeCondicoes;
        Core.Cartas.CatalogoDeCartas catalogoDeCartas;

        /// <summary>
        /// A dica da condição no assento: o nome, os movimentos em que ela pesa (−2) e o que a tira no fim da cena,
        /// tudo lido do nucleo.json (os nomes dos movimentos vêm do catálogo de cartas).
        /// </summary>
        string DicaDaCondicao(string condicao, bool marcada)
        {
            regrasDeCondicoes ??= Core.Regras.RegrasDeCondicoes.Carregar(new FonteDeRegrasResources());
            catalogoDeCartas ??= Core.Cartas.CatalogoDeCartas.Carregar(new FonteDeRegrasResources());
            var texto = Condicoes.Nome(condicao) + (marcada ? " (marcada)" : string.Empty);
            var movimentos = regrasDeCondicoes.MovimentosAfetados(condicao)
                .Select(m => catalogoDeCartas.Buscar(m)?.Titulo ?? m).ToList();
            if (movimentos.Count > 0)
                texto += $"\n{(regrasDeCondicoes.Penalidade < 0 ? "−" : "+")}{Math.Abs(regrasDeCondicoes.Penalidade)} em {string.Join(" e ", movimentos)}";
            var remover = regrasDeCondicoes.ComoRemover(condicao);
            if (remover != null)
                texto += $"\nSai no fim da cena se: {remover}";
            return texto;
        }

        /// <summary>Abre o painel para o pedido do log (/eda, /tgp): quem, movimento e contra quem vêm dele, travados.</summary>
        void AbrirPedido(Evento pedido)
        {
            basicos ??= Core.Regras.MovimentosBasicos.Carregar(new FonteDeRegrasResources());
            var indice = basicos.Todos.ToList().FindIndex(m => m.Id == pedido.Pedido.Movimento);
            if (indice < 0)
            {
                Mostrar(Resultado.Falha($"Movimento do pedido não existe mais: {pedido.Pedido.NomeDoMovimento}."));
                return;
            }
            rolMovimento = indice;
            rolQuem = pedido.Personagem;
            rolAlvo = pedido.Pedido.Alvo;
            rolEvento = null;
            rolOrigem = pedido.Id;
            LimparEscolhas();
            DesenharRolagem();
        }

        /// <summary>
        /// O golpe poderoso que uma rolagem pediu (a troca do Enfrentar): o mesmo painel, com quem toma o golpe já
        /// escolhido, sem contra quem, e a origem gravada na rolagem nova.
        /// </summary>
        void AbrirGolpePoderoso(string origem, string quemToma)
        {
            basicos ??= Core.Regras.MovimentosBasicos.Carregar(new FonteDeRegrasResources());
            var indice = basicos.Todos.ToList().FindIndex(m => m.SomaCondicoes);
            if (indice < 0)
                return;
            rolMovimento = indice;
            rolQuem = quemToma;
            rolAlvo = null;
            rolEvento = null;
            rolOrigem = origem;
            LimparEscolhas();
            DesenharRolagem();
        }

        /// <summary>
        /// O painel de rolagem (desenho do Alan): 1, o rolamento simples (quem, contra quem, movimento, rolar) com
        /// o resultado parcial lido duas vezes (sucesso/falha e 10+/7–9/6−); 2, a intervenção (ainda por fazer);
        /// à direita, o efeito esperado do movimento, com Intervir e Aceitar.
        /// </summary>
        void DesenharRolagem()
        {
            var movimentos = basicos.Todos;
            rolMovimento = Math.Max(0, Math.Min(movimentos.Count - 1, rolMovimento));
            var movimento = movimentos[rolMovimento];
            var golpe = movimento.SomaCondicoes; // o golpe poderoso: soma condições, sem contra quem
            if (golpe)
                rolAlvo = null;
            var quem = rolQuem == null ? null : personagens.Buscar(rolQuem);
            var evento = rolEvento == null ? null : mesa.Eventos.FirstOrDefault(x => x.Id == rolEvento);
            var rolou = evento?.Rolagem != null;
            int? total = rolou ? evento.Rolagem.Total : (int?)null;
            string Sinal(int v) => (v < 0 ? "−" : "+") + Math.Abs(v);

            var caixa = Caixa("ROLAR MOVIMENTO", 960, 1500);
            var corpo = Estilo.Fila(caixa, 840, 18);
            corpo.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
            var esquerda = Coluna(corpo, 0, false, 10);
            esquerda.GetComponent<LayoutElement>().flexibleWidth = 1;
            var direita = Coluna(corpo, 20, true, 6);
            direita.GetComponent<LayoutElement>().preferredWidth = 500;

            // 1 · rolamento simples
            Estilo.Rotulo(esquerda, "1 · ROLAMENTO SIMPLES", 22, Estilo.Azul, FontStyles.Bold).characterSpacing = 3;
            // Os campos em cima, com a largura toda (as listas espremidas ficavam ilegíveis); o resultado embaixo.
            var sec1 = Coluna(esquerda, 22, true, 12);
            var campos = Coluna(sec1, 0, false, 14);
            var resultado = Coluna(sec1, 0, false, 8);

            var opcoesQuem = new List<(string, string)> { (QuemGenerico, HeroiGenerico) };
            opcoesQuem.AddRange(NaMesa().Select(h => (h.Id, h.NomeExibicao)));
            // Golpe pedido por outra rolagem: quem toma e o movimento vêm dela, travados.
            var pedido = rolOrigem != null;
            CampoDaRolagem(campos, golpe ? "QUEM TOMA?" : "QUEM ROLA?", opcoesQuem, rolQuem ?? QuemGenerico, id =>
            {
                rolQuem = id == QuemGenerico ? null : id;
                if (rolAlvo == rolQuem)
                    rolAlvo = null;
                DesenharRolagem();
            }, !rolou && !pedido);
            var opcoesAlvo = new List<(string, string)> { (AlvoNinguem, golpe ? "— (quem toma é quem rola)" : "Ninguém") };
            if (!golpe)
                opcoesAlvo.AddRange(mesa.EmCena().Concat(NaMesa().Where(h => h.Id != rolQuem)).Select(a => (a.Id, a.NomeExibicao)));
            CampoDaRolagem(campos, "CONTRA QUEM?", opcoesAlvo, rolAlvo ?? AlvoNinguem, id =>
            {
                rolAlvo = id == AlvoNinguem ? null : id;
                DesenharRolagem();
            }, !rolou && !golpe && !pedido);
            var opcoesMov = movimentos.Select(m => (m.Id, m.Nome)).ToList();
            CampoDaRolagem(campos, "MOVIMENTO", opcoesMov, movimento.Id, id =>
            {
                rolMovimento = Math.Max(0, movimentos.ToList().FindIndex(m => m.Id == id));
                DesenharRolagem();
            }, !rolou && !pedido);
            var pediu = pedido ? mesa.Eventos.FirstOrDefault(x => x.Id == rolOrigem) : null;
            if (pediu?.Rolagem != null)
            {
                var dequem = pediu.Personagem == null ? HeroiGenerico : personagens.Buscar(pediu.Personagem)?.NomeExibicao ?? "?";
                var contra = pediu.Rolagem.Alvo == null ? "" : " → " + (personagens.Buscar(pediu.Rolagem.Alvo)?.NomeExibicao ?? "?");
                Estilo.Rotulo(campos, $"Pedido por: {pediu.Rolagem.NomeDoMovimento} de {dequem}{contra} (r{pediu.Rodada})", 17,
                    Estilo.TextoSuave, FontStyles.Italic).textWrappingMode = TextWrappingModes.Normal;
            }
            else if (pediu?.Pedido != null)
                Estilo.Rotulo(campos, $"Pedido no log (r{pediu.Rodada})" + (string.IsNullOrWhiteSpace(pediu.Motivo) ? "" : ": " + pediu.Motivo), 17,
                    Estilo.TextoSuave, FontStyles.Italic).textWrappingMode = TextWrappingModes.Normal;
            // O que a rolagem soma, ao lado do botão (na lista ele espremia o nome do movimento).
            var filaRolar = Estilo.Fila(campos, 56, 12);
            // A conta antes de rolar, parte por parte: "2d6 + Perigoso +1  Assustado −2  = −1" (a condição em vermelho).
            var partes = mesa.PartesDoModificador(quem, movimento);
            var vermelho = ColorUtility.ToHtmlStringRGB(Estilo.Erro);
            var conta = "2d6 + " + string.Join("  ", partes.Select((x, k) =>
            {
                var nome = k == 0 && golpe ? "condições marcadas" : x.Texto;
                var parte = $"{nome} <b>{Sinal(x.Valor)}</b>";
                return x.Origem.StartsWith("condicao:") ? $"<color=#{vermelho}>{parte}</color>" : parte;
            }));
            var somaDasPartes = partes.Sum(x => x.Valor);
            var comTeto = Math.Max(Mesa.TetoMinimo, Math.Min(Mesa.TetoMaximo, somaDasPartes));
            if (partes.Count > 1 || comTeto != somaDasPartes)
                conta += $"  = <b>{Sinal(comTeto)}</b>" + (comTeto != somaDasPartes ? " <size=80%>(teto)</size>" : "");
            var rotuloDaRolagem = Estilo.Rotulo(filaRolar,
                conta + (quem == null ? "  <size=80%>(herói genérico)</size>" : ""), 22, Estilo.Dourado);
            rotuloDaRolagem.textWrappingMode = TextWrappingModes.Normal;
            Estilo.ColunaElastica(rotuloDaRolagem);
            if (rolou)
                Estilo.BotaoContorno(filaRolar, "Já rolou", 240, Estilo.TextoApagado, () => { });
            else
            {
                // Debug: "Rolar fixo" escolhe a soma dos dados (para seguir um exemplo do livro) e rola por ela.
                var fixo = Estilo.BotaoContorno(filaRolar, "Rolar fixo", 170, Estilo.TextoSuave, () => AbrirRolarFixo(movimento));
                fixo.gameObject.AddComponent<Dica>().Configurar("Debug: escolha quanto os dados dão (2 a 12) e a rolagem segue por esse valor, como o /fixo.");
                Estilo.BotaoPrincipal(filaRolar, "Rolar 2d6", 240, () => RolarMovimentoDoPainel(movimento));
            }

            // Com intervenção, o resultado do dado continua à vista ao lado do total: "10   8 + 2".
            var antes = string.Empty;
            if (rolou && evento.Rolagem.Intervencoes.Count > 0)
            {
                var soma = evento.Rolagem.Intervencoes.Sum(i => i.Mod);
                antes = $"   <color=#{ColorUtility.ToHtmlStringRGB(Estilo.Dourado)}><size=130%>{evento.Rolagem.Inicial} " +
                        $"{(soma < 0 ? "−" : "+")} {Math.Abs(soma)}</size></color>";
            }
            var tr = Estilo.Rotulo(resultado, $"RESULTADO PARCIAL:  <size=130%>{(total.HasValue ? total.ToString() : "—")}</size>{antes}", 22, Estilo.Texto, FontStyles.Bold);
            tr.gameObject.AddComponent<LayoutElement>().minHeight = 44;
            // Faixas invertidas (golpe poderoso): rolar alto é pior, então as cores trocam de lado.
            var (corAlta, corBaixa) = movimento.Invertido ? (Estilo.Erro, Estilo.Azul) : (Estilo.Azul, Estilo.Erro);
            var binario = Estilo.Fila(resultado, 34, 18);
            Marca(binario, "Sucesso", total >= 7, corAlta);
            Marca(binario, "Falha", total <= 6, corBaixa);
            var faixas = Estilo.Fila(resultado, 34, 18);
            Marca(faixas, "10+", total >= 10, corAlta);
            Marca(faixas, "7–9", total >= 7 && total <= 9, Estilo.Dourado);
            Marca(faixas, "6−", total <= 6, corBaixa);
            if (movimento.Invertido)
                Estilo.Rotulo(resultado, "Faixas invertidas: rolar alto é pior. Na falha, quem toma se mantém firme.", 16,
                    Estilo.Dourado, FontStyles.Italic).textWrappingMode = TextWrappingModes.Normal;
            if (rolou)
            {
                var somaIntervencoes = evento.Rolagem.Intervencoes.Sum(i => i.Mod);
                var nomeDoMod = evento.Rolagem.Partes.Count > 0 ? Mesa.TextoDasPartes(evento.Rolagem)
                    : evento.Rolagem.Rotulo != null ? Rotulos.Nome(evento.Rolagem.Rotulo) : "condições";
                var dados = Estilo.Rotulo(resultado, evento.Rolagem.Dados[0] + " + " + evento.Rolagem.Dados[1] +
                    (evento.Rolagem.Mod == 0 && evento.Rolagem.Partes.Count <= 1 ? "" : $" {Sinal(evento.Rolagem.Mod)} ({nomeDoMod})") +
                    (evento.Rolagem.Intervencoes.Count == 0 ? "" : $" {Sinal(somaIntervencoes)} (intervenção)"), 18, Estilo.TextoSuave);
                dados.textWrappingMode = TextWrappingModes.Normal;
            }

            // 2 · intervenção: depois do dado, antes das opções (o total ainda pode mudar)
            Estilo.Rotulo(esquerda, "2 · INTERVENÇÃO (se houver)", 22, Estilo.Dourado, FontStyles.Bold).characterSpacing = 3;
            var sec2 = Coluna(esquerda, 20, true, 8);
            sec2.GetComponent<LayoutElement>().flexibleHeight = 1;
            if (movimento.SemIntervencao)
                Estilo.Rotulo(sec2, $"{movimento.Nome} não aceita intervenção: o dado vale como caiu.", 16,
                    Estilo.TextoApagado, FontStyles.Italic).textWrappingMode = TextWrappingModes.Normal;
            else
                SecaoDeIntervencao(sec2, evento, rolou, movimento);

            // Efeito esperado: as linhas que não valem para o resultado ficam apagadas.
            Estilo.Rotulo(direita, "EFEITO ESPERADO", 24, Estilo.Texto, FontStyles.Bold).gameObject.AddComponent<LayoutElement>().minHeight = 36;
            Estilo.Rotulo(direita, movimento.Nome, 17, Estilo.TextoSuave, FontStyles.Italic).textWrappingMode = TextWrappingModes.Normal;
            // Depois de rolar, as opções da faixa viram botões de marcar (e as alternativas de dentro delas).
            var faixaDoResultado = !total.HasValue ? Core.Regras.MovimentosBasicos.Faixa.Falha
                : total >= 10 ? Core.Regras.MovimentosBasicos.Faixa.Forte
                : total >= 7 ? Core.Regras.MovimentosBasicos.Faixa.Parcial : Core.Regras.MovimentosBasicos.Faixa.Falha;
            var opcoes = rolou && !rolIntervindo ? movimento.OpcoesNo(faixaDoResultado) : new List<Core.Regras.MovimentosBasicos.OpcaoDeEscolha>();
            var pede = Math.Min(rolou ? movimento.EscolhasNo(faixaDoResultado) : 0, opcoes.Count);
            var textosDasOpcoes = new HashSet<string>(pede > 0 ? opcoes.Select(o => o.Texto) : Enumerable.Empty<string>());
            foreach (var linha in movimento.EfeitoEsperado())
            {
                if (linha.Nivel > 0 && textosDasOpcoes.Contains(linha.Texto))
                    continue; // aparece abaixo, como botão
                var vale = !total.HasValue || Core.Regras.MovimentosBasicos.Vale(linha.Faixa, total.Value);
                if (!vale)
                    continue;
                var t = Estilo.Rotulo(direita, (linha.Nivel > 0 ? new string(' ', linha.Nivel * 3) + "· " : string.Empty) + linha.Texto,
                    linha.Nivel == 0 ? 17 : 16, vale ? (linha.Nivel == 0 ? Estilo.Texto : Estilo.TextoSuave) : Estilo.TextoApagado,
                    linha.Nivel == 0 ? FontStyles.Bold : FontStyles.Normal);
                t.textWrappingMode = TextWrappingModes.Normal;
                if (!vale)
                    t.alpha = 0.45f;
            }
            if (pede > 0)
            {
                Estilo.Rotulo(direita, $"Escolhas ({RolagemDaMesa.NomeDoTipo(RolagemDaMesa.TipoDe(total.Value))}): {rolEscolhas.Count} de {pede}",
                    17, rolEscolhas.Count == pede ? Estilo.Azul : Estilo.Dourado, FontStyles.Bold).gameObject.AddComponent<LayoutElement>().minHeight = 30;
                foreach (var o in opcoes)
                {
                    var opcao = o;
                    var marcada = rolEscolhas.FirstOrDefault(x => x.Opcao == opcao.Id);
                    var fila = Estilo.Fila(direita, 46, 6);
                    UnityEngine.Events.UnityAction alternar = () =>
                    {
                        if (marcada != null)
                        {
                            rolEscolhas.Remove(marcada);
                            if (movimento.ReflectePercepcao(opcao.Id) && personagens.Buscar(rolAlvo ?? "")?.Papel == Papel.Heroi)
                                rolCondAlvo = null;
                        }
                        else if (rolEscolhas.Count < pede)
                            rolEscolhas.Add(NovaEscolha(movimento, opcao.Id));
                        else if (pede == 1)
                        {
                            rolEscolhas.Clear();
                            rolEscolhas.Add(NovaEscolha(movimento, opcao.Id));
                        }
                        else
                        {
                            Mostrar(Resultado.Falha($"Já marcou {pede}: desmarque uma antes."));
                            return;
                        }
                        DesenharRolagem();
                    };
                    var b = marcada != null
                        ? Estilo.BotaoPrincipal(fila, opcao.Texto, 0, alternar)
                        : Estilo.BotaoContorno(fila, opcao.Texto, 0, Estilo.Dourado, alternar);
                    b.GetComponent<LayoutElement>().flexibleWidth = 1;
                    b.name = TextoLivre;
                    TextoDeOpcao(b, 17);
                    if (marcada == null)
                        continue;
                    DetalhesDaEscolha(direita, movimento, opcao, marcada, quem, 20);
                }
            }
            var alvoDaRolagem = rolAlvo == null ? null : personagens.Buscar(rolAlvo);
            if (rolou && total <= 6 && !rolIntervindo && alvoDaRolagem?.Papel == Papel.Heroi)
                Estilo.Rotulo(direita, "PJ contra PJ: o MJ pode virar o movimento contra quem falhou (o alvo ganha o efeito como se tivesse tirado 10+).",
                    16, Estilo.Dourado, FontStyles.Italic).textWrappingMode = TextWrappingModes.Normal;
            if (rolIntervindo)
                Estilo.Rotulo(direita, "Intervenção aberta: o total ainda pode mudar. Termine a intervenção para marcar as opções e a troca.",
                    16, Estilo.Dourado, FontStyles.Italic).textWrappingMode = TextWrappingModes.Normal;
            else if (rolou && total >= 7 && movimento.TrocaGolpes)
                CartoesDaTroca(direita, movimento, quem);
            Estilo.Rect("Vao", direita).gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var acoes = Estilo.Fila(direita, 52, 12);
            if (!movimento.SemIntervencao)
            {
                var intervir = Estilo.BotaoContorno(acoes, "Intervir", 0, Estilo.Dourado, () =>
                {
                    if (!rolou)
                    {
                        Mostrar(Resultado.Falha("Role os dados antes: a intervenção vem depois do dado."));
                        return;
                    }
                    ComecarIntervencao();
                    DesenharRolagem();
                });
                intervir.GetComponent<LayoutElement>().flexibleWidth = 1;
            }
            var aceitar = Estilo.BotaoPrincipal(acoes, "Aceitar", 0, () =>
            {
                if (!rolou)
                {
                    Mostrar(Resultado.Falha("Role os dados antes de aceitar."));
                    return;
                }
                if (rolIntervindo)
                {
                    Mostrar(Resultado.Falha("Termine a intervenção antes de aceitar."));
                    return;
                }
                var r = mesa.AceitarRolagem(evento.Id, movimento, rolEscolhas,
                    new TrocaDeGolpes { CondicaoDoAlvo = rolCondAlvo, Retorno = rolRetorno, CondicaoDeQuemRolou = rolCondRolou });
                if (!r.Ok)
                {
                    Mostrar(r);
                    return;
                }
                FecharBatalha();
                // O golpe poderoso que a troca pediu fica como botão no log, sem abrir sozinho: a condição do vilão
                // interrompe a mesa com o movimento dela, e o MJ resolve na ordem que couber.
                Aplicar(r);
            });
            aceitar.GetComponent<LayoutElement>().flexibleWidth = 1;
            var fechar = Estilo.Fila(direita, 44, 12);
            var bf = Estilo.BotaoContorno(fechar, rolou ? "Fechar (fica pendente no log)" : "Cancelar", 0, Estilo.TextoSuave, FecharBatalha);
            bf.GetComponent<LayoutElement>().flexibleWidth = 1;
            Encolher(bf);
        }

        /// <summary>
        /// Rola o movimento do painel: recusa antes de jogar se falta o alvo; a caixa fecha enquanto os dados rolam (para
        /// vê-los) e volta com o resultado. Com dado fixo na fila (/fixo, "Rolar fixo"), os dados dão o valor pedido.
        /// </summary>
        async void RolarMovimentoDoPainel(Core.Regras.MovimentosBasicos.Movimento movimento)
        {
            if (movimento.PedeAlvo && rolAlvo == null)
            {
                Mostrar(Resultado.Falha(Mesa.MensagemPedeAlvo(movimento)));
                return;
            }
            var (rolador, alvo, origem) = (rolQuem, rolAlvo, rolOrigem);
            FecharBatalha();
            var d = await JogarDados();
            if (this == null || mesa == null)
                return;
            if (d == null)
            {
                Mostrar(Resultado.Falha("Os dados ainda estão rolando."));
                return;
            }
            var r = mesa.RolarMovimento(rolador, movimento, alvo, d.Value.Item1, d.Value.Item2, null, origem);
            Redesenhar();
            if (!r.Ok)
            {
                Mostrar(r);
                return;
            }
            rolEvento = mesa.Eventos.Last().Id;
            LimparEscolhas();
            DesenharRolagem();
        }

        /// <summary>
        /// Debug: a janelinha do "Rolar fixo". Clicar numa soma (2 a 12) fixa os dados nela e já rola; "Voltar" devolve
        /// ao painel sem rolar. O modificador soma por cima: "tira 8 com +1" no livro é 7 aqui.
        /// </summary>
        void AbrirRolarFixo(Core.Regras.MovimentosBasicos.Movimento movimento)
        {
            if (movimento.PedeAlvo && rolAlvo == null)
            {
                Mostrar(Resultado.Falha(Mesa.MensagemPedeAlvo(movimento)));
                return;
            }
            var caixa = Caixa("ROLAR FIXO (DEBUG)", 380, 820);
            Nota(caixa, "Quanto os dados dão? O modificador soma por cima (no livro, \"tira 8 com +1\" é 7 aqui).");
            foreach (var faixa in new[] { new[] { 2, 3, 4, 5, 6 }, new[] { 7, 8, 9 }, new[] { 10, 11, 12 } })
            {
                var fila = Estilo.Fila(caixa, 56, 8);
                foreach (var soma in faixa)
                {
                    var valor = soma;
                    var b = Estilo.BotaoContorno(fila, valor.ToString(), 0, soma >= 10 ? Estilo.Azul : soma >= 7 ? Estilo.Dourado : Estilo.Erro, () =>
                    {
                        mesa.FixarDados(new[] { valor });
                        RolarMovimentoDoPainel(movimento);
                    });
                    b.GetComponent<LayoutElement>().flexibleWidth = 1;
                }
            }
            var fim = Estilo.Fila(caixa, 52, 12);
            Estilo.Rect("Vao", fim).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Estilo.BotaoContorno(fim, "Voltar", 200, Estilo.TextoSuave, DesenharRolagem);
        }

        /// <summary>
        /// A seção 2 do painel: as intervenções já feitas (cada uma com o que mudou) e, aberta pelo Intervir, a linha
        /// para acrescentar outra (de onde, quem, modificador; Defender rola). O total do painel acompanha na hora.
        /// "Terminar intervenção" fecha e libera as opções.
        /// </summary>
        void SecaoDeIntervencao(Transform sec, Evento evento, bool rolou, Core.Regras.MovimentosBasicos.Movimento movimento)
        {
            if (!rolou)
            {
                Estilo.Rotulo(sec, "Depois do dado: ajuda da pilha, egoísta, renúncia de Influência, Defender, carta custom.", 16,
                    Estilo.TextoApagado, FontStyles.Italic).textWrappingMode = TextWrappingModes.Normal;
                return;
            }
            var r = evento.Rolagem;
            if (r.Intervencoes.Count == 0)
                Estilo.Rotulo(sec, rolIntervindo ? "Nenhuma intervenção ainda." : "Ninguém interveio.", 17, Estilo.TextoApagado, FontStyles.Italic);
            for (var k = 0; k < r.Intervencoes.Count; k++)
            {
                var indice = k;
                var i = r.Intervencoes[k];
                var fila = Estilo.Fila(sec, 36, 8);
                var nome = personagens.Buscar(i.Quem)?.NomeExibicao ?? i.Quem;
                var sinal = i.Mod > 0 ? $"+{i.Mod}" : i.Mod < 0 ? $"−{-i.Mod}" : "±0";
                var t = Estilo.Rotulo(fila, $"{nome} · {i.DeOnde}: <b>{sinal}</b>" +
                    (i.CustoEquipe > 0 ? $"  (−{i.CustoEquipe} Equipe)" : "") + (i.Rolagem != null ? $"  [{i.Rolagem}]" : ""), 18, Estilo.Texto);
                Estilo.ColunaElastica(t);
                if (rolIntervindo)
                    Estilo.BotaoContorno(fila, "Retirar", 130, Estilo.Erro, () =>
                    {
                        var res = mesa.RetirarIntervencao(evento.Id, indice);
                        Redesenhar();
                        Mostrar(res);
                        DesenharRolagem();
                    });
            }
            if (!rolIntervindo)
                return;

            // Três listas, na ordem da pergunta (como os campos da seção 1): quem, de onde, quanto. Todas as opções ficam
            // à mão; trocar a origem sugere o mod dela, e o mestre ajusta se for o caso.
            var origem = Origens[intervOrigem];
            var opcoesQuem = NaMesa().Concat(mesa.EmCena()).Select(p => (p.Id, p.NomeExibicao)).ToList();
            CampoDaRolagem(sec, "QUEM AJUDA?", opcoesQuem, intervQuem, id => { intervQuem = id; DesenharRolagem(); }, true);
            var opcoesOrigem = Origens.Select((o, k) => (k.ToString(), o.nome)).ToList();
            // A explicação da origem vai na dica flutuante: um texto fixo aqui era espremido a cada intervenção empilhada.
            CampoDaRolagem(sec, "DE ONDE VEM?", opcoesOrigem, intervOrigem.ToString(), id =>
            {
                intervOrigem = int.Parse(id);
                intervMod = Origens[intervOrigem].mod;
                intervRolagem = null;
                DesenharRolagem();
            }, true).gameObject.AddComponent<Dica>().Configurar(origem.dica);
            var opcoesMod = Enumerable.Range(Mesa.IntervencaoMinima, Mesa.IntervencaoMaxima - Mesa.IntervencaoMinima + 1)
                .Select(m => (m.ToString(), m > 0 ? $"+{m}" : m < 0 ? $"−{-m}" : "0")).ToList();
            CampoDaRolagem(sec, "MOD", opcoesMod, intervMod.ToString(), id => { intervMod = int.Parse(id); DesenharRolagem(); }, true);
            if (origem.rola)
            {
                var filaR = Estilo.Fila(sec, 40, 8);
                var modAnterior = campoModDefensorDoPainel != null ? campoModDefensorDoPainel.text : string.Empty;
                campoModDefensorDoPainel = Estilo.CampoTexto(filaR, "mod de quem defende");
                campoModDefensorDoPainel.text = modAnterior;
                campoModDefensorDoPainel.gameObject.AddComponent<LayoutElement>().preferredWidth = 220;
                Estilo.BotaoContorno(filaR, "Rolar 2d6", 160, Estilo.Dourado, async () =>
                {
                    int.TryParse((campoModDefensorDoPainel.text ?? string.Empty).Replace("+", string.Empty).Trim(), out var md);
                    md = Math.Max(Mesa.TetoMinimo, Math.Min(Mesa.TetoMaximo, md));
                    FecharBatalha(); // os dados aparecem na mesa
                    var d = await JogarDados();
                    if (this == null || mesa == null)
                        return;
                    if (d != null)
                    {
                        var (a, b2) = d.Value;
                        var soma = a + b2 + md;
                        intervRolagem = $"{a} + {b2}{(md == 0 ? "" : $" {(md > 0 ? "+" : "−")} {Math.Abs(md)}")} = {soma} ({RolagemDaMesa.Faixa(soma)})";
                    }
                    else
                        Mostrar(Resultado.Falha("Os dados ainda estão rolando."));
                    DesenharRolagem();
                });
                var tr = Estilo.Rotulo(filaR, intervRolagem == null ? "ainda não rolou" : "defesa: " + intervRolagem, 16,
                    intervRolagem == null ? Estilo.TextoApagado : Estilo.Texto);
                Estilo.ColunaElastica(tr);
            }
            else
                campoModDefensorDoPainel = null;

            var fim = Estilo.Fila(sec, 46, 10);
            Estilo.BotaoPrincipal(fim, "Adicionar", 200, () =>
            {
                if (origem.rola && intervRolagem == null)
                {
                    Mostrar(Resultado.Falha("Role a defesa antes (quem defende)."));
                    return;
                }
                var res = mesa.Intervir(evento.Id, new Intervencao
                {
                    Quem = intervQuem,
                    DeOnde = origem.nome,
                    Mod = intervMod,
                    CustoEquipe = origem.custo,
                    Rolagem = origem.rola ? intervRolagem : null,
                }, movimento);
                if (!res.Ok)
                {
                    Mostrar(res);
                    return;
                }
                intervQuem = null;
                intervRolagem = null;
                Redesenhar(); // a pilha e o log mudam
                DesenharRolagem();
            });
            Estilo.Rect("Vao", fim).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Estilo.BotaoContorno(fim, "Terminar intervenção", 280, Estilo.Azul, () =>
            {
                rolIntervindo = false;
                DesenharRolagem();
            });
        }

        /// <summary>
        /// Os dois cartões da troca de golpes (livro p.57): o alvo NPC marca 1 condição (com "impressionar", uma que
        /// mostre a nova percepção); quem rolou recebe o retorno que o MJ escolher, condição OU golpe poderoso, ou
        /// sai ileso com "resistir". Contra PJ, os dois tomam golpe poderoso.
        /// </summary>
        void CartoesDaTroca(Transform pai, Core.Regras.MovimentosBasicos.Movimento movimento, Personagem quem)
        {
            var alvo = rolAlvo == null ? null : personagens.Buscar(rolAlvo);
            var ileso = rolEscolhas.Any(x => movimento.AnulaRetorno(x.Opcao));
            var percepcao = rolEscolhas.Any(x => movimento.ReflectePercepcao(x.Opcao));
            Estilo.Rotulo(pai, "TROCA DE GOLPES", 17, Estilo.Texto, FontStyles.Bold).gameObject.AddComponent<LayoutElement>().minHeight = 28;

            if (alvo != null)
            {
                var c = CartaoDaTroca(pai, alvo, alvo.NomeExibicao);
                if (alvo.Papel == Papel.Heroi)
                {
                    LinhaDoCartao(c, "toma um golpe poderoso", Estilo.Dourado);
                    if (percepcao)
                    {
                        // p.58: contra PJ, o MJ pode dar uma condição pela nova percepção (opcional; clicar de novo tira).
                        LinhaDoCartao(c, "impressionar: o MJ pode dar uma condição pela nova percepção (opcional)", Estilo.TextoSuave);
                        var f = Estilo.Fila(c, 32, 6);
                        foreach (var cond in Condicoes.Todas.Where(x => !alvo.CondicoesMarcadas.Contains(x)))
                        {
                            var id = cond;
                            UnityEngine.Events.UnityAction alternar = () =>
                            {
                                rolCondAlvo = rolCondAlvo == id ? null : id;
                                DesenharRolagem();
                            };
                            var b = rolCondAlvo == id
                                ? Estilo.BotaoPrincipal(f, Condicoes.Nome(cond), 0, alternar)
                                : Estilo.BotaoContorno(f, Condicoes.Nome(cond), 0, Estilo.Dourado, alternar);
                            b.GetComponent<LayoutElement>().flexibleWidth = 1;
                            Encolher(b);
                        }
                    }
                }
                else
                {
                    var marcadas = mesa.CondicoesMarcadasDoVilao(alvo.Id);
                    var livres = (alvo.Vilao?.Condicoes ?? new List<string>()).Where(x => !marcadas.Contains(x)).ToList();
                    var padrao = movimento.CondicaoPadraoDoAlvo;
                    LinhaDoCartao(c, livres.Count == 0 ? "já marcou todas as condições: está no limite"
                        : percepcao ? "marca 1 condição: uma que mostre a nova percepção (impressionar)"
                        : "marca 1 condição (o MJ escolhe" + (padrao != null ? $"; de costume, {Condicoes.Nome(padrao)})" : ")"),
                        livres.Count == 0 ? Estilo.Erro : Estilo.TextoSuave);
                    if (livres.Count > 0)
                    {
                        var f = Estilo.Fila(c, 32, 6);
                        foreach (var cond in livres)
                        {
                            var id = cond;
                            Escolha(f, Condicoes.Nome(cond), rolCondAlvo == id, () => { rolCondAlvo = id; DesenharRolagem(); });
                        }
                    }
                }
            }

            var cq = CartaoDaTroca(pai, quem, quem?.NomeExibicao ?? HeroiGenerico);
            if (ileso)
                LinhaDoCartao(cq, "SEM RETORNO: escolheu resistir ou evitar os golpes", Estilo.Azul);
            else if (alvo != null && alvo.Papel == Papel.Heroi)
                LinhaDoCartao(cq, "toma um golpe poderoso", Estilo.Dourado);
            else
            {
                LinhaDoCartao(cq, "retorno (o MJ escolhe um):", Estilo.TextoSuave);
                var f = Estilo.Fila(cq, 32, 6);
                Escolha(f, "condição", rolRetorno == TrocaDeGolpes.RetornoCondicao, () =>
                {
                    rolRetorno = TrocaDeGolpes.RetornoCondicao;
                    DesenharRolagem();
                });
                Escolha(f, "golpe poderoso", rolRetorno == TrocaDeGolpes.RetornoGolpePoderoso, () =>
                {
                    rolRetorno = TrocaDeGolpes.RetornoGolpePoderoso;
                    rolCondRolou = null;
                    DesenharRolagem();
                });
                if (rolRetorno == TrocaDeGolpes.RetornoCondicao)
                {
                    var f2 = Estilo.Fila(cq, 32, 6);
                    foreach (var cond in Condicoes.Todas.Where(x => quem == null || !quem.CondicoesMarcadas.Contains(x)))
                    {
                        var id = cond;
                        Escolha(f2, Condicoes.Nome(cond), rolCondRolou == id, () => { rolCondRolou = id; DesenharRolagem(); });
                    }
                }
            }
        }

        /// <summary>
        /// Dentro de uma opção marcada: quais condições ela marca em quem rolou ("aguentar a dor": duas, entre as livres e
        /// sem repetir as de outra escolha) e a escolha de dentro ("duas opções da lista de 7–9"), cada uma com os
        /// detalhes dela, mais recuada.
        /// </summary>
        void DetalhesDaEscolha(Transform pai, Core.Regras.MovimentosBasicos.Movimento movimento,
            Core.Regras.MovimentosBasicos.OpcaoDeEscolha opcao, EscolhaDaRolagem marcada, Personagem quem, float recuo)
        {
            // As alternativas de dentro da opção (a oportunidade: aliado ou +1 Equipe; descontar num colega: provocar ou
            // Influência), uma marcada.
            if (opcao.Alternativas.Count > 0)
            {
                var filaAlt = Estilo.Fila(pai, 34, 6);
                Estilo.Rect("Recuo", filaAlt).gameObject.AddComponent<LayoutElement>().preferredWidth = recuo;
                var contraPj = personagens.Buscar(rolAlvo ?? "")?.Papel == Papel.Heroi;
                for (var k = 0; k < opcao.Alternativas.Count; k++)
                {
                    if (contraPj && !movimento.AlternativaValeContraPj(opcao.Id, k))
                        continue; // contra PJ, nada de encher a pilha
                    var alternativa = k;
                    var ba = Escolha(filaAlt, opcao.Alternativas[k], marcada.Subopcao == k, () =>
                    {
                        marcada.Subopcao = alternativa;
                        DesenharRolagem();
                    });
                    ba.GetComponentInChildren<TextMeshProUGUI>().fontSize = 15;
                }
            }
            // "Ceder terreno": quem ganha a abertura recebe o lembrete no cartão ("ninguém" = o mestre anota à mão).
            if (movimento.LembreteNaOposicao(marcada.Opcao) != null)
            {
                var opcoesPara = new List<(string, string)> { (ParaNinguem, "ninguém (anoto à mão)") };
                opcoesPara.AddRange(mesa.EmCena().Concat(NaMesa().Where(h => h.Id != rolQuem)).Select(a => (a.Id, a.NomeExibicao)));
                var f = Estilo.Fila(pai, 44, 8);
                Estilo.Rect("Recuo", f).gameObject.AddComponent<LayoutElement>().preferredWidth = recuo;
                Estilo.Rotulo(f, "quem ganha a abertura?", 15, Estilo.TextoSuave).gameObject.AddComponent<LayoutElement>().preferredWidth = 190;
                var lista = Estilo.ListaSuspensa(f, 260, opcoesPara, marcada.Para ?? ParaNinguem, id =>
                {
                    marcada.Para = id == ParaNinguem ? null : id;
                    DesenharRolagem();
                });
                Estilo.ColunaElastica(lista);
            }

            var n = movimento.CondicoesQueMarca(marcada.Opcao);
            if (n > 0)
            {
                marcada.Condicoes ??= new List<string>();
                var usadas = rolEscolhas.SelectMany(x => x.ComAsDeDentro).Where(x => x != marcada)
                    .SelectMany(x => x.Condicoes ?? new List<string>()).ToList();
                var livres = Condicoes.Todas.Where(c => quem?.CondicoesMarcadas.Contains(c) != true && !usadas.Contains(c)).ToList();
                marcada.Condicoes.RemoveAll(c => !livres.Contains(c));
                var cabe = Math.Min(n, livres.Count);
                var aviso = Estilo.Fila(pai, 26, 6);
                Estilo.Rect("Recuo", aviso).gameObject.AddComponent<LayoutElement>().preferredWidth = recuo;
                Estilo.ColunaElastica(Estilo.Rotulo(aviso, cabe < n
                        ? $"quais condições? {marcada.Condicoes.Count} de {cabe} (faltam livres: sai de cena)"
                        : $"quais condições? {marcada.Condicoes.Count} de {n}", 15,
                    marcada.Condicoes.Count == cabe ? Estilo.Azul : Estilo.Dourado));
                var f = Estilo.Fila(pai, 32, 6);
                Estilo.Rect("Recuo", f).gameObject.AddComponent<LayoutElement>().preferredWidth = recuo;
                foreach (var cond in livres)
                {
                    var id = cond;
                    var marcadaJa = marcada.Condicoes.Contains(id);
                    UnityEngine.Events.UnityAction alternar = () =>
                    {
                        if (marcadaJa)
                            marcada.Condicoes.Remove(id);
                        else if (marcada.Condicoes.Count < cabe)
                            marcada.Condicoes.Add(id);
                        else if (cabe == 1)
                            marcada.Condicoes = new List<string> { id };
                        else
                        {
                            Mostrar(Resultado.Falha($"Já marcou {cabe}: desmarque uma antes."));
                            return;
                        }
                        DesenharRolagem();
                    };
                    var b = marcadaJa
                        ? Estilo.BotaoPrincipal(f, Condicoes.Nome(cond), 0, alternar)
                        : Estilo.BotaoContorno(f, Condicoes.Nome(cond), 0, Estilo.Dourado, alternar);
                    b.GetComponent<LayoutElement>().flexibleWidth = 1;
                    Encolher(b);
                }
            }

            var (nDentro, dentro) = movimento.OpcoesDentro(marcada.Opcao);
            if (dentro.Count == 0)
                return;
            marcada.Dentro ??= new List<EscolhaDaRolagem>();
            var pede = Math.Min(nDentro, dentro.Count);
            var cab = Estilo.Fila(pai, 26, 6);
            Estilo.Rect("Recuo", cab).gameObject.AddComponent<LayoutElement>().preferredWidth = recuo;
            Estilo.ColunaElastica(Estilo.Rotulo(cab, $"escolha {pede}: {marcada.Dentro.Count} de {pede}", 15,
                marcada.Dentro.Count == pede ? Estilo.Azul : Estilo.Dourado));
            foreach (var o in dentro)
            {
                var deDentro = o;
                var ja = marcada.Dentro.FirstOrDefault(x => x.Opcao == deDentro.Id);
                var f = Estilo.Fila(pai, 44, 6);
                Estilo.Rect("Recuo", f).gameObject.AddComponent<LayoutElement>().preferredWidth = recuo;
                UnityEngine.Events.UnityAction alternar = () =>
                {
                    if (ja != null)
                        marcada.Dentro.Remove(ja);
                    else if (marcada.Dentro.Count < pede)
                        marcada.Dentro.Add(NovaEscolha(movimento, deDentro.Id));
                    else
                    {
                        Mostrar(Resultado.Falha($"Já marcou {pede}: desmarque uma antes."));
                        return;
                    }
                    DesenharRolagem();
                };
                var b = ja != null
                    ? Estilo.BotaoPrincipal(f, deDentro.Texto, 0, alternar)
                    : Estilo.BotaoContorno(f, deDentro.Texto, 0, Estilo.Dourado, alternar);
                b.GetComponent<LayoutElement>().flexibleWidth = 1;
                b.name = TextoLivre;
                TextoDeOpcao(b, 15);
                if (ja != null)
                    DetalhesDaEscolha(pai, movimento, deDentro, ja, quem, recuo + 20);
            }
        }

        const string ParaNinguem = "_ninguem";

        /// <summary>
        /// Uma escolha recém-marcada. Se a opção põe lembrete em quem ganha com ela ("ceder terreno"), já vem com a
        /// oposição sugerida: no golpe pedido pela troca, quem estava do outro lado; senão, o vilão, se há um só em cena.
        /// </summary>
        EscolhaDaRolagem NovaEscolha(Core.Regras.MovimentosBasicos.Movimento movimento, string opcao)
        {
            var escolha = new EscolhaDaRolagem { Opcao = opcao };
            if (movimento.LembreteNaOposicao(opcao) == null)
                return escolha;
            var pediu = rolOrigem == null ? null : mesa.Eventos.FirstOrDefault(x => x.Id == rolOrigem)?.Rolagem;
            var dono = rolOrigem == null ? null : mesa.Eventos.FirstOrDefault(x => x.Id == rolOrigem)?.Personagem;
            if (pediu?.Alvo != null)
                escolha.Para = pediu.Alvo == rolQuem ? dono : pediu.Alvo; // no PvP, o alvo que toma o golpe cede para quem rolou
            else
            {
                var viloes = mesa.EmCena();
                escolha.Para = viloes.Count == 1 ? viloes[0].Id : null;
            }
            if (escolha.Para == rolQuem)
                escolha.Para = null;
            return escolha;
        }

        /// <summary>Um cartão da troca: retrato à esquerda, nome e o que acontece à direita. Devolve a coluna da direita.</summary>
        RectTransform CartaoDaTroca(Transform pai, Personagem p, string nome)
        {
            var linha = Estilo.Rect("Cartao", pai);
            Estilo.Moldura(linha.gameObject, Estilo.Linha, Estilo.BordaApagada);
            var h = linha.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(10, 10, 8, 8);
            h.spacing = 10;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            linha.gameObject.AddComponent<LayoutElement>().minHeight = 70;
            if (p != null)
                Retrato(linha, p);
            var coluna = Coluna(linha, 0, false, 4);
            coluna.GetComponent<LayoutElement>().flexibleWidth = 1;
            Estilo.Rotulo(coluna, nome, 18, Estilo.Texto, FontStyles.Bold).gameObject.AddComponent<LayoutElement>().minHeight = 24;
            return coluna;
        }

        static void LinhaDoCartao(Transform cartao, string texto, Color cor)
        {
            var t = Estilo.Rotulo(cartao, texto, 15, cor);
            t.textWrappingMode = TextWrappingModes.Normal;
        }

        /// <summary>Uma linha do painel: o rótulo em maiúsculas e a lista suspensa (devolvida, para a dica).</summary>
        Button CampoDaRolagem(Transform pai, string rotulo, IReadOnlyList<(string id, string nome)> opcoes, string atual,
            UnityEngine.Events.UnityAction<string> aoMudar, bool editavel)
        {
            var fila = Estilo.Fila(pai, 52, 14);
            Estilo.Rotulo(fila, rotulo, 21, Estilo.Texto, FontStyles.Bold).gameObject.AddComponent<LayoutElement>().preferredWidth = 210;
            var lista = Estilo.ListaSuspensa(fila, 420, opcoes, atual, aoMudar, editavel);
            Estilo.ColunaElastica(lista);
            return lista;
        }

        /// <summary>O "rádio" do resultado: aceso na cor da faixa quando vale, apagado quando não (ou antes de rolar).</summary>
        static void Marca(Transform fila, string texto, bool acesa, Color cor)
        {
            var t = Estilo.Rotulo(fila, (acesa ? "● " : "○ ") + texto, 20, acesa ? cor : Estilo.TextoApagado,
                acesa ? FontStyles.Bold : FontStyles.Normal);
            t.gameObject.AddComponent<LayoutElement>().preferredWidth = 110;
        }

        /// <summary>Coluna vertical (com moldura opcional) para montar painéis em blocos.</summary>
        static RectTransform Coluna(Transform pai, int margem, bool moldura, float espaco)
        {
            var r = Estilo.Rect("Coluna", pai);
            if (moldura)
                Estilo.Moldura(r.gameObject, Estilo.Campo, Estilo.BordaApagada);
            var v = r.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(margem, margem, margem, margem);
            v.spacing = espaco;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            r.gameObject.AddComponent<LayoutElement>();
            return r;
        }

        /// <summary>Botão de escolha numa fila: o marcado é cheio, os outros de contorno.</summary>
        Button Escolha(Transform fila, string texto, bool marcada, UnityEngine.Events.UnityAction acao)
        {
            var b = marcada
                ? Estilo.BotaoPrincipal(fila, texto, 0, () => { })
                : Estilo.BotaoContorno(fila, texto, 0, Estilo.Dourado, acao);
            b.GetComponent<LayoutElement>().flexibleWidth = 1;
            Encolher(b);
            return b;
        }

        // ---------- intervir numa rolagem (depois do dado) ----------

        /// <summary>
        /// De onde vem a intervenção: o modificador e o custo na pilha sugeridos (o mestre ajusta o modificador)
        /// e se ela pede uma segunda rolagem (Defender).
        /// </summary>
        static readonly (string nome, int mod, int custo, bool rola, string dica)[] Origens =
        {
            (Intervencao.DaPilha, 1, 1, false, "Gasta 1 da pilha. Outra pessoa que alcança a cena: ajuda, +1 (o Farol dá +2). " +
                "Quem rola: egoísta (a mesa confirma a mudança de Rótulos, feita na ficha; o resultado só sobe se o Rótulo rolado subir: ajuste o modificador)."),
            ("renúncia de Influência", 1, 0, false, "Quem rola abre mão da Influência sobre o alvo: +1. Quem tem Influência sobre quem rola abre mão dela: −2."),
            ("defender", -2, 0, true, "PvP: quem defende rola; pelo resultado, escolha o efeito (−2 antes de o resultado valer)."),
            ("outro", 1, 0, false, "Carta custom ou carta do mestre (fim do cap. 10): de −2 a +2."),
        };

        string intervQuem;
        int intervOrigem;
        int intervMod = 1;
        string intervRolagem;
        TMP_InputField campoDetalhe, campoModDefensor;
        readonly System.Random sorteio = new System.Random();

        void AbrirIntervencao(Evento e)
        {
            intervQuem = null;
            intervOrigem = 0;
            intervMod = Origens[0].mod;
            intervRolagem = null;
            DesenharIntervencao(e, string.Empty, string.Empty);
        }

        void DesenharIntervencao(Evento e, string detalhe, string modDefensor)
        {
            var r = e.Rolagem;
            var origem = Origens[intervOrigem];
            var caixa = Caixa($"INTERVIR  ·  agora {r.Total} ({RolagemDaMesa.Faixa(r.Total)})",
                700 + r.Intervencoes.Count * 46 + (origem.rola ? 110 : 0), 1150);
            Nota(caixa, e.Descricao);
            void Redesenhar2() => DesenharIntervencao(e, campoDetalhe.text, campoModDefensor != null ? campoModDefensor.text : modDefensor);

            for (var k = 0; k < r.Intervencoes.Count; k++)
            {
                var indice = k;
                var i = r.Intervencoes[k];
                var fila = Estilo.Fila(caixa, 38, 8);
                var nome = personagens.Buscar(i.Quem)?.NomeExibicao ?? i.Quem;
                var t = Estilo.Rotulo(fila, $"{(i.Mod > 0 ? "+" : i.Mod < 0 ? "−" : "±")}{Math.Abs(i.Mod)}  {nome} ({i.DeOnde})", 17, Estilo.TextoSuave);
                Estilo.ColunaElastica(t);
                Estilo.BotaoContorno(fila, "Retirar", 140, Estilo.Erro, () =>
                {
                    var res = mesa.RetirarIntervencao(e.Id, indice);
                    Mostrar(res);
                    if (res.Ok)
                        Redesenhar2();
                });
            }

            Pergunta(caixa, "De onde?");
            var filaO = Estilo.Fila(caixa, 48, 8);
            for (var k = 0; k < Origens.Length; k++)
            {
                var alvo = k;
                var b = k == intervOrigem
                    ? Estilo.BotaoPrincipal(filaO, Origens[k].nome, 0, () => { })
                    : Estilo.BotaoContorno(filaO, Origens[k].nome, 0, Estilo.Dourado, () =>
                    {
                        intervOrigem = alvo;
                        intervMod = Origens[alvo].mod;
                        intervRolagem = null;
                        Redesenhar2();
                    });
                b.GetComponent<LayoutElement>().flexibleWidth = 1;
                Encolher(b);
            }
            Nota(caixa, origem.dica + (origem.custo > 0 ? $"  (pilha: {mesa.Equipe})" : string.Empty));

            Pergunta(caixa, "Quem intervém?");
            var filaP = Estilo.Fila(caixa, 48, 8);
            foreach (var p in NaMesa().Concat(mesa.EmCena()))
            {
                var id = p.Id;
                var b = id == intervQuem
                    ? Estilo.BotaoPrincipal(filaP, p.NomeExibicao, 0, () => { intervQuem = null; Redesenhar2(); })
                    : Estilo.BotaoContorno(filaP, p.NomeExibicao, 0, Estilo.Dourado, () => { intervQuem = id; Redesenhar2(); });
                b.GetComponent<LayoutElement>().flexibleWidth = 1;
                Encolher(b);
            }

            if (origem.rola)
            {
                Pergunta(caixa, "A segunda rolagem (quem defende)");
                var filaR = Estilo.Fila(caixa, 48, 8);
                campoModDefensor = Estilo.CampoTexto(filaR, "mod de quem defende (ex.: +1)");
                campoModDefensor.text = modDefensor;
                Estilo.ColunaElastica(campoModDefensor);
                Estilo.BotaoContorno(filaR, "Rolar 2d6", 200, Estilo.Dourado, async () =>
                {
                    int.TryParse((campoModDefensor.text ?? string.Empty).Replace("+", string.Empty).Trim(), out var m);
                    m = Math.Max(Mesa.TetoMinimo, Math.Min(Mesa.TetoMaximo, m));
                    var d = await JogarDados();
                    if (this == null || dialogo == null)
                        return;
                    if (d == null)
                    {
                        Mostrar(Resultado.Falha("Os dados ainda estão rolando."));
                        return;
                    }
                    var (a, b2) = d.Value;
                    var total = a + b2 + m;
                    var modTexto = m == 0 ? string.Empty : $" {(m > 0 ? "+" : "−")} {Math.Abs(m)}";
                    intervRolagem = $"{a} + {b2}{modTexto} = {total} ({RolagemDaMesa.Faixa(total)})";
                    Redesenhar2();
                });
                Nota(caixa, intervRolagem == null ? "Ainda não rolou." : "Rolou: " + intervRolagem);
            }
            else
                campoModDefensor = null;

            Pergunta(caixa, "Modificador (depois do dado não tem teto)");
            var filaM = Estilo.Fila(caixa, 48, 8);
            for (var m = Mesa.IntervencaoMinima; m <= Mesa.IntervencaoMaxima; m++)
            {
                var alvo = m;
                var texto = m > 0 ? $"+{m}" : m < 0 ? $"−{-m}" : "0";
                var b = m == intervMod
                    ? Estilo.BotaoPrincipal(filaM, texto, 0, () => { })
                    : Estilo.BotaoContorno(filaM, texto, 0, Estilo.Dourado, () => { intervMod = alvo; Redesenhar2(); });
                b.GetComponent<LayoutElement>().flexibleWidth = 1;
            }

            campoDetalhe = CampoMotivo(caixa, "detalhe (opcional): qual carta, o que fez");
            campoDetalhe.text = detalhe;
            Rodape(caixa, "Intervir", () =>
            {
                if (origem.rola && intervRolagem == null)
                {
                    Mostrar(Resultado.Falha("Role a segunda rolagem antes (quem defende)."));
                    return;
                }
                var res = mesa.Intervir(e.Id, new Intervencao
                {
                    Quem = intervQuem,
                    DeOnde = origem.nome,
                    Mod = intervMod,
                    CustoEquipe = origem.custo,
                    Rolagem = origem.rola ? intervRolagem : null,
                    Detalhe = campoDetalhe.text,
                });
                if (!res.Ok)
                {
                    Mostrar(res);
                    return;
                }
                FecharBatalha();
                Aplicar(res);
            });
        }

        /// <summary>Joga 2d6: os dados físicos, se estiverem na cena; senão, sorteio. Null se já estão rolando.</summary>
        async Task<(int, int)?> JogarDados()
        {
            var mesaDaRolagem = mesa;
            var fisicos = FindAnyObjectByType<SobreposicaoDados>();
            bool temVisual = fisicos != null && fisicos.isActiveAndEnabled;
            if (temVisual && fisicos.EmRolagem)
                return null;
            bool fixo = mesa.TirarDadoFixo(out var fixo1, out var fixo2);
            if (!temVisual)
                return fixo ? (fixo1, fixo2) : (sorteio.Next(1, 7), sorteio.Next(1, 7));
            try
            {
                var d = fixo ? await fisicos.Rolar2D6ForcadoAsync(fixo1, fixo2)
                    : await fisicos.Rolar2D6Async();
                return (d.Dado1, d.Dado2);
            }
            catch (OperationCanceledException)
            {
                if (fixo)
                    mesaDaRolagem.FixarDados(new[] { fixo1 + fixo2 }.Concat(mesaDaRolagem.DadosFixos));
                return null;
            }
            catch (Exception erro)
            {
                if (fixo)
                    mesaDaRolagem.FixarDados(new[] { fixo1 + fixo2 }.Concat(mesaDaRolagem.DadosFixos));
                Debug.LogException(erro);
                if (this != null)
                    Mostrar(Resultado.Falha("Nao foi possivel concluir a animacao dos dados. " + erro.Message));
                return null;
            }
        }

        /// <summary>
        /// Gerir a pilha (telinha): ajuste rápido +1/−1 com motivo opcional, e as três ações do livro, cada uma
        /// na sua caixa: usar a pilha, o questionário (entrar em batalha) e a passagem do tempo.
        /// </summary>
        void AbrirGestaoDaPilha()
        {
            var caixa = Caixa($"GERIR PILHA  ·  {mesa.Equipe} ponto{(mesa.Equipe == 1 ? "" : "s")}", 520);
            Pergunta(caixa, "Ajuste rápido (o mestre corrigindo, ou um movimento que deu/tirou ponto)");
            var filaA = Estilo.Fila(caixa, 52, 10);
            var motivo = Estilo.CampoTexto(filaA, "motivo (opcional)");
            Estilo.ColunaElastica(motivo);
            Estilo.BotaoContorno(filaA, "−1", 90, Estilo.Dourado, () => Ajustar(-1, motivo.text));
            Estilo.BotaoContorno(filaA, "+1", 90, Estilo.Dourado, () => Ajustar(1, motivo.text));

            Pergunta(caixa, "Ações do livro");
            var filaB = Estilo.Fila(caixa, 60, 10);
            foreach (var (texto, dica, acao) in new (string, string, UnityEngine.Events.UnityAction)[]
                     {
                         ("Usar pilha", "Gastar pontos: ajudar, agir de forma egoísta, ou movimento que usa mais de um (ex.: Farol).", AbrirUso),
                         ("Questionário", "Entrar em batalha como equipe: a conta soma ao que já está na pilha.", AbrirBatalha),
                         ("Passagem do tempo", "Início da sessão (+1) ou o tempo passa (volta para 1).", AbrirTempo),
                     })
            {
                var b = Estilo.BotaoPrincipal(filaB, texto, 0, acao);
                b.GetComponent<LayoutElement>().flexibleWidth = 1;
                Encolher(b);
                b.gameObject.AddComponent<Dica>().Configurar(dica);
            }
            var fim = Estilo.Fila(caixa, 56, 12);
            Estilo.Rect("Vao", fim).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Estilo.BotaoContorno(fim, "Fechar", 200, Estilo.TextoSuave, FecharBatalha);
        }

        void Ajustar(int delta, string motivo)
        {
            var r = mesa.MudarEquipe(delta, delta > 0 ? "+1 Equipe" : "−1 Equipe", motivo);
            Aplicar(r);
            if (r.Ok)
                AbrirGestaoDaPilha(); // a telinha fica aberta, com o número novo
        }

        bool ajustarTextos;
        const string TextoLivre = "TextoLivre";

        /// <summary>
        /// Depois de montar (mesa ou caixa), todo botão do painel ganha o texto numa linha que encolhe até
        /// caber: nome comprido ("Garça Escarlate", "Passagem do tempo") não vaza para fora.
        /// </summary>
        void LateUpdate()
        {
            if (!ajustarTextos)
                return;
            ajustarTextos = false;
            foreach (var b in GetComponentsInChildren<Button>(true))
                if (b.name != TextoLivre)
                    Encolher(b);
        }

        /// <summary>Botão que divide a linha com os outros (largura elástica).</summary>
        static Button Elastico(Button b)
        {
            var le = b.GetComponent<LayoutElement>();
            le.preferredWidth = 0;
            le.flexibleWidth = 1;
            return b;
        }

        /// <summary>
        /// Rótulo de opção do resultado (frase longa, como "Descontar num colega: ..."): até duas linhas, alinhado à
        /// esquerda, a fonte encolhe até caber. Numa linha só, nem a fonte mínima cabia e o texto vazava do botão.
        /// </summary>
        static void TextoDeOpcao(Button b, float tamanhoMaximo)
        {
            var r = b.GetComponentInChildren<TextMeshProUGUI>();
            r.textWrappingMode = TextWrappingModes.Normal;
            r.overflowMode = TextOverflowModes.Truncate;
            r.enableAutoSizing = true;
            r.fontSizeMin = 11;
            r.fontSizeMax = tamanhoMaximo;
            r.characterSpacing = 1;
            r.lineSpacing = -10;
            r.alignment = TextAlignmentOptions.MidlineLeft;
            r.rectTransform.offsetMin = new Vector2(12, 2);
            r.rectTransform.offsetMax = new Vector2(-8, -2);
        }

        /// <summary>Rótulo do botão numa linha só: a fonte encolhe até caber.</summary>
        static void Encolher(Button b)
        {
            var r = b.GetComponentInChildren<TextMeshProUGUI>();
            r.textWrappingMode = TextWrappingModes.NoWrap;
            r.enableAutoSizing = true;
            r.fontSizeMin = 12;
            r.fontSizeMax = 22;
            r.characterSpacing = 1;
            r.rectTransform.offsetMin = new Vector2(6, 0);
            r.rectTransform.offsetMax = new Vector2(-6, 0);
        }

        /// <summary>O retrato ao lado do nome: a foto heroica (se não foi recusada) ou a arte do arquétipo.</summary>
        void Retrato(Transform pai, Personagem p)
        {
            var moldura = Estilo.Rect("Retrato", pai);
            var lm = moldura.gameObject.AddComponent<LayoutElement>();
            lm.minWidth = lm.preferredWidth = 52;
            lm.flexibleWidth = 0;
            lm.layoutPriority = 2;
            Estilo.Moldura(moldura.gameObject, Estilo.Campo, Estilo.Dourado);
            var miolo = Estilo.Rect("Imagem", moldura);
            Estilo.Esticar(miolo);
            miolo.offsetMin = new Vector2(2, 2);
            miolo.offsetMax = new Vector2(-2, -2);
            var imagem = miolo.gameObject.AddComponent<RawImage>();
            imagem.raycastTarget = false;
            var ajuste = miolo.gameObject.AddComponent<AspectRatioFitter>();
            ajuste.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            moldura.gameObject.AddComponent<RectMask2D>();
            var aviso = Estilo.Rotulo(moldura, string.Empty, 10, Estilo.TextoSuave);
            aviso.gameObject.SetActive(false);
            var i = p.Identidade;
            var arte = Retratos.Buscar(personagens.RegraDoArquetipo(p)?.Retrato);
            var temFoto = !string.IsNullOrWhiteSpace(i.FotoHeroica) && i.FotoHeroicaAprovacao != AprovacaoFoto.Recusada;
            if (temFoto || arte != null)
            {
                moldura.gameObject.AddComponent<FotoDaWeb>().Mostrar(i.FotoHeroica, imagem, ajuste, aviso, temFoto ? null : arte);
                return;
            }
            // Sem foto nem arte (NPC sem link): a inicial, em vez de um quadrado branco.
            imagem.enabled = false;
            var inicial = Estilo.Rotulo(moldura, string.IsNullOrEmpty(p.NomeExibicao) ? "?" : p.NomeExibicao.Substring(0, 1).ToUpperInvariant(),
                30, p.Controle == Controle.Mestre ? Estilo.Erro : Estilo.Dourado, FontStyles.Bold);
            inicial.alignment = TextAlignmentOptions.Center;
            Estilo.Esticar(inicial.rectTransform);
        }

        // ---------- caixas de diálogo (usar a pilha, passagem do tempo) ----------

        RectTransform Caixa(string titulo, float altura, float largura = 900)
        {
            FecharBatalha();
            dialogo = Estilo.Rect("Dialogo", transform);
            Estilo.Esticar(dialogo);
            dialogo.gameObject.AddComponent<Image>().color = Estilo.Veu;
            var caixa = Estilo.Rect("Caixa", dialogo);
            caixa.anchorMin = caixa.anchorMax = new Vector2(0.5f, 0.5f);
            caixa.sizeDelta = new Vector2(largura, altura);
            Estilo.Moldura(caixa.gameObject, Estilo.Cartao, Estilo.Azul);
            var v = caixa.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(36, 36, 28, 28);
            v.spacing = 12;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandHeight = false;
            Estilo.Rotulo(caixa, titulo, 28, Estilo.Azul, FontStyles.Bold).characterSpacing = 5;
            ajustarTextos = true;
            return caixa;
        }

        TMP_InputField CampoMotivo(Transform caixa, string dica)
        {
            var campo = Estilo.CampoTexto(caixa, dica);
            var l = campo.gameObject.AddComponent<LayoutElement>();
            l.minHeight = l.preferredHeight = 52;
            return campo;
        }

        void Rodape(Transform caixa, string confirmar, UnityEngine.Events.UnityAction acao)
        {
            var fim = Estilo.Fila(caixa, 56, 12);
            Estilo.Rect("Vao", fim).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Estilo.BotaoContorno(fim, "Cancelar", 200, Estilo.TextoSuave, FecharBatalha);
            Estilo.BotaoPrincipal(fim, confirmar, 240, acao);
        }

        int usoQuantidade = 1;
        string usoQuem;

        void AbrirUso()
        {
            if (mesa.Equipe == 0)
            {
                Mostrar(Resultado.Falha("A pilha de Equipe está vazia."));
                return;
            }
            usoQuantidade = Math.Min(Math.Max(1, usoQuantidade), mesa.Equipe);
            DesenharUso(string.Empty);
        }

        void DesenharUso(string motivoAtual)
        {
            var caixa = Caixa($"USAR A PILHA  ·  {mesa.Equipe} disponíve{(mesa.Equipe == 1 ? "l" : "is")}", 560);
            Pergunta(caixa, "Quantos pontos?");
            var filaQ = Estilo.Fila(caixa, 48, 8);
            for (var n = 1; n <= Math.Min(mesa.Equipe, 6); n++)
            {
                var alvo = n;
                var b = n == usoQuantidade
                    ? Estilo.BotaoPrincipal(filaQ, n.ToString(), 80, () => { })
                    : Estilo.BotaoContorno(filaQ, n.ToString(), 80, Estilo.Dourado, () => { usoQuantidade = alvo; DesenharUso(campoUso.text); });
            }
            Pergunta(caixa, "Quem usa? (opcional)");
            var filaP = Estilo.Fila(caixa, 48, 8);
            foreach (var h in NaMesa())
            {
                var id = h.Id;
                var b = id == usoQuem
                    ? Estilo.BotaoPrincipal(filaP, h.NomeExibicao, 0, () => { usoQuem = null; DesenharUso(campoUso.text); })
                    : Estilo.BotaoContorno(filaP, h.NomeExibicao, 0, Estilo.Dourado, () => { usoQuem = id; DesenharUso(campoUso.text); });
                b.GetComponent<LayoutElement>().flexibleWidth = 1;
                Encolher(b);
            }
            campoUso = CampoMotivo(caixa, "motivo (ex.: ajudar a Garça a enfrentar; Farol usa 2 no movimento X)");
            campoUso.text = motivoAtual;
            Rodape(caixa, $"Usar {usoQuantidade}", () =>
            {
                var r = mesa.UsarEquipe(usoQuem, usoQuantidade, campoUso.text);
                if (!r.Ok)
                {
                    Mostrar(r);
                    return;
                }
                FecharBatalha();
                Aplicar(r);
            });
        }

        TMP_InputField campoUso;

        /// <summary>
        /// Passagem do tempo, em duas regras do livro: início da sessão (+1, soma ao que sobrou) ou o tempo passa
        /// (salto na história: a pilha volta para 1). Às vezes as duas acontecem juntas: aí são dois cliques.
        /// </summary>
        void AbrirTempo()
        {
            var caixa = Caixa("PASSAGEM DO TEMPO", 520);
            Nota(caixa, $"A pilha está com {mesa.Equipe}. Se a sessão nova começa semanas depois na história, " +
                        "use os dois: primeiro \"o tempo passa\", depois \"início da sessão\".");
            var campo = CampoMotivo(caixa, "motivo (ex.: sessão 3; \"três semanas depois...\")");
            var fila = Estilo.Fila(caixa, 64, 12);
            var inicio = Estilo.BotaoPrincipal(fila, "Início da sessão  (+1)", 0, () =>
            {
                FecharBatalha();
                Aplicar(mesa.InicioDeSessao(campo.text));
            });
            inicio.GetComponent<LayoutElement>().flexibleWidth = 1;
            Encolher(inicio);
            var passa = Estilo.BotaoContorno(fila, "O tempo passa  (volta para 1)", 0, Estilo.Dourado, () =>
            {
                FecharBatalha();
                Aplicar(mesa.OTempoPassa(campo.text));
            });
            passa.GetComponent<LayoutElement>().flexibleWidth = 1;
            Encolher(passa);
            var fim = Estilo.Fila(caixa, 56, 12);
            Estilo.Rect("Vao", fim).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Estilo.BotaoContorno(fim, "Cancelar", 200, Estilo.TextoSuave, FecharBatalha);
        }

        void Aplicar(Resultado r)
        {
            Mostrar(r);
            Redesenhar();
        }

        // ---------- Entrar em batalha (o questionário, livro pp.86–87) ----------

        readonly Mesa.EntradaEmBatalha batalha = new Mesa.EntradaEmBatalha();
        readonly List<string> evitar = new List<string>();
        string motivoBatalha = string.Empty;

        void AbrirBatalha()
        {
            var herois = NaMesa();
            if (herois.Count < 2)
            {
                Mostrar(Resultado.Falha("Entrar em batalha pede mais de um personagem na mesa."));
                return;
            }
            batalha.Lider = herois.Any(h => h.Id == batalha.Lider) ? batalha.Lider : herois[0].Id;
            batalha.Colegas = herois.Select(h => h.Id).ToList();
            batalha.MesmoObjetivo = batalha.AlguemNaoConfia = batalha.MalPreparada = false;
            evitar.Clear();
            DesenharBatalha();
        }

        void FecharBatalha()
        {
            if (dialogo != null)
                Destroy(dialogo.gameObject);
            dialogo = null;
        }

        void DesenharBatalha()
        {
            ajustarTextos = true;
            FecharBatalha();
            var herois = NaMesa();
            dialogo = Estilo.Rect("Batalha", transform);
            Estilo.Esticar(dialogo);
            dialogo.gameObject.AddComponent<Image>().color = Estilo.Veu;
            var caixa = Estilo.Rect("Caixa", dialogo);
            caixa.anchorMin = caixa.anchorMax = new Vector2(0.5f, 0.5f);
            caixa.sizeDelta = new Vector2(1100, 940);
            Estilo.Moldura(caixa.gameObject, Estilo.Cartao, Estilo.Azul);
            var v = caixa.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(36, 36, 28, 28);
            v.spacing = 10;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandHeight = false;

            Estilo.Rotulo(caixa, "ENTRAR EM BATALHA COMO EQUIPE", 30, Estilo.Azul, FontStyles.Bold).characterSpacing = 5;
            Nota(caixa, "Na pose antes da luta, ou no instante em que o inimigo aparece. Livro pp.86–87.");

            // Quem está na luta: nem sempre é a equipe inteira (já aconteceu de entrarem só 3). Só essas contam
            // como colegas (a Influência da líder é sobre elas) e só elas gastam da pilha.
            Pergunta(caixa, "Quem está entrando na luta? (pelo menos 2)");
            var filaE = Estilo.Fila(caixa, 48, 8);
            foreach (var h in herois)
            {
                var id = h.Id;
                var dentro = batalha.Colegas.Contains(id);
                var b = dentro
                    ? Estilo.BotaoPrincipal(filaE, h.NomeExibicao, 0, () =>
                    {
                        batalha.Colegas = batalha.Colegas.Where(x => x != id).ToList();
                        if (batalha.Lider == id)
                            batalha.Lider = batalha.Colegas.FirstOrDefault();
                        evitar.Clear();
                        DesenharBatalha();
                    })
                    : Estilo.BotaoContorno(filaE, h.NomeExibicao, 0, Estilo.TextoApagado, () =>
                    {
                        batalha.Colegas = batalha.Colegas.Append(id).ToList();
                        batalha.Lider ??= id;
                        DesenharBatalha();
                    });
                b.GetComponent<LayoutElement>().flexibleWidth = 1;
                Encolher(b);
            }

            Pergunta(caixa, "Quem lidera?");
            var filaL = Estilo.Fila(caixa, 48, 8);
            foreach (var h in herois.Where(h => batalha.Colegas.Contains(h.Id)))
            {
                var id = h.Id;
                var b = id == batalha.Lider
                    ? Estilo.BotaoPrincipal(filaL, h.NomeExibicao, 0, () => { })
                    : Estilo.BotaoContorno(filaL, h.NomeExibicao, 0, Estilo.Dourado, () =>
                    {
                        batalha.Lider = id;
                        evitar.Clear();
                        DesenharBatalha();
                    });
                b.GetComponent<LayoutElement>().flexibleWidth = 1;
                var r = b.GetComponentInChildren<TextMeshProUGUI>();
                r.enableAutoSizing = true;
                r.fontSizeMin = 12;
                r.fontSizeMax = 20;
                r.textWrappingMode = TextWrappingModes.NoWrap;
            }

            batalha.CondicoesParaEvitar = evitar.ToList();
            if (batalha.Colegas.Count < 2 || batalha.Lider == null)
            {
                Nota(caixa, "Uma pessoa só não é equipe: marque pelo menos 2 personagens na luta.");
                var sair = Estilo.Fila(caixa, 56, 12);
                Estilo.Rect("Vao", sair).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                Estilo.BotaoContorno(sair, "Cancelar", 200, Estilo.TextoSuave, FecharBatalha);
                return;
            }
            var (total, linhas, semInfluencia) = mesa.ContarBatalha(batalha);
            Nota(caixa, semInfluencia.Count == 0
                ? "Quem lidera tem Influência sobre cada colega (lido das fichas): +1."
                : $"Quem lidera não tem Influência sobre: {string.Join(", ", semInfluencia)} (lido das fichas).");

            SimNao(caixa, "Todos têm o mesmo objetivo na luta? (pergunte o de quem lidera; variações leves contam)", batalha.MesmoObjetivo,
                x => batalha.MesmoObjetivo = x);
            SimNao(caixa, "Alguém não confia em quem lidera ou na equipe? (cada jogador decide por si)", batalha.AlguemNaoConfia,
                x => batalha.AlguemNaoConfia = x);
            SimNao(caixa, "A equipe está mal preparada ou desequilibrada? (o mestre decide, pela ficção)", batalha.MalPreparada,
                x => batalha.MalPreparada = x);

            var perdas = (batalha.AlguemNaoConfia ? 1 : 0) + (batalha.MalPreparada ? 1 : 0);
            if (perdas > 0)
            {
                var lider = personagens.Buscar(batalha.Lider);
                Pergunta(caixa, $"Quem lidera marca condição para não perder ponto? (até {perdas})");
                var filaC = Estilo.Fila(caixa, 44, 6);
                foreach (var c in Condicoes.Todas)
                {
                    var id = c;
                    var ja = lider.CondicoesMarcadas.Contains(c);
                    var escolhida = evitar.Contains(c);
                    var b = escolhida
                        ? Estilo.BotaoPrincipal(filaC, Condicoes.Nome(c), 0, () => { evitar.Remove(id); DesenharBatalha(); })
                        : Estilo.BotaoContorno(filaC, Condicoes.Nome(c), 0, ja ? Estilo.TextoApagado : Estilo.Dourado, () =>
                        {
                            if (ja || evitar.Count >= perdas)
                                return;
                            evitar.Add(id);
                            DesenharBatalha();
                        });
                    b.GetComponent<LayoutElement>().flexibleWidth = 1;
                    b.GetComponentInChildren<TextMeshProUGUI>().fontSize = 16;
                }
            }

            var conta = Estilo.Rotulo(caixa, string.Join("\n", linhas), 20, Estilo.TextoSuave);
            conta.textWrappingMode = TextWrappingModes.Normal;
            var sinal = total >= 0 ? "+" : string.Empty;
            Estilo.Rotulo(caixa, $"PILHA: {mesa.Equipe} {sinal}{total} = {Math.Max(0, mesa.Equipe + total)}", 30, Estilo.Texto, FontStyles.Bold);

            var campoMotivo = CampoMotivo(caixa, "motivo (ex.: usina Halcyon Sul contra o Doutor Ocaso)");
            campoMotivo.text = motivoBatalha;
            campoMotivo.onValueChanged.AddListener(t => motivoBatalha = t);
            var fim = Estilo.Fila(caixa, 56, 12);
            Estilo.Rect("Vao", fim).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Estilo.BotaoContorno(fim, "Cancelar", 200, Estilo.TextoSuave, FecharBatalha);
            Estilo.BotaoPrincipal(fim, "Confirmar", 240, () =>
            {
                var r = mesa.EntrarEmBatalha(batalha, motivoBatalha);
                if (!r.Ok)
                {
                    Mostrar(r);
                    return;
                }
                FecharBatalha();
                Aplicar(r);
            });
        }

        void SimNao(Transform pai, string pergunta, bool valor, Action<bool> definir)
        {
            var fila = Estilo.Fila(pai, 48, 10);
            var t = Estilo.Rotulo(fila, pergunta, 20, Estilo.Texto);
            t.textWrappingMode = TextWrappingModes.Normal;
            Estilo.ColunaElastica(t);
            foreach (var (texto, sim) in new[] { ("Sim", true), ("Não", false) })
            {
                var alvo = sim;
                if (sim == valor)
                    Estilo.BotaoPrincipal(fila, texto, 110, () => { });
                else
                    Estilo.BotaoContorno(fila, texto, 110, Estilo.Dourado, () =>
                    {
                        definir(alvo);
                        evitar.Clear();
                        DesenharBatalha();
                    });
            }
        }

        static void Pergunta(Transform pai, string texto) =>
            Estilo.Rotulo(pai, texto, 20, Estilo.Dourado, FontStyles.Bold);

        static void Nota(Transform pai, string texto)
        {
            var n = Estilo.Rotulo(pai, texto, 18, Estilo.TextoSuave, FontStyles.Italic);
            n.textWrappingMode = TextWrappingModes.Normal;
        }
    }
}
