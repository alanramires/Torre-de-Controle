using System.Linq;
using NUnit.Framework;
using TorreDeControle.Core.Cartas;
using TorreDeControle.Core.Personagens;

namespace TorreDeControle.Tests
{
    /// <summary>Catálogo de cartas (Regras/cartas.json) e o montador do baralho a partir da ficha.</summary>
    public class CartasTests
    {
        CatalogoDeCartas catalogo;
        CadastroPersonagens personagens;

        [SetUp]
        public void Preparar()
        {
            catalogo = CatalogoDeCartas.Carregar(new RegrasDeTeste());
            personagens = CadastroPersonagens.Carregar(new ArmazenamentoEmMemoria(), RegrasDeTeste.Arquetipos());
        }

        [Test]
        public void Catalogo_sem_ids_repetidos_e_com_os_ids_que_as_regras_ja_citam()
        {
            Assert.That(catalogo.Repetidos(), Is.Empty);
            // As regras apontam para estas cartas pelo campo "catalogo".
            foreach (var id in new[] { "basico/usar-influencia", "basico/rejeitar-influencia", "basico/pilha-equipe",
                                       "basico/egoista", "pvp/pvp-01-enfrentar", "pvp/pvp-05-perfurar" })
                Assert.That(catalogo.Buscar(id), Is.Not.Null, id);
        }

        [Test]
        public void Todo_movimento_e_extra_de_arquetipo_tem_carta()
        {
            var regras = RegrasDeTeste.Arquetipos();
            foreach (var a in regras.Todos)
            {
                foreach (var o in a.Movimentos.Opcoes)
                    Assert.That(catalogo.Buscar($"{a.Id}/{o.Id}"), Is.Not.Null, $"{a.Id}/{o.Id}");
                foreach (var e in a.Extras)
                    Assert.That(catalogo.Buscar(e.Id), Is.Not.Null, e.Id);
                Assert.That(catalogo.Buscar($"equipe/{a.Id}"), Is.Not.Null, $"equipe/{a.Id}");
            }
        }

        [Test]
        public void Montador_leva_basicos_pvp_os_movimentos_da_ficha_a_equipe_e_os_extras()
        {
            var nova = FichaPronta.Criada(personagens, "Pulsar"); // a ficha pronta é uma Nova
            var baralho = MontadorDeBaralho.Montar(nova, personagens, catalogo);

            Assert.That(baralho.Basicos.Count, Is.EqualTo(catalogo.DoTipo(Carta.Basico).Count));
            Assert.That(baralho.Pvp.Count, Is.EqualTo(catalogo.DoTipo(Carta.Pvp).Count));
            Assert.That(baralho.Equipe.Single().Id, Is.EqualTo("equipe/nova"));
            Assert.That(baralho.Extras.Select(c => c.Origem), Is.All.EqualTo("nova"));
            Assert.That(baralho.Faltando, Is.Empty);
            Assert.That(baralho.Arquetipo.Count, Is.EqualTo(personagens.MovimentosNaFicha(nova).Count));
        }

        [Test]
        public void Montador_da_bruta_traz_os_movimentos_escolhidos()
        {
            var p = FichaPronta.Criada(personagens, "Titânia");
            Assert.That(personagens.ReabrirCriacao(p.Id).Ok, Is.True);
            p.Identidade.Arquetipo = "bruta"; // atalho de teste
            personagens.AlternarMovimento(p.Id, "casca-grossa-pele-fina");
            var baralho = MontadorDeBaralho.Montar(p, personagens, catalogo);
            Assert.That(baralho.Arquetipo.Select(c => c.Id), Has.Member("bruta/casca-grossa-pele-fina"));
            Assert.That(baralho.Equipe.Single().Id, Is.EqualTo("equipe/bruta"));
        }
    }
}
