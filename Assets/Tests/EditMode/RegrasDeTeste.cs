using System;
using System.IO;
using TorreDeControle.Core.Regras;

namespace TorreDeControle.Tests
{
    /// <summary>
    /// Lê os JSON de regras de verdade (Assets/Resources/Regras), para os testes usarem as mesmas
    /// regras que a Torre. Procura subindo a partir da pasta atual e da pasta do assembly.
    /// </summary>
    sealed class RegrasDeTeste : IFonteDeRegras
    {
        static readonly string Pasta = Achar();

        public string Ler(string nome)
        {
            var caminho = Path.Combine(Pasta, nome + ".json");
            return File.Exists(caminho) ? File.ReadAllText(caminho) : null;
        }

        public static RegrasArquetipos Arquetipos() => RegrasArquetipos.Carregar(new RegrasDeTeste());

        static string Achar()
        {
            foreach (var inicio in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                for (var dir = new DirectoryInfo(inicio); dir != null; dir = dir.Parent)
                {
                    var candidata = Path.Combine(dir.FullName, "Assets", "Resources", "Regras");
                    if (Directory.Exists(candidata))
                        return candidata;
                    var aoLado = Path.Combine(dir.FullName, "Regras");
                    if (File.Exists(Path.Combine(aoLado, "arquetipos.json")))
                        return aoLado;
                }
            }
            throw new DirectoryNotFoundException("Pasta Assets/Resources/Regras não encontrada.");
        }
    }
}
