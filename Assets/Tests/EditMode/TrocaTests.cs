using System.Linq;
using NUnit.Framework;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Salas;

namespace TorreDeControle.Tests
{
    public class TrocaTests
    {
        [Test]
        public void Exportar_e_importar_em_outra_Torre_traz_a_ficha_como_nova()
        {
            // Torre A: uma personagem criada, com jogador e jogo marcado.
            var a = CadastroPersonagens.Carregar(new ArmazenamentoEmMemoria(), RegrasDeTeste.Arquetipos());
            var original = FichaPronta.Criada(a, "Estrela", jogador: "j1");
            original.Potencial = 6;
            var json = a.Exportar(original.Id);
            StringAssert.Contains(CadastroPersonagens.FormatoDeTroca, json);

            // Torre B.
            var b = CadastroPersonagens.Carregar(new ArmazenamentoEmMemoria(), RegrasDeTeste.Arquetipos());
            Assert.That(b.Importar(json, out var nova).Ok, Is.True);
            Assert.That(nova.Id, Is.Not.EqualTo(original.Id));
            Assert.That(nova.NomeExibicao, Is.EqualTo("Estrela"));
            Assert.That(nova.Status, Is.EqualTo(StatusCriacao.EmAprovacao));
            Assert.That(nova.Identidade.Jogador, Is.Null); // jogador é registro de cada Torre
            Assert.That(nova.Potencial, Is.EqualTo(6));
            Assert.That(b.MovimentosNaFicha(nova), Is.EquivalentTo(a.MovimentosNaFicha(original)));
            Assert.That(b.Listar(), Has.Member(nova));
        }

        [Test]
        public void Instancia_de_sala_volta_ao_laboratorio_sem_a_campanha()
        {
            var armazenamento = new ArmazenamentoEmMemoria();
            var personagens = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            var salas = CadastroSalas.Carregar(armazenamento, personagens);
            salas.Criar("Nova Era", 6);
            var sala = salas.Listar().Single();
            var original = FichaPronta.Criada(personagens, "Estrela");
            salas.PorPersonagem(sala.Id, 0, original.Id, false);
            var copia = personagens.Buscar(sala.Slots[0].Personagem);
            personagens.AprovarParaCampanha(copia.Id);

            Assert.That(personagens.Importar(personagens.Exportar(copia.Id), out var nova).Ok, Is.True);
            Assert.That(nova.Sala, Is.Null);
            Assert.That(nova.Origem, Is.Null);
            Assert.That(nova.Status, Is.EqualTo(StatusCriacao.EmAprovacao));
            Assert.That(nova.Relacionamentos, Is.Empty);
        }

        [Test]
        public void Reimportar_atualiza_a_mesma_ficha_e_mantem_o_contexto_daqui()
        {
            var armazenamento = new ArmazenamentoEmMemoria();
            var personagens = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            var salas = CadastroSalas.Carregar(armazenamento, personagens);
            salas.Criar("Nova Era", 6);
            var sala = salas.Listar().Single();
            var original = FichaPronta.Criada(personagens, "Estrela");
            salas.PorPersonagem(sala.Id, 0, original.Id, false);
            salas.DefinirJogador(sala.Id, 0, "j1");
            var ficha = personagens.Buscar(sala.Slots[0].Personagem);

            // Outra máquina ajusta a aparência e devolve o JSON (com jogador e status diferentes).
            var json = personagens.Exportar(ficha.Id)
                .Replace("\"aparencia\": \"\"", "\"aparencia\": \"cabelos rosas\"") // a ficha pronta nasce sem aparência
                .Replace("\"jogador\": \"j1\"", "\"jogador\": \"outro\"");
            Assert.That(personagens.IdNaTroca(json), Is.EqualTo(ficha.Id));
            Assert.That(personagens.Reimportar(ficha.Id, json).Ok, Is.True);

            Assert.That(ficha.Identidade.Aparencia, Is.EqualTo("cabelos rosas"));
            Assert.That(ficha.Identidade.Jogador, Is.EqualTo("j1"));       // o jogador é daqui
            Assert.That(ficha.Sala, Is.EqualTo(sala.Id));                   // a sala também
            Assert.That(ficha.Status, Is.EqualTo(StatusCriacao.EmAprovacao));
            Assert.That(sala.Slots[0].Personagem, Is.EqualTo(ficha.Id));
        }

        [Test]
        public void Reimportar_respeita_o_ciclo_e_o_modo_teste()
        {
            var personagens = CadastroPersonagens.Carregar(new ArmazenamentoEmMemoria(), RegrasDeTeste.Arquetipos());
            var concluida = FichaPronta.Criada(personagens, "Estrela");
            personagens.Criar("", "Rascunho", "farol", null);
            var rascunho = personagens.Listar().Single(x => x.NomeExibicao == "Rascunho");

            // Ficha concluída não aceita um arquivo sem a criação concluída.
            Assert.That(personagens.Reimportar(concluida.Id, personagens.Exportar(rascunho.Id)).Ok, Is.False);
            // Em criação, a foto do arquivo é ignorada.
            Assert.That(personagens.Reimportar(rascunho.Id, personagens.Exportar(concluida.Id)).Ok, Is.True);
            Assert.That(rascunho.Foto, Is.Null);
            Assert.That(rascunho.Status, Is.EqualTo(StatusCriacao.Novo));
            // Modo teste ligado: recusa.
            personagens.LigarTeste(concluida.Id);
            Assert.That(personagens.Reimportar(concluida.Id, personagens.Exportar(concluida.Id)).Ok, Is.False);
        }

        [Test]
        public void Lixo_ou_outro_formato_e_recusado()
        {
            var b = CadastroPersonagens.Carregar(new ArmazenamentoEmMemoria(), RegrasDeTeste.Arquetipos());
            Assert.That(b.Importar("isso não é json", out _).Ok, Is.False);
            Assert.That(b.Importar("{\"formato\":\"outra-coisa\"}", out _).Ok, Is.False);
            Assert.That(b.Importar("{\"formato\":\"torre-de-controle/personagem\",\"versao\":99,\"personagem\":{}}", out _).Ok, Is.False);
            Assert.That(b.Listar(), Is.Empty);
        }
    }
}
