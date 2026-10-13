using System.IO;
using System.Text;
using TorreDeControle.Core.Armazenamento;

namespace TorreDeControle.Infra
{
    /// <summary>
    /// Fase 1: cada chave vira <c>&lt;pasta&gt;/&lt;chave&gt;.json</c>.
    /// Único lugar do projeto que usa System.IO (WebGL não grava arquivo local).
    /// </summary>
    public sealed class ArmazenamentoLocalJson : IArmazenamento
    {
        readonly string pasta;

        public ArmazenamentoLocalJson(string pasta)
        {
            this.pasta = pasta;
        }

        public string Pasta => pasta;

        public string Ler(string chave)
        {
            var caminho = Caminho(chave);
            return File.Exists(caminho) ? File.ReadAllText(caminho, Encoding.UTF8) : null;
        }

        public void Gravar(string chave, string conteudo)
        {
            Directory.CreateDirectory(pasta);
            var caminho = Caminho(chave);
            var temporario = caminho + ".tmp";

            // Grava ao lado e troca: se a Torre cair no meio, o arquivo antigo continua inteiro.
            File.WriteAllText(temporario, conteudo, new UTF8Encoding(false));
            if (File.Exists(caminho))
                File.Replace(temporario, caminho, null);
            else
                File.Move(temporario, caminho);
        }

        string Caminho(string chave) => Path.Combine(pasta, chave + ".json");
    }
}
