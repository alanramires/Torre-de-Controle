using System.Linq;
using NUnit.Framework;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Salas;

namespace TorreDeControle.Tests
{
    public class InfluenciaTests
    {
        CadastroPersonagens personagens;
        CadastroSalas salas;
        Sala sala;
        Personagem[] heroinas;

        [SetUp]
        public void Preparar()
        {
            var armazenamento = new ArmazenamentoEmMemoria();
            personagens = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            salas = CadastroSalas.Carregar(armazenamento, personagens);
            salas.Criar("Nova Era", 6);
            sala = salas.Listar().Single();
            heroinas = new Personagem[4];
            for (var k = 0; k < 4; k++)
            {
                var original = FichaPronta.Criada(personagens, "H" + k);
                salas.PorPersonagem(sala.Id, k, original.Id, false);
                heroinas[k] = personagens.Buscar(sala.Slots[k].Personagem);
            }
        }

        /// <summary>Atalho de teste: a ficha pronta é uma Nova; troca o arquétipo da foto para outra regra.</summary>
        void ComoArquetipo(Personagem p, string arquetipo) => p.Foto.Arquetipo = arquetipo;

        string[] Colegas(Personagem p, int quantos) =>
            personagens.Colegas(p).Take(quantos).Select(c => c.Id).ToArray();

        [Test]
        public void Comportamento_define_a_quantidade_e_trocar_desfaz()
        {
            var nova = heroinas[0]; // Nova: fachada feliz (3) ou cara fechada (1)
            Assert.That(personagens.DarInfluencia(nova.Id, Colegas(nova, 3)).Ok, Is.False); // sem comportamento
            personagens.EscolherComportamento(nova.Id, "fachada_feliz");
            Assert.That(personagens.DarInfluencia(nova.Id, Colegas(nova, 2)).Ok, Is.False);
            Assert.That(personagens.DarInfluencia(nova.Id, Colegas(nova, 3)).Ok, Is.True);
            Assert.That(personagens.InfluenciaDada(nova), Has.Count.EqualTo(3));

            personagens.EscolherComportamento(nova.Id, "cara_fechada");
            Assert.That(personagens.InfluenciaDada(nova), Is.Empty);
            Assert.That(personagens.QuantasInfluencias(nova), Is.EqualTo(1));
        }

        [Test]
        public void Fixo_pede_exatamente_a_quantidade_do_arquetipo()
        {
            var amaldicoada = heroinas[1];
            ComoArquetipo(amaldicoada, "amaldicoada"); // 2 colegas
            Assert.That(personagens.QuantasInfluencias(amaldicoada), Is.EqualTo(2));
            Assert.That(personagens.DarInfluencia(amaldicoada.Id, Colegas(amaldicoada, 3)).Ok, Is.False);
            Assert.That(personagens.DarInfluencia(amaldicoada.Id, Colegas(amaldicoada, 2)).Ok, Is.True);
            Assert.That(personagens.InfluenciaConfirmada(amaldicoada), Is.True);
        }

        [Test]
        public void Todos_e_todos_os_colegas_confirmado_pelo_jogador()
        {
            var legado = heroinas[2];
            ComoArquetipo(legado, "legado");
            Assert.That(personagens.InfluenciaParaTodos(legado), Is.True);
            Assert.That(personagens.InfluenciaDada(legado), Is.Empty); // ainda não confirmou
            Assert.That(personagens.DarInfluencia(legado.Id, Colegas(legado, 2)).Ok, Is.False);
            Assert.That(personagens.DarInfluencia(legado.Id, Colegas(legado, 3)).Ok, Is.True);
        }

        [Test]
        public void Arrogante_nao_da_a_ninguem_e_isso_conta_como_confirmado()
        {
            var forasteira = heroinas[3];
            ComoArquetipo(forasteira, "forasteira");
            Assert.That(personagens.InfluenciaConfirmada(forasteira), Is.False); // sem comportamento
            personagens.EscolherComportamento(forasteira.Id, "arrogante");
            Assert.That(personagens.DarInfluencia(forasteira.Id, new string[0]).Ok, Is.True);
            Assert.That(personagens.InfluenciaConfirmada(forasteira), Is.True);
            // E o porquê de ninguém (roleplay).
            Assert.That(personagens.DescreverInfluenciaDeNinguem(forasteira.Id, "  Eles ainda não provaram nada. ").Ok, Is.True);
            Assert.That(forasteira.InfluenciaPorqueNinguem, Is.EqualTo("Eles ainda não provaram nada."));
            Assert.That(forasteira.InfluenciaPorque, Is.Empty); // nada de colega falso na tabela dos colegas
        }

        [Test]
        public void Leitura_reversa_quem_me_deu_influencia()
        {
            var a = heroinas[0];
            var b = heroinas[1];
            ComoArquetipo(b, "amaldicoada");
            personagens.DarInfluencia(b.Id, new[] { a.Id, heroinas[2].Id });
            Assert.That(personagens.QuemDeuInfluencia(a), Has.Member(b));
            Assert.That(personagens.QuemDeuInfluencia(heroinas[3]), Has.No.Member(b));
        }

        [Test]
        public void Porque_da_influencia_so_para_quem_recebeu_e_vale_depois_de_aprovada()
        {
            var a = heroinas[0];
            personagens.EscolherComportamento(a.Id, "cara_fechada");
            var colega = personagens.Colegas(a).First().Id;
            var outro = personagens.Colegas(a).Last().Id;
            personagens.DarInfluencia(a.Id, new[] { colega });

            Assert.That(personagens.DescreverInfluencia(a.Id, outro, "não recebeu").Ok, Is.False);
            Assert.That(personagens.DescreverInfluencia(a.Id, colega, "  me salvou no Helix  ").Ok, Is.True);
            Assert.That(personagens.PorqueDaInfluencia(a, colega), Is.EqualTo("me salvou no Helix"));

            FichaPronta.ParaCarreira(salas, personagens, sala, a); // anotação: segue editável em carreira
            Assert.That(personagens.DescreverInfluencia(a.Id, colega, "").Ok, Is.True);
            Assert.That(personagens.PorqueDaInfluencia(a, colega), Is.Empty);
        }

        [Test]
        public void No_jogo_a_influencia_vira_perdida_e_a_reversa_le_o_estado()
        {
            var a = heroinas[0];
            var b = heroinas[1];
            ComoArquetipo(b, "amaldicoada");
            personagens.DarInfluencia(b.Id, new[] { a.Id, heroinas[2].Id });
            personagens.DescreverInfluencia(b.Id, a.Id, "a Nova me tirou dos escombros");
            Assert.That(personagens.DefinirEstadoDaInfluencia(b.Id, a.Id, EstadoInfluencia.Perdida).Ok, Is.False); // ainda em aprovação

            FichaPronta.ParaCarreira(salas, personagens, sala, b);
            Assert.That(personagens.DefinirEstadoDaInfluencia(b.Id, a.Id, EstadoInfluencia.Perdida).Ok, Is.True);
            Assert.That(personagens.QuemDeuInfluencia(a), Has.No.Member(b));
            Assert.That(personagens.QuemPerdeuInfluencia(a), Has.Member(b));
            Assert.That(personagens.InfluenciaPerdida(b), Has.Member(a.Id));
            Assert.That(personagens.PorqueDaInfluencia(b, a.Id), Is.EqualTo("a Nova me tirou dos escombros")); // o texto fica

            // Pode voltar.
            personagens.DefinirEstadoDaInfluencia(b.Id, a.Id, EstadoInfluencia.Dada);
            Assert.That(personagens.QuemDeuInfluencia(a), Has.Member(b));
        }

        [Test]
        public void No_jogo_da_influencia_nova_mesmo_quem_comecou_sem_dar()
        {
            var f = heroinas[3];
            ComoArquetipo(f, "forasteira");
            personagens.EscolherComportamento(f.Id, "arrogante"); // a ninguém
            personagens.DarInfluencia(f.Id, new string[0]);
            personagens.DescreverInfluenciaDeNinguem(f.Id, "ninguém provou nada");
            FichaPronta.ParaCarreira(salas, personagens, sala, f);

            // Começou a gostar de alguém.
            var colega = heroinas[0].Id;
            Assert.That(personagens.DefinirEstadoDaInfluencia(f.Id, colega, EstadoInfluencia.Dada).Ok, Is.True);
            Assert.That(personagens.InfluenciaDada(f), Has.Member(colega));
            Assert.That(f.InfluenciaPorqueNinguem, Is.EqualTo("ninguém provou nada"));
            // Perder o que nunca deu: não.
            Assert.That(personagens.DefinirEstadoDaInfluencia(f.Id, heroinas[1].Id, EstadoInfluencia.Perdida).Ok, Is.False);
        }

        [Test]
        public void Desmarcar_na_criacao_nao_e_perder_e_o_porque_fica()
        {
            var b = heroinas[1];
            ComoArquetipo(b, "amaldicoada");
            var c1 = heroinas[0].Id;
            var c2 = heroinas[2].Id;
            var c3 = heroinas[3].Id;
            personagens.DarInfluencia(b.Id, new[] { c1, c2 });
            personagens.DescreverInfluencia(b.Id, c1, "porque sim");
            personagens.DarInfluencia(b.Id, new[] { c2, c3 }); // trocou c1 por c3, ainda na criação

            Assert.That(personagens.InfluenciaPerdida(b), Is.Empty);
            Assert.That(personagens.InfluenciaDada(b), Is.EquivalentTo(new[] { c2, c3 }));
            Assert.That(personagens.PorqueDaInfluencia(b, c1), Is.EqualTo("porque sim"));
        }

        [Test]
        public void Formato_antigo_da_lista_vira_dada_ao_carregar()
        {
            var armazenamento = new ArmazenamentoEmMemoria();
            var cadastro = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            var p = FichaPronta.Criada(cadastro, "Antiga");
            armazenamento.Gravar(CadastroPersonagens.Chave,
                armazenamento.Ler(CadastroPersonagens.Chave).Replace("\"influencias\": {}", "\"influencia_para\": [\"c1\", \"c2\"]"));

            var relido = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos()).Buscar(p.Id);
            Assert.That(relido.Influencias["c1"], Is.EqualTo(EstadoInfluencia.Dada));
            Assert.That(relido.Influencias["c2"], Is.EqualTo(EstadoInfluencia.Dada));
            Assert.That(relido.InfluenciaParaAntiga, Is.Null);
        }

        [Test]
        public void So_em_aprovacao_e_so_colegas_da_sala()
        {
            var a = heroinas[0];
            personagens.EscolherComportamento(a.Id, "cara_fechada");
            Assert.That(personagens.DarInfluencia(a.Id, new[] { "alguem-de-fora" }).Ok, Is.False);
            FichaPronta.ParaCarreira(salas, personagens, sala, a); // em carreira: Influência muda pelo jogo, não aqui
            Assert.That(personagens.DarInfluencia(a.Id, Colegas(a, 1)).Ok, Is.False);
        }
    }
}
