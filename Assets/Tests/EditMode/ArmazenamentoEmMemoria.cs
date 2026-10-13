using System.Collections.Generic;
using TorreDeControle.Core.Armazenamento;

namespace TorreDeControle.Tests
{
    sealed class ArmazenamentoEmMemoria : IArmazenamento
    {
        public readonly Dictionary<string, string> Dados = new Dictionary<string, string>();
        public string Ler(string chave) => Dados.TryGetValue(chave, out var v) ? v : null;
        public void Gravar(string chave, string conteudo) => Dados[chave] = conteudo;
    }
}
