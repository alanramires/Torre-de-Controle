using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TorreDeControle.UI
{
    /// <summary>
    /// A alça "=" no começo de uma linha: arrastar reordena a linha entre as irmãs do mesmo bloco.
    /// Durante o arrasto a linha troca de lugar na hora (o layout acompanha); ao soltar, avisa a
    /// posição final para o cadastro gravar. Como a alça trata o arrasto, a lista não rola junto.
    /// </summary>
    public sealed class AlcaArrastar : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        RectTransform linha;
        Action<int> aoSoltar;
        CanvasGroup grupo;
        int inicio;

        public void Configurar(RectTransform linhaArrastada, Action<int> aoSoltarNaPosicao)
        {
            linha = linhaArrastada;
            aoSoltar = aoSoltarNaPosicao;
        }

        public void OnBeginDrag(PointerEventData e)
        {
            inicio = linha.GetSiblingIndex();
            if (!linha.TryGetComponent(out grupo))
                grupo = linha.gameObject.AddComponent<CanvasGroup>();
            grupo.alpha = 0.55f;
        }

        public void OnDrag(PointerEventData e)
        {
            var bloco = linha.parent;
            var atual = linha.GetSiblingIndex();
            var y = e.position.y;

            // Passou do meio da irmã de cima: sobe uma. Passou do meio da de baixo: desce uma.
            if (atual > 0 && y > Meio(bloco.GetChild(atual - 1), e))
                linha.SetSiblingIndex(atual - 1);
            else if (atual < bloco.childCount - 1 && y < Meio(bloco.GetChild(atual + 1), e))
                linha.SetSiblingIndex(atual + 1);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (grupo != null)
                grupo.alpha = 1f;
            var fim = linha.GetSiblingIndex();
            if (fim != inicio)
                aoSoltar?.Invoke(fim);
        }

        static float Meio(Transform irma, PointerEventData e) =>
            RectTransformUtility.WorldToScreenPoint(e.pressEventCamera, irma.position).y;
    }
}
