using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A transparent dice stage above the existing game interface.</summary>
public class SobreposicaoDados : MonoBehaviour
{
    [SerializeField] private GameObject palcoPrefab = null;
    [SerializeField] private bool mostrarControlesTemporarios = true;
    [SerializeField, Min(0f)] private float segundosExibindoResultado = 3f;
    [SerializeField, Min(0.05f)] private float duracaoFade = 0.5f;
    private GameObject palco;
    private GameObject interfaceDados;
    private Camera cameraDados;
    private RenderTexture imagem;
    private RawImage visual;
    private TMP_Text placar;
    private Button botao;
    private RolagemDados rolagem;
    private readonly Vector3 posicaoPalco = new Vector3(1000f, 0f, 0f);
    private Resultado2D6? ultimoResultado;
    private float instanteResultado;
    private int largura;
    private int altura;

    public bool EmRolagem => rolagem != null && rolagem.EmRolagem;

    /// <summary>Await both dice and the sum (only at the end); modifiers belong to the caller.</summary>
    public Task<Resultado2D6> Rolar2D6Async() => rolagem.Rolar2D6Async();

    private void Awake()
    {
        if (palcoPrefab == null)
        {
            Debug.LogError("Configure o prefab do palco de dados.", this);
            enabled = false;
            return;
        }

        palco = Instantiate(palcoPrefab, posicaoPalco, Quaternion.identity, transform);
        rolagem = palco.GetComponentInChildren<RolagemDados>();
        rolagem.DefinirOrigem(posicaoPalco + new Vector3(0f, 1f, -4f));
        rolagem.RolagemConcluida += GuardarResultado;
        int camada = LayerMask.NameToLayer("Dados");
        foreach (var parte in palco.GetComponentsInChildren<Transform>(true))
            parte.gameObject.layer = camada;

        var objetoCamera = new GameObject("Camera dos dados", typeof(Camera));
        objetoCamera.transform.SetParent(transform, false);
        objetoCamera.transform.SetPositionAndRotation(posicaoPalco + Vector3.up * 12f,
            Quaternion.Euler(90f, 0f, 0f));
        cameraDados = objetoCamera.GetComponent<Camera>();
        cameraDados.clearFlags = CameraClearFlags.SolidColor;
        cameraDados.backgroundColor = Color.clear;
        cameraDados.cullingMask = 1 << camada;
        cameraDados.nearClipPlane = 0.1f;
        cameraDados.farClipPlane = 30f;
        cameraDados.allowHDR = false;
        cameraDados.allowMSAA = false;

        CriarInterface();
        AtualizarImagem();
        foreach (var dado in palco.GetComponentsInChildren<ArremessoDado>())
            dado.AguardarArremesso();
    }

    private void GuardarResultado(Resultado2D6 valor)
    {
        ultimoResultado = valor;
        instanteResultado = Time.unscaledTime;
    }

    private void CriarInterface()
    {
        interfaceDados = new GameObject("Dados sobre o jogo", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        interfaceDados.transform.SetParent(transform, false);
        var canvas = interfaceDados.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var escala = interfaceDados.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1280f, 720f);
        escala.matchWidthOrHeight = 0.5f;

        var plano = new GameObject("Imagem transparente", typeof(RectTransform), typeof(RawImage));
        plano.transform.SetParent(interfaceDados.transform, false);
        visual = plano.GetComponent<RawImage>();
        visual.raycastTarget = false;
        var rect = visual.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        if (!mostrarControlesTemporarios)
            return;

        placar = CriarTexto("Resultado temporario", interfaceDados.transform, "2d6");
        Posicionar(placar.rectTransform, new Vector2(1f, 1f), new Vector2(-135f, -35f),
            new Vector2(240f, 50f));

        var objetoBotao = new GameObject("Rolar 2d6 (temporario)", typeof(RectTransform),
            typeof(Image), typeof(Button));
        objetoBotao.transform.SetParent(interfaceDados.transform, false);
        var fundo = objetoBotao.GetComponent<Image>();
        fundo.color = new Color(0.12f, 0.2f, 0.32f, 0.95f);
        botao = objetoBotao.GetComponent<Button>();
        botao.targetGraphic = fundo;
        botao.onClick.AddListener(() => rolagem.Rolar());
        Posicionar((RectTransform)objetoBotao.transform, new Vector2(1f, 0f),
            new Vector2(-115f, 40f), new Vector2(200f, 50f));
        var legenda = CriarTexto("Legenda", objetoBotao.transform, "Rolar 2d6");
        legenda.rectTransform.anchorMin = Vector2.zero;
        legenda.rectTransform.anchorMax = Vector2.one;
        legenda.rectTransform.offsetMin = legenda.rectTransform.offsetMax = Vector2.zero;
    }

    private static TMP_Text CriarTexto(string nome, Transform pai, string conteudo)
    {
        var objeto = new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI));
        objeto.transform.SetParent(pai, false);
        var texto = objeto.GetComponent<TextMeshProUGUI>();
        texto.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        texto.text = conteudo;
        texto.fontSize = 26f;
        texto.color = Color.white;
        texto.alignment = TextAlignmentOptions.Center;
        texto.outlineWidth = 0.2f;
        texto.outlineColor = Color.black;
        texto.raycastTarget = false;
        return texto;
    }

    private static void Posicionar(RectTransform rect, Vector2 ancora, Vector2 posicao, Vector2 tamanho)
    {
        rect.anchorMin = rect.anchorMax = ancora;
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;
    }

    private void AtualizarImagem()
    {
        largura = Mathf.Max(1, Screen.width);
        altura = Mathf.Max(1, Screen.height);
        cameraDados.targetTexture = null;
        if (imagem != null)
        {
            imagem.Release();
            Destroy(imagem);
        }
        float reducao = Mathf.Min(1f, 2048f / Mathf.Max(largura, altura));
        imagem = new RenderTexture(Mathf.Max(1, Mathf.RoundToInt(largura * reducao)),
            Mathf.Max(1, Mathf.RoundToInt(altura * reducao)), 24, RenderTextureFormat.ARGB32);
        imagem.name = "Dados transparentes";
        imagem.Create();
        cameraDados.targetTexture = imagem;
        cameraDados.aspect = (float)largura / altura;
        cameraDados.fieldOfView = 2f * Mathf.Atan(5.5f / (12f * Mathf.Min(1f, cameraDados.aspect))) * Mathf.Rad2Deg;
        visual.texture = imagem;
    }

    private void Update()
    {
        if (Screen.width != largura || Screen.height != altura)
            AtualizarImagem();
        if (botao != null)
            botao.interactable = !EmRolagem;

        float opacidade = 0f;
        if (EmRolagem)
            opacidade = 1f;
        else if (ultimoResultado.HasValue)
        {
            float tempoFade = Time.unscaledTime - instanteResultado - segundosExibindoResultado;
            opacidade = 1f - Mathf.Clamp01(tempoFade / Mathf.Max(0.05f, duracaoFade));
        }
        // Fade the rendered overlay, including every face, without changing materials.
        visual.color = new Color(1f, 1f, 1f, opacidade);
        visual.enabled = opacidade > 0f;
        cameraDados.enabled = opacidade > 0f;
        if (placar != null)
        {
            placar.enabled = opacidade > 0f;
            placar.alpha = opacidade;
            placar.text = EmRolagem ? "Rolando 2d6..."
                : ultimoResultado.HasValue ? "2d6: " + ultimoResultado.Value : "2d6";
        }
    }

    private void OnDestroy()
    {
        if (rolagem != null)
            rolagem.RolagemConcluida -= GuardarResultado;
        if (cameraDados != null)
            cameraDados.targetTexture = null;
        if (imagem != null)
        {
            imagem.Release();
            Destroy(imagem);
        }
    }
}
