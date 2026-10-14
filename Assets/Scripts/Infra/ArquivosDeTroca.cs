using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TorreDeControle.Infra
{
    /// <summary>
    /// Pastas de troca com outras máquinas: <c>dados/exportacoes</c> (o que a Torre exporta) e
    /// <c>dados/importar</c> (onde se põe o que vai ser importado). Só arquivos: o conteúdo
    /// (o JSON da personagem) é do Core.
    /// </summary>
    public sealed class ArquivosDeTroca
    {
        public string PastaExportacoes { get; }
        public string PastaImportar { get; }

        public ArquivosDeTroca(string pastaDados)
        {
            PastaExportacoes = Path.Combine(pastaDados, "exportacoes");
            PastaImportar = Path.Combine(pastaDados, "importar");
        }

        /// <summary>Grava em exportacoes/&lt;nome&gt;_&lt;data&gt;.json e devolve o caminho.</summary>
        public string Exportar(string nome, string conteudo)
        {
            Directory.CreateDirectory(PastaExportacoes);
            var caminho = Path.Combine(PastaExportacoes, $"{NomeDeArquivo(nome)}_{DateTime.Now:yyyy-MM-dd_HHmmss}.json");
            File.WriteAllText(caminho, conteudo, new UTF8Encoding(false));
            return caminho;
        }

        /// <summary>Os .json da pasta importar, do mais novo ao mais velho (cria a pasta se faltar).</summary>
        public IReadOnlyList<string> Importaveis()
        {
            Directory.CreateDirectory(PastaImportar);
            return Directory.GetFiles(PastaImportar, "*.json")
                .OrderByDescending(File.GetLastWriteTime)
                .ToList();
        }

        public string Ler(string caminho) => File.ReadAllText(caminho, Encoding.UTF8);

        static string NomeDeArquivo(string nome)
        {
            var invalidos = Path.GetInvalidFileNameChars();
            var limpo = new string((nome ?? "personagem").Select(c => invalidos.Contains(c) ? '_' : c).ToArray()).Trim();
            return limpo.Length == 0 ? "personagem" : limpo;
        }
    }
}
