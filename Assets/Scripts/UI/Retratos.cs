using System.Collections.Generic;
using UnityEngine;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Retratos dos arquétipos (a arte quadrada de cada classe), achados pelo nome que o
    /// arquetipos.json dá em "retrato". Carrega de Resources/Retratos quando não há
    /// textura registrada pela TelaEntrada, sem depender do Inspector.
    /// </summary>
    public static class Retratos
    {
        static readonly Dictionary<string, Texture2D> porNome = new Dictionary<string, Texture2D>();

        /// <summary>Play sem recarregar o domínio: o cache estático começa limpo a cada Play.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimparAoEntrarNoPlay() => porNome.Clear();

        public static void Registrar(IEnumerable<Texture2D> texturas)
        {
            porNome.Clear();
            if (texturas == null)
                return;
            foreach (var t in texturas)
                if (t != null)
                    porNome[t.name] = t;
        }

        /// <summary>A textura registrada ou carregada de Resources pelo nome.</summary>
        public static Texture2D Buscar(string nome)
        {
            if (string.IsNullOrEmpty(nome)) return null;
            if (porNome.TryGetValue(nome, out var pronta) && pronta != null) return pronta;
            var textura = Resources.Load<Texture2D>("Retratos/" + nome);
            if (textura != null) porNome[nome] = textura;
            return textura;
        }
    }
}
