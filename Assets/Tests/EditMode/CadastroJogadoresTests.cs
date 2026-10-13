using System.Collections.Generic;
using NUnit.Framework;
using TorreDeControle.Core.Armazenamento;
using TorreDeControle.Core.Jogadores;

namespace TorreDeControle.Tests
{
    public class CadastroJogadoresTests
    {
        sealed class ArmazenamentoEmMemoria : IArmazenamento
        {
            public readonly Dictionary<string, string> Dados = new Dictionary<string, string>();
            public string Ler(string chave) => Dados.TryGetValue(chave, out var v) ? v : null;
            public void Gravar(string chave, string conteudo) => Dados[chave] = conteudo;
        }

        ArmazenamentoEmMemoria armazenamento;
        CadastroJogadores cadastro;

        [SetUp]
        public void Preparar()
        {
            armazenamento = new ArmazenamentoEmMemoria();
            cadastro = CadastroJogadores.Carregar(armazenamento);
        }

        [Test]
        public void Criar_novo_jogador_nasce_ativo_e_sem_espacos()
        {
            Assert.That(cadastro.Criar("  Mayane ").Ok, Is.True);
            var j = cadastro.Listar()[0];
            Assert.That(j.Nome, Is.EqualTo("Mayane"));
            Assert.That(j.Ativo, Is.True);
        }

        [Test]
        public void Nome_vazio_falha()
        {
            Assert.That(cadastro.Criar("   ").Ok, Is.False);
            Assert.That(cadastro.Listar(), Is.Empty);
        }

        [Test]
        public void Nome_repetido_ignora_maiusculas()
        {
            cadastro.Criar("Sarah");
            Assert.That(cadastro.Criar("sarah").Ok, Is.False);
        }

        [Test]
        public void Renomear_para_nome_de_outro_falha_e_para_o_proprio_passa()
        {
            cadastro.Criar("Amanda");
            cadastro.Criar("Sarah");
            var amanda = cadastro.Listar()[0];
            Assert.That(cadastro.Renomear(amanda.Id, "SARAH").Ok, Is.False);
            Assert.That(cadastro.Renomear(amanda.Id, "AMANDA").Ok, Is.True);
            Assert.That(cadastro.Buscar(amanda.Id).Nome, Is.EqualTo("AMANDA"));
        }

        [Test]
        public void Inativos_vao_para_o_fim_da_lista()
        {
            cadastro.Criar("Ana");
            cadastro.Criar("Bia");
            var ana = cadastro.Listar()[0];
            cadastro.DefinirAtivo(ana.Id, false);
            var lista = cadastro.Listar();
            Assert.That(lista[0].Nome, Is.EqualTo("Bia"));
            Assert.That(lista[1].Ativo, Is.False);
        }

        [Test]
        public void Toda_mudanca_grava_e_sobrevive_a_recarga()
        {
            cadastro.Criar("Alan");
            var id = cadastro.Listar()[0].Id;
            cadastro.DefinirAtivo(id, false);

            var recarregado = CadastroJogadores.Carregar(armazenamento);
            var alan = recarregado.Buscar(id);
            Assert.That(alan.Nome, Is.EqualTo("Alan"));
            Assert.That(alan.Ativo, Is.False);
            StringAssert.Contains("\"versao\": 1", armazenamento.Dados[CadastroJogadores.Chave]);
        }
    }
}
