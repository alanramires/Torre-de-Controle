using System.Linq;
using TorreDeControle.Core.Regras;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// Seção Crescimento, protótipo marcável: as casas de avanço da ficha do arquétipo.
    /// Por enquanto só a marca (sem aplicar o efeito, sem gastar potencial, sem evento).
    /// </summary>
    public sealed partial class CadastroPersonagens
    {
        public const string Cima = "cima";
        public const string Baixo = "baixo";

        public RegrasArquetipos.ListaDeAvancos AvancosDoArquetipo(Personagem p) =>
            RegraDoArquetipo(p)?.Avancos ?? new RegrasArquetipos.ListaDeAvancos();

        public static string Casa(string lista, int indice) => $"{lista}:{indice}";

        public bool Marcado(Personagem p, string lista, int indice) => p.AvancosMarcados.Contains(Casa(lista, indice));

        public int MarcadosEmCima(Personagem p) => p.AvancosMarcados.Count(c => c.StartsWith(Cima + ":"));

        /// <summary>Os de baixo liberam com 5 marcados em cima.</summary>
        public bool BaixoLiberado(Personagem p) =>
            MarcadosEmCima(p) >= RegrasArquetipos.ListaDeAvancos.ParaLiberarBaixo;

        /// <summary>Marca ou desmarca uma casa de avanço.</summary>
        public Resultado AlternarAvanco(string id, string lista, int indice)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (p.Status == StatusCriacao.Novo)
                return Resultado.Falha("Avanços vêm depois de concluir a criação.");

            var avancos = AvancosDoArquetipo(p);
            var itens = lista == Cima ? avancos.Cima : lista == Baixo ? avancos.Baixo : null;
            if (itens == null || indice < 0 || indice >= itens.Count)
                return Resultado.Falha("Avanço desconhecido.");

            var casa = Casa(lista, indice);
            var marcando = !p.AvancosMarcados.Contains(casa);
            if (marcando && lista == Baixo && !BaixoLiberado(p))
                return Resultado.Falha("Os avanços de baixo liberam com 5 marcados em cima.");

            var baixoMarcado = p.AvancosMarcados.Any(c => c.StartsWith(Baixo + ":"));
            if (!marcando && lista == Cima && baixoMarcado &&
                MarcadosEmCima(p) - 1 < RegrasArquetipos.ListaDeAvancos.ParaLiberarBaixo)
                return Resultado.Falha("Há avanço de baixo marcado: desmarque-o antes de ficar com menos de 5 em cima.");

            if (marcando)
                p.AvancosMarcados.Add(casa);
            else
                p.AvancosMarcados.Remove(casa);
            Gravar();
            return Resultado.Sucesso();
        }
    }
}
