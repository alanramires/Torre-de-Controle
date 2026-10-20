using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Salas;

namespace TorreDeControle.Tests
{
    /// <summary>Laboratório de NPCs e cópia entre campanhas (docs/npc_lab.md).</summary>
    public class NpcLabTests
    {
        CadastroPersonagens personagens;
        CadastroSalas salas;
        Sala halcyon, novaEra;

        [SetUp]
        public void Preparar()
        {
            var armazenamento = new ArmazenamentoEmMemoria();
            personagens = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            salas = CadastroSalas.Carregar(armazenamento, personagens);
            salas.Criar("Halcyon City", 5);
            salas.Criar("Nova Era", 5);
            halcyon = salas.Listar().Single(s => s.Nome == "Halcyon City");
            novaEra = salas.Listar().Single(s => s.Nome == "Nova Era");
        }

        [Test]
        public void Npc_do_laboratorio_nao_aparece_entre_as_heroinas()
        {
            FichaPronta.Criada(personagens, "Pulsar");
            Assert.That(personagens.CriarNpc(null, "Lady Faust", Maturidade.Adulto, Papel.Vilao).Ok, Is.True);

            Assert.That(personagens.NpcsDoLaboratorio().Single().NomeExibicao, Is.EqualTo("Lady Faust"));
            Assert.That(personagens.Listar().Select(p => p.NomeExibicao), Is.EqualTo(new[] { "Pulsar" }));
        }

        [Test]
        public void Reboot_leva_o_que_o_npc_e_e_deixa_o_que_aconteceu()
        {
            personagens.CriarNpc(halcyon.Id, "Lady Faust", Maturidade.Adulto, Papel.Vilao);
            var original = personagens.NpcsDaSala(halcyon.Id).Single();
            personagens.DefinirPerfilPnj(original.Id, "cruel", "vingar a linhagem");
            personagens.DefinirMovimentoDeVilao(original.Id, 0, "Abrir um portal sombrio");
            personagens.AlterarTextos(original.Id, "cicatriz no rosto", "esconde que é ancestral da Crepúsculo");
            original.Influencias["alguem"] = EstadoInfluencia.Dada; // o que aconteceu na Halcyon
            original.Pnj.Vinculo = new VinculoNpc { Personagem = "heroina", Extra = "amaldicoada/nemesis", Campo = "nome" };

            var copia = personagens.CopiarNpc(original.Id, novaEra.Id, out var r);
            Assert.That(r.Ok, Is.True);
            Assert.That(copia.Id, Is.Not.EqualTo(original.Id));
            Assert.That(copia.Sala, Is.EqualTo(novaEra.Id));
            Assert.That(personagens.OrigemDaCopia(copia), Is.EqualTo(original));
            // leva
            Assert.That(copia.Papel, Is.EqualTo(Papel.Vilao));
            Assert.That(copia.Pnj.Motivacao, Is.EqualTo("vingar a linhagem"));
            Assert.That(copia.Vilao.Movimentos, Is.EqualTo(new[] { "Abrir um portal sombrio" }));
            Assert.That(copia.Identidade.Resumo, Is.EqualTo("esconde que é ancestral da Crepúsculo")); // notas vão junto
            // deixa
            Assert.That(copia.Influencias, Is.Empty);
            Assert.That(copia.Pnj.Vinculo, Is.Null);
            Assert.That(personagens.OrigemDoNpc(copia), Is.Null); // NPC livre do mestre na campanha nova

            // independente: mexer na cópia não mexe na original
            personagens.DefinirPerfilPnj(copia.Id, "cruel", "conquistar a Nova Era");
            Assert.That(original.Pnj.Motivacao, Is.EqualTo("vingar a linhagem"));
        }

        [Test]
        public void Copia_para_o_laboratorio_e_dele_para_a_sala_com_aviso_de_nome_igual()
        {
            personagens.CriarNpc(halcyon.Id, "Eclipse", Maturidade.Adulto, Papel.Vilao);
            var daSala = personagens.NpcsDaSala(halcyon.Id).Single();

            var noLab = personagens.CopiarNpc(daSala.Id, null, out _);
            Assert.That(personagens.NpcsDoLaboratorio(), Has.Member(noLab));

            Assert.That(personagens.NomeRepetidoNoDestino(noLab, novaEra.Id), Is.False);
            personagens.CopiarNpc(noLab.Id, novaEra.Id, out _);
            Assert.That(personagens.NomeRepetidoNoDestino(noLab, novaEra.Id), Is.True); // pode copiar de novo, com aviso
            personagens.CopiarNpc(noLab.Id, novaEra.Id, out var r);
            Assert.That(r.Ok, Is.True);
            Assert.That(personagens.NpcsDaSala(novaEra.Id).Count, Is.EqualTo(2));
        }

        [Test]
        public void Exportar_os_npcs_da_campanha_e_importar_em_outra_torre()
        {
            personagens.CriarNpc(halcyon.Id, "Lady Faust", Maturidade.Adulto, Papel.Vilao);
            personagens.CriarNpc(halcyon.Id, "Louva-a-Deus", Maturidade.Adulto, Papel.Aliado);
            var faust = personagens.NpcsDaSala(halcyon.Id).First(n => n.NomeExibicao == "Lady Faust");
            personagens.DefinirMovimentoDeVilao(faust.Id, 0, "Abrir um portal sombrio");
            faust.Influencias["alguem"] = EstadoInfluencia.Dada;
            personagens.DefinirAtivo(personagens.NpcsDaSala(halcyon.Id).First(n => n.NomeExibicao == "Louva-a-Deus").Id, false);

            var json = personagens.ExportarNpcs(halcyon.Id, "Campanha Halcyon City");
            Assert.That(json, Does.Contain(CadastroPersonagens.FormatoDeTrocaNpcs));
            Assert.That(personagens.ExportarNpcs(novaEra.Id, "Campanha Nova Era"), Is.Null); // vazia

            // Outra Torre: importa no laboratório dela.
            var outra = CadastroPersonagens.Carregar(new ArmazenamentoEmMemoria(), RegrasDeTeste.Arquetipos());
            Assert.That(outra.ImportarNpcs(json, null, out var novos).Ok, Is.True);
            Assert.That(novos.Count, Is.EqualTo(2));
            var copia = outra.NpcsDoLaboratorio().Single(n => n.NomeExibicao == "Lady Faust");
            Assert.That(copia.Id, Is.Not.EqualTo(faust.Id));
            Assert.That(copia.Vilao.Movimentos, Is.EqualTo(new[] { "Abrir um portal sombrio" }));
            Assert.That(copia.Influencias, Is.Empty);
            Assert.That(copia.Origem, Is.Null);
            Assert.That(outra.NpcsDoLaboratorio().Single(n => n.NomeExibicao == "Louva-a-Deus").Ativo, Is.False); // vem como estava
        }

        [Test]
        public void Lista_revisada_volta_por_cima_dos_mesmos_npcs_sem_duplicar()
        {
            personagens.CriarNpc(halcyon.Id, "Doutor Ocaso", Maturidade.Adulto, Papel.Vilao);
            var ocaso = personagens.NpcsDaSala(halcyon.Id).Single();
            ocaso.Pnj.Vinculo = new VinculoNpc { Personagem = "aurora", Extra = "legado/membros", Campo = "membros" };
            var json = personagens.ExportarNpcs(halcyon.Id, "Campanha Halcyon City");

            // Revisado fora da Torre (outra IA, editor de texto): mesmo id, campos novos.
            var revisado = json.Replace("\"motivacao\": \"\"", "\"motivacao\": \"apagar a luz da cidade\"");
            Assert.That(personagens.ImportarNpcs(revisado, halcyon.Id, out var novos, out var atualizados).Ok, Is.True);
            Assert.That((novos.Count, atualizados.Count), Is.EqualTo((0, 1)));
            Assert.That(personagens.NpcsDaSala(halcyon.Id).Count, Is.EqualTo(1));
            Assert.That(ocaso.Pnj.Motivacao, Is.EqualTo("apagar a luz da cidade"));
            Assert.That(ocaso.Pnj.Vinculo.Extra, Is.EqualTo("legado/membros")); // o laço com a heroína fica

            // Em outro destino o mesmo arquivo continua criando cópias novas.
            Assert.That(personagens.ImportarNpcs(revisado, null, out var noLab, out _).Ok, Is.True);
            Assert.That(noLab.Single().Id, Is.Not.EqualTo(ocaso.Id));
        }

        [Test]
        public void Importar_npcs_recusa_json_de_personagem()
        {
            var heroina = FichaPronta.Criada(personagens, "Pulsar");
            Assert.That(personagens.ImportarNpcs(personagens.Exportar(heroina.Id), null, out _).Ok, Is.False);
            Assert.That(personagens.ImportarNpcs("lixo", null, out _).Ok, Is.False);
        }

        [Test]
        public void Npc_individual_vai_e_volta_por_cima_guardando_o_que_e_da_campanha()
        {
            personagens.CriarNpc(halcyon.Id, "Lady Faust", Maturidade.Adulto, Papel.Vilao);
            var aqui = personagens.NpcsDaSala(halcyon.Id).Single();
            aqui.Influencias["pulsar"] = EstadoInfluencia.Dada;
            aqui.Pnj.Vinculo = new VinculoNpc { Personagem = "estrela", Extra = "amaldicoada/nemesis", Campo = "nome" };
            var json = personagens.ExportarNpc(aqui.Id);

            // Na outra máquina: importa, mexe, exporta de volta.
            var outra = CadastroPersonagens.Carregar(new ArmazenamentoEmMemoria(), RegrasDeTeste.Arquetipos());
            outra.ImportarNpcs(json, null, out var la);
            outra.DefinirPerfilPnj(la[0].Id, "paciente", "abrir o portal");
            outra.DefinirMovimentoDeVilao(la[0].Id, 0, "Possuir um parente");
            var volta = outra.ExportarNpc(la[0].Id);

            Assert.That(personagens.ReimportarNpc(aqui.Id, volta).Ok, Is.True);
            Assert.That(aqui.Pnj.Motivacao, Is.EqualTo("abrir o portal"));
            Assert.That(aqui.Vilao.Movimentos, Is.EqualTo(new[] { "Possuir um parente" }));
            Assert.That(aqui.Sala, Is.EqualTo(halcyon.Id));                         // fica
            Assert.That(aqui.Influencias.ContainsKey("pulsar"), Is.True);           // fica
            Assert.That(aqui.Pnj.Vinculo.Personagem, Is.EqualTo("estrela"));         // fica
            Assert.That(personagens.NpcsDaSala(halcyon.Id).Single(), Is.SameAs(aqui)); // o mesmo objeto, não outro

            // Arquivo com vários NPCs não grava por cima de um.
            personagens.CriarNpc(halcyon.Id, "Louva-a-Deus", Maturidade.Adulto, Papel.Aliado);
            Assert.That(personagens.ReimportarNpc(aqui.Id, personagens.ExportarNpcs(halcyon.Id, "Halcyon")).Ok, Is.False);
        }

        [Test]
        public void Adulto_tem_influencia_por_padrao_e_o_mestre_liga_e_desliga()
        {
            salas.PorPersonagem(halcyon.Id, 0, FichaPronta.Criada(personagens, "Pulsar").Id, false);
            var pulsar = personagens.Buscar(halcyon.Slots[0].Personagem);
            personagens.CriarNpc(halcyon.Id, "Rosa Rook", Maturidade.Adulto, Papel.Vilao);
            personagens.CriarNpc(halcyon.Id, "Marina", Maturidade.Jovem, Papel.Aliado);
            var rosa = personagens.NpcsDaSala(halcyon.Id).Single(n => n.NomeExibicao == "Rosa Rook");
            var marina = personagens.NpcsDaSala(halcyon.Id).Single(n => n.NomeExibicao == "Marina");

            // Adulto: tem por padrão, sem nada gravado. Jovem: não.
            Assert.That(personagens.TemInfluencia(rosa, pulsar), Is.True);
            Assert.That(personagens.InfluenciaPorPadrao(rosa, pulsar), Is.True);
            Assert.That(personagens.TemInfluencia(marina, pulsar), Is.False);
            Assert.That(personagens.TemInfluencia(pulsar, rosa), Is.False);

            // A Pulsar rejeitou e cancelou a da Rosa: fica "perdida", não some.
            Assert.That(personagens.AlternarInfluencia(rosa.Id, pulsar.Id).Ok, Is.True);
            Assert.That(personagens.TemInfluencia(rosa, pulsar), Is.False);
            Assert.That(personagens.PerdeuInfluencia(rosa, pulsar), Is.True);

            // A irmã novinha: a heroína tem Influência sobre ela.
            Assert.That(personagens.AlternarInfluencia(pulsar.Id, marina.Id).Ok, Is.True);
            Assert.That(personagens.TemInfluencia(pulsar, marina), Is.True);
            Assert.That(personagens.InfluenciaPorPadrao(pulsar, marina), Is.False);

            // NPC com NPC não tem; fora da campanha também não.
            Assert.That(personagens.AlternarInfluencia(rosa.Id, marina.Id).Ok, Is.False);
            personagens.CriarNpc(novaEra.Id, "Outra", Maturidade.Adulto, Papel.Neutro);
            Assert.That(personagens.AlternarInfluencia(personagens.NpcsDaSala(novaEra.Id).Single().Id, pulsar.Id).Ok, Is.False);
        }

        [Test]
        public void Clique_errado_na_influencia_se_desfaz_e_volta_ao_que_era()
        {
            salas.PorPersonagem(halcyon.Id, 0, FichaPronta.Criada(personagens, "Toro").Id, false);
            var toro = personagens.Buscar(halcyon.Slots[0].Personagem);
            personagens.CriarNpc(halcyon.Id, "Soldados de Preto", Maturidade.Jovem, Papel.Vilao);
            personagens.CriarNpc(halcyon.Id, "Rosa Rook", Maturidade.Adulto, Papel.Vilao);
            var soldados = personagens.NpcsDaSala(halcyon.Id).Single(n => n.NomeExibicao == "Soldados de Preto");
            var rosa = personagens.NpcsDaSala(halcyon.Id).Single(n => n.NomeExibicao == "Rosa Rook");

            // O caso do Alan: dois cliques no lugar errado deixaram "perdida" da Toro sobre os soldados.
            personagens.AlternarInfluencia(toro.Id, soldados.Id);
            personagens.AlternarInfluencia(toro.Id, soldados.Id);
            Assert.That(personagens.PerdeuInfluencia(toro, soldados), Is.True);
            Assert.That(personagens.TemRegistroDeInfluencia(toro, soldados), Is.True);
            Assert.That(personagens.DesfazerEnganoDeInfluencia(toro.Id, soldados.Id).Ok, Is.True);
            Assert.That(personagens.TemRegistroDeInfluencia(toro, soldados), Is.False);
            Assert.That(personagens.PerdeuInfluencia(toro, soldados), Is.False, "nunca houve");
            Assert.That(personagens.DesfazerEnganoDeInfluencia(toro.Id, soldados.Id).Ok, Is.False, "nada mais a desfazer");

            // Adulto: desfazer o clique devolve a Influência por padrão.
            personagens.AlternarInfluencia(rosa.Id, toro.Id);
            Assert.That(personagens.TemInfluencia(rosa, toro), Is.False);
            personagens.DesfazerEnganoDeInfluencia(rosa.Id, toro.Id);
            Assert.That(personagens.TemInfluencia(rosa, toro), Is.True);
            Assert.That(personagens.InfluenciaPorPadrao(rosa, toro), Is.True);
        }

        [Test]
        public void Heroina_nao_se_copia_como_npc()
        {
            var heroina = FichaPronta.Criada(personagens, "Pulsar");
            personagens.CopiarNpc(heroina.Id, novaEra.Id, out var r);
            Assert.That(r.Ok, Is.False);
        }
    }
}
