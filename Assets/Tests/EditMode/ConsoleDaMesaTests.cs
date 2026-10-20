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
        public void Pot_forca_o_potencial_para_o_valor()
        {
            var nome = heroi.NomeExibicao.Split(' ')[0];
            Assert.That(console.Executar($"/pot {nome} 3").ok, Is.True);
            Assert.That(heroi.Potencial, Is.EqualTo(3));
            Assert.That(console.Executar($"/potencial {nome} 1").ok, Is.True, "também desce");
            Assert.That(heroi.Potencial, Is.EqualTo(1));
            Assert.That(console.Executar($"/pot {nome} -1").ok, Is.False);
            Assert.That(console.Executar($"/potencial {nome} o soco foi absorvido").ok, Is.True, "com motivo: +1 como antes");
            Assert.That(heroi.Potencial, Is.EqualTo(2));
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
            Assert.That(console.Executar("/clear").ok, Is.True, "/clear é o mesmo que /clearlog");
            Assert.That(ok, Is.True);
            Assert.That(mesa.Visiveis, Is.Empty);
            Assert.That(mesa.Equipe, Is.EqualTo(2), "limpar o log não desfaz nada");
            Assert.That(console.Executar("/xyz").ok, Is.False);
        }

        [Test]
        public void Fixo_forca_as_proximas_rolagens_na_ordem_e_limpa()
        {
            Assert.That(console.Executar("/fixo 13").ok, Is.False, "2d6 vai até 12");
            Assert.That(console.Executar("/fixo 8 11").ok, Is.True);
            console.Executar("/roll +1");
            Assert.That(mesa.Eventos.Last().Rolagem.Dados.Sum(), Is.EqualTo(8));
            Assert.That(mesa.Eventos.Last().Rolagem.Total, Is.EqualTo(9), "o modificador soma por cima");
            Assert.That(mesa.TirarDadoFixo(out var a, out var b), Is.True);
            Assert.That((a, b), Is.EqualTo((6, 5)));
            Assert.That(mesa.TirarDadoFixo(out _, out _), Is.False, "a fila acabou");
            console.Executar("/fixo 2");
            Assert.That(console.Executar("/fixo").ok, Is.True);
            Assert.That(mesa.DadosFixos, Is.Empty, "/fixo sozinho limpa");
        }

        [Test]
        public void Cena_marca_o_log_sem_mexer_na_pilha_e_tempo_volta_para_um()
        {
            console.Executar("/pilha +3");
            Assert.That(console.Executar("/cena \"o hospital\"").ok, Is.True);
            Assert.That(mesa.Visiveis.Last().Descricao, Does.StartWith("nova cena: o hospital · em cena: "));
            Assert.That(mesa.Equipe, Is.EqualTo(3), "a pilha só volta para 1 quando o tempo passa (p.88)");
            Assert.That(console.Executar("/tempo três semanas depois").ok, Is.True);
            Assert.That(mesa.Equipe, Is.EqualTo(1));
            Assert.That(console.Executar("/avancar").ok, Is.True);
            Assert.That(mesa.Rodada, Is.EqualTo(2));
            var (ok, texto) = console.Executar("/salvar");
            Assert.That(ok, Is.True);
            Assert.That(texto, Does.Contain("r1  Mesa: nova cena: o hospital"));
            Assert.That(texto, Does.Contain("— três semanas depois"));
        }

        [Test]
        public void Reset_volta_a_mesa_e_as_fichas_ao_inicio_e_cada_parte_se_desfaz()
        {
            var nome = heroi.NomeExibicao.Split(' ')[0];
            console.Executar("/pilha +2");
            console.Executar($"/pot {nome} 3");
            console.Executar($"/cond {nome} inseguro");
            console.Executar("/rodada");
            Assert.That(console.Executar("/reset").ok, Is.True);
            Assert.That(mesa.Rodada, Is.EqualTo(1));
            Assert.That(mesa.Equipe, Is.EqualTo(0));
            Assert.That(heroi.Potencial, Is.EqualTo(0));
            Assert.That(heroi.CondicoesMarcadas, Is.Empty);
            Assert.That(mesa.Visiveis.Count, Is.EqualTo(2), "só os dois eventos do reset (ficha e mesa); o resto ficou escondido");
            Assert.That(mesa.Eventos.Count(e => e.Oculto), Is.EqualTo(3), "pilha, potencial e condição: escondidos, nada se exclui");
            var daFicha = mesa.Visiveis.First(e => e.Personagem == heroi.Id);
            Assert.That(mesa.Corrigir(daFicha.Id, "engano").Ok, Is.True);
            Assert.That(heroi.Potencial, Is.EqualTo(3), "desfazer o reset da ficha devolve o potencial");
            Assert.That(heroi.CondicoesMarcadas, Does.Contain("inseguro"));
        }

        [Test]
        public void Atalho_deixa_a_rolagem_pendente_no_log_e_a_rolagem_com_origem_cumpre()
        {
            var basicos = TorreDeControle.Core.Regras.MovimentosBasicos.Carregar(new RegrasDeTeste());
            var comAtalhos = new ConsoleDaMesa(mesa, personagens, sala, new Random(42), basicos);
            personagens.CriarNpc(sala.Id, "Doutor Ocaso", Maturidade.Adulto, Papel.Vilao);
            var ocaso = personagens.NpcsDaSala(sala.Id).Single();
            mesa.AlternarEmCena(ocaso.Id);
            var nome = heroi.NomeExibicao.Split(' ')[0];

            Assert.That(console.Executar($"/eda {nome} vs Ocaso").ok, Is.False, "sem os movimentos, o atalho não vale");
            Assert.That(comAtalhos.Executar($"/eda {nome}").ok, Is.False, "o Enfrentar pede contra quem");
            Assert.That(comAtalhos.Executar($"/tgp {nome} vs Ocaso").ok, Is.False, "o golpe não tem contra quem");

            var (ok, msg) = comAtalhos.Executar($"/eda {nome} vs Ocaso na raça");
            Assert.That(ok, Is.True, msg);
            var pedido = mesa.Eventos.Last();
            Assert.That(pedido.Pedido.Movimento, Is.EqualTo("basico/enfrentar"));
            Assert.That(pedido.Pedido.Alvo, Is.EqualTo(ocaso.Id));
            Assert.That(pedido.Motivo, Is.EqualTo("na raça"));
            Assert.That(pedido.Descricao, Is.EqualTo("pendente: Enfrentar diretamente a ameaça → Doutor Ocaso"));
            Assert.That(mesa.PedidoAberto(pedido), Is.True);

            var enfrentar = basicos.Todos.First(m => m.Id == "basico/enfrentar");
            Assert.That(mesa.RolarMovimento(heroi.Id, enfrentar, null, 3, 4, null, origem: pedido.Id).Ok, Is.False, "precisa bater com o pedido");
            Assert.That(mesa.RolarMovimento(heroi.Id, enfrentar, ocaso.Id, 3, 4, null, origem: pedido.Id).Ok, Is.True);
            Assert.That(mesa.PedidoAberto(pedido), Is.False, "rolou: o botão some");
            Assert.That(mesa.Eventos.Last().Descricao, Does.Contain("pedido no log (r1): na raça"));

            Assert.That(comAtalhos.Executar($"/tgp {nome} \"descobriu a verdade sobre a irmã\"").ok, Is.True);
            var golpe = mesa.Eventos.Last();
            Assert.That(golpe.Pedido.Alvo, Is.Null);
            Assert.That(golpe.Motivo, Is.EqualTo("descobriu a verdade sobre a irmã"));
            mesa.ApagarEvento(golpe.Id, desfazerEfeito: true);
            Assert.That(mesa.PedidoAberto(golpe), Is.False, "escondido: desistiu do pedido");
        }
    }
}
