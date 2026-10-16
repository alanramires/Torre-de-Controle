using System.Linq;
using NUnit.Framework;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Salas;

namespace TorreDeControle.Tests
{
    /// <summary>Área do mestre, primeira sessão: preparo conferido nos dados e roteiro marcado pelo mestre.</summary>
    public class PrimeiraSessaoTests
    {
        ArmazenamentoEmMemoria armazenamento;
        CadastroPersonagens personagens;
        CadastroSalas salas;
        Sala sala;
        PrimeiraSessao roteiro;

        [SetUp]
        public void Preparar()
        {
            armazenamento = new ArmazenamentoEmMemoria();
            personagens = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            salas = CadastroSalas.Carregar(armazenamento, personagens);
            salas.Criar("Halcyon City", 5);
            sala = salas.Listar().Single();
            roteiro = PrimeiraSessao.Carregar(new RegrasDeTeste());
        }

        PrimeiraSessao.Conferido Item(string id) => roteiro.Conferir(sala, personagens).Single(c => c.Item.Id == id);

        [Test]
        public void Roteiro_traz_os_oito_movimentos_basicos_com_o_nome_das_regras()
        {
            var movimentos = roteiro.Roteiro.Where(i => i.Grupo == "movimentos").ToList();
            Assert.That(movimentos.Count, Is.EqualTo(8));
            Assert.That(movimentos.Single(i => i.Id == "basico/golpe-poderoso").Texto, Is.EqualTo("Tomar um golpe poderoso"));
            Assert.That(roteiro.Grupos.Select(g => g.id), Is.EqualTo(new[] { "abertura", "movimentos", "mecanicas", "depois" }));
        }

        [Test]
        public void Sala_vazia_nao_esta_pronta()
        {
            Assert.That(Item("fichas").Pronto, Is.False);
            Assert.That(Item("vilao").Pronto, Is.False);
            Assert.That(Item("ficha-do-vilao").Pronto, Is.False);
        }

        [Test]
        public void Inimigo_do_legado_e_o_vilao_e_a_ficha_dele_fica_pronta_com_tres_movimentos()
        {
            const string legado = "legado/membros";
            var original = FichaPronta.Criada(personagens, "Aurora Dourada");
            Assert.That(salas.PorPersonagem(sala.Id, 0, original.Id, false).Ok, Is.True);
            var aurora = personagens.Buscar(sala.Slots[0].Personagem);
            aurora.Identidade.Arquetipo = "legado"; // atalho de teste
            aurora.Foto.Arquetipo = "legado";
            personagens.DefinirTextoDeExtra(aurora.Id, legado, "membros", "maior-inimigo-ainda-a-solta", "Doutor Ocaso");

            Assert.That(Item("fichas").Pronto, Is.False, "em aprovação ainda não está pronta");
            Assert.That(Item("fichas").Detalhe, Does.Contain("Aurora Dourada em aprovação"));
            aurora.Status = StatusCriacao.EmCarreira; // atalho: aprovada
            Assert.That(Item("fichas").Pronto, Is.True);
            Assert.That(Item("vilao").Pronto, Is.True);
            Assert.That(Item("vilao").Detalhe, Does.Contain("Doutor Ocaso").And.Contain("Aurora Dourada"));
            Assert.That(Item("materia-prima").Pronto, Is.True);

            var ocaso = personagens.NpcDaFicha(aurora, legado, "membros", "maior-inimigo-ainda-a-solta");
            Assert.That(Item("ficha-do-vilao").Pronto, Is.False);
            personagens.DefinirMovimentoDeVilao(ocaso.Id, 0, "Apagar as luzes da cidade");
            personagens.DefinirMovimentoDeVilao(ocaso.Id, 1, "Fazer um refém da família Dourada");
            personagens.DefinirMovimentoDeVilao(ocaso.Id, 2, "Virar o público contra a heroína");
            personagens.AlternarCondicaoDeVilao(ocaso.Id, "irritado");
            Assert.That(Item("ficha-do-vilao").Pronto, Is.True, Item("ficha-do-vilao").Detalhe);
        }

        [Test]
        public void Renomear_o_vilao_da_ficha_muda_a_ficha_e_a_sincronizacao_nao_desfaz()
        {
            const string legado = "legado/membros";
            var original = FichaPronta.Criada(personagens, "Aurora Dourada");
            salas.PorPersonagem(sala.Id, 0, original.Id, false);
            var aurora = personagens.Buscar(sala.Slots[0].Personagem);
            aurora.Identidade.Arquetipo = "legado"; // atalho de teste
            aurora.Foto.Arquetipo = "legado";
            personagens.DefinirTextoDeExtra(aurora.Id, legado, "membros", "maior-inimigo-ainda-a-solta", "Doutor Ocaso (Augusto Vilar)");
            var ocaso = personagens.NpcDaFicha(aurora, legado, "membros", "maior-inimigo-ainda-a-solta");

            Assert.That(personagens.RenomearNpc(ocaso.Id, "Augusto Vilar", "Doutor Ocaso").Ok, Is.True);
            Assert.That(personagens.TextoDoExtra(aurora, legado, "membros", "maior-inimigo-ainda-a-solta"), Is.EqualTo("Doutor Ocaso"));
            personagens.DefinirTextoDeExtra(aurora.Id, legado, "membros", "aposentado-e-critico", "Helena"); // sincroniza
            Assert.That(ocaso.Identidade.Codinome, Is.EqualTo("Doutor Ocaso"));
            Assert.That(ocaso.NomeCompleto, Is.EqualTo("Doutor Ocaso (Augusto Vilar)")); // só exibição
            Assert.That(personagens.RenomearNpc(ocaso.Id, "Augusto Vilar", "").Ok, Is.False, "apagar é na ficha");
        }

        [Test]
        public void Vilao_criado_pelo_mestre_sem_ligacao_nao_conta_como_ligado()
        {
            personagens.CriarNpc(sala.Id, "Rosa Rook", Maturidade.Adulto, Papel.Vilao);
            Assert.That(Item("vilao").Pronto, Is.False);
            Assert.That(Item("vilao").Detalhe, Does.Contain("Rosa Rook"));
        }

        [Test]
        public void Vilao_do_mestre_ligado_a_uma_heroina_conta_e_mostra_o_como()
        {
            var original = FichaPronta.Criada(personagens, "Fantasma");
            salas.PorPersonagem(sala.Id, 0, original.Id, false);
            var fantasma = personagens.Buscar(sala.Slots[0].Personagem);
            personagens.CriarNpc(sala.Id, "Doutora Helix", Maturidade.Adulto, Papel.Vilao);
            var helix = personagens.NpcsDaSala(sala.Id).Single();

            Assert.That(personagens.DescreverLigacao(helix.Id, fantasma.Id, "x").Ok, Is.False, "sem ligar antes");
            Assert.That(personagens.AlternarLigacao(helix.Id, fantasma.Id).Ok, Is.True);
            Assert.That(personagens.DescreverLigacao(helix.Id, fantasma.Id, " destruiu o laboratório ").Ok, Is.True);
            Assert.That(Item("vilao").Pronto, Is.True);
            Assert.That(Item("vilao").Detalhe, Does.Contain("Doutora Helix (Fantasma · destruiu o laboratório)"));

            // Gravado; e desligar esquece o como.
            var relido = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos()).Buscar(helix.Id);
            Assert.That(relido.Pnj.Ligacoes.Single().Como, Is.EqualTo("destruiu o laboratório"));
            personagens.AlternarLigacao(helix.Id, fantasma.Id);
            Assert.That(personagens.LigacoesDoNpc(helix), Is.Empty);
            Assert.That(Item("vilao").Pronto, Is.False);
        }

        [Test]
        public void Ligacao_nao_vai_junto_na_copia_para_outra_campanha()
        {
            var original = FichaPronta.Criada(personagens, "Fantasma");
            salas.PorPersonagem(sala.Id, 0, original.Id, false);
            personagens.CriarNpc(sala.Id, "Doutora Helix", Maturidade.Adulto, Papel.Vilao);
            var helix = personagens.NpcsDaSala(sala.Id).Single();
            personagens.AlternarLigacao(helix.Id, sala.Slots[0].Personagem);
            Assert.That(personagens.AlternarLigacao(helix.Id, "nao-existe").Ok, Is.False);

            var copia = personagens.CopiarNpc(helix.Id, null, out _);
            Assert.That(copia.Pnj.Ligacoes, Is.Empty);
        }

        [Test]
        public void Anotacoes_do_mestre_vao_e_voltam_em_json()
        {
            salas.DefinirAnotacao(sala.Id, CadastroSalas.Anotacao.Formacao, " o Helix ficou em ruínas ");
            salas.DefinirAnotacao(sala.Id, CadastroSalas.Anotacao.Trama, "Ocaso monta uma bateria solar");
            var json = salas.ExportarAnotacoes(sala.Id);

            salas.Criar("Reboot", 5);
            var outra = salas.Listar().Single(s => s.Nome == "Reboot");
            salas.DefinirAnotacao(outra.Id, CadastroSalas.Anotacao.Npcs, "fica?");
            Assert.That(salas.ImportarAnotacoes(outra.Id, json).Ok, Is.True);
            Assert.That(outra.AnotacoesFormacao, Is.EqualTo("o Helix ficou em ruínas"));
            Assert.That(outra.Trama, Is.EqualTo("Ocaso monta uma bateria solar"));
            Assert.That(outra.AnotacoesNpcs, Is.Empty); // presente no arquivo (vazio): substitui
            Assert.That(salas.ImportarAnotacoes(outra.Id, "{\"formato\":\"torre-de-controle/npcs\"}").Ok, Is.False);
        }

        [Test]
        public void Influencia_e_relacionamentos_vao_e_voltam_juntos_so_nas_heroinas_da_campanha()
        {
            var original = FichaPronta.Criada(personagens, "Garça Escarlate");
            salas.PorPersonagem(sala.Id, 0, original.Id, false);
            var garca = personagens.Buscar(sala.Slots[0].Personagem);
            garca.InfluenciaPorqueNinguem = "antes";
            var json = personagens.ExportarVinculos(new[] { garca }, "Campanha Halcyon City");

            var revisado = json.Replace("\"antes\"", "\"ninguém a conhece de verdade\"");
            Assert.That(personagens.ImportarVinculos(revisado, new[] { garca }, out var n).Ok, Is.True);
            Assert.That(n, Is.EqualTo(1));
            Assert.That(garca.InfluenciaPorqueNinguem, Is.EqualTo("ninguém a conhece de verdade"));
            Assert.That(personagens.ImportarVinculos(revisado, new Personagem[0], out _).Ok, Is.False, "ninguém daqui");
        }

        [Test]
        public void Mestre_marca_e_desmarca_o_roteiro_e_fica_gravado()
        {
            Assert.That(salas.AlternarItemDaPrimeiraSessao(sala.Id, "abertura").Ok, Is.True);
            salas.AlternarItemDaPrimeiraSessao(sala.Id, "basico/defender");
            salas.AlternarItemDaPrimeiraSessao(sala.Id, "basico/defender");
            var relida = CadastroSalas.Carregar(armazenamento, personagens).Buscar(sala.Id);
            Assert.That(relida.PrimeiraSessao, Is.EqualTo(new[] { "abertura" }));
        }
    }
}
