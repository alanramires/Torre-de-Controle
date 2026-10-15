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
        }

        static JArray Help()
        {
            if (textosDoHelp == null)
            {
                var fonte = Resources.Load<TextAsset>("Ajuda/catalogo");
                textosDoHelp = fonte == null ? new JArray() : (JArray)JObject.Parse(fonte.text)["cartas"];
            }
            return textosDoHelp;
        }

        static CatalogoDeCartas Cartas() =>
            cartasDoJogo ??= CatalogoDeCartas.Carregar(new FonteDeRegrasResources());

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
        public static RectTransform CriarDaAjuda(RectTransform area, JObject entrada, bool rolavel = false)
        {
            var id = (string)entrada["id"];
            var carta = Cartas().Todas.FirstOrDefault(c => c.Ajuda == id);
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
            var icone = carta == null ? null : Icone(carta);
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
            var classe = carta != null && carta.Origem != null && carta.Origem != "todos" && carta.Origem != "mestre"
                ? Core.Personagens.Arquetipos.Nome(carta.Origem)
                : (string)entrada?["arquetipo"];
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
            var c = Estilo.Rotulo(faixa, $"<noparse>{texto}</noparse>", 12, TextoDaSecao);
            c.textWrappingMode = TextWrappingModes.Normal;
        }

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

        static void Simbolo(Transform pai, Texture2D icone)
        {
            var caixa = Estilo.Rect("Simbolo", pai);
            var lt = caixa.gameObject.AddComponent<LayoutElement>();
            lt.preferredWidth = 40;
            lt.flexibleWidth = 0;
            lt.layoutPriority = 2;
            // A imagem num filho: o objeto da fila é do layout; o ajuste de proporção briga com ele.
            var miolo = Estilo.Rect("Icone", caixa);
            Estilo.Esticar(miolo);
            var img = miolo.gameObject.AddComponent<RawImage>();
            img.texture = icone;
            img.raycastTarget = false;
            var ajuste = miolo.gameObject.AddComponent<AspectRatioFitter>();
            ajuste.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            ajuste.aspectRatio = (float)icone.width / Mathf.Max(1, icone.height);
        }

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
            l.preferredWidth = t.GetPreferredValues(texto.ToUpperInvariant()).x + 16;
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
                var img = Estilo.Rect("Moeda", grupo).gameObject.AddComponent<RawImage>();
                img.texture = moeda;
                img.raycastTarget = false;
                var li = img.gameObject.AddComponent<LayoutElement>();
                li.preferredWidth = li.preferredHeight = 22;
                var nome = Estilo.Rotulo(grupo, rotulo, 10, cor, FontStyles.Bold);
                nome.textWrappingMode = TextWrappingModes.NoWrap;
                var lg = grupo.gameObject.AddComponent<LayoutElement>();
                lg.preferredWidth = 24 + nome.GetPreferredValues(rotulo).x + 4;
                lg.flexibleWidth = 0;
                return;
            }
            var chip = Estilo.Rect("Ficha " + token, pai);
            Estilo.Moldura(chip.gameObject, new Color(cor.r, cor.g, cor.b, 0.15f), cor, 1.5f);
            var t = Estilo.Rotulo(chip, rotulo, 11, cor, FontStyles.Bold);
            t.alignment = TextAlignmentOptions.Center;
            Estilo.Esticar(t.rectTransform);
            var lc = chip.gameObject.AddComponent<LayoutElement>();
            lc.preferredWidth = t.GetPreferredValues(rotulo).x + 12;
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
