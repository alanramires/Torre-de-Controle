using System.Collections.Generic;

namespace TorreDeControle.Core.Armazenamento
{
    /// <summary>
    /// Armazenamento que grava várias chaves como uma operação só: ou todas ficam com a versão nova, ou a
    /// Torre termina a troca ao abrir. Ficha e mesa nunca ficam de "fotos" diferentes.
    /// </summary>
    public interface IGravacaoEmLote
    {
        void GravarLote(IReadOnlyList<KeyValuePair<string, string>> itens);
    }
}
