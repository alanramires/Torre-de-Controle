using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Records a hidden physical throw, then replays cube-symmetric orientations for debug.</summary>
internal sealed class RolagemDirigidaDebug : IDisposable
{
    private Scene cena;
    private struct Quadro
    {
        internal Vector3 P0, P1;
        internal Quaternion Q0, Q1;
        internal bool Som0, Som1;
    }

    internal IEnumerator Executar(ArremessoDado[] dados, Vector3 origem, float separacao,
        BoxCollider mesa, int valor1, int valor2, Action<Exception> falhar)
    {
        cena = SceneManager.CreateScene("Simulacao 2d6 debug " + Guid.NewGuid().ToString("N"),
            new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        var fisica = cena.GetPhysicsScene();
        foreach (var caixa in mesa.transform.root.GetComponentsInChildren<BoxCollider>())
        {
            if (!caixa.enabled || caixa.isTrigger || caixa.attachedRigidbody != null)
                continue;
            var copia = NovoObjeto("Colisao debug");
            copia.transform.SetPositionAndRotation(caixa.transform.position, caixa.transform.rotation);
            copia.transform.localScale = caixa.transform.lossyScale;
            CopiarCaixa(caixa, copia);
        }

        var corpos = new Rigidbody[2];
        var sensores = new ImpactoDadoDebug[2];
        float margem = 0f;
        for (int i = 0; i < 2; i++)
        {
            var real = dados[i].GetComponent<Rigidbody>();
            var caixa = dados[i].GetComponent<BoxCollider>();
            var copia = NovoObjeto("Dado simulado " + i);
            copia.transform.localScale = dados[i].transform.lossyScale;
            CopiarCaixa(caixa, copia);
            var corpo = copia.AddComponent<Rigidbody>();
            corpo.mass = real.mass;
            corpo.linearDamping = real.linearDamping;
            corpo.angularDamping = real.angularDamping;
            corpo.maxAngularVelocity = real.maxAngularVelocity;
            corpo.useGravity = real.useGravity;
            corpo.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            corpos[i] = corpo;
            sensores[i] = copia.AddComponent<ImpactoDadoDebug>();
            sensores[i].Ordem = i;
            sensores[i].VelocidadeMinima = dados[i].VelocidadeMinimaQuique;
            margem = Mathf.Max(margem, Vector3.Scale(caixa.size, copia.transform.lossyScale).magnitude * 0.5f + 0.1f);
        }

        var limites = mesa.bounds;
        float passo = Mathf.Max(separacao, margem * 2f);
        if (limites.size.x < passo + margem * 2f || limites.size.z < margem * 2f)
        {
            Dispose();
            falhar(new InvalidOperationException("A mesa nao comporta os dois dados de debug."));
            yield break;
        }
        float x = Mathf.Clamp(origem.x, limites.min.x + margem + passo / 2f,
            limites.max.x - margem - passo / 2f);
        float z = Mathf.Clamp(origem.z, limites.min.z + margem, limites.max.z - margem);
        float y = Mathf.Max(origem.y, limites.max.y + margem);
        float dt = Mathf.Clamp(Time.fixedDeltaTime, 0.005f, 0.05f);
        List<Quadro> quadros = null;
        int face0 = 0, face1 = 0;
        // Bounded work and regular yields keep the game responsive during planning.
        for (int tentativa = 0; tentativa < 3 && face0 == 0; tentativa++)
        {
            var gravacao = new List<Quadro>();
            var parados = new float[2];
            for (int i = 0; i < 2; i++)
            {
                ArremessoDado.AplicarLancamento(corpos[i], new Vector3(x + (i - 0.5f) * passo, y, z));
                sensores[i].HouveImpacto = false;
            }
            gravacao.Add(Gravar(corpos, sensores));
            for (int passoSimulado = 0; passoSimulado < Mathf.CeilToInt(40f / dt); passoSimulado++)
            {
                fisica.Simulate(dt);
                gravacao.Add(Gravar(corpos, sensores));
                for (int i = 0; i < 2; i++)
                {
                    var corpo = corpos[i];
                    bool quieto = corpo.linearVelocity.sqrMagnitude <= 0.0025f
                        && corpo.angularVelocity.sqrMagnitude <= 0.01f;
                    bool apoiado = fisica.Raycast(corpo.position, Vector3.down, out _, margem * 2f + 0.1f);
                    parados[i] = quieto && apoiado ? parados[i] + dt : 0f;
                    if (parados[i] >= 0.5f && DadoD6.LerFaceSuperior(corpo.rotation) == 0)
                    {
                        dados[i].GetComponent<DadoD6>().DesempatarCorpo(corpo);
                        parados[i] = 0f;
                    }
                }
                if (parados[0] >= 0.75f && parados[1] >= 0.75f)
                {
                    face0 = DadoD6.LerFaceSuperior(corpos[0].rotation);
                    face1 = DadoD6.LerFaceSuperior(corpos[1].rotation);
                    if (face0 != 0 && face1 != 0)
                    {
                        quadros = gravacao;
                        break;
                    }
                    face0 = 0;
                }
                if (passoSimulado % 40 == 39)
                    yield return null;
            }
        }
        Dispose();
        if (quadros == null)
        {
            falhar(new TimeoutException("Os dados de debug nao se acomodaram na simulacao. Tente novamente."));
            yield break;
        }

        var ajuste0 = DadoD6.OrientarTrajetoria(valor1, face0);
        var ajuste1 = DadoD6.OrientarTrajetoria(valor2, face1);
        var primeiro = quadros[0];
        dados[0].IniciarReproducaoDebug(primeiro.P0, primeiro.Q0 * ajuste0, true);
        dados[1].IniciarReproducaoDebug(primeiro.P1, primeiro.Q1 * ajuste1, false);
        float tempo = 0f;
        int ultimoSom = 0;
        float duracao = (quadros.Count - 1) * dt;
        while (tempo < duracao)
        {
            float indice = tempo / dt;
            int a = Mathf.Min(Mathf.FloorToInt(indice), quadros.Count - 1);
            int b = Mathf.Min(a + 1, quadros.Count - 1);
            float t = indice - a;
            dados[0].AplicarPoseDebug(Vector3.Lerp(quadros[a].P0, quadros[b].P0, t),
                Quaternion.Slerp(quadros[a].Q0, quadros[b].Q0, t) * ajuste0);
            dados[1].AplicarPoseDebug(Vector3.Lerp(quadros[a].P1, quadros[b].P1, t),
                Quaternion.Slerp(quadros[a].Q1, quadros[b].Q1, t) * ajuste1);
            while (ultimoSom < a)
            {
                var quadro = quadros[++ultimoSom];
                if (quadro.Som0) dados[0].TocarQuiqueDebug();
                if (quadro.Som1) dados[1].TocarQuiqueDebug();
            }
            yield return null;
            tempo += Time.deltaTime;
        }
        var final = quadros[quadros.Count - 1];
        while (ultimoSom < quadros.Count - 1)
        {
            var quadro = quadros[++ultimoSom];
            if (quadro.Som0) dados[0].TocarQuiqueDebug();
            if (quadro.Som1) dados[1].TocarQuiqueDebug();
        }
        dados[0].AplicarPoseDebug(final.P0, final.Q0 * ajuste0);
        dados[1].AplicarPoseDebug(final.P1, final.Q1 * ajuste1);
        foreach (var dado in dados)
            dado.GetComponent<DadoD6>().ConfirmarPoseDebug();
    }

    private GameObject NovoObjeto(string nome)
    {
        var objeto = new GameObject(nome);
        SceneManager.MoveGameObjectToScene(objeto, cena);
        return objeto;
    }

    private static void CopiarCaixa(BoxCollider origem, GameObject alvo)
    {
        var caixa = alvo.AddComponent<BoxCollider>();
        caixa.center = origem.center;
        caixa.size = origem.size;
        caixa.sharedMaterial = origem.sharedMaterial;
    }

    private static Quadro Gravar(Rigidbody[] corpos, ImpactoDadoDebug[] sensores)
    {
        var quadro = new Quadro { P0 = corpos[0].position, P1 = corpos[1].position,
            Q0 = corpos[0].rotation, Q1 = corpos[1].rotation,
            Som0 = sensores[0].HouveImpacto, Som1 = sensores[1].HouveImpacto };
        sensores[0].HouveImpacto = sensores[1].HouveImpacto = false;
        return quadro;
    }

    public void Dispose()
    {
        if (cena.IsValid() && cena.isLoaded)
            SceneManager.UnloadSceneAsync(cena);
        cena = default;
    }
}
