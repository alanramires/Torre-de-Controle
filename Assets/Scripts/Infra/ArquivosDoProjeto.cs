using System.IO;
using System.Text;

namespace TorreDeControle.Infra
{
    /// <summary>
    /// Ler e gravar arquivos do próprio projeto (Assets/...), para as ferramentas de edição do mestre (ex.: a
    /// gestão das cartas grava Regras/cartas.json e Ajuda/catalogo.json). Só faz sentido rodando no Editor da
    /// Unity: num build não há pasta Assets, e quem usa deve checar <see cref="Disponivel"/>.
    /// </summary>
    public sealed class ArquivosDoProjeto
    {
        readonly string pastaAssets;

        /// <param name="pastaAssets">Application.dataPath no Editor; null num build.</param>
        public ArquivosDoProjeto(string pastaAssets) => this.pastaAssets = pastaAssets;

        public bool Disponivel => pastaAssets != null && Directory.Exists(pastaAssets);

        /// <summary>Caminho relativo à pasta Assets (ex.: "Resources/Regras/cartas.json").</summary>
        public string Ler(string relativo) => File.ReadAllText(Path.Combine(pastaAssets, relativo), Encoding.UTF8);

        /// <summary>Grava mantendo o fim de linha que o arquivo já usa (CRLF ou LF), para o git não ver tudo mudado.</summary>
        public void Gravar(string relativo, string conteudo)
        {
            var caminho = Path.Combine(pastaAssets, relativo);
            var crlf = File.Exists(caminho) && File.ReadAllText(caminho).Contains("\r\n");
            var texto = conteudo.Replace("\r\n", "\n");
            if (crlf)
                texto = texto.Replace("\n", "\r\n");
            File.WriteAllText(caminho, texto, new UTF8Encoding(false));
        }
    }
}
