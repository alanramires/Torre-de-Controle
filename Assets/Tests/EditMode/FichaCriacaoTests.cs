using System.Linq;
using NUnit.Framework;
using TorreDeControle.Core.Personagens;

namespace TorreDeControle.Tests
{
    public class FichaCriacaoTests
    {
        ArmazenamentoEmMemoria armazenamento;
        CadastroPersonagens cadastro;

        [SetUp]
        public void Preparar()
        {
            armazenamento = new ArmazenamentoEmMemoria();
            cadastro = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
        }

        Personagem Nova(string arquetipo)
        {
            cadastro.Criar("Teste", "", arquetipo, null);
            return cadastro.Listar().Last();
        }

        [Test]
        public void Todos_os_arquetipos_tem_regras_e_somam_mais_dois()
        {
            var regras = RegrasDeTeste.Arquetipos();
            foreach (var a in Arquetipos.Todos)
            {
                var regra = regras.Buscar(a.Id);
                Assert.That(regra, Is.Not.Null, a.Id);
                Assert.That(regra.Rotulos.Values.Sum(), Is.EqualTo(2), a.Id);
            }
        }

        [Test]
        public void Farol_comeca_como_a_ficha_do_Max()
        {
            var p = Nova("farol");
            var r = cadastro.RotulosAtuais(p);
            Assert.That(r[Rotulos.Perigoso], Is.EqualTo(-1));
            Assert.That(r[Rotulos.Aberracao], Is.EqualTo(-1));
            Assert.That(r[Rotulos.Salvador], Is.EqualTo(2));
            Assert.That(r[Rotulos.Superior], Is.EqualTo(0));
            Assert.That(r[Rotulos.Mundano], Is.EqualTo(2));
        }

        [Test]
        public void Bonus_livre_soma_um_e_pode_trocar_de_lugar()
        {
            var p = Nova("legado");
            Assert.That(cadastro.EscolherBonusLivre(p.Id, Rotulos.Superior).Ok, Is.True);
            Assert.That(cadastro.RotulosAtuais(p)[Rotulos.Superior], Is.EqualTo(1));

            cadastro.EscolherBonusLivre(p.Id, Rotulos.Mundano);
            Assert.That(cadastro.RotulosAtuais(p)[Rotulos.Superior], Is.EqualTo(0));
            Assert.That(cadastro.RotulosAtuais(p)[Rotulos.Mundano], Is.EqualTo(2));
        }

        [Test]
        public void Bonus_livre_nao_passa_de_mais_tres()
        {
            var janus = Nova("janus");
            Assert.That(cadastro.EscolherBonusLivre(janus.Id, Rotulos.Mundano).Ok, Is.False);
            var transformado = Nova("transformado");
            Assert.That(cadastro.EscolherBonusLivre(transformado.Id, Rotulos.Aberracao).Ok, Is.False);
        }

        [Test]
        public void Trocar_de_arquetipo_em_novo_reseta_a_criacao()
        {
            var p = Nova("farol");
            cadastro.EscolherBonusLivre(p.Id, Rotulos.Superior);
            cadastro.AlterarIdentidade(p.Id, "Teste", "", "nova", null);
            Assert.That(p.Criacao.BonusLivre, Is.Null);
            Assert.That(cadastro.RotulosAtuais(p)[Rotulos.Aberracao], Is.EqualTo(2));
        }

        [Test]
        public void Com_pendencia_nao_aprova()
        {
            var p = Nova("bruta");
            var resultado = cadastro.Aprovar(p.Id);
            Assert.That(resultado.Ok, Is.False);
            StringAssert.Contains("Falta escolher o +1 livre.", resultado.Erro);
            Assert.That(p.Status, Is.EqualTo(StatusCriacao.Novo));
            Assert.That(p.Foto, Is.Null);
        }

        [Test]
        public void Observacao_nao_trava_aprovar()
        {
            var p = Nova("amaldicoada");
            cadastro.EscolherBonusLivre(p.Id, Rotulos.Mundano);
            cadastro.AlternarHabilidade(p.Id, "telecinese");
            for (var k = 0; k < 5; k++)
                cadastro.ResponderHistorico(p.Id, k, "resposta " + k);
            Assert.That(cadastro.Observacoes(p), Has.Some.Contains("1 de até 3"));
            Assert.That(cadastro.Aprovar(p.Id).Ok, Is.True);
        }

        [Test]
        public void Aprovar_congela_a_foto_e_trava_o_bonus_e_o_arquetipo()
        {
            var p = Nova("nova");
            cadastro.EscolherBonusLivre(p.Id, Rotulos.Superior);
            cadastro.AlternarHabilidade(p.Id, "feiticaria");
            for (var k = 0; k < 5; k++)
                cadastro.ResponderHistorico(p.Id, k, "resposta " + k);
            Assert.That(cadastro.Aprovar(p.Id).Ok, Is.True);
            Assert.That(p.Foto.Habilidades.Single().Nome, Is.EqualTo("Feitiçaria"));

            Assert.That(p.Status, Is.EqualTo(StatusCriacao.Criado));
            Assert.That(p.Foto.Rotulos[Rotulos.Superior], Is.EqualTo(1));
            Assert.That(cadastro.EscolherBonusLivre(p.Id, Rotulos.Perigoso).Ok, Is.False);
            Assert.That(cadastro.AlterarIdentidade(p.Id, "Teste", "", "farol", null).Ok, Is.False);

            var recarregada = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos()).Buscar(p.Id);
            Assert.That(recarregada.Foto.Rotulos[Rotulos.Superior], Is.EqualTo(1));
            Assert.That(recarregada.Status, Is.EqualTo(StatusCriacao.Criado));
        }
    

        // ---------- habilidades ----------

        [Test]
        public void Lista_escolhe_ate_o_maximo_e_cobra_o_que_falta()
        {
            var p = Nova("farol");
            Assert.That(cadastro.AlternarHabilidade(p.Id, "espadas").Ok, Is.True);
            Assert.That(cadastro.Validar(p).avisos, Has.Some.Contains("faltam 1"));
            Assert.That(cadastro.AlternarHabilidade(p.Id, "acrobacia").Ok, Is.True);
            Assert.That(cadastro.AlternarHabilidade(p.Id, "artes-marciais").Ok, Is.False);
            Assert.That(cadastro.Validar(p).avisos, Has.None.Contains("Habilidades"));
        }

        [Test]
        public void Janus_tem_a_fixa_mais_as_escolhidas()
        {
            var p = Nova("janus");
            cadastro.AlternarHabilidade(p.Id, "absorcao-de-energia");
            var atuais = cadastro.HabilidadesAtuais(p);
            Assert.That(atuais.Select(h => h.Nome), Is.EqualTo(new[] { "Força", "Agilidade", "Resistência", "Absorção de energia" }));
            Assert.That(atuais.Count(h => h.Fixa), Is.EqualTo(3));
            Assert.That(cadastro.Validar(p).avisos, Has.Some.Contains("faltam 1"));
        }

        [Test]
        public void Legado_escolhe_a_linha_nega_dois_e_fica_com_tres()
        {
            var p = Nova("legado");
            Assert.That(cadastro.AlternarNegada(p.Id, "voo").Ok, Is.False);
            cadastro.EscolherLinha(p.Id, "classica");
            cadastro.AlternarNegada(p.Id, "raios-oculares");
            cadastro.AlternarNegada(p.Id, "supersentidos");
            Assert.That(cadastro.AlternarNegada(p.Id, "voo").Ok, Is.False);
            Assert.That(cadastro.HabilidadesAtuais(p).Select(h => h.Id),
                Is.EqualTo(new[] { "superforca", "invulnerabilidade", "voo" }));

            cadastro.EscolherLinha(p.Id, "sombras");
            Assert.That(p.Criacao.Habilidades.Negadas, Is.Empty);
        }

        [Test]
        public void Discipula_preenche_tres_vagas_diferentes()
        {
            var p = Nova("discipula");
            Assert.That(cadastro.DefinirVaga(p.Id, "compartilhada", "luta-impossivel").Ok, Is.True);
            Assert.That(cadastro.DefinirVaga(p.Id, "sua", "luta-impossivel").Ok, Is.False);
            cadastro.DefinirVaga(p.Id, "sua", "telepatia-e-telecinese");
            Assert.That(cadastro.Validar(p).avisos, Has.Some.Contains("da mentora"));
            cadastro.DefinirVaga(p.Id, "mentora", "controle-elemental");
            Assert.That(cadastro.HabilidadesAtuais(p), Has.Count.EqualTo(3));
            Assert.That(cadastro.HabilidadesAtuais(p)[0].Nome, Is.EqualTo("Luta impossível (compartilhada)"));
        }

        [Test]
        public void Bruta_pede_descricao_de_cada_fixa()
        {
            var p = Nova("bruta");
            Assert.That(cadastro.HabilidadesAtuais(p), Has.Count.EqualTo(3));
            Assert.That(cadastro.Validar(p).avisos, Has.Some.Contains("Superresistente"));
            cadastro.DescreverHabilidade(p.Id, "superresistente", "dobra de tamanho");
            Assert.That(cadastro.HabilidadesAtuais(p)[0].Descricao, Is.EqualTo("dobra de tamanho"));
            Assert.That(cadastro.Validar(p).avisos, Has.None.Contains("Superresistente"));
        }

        [Test]
        public void Perguntas_de_historico_sao_cobradas_e_trocar_de_arquetipo_apaga_as_respostas()
        {
            var p = Nova("bruta");
            Assert.That(cadastro.Validar(p).avisos, Has.Some.Contains("faltam 5"));
            cadastro.ResponderHistorico(p.Id, 0, "O laboratório da Fundação.");
            Assert.That(cadastro.Validar(p).avisos, Has.Some.Contains("faltam 4"));
            cadastro.AlterarTextos(p.Id, "Alta, cheia de cicatrizes.", "Fugiu aos 15.");
            Assert.That(cadastro.TrocaDeArquetipoApaga(p), Is.True);

            cadastro.AlterarIdentidade(p.Id, "Teste", "", "farol", null);
            Assert.That(p.Identidade.RespostasHistorico, Is.Empty);
            Assert.That(p.Identidade.Aparencia, Is.EqualTo("Alta, cheia de cicatrizes."));
            Assert.That(p.Identidade.Resumo, Is.EqualTo("Fugiu aos 15."));
        }

        [Test]
        public void Trocar_de_arquetipo_zera_as_habilidades()
        {
            var p = Nova("farol");
            cadastro.AlternarHabilidade(p.Id, "espadas");
            Assert.That(p.Criacao.TemEscolhas, Is.True);
            cadastro.AlterarIdentidade(p.Id, "Teste", "", "nova", null);
            Assert.That(cadastro.HabilidadesAtuais(p), Is.Empty);
        }
    }
}
