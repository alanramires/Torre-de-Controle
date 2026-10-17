using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class BordasMesa : MonoBehaviour
{
    [SerializeField, Min(1f)] private float altura = 6f;
    [SerializeField, Min(0.1f)] private float espessura = 0.4f;
    private GameObject bordas;

    private void Awake()
    {
        // Match the table's world bounds; these walls have no visible mesh.
        Bounds mesa = GetComponent<BoxCollider>().bounds;
        bordas = new GameObject("Bordas invisiveis");
        bordas.transform.SetParent(transform, true);
        float y = mesa.max.y + altura / 2f;
        float margem = espessura / 2f;

        CriarBorda("Esquerda", new Vector3(mesa.min.x - margem, y, mesa.center.z),
            new Vector3(espessura, altura, mesa.size.z + 2f * espessura));
        CriarBorda("Direita", new Vector3(mesa.max.x + margem, y, mesa.center.z),
            new Vector3(espessura, altura, mesa.size.z + 2f * espessura));
        CriarBorda("Inferior", new Vector3(mesa.center.x, y, mesa.min.z - margem),
            new Vector3(mesa.size.x, altura, espessura));
        CriarBorda("Superior", new Vector3(mesa.center.x, y, mesa.max.z + margem),
            new Vector3(mesa.size.x, altura, espessura));
    }

    private void CriarBorda(string nome, Vector3 centro, Vector3 tamanho)
    {
        var parede = new GameObject(nome);
        parede.layer = gameObject.layer;
        parede.transform.SetParent(bordas.transform, false);
        var colisor = parede.AddComponent<BoxCollider>();
        colisor.center = centro;
        colisor.size = tamanho;
    }

    private void OnDestroy()
    {
        if (bordas != null)
            Destroy(bordas);
    }
}
