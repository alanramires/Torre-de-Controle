using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>O resultado de uma rolagem de 2d6, entregue só quando os dois dados param (sem spoiler).</summary>
public readonly struct Resultado2D6
{
    public readonly int Dado1;
    public readonly int Dado2;
    public int Soma => Dado1 + Dado2;

    public Resultado2D6(int dado1, int dado2)
    {
        Dado1 = dado1;
        Dado2 = dado2;
    }

    public override string ToString() => Dado1 + " + " + Dado2 + " = " + Soma;
}

public class RolagemDados : MonoBehaviour
{
    [SerializeField] private ArremessoDado[] dados = new ArremessoDado[2];
    [SerializeField] private Vector3 origem = new Vector3(0f, 1f, -4f);
    [SerializeField, Min(1.8f)] private float separacao = 2f;
    [SerializeField, Min(0.2f)] private float intervaloEntreGrupos = 0.35f;
    [SerializeField] private bool rolarAoIniciar = true;
    [SerializeField] private bool mostrarInterfaceTemporaria = true;
    private DadoD6[] leituras;
    private Coroutine lancamento;
    private TaskCompletionSource<Resultado2D6> conclusao;
    private Resultado2D6? ultimo;
    private bool lancando;
    private float tempoConfirmacao;

    public bool EmRolagem => conclusao != null && !conclusao.Task.IsCompleted;
    public event Action<Resultado2D6> RolagemConcluida;

    public void DefinirOrigem(Vector3 posicao) => origem = posicao;

    /// <summary>
    /// Rola os dados físicos e devolve os dois valores e a soma só depois que ambos param; quem chama
    /// aplica os modificadores do jogo. Durante a rolagem nenhum valor é exposto.
    /// </summary>
    public Task<Resultado2D6> Rolar2D6Async()
    {
        if (!isActiveAndEnabled)
            throw new InvalidOperationException("O controlador de dados precisa estar ativo.");
        if (!PrepararDados() || dados.Length != 2)
            throw new InvalidOperationException("A rolagem 2d6 precisa de exatamente dois dados.");
        if (!EmRolagem)
            Rolar();
        return conclusao.Task;
    }

    // Zero means that at least one die is still rolling. Private: reading it mid-roll would spoil the result.
    private int ResultadoTotal
    {
        get
        {
            if (leituras == null || leituras.Length == 0)
                return 0;

            int soma = 0;
            foreach (var dado in leituras)
            {
                if (dado == null || dado.Resultado == 0)
                    return 0;
                soma += dado.Resultado;
            }
            return soma;
        }
    }

    private void OnEnable() => leituras = null;

    private bool PrepararDados()
    {
        if (leituras != null && Array.TrueForAll(leituras, dado => dado != null)
            && dados != null && dados.Length == leituras.Length
            && Array.TrueForAll(dados, dado => dado != null))
            return true;

        if (dados == null || dados.Length == 0
            || Array.Exists(dados, dado => dado == null)
            || new HashSet<ArremessoDado>(dados).Count != dados.Length)
        {
            var encontrados = Array.FindAll(
                FindObjectsByType<ArremessoDado>(),
                dado => dado.gameObject.scene == gameObject.scene);
            // Recover missing Inspector references without including another scene.
            if (encontrados.Length == 0)
                return false;
            Array.Sort(encontrados, (a, b) => string.CompareOrdinal(a.name, b.name));
            dados = encontrados;
        }

        var novasLeituras = new DadoD6[dados.Length];
        for (int i = 0; i < dados.Length; i++)
        {
            if (!dados[i].TryGetComponent(out novasLeituras[i]))
                return false;
        }
        // Publish the cache only after every entry has been validated.
        leituras = novasLeituras;
        return true;
    }

    private void Start()
    {
        if (rolarAoIniciar)
            Rolar();
    }

    public void Rolar()
    {
        if (!enabled || EmRolagem || !PrepararDados())
            return;

        conclusao = new TaskCompletionSource<Resultado2D6>();
        ultimo = null;
        tempoConfirmacao = 0f;
        lancando = true;
        if (lancamento != null)
            StopCoroutine(lancamento);
        foreach (var dado in dados)
            dado.AguardarArremesso();
        lancamento = StartCoroutine(LancarGrupos());
    }

    private IEnumerator LancarGrupos()
    {
        Bounds mesa = TryGetComponent<BoxCollider>(out var colisorMesa)
            ? colisorMesa.bounds : new Bounds(Vector3.down * 0.25f, new Vector3(10f, 0.5f, 10f));
        float margem = 0f;
        foreach (var dado in dados)
        {
            var caixa = dado.GetComponent<BoxCollider>();
            // Bounding sphere contains the cube at every random rotation.
            float raio = Vector3.Scale(caixa.size, dado.transform.lossyScale).magnitude * 0.5f;
            margem = Mathf.Max(margem, raio + 0.1f);
        }
        float passo = Mathf.Max(separacao, margem * 2f);
        int porGrupo = Mathf.Max(1, Mathf.FloorToInt((mesa.size.x - margem * 2f) / passo) + 1);
        float z = Mathf.Clamp(origem.z, mesa.min.z + margem, mesa.max.z - margem);
        float y = Mathf.Max(origem.y, mesa.max.y + margem);

        for (int inicio = 0; inicio < dados.Length; inicio += porGrupo)
        {
            int quantidade = Mathf.Min(porGrupo, dados.Length - inicio);
            float meiaLargura = (quantidade - 1) * passo * 0.5f;
            float centroX = Mathf.Clamp(origem.x, mesa.min.x + margem + meiaLargura,
                mesa.max.x - margem - meiaLargura);
            for (int i = 0; i < quantidade; i++)
            {
                var posicao = new Vector3(centroX + i * passo - meiaLargura, y, z);
                dados[inicio + i].Rolar(posicao, i == 0);
            }
            if (inicio + quantidade < dados.Length)
                yield return new WaitForSeconds(intervaloEntreGrupos);
        }
        lancamento = null;
        lancando = false;
    }

    private void Update()
    {
        if (!EmRolagem)
            return;
        int total = ResultadoTotal;
        if (lancando || total == 0)
        {
            tempoConfirmacao = 0f;
            return;
        }
        // Give both dice time to settle after their last mutual collision.
        tempoConfirmacao += Time.deltaTime;
        if (tempoConfirmacao < 0.25f)
            return;
        var resultado = new Resultado2D6(leituras[0].Resultado, leituras[1].Resultado);
        ultimo = resultado;
        conclusao.TrySetResult(resultado);
        RolagemConcluida?.Invoke(resultado);
    }

    private void OnDisable()
    {
        if (lancamento != null)
            StopCoroutine(lancamento);
        lancamento = null;
        lancando = false;
        conclusao?.TrySetCanceled();
    }

    private void OnGUI()
    {
        if (!mostrarInterfaceTemporaria)
            return;
        bool pronto = PrepararDados();
        // Values only after the roll is confirmed, never die by die.
        string texto = !pronto ? "Vincule os dados na Mesa > Rolagem Dados"
            : EmRolagem ? "Rolando " + dados.Length + "d6..."
            : ultimo.HasValue ? ultimo.Value.ToString() : dados.Length + "d6";
        float largura = Mathf.Min(640f, Screen.width - 20f);
        GUI.Box(new Rect((Screen.width - largura) / 2f, 20f, largura, 60f), texto);

        bool habilitadoAntes = GUI.enabled;
        GUI.enabled = habilitadoAntes && pronto && !EmRolagem;
        if (GUI.Button(new Rect((Screen.width - 160f) / 2f, Screen.height - 65f, 160f, 45f),
            pronto ? "Rolar " + dados.Length + "d6" : "Rolar 2d6"))
            Rolar();
        GUI.enabled = habilitadoAntes;
    }
}
