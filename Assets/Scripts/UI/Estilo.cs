using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Linguagem visual da Torre, tirada do menu de entrada: azul-marinho translúcido, borda fina
    /// azulada, dourado para o destaque. Os painéis montados por código usam só estas peças.
    /// </summary>
    public static class Estilo
    {
        public static readonly Color Veu = new Color(0.01f, 0.02f, 0.06f, 0.8f);
        public static readonly Color Cartao = new Color(0.04f, 0.07f, 0.17f, 0.98f);
        public static readonly Color CaixaOpcoes = new Color(0.03f, 0.05f, 0.13f, 1f);
        public static readonly Color Linha = new Color(0.07f, 0.11f, 0.24f, 0.85f);
        public static readonly Color Campo = new Color(0.02f, 0.04f, 0.11f, 0.9f);
        public static readonly Color Borda = new Color(0.42f, 0.55f, 0.85f, 0.55f);
        public static readonly Color BordaApagada = new Color(0.42f, 0.48f, 0.6f, 0.3f);

        public static readonly Color Dourado = new Color(0.88f, 0.71f, 0.38f);
        public static readonly Color DouradoEscuro = new Color(0.32f, 0.24f, 0.1f, 0.95f);
        public static readonly Color Texto = new Color(0.93f, 0.94f, 0.98f);
        public static readonly Color TextoSuave = new Color(0.68f, 0.74f, 0.86f);
        public static readonly Color TextoApagado = new Color(0.55f, 0.58f, 0.66f);
        public static readonly Color Erro = new Color(1f, 0.5f, 0.45f);

        // ---------- estrutura ----------

        public static RectTransform Rect(string nome, Transform pai)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            return (RectTransform)go.transform;
        }

        public static void Esticar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Fundo com borda fina, como os cartões do menu. A borda são quatro filetes filhos, não o
        /// componente Outline: ele desenha cópias inteiras da forma deslocadas, e com fundo translúcido
        /// as cópias aparecem por trás como um preenchimento sólido.
        /// </summary>
        public static Image Moldura(GameObject alvo, Color fundo, Color borda, float espessura = 2f)
        {
            var imagem = alvo.AddComponent<Image>();
            imagem.color = fundo;
            Filete(alvo.transform, borda, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -espessura), Vector2.zero);
            Filete(alvo.transform, borda, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, espessura));
            Filete(alvo.transform, borda, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(espessura, 0));
            Filete(alvo.transform, borda, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-espessura, 0), Vector2.zero);
            return imagem;
        }

        static void Filete(Transform pai, Color cor, Vector2 ancoraMin, Vector2 ancoraMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = Rect("Borda", pai);
            rect.anchorMin = ancoraMin;
            rect.anchorMax = ancoraMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var imagem = rect.gameObject.AddComponent<Image>();
            imagem.color = cor;
            imagem.raycastTarget = false;
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        public static RectTransform Fila(Transform pai, float altura, float espaco = 14)
        {
            var linha = Rect("Fila", pai);
            var h = linha.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = espaco;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            h.childAlignment = TextAnchor.MiddleLeft;
            var layout = linha.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = altura;
            layout.preferredHeight = altura;
            // Sem isto o HorizontalLayoutGroup se declara esticável (childForceExpandHeight)
            // e a fila rouba a altura de quem deveria crescer.
            layout.flexibleHeight = 0;
            return linha;
        }

        // ---------- peças ----------

        public static TextMeshProUGUI Rotulo(Transform pai, string conteudo, float tamanho, Color cor,
            FontStyles estilo = FontStyles.Normal)
        {
            var rect = Rect("Texto", pai);
            var texto = rect.gameObject.AddComponent<TextMeshProUGUI>();
            texto.text = conteudo;
            texto.fontSize = tamanho;
            texto.fontStyle = estilo;
            texto.color = cor;
            texto.alignment = TextAlignmentOptions.MidlineLeft;
            texto.raycastTarget = false;
            return texto;
        }

        /// <summary>Botão de destaque: dourado cheio, texto escuro.</summary>
        public static Button BotaoPrincipal(Transform pai, string rotulo, float largura, UnityAction aoClicar) =>
            Botao(pai, rotulo, largura, Dourado, Dourado, Cartao, aoClicar);

        /// <summary>Botão secundário: só a borda, como os cartões do menu.</summary>
        public static Button BotaoContorno(Transform pai, string rotulo, float largura, Color cor, UnityAction aoClicar) =>
            Botao(pai, rotulo, largura, new Color(cor.r, cor.g, cor.b, 0.08f), cor, cor, aoClicar);

        static Button Botao(Transform pai, string rotulo, float largura, Color fundo, Color borda, Color corTexto,
            UnityAction aoClicar)
        {
            var rect = Rect("Botao " + rotulo, pai);
            var imagem = Moldura(rect.gameObject, fundo, borda);
            var botao = rect.gameObject.AddComponent<Button>();
            botao.targetGraphic = imagem;
            var cores = botao.colors;
            cores.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            cores.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            botao.colors = cores;
            botao.onClick.AddListener(aoClicar);
            rect.gameObject.AddComponent<LayoutElement>().preferredWidth = largura;

            var texto = Rotulo(rect, rotulo.ToUpperInvariant(), 24, corTexto, FontStyles.Bold);
            texto.alignment = TextAlignmentOptions.Center;
            texto.characterSpacing = 4;
            Esticar(texto.rectTransform);
            return botao;
        }

        public static TMP_InputField CampoTexto(Transform pai, string dica)
        {
            var rect = Rect("Campo", pai);
            // Montado desligado: o TMP_InputField cria o cursor no OnEnable, e só consegue
            // se o textComponent já estiver ligado. Ligar antes deixa o campo sem edição.
            rect.gameObject.SetActive(false);
            Moldura(rect.gameObject, Campo, BordaApagada);
            var campo = rect.gameObject.AddComponent<TMP_InputField>();

            var area = Rect("Area", rect);
            Esticar(area);
            area.offsetMin = new Vector2(18, 6);
            area.offsetMax = new Vector2(-18, -6);
            area.gameObject.AddComponent<RectMask2D>();

            var placeholder = Rotulo(area, dica, 26, TextoApagado, FontStyles.Italic);
            Esticar(placeholder.rectTransform);

            var texto = Rotulo(area, string.Empty, 26, Texto);
            Esticar(texto.rectTransform);

            campo.textViewport = area;
            campo.textComponent = texto;
            campo.placeholder = placeholder;
            campo.lineType = TMP_InputField.LineType.SingleLine;
            campo.caretColor = Dourado;
            campo.customCaretColor = true;
            campo.selectionColor = new Color(Dourado.r, Dourado.g, Dourado.b, 0.35f);
            // Tab é navegação (ver NavegacaoTab), nunca caractere.
            campo.onValidateInput = (_, __, c) => c == '	' ? ' ' : c;
            rect.gameObject.AddComponent<Tabulavel>();
            rect.gameObject.SetActive(true);
            return campo;
        }

        /// <summary>
        /// Lista suspensa: o campo mostra o valor; clicar (ou Enter com ele selecionado) abre a lista
        /// embaixo dele, por cima de tudo; escolher fecha. Clicar fora fecha sem mudar.
        /// Feita aqui em vez do TMP_Dropdown, que exige um template montado à mão.
        /// </summary>
        public static Button ListaSuspensa(Transform pai, float largura, IReadOnlyList<(string id, string nome)> opcoes,
            string idAtual, UnityAction<string> aoMudar, bool editavel = true)
        {
            var rect = Rect("ListaSuspensa", pai);
            var imagem = Moldura(rect.gameObject, Campo, BordaApagada);
            var botao = rect.gameObject.AddComponent<Button>();
            botao.targetGraphic = imagem;
            var cores = botao.colors;
            cores.highlightedColor = new Color(1.25f, 1.25f, 1.25f);
            cores.selectedColor = new Color(1.25f, 1.25f, 1.25f);
            botao.colors = cores;
            rect.gameObject.AddComponent<LayoutElement>().preferredWidth = largura;
            rect.gameObject.AddComponent<Tabulavel>();

            var atual = string.Empty;
            foreach (var o in opcoes)
                if (o.id == idAtual)
                    atual = o.nome;

            var valor = Rotulo(rect, atual.Length > 0 ? atual : "—", 24, Texto);
            valor.textWrappingMode = TextWrappingModes.NoWrap;
            valor.overflowMode = TextOverflowModes.Ellipsis;
            Esticar(valor.rectTransform);
            valor.rectTransform.offsetMin = new Vector2(16, 0);
            valor.rectTransform.offsetMax = new Vector2(-40, 0);

            // "›" girado para baixo: a fonte padrão do TMP não tem ▾ (vira quadradinho).
            var seta = Rotulo(rect, "›", 34, Dourado, FontStyles.Bold);
            seta.alignment = TextAlignmentOptions.Center;
            seta.rectTransform.anchorMin = new Vector2(1, 0.5f);
            seta.rectTransform.anchorMax = new Vector2(1, 0.5f);
            seta.rectTransform.sizeDelta = new Vector2(30, 30);
            seta.rectTransform.anchoredPosition = new Vector2(-22, 0);
            seta.rectTransform.localEulerAngles = new Vector3(0, 0, -90);

            if (!editavel)
            {
                // Travada: mostra o valor, sem seta, sem clique, fora do Tab.
                botao.interactable = false;
                var travada = botao.colors;
                travada.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.6f);
                botao.colors = travada;
                valor.color = TextoSuave;
                seta.gameObject.SetActive(false);
                return botao;
            }

            botao.onClick.AddListener(() => AbrirOpcoes(rect, opcoes, idAtual, escolhido =>
            {
                foreach (var o in opcoes)
                    if (o.id == escolhido)
                        valor.text = o.nome;
                idAtual = escolhido;
                aoMudar(escolhido);
            }));
            return botao;
        }

        static void AbrirOpcoes(RectTransform campo, IReadOnlyList<(string id, string nome)> opcoes, string idAtual,
            UnityAction<string> aoEscolher)
        {
            const float alturaOpcao = 46;
            const int visiveis = 8;

            var canvas = campo.GetComponentInParent<Canvas>().rootCanvas;
            var raizCanvas = (RectTransform)canvas.transform;

            // Bloqueador invisível na tela toda: clicar fora fecha.
            var bloqueio = Rect("Opcoes", raizCanvas);
            Esticar(bloqueio);
            var fundoBloqueio = bloqueio.gameObject.AddComponent<Image>();
            fundoBloqueio.color = new Color(0, 0, 0, 0);
            var fechar = bloqueio.gameObject.AddComponent<Button>();
            fechar.transition = Selectable.Transition.None;
            fechar.onClick.AddListener(() => Object.Destroy(bloqueio.gameObject));

            var cantos = new Vector3[4];
            campo.GetWorldCorners(cantos);
            Vector2 baixoEsq = bloqueio.InverseTransformPoint(cantos[0]);
            Vector2 cimaDir = bloqueio.InverseTransformPoint(cantos[2]);
            var altura = Mathf.Min(opcoes.Count, visiveis) * alturaOpcao + 8;

            var caixa = Rect("Caixa", bloqueio);
            caixa.anchorMin = caixa.anchorMax = new Vector2(0.5f, 0.5f);
            caixa.sizeDelta = new Vector2(cimaDir.x - baixoEsq.x, altura);
            // Abre para baixo; se não couber na tela, para cima.
            if (baixoEsq.y - 4 - altura < bloqueio.rect.yMin)
            {
                caixa.pivot = new Vector2(0, 0);
                caixa.anchoredPosition = new Vector2(baixoEsq.x, cimaDir.y + 4);
            }
            else
            {
                caixa.pivot = new Vector2(0, 1);
                caixa.anchoredPosition = new Vector2(baixoEsq.x, baixoEsq.y - 4);
            }
            Moldura(caixa.gameObject, CaixaOpcoes, Borda);

            var lista = ListaRolavel(caixa, 2);
            var rolagem = lista.GetComponentInParent<ScrollRect>();
            Esticar((RectTransform)rolagem.transform);
            ((RectTransform)rolagem.transform).offsetMin = new Vector2(4, 4);
            ((RectTransform)rolagem.transform).offsetMax = new Vector2(-4, -4);

            foreach (var (id, nome) in opcoes)
            {
                var item = Rect("Opcao " + nome, lista);
                var fundo = item.gameObject.AddComponent<Image>();
                fundo.color = id == idAtual ? DouradoEscuro : new Color(1, 1, 1, 0);
                var b = item.gameObject.AddComponent<Button>();
                b.targetGraphic = fundo;
                var c = b.colors;
                c.highlightedColor = new Color(1f, 1f, 1f, 0.6f);
                b.colors = c;
                var layout = item.gameObject.AddComponent<LayoutElement>();
                layout.minHeight = alturaOpcao - 2;
                layout.preferredHeight = alturaOpcao - 2;
                var texto = Rotulo(item, nome, 24, id == idAtual ? Dourado : Texto);
                Esticar(texto.rectTransform);
                texto.rectTransform.offsetMin = new Vector2(14, 0);
                var escolhido = id;
                b.onClick.AddListener(() =>
                {
                    Object.Destroy(bloqueio.gameObject);
                    aoEscolher(escolhido);
                });
            }
        }

        /// <summary>
        /// Coluna que divide o espaço por igual com as irmãs. Sem zerar a largura preferida, um campo
        /// de texto pede a largura do próprio conteúdo e as colunas desalinham de linha para linha.
        /// </summary>
        public static void ColunaElastica(Component alvo, float peso = 1)
        {
            if (!alvo.TryGetComponent<LayoutElement>(out var layout))
                layout = alvo.gameObject.AddComponent<LayoutElement>();
            // Prioridade acima da do TMP_InputField (1): no empate o Unity fica com o MAIOR valor,
            // e a largura do texto venceria o zero.
            layout.layoutPriority = 2;
            layout.minWidth = 0;
            layout.preferredWidth = 0;
            layout.flexibleWidth = peso;
        }

        public static RectTransform ListaRolavel(Transform pai, float espaco = 10)
        {
            var rect = Rect("Lista", pai);
            rect.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var scroll = rect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 30;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = Rect("Viewport", rect);
            Esticar(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var conteudo = Rect("Conteudo", viewport);
            conteudo.anchorMin = new Vector2(0, 1);
            conteudo.anchorMax = new Vector2(1, 1);
            conteudo.pivot = new Vector2(0.5f, 1);
            conteudo.offsetMin = Vector2.zero;
            conteudo.offsetMax = Vector2.zero;
            var v = conteudo.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = espaco;
            v.padding = new RectOffset(2, 2, 2, 2);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;
            conteudo.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = conteudo;
            return conteudo;
        }
    }
}
