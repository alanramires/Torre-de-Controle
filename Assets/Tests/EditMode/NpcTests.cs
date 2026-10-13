using System.Linq;
using NUnit.Framework;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Salas;

namespace TorreDeControle.Tests
{
    public class NpcTests
    {
        ArmazenamentoEmMemoria armazenamento;
        CadastroPersonagens personagens;
        CadastroSalas salas;
        Sala sala;

        [SetUp]
        public void Preparar()
        {
            armazenamento = new ArmazenamentoEmMemoria();
            personagens = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            salas = CadastroSalas.Carregar(armazenamento, personagens);
            salas.Criar("Nova Era", 6);
            sala = salas.Listar().Single();
        }

        Personagem Npc(string nome, Papel papel = Papel.Neutro)
        {
            Assert.That(personagens.CriarNpc(sala.Id, nome, Maturidade.Adulto, papel).Ok, Is.True);
            return personagens.NpcsDaSala(sala.Id).Last();
        }

        [Test]
        public void Npc_nasce_na_sala_com_controle_do_mestre_e_fora_do_laboratorio()
        {
            var npc = Npc("Sra. Okafor");
            Assert.That(npc.Controle, Is.EqualTo(Controle.Mestre));
            Assert.That(npc.Sala, Is.EqualTo(sala.Id));
            Assert.That(npc.Pnj, Is.Not.Null);
            Assert.That(npc.Vilao, Is.Null);
            Assert.That(personagens.Listar(), Has.No.Member(npc));
            Assert.That(personagens.HeroisDaSala(sala.Id), Has.No.Member(npc));
            Assert.That(personagens.CriarNpc(sala.Id, "  ", Maturidade.Adulto, Papel.Neutro).Ok, Is.False);
        }

        [Test]
        public void Perfil_pnj_e_avisos()
        {
            var npc = Npc("Searley");
            Assert.That(personagens.AvisosDoNpc(npc), Has.Some.Contains("motivação"));
            personagens.DefinirPerfilPnj(npc.Id, "sensacionalista", "ganhar audiência");
            Assert.That(npc.Pnj.Motivacao, Is.EqualTo("ganhar audiência"));
            Assert.That(personagens.AvisosDoNpc(npc), Is.Empty);
        }

        [Test]
        public void Promover_a_vilao_da_ficha_e_voltar_guarda()
        {
            var npc = Npc("Rainha Pavorosa");
            Assert.That(personagens.DefinirGeracao(npc.Id, "prata").Ok, Is.False); // ainda não é vilã

            personagens.DefinirPapel(npc.Id, Papel.Vilao);
            Assert.That(npc.Vilao, Is.Not.Null);
            Assert.That(personagens.DefinirGeracao(npc.Id, "prata").Ok, Is.True);
            Assert.That(personagens.DefinirGeracao(npc.Id, "lata").Ok, Is.False);

            personagens.DefinirPapel(npc.Id, Papel.Neutro);
            personagens.DefinirPapel(npc.Id, Papel.Vilao);
            Assert.That(npc.Vilao.Geracao, Is.EqualTo("prata"));
        }

        [Test]
        public void Movimentos_de_vilao_acrescenta_edita_e_tira()
        {
            var npc = Npc("Rainha Pavorosa", Papel.Vilao);
            personagens.DefinirMovimentoDeVilao(npc.Id, 0, "sacar uma arma do juízo final");
            personagens.DefinirMovimentoDeVilao(npc.Id, 1, "revelar a contramedida perfeita");
            personagens.DefinirMovimentoDeVilao(npc.Id, 2, "   "); // vazio no fim: nada
            Assert.That(npc.Vilao.Movimentos, Has.Count.EqualTo(2));

            personagens.DefinirMovimentoDeVilao(npc.Id, 0, "distorcer a sorte com o anel");
            Assert.That(npc.Vilao.Movimentos[0], Is.EqualTo("distorcer a sorte com o anel"));
            personagens.DefinirMovimentoDeVilao(npc.Id, 0, "");
            Assert.That(npc.Vilao.Movimentos.Single(), Is.EqualTo("revelar a contramedida perfeita"));
            Assert.That(personagens.DefinirMovimentoDeVilao(npc.Id, 5, "x").Ok, Is.False);
        }

        [Test]
        public void Condicoes_de_vilao_na_ordem_da_ficha()
        {
            var npc = Npc("Rainha Pavorosa", Papel.Vilao);
            personagens.AlternarCondicaoDeVilao(npc.Id, Condicoes.Inseguro);
            personagens.AlternarCondicaoDeVilao(npc.Id, Condicoes.Irritado);
            personagens.AlternarCondicaoDeVilao(npc.Id, Condicoes.Culpado);
            Assert.That(npc.Vilao.Condicoes, Is.EqualTo(new[] { Condicoes.Irritado, Condicoes.Culpado, Condicoes.Inseguro }));
            personagens.AlternarCondicaoDeVilao(npc.Id, Condicoes.Culpado);
            Assert.That(npc.Vilao.Condicoes, Has.Count.EqualTo(2));
            Assert.That(personagens.AlternarCondicaoDeVilao(npc.Id, "faminto").Ok, Is.False);
        }

        [Test]
        public void Heroi_nao_e_npc()
        {
            var heroi = FichaPronta.Criada(personagens, "Estrela");
            Assert.That(personagens.DefinirPapel(heroi.Id, Papel.Vilao).Ok, Is.False);
        }

        [Test]
        public void Npc_sobrevive_a_recarga()
        {
            var npc = Npc("Rainha Pavorosa", Papel.Vilao);
            personagens.DefinirPerfilPnj(npc.Id, "régia", "proteger a nação");
            personagens.AlternarCondicaoDeVilao(npc.Id, Condicoes.Irritado);

            var p2 = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            var de2 = p2.NpcsDaSala(sala.Id).Single();
            Assert.That(de2.Pnj.Motivacao, Is.EqualTo("proteger a nação"));
            Assert.That(de2.Vilao.Condicoes.Single(), Is.EqualTo(Condicoes.Irritado));
            Assert.That(de2.Controle, Is.EqualTo(Controle.Mestre));
        }
    }
}
