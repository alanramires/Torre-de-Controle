using System;

namespace TorreDeControle.Core.Armazenamento
{
    /// <summary>
    /// Armazenamento que junta gravações: em vez de gravar a cada clique, guarda o "como gerar o texto"
    /// e grava uma vez depois de uma pausa. Quem tem dado grande (o cadastro de personagens) agenda por
    /// aqui quando o armazenamento oferece; senão grava na hora.
    /// </summary>
    public interface IGravacaoAdiada
    {
        /// <summary>Agenda a gravação da chave; o texto só é gerado na hora de gravar (o último vence).</summary>
        void Agendar(string chave, Func<string> conteudo);
    }
}
