using System.Linq;
using NUnit.Framework;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Salas;

namespace TorreDeControle.Tests
{
    public class CadastroSalasTests
    {
        ArmazenamentoEmMemoria armazenamento;
        CadastroPersonagens personagens;
        CadastroSalas salas;

        [SetUp]
        public void Preparar()
        {
            armazenamento = new ArmazenamentoEmMemoria();
            personagens = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            salas = CadastroSalas.Carregar(armazenamento, personagens);
        }

        Sala NovaSala(string nome = "Nova Era", int slots = 6)
        {
            Assert.That(salas.Criar(nome, slots).Ok, Is.True);
            return salas.Listar().Last();
        }

        [Test]
        public void Criar_sala_com_slots_e_nome_unico()
        {
            var sala = NovaSala();
            Assert.That(sala.Slots, Has.Count.EqualTo(6));
            Assert.That(salas.Criar("nova era", 4).Ok, Is.False);
            Assert.That(salas.Criar("", 4).Ok, Is.False);
            Assert.That(salas.Criar("Outra", 0).Ok, Is.False);
        }

        [Test]
        public void Diminuir_slots_so_tira_vazios_do_fim()
        {
            var sala = NovaSala(slots: 4);
            salas.DefinirJogador(sala.Id, 3, "j1");
            Assert.That(salas.DefinirSlots(sala.Id, 2).Ok, Is.False);
            salas.DefinirJogador(sala.Id, 3, null);
            Assert.That(salas.DefinirSlots(sala.Id, 2).Ok, Is.True);
            Assert.That(sala.Slots, Has.Count.EqualTo(2));
            Assert.That(salas.DefinirSlots(sala.Id, 5).Ok, Is.True);
            Assert.That(sala.Slots, Has.Count.EqualTo(5));
        }

        [Test]
        public void Um_slot_por_jogador_na_sala()
        {
            var sala = NovaSala();
            Assert.That(salas.DefinirJogador(sala.Id, 0, "j1").Ok, Is.True);
            Assert.That(salas.DefinirJogador(sala.Id, 1, "j1").Ok, Is.False);
        }

        [Test]
        public void Por_na_sala_copia_e_a_original_fica_no_laboratorio()
        {
            var sala = NovaSala();
            var original = FichaPronta.Criada(personagens, "Estrela", jogador: "j1");

            Assert.That(salas.PorPersonagem(sala.Id, 0, original.Id, comJogo: false).Ok, Is.True);

            var copia = personagens.Buscar(sala.Slots[0].Personagem);
            Assert.That(copia.Id, Is.Not.EqualTo(original.Id));
            Assert.That(copia.Origem, Is.EqualTo(original.Id));
            Assert.That(copia.Sala, Is.EqualTo(sala.Id));
            Assert.That(copia.Status, Is.EqualTo(StatusCriacao.EmAprovacao)); // o mestre aprova na campanha
            Assert.That(original.Status, Is.EqualTo(StatusCriacao.EmAprovacao));
            // O slot vazio herdou o jogador da personagem.
            Assert.That(sala.Slots[0].Jogador, Is.EqualTo("j1"));
            Assert.That(copia.Identidade.Jogador, Is.EqualTo("j1"));
            // A cópia não aparece no laboratório.
            Assert.That(personagens.Listar(), Has.No.Member(copia));
            Assert.That(personagens.DaSala(sala.Id), Has.Member(copia));
        }

        [Test]
        public void So_ficha_criada_entra_e_uma_vez_por_sala()
        {
            var sala = NovaSala();
            personagens.Criar("", "Rascunho", "farol", null);
            var rascunho = personagens.Listar().Last();
            Assert.That(salas.PorPersonagem(sala.Id, 0, rascunho.Id, false).Ok, Is.False);

            var criada = FichaPronta.Criada(personagens, "Estrela");
            Assert.That(salas.PorPersonagem(sala.Id, 0, criada.Id, false).Ok, Is.True);
            Assert.That(salas.PorPersonagem(sala.Id, 1, criada.Id, false).Ok, Is.False);
            // Em outra sala pode.
            var outra = NovaSala("Outra");
            Assert.That(salas.PorPersonagem(outra.Id, 0, criada.Id, false).Ok, Is.True);
        }

        [Test]
        public void So_a_criacao_deixa_o_jogo_do_laboratorio_para_tras()
        {
            var sala = NovaSala();
            var original = FichaPronta.Criada(personagens, "Estrela");
            original.Potencial = 7;
            original.AvancosMarcados.Add("cima:0");

            salas.PorPersonagem(sala.Id, 0, original.Id, comJogo: false);
            salas.PorPersonagem(sala.Id, 1, original.Id, comJogo: true); // mesma origem: recusado
            var limpa = personagens.Buscar(sala.Slots[0].Personagem);
            Assert.That(limpa.Potencial, Is.EqualTo(0));
            Assert.That(limpa.AvancosMarcados, Is.Empty);
            Assert.That(limpa.Foto, Is.Not.Null);

            var outra = NovaSala("Outra");
            salas.PorPersonagem(outra.Id, 0, original.Id, comJogo: true);
            var comJogo = personagens.Buscar(outra.Slots[0].Personagem);
            Assert.That(comJogo.Potencial, Is.EqualTo(7));
            Assert.That(comJogo.AvancosMarcados, Has.Count.EqualTo(1));
        }

        [Test]
        public void Trocar_jogador_do_slot_acompanha_a_copia()
        {
            var sala = NovaSala();
            var original = FichaPronta.Criada(personagens, "Estrela", jogador: "j1");
            salas.PorPersonagem(sala.Id, 0, original.Id, false);
            salas.DefinirJogador(sala.Id, 0, "j2");
            Assert.That(personagens.Buscar(sala.Slots[0].Personagem).Identidade.Jogador, Is.EqualTo("j2"));
            Assert.That(original.Identidade.Jogador, Is.EqualTo("j1"));
        }

        [Test]
        public void Tirar_do_slot_inativa_a_copia_sem_apagar()
        {
            var sala = NovaSala();
            var original = FichaPronta.Criada(personagens, "Estrela");
            salas.PorPersonagem(sala.Id, 0, original.Id, false);
            var copia = sala.Slots[0].Personagem;

            Assert.That(salas.TirarPersonagem(sala.Id, 0).Ok, Is.True);
            Assert.That(sala.Slots[0].Personagem, Is.Null);
            Assert.That(personagens.Buscar(copia).Ativo, Is.False);
            Assert.That(personagens.Buscar(copia).Status, Is.EqualTo(StatusCriacao.Aposentado));
            // Liberou: a mesma personagem pode voltar (nova cópia).
            Assert.That(salas.PorPersonagem(sala.Id, 0, original.Id, false).Ok, Is.True);
        }

        [Test]
        public void Original_com_instancia_em_vaga_nao_volta_para_novo()
        {
            var sala = NovaSala();
            var original = FichaPronta.Criada(personagens, "Estrela");
            salas.PorPersonagem(sala.Id, 0, original.Id, false);
            Assert.That(personagens.ReabrirCriacao(original.Id).Ok, Is.False);

            // Tirada da vaga, a original volta a poder.
            salas.TirarPersonagem(sala.Id, 0);
            Assert.That(personagens.ReabrirCriacao(original.Id).Ok, Is.True);
        }

        [Test]
        public void Instancia_reaberta_fica_na_vaga_e_volta_a_carreira_ao_concluir()
        {
            var sala = NovaSala();
            var original = FichaPronta.Criada(personagens, "Estrela");
            salas.PorPersonagem(sala.Id, 0, original.Id, false);
            var copia = personagens.Buscar(sala.Slots[0].Personagem);
            copia.Potencial = 3;

            Assert.That(personagens.ReabrirCriacao(copia.Id).Ok, Is.True);
            Assert.That(copia.Status, Is.EqualTo(StatusCriacao.EmRevisao));
            Assert.That(copia.Foto, Is.Null);
            Assert.That(sala.Slots[0].Personagem, Is.EqualTo(copia.Id));
            Assert.That(copia.Potencial, Is.EqualTo(3)); // o jogo marcado fica

            Assert.That(personagens.Aprovar(copia.Id).Ok, Is.True);
            Assert.That(copia.Status, Is.EqualTo(StatusCriacao.EmAprovacao));
            Assert.That(original.Status, Is.EqualTo(StatusCriacao.EmAprovacao));
        }

        [Test]
        public void Em_carreira_nao_volta_para_revisao()
        {
            var sala = NovaSala();
            var original = FichaPronta.Criada(personagens, "Estrela");
            salas.PorPersonagem(sala.Id, 0, original.Id, false);
            var copia = personagens.Buscar(sala.Slots[0].Personagem);
            Assert.That(personagens.AprovarParaCampanha(copia.Id).Ok, Is.True);
            Assert.That(personagens.ReabrirCriacao(copia.Id).Ok, Is.False);
            Assert.That(copia.Status, Is.EqualTo(StatusCriacao.EmCarreira));
        }

        [Test]
        public void Instancia_do_arquivo_nao_volta_para_novo()
        {
            var sala = NovaSala();
            var original = FichaPronta.Criada(personagens, "Estrela");
            salas.PorPersonagem(sala.Id, 0, original.Id, false);
            var copia = sala.Slots[0].Personagem;
            salas.TirarPersonagem(sala.Id, 0);
            Assert.That(personagens.ReabrirCriacao(copia).Ok, Is.False);
        }

        [Test]
        public void Aprovar_para_a_campanha_so_na_sala_e_em_aprovacao()
        {
            var sala = NovaSala();
            var original = FichaPronta.Criada(personagens, "Estrela");
            Assert.That(personagens.AprovarParaCampanha(original.Id).Ok, Is.False); // laboratório
            salas.PorPersonagem(sala.Id, 0, original.Id, false);
            var copia = personagens.Buscar(sala.Slots[0].Personagem);
            Assert.That(personagens.AprovarParaCampanha(copia.Id).Ok, Is.True);
            Assert.That(personagens.AprovarParaCampanha(copia.Id).Ok, Is.False); // já em carreira
        }

        [Test]
        public void Salas_e_copias_sobrevivem_a_recarga()
        {
            var sala = NovaSala();
            var original = FichaPronta.Criada(personagens, "Estrela");
            salas.PorPersonagem(sala.Id, 2, original.Id, false);

            var p2 = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            var s2 = CadastroSalas.Carregar(armazenamento, p2);
            var recarregada = s2.Buscar(sala.Id);
            Assert.That(recarregada.Slots, Has.Count.EqualTo(6));
            Assert.That(p2.Buscar(recarregada.Slots[2].Personagem).Sala, Is.EqualTo(sala.Id));
            Assert.That(p2.Listar(), Has.Count.EqualTo(1));
        }
    }
}
