using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>Galeria do panorama básico e leitura ampliada. Sem cache estático.</summary>
    public sealed class PainelAjuda : MonoBehaviour
    {
        string[] nomes;
        string[] arquivos;
        string[] ids;
        JArray catalogo;
        Texture2D[] panoramas;
        readonly string[] categorias = { "referencia-rapida", "movimentos-basicos", "pvp", "influencia", "condicoes", "equipe-sessao", "movimentos-equipe", "movimentos-arquetipo", "viloes" };
        readonly string[] categoriasNomes = { "Visão geral", "Movimentos básicos", "PvP", "Influência", "Condições", "Equipe e sessão", "Movimentos de equipe", "Movimentos de arquétipo", "Vilões" };
        string arquetipoAtual;
        bool condicoesVilao;
        bool contextoCondicoesVilao;
        RectTransform[] cortinas;
        Button[] botoesCategoria;
        int categoriaAtual;
        Texture2D[] cards;
        RectTransform galeria;
        RectTransform areaGaleria;
        RectTransform leitor;
        RawImage ampliada;
        TextMeshProUGUI titulo;
        TextMeshProUGUI pagina;
        Button anterior;
        Button proxima;
        RectTransform assuntos;
        Button[] atalhos;
        int indice;

        public static PainelAjuda Criar(Transform pai)
        {
            var veu = Estilo.Rect("PainelAjuda", pai);
            Estilo.Esticar(veu);
            veu.gameObject.AddComponent<Image>().color = Estilo.Veu;
            veu.gameObject.AddComponent<NavegacaoTab>();
            var painel = veu.gameObject.AddComponent<PainelAjuda>();
            painel.Montar(veu);
            return painel;
        }

        public void Abrir(Texture2D[] texturas)
        {
            contextoCondicoesVilao = false;
            var fonte = Resources.Load<TextAsset>("Ajuda/catalogo");
            if (fonte == null)
            {
                Debug.LogError("[Torre] Cat?logo de ajuda ausente.");
                return;
            }
            catalogo = (JArray)JObject.Parse(fonte.text)["cartas"];
            panoramas = texturas;
            foreach (var botao in botoesCategoria) botao.gameObject.SetActive(true);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            EscolherCategoria(0);
        }

        /// <summary>Prefixo dos ids das cartas de movimento de um arquétipo (o id é o do arquetipos.json).</summary>
        static string PrefixoDoArquetipo(string arquetipoId) => $"arquetipo/{arquetipoId}/";

        /// <summary>
        /// O catálogo tem cartas de movimento deste arquétipo? Busca pelo id no texto (sem montar o JSON nem
        /// guardar cache), barato o bastante para a ficha perguntar ao desenhar a seção.
        /// </summary>
        public static bool TemMovimentos(string arquetipoId)
        {
            if (string.IsNullOrEmpty(arquetipoId))
                return false;
            var fonte = Resources.Load<TextAsset>("Ajuda/catalogo");
            return fonte != null && fonte.text.Contains("\"" + PrefixoDoArquetipo(arquetipoId));
        }

        /// <summary>
        /// Consulta contextual: só os movimentos do arquétipo (pelo id, não pelo nome exibido) e, se
        /// <paramref name="movimento"/> vier (id do movimento no arquetipos.json), já abre a carta dele.
        /// </summary>
        public void AbrirMovimentos(string arquetipoId, string movimento = null)
        {
            Abrir(null);
            var prefixo = PrefixoDoArquetipo(arquetipoId);
            catalogo = new JArray(catalogo.Cast<JObject>().Where(c =>
                (string)c["categoria"] == "movimentos-arquetipo" &&
                ((string)c["id"] ?? string.Empty).StartsWith(prefixo)));
            var grupo = catalogo.Count > 0 ? (string)catalogo[0]["arquetipo"] : null;
            var categoria = System.Array.IndexOf(categorias, "movimentos-arquetipo");
            for (var k = 0; k < botoesCategoria.Length; k++)
                botoesCategoria[k].gameObject.SetActive(k == categoria);
            EscolherCategoria(categoria, grupo);
            var carta = movimento == null ? -1 : System.Array.IndexOf(ids, prefixo + movimento);
            if (carta >= 0)
                Ler(carta);
        }

        void MontarGaleria()
        {
            foreach (Transform filho in galeria) Destroy(filho.gameObject);
            for (var k = 0; k < nomes.Length; k++)
            {
                var escolhido = k;
                var coluna = Estilo.Rect(nomes[k], galeria);
                coluna.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                var layout = coluna.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 14;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
                var quadro = Estilo.Rect("Miniatura", coluna);
                quadro.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
                var alvo = quadro.gameObject.AddComponent<Image>();
                alvo.color = Estilo.Campo;
                var botao = quadro.gameObject.AddComponent<Button>();
                SomBotao.Ligar(botao);
                botao.targetGraphic = alvo;
                botao.onClick.AddListener(() => Ler(escolhido));
                var imagem = Imagem(quadro);
                imagem.texture = Textura(k);
                imagem.enabled = imagem.texture != null;
            }
            MostrarGaleria();
        }

        /// <summary>Consulta contextual de uma categoria inteira (ex.: "influencia"), só ela no menu.</summary>
        public void AbrirCategoria(string id)
        {
            Abrir(null);
            var categoria = System.Array.IndexOf(categorias, id);
            if (categoria < 0)
                return;
            for (var k = 0; k < botoesCategoria.Length; k++)
                botoesCategoria[k].gameObject.SetActive(k == categoria);
            EscolherCategoria(categoria);
        }

        public void AbrirCondicoesVilao()
        {
            Abrir(null);
            contextoCondicoesVilao = true;
            catalogo = new JArray(catalogo.Cast<JObject>().Where(c =>
                (string)c["id"] == "viloes/luta" ||
                ((string)c["id"] ?? string.Empty).StartsWith("viloes/condicao-")));
            var categoria = System.Array.IndexOf(categorias, "viloes");
            for (var k = 0; k < botoesCategoria.Length; k++)
                botoesCategoria[k].gameObject.SetActive(k == categoria);
            EscolherCategoria(categoria, somenteCondicoes: true);
        }

        void EscolherCategoria(int categoria, string arquetipo = null, bool somenteCondicoes = false)
        {
            categoriaAtual = categoria;
            condicoesVilao = categorias[categoria] == "viloes" && somenteCondicoes;
            var entradas = catalogo.Cast<JObject>()
                .Where(c => (string)c["categoria"] == categorias[categoria]).ToArray();
            if (condicoesVilao)
                entradas = entradas.Where(c => (string)c["id"] == "viloes/luta" ||
                    ((string)c["id"] ?? string.Empty).StartsWith("viloes/condicao-")).ToArray();
            var grupos = entradas.Select(c => (string)c["arquetipo"]).Where(a => a != null).Distinct().ToArray();
            arquetipoAtual = grupos.Length == 0 ? null : arquetipo ?? grupos[0];
            if (arquetipoAtual != null)
                entradas = entradas.Where(c => (string)c["arquetipo"] == arquetipoAtual).ToArray();
            nomes = entradas.Select(c => (string)c["titulo"]).ToArray();
            ids = entradas.Select(c => (string)c["id"]).ToArray();
            arquivos = entradas.Select(c => ((string)c["arquivo"]).Replace(".png", string.Empty)).ToArray();
            cards = new Texture2D[arquivos.Length];
            for (var k = 0; k < cards.Length; k++)
                cards[k] = categoria == 0 && panoramas != null && k < panoramas.Length && panoramas[k] != null
                    ? panoramas[k] : Resources.Load<Texture2D>("Cards/Regras/" + arquivos[k]);
            for (var k = 0; k < cortinas.Length; k++)
            {
                cortinas[k].gameObject.SetActive(k == categoria);
                botoesCategoria[k].targetGraphic.color = k == categoria ? Estilo.Dourado
                    : new Color(Estilo.TextoSuave.r, Estilo.TextoSuave.g, Estilo.TextoSuave.b, 0.08f);
                botoesCategoria[k].GetComponentInChildren<TextMeshProUGUI>().color = k == categoria
                    ? Estilo.Campo : Estilo.TextoSuave;
            }
            assuntos = cortinas[categoria];
            foreach (Transform filho in assuntos) Destroy(filho.gameObject);
            if (categorias[categoria] == "viloes" && !contextoCondicoesVilao)
            {
                var b = Estilo.BotaoContorno(assuntos,
                    condicoesVilao ? "Todos os vilões" : "Movimentos de condição", 0, Estilo.Dourado,
                    () => EscolherCategoria(categoria, somenteCondicoes: !condicoesVilao));
                b.GetComponent<LayoutElement>().preferredHeight = 42;
                var textoGrupo = b.GetComponentInChildren<TextMeshProUGUI>();
                textoGrupo.fontSize = 15;
                textoGrupo.characterSpacing = 0;
            }
            foreach (var grupo in grupos)
            {
                var selecionado = grupo;
                var b = Estilo.BotaoContorno(assuntos, grupo, 0, Estilo.Dourado,
                    () => EscolherCategoria(categoria, selecionado));
                b.GetComponent<LayoutElement>().preferredHeight = 42;
                b.GetComponentInChildren<TextMeshProUGUI>().fontSize = 17;
            }
            atalhos = new Button[nomes.Length];
            for (var k = 0; k < nomes.Length; k++)
            {
                var escolhido = k;
                var b = Estilo.BotaoContorno(assuntos, nomes[k], 0, Estilo.TextoSuave, () => Ler(escolhido));
                var tamanho = b.GetComponent<LayoutElement>();
                tamanho.minHeight = tamanho.preferredHeight = 42;
                tamanho.flexibleHeight = 0;
                var texto = b.GetComponentInChildren<TextMeshProUGUI>();
                texto.fontSize = 15;
                texto.enableAutoSizing = true;
                texto.fontSizeMin = 12;
                texto.fontSizeMax = 15;
                texto.characterSpacing = 0;
                texto.textWrappingMode = TextWrappingModes.Normal;
                atalhos[k] = b;
            }
            MontarGaleria();
            areaGaleria.GetComponent<ScrollRect>().verticalNormalizedPosition = 1;
        }

        Texture2D Textura(int k) => cards != null && k < cards.Length ? cards[k] : null;

        void Montar(RectTransform veu)
        {
            var cartao = Estilo.Rect("Cartao", veu);
            cartao.anchorMin = cartao.anchorMax = new Vector2(0.5f, 0.5f);
            cartao.sizeDelta = new Vector2(1680, 940);
            Estilo.Moldura(cartao.gameObject, Estilo.Cartao, Estilo.Borda);
            var coluna = cartao.gameObject.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(48, 48, 32, 28);
            coluna.spacing = 16;
            coluna.childControlWidth = coluna.childControlHeight = true;
            coluna.childForceExpandHeight = false;
            var topo = Estilo.Fila(cartao, 62);
            titulo = Estilo.Rotulo(topo, "AJUDA", 36, Estilo.Dourado, FontStyles.Bold);
            Estilo.ColunaElastica(titulo);
            Estilo.BotaoContorno(topo, "Voltar", 190, Estilo.TextoSuave, Voltar);
            var corpo = Estilo.Fila(cartao, 0, 24);
            var corpoLayout = corpo.GetComponent<LayoutElement>();
            corpoLayout.minHeight = corpoLayout.preferredHeight = 0;
            corpoLayout.flexibleHeight = 1;
            var lateral = Estilo.Rect("Categorias", corpo);
            var larguraLateral = lateral.gameObject.AddComponent<LayoutElement>();
            larguraLateral.layoutPriority = 2;
            larguraLateral.minWidth = larguraLateral.preferredWidth = 200;
            larguraLateral.flexibleWidth = 0;
            Estilo.Moldura(lateral.gameObject, Estilo.Campo, Estilo.Borda);
            var menu = lateral.gameObject.AddComponent<VerticalLayoutGroup>();
            menu.padding = new RectOffset(12, 12, 16, 16);
            menu.spacing = 12;
            menu.childControlWidth = menu.childControlHeight = true;
            menu.childForceExpandHeight = false;
            Estilo.Rotulo(lateral, "BIBLIOTECA", 18, Estilo.TextoSuave, FontStyles.Bold);
            // O conte?do da lateral rola quando os assuntos ultrapassam a altura.
            var listaMenu = Estilo.ListaRolavel(lateral, 10);
            listaMenu.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 0, 0);
            cortinas = new RectTransform[categorias.Length];
            botoesCategoria = new Button[categorias.Length];
            for (var categoria = 0; categoria < categorias.Length; categoria++)
            {
                var escolhida = categoria;
                var b = Estilo.BotaoContorno(listaMenu, categoriasNomes[categoria], 0,
                    Estilo.TextoSuave, () =>
                    {
                        var recolher = categoriaAtual == escolhida && areaGaleria.gameObject.activeSelf
                            && assuntos.gameObject.activeSelf;
                        EscolherCategoria(escolhida);
                        assuntos.gameObject.SetActive(!recolher);
                    });
                b.GetComponent<LayoutElement>().preferredHeight = 64;
                var t = b.GetComponentInChildren<TextMeshProUGUI>();
                t.fontSize = 19;
                t.characterSpacing = 0;
                t.textWrappingMode = TextWrappingModes.Normal;
                b.gameObject.AddComponent<Dica>().Configurar(categoria == 0
                    ? "Fundamentos para consultar durante o jogo."
                    : categoria == 1 ? "Gatilhos, rolagens, escolhas e consequências dos oito movimentos básicos."
                    : categoria == 2 ? "Regras e variantes para ações entre personagens de jogadores."
                    : categoria == 3 ? "Entender, usar, renunciar e rejeitar Influência."
                    : categoria == 4 ? "Penalidades, marcação e formas de remover condições."
                    : categoria == 5 ? "Pilha de Equipe e início e fim de sessão."
                    : categoria == 6 ? "Celebração e vulnerabilidade: movimentos de equipe dos dez arquétipos."
                    : categoria == 7 ? "Movimentos próprios de cada arquétipo, agrupados por classe."
                    : "Consulta do mestre: criar vilões, conduzir conflitos e reagir às condições.");
                botoesCategoria[categoria] = b;
                var cortinaRect = Estilo.Rect("Assuntos " + categoriasNomes[categoria], listaMenu);
                var cortina = cortinaRect.gameObject.AddComponent<VerticalLayoutGroup>();
                cortina.padding = new RectOffset(8, 0, 0, 0);
                cortina.spacing = 5;
                cortina.childControlWidth = cortina.childControlHeight = true;
                cortina.childForceExpandHeight = false;
                cortinas[categoria] = cortinaRect;
            }

            var direita = Estilo.Rect("Conteudo", corpo);
            direita.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var conteudoLayout = direita.gameObject.AddComponent<VerticalLayoutGroup>();
            conteudoLayout.childControlWidth = conteudoLayout.childControlHeight = true;
            conteudoLayout.childForceExpandHeight = false;
            areaGaleria = Estilo.Rect("GaleriaRolavel", direita);
            areaGaleria.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            areaGaleria.gameObject.AddComponent<Image>().color = Color.clear;
            var scroll = areaGaleria.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 35;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Estilo.Rect("Viewport", areaGaleria);
            Estilo.Esticar(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            galeria = Estilo.Rect("Cartas", viewport);
            galeria.anchorMin = new Vector2(0, 1);
            galeria.anchorMax = new Vector2(1, 1);
            galeria.pivot = new Vector2(0.5f, 1);
            galeria.sizeDelta = Vector2.zero;
            var grade = galeria.gameObject.AddComponent<GridLayoutGroup>();
            grade.cellSize = new Vector2(370, 578);
            grade.spacing = new Vector2(18, 24);
            grade.padding = new RectOffset(10, 10, 6, 18);
            grade.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grade.constraintCount = 3;
            galeria.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = galeria;
            leitor = Estilo.Rect("Leitor", direita);
            leitor.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            ampliada = Imagem(leitor);
            var rodape = Estilo.Fila(cartao, 58);
            anterior = Estilo.BotaoContorno(rodape, "Anterior", 200, Estilo.TextoSuave,
                () => Ler(indice - 1));
            pagina = Estilo.Rotulo(rodape, string.Empty, 22, Estilo.TextoSuave);
            pagina.alignment = TextAlignmentOptions.Center;
            Estilo.ColunaElastica(pagina);
            proxima = Estilo.BotaoContorno(rodape, "Próxima", 200, Estilo.TextoSuave,
                () => Ler(indice + 1));
        }

        static RawImage Imagem(Transform pai)
        {
            var area = Estilo.Rect("Imagem", pai);
            var imagem = area.gameObject.AddComponent<RawImage>();
            imagem.raycastTarget = false;
            var ajuste = area.gameObject.AddComponent<AspectRatioFitter>();
            ajuste.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            ajuste.aspectRatio = 672f / 1050f;
            return imagem;
        }

        void MostrarGaleria()
        {
            assuntos.gameObject.SetActive(true);
            DestacarAssunto(-1);
            areaGaleria.gameObject.SetActive(true);
            leitor.gameObject.SetActive(false);
            titulo.text = "AJUDA · " + categoriasNomes[categoriaAtual].ToUpperInvariant();
            if (arquetipoAtual != null) titulo.text += " · " + arquetipoAtual.ToUpperInvariant();
            if (condicoesVilao) titulo.text = "AJUDA · MOVIMENTOS DE CONDIÇÃO DO VILÃO";
            pagina.text = "Escolha uma carta para ampliar";
            anterior.gameObject.SetActive(false);
            proxima.gameObject.SetActive(false);
        }

        void Ler(int escolhido)
        {
            indice = Mathf.Clamp(escolhido, 0, nomes.Length - 1);
            assuntos.gameObject.SetActive(true);
            DestacarAssunto(indice);
            areaGaleria.gameObject.SetActive(false);
            leitor.gameObject.SetActive(true);
            ampliada.texture = Textura(indice);
            ampliada.enabled = ampliada.texture != null;
            titulo.text = "AJUDA · " + nomes[indice].ToUpperInvariant();
            pagina.text = ampliada.texture != null ? $"{indice + 1} / {nomes.Length}"
                : "Imagem não ligada no Canvas (Cards Ajuda).";
            anterior.gameObject.SetActive(true);
            proxima.gameObject.SetActive(true);
            anterior.interactable = indice > 0;
            proxima.interactable = indice < nomes.Length - 1;
        }

        void Voltar()
        {
            if (leitor.gameObject.activeSelf) MostrarGaleria();
            else gameObject.SetActive(false);
        }

        void DestacarAssunto(int selecionado)
        {
            for (var k = 0; k < atalhos.Length; k++)
            {
                atalhos[k].targetGraphic.color = k == selecionado
                    ? Estilo.Dourado : new Color(Estilo.TextoSuave.r, Estilo.TextoSuave.g, Estilo.TextoSuave.b, 0.08f);
                atalhos[k].GetComponentInChildren<TextMeshProUGUI>().color = k == selecionado
                    ? Estilo.Campo : Estilo.TextoSuave;
            }
        }
    }
}
