using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class DadoD6 : MonoBehaviour
{
    public enum EstiloDeFaces { Numeros, Pontinhos }

    [SerializeField] private EstiloDeFaces estilo = EstiloDeFaces.Numeros;
    [Header("Desempate do dado inclinado")]
    [SerializeField, Min(0.1f)] private float impulsoDesempate = 0.8f;
    [SerializeField, Min(0.1f)] private float giroDesempate = 1.3f;

    // Index + 1 is the value of each face; opposite faces add up to seven.
    private static readonly Vector3[] Normais =
    {
        Vector3.up, Vector3.forward, Vector3.right,
        Vector3.left, Vector3.back, Vector3.down
    };

    private readonly HashSet<Collider> apoios = new HashSet<Collider>();
    private Rigidbody corpo;
    private Material materialPontos;
    private float tempoParado;
    // Internal: only RolagemDados reads it, and only reports it once both dice have settled.
    internal int Resultado { get; private set; }

    public void ReiniciarLeitura()
    {
        apoios.Clear();
        tempoParado = 0f;
        Resultado = 0;
    }

    private void Awake()
    {
        corpo = GetComponent<Rigidbody>();
        if (estilo == EstiloDeFaces.Pontinhos)
        {
            // Reuse the cube shader so the dots work with this project's renderer.
            materialPontos = new Material(GetComponent<MeshRenderer>().sharedMaterial);
            materialPontos.color = Color.white;
            if (materialPontos.HasProperty("_BaseColor"))
                materialPontos.SetColor("_BaseColor", Color.white);
        }

        for (int i = 0; i < Normais.Length; i++)
            CriarFace(i + 1, Normais[i]);
    }

    private void CriarFace(int valor, Vector3 normal)
    {
        var face = new GameObject("Face " + valor).transform;
        face.SetParent(transform, false);
        face.localPosition = normal * 0.501f;
        face.localRotation = Quaternion.LookRotation(normal);

        if (estilo == EstiloDeFaces.Numeros)
        {
            CriarNumero(face, valor, normal);
            return;
        }

        if (valor % 2 == 1)
            CriarPonto(face, 0f, 0f);
        if (valor >= 2)
        {
            CriarPonto(face, -0.23f, -0.23f);
            CriarPonto(face, 0.23f, 0.23f);
        }
        if (valor >= 4)
        {
            CriarPonto(face, -0.23f, 0.23f);
            CriarPonto(face, 0.23f, -0.23f);
        }
        if (valor == 6)
        {
            CriarPonto(face, -0.23f, 0f);
            CriarPonto(face, 0.23f, 0f);
        }
    }

    private void CriarNumero(Transform face, int valor, Vector3 normal)
    {
        // TextMesh Pro is read from its local -Z side.
        face.localRotation = Quaternion.LookRotation(-normal);
        var numero = new GameObject("Numero " + valor, typeof(TextMeshPro));
        numero.layer = gameObject.layer;
        numero.transform.SetParent(face, false);
        var texto = numero.GetComponent<TextMeshPro>();
        texto.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        texto.text = valor.ToString();
        texto.color = Color.white;
        texto.fontSize = 7f;
        texto.fontStyle = FontStyles.Bold;
        texto.alignment = TextAlignmentOptions.Center;
        texto.rectTransform.sizeDelta = new Vector2(0.85f, 0.85f);
        texto.renderer.sortingOrder = 1;
    }

    private void CriarPonto(Transform face, float x, float y)
    {
        var ponto = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ponto.name = "Ponto";
        ponto.layer = gameObject.layer;
        var colisor = ponto.GetComponent<Collider>();
        colisor.enabled = false;
        Destroy(colisor);
        ponto.transform.SetParent(face, false);
        ponto.transform.localPosition = new Vector3(x, y, 0f);
        ponto.transform.localScale = new Vector3(0.14f, 0.14f, 0.012f);
        ponto.GetComponent<MeshRenderer>().sharedMaterial = materialPontos;
    }

    private void FixedUpdate()
    {
        if (corpo.isKinematic)
            return;
        if (apoios.Count == 0 || corpo.linearVelocity.sqrMagnitude > 0.0025f
            || corpo.angularVelocity.sqrMagnitude > 0.01f)
        {
            tempoParado = 0f;
            Resultado = 0;
            return;
        }

        tempoParado += Time.fixedDeltaTime;
        if (tempoParado < 0.5f || Resultado != 0)
            return;

        Resultado = LerFaceSuperior(corpo.rotation);
        if (Resultado == 0)
            Desempatar();
    }

    internal static int LerFaceSuperior(Quaternion rotacao)
    {
        int valor = 0;
        float melhor = 0.95f;
        for (int i = 0; i < Normais.Length; i++)
        {
            float alinhamento = Vector3.Dot(rotacao * Normais[i], Vector3.up);
            if (alinhamento > melhor)
            {
                melhor = alinhamento;
                valor = i + 1;
            }
        }
        return valor;
    }

    // A cube has 24 equivalent geometric orientations. Pick one before playback:
    // the entire recorded path stays physically identical, with the requested face up.
    internal static Quaternion OrientarTrajetoria(int desejado, int simulado)
    {
        foreach (var frente in Normais)
        foreach (var cima in Normais)
        {
            if (Mathf.Abs(Vector3.Dot(frente, cima)) > 0.01f)
                continue;
            var ajuste = Quaternion.LookRotation(frente, cima);
            if (Vector3.Dot(ajuste * Normais[desejado - 1], Normais[simulado - 1]) > 0.99f)
                return ajuste;
        }
        throw new System.InvalidOperationException("Nao foi possivel orientar o dado de debug.");
    }

    private void Desempatar()
    {
        tempoParado = 0f;
        DesempatarCorpo(corpo);
    }

    internal void DesempatarCorpo(Rigidbody alvo)
    {
        // A small physical nudge, independent of mass; never choose a face.
        float angulo = Random.Range(0f, Mathf.PI * 2f);
        var direcao = new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo));
        alvo.WakeUp();
        alvo.AddForce((Vector3.up + direcao * 0.2f) * impulsoDesempate,
            ForceMode.VelocityChange);
        alvo.AddTorque(Vector3.Cross(Vector3.up, direcao) * giroDesempate,
            ForceMode.VelocityChange);
    }

    internal void ConfirmarPoseDebug() => Resultado = LerFaceSuperior(corpo.rotation);

    private void OnCollisionEnter(Collision colisao) => AtualizarApoio(colisao);
    private void OnCollisionStay(Collision colisao) => AtualizarApoio(colisao);
    private void OnCollisionExit(Collision colisao) => apoios.Remove(colisao.collider);

    private void AtualizarApoio(Collision colisao)
    {
        for (int i = 0; i < colisao.contactCount; i++)
        {
            // Another die can support this one on a steep edge as well.
            // Any upward support counts, even when shared between several dice.
            if (Vector3.Dot(colisao.GetContact(i).normal, Vector3.up) > 0.01f)
            {
                apoios.Add(colisao.collider);
                return;
            }
        }
        apoios.Remove(colisao.collider);
    }

    private void OnDestroy()
    {
        if (materialPontos != null)
            Destroy(materialPontos);
    }
}
