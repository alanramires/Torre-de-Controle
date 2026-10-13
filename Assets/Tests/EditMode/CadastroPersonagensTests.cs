using System.Linq;
using NUnit.Framework;
using TorreDeControle.Core.Personagens;

namespace TorreDeControle.Tests
{
    public class CadastroPersonagensTests
    {
        ArmazenamentoEmMemoria armazenamento;
        CadastroPersonagens cadastro;

        [SetUp]
        public void Preparar()
        {
            armazenamento = new ArmazenamentoEmMemoria();
            cadastro = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
        }

        [Test]
        public void Nasce_heroi_jovem_de_jogador_com_status_novo_e_sem_jogador()
        {
            Assert.That(cadastro.Criar("Max Silva", "Farolete", "farol", null).Ok, Is.True);
            var p = cadastro.Listar()[0];
            Assert.That(p.Controle, Is.EqualTo(Controle.Jogador));
            Assert.That(p.Maturidade, Is.EqualTo(Maturidade.Jovem));
            Assert.That(p.Papel, Is.EqualTo(Papel.Heroi));
            Assert.That(p.Status, Is.EqualTo(StatusCriacao.Novo));
            Assert.That(p.Identidade.Jogador, Is.Null);
            Assert.That(p.NomeExibicao, Is.EqualTo("Farolete"));
        }

        [Test]
        public void Basta_nome_ou_codinome_mas_um_dos_dois()
        {
            Assert.That(cadastro.Criar("", "Jinx", "bruta", null).Ok, Is.True);
            Assert.That(cadastro.Criar("Kima", "  ", "bruta", null).Ok, Is.True);
            Assert.That(cadastro.Criar(" ", "", "bruta", null).Ok, Is.False);
        }

        [Test]
        public void Arquetipo_precisa_existir()
        {
            Assert.That(cadastro.Criar("X", "", null, null).Ok, Is.False);
            Assert.That(cadastro.Criar("X", "", "paladino", null).Ok, Is.False);
        }

        [Test]
        public void Nome_repetido_e_permitido()
        {
            cadastro.Criar("Max", "", "farol", null);
            Assert.That(cadastro.Criar("Max", "", "nova", null).Ok, Is.True);
        }

        [Test]
        public void Depois_de_criada_nao_troca_de_arquetipo_por_edicao()
        {
            cadastro.Criar("Max", "", "farol", null);
            var p = cadastro.Listar()[0];
            Assert.That(cadastro.AlterarIdentidade(p.Id, "Max", "", "nova", null).Ok, Is.True);

            p.Status = StatusCriacao.Criado;
            Assert.That(cadastro.AlterarIdentidade(p.Id, "Max", "", "farol", null).Ok, Is.False);
            Assert.That(cadastro.AlterarIdentidade(p.Id, "Max Silva", "", "nova", "j1").Ok, Is.True);
        }

        [Test]
        public void Grava_enums_legiveis_e_sobrevive_a_recarga()
        {
            cadastro.Criar("Raposa", "Raposa Prateada", "janus", "j1");
            var id = cadastro.Listar()[0].Id;
            cadastro.DefinirAtivo(id, false);

            var json = armazenamento.Dados[CadastroPersonagens.Chave];
            StringAssert.Contains("\"status\": \"novo\"", json);
            StringAssert.Contains("\"controle\": \"jogador\"", json);

            var p = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos()).Buscar(id);
            Assert.That(p.Identidade.Codinome, Is.EqualTo("Raposa Prateada"));
            Assert.That(p.Identidade.Jogador, Is.EqualTo("j1"));
            Assert.That(p.Ativo, Is.False);
        }

        [Test]
        public void Ordem_manual_dentro_de_cada_bloco()
        {
            cadastro.Criar("A", "", "farol", null);
            cadastro.Criar("B", "", "farol", null);
            cadastro.Criar("C", "", "farol", null);
            var c = cadastro.Listar()[2];
            cadastro.Mover(c.Id, 0);
            Assert.That(cadastro.Listar().Select(p => p.NomeExibicao), Is.EqualTo(new[] { "C", "A", "B" }));

            // Inativar manda para o fim; o bloco dos inativos fica embaixo.
            cadastro.DefinirAtivo(c.Id, false);
            Assert.That(cadastro.Listar().Select(p => p.NomeExibicao), Is.EqualTo(new[] { "A", "B", "C" }));

            var recarregado = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            Assert.That(recarregado.Listar().Select(p => p.NomeExibicao), Is.EqualTo(new[] { "A", "B", "C" }));
        }
    }
}
