using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TorreDeControle.Core.Armazenamento;
using TorreDeControle.Core.Personagens;

namespace TorreDeControle.Tests
{
    public class GravacaoAdiadaTests
    {
        /// <summary>Armazenamento que junta gravações, com o descarregar na mão do teste.</summary>
        sealed class ArmazenamentoQueJunta : IArmazenamento, IGravacaoAdiada
        {
            public readonly ArmazenamentoEmMemoria Disco = new ArmazenamentoEmMemoria();
            public readonly Dictionary<string, Func<string>> Pendentes = new Dictionary<string, Func<string>>();
            public int Geracoes;

            public string Ler(string chave) =>
                Pendentes.TryGetValue(chave, out var f) ? f() : Disco.Ler(chave);

            public void Gravar(string chave, string conteudo) => Agendar(chave, () => conteudo);

            public void Agendar(string chave, Func<string> conteudo) => Pendentes[chave] = conteudo;

            public void Descarregar()
            {
                foreach (var item in Pendentes.ToList())
                {
                    Geracoes++;
                    Disco.Gravar(item.Key, item.Value());
                }
                Pendentes.Clear();
            }
        }

        [Test]
        public void Cliques_seguidos_viram_uma_gravacao_com_o_estado_final()
        {
            var armazenamento = new ArmazenamentoQueJunta();
            var cadastro = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            cadastro.Criar("", "Estrela", "farol", null);
            var p = cadastro.Listar().Single();
            cadastro.AlternarHabilidade(p.Id, "espadas");
            cadastro.AlternarHabilidade(p.Id, "artes-marciais");
            cadastro.AlternarHabilidade(p.Id, "espadas");

            Assert.That(armazenamento.Disco.Dados, Is.Empty); // nada foi para o disco ainda
            armazenamento.Descarregar();
            Assert.That(armazenamento.Geracoes, Is.EqualTo(1)); // um texto gerado, não quatro

            var relido = CadastroPersonagens.Carregar(armazenamento.Disco, RegrasDeTeste.Arquetipos());
            Assert.That(relido.Buscar(p.Id).Criacao.Habilidades.Escolhidas, Is.EqualTo(new[] { "artes-marciais" }));
        }
    }
}
