using System.Linq;
using NUnit.Framework;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Salas;

namespace TorreDeControle.Tests
{
    public class MesaTests
    {
        ArmazenamentoEmMemoria armazenamento;
        CadastroPersonagens personagens;
        CadastroSalas salas;
        Sala sala;
        Mesa mesa;
        Personagem heroi;

        [SetUp]
        public void Preparar()
        {
            armazenamento = new ArmazenamentoEmMemoria();
            personagens = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            salas = CadastroSalas.Carregar(armazenamento, personagens);
            salas.Criar("Nova Era", 6);
            sala = salas.Listar().Single();
            var original = FichaPronta.Criada(personagens, "Estrela");
            salas.PorPersonagem(sala.Id, 0, original.Id, false);
            heroi = personagens.Buscar(sala.Slots[0].Personagem);
            personagens.AprovarParaCampanha(heroi.Id);
            mesa = Mesa.Carregar(armazenamento, personagens, sala.Id);
        }

        [Test]
        public void Sempre_ha_uma_rodada_corrente()
        {
            Assert.That(mesa.Rodada, Is.EqualTo(1));
            mesa.NovaRodada();
            Assert.That(mesa.Rodada, Is.EqualTo(2));
        }

        [Test]
        public void Dar_potencial_vira_evento_com_rodada_e_motivo()
        {
            Assert.That(mesa.DarPotencial(heroi.Id, 1, "falhou ao enfrentar").Ok, Is.True);
            Assert.That(heroi.Potencial, Is.EqualTo(1));
            var e = mesa.Eventos.Single();
            Assert.That(e.Rodada, Is.EqualTo(1));
            Assert.That(e.Motivo, Is.EqualTo("falhou ao enfrentar"));
            Assert.That(e.Mudancas.Single().Antes, Is.EqualTo(0));
            Assert.That(e.Mudancas.Single().Depois, Is.EqualTo(1));
            Assert.That(mesa.DarPotencial(heroi.Id, -5, null).Ok, Is.False);
        }

        [Test]
        public void Condicao_marca_e_desmarca()
        {
            mesa.AlternarCondicao(heroi.Id, Condicoes.Culpado, null);
            mesa.AlternarCondicao(heroi.Id, Condicoes.Irritado, null);
            Assert.That(heroi.CondicoesMarcadas, Is.EqualTo(new[] { Condicoes.Irritado, Condicoes.Culpado }));
            mesa.AlternarCondicao(heroi.Id, Condicoes.Culpado, null);
            Assert.That(heroi.CondicoesMarcadas.Single(), Is.EqualTo(Condicoes.Irritado));
        }

        [Test]
        public void Mudar_rotulos_sobe_um_e_desce_outro()
        {
            var antes = personagens.RotulosAtuais(heroi).ToDictionary(kv => kv.Key, kv => kv.Value);
            Assert.That(mesa.MudarRotulos(heroi.Id, Rotulos.Perigoso, Rotulos.Mundano, "provocou").Ok, Is.True);
            var depois = personagens.RotulosAtuais(heroi);
            Assert.That(depois[Rotulos.Perigoso], Is.EqualTo(antes[Rotulos.Perigoso] + 1));
            Assert.That(depois[Rotulos.Mundano], Is.EqualTo(antes[Rotulos.Mundano] - 1));
            // A foto da criação não muda.
            Assert.That(heroi.Foto.Rotulos[Rotulos.Perigoso], Is.EqualTo(antes[Rotulos.Perigoso]));
        }

        [Test]
        public void Estouro_nao_muda_rotulos_e_marca_a_condicao_escolhida()
        {
            // Leva Superior ao máximo.
            while (personagens.RotulosAtuais(heroi)[Rotulos.Superior] < Rotulos.Maximo)
            {
                var alvoDesce = Rotulos.Todos.First(r => r != Rotulos.Superior &&
                                                        personagens.RotulosAtuais(heroi)[r] > Rotulos.Minimo);
                mesa.MudarRotulos(heroi.Id, Rotulos.Superior, alvoDesce, null);
            }
            var antes = personagens.RotulosAtuais(heroi).ToDictionary(kv => kv.Key, kv => kv.Value);
            var semEscolha = mesa.MudarRotulos(heroi.Id, Rotulos.Superior, Rotulos.Mundano, null);
            Assert.That(semEscolha.Ok, Is.False);
            Assert.That(semEscolha.Erro, Is.EqualTo(Mesa.MensagemDeEstouro));

            Assert.That(mesa.MudarRotulos(heroi.Id, Rotulos.Superior, Rotulos.Mundano, null, Condicoes.Inseguro).Ok, Is.True);
            Assert.That(personagens.RotulosAtuais(heroi), Is.EqualTo(antes));
            Assert.That(heroi.CondicoesMarcadas, Has.Member(Condicoes.Inseguro));
        }

        [Test]
        public void Corrigir_lanca_o_inverso_e_nao_apaga()
        {
            mesa.DarPotencial(heroi.Id, 1, null);
            mesa.DarPotencial(heroi.Id, 1, null);
            var primeiro = mesa.Eventos.First();
            Assert.That(mesa.Corrigir(primeiro.Id, "foi engano").Ok, Is.True);
            Assert.That(heroi.Potencial, Is.EqualTo(1));
            Assert.That(mesa.Eventos, Has.Count.EqualTo(3));
            Assert.That(mesa.Corrigido(primeiro), Is.True);
            Assert.That(mesa.Corrigir(primeiro.Id, null).Ok, Is.False);
            Assert.That(mesa.Corrigir(mesa.Eventos.Last().Id, null).Ok, Is.False);
        }

        [Test]
        public void So_instancia_em_jogo_desta_sala()
        {
            var original = personagens.Buscar(heroi.Origem);
            Assert.That(mesa.DarPotencial(original.Id, 1, null).Ok, Is.False); // laboratório

            // Outra instância, ainda em aprovação (e depois em revisão): não joga.
            var outra = FichaPronta.Criada(personagens, "Cometa");
            salas.PorPersonagem(sala.Id, 1, outra.Id, false);
            var emAprovacao = personagens.Buscar(sala.Slots[1].Personagem);
            Assert.That(mesa.DarPotencial(emAprovacao.Id, 1, null).Ok, Is.False);
            personagens.ReabrirCriacao(emAprovacao.Id);
            Assert.That(emAprovacao.Status, Is.EqualTo(StatusCriacao.EmRevisao));
            Assert.That(mesa.DarPotencial(emAprovacao.Id, 1, null).Ok, Is.False);
        }

        [Test]
        public void Log_e_estado_sobrevivem_a_recarga()
        {
            mesa.DarPotencial(heroi.Id, 1, "fim de sessão");
            mesa.AlternarCondicao(heroi.Id, Condicoes.Assustado, null);
            mesa.NovaRodada();

            var p2 = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            var m2 = Mesa.Carregar(armazenamento, p2, sala.Id);
            Assert.That(m2.Rodada, Is.EqualTo(2));
            Assert.That(m2.Eventos, Has.Count.EqualTo(2));
            var h2 = p2.Buscar(heroi.Id);
            Assert.That(h2.Potencial, Is.EqualTo(1));
            Assert.That(h2.CondicoesMarcadas.Single(), Is.EqualTo(Condicoes.Assustado));
        }
    }
}
