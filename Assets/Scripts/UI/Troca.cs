using System.Collections.Generic;
using System.Linq;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Infra;

namespace TorreDeControle.UI
{
    /// <summary>As pastas de exportar/importar, configuradas pela TelaEntrada ao abrir.</summary>
    public static class Troca
    {
        public static ArquivosDeTroca Arquivos { get; set; }

        /// <summary>
        /// Os .json da pasta de importar que são do <paramref name="formato"/> pedido (personagem ou NPCs):
        /// cada tela mostra só o que sabe importar. Arquivo ilegível ou de outro tipo fica de fora.
        /// </summary>
        public static IReadOnlyList<string> Importaveis(string formato)
        {
            if (Arquivos == null)
                return new string[0];
            return Arquivos.Importaveis().Where(caminho =>
            {
                try
                {
                    return CadastroPersonagens.FormatoDaTroca(Arquivos.Ler(caminho)) == formato;
                }
                catch (System.IO.IOException)
                {
                    return false;
                }
            }).ToList();
        }
    }
}
