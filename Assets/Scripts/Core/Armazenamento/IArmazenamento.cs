namespace TorreDeControle.Core.Armazenamento
{
    /// <summary>
    /// Onde os dados da Torre moram. O Core só conhece esta interface: grava e lê texto por chave.
    /// Fase 1: <c>ArmazenamentoLocalJson</c> (arquivos). Ninguém fora da implementação sabe onde fica.
    /// </summary>
    public interface IArmazenamento
    {
        /// <summary>Conteúdo gravado na chave, ou null se nunca foi gravado.</summary>
        string Ler(string chave);

        void Gravar(string chave, string conteudo);
    }
}
