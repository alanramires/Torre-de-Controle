using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>Marca uma caixinha (campo de texto, lista suspensa) como parada do Tab.</summary>
    public sealed class Tabulavel : MonoBehaviour
    {
    }

    /// <summary>
    /// Tab vai para a próxima caixinha <see cref="Tabulavel"/> do painel, Shift+Tab volta.
    /// A ordem é a da hierarquia: da esquerda para a direita, de cima para baixo.
    /// Vai na raiz de cada painel. Com painéis empilhados (a ficha aberta sobre a lista), só o
    /// último ligado responde: senão o de baixo também anda e rouba o foco para as caixinhas dele.
    /// </summary>
    public sealed class NavegacaoTab : MonoBehaviour
    {
        static readonly List<NavegacaoTab> ligadas = new List<NavegacaoTab>();
        readonly List<Tabulavel> paradas = new List<Tabulavel>();

        /// <summary>Play sem recarregar o domínio: a pilha estática começa vazia a cada Play.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimparAoEntrarNoPlay() => ligadas.Clear();

        void OnEnable() => ligadas.Add(this);
        void OnDisable() => ligadas.Remove(this);

        void Update()
        {
            ligadas.RemoveAll(n => n == null);
            if (ligadas.Count == 0 || ligadas[ligadas.Count - 1] != this)
                return;

            var teclado = Keyboard.current;
            if (teclado == null || !teclado.tabKey.wasPressedThisFrame)
                return;

            var sistema = EventSystem.current;
            if (sistema == null)
                return;

            GetComponentsInChildren(false, paradas);
            paradas.RemoveAll(p => !Disponivel(p));
            if (paradas.Count == 0)
                return;

            var atual = Indice(sistema.currentSelectedGameObject);
            var passo = teclado.shiftKey.isPressed ? -1 : 1;
            var proximo = atual < 0
                ? (passo > 0 ? 0 : paradas.Count - 1)
                : (atual + passo + paradas.Count) % paradas.Count;

            var alvo = paradas[proximo].gameObject;
            sistema.SetSelectedGameObject(alvo);
            if (alvo.TryGetComponent<TMP_InputField>(out var campo))
                campo.ActivateInputField();
        }

        int Indice(GameObject selecionado)
        {
            if (selecionado == null)
                return -1;
            for (var i = 0; i < paradas.Count; i++)
                if (paradas[i].gameObject == selecionado)
                    return i;
            return -1;
        }

        static bool Disponivel(Tabulavel parada) =>
            parada.isActiveAndEnabled &&
            parada.TryGetComponent<Selectable>(out var s) && s.IsInteractable();
    }
}
