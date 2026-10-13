using System.Collections.Generic;
using UnityEngine;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Retratos dos arquétipos (a arte quadrada de cada classe), achados pelo nome que o
    /// arquetipos.json dá em "retrato". As texturas vêm da TelaEntrada (lista ligada no Inspector):
    /// assim os arquivos ficam onde estão, em img/classes de personagens.
    /// </summary>
    public static class Retratos
    {
        static readonly Dictionary<string, Texture2D> porNome = new Dictionary<string, Texture2D>();

        public static void Registrar(IEnumerable<Texture2D> texturas)
        {
            porNome.Clear();
            if (texturas == null)
                return;
            foreach (var t in texturas)
                if (t != null)
                    porNome[t.name] = t;
        }

        /// <summary>A textura com esse nome, ou null (sem nome, ou não ligada no Inspector).</summary>
        public static Texture2D Buscar(string nome) =>
            !string.IsNullOrEmpty(nome) && porNome.TryGetValue(nome, out var t) ? t : null;
    }
}
