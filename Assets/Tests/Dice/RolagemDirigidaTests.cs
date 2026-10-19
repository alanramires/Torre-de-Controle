#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class RolagemDirigidaTests
{
    private GameObject palco;
    private RolagemDados controlador;

    [SetUp]
    public void Preparar()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Palco2D6.prefab");
        Assert.That(prefab, Is.Not.Null);
        palco = UnityEngine.Object.Instantiate(prefab, Vector3.right * 2000f, Quaternion.identity);
        controlador = palco.GetComponentInChildren<RolagemDados>();
        controlador.DefinirOrigem(new Vector3(2000f, 1f, -4f));
    }

    [TearDown]
    public void Limpar()
    {
        if (palco != null)
            UnityEngine.Object.DestroyImmediate(palco);
    }

    [Test]
    public void OrientacoesPreservamGeometriaEExibemCadaFace()
    {
        var ler = typeof(DadoD6).GetMethod("LerFaceSuperior", BindingFlags.NonPublic | BindingFlags.Static);
        var orientar = typeof(DadoD6).GetMethod("OrientarTrajetoria", BindingFlags.NonPublic | BindingFlags.Static);
        Vector3[] normais = { Vector3.up, Vector3.forward, Vector3.right,
            Vector3.left, Vector3.back, Vector3.down };
        for (int simulado = 1; simulado <= 6; simulado++)
        for (int desejado = 1; desejado <= 6; desejado++)
        {
            var final = Quaternion.FromToRotation(normais[simulado - 1], Vector3.up);
            var ajuste = (Quaternion)orientar.Invoke(null, new object[] { desejado, simulado });
            Assert.That((int)ler.Invoke(null, new object[] { final * ajuste }), Is.EqualTo(desejado));
            foreach (var normal in normais)
            {
                var eixo = ajuste * normal;
                Assert.That(Mathf.Max(Mathf.Abs(eixo.x), Mathf.Abs(eixo.y), Mathf.Abs(eixo.z)),
                    Is.EqualTo(1f).Within(0.0001f), "Ajuste deve ser uma simetria do cubo");
            }
        }
    }

    [Test]
    public void RejeitaSomasEParesInvalidos()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => controlador.Rolar2D6ForcadoAsync(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => controlador.Rolar2D6ForcadoAsync(13));
        Assert.Throws<ArgumentOutOfRangeException>(() => controlador.Rolar2D6ForcadoAsync(0, 5));
        Assert.That(controlador.EmRolagem, Is.False);
    }

    [UnityTest]
    public IEnumerator Somas2_5_12CorrespondemAsFacesVisiveis()
    {
        foreach (int soma in new[] { 2, 5, 12 })
        {
            int eventos = 0;
            Action<Resultado2D6> aoConcluir = _ => eventos++;
            controlador.RolagemConcluida += aoConcluir;
            var tarefa = controlador.Rolar2D6ForcadoAsync(soma);
            Assert.That(controlador.EmRolagem, Is.True);
            Assert.Throws<InvalidOperationException>(() => controlador.Rolar2D6ForcadoAsync(soma));
            yield return Aguardar(tarefa);
            Assert.That(tarefa.Result.Soma, Is.EqualTo(soma));
            var dados = palco.GetComponentsInChildren<DadoD6>();
            var ler = typeof(DadoD6).GetMethod("LerFaceSuperior", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That((int)ler.Invoke(null, new object[] { dados[0].transform.rotation }), Is.EqualTo(tarefa.Result.Dado1));
            Assert.That((int)ler.Invoke(null, new object[] { dados[1].transform.rotation }), Is.EqualTo(tarefa.Result.Dado2));
            Assert.That(eventos, Is.EqualTo(1));
            controlador.RolagemConcluida -= aoConcluir;
        }
        // A directed throw must not leave the next ordinary roll frozen or forced.
        var normal = controlador.Rolar2D6Async();
        foreach (var dado in palco.GetComponentsInChildren<Rigidbody>())
            Assert.That(dado.isKinematic, Is.False);
        yield return Aguardar(normal);
        Assert.That(normal.Result.Soma, Is.InRange(2, 12));
    }

    [UnityTest]
    public IEnumerator DesativarCancelaPlanejamento()
    {
        var tarefa = controlador.Rolar2D6ForcadoAsync(5);
        controlador.enabled = false;
        yield return null;
        Assert.That(tarefa.IsCanceled, Is.True);
        Assert.That(controlador.EmRolagem, Is.False);
    }

    private static IEnumerator Aguardar(Task<Resultado2D6> tarefa)
    {
        float prazo = Time.realtimeSinceStartup + 55f;
        while (!tarefa.IsCompleted && Time.realtimeSinceStartup < prazo)
            yield return null;
        Assert.That(tarefa.IsCompleted, Is.True, "Rolagem nao concluiu no prazo");
        if (tarefa.IsFaulted)
            Assert.Fail(tarefa.Exception.ToString());
        Assert.That(tarefa.IsCanceled, Is.False);
    }
}
#endif
