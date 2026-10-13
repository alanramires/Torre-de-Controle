using TMPro;
using TorreDeControle.Core;
using TorreDeControle.Core.Jogadores;
using UnityEngine;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Gestão de Jogadores. Montado por código (sem prefab) por cima da tela que o abre.
    /// Digitar o nome + Enter cria; editar o nome na lista + Enter renomeia; o botão alterna ativo/inativo.
    /// </summary>
    public sealed class PainelJogadores : MonoBehaviour
    {
        static readonly Color CorFundo = new Color(0.08f, 0.09f, 0.12f, 0.97f);
        static readonly Color CorLinha = new Color(1f, 1f, 1f, 0.06f);
        static readonly Color CorAtivo = new Color(0.25f, 0.6f, 0.35f);
        static readonly Color CorInativo = new Color(0.45f, 0.45f, 0.45f);
        static readonly Color CorTextoInativo = new Color(0.68f, 0.68f, 0.68f);
        static readonly Color CorCampo = new Color(0.13f, 0.14f, 0.18f, 0.95f);
        static readonly Color CorErro = new Color(1f, 0.45f, 0.4f);

        CadastroJogadores cadastro;
        TMP_InputField campoNovo;
        TextMeshProUGUI mensagem;
        RectTransform lista;

        public static PainelJogadores Criar(Transform pai)
        {
            var raiz = NovoRect("PainelJogadores", pai);
            Esticar(raiz);
            raiz.gameObject.AddComponent<Image>().color = CorFundo;
            var painel = raiz.gameObject.AddComponent<PainelJogadores>();
            painel.Montar(raiz);
            return painel;
        }

        public void Abrir(CadastroJogadores cadastroJogadores)
        {
            cadastro = cadastroJogadores;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Mostrar(null);
            Redesenhar();
            campoNovo.ActivateInputField();
        }

        public void Fechar() => gameObject.SetActive(false);

        // ---------- ações ----------

        void Adicionar(string nome)
        {
            var resultado = cadastro.Criar(nome);
            Mostrar(resultado);
            if (resultado.Ok)
            {
                campoNovo.text = string.Empty;
                Redesenhar();
            }
            campoNovo.ActivateInputField();
        }

        void Renomear(Jogador jogador, TMP_InputField campo)
        {
            // Sair do campo sem mudar nada não grava nem redesenha.
            if (campo.text.Trim() == jogador.Nome)
                return;

            var resultado = cadastro.Renomear(jogador.Id, campo.text);
            Mostrar(resultado);
            if (!resultado.Ok)
                campo.text = jogador.Nome;
            else
                Redesenhar();
        }

        void AlternarAtivo(Jogador jogador)
        {
            Mostrar(cadastro.DefinirAtivo(jogador.Id, !jogador.Ativo));
            Redesenhar();
        }

        void Mostrar(Resultado? resultado)
        {
            var erro = resultado.HasValue && !resultado.Value.Ok ? resultado.Value.Erro : null;
            mensagem.text = erro ?? string.Empty;
        }

        // ---------- montagem ----------

        void Montar(RectTransform raiz)
        {
            var coluna = raiz.gameObject.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(80, 80, 60, 60);
            coluna.spacing = 16;
            coluna.childControlWidth = true;
            coluna.childControlHeight = true;
            coluna.childForceExpandHeight = false;

            // Cabeçalho: título + voltar
            var cabecalho = Linha(raiz, 60);
            var titulo = Texto(cabecalho, "Jogadores", 44, FontStyles.Bold);
            titulo.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Botao(cabecalho, "Voltar", 200, new Color(0.3f, 0.3f, 0.35f), Fechar);

            // Novo jogador
            var novo = Linha(raiz, 64);
            campoNovo = Campo(novo, "Nome do novo jogador e Enter");
            campoNovo.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            campoNovo.onSubmit.AddListener(Adicionar);
            Botao(novo, "Adicionar", 200, CorAtivo, () => Adicionar(campoNovo.text));

            mensagem = Texto(raiz, string.Empty, 26, FontStyles.Normal);
            mensagem.color = CorErro;
            mensagem.gameObject.AddComponent<LayoutElement>().minHeight = 34;

            lista = Rolagem(raiz);
        }

        void Redesenhar()
        {
            for (var i = lista.childCount - 1; i >= 0; i--)
                Destroy(lista.GetChild(i).gameObject);

            var todos = cadastro.Listar();
            if (todos.Count == 0)
            {
                var vazio = Texto(lista, "Nenhum jogador ainda.", 28, FontStyles.Italic);
                vazio.color = CorInativo;
                return;
            }

            foreach (var jogador in todos)
            {
                var linha = Linha(lista, 60);
                linha.gameObject.AddComponent<Image>().color = CorLinha;

                var campo = Campo(linha, string.Empty);
                campo.text = jogador.Nome;
                if (!jogador.Ativo)
                {
                    campo.textComponent.color = CorTextoInativo;
                    campo.textComponent.fontStyle = FontStyles.Italic;
                }
                campo.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                campo.onSubmit.AddListener(_ => Renomear(jogador, campo));
                campo.onDeselect.AddListener(_ => Renomear(jogador, campo));

                var j = jogador;
                Botao(linha, jogador.Ativo ? "Ativo" : "Inativo", 200,
                    jogador.Ativo ? CorAtivo : CorInativo, () => AlternarAtivo(j));
            }
        }

        // ---------- peças ----------

        static RectTransform NovoRect(string nome, Transform pai)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            return (RectTransform)go.transform;
        }

        static void Esticar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static RectTransform Linha(Transform pai, float altura)
        {
            var linha = NovoRect("Linha", pai);
            var h = linha.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12;
            h.padding = new RectOffset(8, 8, 4, 4);
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            var layout = linha.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = altura;
            layout.preferredHeight = altura;
            // Sem isto o HorizontalLayoutGroup se declara esticável (childForceExpandHeight)
            // e a linha rouba a altura que era da lista.
            layout.flexibleHeight = 0;
            return linha;
        }

        static TextMeshProUGUI Texto(Transform pai, string conteudo, float tamanho, FontStyles estilo)
        {
            var rect = NovoRect("Texto", pai);
            var texto = rect.gameObject.AddComponent<TextMeshProUGUI>();
            texto.text = conteudo;
            texto.fontSize = tamanho;
            texto.fontStyle = estilo;
            texto.color = Color.white;
            texto.alignment = TextAlignmentOptions.MidlineLeft;
            return texto;
        }

        static void Botao(Transform pai, string rotulo, float largura, Color cor, UnityEngine.Events.UnityAction aoClicar)
        {
            var rect = NovoRect("Botao " + rotulo, pai);
            var imagem = rect.gameObject.AddComponent<Image>();
            imagem.color = cor;
            var botao = rect.gameObject.AddComponent<Button>();
            botao.targetGraphic = imagem;
            botao.onClick.AddListener(aoClicar);
            rect.gameObject.AddComponent<LayoutElement>().preferredWidth = largura;

            var texto = Texto(rect, rotulo, 28, FontStyles.Bold);
            texto.alignment = TextAlignmentOptions.Center;
            Esticar(texto.rectTransform);
        }

        static TMP_InputField Campo(Transform pai, string dica)
        {
            var rect = NovoRect("Campo", pai);
            // Montado desligado: o TMP_InputField cria o cursor no OnEnable, e só consegue
            // se o textComponent já estiver ligado. Ligar antes deixa o campo sem edição.
            rect.gameObject.SetActive(false);
            rect.gameObject.AddComponent<Image>().color = CorCampo;
            var campo = rect.gameObject.AddComponent<TMP_InputField>();

            var area = NovoRect("Area", rect);
            Esticar(area);
            area.offsetMin = new Vector2(14, 6);
            area.offsetMax = new Vector2(-14, -6);
            area.gameObject.AddComponent<RectMask2D>();

            var placeholder = Texto(area, dica, 28, FontStyles.Italic);
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            Esticar(placeholder.rectTransform);

            var texto = Texto(area, string.Empty, 28, FontStyles.Normal);
            Esticar(texto.rectTransform);

            campo.textViewport = area;
            campo.textComponent = texto;
            campo.placeholder = placeholder;
            campo.lineType = TMP_InputField.LineType.SingleLine;
            rect.gameObject.SetActive(true);
            return campo;
        }

        static RectTransform Rolagem(Transform pai)
        {
            var rect = NovoRect("Lista", pai);
            rect.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var scroll = rect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 30;

            var viewport = NovoRect("Viewport", rect);
            Esticar(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var conteudo = NovoRect("Conteudo", viewport);
            conteudo.anchorMin = new Vector2(0, 1);
            conteudo.anchorMax = new Vector2(1, 1);
            conteudo.pivot = new Vector2(0.5f, 1);
            conteudo.offsetMin = Vector2.zero;
            conteudo.offsetMax = Vector2.zero;
            var v = conteudo.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 8;
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
