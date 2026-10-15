using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using TorreDeControle.Core.Cartas;
using UnityEngine;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// O desenhador de cartas: a Unity desenha a carta na hora, no estilo das cartas oficiais do help (seções em
    /// faixas creme e azul, borda colorida, origem no pé), com o layout do Alan por cima:
    /// [ícone][NOME] / seções / [rodapé de tokens] + origem.
    /// Fontes: a entrada do help (Ajuda/catalogo.json: título, seções, borda, origem) e, se houver, a carta de
    /// jogo ligada a ela (Regras/cartas.json: ícone, rodapé). Corrigir texto é editar JSON, sem PNG.
    /// A carta é montada num tamanho de referência e escalada para caber onde for posta (miniatura ou leitor).
    /// </summary>
    public static class DesenhoDeCarta
    {
        public const float Largura = 300;
        public const float Altura = 469; // a proporção das cartas do help (672 × 1050)

        static readonly Color Creme = new Color(0.94f, 0.90f, 0.82f);
        static readonly Color AzulClaro = new Color(0.86f, 0.89f, 0.93f);
        static readonly Color TituloDaSecao = new Color(0.18f, 0.30f, 0.50f);
        static readonly Color TextoDaSecao = new Color(0.12f, 0.14f, 0.20f);

        static JArray textosDoHelp;
        static CatalogoDeCartas cartasDoJogo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimparAoEntrarNoPlay()
        {
            textosDoHelp = null;
            cartasDoJogo = null;
            disco = null;
            helpEditado = null;
            cartasEditadas = null;
        }

        // Textos editados nesta sessão (a gestão das cartas grava os JSON no projeto, mas o Resources da Unity só
        // os relê depois de reimportar): enquanto houver, valem eles.
        static string helpEditado;
        static string cartasEditadas;

        /// <summary>O catalogo.json do help como está agora (editado nesta sessão, ou o do Resources).</summary>
        public static string TextoDoHelp() =>
            helpEditado ?? Resources.Load<TextAsset>("Ajuda/catalogo")?.text;

        /// <summary>O cartas.json como está agora (editado nesta sessão, ou o do Resources).</summary>
        public static string TextoDasCartas() =>
            cartasEditadas ?? Resources.Load<TextAsset>("Regras/cartas")?.text;

        /// <summary>Depois de gravar uma edição: tudo passa a ler os textos novos.</summary>
        public static void Recarregar(string help, string cartas)
        {
            helpEditado = help;
            cartasEditadas = cartas;
            textosDoHelp = null;
            cartasDoJogo = null;
        }

        /// <summary>No Editor, inclui alterações externas sem manter a cópia antiga da gestão.</summary>
        public static void AtualizarDoProjeto()
        {
            if (!Application.isEditor) return;
            var arquivos = new TorreDeControle.Infra.ArquivosDoProjeto(Application.dataPath);
            if (!arquivos.Disponivel) return;
            var help = arquivos.Ler("Resources/Ajuda/catalogo.json");
            var cartas = arquivos.Ler("Resources/Regras/cartas.json");
            if (help == helpEditado && cartas == cartasEditadas) return;
            // Valida antes de substituir o estado em memória.
            JObject.Parse(help);
            JObject.Parse(cartas);
            Recarregar(help, cartas);
        }

        sealed class FonteDeTexto : Core.Regras.IFonteDeRegras
        {
            readonly string cartas;
            public FonteDeTexto(string cartas) => this.cartas = cartas;
            public string Ler(string nome) => nome == "cartas" ? cartas : null;
        }

        static JArray Help()
        {
            if (textosDoHelp == null)
            {
                var texto = TextoDoHelp();
                textosDoHelp = texto == null ? new JArray() : (JArray)JObject.Parse(texto)["cartas"];
            }
            return textosDoHelp;
        }

        static CatalogoDeCartas Cartas() =>
            cartasDoJogo ??= CatalogoDeCartas.Carregar(new FonteDeTexto(TextoDasCartas()));

        /// <summary>O catálogo de cartas atual (com as edições da sessão).</summary>
        public static CatalogoDeCartas CatalogoAtual => Cartas();

        /// <summary>A entrada do help com este id (com as edições da sessão), ou null.</summary>
        public static JObject EntradaDoHelpAtual(string id) => EntradaDoHelp(id);

        static JObject EntradaDoHelp(string id) =>
            id == null ? null : Help().Cast<JObject>().FirstOrDefault(c => (string)c["id"] == id);

        /// <summary>
        /// Carta de jogo no tamanho de referência, como item de grade/lista (ex.: a gestão das cartas).
        /// </summary>
        public static RectTransform Criar(Transform pai, Carta carta)
        {
            var celula = Estilo.Rect("Carta " + carta.Id, pai);
            var le = celula.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = Largura;
            le.preferredHeight = Altura;
            Desenhar(celula, EntradaDoHelp(carta.Ajuda), carta, rolavel: false);
            return celula;
        }

        /// <summary>
        /// Uma entrada do help desenhada dentro de <paramref name="area"/> (miniatura ou leitor), escalada para
        /// caber mantendo a proporção. Se uma carta de jogo apontar para esta entrada, ganha ícone e rodapé.
        /// </summary>
        /// <param name="rolavel">
        /// O texto rola dentro da carta? Na carta ampliada, sim; nas miniaturas, não: a roda do mouse passa para
        /// a grade (a carta inteira se lê ao clicar).
        /// </param>
        /// <param name="carta">A carta de jogo a usar (a prévia do editor passa a sua); null: a do catálogo.</param>
        public static RectTransform CriarDaAjuda(RectTransform area, JObject entrada, bool rolavel = false, Carta carta = null)
        {
            var id = (string)entrada["id"];
            carta ??= Cartas().Todas.FirstOrDefault(c => c.Ajuda == id);
            var raiz = Estilo.Rect("Carta desenhada " + id, area);
            raiz.anchorMin = raiz.anchorMax = raiz.pivot = new Vector2(0.5f, 0.5f);
            raiz.sizeDelta = new Vector2(Largura, Altura);
            raiz.gameObject.AddComponent<EscalaParaCaber>();
            Desenhar(raiz, entrada, carta, rolavel);
            return raiz;
        }

        static void Desenhar(RectTransform raiz, JObject entrada, Carta carta, bool rolavel)
        {
            var borda = CorDaBorda(entrada, carta);
            Estilo.Moldura(raiz.gameObject, Estilo.Cartao, borda, 3f);
            var v = raiz.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(10, 10, 10, 8);
            v.spacing = 6;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;

            // [ícone][NOME]
            var titulo = (string)entrada?["titulo"] ?? carta?.Titulo ?? "?";
            var topo = Estilo.Fila(raiz, 46, 8);
            // Padrão: o ícone próprio da carta; senão, o símbolo da categoria do help (todas as cartas da
            // categoria com a mesma cara).
            var icone = (carta == null ? null : Icone(carta)) ?? IconeDaCategoria((string)entrada?["categoria"]);
            if (icone != null)
                Simbolo(topo, icone);
            var nome = Estilo.Rotulo(topo, titulo.ToUpperInvariant(), 17, Color.white, FontStyles.Bold);
            nome.textWrappingMode = TextWrappingModes.Normal;
            nome.enableAutoSizing = true;
            nome.fontSizeMin = 10;
            nome.fontSizeMax = 17;
            nome.alignment = TextAlignmentOptions.MidlineLeft;
            Estilo.ColunaElastica(nome);

            // As seções, em faixas alternadas creme e azul (como as cartas oficiais); rolam se não couberem.
            var secoes = entrada?["secoes"] as JArray;
            var lista = Estilo.ListaRolavel(raiz, 0);
            if (!rolavel)
            {
                // Componente desligado não recebe o evento: a roda sobe para a rolagem da grade.
                var rolagem = lista.GetComponentInParent<ScrollRect>();
                if (rolagem != null)
                {
                    // A barra só se esconde sozinha com a rolagem ligada: sem ela, some junto e o texto
                    // ganha a largura de volta.
                    if (rolagem.verticalScrollbar != null)
                        rolagem.verticalScrollbar.gameObject.SetActive(false);
                    rolagem.verticalScrollbar = null;
                    if (rolagem.viewport != null)
                        Estilo.Esticar(rolagem.viewport);
                    rolagem.enabled = false;
                }
            }
            if (secoes == null || secoes.Count == 0)
                Estilo.Rotulo(lista, "texto a escrever", 13, Estilo.TextoApagado, FontStyles.Italic);
            else
                for (var k = 0; k < secoes.Count; k++)
                    Secao(lista, (string)secoes[k]["titulo"], (string)secoes[k]["texto"], k % 2 == 0 ? Creme : AzulClaro);

            // [rodapé de tokens] + origem
            var fichas = carta == null ? new string[0] : Rodape(carta);
            // Carta de um arquétipo: o nome da classe num botãozinho no canto do rodapé.
            // Com carta de jogo, vale a exclusividade dela ("todos"/"mestre": sem selo); sem, a classe do help.
            var classe = carta == null ? (string)entrada?["arquetipo"]
                : carta.Origem != null && carta.Origem != "todos" && carta.Origem != "mestre"
                    ? Core.Personagens.Arquetipos.Nome(carta.Origem)
                    : null;
            if (fichas.Count > 0 || !string.IsNullOrEmpty(classe))
            {
                var rodape = Estilo.Fila(raiz, 24, 5);
                foreach (var f in fichas)
                    Ficha(rodape, f);
                Estilo.Rect("Vao", rodape).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                if (!string.IsNullOrEmpty(classe) && classe != "—")
                    Selo(rodape, classe, borda);
            }
            var origem = (string)entrada?["origem"];
            if (!string.IsNullOrEmpty(origem))
            {
                var o = Estilo.Rotulo(raiz, origem, 9, Estilo.TextoSuave);
                o.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        static void Secao(Transform pai, string titulo, string texto, Color fundo)
        {
            var faixa = Estilo.Rect("Secao", pai);
            faixa.gameObject.AddComponent<Image>().color = fundo;
            var v = faixa.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(8, 8, 5, 6);
            v.spacing = 1;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;
            if (!string.IsNullOrEmpty(titulo))
            {
                var t = Estilo.Rotulo(faixa, titulo, 12, TituloDaSecao, FontStyles.Bold);
                t.textWrappingMode = TextWrappingModes.Normal;
            }
            var c = Estilo.Rotulo(faixa, Marcacao(texto), 12, TextoDaSecao);
            c.textWrappingMode = TextWrappingModes.Normal;
        }

        /// <summary>
        /// A marcação do texto das cartas (editável no JSON): [cor=verde|azul|vermelho|dourado|laranja|#hex]...[/cor]
        /// e [b]...[/b]. O resto é texto puro (nada de tag do TMP escapa do JSON para a tela).
        /// </summary>
        public static string Marcacao(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return string.Empty;
            var sb = new System.Text.StringBuilder();
            var marca = new System.Text.RegularExpressions.Regex(@"\[cor=([^\]]+)\]|\[/cor\]|\[b\]|\[/b\]");
            var pos = 0;
            foreach (System.Text.RegularExpressions.Match m in marca.Matches(texto))
            {
                if (m.Index > pos)
                    sb.Append("<noparse>").Append(texto, pos, m.Index - pos).Append("</noparse>");
                sb.Append(m.Value switch
                {
                    "[/cor]" => "</color>",
                    "[b]" => "<b>",
                    "[/b]" => "</b>",
                    _ => $"<color={Cor(m.Groups[1].Value.Trim())}>",
                });
                pos = m.Index + m.Length;
            }
            if (pos < texto.Length)
                sb.Append("<noparse>").Append(texto, pos, texto.Length - pos).Append("</noparse>");
            return sb.ToString();
        }

        static string Cor(string nome) => nome.ToLowerInvariant() switch
        {
            "verde" => "#3E8F52",
            "azul" => "#2F5FA8",
            "vermelho" => "#B83A3A",
            "dourado" => "#A07A24",
            "laranja" => "#C46A1C",
            _ => System.Text.RegularExpressions.Regex.IsMatch(nome, "^#[0-9a-fA-F]{6}$") ? nome : "#000000",
        };

        /// <summary>Cor da moldura: a do help; senão, pelo tipo da carta (mesma convenção do help).</summary>
        static Color CorDaBorda(JObject entrada, Carta carta)
        {
            if (entrada?["borda"] is JValue v && ColorUtility.TryParseHtmlString((string)v, out var cor))
                return cor;
            return carta?.Tipo switch
            {
                Carta.Pvp => new Color(0.86f, 0.42f, 0.42f),
                Carta.Arquetipo => new Color(0.84f, 0.33f, 0.33f),
                Carta.Equipe => new Color(0.36f, 0.62f, 0.95f),
                Carta.Extra => new Color(0.70f, 0.50f, 0.90f),
                _ => Estilo.Dourado,
            };
        }

        /// <summary>O rodapé gravado na carta; sem ele, o mínimo que a regra garante: rolou, falhou, marca potencial.</summary>
        static IReadOnlyList<string> Rodape(Carta carta) =>
            carta.Rodape.Count > 0 ? carta.Rodape
            : carta.Rotulo != null ? new[] { "potencial" }
            : new string[0];

        /// <summary>
        /// O ícone do movimento (Resources/Icones/Movimentos/&lt;nome&gt;), se houver: o básico pelo próprio nome,
        /// a variante PvP pelo do básico ("pvp/pvp-01-enfrentar" → enfrentar).
        /// </summary>
        static Texture2D Icone(Carta carta)
        {
            if (!string.IsNullOrEmpty(carta.Icone))
                return carta.Icone == "nenhum" ? null : Resources.Load<Texture2D>("Icones/" + carta.Icone);
            var nome = carta.Id.Substring(carta.Id.IndexOf('/') + 1);
            if (carta.Tipo == Carta.Pvp)
                nome = System.Text.RegularExpressions.Regex.Replace(nome, @"^pvp-\d+-", string.Empty);
            if (nome == "perfurar")
                nome = "perfurar-mascara";
            var icone = Resources.Load<Texture2D>("Icones/Movimentos/" + nome);
            if (icone != null)
                return icone;
            // Sem ícone de movimento: a moeda do token quando a carta é dele (Equipe, Influência, Chamas).
            var token = carta.Tipo == Carta.Equipe || carta.Id == "basico/pilha-equipe" || carta.Id == "basico/egoista" ? "equipe"
                : carta.Id == "basico/usar-influencia" ? "influencia"
                : carta.Id == "nova/chamas" ? "chamas"
                : null;
            return token == null ? null : Resources.Load<Texture2D>("Icones/Tokens/" + token);
        }

        /// <summary>Símbolo de uma categoria do help, para as cartas dela sem ícone próprio.</summary>
        static Texture2D IconeDaCategoria(string categoria) => categoria switch
        {
            "influencia" => Resources.Load<Texture2D>("Icones/Tokens/influencia"),
            "equipe-sessao" => Resources.Load<Texture2D>("Icones/Tokens/equipe"),
            "movimentos-equipe" => Resources.Load<Texture2D>("Icones/Tokens/equipe"),
            _ => null,
        };

        static Texture2D disco;

        /// <summary>Um disco azul-marinho (gerado uma vez) para preencher moedas de miolo vazio, como a da Influência.</summary>
        static Texture2D Disco()
        {
            if (disco != null)
                return disco;
            const int n = 64;
            disco = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var cor = new Color(0.06f, 0.10f, 0.24f);
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                disco.SetPixel(x, y, d <= n * 0.40f ? cor : Color.clear);
            }
            disco.Apply();
            return disco;
        }

        static void Simbolo(Transform pai, Texture2D icone)
        {
            var caixa = Estilo.Rect("Simbolo", pai);
            var lt = caixa.gameObject.AddComponent<LayoutElement>();
            lt.preferredWidth = 40;
            lt.flexibleWidth = 0;
            lt.layoutPriority = 2;
            // A imagem num filho: o objeto da fila é do layout; o ajuste de proporção briga com ele.
            // Moeda de miolo transparente (Influência): um disco por trás, para não ficar oca.
            if (icone.name == "influencia")
            {
                var fundo = Estilo.Rect("Fundo", caixa);
                Estilo.Esticar(fundo);
                var f = fundo.gameObject.AddComponent<RawImage>();
                f.texture = Disco();
                f.raycastTarget = false;
            }
            var miolo = Estilo.Rect("Icone", caixa);
            Estilo.Esticar(miolo);
            var img = miolo.gameObject.AddComponent<RawImage>();
            img.texture = icone;
            img.raycastTarget = false;
            var ajuste = miolo.gameObject.AddComponent<AspectRatioFitter>();
            ajuste.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            ajuste.aspectRatio = (float)icone.width / Mathf.Max(1, icone.height);
        }

        /// <summary>
        /// Largura estimada de um texto curto em negrito (pelas letras, não pelo TMP): o TMP não mede texto que
        /// está numa parte escondida da tela (ex.: a grade, enquanto se lê uma carta ampliada) e quebra.
        /// </summary>
        static float LarguraEstimada(string texto, float tamanho, float espacamento = 0) =>
            (texto?.Length ?? 0) * (tamanho * 0.62f + espacamento);

        /// <summary>O selo da classe (ex.: "Bruta"), na cor da borda da carta.</summary>
        static void Selo(Transform pai, string texto, Color cor)
        {
            var selo = Estilo.Rect("Classe", pai);
            Estilo.Moldura(selo.gameObject, new Color(cor.r, cor.g, cor.b, 0.18f), cor, 1.5f);
            var t = Estilo.Rotulo(selo, texto.ToUpperInvariant(), 11, cor, FontStyles.Bold);
            t.alignment = TextAlignmentOptions.Center;
            t.characterSpacing = 2;
            Estilo.Esticar(t.rectTransform);
            var l = selo.gameObject.AddComponent<LayoutElement>();
            l.preferredWidth = LarguraEstimada(texto.ToUpperInvariant(), 11, 2) + 16;
            l.flexibleWidth = 0;
        }

        /// <summary>Placeholder de moeda: o nome curto do token na cor dele (a arte entra depois).</summary>
        static void Ficha(Transform pai, string token)
        {
            var (rotulo, cor) = token switch
            {
                "equipe" => ("Equipe", new Color(0.36f, 0.62f, 0.95f)),
                "potencial" => ("Potencial", Estilo.Dourado),
                "influencia" => ("Influência", new Color(0.45f, 0.80f, 0.50f)),
                "adiante" => ("Adiante", new Color(0.35f, 0.85f, 0.90f)),
                "constante" => ("Constante", new Color(0.78f, 0.80f, 0.86f)),
                "controle" => ("Controle", new Color(0.65f, 0.80f, 0.35f)),
                "condicao" => ("−Condição", new Color(0.95f, 0.45f, 0.40f)),
                _ => (token, Estilo.TextoSuave),
            };
            var moeda = Resources.Load<Texture2D>("Icones/Tokens/" + token);
            if (moeda != null)
            {
                // A moedinha de verdade e o nome pequeno ao lado.
                var grupo = Estilo.Rect("Ficha " + token, pai);
                var h = grupo.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 2;
                h.childControlWidth = true;
                h.childControlHeight = true;
                h.childForceExpandWidth = false;
                h.childAlignment = TextAnchor.MiddleLeft;
                var caixaMoeda = Estilo.Rect("Moeda", grupo);
                var li = caixaMoeda.gameObject.AddComponent<LayoutElement>();
                li.preferredWidth = li.preferredHeight = 22;
                if (token == "influencia")
                {
                    var fundo = Estilo.Rect("Fundo", caixaMoeda);
                    Estilo.Esticar(fundo);
                    var f = fundo.gameObject.AddComponent<RawImage>();
                    f.texture = Disco();
                    f.raycastTarget = false;
                }
                var miolo = Estilo.Rect("Imagem", caixaMoeda);
                Estilo.Esticar(miolo);
                var img = miolo.gameObject.AddComponent<RawImage>();
                img.texture = moeda;
                img.raycastTarget = false;
                var nome = Estilo.Rotulo(grupo, rotulo, 10, cor, FontStyles.Bold);
                nome.textWrappingMode = TextWrappingModes.NoWrap;
                var lg = grupo.gameObject.AddComponent<LayoutElement>();
                lg.preferredWidth = 24 + LarguraEstimada(rotulo, 10) + 4;
                lg.flexibleWidth = 0;
                return;
            }
            var chip = Estilo.Rect("Ficha " + token, pai);
            Estilo.Moldura(chip.gameObject, new Color(cor.r, cor.g, cor.b, 0.15f), cor, 1.5f);
            var t = Estilo.Rotulo(chip, rotulo, 11, cor, FontStyles.Bold);
            t.alignment = TextAlignmentOptions.Center;
            Estilo.Esticar(t.rectTransform);
            var lc = chip.gameObject.AddComponent<LayoutElement>();
            lc.preferredWidth = LarguraEstimada(rotulo, 11) + 12;
            lc.flexibleWidth = 0;
        }
    }

    /// <summary>Escala a carta (montada no tamanho de referência) para caber no pai, mantendo a proporção.</summary>
    public sealed class EscalaParaCaber : MonoBehaviour
    {
        RectTransform rect;
        RectTransform pai;

        void Awake()
        {
            rect = (RectTransform)transform;
            pai = (RectTransform)transform.parent;
        }

        void LateUpdate()
        {
            if (pai == null)
                return;
            var tamanho = pai.rect.size;
            var escala = Mathf.Min(tamanho.x / DesenhoDeCarta.Largura, tamanho.y / DesenhoDeCarta.Altura);
            if (escala > 0 && !Mathf.Approximately(rect.localScale.x, escala))
                rect.localScale = new Vector3(escala, escala, 1);
        }
    }
}
