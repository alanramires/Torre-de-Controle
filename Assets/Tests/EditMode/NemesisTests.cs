using System.Linq;
using NUnit.Framework;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Salas;

namespace TorreDeControle.Tests
{
    /// <summary>O nêmesis da Amaldiçoada: escrito na ficha, vira um NPC vilão da sala.</summary>
    public class NemesisTests
    {
        const string Extra = "amaldicoada/nemesis";

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

        /// <summary>Uma Amaldiçoada no laboratório, ainda em criação (atalho: a ficha pronta é uma Nova).</summary>
        Personagem Amaldicoada()
        {
            var p = FichaPronta.Criada(personagens, "Crepúsculo");
            Assert.That(personagens.ReabrirCriacao(p.Id).Ok, Is.True);
            p.Identidade.Arquetipo = "amaldicoada";
            return p;
        }

        /// <summary>Atalho de teste: dá a criação por concluída sem refazer a ficha toda.</summary>
        static void Concluida(Personagem p) => p.Status = StatusCriacao.EmAprovacao;

        [Test]
        public void No_laboratorio_fica_so_o_texto_e_na_sala_nasce_o_vilao()
        {
            var original = Amaldicoada();
            Assert.That(personagens.DefinirTextoDeExtra(original.Id, Extra, "nome", null, "Lady Faust").Ok, Is.True);
            personagens.DefinirTextoDeExtra(original.Id, Extra, "descricao", null, "Minha ancestral.");
            Assert.That(personagens.NpcDaFicha(original, Extra), Is.Null); // laboratório não tem sala

            Concluida(original);
            var copia = personagens.CopiarParaSala(original.Id, sala.Id, null, false, out var r);
            Assert.That(r.Ok, Is.True);

            var npc = personagens.NpcDaFicha(copia, Extra);
            Assert.That(npc, Is.Not.Null);
            Assert.That(npc.Sala, Is.EqualTo(sala.Id));
            Assert.That(npc.Papel, Is.EqualTo(Papel.Vilao));
            Assert.That(npc.NomeExibicao, Is.EqualTo("Lady Faust"));
            Assert.That(personagens.NpcsDaSala(sala.Id), Has.Member(npc));

            var origem = personagens.OrigemDoNpc(npc).Value;
            Assert.That(origem.dono, Is.EqualTo(copia));
            Assert.That(origem.vinculo, Is.EqualTo("nêmesis"));
            Assert.That(origem.descricao, Is.EqualTo("Minha ancestral."));
        }

        [Test]
        public void Renomear_muda_o_npc_e_apagar_inativa_sem_excluir()
        {
            var original = Amaldicoada();
            personagens.DefinirTextoDeExtra(original.Id, Extra, "nome", null, "Lady Faust");
            Concluida(original);
            var copia = personagens.CopiarParaSala(original.Id, sala.Id, null, false, out _);
            Assert.That(personagens.ReabrirCriacao(copia.Id).Ok, Is.True); // em revisão: a ficha da sala volta a ser editável
            var npc = personagens.NpcDaFicha(copia, Extra);

            personagens.DefinirTextoDeExtra(copia.Id, Extra, "nome", null, "Madame Faust");
            Assert.That(npc.NomeExibicao, Is.EqualTo("Madame Faust"));

            personagens.DefinirTextoDeExtra(copia.Id, Extra, "nome", null, "");
            Assert.That(npc.Ativo, Is.False);
            Assert.That(personagens.NpcsDaSala(sala.Id), Has.Member(npc)); // inativo, não excluído

            personagens.DefinirTextoDeExtra(copia.Id, Extra, "nome", null, "Lady Faust");
            Assert.That(npc.Ativo, Is.True); // o mesmo NPC volta
            Assert.That(personagens.NpcsDaSala(sala.Id).Count, Is.EqualTo(1));
        }

        /// <summary>Uma instância na sala, em aprovação (foto congelada), como Amaldiçoada.</summary>
        Personagem AmaldicoadaEmAprovacao()
        {
            var original = FichaPronta.Criada(personagens, "Estrela Negra");
            var copia = personagens.CopiarParaSala(original.Id, sala.Id, null, false, out _);
            copia.Identidade.Arquetipo = "amaldicoada"; // atalho de teste: a ficha pronta é uma Nova
            copia.Foto.Arquetipo = "amaldicoada";
            return copia;
        }

        [Test]
        public void Em_aprovacao_na_sala_o_nemesis_ainda_se_edita_e_em_carreira_trava()
        {
            var copia = AmaldicoadaEmAprovacao();
            Assert.That(copia.Status, Is.EqualTo(StatusCriacao.EmAprovacao));
            Assert.That(personagens.DefinirTextoDeExtra(copia.Id, Extra, "nome", null, "Eclipse").Ok, Is.True);
            Assert.That(personagens.TextoDoExtra(copia, Extra, "nome"), Is.EqualTo("Eclipse")); // lido da foto
            Assert.That(personagens.NpcDaFicha(copia, Extra).NomeExibicao, Is.EqualTo("Eclipse"));
            Assert.That(personagens.DefinirTextoDeExtra(copia.Id, "amaldicoada/perdicao", "gatilhos", null, "x").Ok, Is.False); // só o que vira NPC

            copia.Status = StatusCriacao.EmCarreira; // atalho de teste
            Assert.That(personagens.DefinirTextoDeExtra(copia.Id, Extra, "nome", null, "Outro").Ok, Is.False);
        }

        [Test]
        public void Ficha_que_ja_estava_na_sala_ganha_o_npc_ao_carregar()
        {
            var copia = AmaldicoadaEmAprovacao();
            copia.Foto.ExtrasTextos[Extra + "|nome"] = "Eclipse"; // como se viesse de antes do nêmesis virar NPC
            personagens.CriarNpc(sala.Id, "Louva-a-Deus", Maturidade.Adulto, Papel.Aliado); // força gravar

            var relido = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            Assert.That(relido.NpcDaFicha(relido.Buscar(copia.Id), Extra)?.NomeExibicao, Is.EqualTo("Eclipse"));
        }

        [Test]
        public void Legado_cada_membro_preenchido_vira_npc_e_o_inimigo_vilao()
        {
            const string legado = "legado/membros";
            var copia = AmaldicoadaEmAprovacao();
            copia.Identidade.Arquetipo = "legado"; // atalho de teste
            copia.Foto.Arquetipo = "legado";

            personagens.DefinirTextoDeExtra(copia.Id, legado, "membros", "ainda-ativo-e-em-destaque", "Beatriz Monteiro");
            personagens.DefinirTextoDeExtra(copia.Id, legado, "membros", "proximo-da-linhagem", "Marina Monteiro");
            personagens.DefinirTextoDeExtra(copia.Id, legado, "membros", "maior-inimigo-ainda-a-solta", "Doutor Ocaso");
            personagens.DefinirTextoDeExtra(copia.Id, legado, "membros",
                Core.Regras.RegrasArquetipos.CampoExtra.DescricaoDaVaga("maior-inimigo-ainda-a-solta"), "Augusto Vilar, 62 anos.");

            var mae = personagens.NpcDaFicha(copia, legado, "membros", "ainda-ativo-e-em-destaque");
            var irma = personagens.NpcDaFicha(copia, legado, "membros", "proximo-da-linhagem");
            var inimigo = personagens.NpcDaFicha(copia, legado, "membros", "maior-inimigo-ainda-a-solta");
            Assert.That(personagens.NpcDaFicha(copia, legado, "membros", "aposentado-e-critico"), Is.Null); // vazio: sem NPC
            Assert.That(mae.Papel, Is.EqualTo(Papel.Aliado));
            Assert.That(mae.Maturidade, Is.EqualTo(Maturidade.Adulto));
            Assert.That(irma.Maturidade, Is.EqualTo(Maturidade.Jovem));
            Assert.That(inimigo.Papel, Is.EqualTo(Papel.Vilao));
            Assert.That(inimigo.NomeExibicao, Is.EqualTo("Doutor Ocaso"));
            Assert.That(personagens.OrigemDoNpc(inimigo).Value.descricao, Is.EqualTo("Augusto Vilar, 62 anos."));
            Assert.That(personagens.TextoDoExtra(copia, legado, "membros", "maior-inimigo-ainda-a-solta"), Is.EqualTo("Doutor Ocaso")); // a descrição não pisa no nome
        }

        [Test]
        public void Nova_quem_feriu_e_quem_ajuda_viram_npcs_com_a_maturidade_a_definir()
        {
            var original = FichaPronta.Criada(personagens, "Pulsar"); // a ficha pronta já é uma Nova
            Assert.That(personagens.DefinirNomeNoHistorico(original.Id, 0, "x").Ok, Is.False); // essa pergunta não dá NPC
            personagens.DefinirNomeNoHistorico(original.Id, 1, "Tio Rui");
            Assert.That(personagens.NpcDaFicha(original, CadastroPersonagens.FonteHistorico), Is.Null); // laboratório

            var copia = personagens.CopiarParaSala(original.Id, sala.Id, null, false, out _);
            personagens.ResponderHistorico(copia.Id, 1, "Meu tio, queimei a mão dele no Natal.");
            personagens.DefinirNomeNoHistorico(copia.Id, 2, "Doutora Lin");

            var tio = personagens.NpcDaFicha(copia, CadastroPersonagens.FonteHistorico, "1");
            var lin = personagens.NpcDaFicha(copia, CadastroPersonagens.FonteHistorico, "2");
            Assert.That(tio.Papel, Is.EqualTo(Papel.Neutro));
            Assert.That(lin.Papel, Is.EqualTo(Papel.Aliado));
            Assert.That(tio.NomeExibicao, Is.EqualTo("Tio Rui"));
            Assert.That(personagens.OrigemDoNpc(tio).Value.descricao, Is.EqualTo("Meu tio, queimei a mão dele no Natal."));
            Assert.That(personagens.AvisosDoNpc(tio), Has.Some.Contains("adulto ou jovem"));

            personagens.DefinirMaturidade(tio.Id, Maturidade.Jovem);
            Assert.That(personagens.AvisosDoNpc(tio), Has.None.Contains("adulto ou jovem"));
        }

        [Test]
        public void Janus_pessoa_de_cada_obrigacao_vira_npc_e_desmarcar_inativa()
        {
            const string ext = "janus/identidade-secreta";
            var original = FichaPronta.Criada(personagens, "Máscara");
            Assert.That(personagens.ReabrirCriacao(original.Id).Ok, Is.True);
            original.Identidade.Arquetipo = "janus"; // atalho de teste
            personagens.AlternarOpcaoDeExtra(original.Id, ext, "obrigacoes", OpcaoDoJanus(original, 0));
            var opcao = OpcaoDoJanus(original, 0);
            var pessoa = Core.Regras.RegrasArquetipos.CampoExtra.PessoaDaOpcao(opcao);
            Assert.That(personagens.DefinirTextoDeExtra(original.Id, ext, "obrigacoes", pessoa, "Doutora Paula").Ok, Is.True);
            personagens.DefinirTextoDeExtra(original.Id, ext, "obrigacoes", opcao, "Estágio na clínica.");
            Assert.That(personagens.DefinirTextoDeExtra(original.Id, ext, "obrigacoes",
                Core.Regras.RegrasArquetipos.CampoExtra.PessoaDaOpcao(OpcaoDoJanus(original, 1)), "x").Ok, Is.False); // não escolhida
            personagens.DefinirNomeNoHistorico(original.Id, 2, "Vó Cida");

            Concluida(original);
            var copia = personagens.CopiarParaSala(original.Id, sala.Id, null, false, out _);
            var chefe = personagens.NpcDaFicha(copia, ext, "obrigacoes", opcao);
            Assert.That(chefe.NomeExibicao, Is.EqualTo("Doutora Paula"));
            Assert.That(personagens.OrigemDoNpc(chefe).Value.descricao, Is.EqualTo("Estágio na clínica."));
            Assert.That(personagens.NpcDaFicha(copia, CadastroPersonagens.FonteHistorico, "2").Papel, Is.EqualTo(Papel.Aliado));

            // Em revisão, desmarcar a obrigação inativa o NPC dela (não exclui).
            Assert.That(personagens.ReabrirCriacao(copia.Id).Ok, Is.True);
            Assert.That(personagens.AlternarOpcaoDeExtra(copia.Id, ext, "obrigacoes", opcao).Ok, Is.True);
            Assert.That(chefe.Ativo, Is.False);
        }

        string OpcaoDoJanus(Personagem p, int k) =>
            personagens.ExtrasDoArquetipo(p).Single(e => e.Id == "janus/identidade-secreta").Campos[0].Opcoes[k].Id;

        [Test]
        public void Farol_impulso_com_pessoa_vira_npc_com_o_papel_da_opcao()
        {
            const string ext = "farol/impulsos";
            var original = FichaPronta.Criada(personagens, "Faísca");
            Assert.That(personagens.ReabrirCriacao(original.Id).Ok, Is.True);
            original.Identidade.Arquetipo = "farol"; // atalho de teste
            foreach (var o in new[] { "beijar-alguem-perigoso", "fazer-uma-acrobacia-ridicula", "superar-um-heroi-adulto" })
                Assert.That(personagens.AlternarOpcaoDeExtra(original.Id, ext, "marcados", o).Ok, Is.True);
            string Pessoa(string o) => Core.Regras.RegrasArquetipos.CampoExtra.PessoaDaOpcao(o);
            personagens.DefinirTextoDeExtra(original.Id, ext, "marcados", Pessoa("beijar-alguem-perigoso"), "Rainha Pavorosa");
            personagens.DefinirTextoDeExtra(original.Id, ext, "marcados", Pessoa("superar-um-heroi-adulto"), "Capitão Cometa");
            Assert.That(personagens.DefinirTextoDeExtra(original.Id, ext, "marcados", Pessoa("fazer-uma-acrobacia-ridicula"), "x").Ok,
                Is.False); // impulso de jogo: não tem pessoa
            personagens.DefinirNomeNoHistorico(original.Id, 2, "Pai");

            Concluida(original);
            var copia = personagens.CopiarParaSala(original.Id, sala.Id, null, false, out _);
            var rainha = personagens.NpcDaFicha(copia, ext, "marcados", "beijar-alguem-perigoso");
            Assert.That(rainha.Papel, Is.EqualTo(Papel.Vilao));
            Assert.That(personagens.NpcDaFicha(copia, ext, "marcados", "fazer-uma-acrobacia-ridicula"), Is.Null);
            var heroi = personagens.NpcDaFicha(copia, ext, "marcados", "superar-um-heroi-adulto");
            Assert.That(heroi.Papel, Is.EqualTo(Papel.Aliado));
            Assert.That(heroi.Maturidade, Is.EqualTo(Maturidade.Adulto));
            Assert.That(personagens.AvisosDoNpc(heroi), Has.None.Contains("adulto ou jovem")); // herói adulto já se sabe
            Assert.That(personagens.NpcDaFicha(copia, CadastroPersonagens.FonteHistorico, "2").Papel, Is.EqualTo(Papel.Neutro));
        }

        [Test]
        public void Npc_criado_pelo_mestre_nao_tem_origem()
        {
            personagens.CriarNpc(sala.Id, "Louva-a-Deus", Maturidade.Adulto, Papel.Aliado);
            Assert.That(personagens.OrigemDoNpc(personagens.NpcsDaSala(sala.Id).Single()), Is.Null);
        }
    }
}
