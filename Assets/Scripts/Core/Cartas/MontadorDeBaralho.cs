using System.Collections.Generic;
using System.Linq;
using TorreDeControle.Core.Personagens;

namespace TorreDeControle.Core.Cartas
{
    /// <summary>O baralho de uma personagem, por seção (a mão que ela leva para a mesa).</summary>
    public sealed class Baralho
    {
        public List<Carta> Basicos { get; } = new List<Carta>();
        public List<Carta> Pvp { get; } = new List<Carta>();
        public List<Carta> Arquetipo { get; } = new List<Carta>();
        public List<Carta> Equipe { get; } = new List<Carta>();
        public List<Carta> Extras { get; } = new List<Carta>();
        /// <summary>O que a ficha tem e o catálogo ainda não (para popular): ids procurados.</summary>
        public List<string> Faltando { get; } = new List<string>();

        public IEnumerable<Carta> Todas => Basicos.Concat(Pvp).Concat(Arquetipo).Concat(Equipe).Concat(Extras);
    }

    /// <summary>
    /// Monta o baralho a partir da ficha ("ficha = ids de cartas + tokens", CLAUDE.md): básicos e PvP para
    /// todos; os movimentos do arquétipo que estão na ficha; o movimento de equipe do arquétipo; os extras.
    /// Não guarda nada: o baralho é sempre lido da ficha, que é a fonte.
    /// </summary>
    public static class MontadorDeBaralho
    {
        public static Baralho Montar(Personagem p, CadastroPersonagens cadastro, CatalogoDeCartas catalogo)
        {
            var baralho = new Baralho();
            baralho.Basicos.AddRange(catalogo.DoTipo(Carta.Basico));
            baralho.Pvp.AddRange(catalogo.DoTipo(Carta.Pvp));

            var arquetipo = cadastro.RegraDoArquetipo(p)?.Id;
            if (arquetipo == null)
                return baralho;

            foreach (var mov in cadastro.MovimentosNaFicha(p))
            {
                // Do próprio arquétipo; se veio de outro (avanço), acha pelo id do movimento em qualquer arquétipo.
                var carta = catalogo.Buscar($"{arquetipo}/{mov}") ??
                            catalogo.DoTipo(Carta.Arquetipo).FirstOrDefault(c => c.Id.EndsWith("/" + mov));
                if (carta != null)
                    baralho.Arquetipo.Add(carta);
                else
                    baralho.Faltando.Add($"{arquetipo}/{mov}");
            }

            var equipe = catalogo.Buscar($"equipe/{arquetipo}");
            if (equipe != null)
                baralho.Equipe.Add(equipe);
            else
                baralho.Faltando.Add($"equipe/{arquetipo}");

            foreach (var extra in cadastro.ExtrasDoArquetipo(p))
            {
                var carta = catalogo.Buscar(extra.Id);
                if (carta != null)
                    baralho.Extras.Add(carta);
                else
                    baralho.Faltando.Add(extra.Id);
            }
            return baralho;
        }
    }
}
