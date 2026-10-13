using TorreDeControle.Core.Regras;
using UnityEngine;

namespace TorreDeControle.UI
{
    /// <summary>Lê Assets/Resources/Regras/&lt;nome&gt;.json (vai junto no build).</summary>
    public sealed class FonteDeRegrasResources : IFonteDeRegras
    {
        public string Ler(string nome) => Resources.Load<TextAsset>("Regras/" + nome)?.text;
    }
}
