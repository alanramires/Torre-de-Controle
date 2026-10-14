using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Mostra num RawImage a imagem de um link (foto da personagem: .jpg, .jpeg ou .png). Baixa uma vez
    /// por link e guarda (a ficha se redesenha muito). Se o servidor não devolver uma imagem jpeg/png,
    /// não mostra nada: o texto avisa e o quadro continua abrindo o link no navegador.
    /// </summary>
    public sealed class FotoDaWeb : MonoBehaviour
    {
        const long LimiteDeBytes = 10 * 1024 * 1024;

        static readonly Dictionary<string, Texture2D> baixadas = new Dictionary<string, Texture2D>();
        static readonly HashSet<string> falharam = new HashSet<string>();

        /// <summary>
        /// O projeto entra no Play sem recarregar o domínio: estáticos sobrevivem de um Play para o outro,
        /// mas as texturas baixadas são destruídas ao sair. Sem limpar, o cache devolve textura morta.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimparAoEntrarNoPlay()
        {
            baixadas.Clear();
            falharam.Clear();
        }

        RawImage imagem;
        AspectRatioFitter proporcao;
        TextMeshProUGUI aviso;
        string link;
        Texture2D padrao;

        public void Mostrar(string url, RawImage alvo, AspectRatioFitter ajuste, TextMeshProUGUI texto,
            Texture2D alternativa = null)
        {
            StopAllCoroutines();
            padrao = alternativa;
            link = url;
            imagem = alvo;
            proporcao = ajuste;
            aviso = texto;
            imagem.enabled = false;
            if (padrao != null)
                Aplicar(padrao);
            if (string.IsNullOrWhiteSpace(link))
            {
                if (padrao == null) Falhou();
                return;
            }

            if (baixadas.TryGetValue(link, out var pronta))
            {
                if (pronta != null)
                {
                    Aplicar(pronta);
                    return;
                }
                baixadas.Remove(link); // destruída: baixa de novo
            }
            if (falharam.Contains(link))
            {
                Falhou();
                return;
            }
            aviso.text = padrao == null ? "carregando..." : string.Empty;
            StartCoroutine(Baixar());
        }

        IEnumerator Baixar()
        {
            using (var pedido = UnityWebRequestTexture.GetTexture(link, nonReadable: true))
            {
                pedido.timeout = 20;
                var operacao = pedido.SendWebRequest();
                while (!operacao.isDone)
                {
                    // Arquivo grande demais: desiste (é uma foto de ficha, não um pôster).
                    if (pedido.downloadedBytes > LimiteDeBytes)
                    {
                        pedido.Abort();
                        break;
                    }
                    yield return null;
                }

                var tipo = (pedido.GetResponseHeader("Content-Type") ?? string.Empty).ToLowerInvariant();
                var ehImagem = tipo.Length == 0 || tipo.Contains("image/jpeg") || tipo.Contains("image/jpg") ||
                               tipo.Contains("image/png");
                if (pedido.result != UnityWebRequest.Result.Success || !ehImagem)
                {
                    falharam.Add(link);
                    Falhou();
                    yield break;
                }
                var textura = DownloadHandlerTexture.GetContent(pedido);
                if (textura == null)
                {
                    falharam.Add(link);
                    Falhou();
                    yield break;
                }
                baixadas[link] = textura;
                Aplicar(textura);
            }
        }

        void Aplicar(Texture2D textura)
        {
            imagem.texture = textura;
            imagem.enabled = true;
            proporcao.aspectRatio = (float)textura.width / Mathf.Max(1, textura.height);
            aviso.text = string.Empty;
        }

        void Falhou()
        {
            if (padrao != null)
                Aplicar(padrao);
            else
                aviso.text = "não deu para mostrar\n<size=80%>(não veio uma imagem .jpg ou .png)\nabrir o link</size>";
        }
    }
}
