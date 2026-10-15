using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class SomBotao : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        Button botao;
        bool cancelar;
        bool somCursor = true;

        public static void Ligar(Button alvo, bool cancelamento = false, bool cursor = true)
        {
            var som = alvo.GetComponent<SomBotao>();
            if (som == null) som = alvo.gameObject.AddComponent<SomBotao>();
            som.cancelar = cancelamento;
            som.somCursor = cursor;
        }

        void Awake()
        {
            botao = GetComponent<Button>();
            botao.onClick.AddListener(Acionar);
        }

        void Acionar()
        {
            if (botao.IsActive() && botao.IsInteractable())
                GetComponentInParent<SonsUI>()?.Acionar(cancelar);
        }

        public void OnPointerEnter(PointerEventData dados) => Cursor();

        public void OnSelect(BaseEventData dados)
        {
            if (!(dados is PointerEventData)) Cursor();
        }

        void Cursor()
        {
            if (somCursor && botao.IsActive() && botao.IsInteractable())
                GetComponentInParent<SonsUI>()?.Cursor();
        }
    }
}
