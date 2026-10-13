using System.Linq;
using NUnit.Framework;
using TorreDeControle.Core.Personagens;

namespace TorreDeControle.Tests
{
    public class FichaCriacaoTests
    {
        ArmazenamentoEmMemoria armazenamento;
        CadastroPersonagens cadastro;

        [SetUp]
        public void Preparar()
        {
            armazenamento = new ArmazenamentoEmMemoria();
            cadastro = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
        }

        Personagem Nova(string arquetipo)
        {
            cadastro.Criar("Teste", "", arquetipo, null);
            return cadastro.Listar().Last();
        }

        [Test]
        public void Todos_os_arquetipos_tem_regras_e_somam_mais_dois()
        {
            var regras = RegrasDeTeste.Arquetipos();
            foreach (var a in Arquetipos.Todos)
            {
                var regra = regras.Buscar(a.Id);
                Assert.That(regra, Is.Not.Null, a.Id);
                Assert.That(regra.Rotulos.Values.Sum(), Is.EqualTo(2), a.Id);
            }
        }

        [Test]
        public void Farol_comeca_como_a_ficha_do_Max()
        {
            var p = Nova("farol");
            var r = cadastro.RotulosAtuais(p);
            Assert.That(r[Rotulos.Perigoso], Is.EqualTo(-1));
            Assert.That(r[Rotulos.Aberracao], Is.EqualTo(-1));
            Assert.That(r[Rotulos.Salvador], Is.EqualTo(2));
            Assert.That(r[Rotulos.Superior], Is.EqualTo(0));
            Assert.That(r[Rotulos.Mundano], Is.EqualTo(2));
        }

        [Test]
        public void Bonus_livre_soma_um_e_pode_trocar_de_lugar()
        {
            var p = Nova("legado");
            Assert.That(cadastro.EscolherBonusLivre(p.Id, Rotulos.Superior).Ok, Is.True);
            Assert.That(cadastro.RotulosAtuais(p)[Rotulos.Superior], Is.EqualTo(1));

            cadastro.EscolherBonusLivre(p.Id, Rotulos.Mundano);
            Assert.That(cadastro.RotulosAtuais(p)[Rotulos.Superior], Is.EqualTo(0));
            Assert.That(cadastro.RotulosAtuais(p)[Rotulos.Mundano], Is.EqualTo(2));
        }

        [Test]
        public void Bonus_livre_nao_passa_de_mais_tres()
        {
            var janus = Nova("janus");
            Assert.That(cadastro.EscolherBonusLivre(janus.Id, Rotulos.Mundano).Ok, Is.False);
            var transformado = Nova("transformado");
            Assert.That(cadastro.EscolherBonusLivre(transformado.Id, Rotulos.Aberracao).Ok, Is.False);
        }

        [Test]
        public void Trocar_de_arquetipo_em_novo_reseta_a_criacao()
        {
            var p = Nova("farol");
            cadastro.EscolherBonusLivre(p.Id, Rotulos.Superior);
            cadastro.AlterarIdentidade(p.Id, "Teste", "", "nova", null);
            Assert.That(p.Criacao.BonusLivre, Is.Null);
            Assert.That(cadastro.RotulosAtuais(p)[Rotulos.Aberracao], Is.EqualTo(2));
        }

        [Test]
        public void Aprovar_sem_bonus_pede_aceite_do_aviso()
        {
            var p = Nova("bruta");
            Assert.That(cadastro.Aprovar(p.Id, aceitarAvisos: false).Ok, Is.False);
            Assert.That(p.Status, Is.EqualTo(StatusCriacao.Novo));

            Assert.That(cadastro.Aprovar(p.Id, aceitarAvisos: true).Ok, Is.True);
            Assert.That(p.Foto.AvisosAceitos, Has.Count.EqualTo(1));
        }

        [Test]
        public void Aprovar_congela_a_foto_e_trava_o_bonus_e_o_arquetipo()
        {
            var p = Nova("nova");
            cadastro.EscolherBonusLivre(p.Id, Rotulos.Superior);
            Assert.That(cadastro.Aprovar(p.Id, aceitarAvisos: false).Ok, Is.True);

            Assert.That(p.Status, Is.EqualTo(StatusCriacao.Criado));
            Assert.That(p.Foto.Rotulos[Rotulos.Superior], Is.EqualTo(1));
            Assert.That(cadastro.EscolherBonusLivre(p.Id, Rotulos.Perigoso).Ok, Is.False);
            Assert.That(cadastro.AlterarIdentidade(p.Id, "Teste", "", "farol", null).Ok, Is.False);

            var recarregada = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos()).Buscar(p.Id);
            Assert.That(recarregada.Foto.Rotulos[Rotulos.Superior], Is.EqualTo(1));
            Assert.That(recarregada.Status, Is.EqualTo(StatusCriacao.Criado));
        }
    }
}
