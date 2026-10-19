using UnityEngine;

// Only used by invisible bodies in the isolated debug physics scene.
internal sealed class ImpactoDadoDebug : MonoBehaviour
{
    internal int Ordem;
    internal float VelocidadeMinima;
    internal bool HouveImpacto;

    private void OnCollisionEnter(Collision colisao)
    {
        if (colisao.relativeVelocity.sqrMagnitude < VelocidadeMinima * VelocidadeMinima)
            return;
        var outro = colisao.rigidbody != null
            ? colisao.rigidbody.GetComponent<ImpactoDadoDebug>() : null;
        if (outro == null || Ordem < outro.Ordem)
            HouveImpacto = true;
    }
}
