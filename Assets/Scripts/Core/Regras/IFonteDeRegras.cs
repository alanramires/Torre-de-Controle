namespace TorreDeControle.Core.Regras
{
    /// <summary>
    /// De onde vêm os JSON de regras (Assets/Resources/Regras). O Core recebe o texto; quem sabe
    /// carregar (Resources na Unity, arquivo nos testes) é a implementação.
    /// </summary>
    public interface IFonteDeRegras
    {
        /// <summary>Conteúdo de <c>Regras/&lt;nome&gt;.json</c>, ou null se não existir.</summary>
        string Ler(string nome);
    }
}
