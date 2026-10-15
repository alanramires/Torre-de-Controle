using System.Linq;
using NUnit.Framework;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Salas;

namespace TorreDeControle.Tests
{
    public class RelacionamentosTests
    {
        ArmazenamentoEmMemoria armazenamento;
        CadastroPersonagens personagens;
        CadastroSalas salas;
        Sala sala;
        Personagem[] heroinas;

        [SetUp]
        public void Preparar()
        {
            armazenamento = new ArmazenamentoEmMemoria();
            personagens = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            salas = CadastroSalas.Carregar(armazenamento, personagens);
            salas.Criar("Nova Era", 6);
            sala = salas.Listar().Single();
            heroinas = new Personagem[3];
            for (var k = 0; k < 3; k++)
            {
                salas.PorPersonagem(sala.Id, k, FichaPronta.Criada(personagens, "H" + k).Id, false);
                heroinas[k] = personagens.Buscar(sala.Slots[k].Personagem);
            }
        }

        [Test]
        public void Escolher_na_criacao_e_a_leitura_reversa()
        {
            var a = heroinas[0];
            Assert.That(personagens.DefinirRelacionamento(a.Id, 0, heroinas[1].Id).Ok, Is.True);
            Assert.That(personagens.DefinirRelacionamento(a.Id, 1, heroinas[2].Id).Ok, Is.True);
            Assert.That(personagens.DefinirRelacionamento(a.Id, 5, heroinas[1].Id).Ok, Is.False);     // frase que não existe
            Assert.That(personagens.DefinirRelacionamento(a.Id, 0, "alguem-de-fora").Ok, Is.False);

            var citacoes = personagens.CitacoesEmRelacionamentos(heroinas[1]);
            Assert.That(citacoes.Single().colega, Is.EqualTo(a));
            Assert.That(citacoes.Single().frase, Is.EqualTo(0));
        }

        [Test]
        public void Repetido_pode_mas_vira_observacao()
        {
            var a = heroinas[0];
            personagens.DefinirRelacionamento(a.Id, 0, heroinas[1].Id);
            personagens.DefinirRelacionamento(a.Id, 1, heroinas[1].Id);
            Assert.That(personagens.ObservacoesDaEquipe(a), Has.Some.Contains("mais de uma frase"));
            Assert.That(personagens.PendenciasParaCarreira(a), Has.None.Contains("Relacionamentos"));
        }

        [Test]
        public void No_jogo_trocar_e_perder_guardam_o_historico()
        {
            var a = heroinas[0];
            FichaPronta.ParaCarreira(salas, personagens, sala, a);
            var antes = personagens.ColegaDoRelacionamento(a, 0).Id;
            var outro = personagens.Colegas(a).First(c => c.Id != antes).Id;
            personagens.DescreverRelacionamento(a.Id, 0, "vivemos juntas o Helix");

            Assert.That(personagens.DefinirRelacionamento(a.Id, 0, outro).Ok, Is.False); // em carreira: é do jogo
            Assert.That(personagens.TrocarRelacionamento(a.Id, 0, outro).Ok, Is.True);
            Assert.That(personagens.ColegaDoRelacionamento(a, 0).Id, Is.EqualTo(outro));
            Assert.That(a.Relacoes[0].Anteriores, Is.EqualTo(new[] { antes }));

            Assert.That(personagens.TrocarRelacionamento(a.Id, 0, null).Ok, Is.True); // perdeu
            Assert.That(personagens.ColegaDoRelacionamento(a, 0), Is.Null);
            Assert.That(a.Relacoes[0].Anteriores, Is.EqualTo(new[] { antes, outro }));
            Assert.That(a.Relacoes[0].Porque, Is.EqualTo("vivemos juntas o Helix")); // o texto fica
        }

        [Test]
        public void Bruta_influencia_primeiro_filtra_amor_e_rival()
        {
            var bruta = ComoBrutaComQuatroColegas();
            var (c1, c2, c3) = (heroinas[1].Id, heroinas[2].Id, heroinas[3].Id);
            personagens.DarInfluencia(bruta.Id, new[] { c1, c2 });
            Assert.That(personagens.OpcoesDeRelacionamento(bruta).Select(c => c.Id), Is.EquivalentTo(new[] { c1, c2 }));
            Assert.That(personagens.DefinirRelacionamento(bruta.Id, 0, c3).Ok, Is.False); // fora da Influência
            Assert.That(personagens.DefinirRelacionamento(bruta.Id, 0, c1).Ok, Is.True);  // amor
            Assert.That(personagens.DefinirRelacionamento(bruta.Id, 1, c2).Ok, Is.True);  // rival

            // Refez a Influência sem o c2: o rival que apontava para ele fica vazio.
            personagens.DarInfluencia(bruta.Id, new[] { c1, c3 });
            Assert.That(personagens.ColegaDoRelacionamento(bruta, 0).Id, Is.EqualTo(c1));
            Assert.That(personagens.ColegaDoRelacionamento(bruta, 1), Is.Null);
        }

        [Test]
        public void Bruta_amor_e_rival_primeiro_viram_a_influencia()
        {
            var bruta = ComoBrutaComQuatroColegas();
            var (c1, c2) = (heroinas[1].Id, heroinas[2].Id);
            Assert.That(personagens.DefinirRelacionamento(bruta.Id, 0, c1).Ok, Is.True); // amor, sem Influência ainda
            Assert.That(personagens.InfluenciaDada(bruta), Is.EquivalentTo(new[] { c1 }));
            Assert.That(personagens.DefinirRelacionamento(bruta.Id, 1, c2).Ok, Is.True); // rival
            Assert.That(personagens.InfluenciaDada(bruta), Is.EquivalentTo(new[] { c1, c2 }));
            Assert.That(personagens.InfluenciaConfirmada(bruta), Is.True);
        }

        /// <summary>A primeira heroína como Bruta, com mais uma colega na sala (para sobrar alguém fora da Influência).</summary>
        Personagem ComoBrutaComQuatroColegas()
        {
            salas.PorPersonagem(sala.Id, 3, FichaPronta.Criada(personagens, "H3").Id, false);
            heroinas = heroinas.Concat(new[] { personagens.Buscar(sala.Slots[3].Personagem) }).ToArray();
            var bruta = heroinas[0];
            bruta.Foto.Arquetipo = "bruta"; // atalho de teste: a ficha pronta é uma Nova
            return bruta;
        }

        [Test]
        public void Formato_antigo_da_lista_vira_relacoes_ao_carregar()
        {
            var a = heroinas[0];
            armazenamento.Gravar(CadastroPersonagens.Chave,
                armazenamento.Ler(CadastroPersonagens.Chave).Replace("\"relacoes\": []", "\"relacionamentos\": [\"x1\", null]"));
            var relida = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos()).Buscar(a.Id);
            Assert.That(relida.Relacoes[0].Colega, Is.EqualTo("x1"));
            Assert.That(relida.Relacoes[1].Colega, Is.Null);
            Assert.That(relida.RelacionamentosAntigos, Is.Null);
        }
    }
}
