using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(DadoD6))]
public class ArremessoDado : MonoBehaviour
{
    [SerializeField] private Vector3 origem = new Vector3(0f, 1f, -4f);
    [Header("Sons")]
    [SerializeField] private AudioClip somRolagem = null;
    [SerializeField] private AudioClip somQuique = null;
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;
    [SerializeField, Min(0f)] private float velocidadeMinimaQuique = 0.3f;
    private Rigidbody corpo;
    private AudioSource fonte;
    private static int proximaOrdemAudio;
    private int ordemAudio;
    internal float VelocidadeMinimaQuique => velocidadeMinimaQuique;

    public void AguardarArremesso()
    {
        fonte.Stop();
        if (!corpo.isKinematic)
        {
            corpo.linearVelocity = Vector3.zero;
            corpo.angularVelocity = Vector3.zero;
        }
        corpo.isKinematic = true;
        corpo.detectCollisions = false;
        foreach (var visual in GetComponentsInChildren<Renderer>())
            visual.enabled = false;
        GetComponent<DadoD6>().ReiniciarLeitura();
    }

    private void Awake()
    {
        ordemAudio = proximaOrdemAudio++;
        corpo = GetComponent<Rigidbody>();
        corpo.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        corpo.interpolation = RigidbodyInterpolation.Interpolate;
        fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.loop = false;
        fonte.spatialBlend = 0f;
    }

    public void Rolar() => Rolar(origem);

    public void Rolar(Vector3 posicaoInicial, bool tocarSomRolagem = true)
    {
        corpo.isKinematic = false;
        corpo.detectCollisions = true;
        foreach (var visual in GetComponentsInChildren<Renderer>())
            visual.enabled = true;
        // Clear the previous throw's sounds when the player rolls again.
        fonte.Stop();
        if (tocarSomRolagem && somRolagem != null)
            fonte.PlayOneShot(somRolagem, volume);

        var dado = GetComponent<DadoD6>();
        if (dado != null)
            dado.ReiniciarLeitura();

        AplicarLancamento(corpo, posicaoInicial);
    }

    internal static void AplicarLancamento(Rigidbody alvo, Vector3 posicaoInicial)
    {
        alvo.linearVelocity = Vector3.zero;
        alvo.angularVelocity = Vector3.zero;
        alvo.position = posicaoInicial;
        alvo.rotation = Random.rotation;
        alvo.WakeUp();
        alvo.AddForce(
            new Vector3(Random.Range(-1f, 1f), 6f, 4f),
            ForceMode.Impulse
        );

        alvo.AddTorque(
            Random.onUnitSphere * 5f,
            ForceMode.Impulse
        );
    }

    internal void IniciarReproducaoDebug(Vector3 posicao, Quaternion rotacao, bool som)
    {
        AguardarArremesso();
        AplicarPoseDebug(posicao, rotacao);
        foreach (var visual in GetComponentsInChildren<Renderer>())
            visual.enabled = true;
        if (som && somRolagem != null)
            fonte.PlayOneShot(somRolagem, volume);
    }

    internal void AplicarPoseDebug(Vector3 posicao, Quaternion rotacao)
    {
        corpo.position = posicao;
        corpo.rotation = rotacao;
        transform.SetPositionAndRotation(posicao, rotacao);
    }

    internal void TocarQuiqueDebug()
    {
        if (somQuique != null)
            fonte.PlayOneShot(somQuique, volume);
    }

    private void OnCollisionEnter(Collision colisao)
    {
        if (somQuique == null || colisao.relativeVelocity.sqrMagnitude
            < velocidadeMinimaQuique * velocidadeMinimaQuique)
            return;

        // Both dice receive the collision: only one plays this impact.
        var outroDado = colisao.rigidbody != null
            ? colisao.rigidbody.GetComponent<ArremessoDado>() : null;
        if (outroDado != null && outroDado.isActiveAndEnabled
            && outroDado.somQuique != null && ordemAudio > outroDado.ordemAudio)
            return;

        fonte.PlayOneShot(somQuique, volume);
    }

}
