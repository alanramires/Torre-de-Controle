namespace TorreDeControle.Core.Armazenamento
{
    /// <summary>
    /// Armazenamento que sabe tirar uma cópia de segurança de todos os dados (fase 1: a pasta local). A mesa
    /// pede uma ao abrir uma campanha, antes de qualquer comando de jogo.
    /// </summary>
    public interface ICopiaDeSeguranca
    {
        /// <summary>Copia os dados atuais com um rótulo (ex.: "mesa Halcyon City"); guarda as últimas, apaga as mais antigas.</summary>
        void CopiaDeSeguranca(string rotulo);
    }
}
