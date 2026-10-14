using System;
using UnityEngine;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Marca um bloco da ficha que sabe se refazer sozinho (ex.: as habilidades, os movimentos). Um clique
    /// dentro dele refaz só o bloco, não a aba inteira: a ficha fica ágil.
    /// </summary>
    public sealed class SecaoRefazivel : MonoBehaviour
    {
        public Action Refazer;
    }
}
