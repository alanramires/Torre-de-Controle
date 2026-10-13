using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Para caixas de texto de várias linhas: o TMP_InputField só repassa a roda do mouse para a lista
    /// quando é de uma linha; nas de várias, fica com o evento (e, com texto curto, não faz nada).
    /// Este componente, no mesmo objeto, entrega a roda à lista quando o texto não tem o que rolar.
    /// </summary>
    public sealed class RepassaRolagem : MonoBehaviour, IScrollHandler
    {
        public void OnScroll(PointerEventData e)
        {
            var campo = GetComponent<TMP_InputField>();
            var texto = campo != null ? campo.textComponent : null;
            var area = campo != null ? campo.textViewport : null;
            var temOQueRolar = texto != null && area != null && texto.preferredHeight > area.rect.height;
            if (temOQueRolar)
                return;
            GetComponentInParent<ScrollRect>()?.OnScroll(e);
        }
    }
}
