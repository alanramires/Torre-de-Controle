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
        /// <summary>O azul do valor inicial na ficha impressa.</summary>
        public static readonly Color Azul = new Color(0.36f, 0.62f, 0.95f);

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
            SomBotao.Ligar(botao, rotulo == "Voltar" || rotulo == "Fechar" || rotulo == "Cancelar");
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
            campo.onValidateInput = (_, __, c) => c == '	' ? '\u0000' : c;
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
            SomBotao.Ligar(botao);
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
            SomBotao.Ligar(fechar, true, false);
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
                SomBotao.Ligar(b);
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

        /// <summary>
        /// Pergunta modal por cima de tudo: mensagem + [cancelar] [confirmar]. Esc não existe ainda;
        /// clicar fora cancela.
        /// </summary>
        public static void Confirmar(Transform qualquer, string mensagem, string textoSim, UnityAction aoConfirmar,
            UnityAction aoCancelar = null) =>
            Perguntar(qualquer, mensagem, new[] { (textoSim, aoConfirmar) }, aoCancelar);

        /// <summary>
        /// Como <see cref="Confirmar"/>, com várias respostas: [cancelar] [opção]... (a última é a principal).
        /// </summary>
        public static void Perguntar(Transform qualquer, string mensagem, (string texto, UnityAction acao)[] opcoes,
            UnityAction aoCancelar = null)
        {
            var raiz = (RectTransform)qualquer.GetComponentInParent<Canvas>().rootCanvas.transform;
            var veu = Rect("Confirmar", raiz);
            Esticar(veu);
            veu.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            var fora = veu.gameObject.AddComponent<Button>();
            SomBotao.Ligar(fora, true, false);
            fora.transition = Selectable.Transition.None;
            void Cancelar()
            {
                Object.Destroy(veu.gameObject);
                aoCancelar?.Invoke();
            }
            fora.onClick.AddListener(Cancelar);

            var caixa = Rect("Caixa", veu);
            caixa.anchorMin = caixa.anchorMax = new Vector2(0.5f, 0.5f);
            caixa.sizeDelta = new Vector2(opcoes.Length > 1 ? 1000 : 820, 0);
            Moldura(caixa.gameObject, CaixaOpcoes, Dourado);
            // A altura acompanha a mensagem (avisos longos não vazam da caixa).
            caixa.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var coluna = caixa.gameObject.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(40, 40, 36, 32);
            coluna.spacing = 24;
            coluna.childControlWidth = true;
            coluna.childControlHeight = true;
            coluna.childForceExpandHeight = false;

            var texto = Rotulo(caixa, mensagem, 28, Texto);
            texto.textWrappingMode = TextWrappingModes.Normal;
            texto.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            var botoes = Fila(caixa, 60);
            botoes.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
            var vao = Rect("Vao", botoes);
            vao.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            BotaoContorno(botoes, "Cancelar", 220, TextoSuave, Cancelar);
            for (var k = 0; k < opcoes.Length; k++)
            {
                var acao = opcoes[k].acao;
                UnityAction clicar = () =>
                {
                    Object.Destroy(veu.gameObject);
                    acao();
                };
                if (k == opcoes.Length - 1)
                    BotaoPrincipal(botoes, opcoes[k].texto, 300, clicar);
                else
                    BotaoContorno(botoes, opcoes[k].texto, 300, Dourado, clicar);
            }
        }

        /// <summary>
        /// Escolha modal numa lista vertical: um botão por opção (a primeira em destaque) e Cancelar.
        /// Opção <c>apagada</c> sai em tom pálido (ex.: arquivo, só consulta). Clicar fora cancela.
        /// </summary>
        public static void Escolher(Transform qualquer, string titulo, (string texto, UnityAction acao, bool apagada)[] opcoes)
        {
            var raiz = (RectTransform)qualquer.GetComponentInParent<Canvas>().rootCanvas.transform;
            var veu = Rect("Escolher", raiz);
            Esticar(veu);
            veu.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            var fora = veu.gameObject.AddComponent<Button>();
            SomBotao.Ligar(fora, true, false);
            fora.transition = Selectable.Transition.None;
            fora.onClick.AddListener(() => Object.Destroy(veu.gameObject));

            var caixa = Rect("Caixa", veu);
            caixa.anchorMin = caixa.anchorMax = new Vector2(0.5f, 0.5f);
            Moldura(caixa.gameObject, CaixaOpcoes, Dourado);
            var coluna = caixa.gameObject.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(40, 40, 32, 32);
            coluna.spacing = 12;
            coluna.childControlWidth = true;
            coluna.childControlHeight = true;
            coluna.childForceExpandHeight = false;
            var ajuste = caixa.gameObject.AddComponent<ContentSizeFitter>();
            ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            caixa.sizeDelta = new Vector2(760, 0);

            var t = Rotulo(caixa, titulo, 28, Dourado, FontStyles.Bold);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.gameObject.AddComponent<LayoutElement>().minHeight = 40;

            for (var k = 0; k < opcoes.Length; k++)
            {
                var acao = opcoes[k].acao;
                UnityAction clicar = () =>
                {
                    Object.Destroy(veu.gameObject);
                    acao();
                };
                var b = opcoes[k].apagada ? BotaoContorno(caixa, opcoes[k].texto, 0, TextoApagado, clicar)
                    : k == 0 ? BotaoPrincipal(caixa, opcoes[k].texto, 0, clicar)
                    : BotaoContorno(caixa, opcoes[k].texto, 0, Dourado, clicar);
                var le = b.GetComponent<LayoutElement>();
                le.minHeight = 58;
                le.preferredHeight = 58;
            }

            var vao = Rect("Vao", caixa);
            vao.gameObject.AddComponent<LayoutElement>().minHeight = 4;
            var cancelar = BotaoContorno(caixa, "Cancelar", 0, TextoSuave, () => Object.Destroy(veu.gameObject));
            cancelar.GetComponent<LayoutElement>().preferredHeight = 52;
        }

        public const float LarguraAlca = 34;

        /// <summary>
        /// A alça "=" (dois traços) no começo de uma linha ordenável. Arrastar por ela reordena
        /// a linha dentro do bloco; <paramref name="aoSoltar"/> recebe a posição final.
        /// </summary>
        public static void Alca(RectTransform linha, System.Action<int> aoSoltar)
        {
            var rect = Rect("Alca", linha);
            rect.SetAsFirstSibling();
            var area = rect.gameObject.AddComponent<Image>();
            area.color = new Color(0, 0, 0, 0); // recebe o clique em toda a área da alça
            rect.gameObject.AddComponent<LayoutElement>().preferredWidth = LarguraAlca;
            foreach (var deslocamento in new[] { 5f, -5f })
            {
                var traco = Rect("Traco", rect);
                traco.anchorMin = traco.anchorMax = new Vector2(0.5f, 0.5f);
                traco.sizeDelta = new Vector2(22, 3);
                traco.anchoredPosition = new Vector2(0, deslocamento);
                var imagem = traco.gameObject.AddComponent<Image>();
                imagem.color = new Color(Dourado.r, Dourado.g, Dourado.b, 0.7f);
                imagem.raycastTarget = false;
            }
            rect.gameObject.AddComponent<AlcaArrastar>().Configurar(linha, aoSoltar);
        }

        /// <summary>Espaço do tamanho da alça, para cabeçalhos e a linha de criação alinharem com as linhas.</summary>
        public static void VaoDaAlca(Transform fila)
        {
            var vao = Rect("VaoAlca", fila);
            vao.SetAsFirstSibling();
            vao.gameObject.AddComponent<LayoutElement>().preferredWidth = LarguraAlca;
        }

        /// <summary>Bloco de uma lista (ATIVOS / INATIVOS): título e o contêiner das linhas.</summary>
        public static RectTransform Bloco(Transform lista, string titulo, int quantidade)
        {
            var cabeca = Fila(lista, 34);
            cabeca.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(6, 6, 8, 0);
            var t = Rotulo(cabeca, $"{titulo}  ·  {quantidade}", 20, TextoSuave, FontStyles.Bold);
            t.characterSpacing = 5;
            ColunaElastica(t);

            var bloco = Rect("Bloco " + titulo, lista);
            var v = bloco.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;
            return bloco;
        }

        /// <summary>Campo de várias linhas (Enter quebra a linha; grava ao sair).</summary>
        public static TMP_InputField AreaTexto(Transform pai, string dica, float altura)
        {
            var campo = CampoTexto(pai, dica);
            campo.lineType = TMP_InputField.LineType.MultiLineNewline;
            foreach (var t in new[] { (TextMeshProUGUI)campo.textComponent, (TextMeshProUGUI)campo.placeholder })
            {
                t.alignment = TextAlignmentOptions.TopLeft;
                t.textWrappingMode = TextWrappingModes.Normal;
                t.fontSize = 24;
            }
            if (!campo.TryGetComponent<LayoutElement>(out var layout)) layout = campo.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = altura;
            layout.preferredHeight = altura;
            // O próprio TMP_InputField também informa altura preferida (a do texto inteiro), com a mesma
            // prioridade: no empate vence a maior, e a caixa cresceu com o texto. Prioridade 2: a altura é esta,
            // o texto rola dentro e o ••• abre tudo.
            layout.layoutPriority = 2;
            layout.flexibleHeight = 0;
            campo.gameObject.AddComponent<RepassaRolagem>();
            return campo;
        }

        /// <summary>
        /// Põe o botão "•••" no fim da caixa (no canto de cima, se for de várias linhas): abre uma mini-tela com área de texto grande.
        /// Salvar escreve no campo e dispara o onDeselect dele (que é quem grava), como se tivesse saído do campo.
        /// </summary>
        public static void ComExpandir(TMP_InputField campo, string titulo, TMP_InputField campoExtra = null, string rotuloExtra = null)
        {
            // Só leitura: o ••• abre o texto inteiro para ler, sem editar.
            var soLeitura = !campo.interactable;
            if (soLeitura && string.IsNullOrWhiteSpace(campo.text))
                return;
            // Abre espaço à direita do texto para o botão.
            var area = campo.textViewport;
            area.offsetMax = new Vector2(-58, area.offsetMax.y);

            var rect = Rect("Expandir", campo.transform);
            if (campo.multiLine)
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
                rect.sizeDelta = new Vector2(52, 44);
            }
            else
            {
                rect.anchorMin = new Vector2(1, 0);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(1, 0.5f);
                rect.sizeDelta = new Vector2(52, 0);
            }
            rect.anchoredPosition = Vector2.zero;
            var fundo = rect.gameObject.AddComponent<Image>();
            fundo.color = new Color(1, 1, 1, 0);
            var botao = rect.gameObject.AddComponent<Button>();
            botao.targetGraphic = fundo;
            SomBotao.Ligar(botao);
            var cores = botao.colors;
            cores.highlightedColor = new Color(1, 1, 1, 0.12f);
            botao.colors = cores;
            var pontos = Rotulo(rect, "•••", 22, Dourado, FontStyles.Bold);
            pontos.alignment = TextAlignmentOptions.Center;
            Esticar(pontos.rectTransform);
            if (soLeitura)
                botao.onClick.AddListener(() => LeitorDeTexto(campo.transform, titulo, $"<noparse>{campo.text}</noparse>"));
            else
                botao.onClick.AddListener(() => EditorDeTexto(campo.transform, titulo, campo.text, novo =>
                {
                    campo.text = novo;
                    campo.onDeselect.Invoke(novo);
                }, campoExtra, rotuloExtra));
        }

        /// <summary>Mini-tela modal com uma área de texto grande: Cancelar ou Salvar.</summary>
        /// <summary>
        /// Mini-tela modal para ler um texto longo (rich text), só leitura, com rolagem e Fechar.
        /// <paramref name="copias"/>: botões que copiam uma versão do texto para a área de transferência
        /// (ex.: para o fórum, para o WhatsApp).
        /// </summary>
        public static void LeitorDeTexto(Transform qualquer, string titulo, string textoRico,
            params (string rotulo, System.Func<string> texto)[] copias)
        {
            var raiz = (RectTransform)qualquer.GetComponentInParent<Canvas>().rootCanvas.transform;
            var veu = Rect("LeitorDeTexto", raiz);
            Esticar(veu);
            // Só o Fechar fecha: clicar no texto (para rolar, ler) não pode sumir com a tela.
            veu.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.65f);

            var caixa = Rect("Caixa", veu);
            caixa.anchorMin = caixa.anchorMax = new Vector2(0.5f, 0.5f);
            caixa.sizeDelta = new Vector2(1300, 860);
            Moldura(caixa.gameObject, CaixaOpcoes, Dourado);
            var coluna = caixa.gameObject.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(36, 36, 28, 28);
            coluna.spacing = 16;
            coluna.childControlWidth = true;
            coluna.childControlHeight = true;
            coluna.childForceExpandHeight = false;

            var t = Rotulo(caixa, titulo, 26, Dourado, FontStyles.Bold);
            t.characterSpacing = 4;
            t.gameObject.AddComponent<LayoutElement>().minHeight = 36;

            var lista = ListaRolavel(caixa, 8);
            var corpo = Rotulo(lista, textoRico, 24, Texto);
            corpo.textWrappingMode = TextWrappingModes.Normal;

            var botoes = Fila(caixa, 60);
            var aviso = Rotulo(botoes, string.Empty, 20, Dourado, FontStyles.Italic);
            ColunaElastica(aviso);
            foreach (var (rotulo, texto) in copias)
            {
                var nome = rotulo;
                var gerar = texto;
                BotaoContorno(botoes, rotulo, 360, Dourado, () =>
                {
                    GUIUtility.systemCopyBuffer = gerar();
                    aviso.text = $"Copiado ({nome.ToLowerInvariant()}): é só colar.";
                });
            }
            BotaoContorno(botoes, "Fechar", 220, TextoSuave, () => Object.Destroy(veu.gameObject));
        }

        /// <param name="campoExtra">
        /// Um campo de uma linha que acompanha o texto (ex.: o nome do NPC da formação): aparece acima do texto,
        /// e salvar também o grava (texto + onEndEdit no campo original).
        /// </param>
        public static void EditorDeTexto(Transform qualquer, string titulo, string texto, System.Action<string> aoSalvar,
            TMP_InputField campoExtra = null, string rotuloExtra = null)
        {
            var raiz = (RectTransform)qualquer.GetComponentInParent<Canvas>().rootCanvas.transform;
            var veu = Rect("EditorDeTexto", raiz);
            Esticar(veu);
            veu.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.65f);
            veu.gameObject.AddComponent<NavegacaoTab>();

            var caixa = Rect("Caixa", veu);
            caixa.anchorMin = caixa.anchorMax = new Vector2(0.5f, 0.5f);
            // Com o campo extra (uma linha a mais), a caixa cresce na mesma medida: os botões não saem dela.
            caixa.sizeDelta = new Vector2(1100, campoExtra != null ? 620 + 68 : 620);
            Moldura(caixa.gameObject, CaixaOpcoes, Dourado);
            var coluna = caixa.gameObject.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(36, 36, 28, 28);
            coluna.spacing = 16;
            coluna.childControlWidth = true;
            coluna.childControlHeight = true;
            coluna.childForceExpandHeight = false;

            var t = Rotulo(caixa, titulo, 26, Dourado, FontStyles.Bold);
            t.gameObject.AddComponent<LayoutElement>().minHeight = 36;

            TMP_InputField extra = null;
            if (campoExtra != null)
            {
                var filaExtra = Fila(caixa, 52, 12);
                var r = Rotulo(filaExtra, rotuloExtra ?? string.Empty, 20, TextoSuave, FontStyles.Italic);
                r.gameObject.AddComponent<LayoutElement>().preferredWidth = 360;
                extra = CampoTexto(filaExtra, (campoExtra.placeholder as TMP_Text)?.text ?? string.Empty);
                extra.text = campoExtra.text;
                ColunaElastica(extra);
            }

            var area = AreaTexto(caixa, "escreva à vontade", 420);
            area.GetComponent<LayoutElement>().flexibleHeight = 1;
            area.text = texto;
            area.ActivateInputField();

            var botoes = Fila(caixa, 60);
            var vao = Rect("Vao", botoes);
            vao.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            BotaoContorno(botoes, "Cancelar", 220, TextoSuave, () => Object.Destroy(veu.gameObject));
            BotaoPrincipal(botoes, "Salvar", 220, () =>
            {
                var novo = area.text;
                var novoExtra = extra?.text;
                Object.Destroy(veu.gameObject);
                if (campoExtra != null && novoExtra != campoExtra.text)
                {
                    campoExtra.text = novoExtra;
                    campoExtra.onEndEdit.Invoke(novoExtra);
                }
                aoSalvar(novo);
            });
        }

        /// <summary>Selo "? dicas da ficha": com o mouse em cima, mostra o texto num balão.</summary>
        public static void SeloDica(Transform pai, string rotulo, string conteudo)
        {
            var rect = Rect("SeloDica", pai);
            Moldura(rect.gameObject, new Color(Dourado.r, Dourado.g, Dourado.b, 0.08f), Dourado);
            rect.gameObject.AddComponent<LayoutElement>().preferredWidth = 230;
            var t = Rotulo(rect, "?  " + rotulo, 20, Dourado, FontStyles.Bold);
            t.alignment = TextAlignmentOptions.Center;
            Esticar(t.rectTransform);
            rect.gameObject.AddComponent<Dica>().Configurar(conteudo);
        }

        public static RectTransform ListaRolavel(Transform pai, float espaco = 10)
        {
            var rect = Rect("Lista", pai);
            rect.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            // Fundo invisível que recebe o mouse: sem ele, a roda sobre títulos e vãos (que não são
            // clicáveis) passa direto para o cartão e a lista não rola.
            rect.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
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

            // Barra de rolagem na lateral: some sozinha quando tudo cabe (e devolve a largura à lista).
            var barra = Rect("Barra", rect);
            barra.anchorMin = new Vector2(1, 0);
            barra.anchorMax = new Vector2(1, 1);
            barra.pivot = new Vector2(1, 0.5f);
            barra.sizeDelta = new Vector2(12, 0);
            barra.gameObject.AddComponent<Image>().color = Campo;
            var rolagem = barra.gameObject.AddComponent<Scrollbar>();
            rolagem.direction = Scrollbar.Direction.BottomToTop;
            var trilho = Rect("Trilho", barra);
            Esticar(trilho);
            var alca = Rect("Alca", trilho);
            Esticar(alca);
            var imagemAlca = alca.gameObject.AddComponent<Image>();
            imagemAlca.color = new Color(Dourado.r, Dourado.g, Dourado.b, 0.75f);
            rolagem.handleRect = alca;
            rolagem.targetGraphic = imagemAlca;
            scroll.verticalScrollbar = rolagem;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.verticalScrollbarSpacing = 10;
            return conteudo;
        }
    }
}
