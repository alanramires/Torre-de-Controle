using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Balão que aparece com o mouse em cima (tooltip). Fica por cima de tudo, perto do alvo,
    /// e some ao sair. Para o que está na ficha impressa e serve de inspiração, sem ocupar a tela.
    /// </summary>
    public sealed class Dica : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        const float Largura = 680;

        string texto;
        GameObject balao;

        public void Configurar(string conteudo) => texto = conteudo;

        public void OnPointerEnter(PointerEventData e)
        {
            if (balao != null || string.IsNullOrEmpty(texto))
                return;

            var raiz = (RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform;
            var caixa = Estilo.Rect("Dica", raiz);
            balao = caixa.gameObject;
            caixa.anchorMin = caixa.anchorMax = new Vector2(0.5f, 0.5f);
            caixa.pivot = new Vector2(0, 1);
            Estilo.Moldura(balao, Estilo.CaixaOpcoes, Estilo.Dourado);
            balao.GetComponent<Image>().raycastTarget = false; // o balão não rouba o mouse do alvo

            var v = balao.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(22, 22, 16, 18);
            v.childControlWidth = true;
            v.childControlHeight = true;
            var ajuste = balao.AddComponent<ContentSizeFitter>();
            ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            caixa.sizeDelta = new Vector2(Largura, 0);

            var t = Estilo.Rotulo(caixa, texto, 22, Estilo.Texto);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.alignment = TextAlignmentOptions.TopLeft;

            // Embaixo e à direita do alvo; se passar da borda direita, alinha pela direita.
            var cantos = new Vector3[4];
            ((RectTransform)transform).GetWorldCorners(cantos);
            Vector2 baixoEsq = raiz.InverseTransformPoint(cantos[0]);
            var x = Mathf.Min(baixoEsq.x, raiz.rect.xMax - Largura - 10);
            caixa.anchoredPosition = new Vector2(x, baixoEsq.y - 6);
            caixa.SetAsLastSibling();
        }

        public void OnPointerExit(PointerEventData e) => Fechar();

        void OnDisable() => Fechar();

        void Fechar()
        {
            if (balao != null)
                Destroy(balao);
            balao = null;
        }
    }
}
