using System.Linq;
using NUnit.Framework;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Salas;

namespace TorreDeControle.Tests
{
    /// <summary>Cenário da campanha: locais, organizações e outros; lugar não é gente, então não é NPC.</summary>
    public class CenarioTests
    {
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

        [Test]
        public void Mestre_cria_edita_e_inativa_e_tudo_fica_gravado()
        {
            Assert.That(personagens.CriarElemento(sala.Id, "  ", ElementoDeCenario.Local).Ok, Is.False);
            Assert.That(personagens.CriarElemento(sala.Id, "AEGIS", ElementoDeCenario.Organizacao).Ok, Is.True);
            var aegis = personagens.CenarioDaSala(sala.Id).Single();
            Assert.That(personagens.AlterarElemento(aegis.Id, "A.E.G.I.S.", ElementoDeCenario.Organizacao, " Agência do governo. ").Ok, Is.True);
            Assert.That(personagens.DefinirAtivoElemento(aegis.Id, false).Ok, Is.True);

            var relido = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos()).CenarioDaSala(sala.Id).Single();
            Assert.That(relido.Nome, Is.EqualTo("A.E.G.I.S."));
            Assert.That(relido.Tipo, Is.EqualTo(ElementoDeCenario.Organizacao));
            Assert.That(relido.Descricao, Is.EqualTo("Agência do governo."));
            Assert.That(relido.Ativo, Is.False);
        }

        [Test]
        public void Local_destruido_da_nova_vira_cenario_e_nao_npc()
        {
            var original = FichaPronta.Criada(personagens, "Aurora Dourada"); // a ficha pronta é uma Nova
            Assert.That(salas.PorPersonagem(sala.Id, 0, original.Id, false).Ok, Is.True);
            var copia = personagens.Buscar(sala.Slots[0].Personagem);
            Assert.That(personagens.OQueAFormacaoCria(copia), Is.EqualTo("Cenário"));
            Assert.That(salas.EnviarFormacao(sala.Id, copia.Id, "Derrubamos a ponte velha sem querer.").Ok, Is.True);
            Assert.That(personagens.DefinirNomeNpcDaFormacao(copia.Id, "Ponte Velha").Ok, Is.True);

            Assert.That(personagens.NpcDaFicha(copia, CadastroPersonagens.FonteFormacao), Is.Null);
            Assert.That(personagens.NpcsDaSala(sala.Id).Any(n => n.NomeExibicao == "Ponte Velha"), Is.False);
            var ponte = personagens.CenarioDaSala(sala.Id).Single();
            Assert.That(ponte.Nome, Is.EqualTo("Ponte Velha"));
            Assert.That(ponte.Tipo, Is.EqualTo(ElementoDeCenario.Local));
            Assert.That(ponte.Descricao, Does.Contain("ponte velha")); // cópia da resposta, uma vez
            Assert.That(personagens.OrigemDoElemento(ponte).Value.dono, Is.SameAs(copia));

            // Cópia, não espelho: a história dela muda, a descrição do mestre fica.
            salas.EnviarFormacao(sala.Id, copia.Id, "Outra versão.");
            Assert.That(ponte.Descricao, Does.Contain("ponte velha"));

            // Nome apagado: inativa (nada se exclui); nome de volta: reativa o mesmo.
            personagens.DefinirNomeNpcDaFormacao(copia.Id, "");
            Assert.That(ponte.Ativo, Is.False);
            personagens.DefinirNomeNpcDaFormacao(copia.Id, "Ponte Nova");
            Assert.That(personagens.CenarioDaSala(sala.Id).Single(), Is.SameAs(ponte));
            Assert.That(ponte.Ativo && ponte.Nome == "Ponte Nova", Is.True);
        }

        [Test]
        public void Exportar_e_importar_leva_a_cidade_sem_duplicar()
        {
            personagens.CriarElemento(sala.Id, "AEGIS", ElementoDeCenario.Organizacao);
            personagens.CriarElemento(sala.Id, "Docas", ElementoDeCenario.Local);
            var docas = personagens.CenarioDaSala(sala.Id).Single(e => e.Nome == "Docas");
            personagens.AlterarElemento(docas.Id, "Docas", ElementoDeCenario.Local, "Contrabando à noite.");
            var json = personagens.ExportarCenario(sala.Id, "Campanha Nova Era");
            Assert.That(CadastroPersonagens.FormatoDaTroca(json), Is.EqualTo(CadastroPersonagens.FormatoDeTrocaCenario));

            salas.Criar("Reboot", 6);
            var outra = salas.Listar().Single(s => s.Nome == "Reboot");
            personagens.CriarElemento(outra.Id, "aegis", ElementoDeCenario.Organizacao);
            Assert.That(personagens.ImportarCenario(json, outra.Id, out var novos, out var repetidos).Ok, Is.True);
            Assert.That((novos, repetidos), Is.EqualTo((1, 1)));
            var lidas = personagens.CenarioDaSala(outra.Id).Single(e => e.Nome == "Docas");
            Assert.That(lidas.Descricao, Is.EqualTo("Contrabando à noite."));
            Assert.That(lidas.Id, Is.Not.EqualTo(docas.Id));

            Assert.That(personagens.ImportarCenario("{\"formato\":\"torre-de-controle/npcs\"}", outra.Id, out _, out _).Ok, Is.False);
        }

        /// <summary>Uma ficha pronta do laboratório trocada de arquétipo (atalho de teste), ainda em criação.</summary>
        Personagem DoArquetipo(string arquetipo, string codinome)
        {
            var p = FichaPronta.Criada(personagens, codinome);
            Assert.That(personagens.ReabrirCriacao(p.Id).Ok, Is.True);
            p.Identidade.Arquetipo = arquetipo;
            return p;
        }

        Personagem NaSala(Personagem original)
        {
            original.Status = StatusCriacao.EmAprovacao; // atalho: criação concluída
            return personagens.CopiarParaSala(original.Id, sala.Id, null, false, out _);
        }

        [Test]
        public void Santuario_da_amaldicoada_nasce_local_com_as_escolhas_na_descricao()
        {
            var original = DoArquetipo("amaldicoada", "Crepúsculo");
            const string extra = "amaldicoada/santuario";
            personagens.AlternarOpcaoDeExtra(original.Id, extra, "caracteristicas", "trancas-e-armadilhas");
            personagens.DefinirTextoDeExtra(original.Id, extra, "caracteristicas", "trancas-e-armadilhas", "portão de ferro");
            personagens.AlternarOpcaoDeExtra(original.Id, extra, "caracteristicas", "reliquias-antigas");
            Assert.That(personagens.CenarioDaSala(sala.Id), Is.Empty); // no laboratório, nada

            var copia = NaSala(original);
            var santuario = personagens.CenarioDaSala(sala.Id).Single();
            Assert.That(santuario.Nome, Is.EqualTo("Santuário de Crepúsculo"));
            Assert.That(santuario.Tipo, Is.EqualTo(ElementoDeCenario.Local));
            Assert.That(santuario.Descricao, Does.Contain("Trancas e armadilhas (portão de ferro)"));
            Assert.That(santuario.Descricao, Does.Contain("Relíquias antigas"));
            Assert.That(personagens.OrigemDoElemento(santuario).Value.dono, Is.SameAs(copia));

            // Nome automático é do mestre: renomeia e a ficha não desfaz.
            Assert.That(personagens.NomeDoElementoVemDaFicha(santuario), Is.False);
            personagens.AlterarElemento(santuario.Id, "Torre do Corvo", santuario.Tipo, santuario.Descricao);
            personagens.DefinirTextoDeExtra(copia.Id, extra, "caracteristicas", "trancas-e-armadilhas", "portão de aço");
            Assert.That(santuario.Nome, Is.EqualTo("Torre do Corvo"));

            // Nome escrito na ficha (opcional) manda; apagado, volta o automático.
            var lugar = personagens.NomeDeCenarioDoExtra(copia, extra).Value;
            Assert.That(personagens.DefinirNomeDoCenarioNaFicha(copia.Id, extra, lugar.indice, "Cripta de Vesper").Ok, Is.True);
            Assert.That(santuario.Nome, Is.EqualTo("Cripta de Vesper"));
            Assert.That(personagens.NomeDoElementoVemDaFicha(santuario), Is.True);
            personagens.DefinirNomeDoCenarioNaFicha(copia.Id, extra, lugar.indice, "");
            Assert.That(santuario.Nome, Is.EqualTo("Santuário de Crepúsculo"));
            Assert.That(personagens.CenarioDaSala(sala.Id).Count, Is.EqualTo(1));
        }

        [Test]
        public void Base_secreta_da_discipula_so_nasce_se_marcada()
        {
            var sem = DoArquetipo("discipula", "Aprendiz");
            personagens.AlternarOpcaoDeExtra(sem.Id, "discipula/recursos", "recursos", "veiculo");
            NaSala(sem);
            Assert.That(personagens.CenarioDaSala(sala.Id), Is.Empty);

            var com = DoArquetipo("discipula", "Pupila");
            personagens.AlternarOpcaoDeExtra(com.Id, "discipula/recursos", "recursos", "base-secreta");
            personagens.DefinirTextoDeExtra(com.Id, "discipula/recursos", "recursos", "base-secreta", "embaixo do museu");
            NaSala(com);
            var base_ = personagens.CenarioDaSala(sala.Id).Single();
            Assert.That(base_.Nome, Is.EqualTo("Base secreta de Pupila"));
            Assert.That(base_.Descricao, Is.EqualTo("embaixo do museu"));
        }

        [Test]
        public void De_onde_a_forasteira_vem_vira_local_e_nao_npc()
        {
            var original = DoArquetipo("forasteira", "Zyra");
            personagens.ResponderHistorico(original.Id, 0, "Um planeta oceânico que orbita duas estrelas.");
            personagens.DefinirNomeNoHistorico(original.Id, 0, "Thalassa");
            NaSala(original);
            Assert.That(personagens.NpcsDaSala(sala.Id).Any(n => n.NomeExibicao == "Thalassa"), Is.False);
            var planeta = personagens.CenarioDaSala(sala.Id).Single();
            Assert.That(planeta.Nome, Is.EqualTo("Thalassa"));
            Assert.That(planeta.Descricao, Does.Contain("duas estrelas"));
            Assert.That(personagens.NomeDoElementoVemDaFicha(planeta), Is.True);
        }

        Personagem ComNave(string codinome, string nomeDaNave, bool telepatica)
        {
            const string nave = "mov:nave-a-kirby";
            var p = DoArquetipo("forasteira", codinome);
            Assert.That(personagens.AlternarMovimento(p.Id, "nave-a-kirby").Ok, Is.True);
            personagens.DefinirTextoDeExtra(p.Id, nave, "nave", null, nomeDaNave);
            personagens.AlternarOpcaoDeExtra(p.Id, nave, "forcas", "rapida");
            if (telepatica)
                personagens.AlternarOpcaoDeExtra(p.Id, nave, "forcas", "telepatica");
            return NaSala(p);
        }

        [Test]
        public void Nave_kirby_e_npc_so_se_for_telepatica()
        {
            ComNave("Zyra", "Estrela d'Alva", telepatica: false);
            Assert.That(personagens.NpcsDaSala(sala.Id).Any(n => n.NomeExibicao == "Estrela d'Alva"), Is.False);
            var objeto = personagens.CenarioDaSala(sala.Id).Single(e => e.Nome == "Estrela d'Alva");
            Assert.That(objeto.Tipo, Is.EqualTo(ElementoDeCenario.Outro));

            ComNave("Kael", "Mente-Viva", telepatica: true);
            Assert.That(personagens.NpcsDaSala(sala.Id).Single(n => n.NomeExibicao == "Mente-Viva").Papel, Is.EqualTo(Papel.Aliado));
            Assert.That(personagens.CenarioDaSala(sala.Id).Any(e => e.Nome == "Mente-Viva"), Is.False);
        }

        [Test]
        public void Nome_de_cenario_igual_ao_de_npc_nao_avisa()
        {
            var original = FichaPronta.Criada(personagens, "Aurora Dourada");
            salas.PorPersonagem(sala.Id, 0, original.Id, false);
            var copia = personagens.Buscar(sala.Slots[0].Personagem);
            personagens.CriarNpc(sala.Id, "Arsenal", Maturidade.Adulto, Papel.Aliado);
            Assert.That(personagens.AvisoDeNpcRepetido(copia, CadastroPersonagens.FonteFormacao, "resposta", "Arsenal"), Is.Null);
        }
    }
}
