using System;
using System.Linq;
using NUnit.Framework;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Salas;

namespace TorreDeControle.Tests
{
    /// <summary>O console do mestre na mesa: comandos com barra pelos mesmos caminhos da tela.</summary>
    public class ConsoleDaMesaTests
    {
        ArmazenamentoEmMemoria armazenamento;
        CadastroPersonagens personagens;
        CadastroSalas salas;
        Sala sala;
        Mesa mesa;
        Personagem heroi;
        ConsoleDaMesa console;

        [SetUp]
        public void Preparar()
        {
            armazenamento = new ArmazenamentoEmMemoria();
            personagens = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            salas = CadastroSalas.Carregar(armazenamento, personagens);
            salas.Criar("Halcyon City", 6);
            sala = salas.Listar().Single();
            var original = FichaPronta.Criada(personagens, "Aurora Dourada");
            salas.PorPersonagem(sala.Id, 0, original.Id, false);
            heroi = personagens.Buscar(sala.Slots[0].Personagem);
            FichaPronta.ParaCarreira(salas, personagens, sala, heroi);
            mesa = Mesa.Carregar(armazenamento, personagens, sala.Id);
            console = new ConsoleDaMesa(mesa, personagens, sala, new Random(42));
        }

        [Test]
        public void Partes_respeitam_aspas()
        {
            Assert.That(ConsoleDaMesa.Partes("/npc_create \"Seu Valdir\" neutral \"vigia da usina\""),
                Is.EqualTo(new[] { "/npc_create", "Seu Valdir", "neutral", "vigia da usina" }));
        }

        [Test]
        public void Roll_anota_no_log_e_respeita_o_teto()
        {
            var (ok, msg) = console.Executar("/roll +9");
            Assert.That(ok, Is.True);
            Assert.That(msg, Does.StartWith("rolou 2d6+4"), "o teto corta em +4");
            Assert.That(mesa.Visiveis.Last().Descricao, Is.EqualTo(msg));
        }

        [Test]
        public void Rolagem_com_os_dados_fisicos()
        {
            Assert.That(ConsoleDaMesa.EhRolagem("/rolar +1 avaliar"), Is.True);
            Assert.That(ConsoleDaMesa.EhRolagem("/nota rolar"), Is.False);
            var (ok, msg) = console.RolarComDados("/roll +1 avaliar a usina", 4, 5);
            Assert.That(ok, Is.True);
            Assert.That(msg, Is.EqualTo("rolou 2d6+1: 4 + 5 + 1 = 10 (10+)"));
            Assert.That(mesa.Visiveis.Last().Motivo, Is.EqualTo("avaliar a usina"));
            Assert.That(console.RolarComDados("/roll", 7, 1).ok, Is.False);
        }

        [Test]
        public void Potencial_pede_motivo_e_acha_pelo_comeco_do_nome()
        {
            Assert.That(console.Executar("/potencial Aur").ok, Is.False);
            var (ok, _) = console.Executar("/potencial aur o soco foi absorvido");
            Assert.That(ok, Is.True);
            Assert.That(heroi.Potencial, Is.EqualTo(1));
            Assert.That(mesa.Eventos.Last().Motivo, Is.EqualTo("o soco foi absorvido"));
        }

        [Test]
        public void Npc_e_local_entram_na_campanha()
        {
            Assert.That(console.Executar("/npc_create \"Seu Valdir\" neutral \"vigia noturno\"").ok, Is.True);
            var valdir = personagens.NpcsDaSala(sala.Id).Single(n => n.NomeExibicao == "Seu Valdir");
            Assert.That(valdir.Papel, Is.EqualTo(Papel.Neutro));
            Assert.That(valdir.Identidade.Resumo, Is.EqualTo("vigia noturno"));
            Assert.That(console.Executar("/npc_create Fulano chefe").ok, Is.False, "papel desconhecido");

            Assert.That(console.Executar("/location_create \"Usina Halcyon Sul\" \"galpão destruído\"").ok, Is.True);
            Assert.That(personagens.CenarioDaSala(sala.Id).Single().Descricao, Is.EqualTo("galpão destruído"));
        }

        [Test]
        public void Pilha_cond_nota_e_clearlog()
        {
            Assert.That(console.Executar("/pilha +2 questionário").ok, Is.True);
            Assert.That(mesa.Equipe, Is.EqualTo(2));
            Assert.That(console.Executar("/cond aurora inseg").ok, Is.True);
            Assert.That(heroi.CondicoesMarcadas, Has.Member(Condicoes.Inseguro));
            Assert.That(console.Executar("/nota a cena vai para o hospital").ok, Is.True);
            var (ok, _) = console.Executar("/clearlog");
            Assert.That(ok, Is.True);
            Assert.That(mesa.Visiveis, Is.Empty);
            Assert.That(mesa.Equipe, Is.EqualTo(2), "limpar o log não desfaz nada");
            Assert.That(console.Executar("/xyz").ok, Is.False);
        }
    }
}
