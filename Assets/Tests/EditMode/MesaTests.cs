using System.Linq;
using NUnit.Framework;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Salas;

namespace TorreDeControle.Tests
{
    public class MesaTests
    {
        ArmazenamentoEmMemoria armazenamento;
        CadastroPersonagens personagens;
        CadastroSalas salas;
        Sala sala;
        Mesa mesa;
        Personagem heroi;

        [SetUp]
        public void Preparar()
        {
            armazenamento = new ArmazenamentoEmMemoria();
            personagens = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            salas = CadastroSalas.Carregar(armazenamento, personagens);
            salas.Criar("Nova Era", 6);
            sala = salas.Listar().Single();
            var original = FichaPronta.Criada(personagens, "Estrela");
            salas.PorPersonagem(sala.Id, 0, original.Id, false);
            heroi = personagens.Buscar(sala.Slots[0].Personagem);
            FichaPronta.ParaCarreira(salas, personagens, sala, heroi);
            mesa = Mesa.Carregar(armazenamento, personagens, sala.Id);
        }

        Personagem Colega()
        {
            var original = FichaPronta.Criada(personagens, "Pulsar");
            salas.PorPersonagem(sala.Id, 1, original.Id, false);
            var colega = personagens.Buscar(sala.Slots[1].Personagem);
            FichaPronta.ParaCarreira(salas, personagens, sala, colega);
            return colega;
        }

        [Test]
        public void Pilha_de_equipe_inicio_de_sessao_gasto_e_o_tempo_passa()
        {
            Assert.That(mesa.Equipe, Is.EqualTo(0));
            mesa.InicioDeSessao();
            Assert.That(mesa.MudarEquipe(2, null, null).Ok, Is.True);
            Assert.That(mesa.Equipe, Is.EqualTo(3));
            Assert.That(mesa.MudarEquipe(-4, null, null).Ok, Is.False, "não fica negativa");
            mesa.OTempoPassa(null);
            Assert.That(mesa.Equipe, Is.EqualTo(1));
            Assert.That(Mesa.Carregar(armazenamento, personagens, sala.Id).Equipe, Is.EqualTo(1)); // gravada
        }

        [Test]
        public void Entrar_em_batalha_conta_o_questionario_e_desfazer_volta_tudo()
        {
            var colega = Colega();
            colega.Influencias[heroi.Id] = EstadoInfluencia.Perdida; // começa sem Influência da líder sobre ela
            var e = new Mesa.EntradaEmBatalha
            {
                Lider = heroi.Id, Colegas = new[] { heroi.Id, colega.Id },
                MesmoObjetivo = true, MalPreparada = true,
            };
            var conta = mesa.ContarBatalha(e);
            Assert.That(conta.total, Is.EqualTo(2), "+2 +1 objetivo −1 despreparada; sem Influência sobre o colega");
            Assert.That(conta.semInfluencia, Is.EqualTo(new[] { colega.NomeExibicao }));

            colega.Influencias[heroi.Id] = EstadoInfluencia.Dada; // a líder tem Influência sobre o colega
            e.CondicoesParaEvitar = new[] { "irritado" };       // e marca Irritado para não perder o ponto
            Assert.That(mesa.ContarBatalha(e).total, Is.EqualTo(4));
            Assert.That(mesa.EntrarEmBatalha(e, null).Ok, Is.True);
            Assert.That(mesa.Equipe, Is.EqualTo(4));
            Assert.That(heroi.CondicoesMarcadas, Has.Member("irritado"));

            Assert.That(mesa.Corrigir(mesa.Eventos.Last().Id, "engano").Ok, Is.True);
            Assert.That(mesa.Equipe, Is.EqualTo(0));
            Assert.That(heroi.CondicoesMarcadas, Has.No.Member("irritado"));
        }

        [Test]
        public void Usar_a_pilha_gasta_varios_de_uma_vez_e_nao_passa_do_que_tem()
        {
            mesa.MudarEquipe(3, null, null);
            Assert.That(mesa.UsarEquipe(heroi.Id, 4, null).Ok, Is.False);
            Assert.That(mesa.UsarEquipe(heroi.Id, 2, "movimento do Farol").Ok, Is.True);
            Assert.That(mesa.Equipe, Is.EqualTo(1));
            Assert.That(mesa.Eventos.Last().Personagem, Is.EqualTo(heroi.Id));
            Assert.That(mesa.UsarEquipe(null, 1, null).Ok, Is.True, "quem usa é opcional");
        }

        [Test]
        public void Vilao_em_cena_marca_as_condicoes_que_tem_e_fica_no_limite()
        {
            personagens.CriarNpc(sala.Id, "Doutor Ocaso", Maturidade.Adulto, Papel.Vilao);
            var ocaso = personagens.NpcsDaSala(sala.Id).Single();
            personagens.AlternarCondicaoDeVilao(ocaso.Id, "irritado");
            personagens.AlternarCondicaoDeVilao(ocaso.Id, "culpado");

            Assert.That(mesa.AlternarCondicaoDoVilao(ocaso.Id, "irritado", null).Ok, Is.False, "fora de cena");
            Assert.That(mesa.AlternarEmCena(ocaso.Id).Ok, Is.True);
            Assert.That(mesa.EmCena().Single(), Is.SameAs(ocaso));
            Assert.That(mesa.AlternarCondicaoDoVilao(ocaso.Id, "assustado", null).Ok, Is.False, "não tem na ficha");
            mesa.AlternarCondicaoDoVilao(ocaso.Id, "irritado", "cortaram o cabo");
            Assert.That(mesa.NoLimite(ocaso), Is.False);
            mesa.AlternarCondicaoDoVilao(ocaso.Id, "culpado", null);
            Assert.That(mesa.NoLimite(ocaso), Is.True);
            Assert.That(ocaso.Vilao.Condicoes.Count, Is.EqualTo(2), "a ficha não muda: a luta é da mesa");

            Assert.That(mesa.UsarMovimentoDeVilao(ocaso.Id, "Fugir prometendo voltar", null).Ok, Is.True);
            mesa.Corrigir(mesa.Eventos.Last(e => e.Mudancas.Count > 0).Id, "engano");
            Assert.That(mesa.CondicoesMarcadasDoVilao(ocaso.Id), Is.EqualTo(new[] { "irritado" }));
            Assert.That(Mesa.Carregar(armazenamento, personagens, sala.Id).CondicoesMarcadasDoVilao(ocaso.Id),
                Is.EqualTo(new[] { "irritado" })); // gravado
        }

        [Test]
        public void Bonus_dado_aparece_usar_tira_e_desfazer_devolve()
        {
            Assert.That(mesa.DarBonus(heroi.Id, Mesa.BonusAdiante, 1, "agindo pelas respostas da avaliação", null).Ok, Is.True);
            var bonus = mesa.BonusDe(heroi.Id).Single();
            Assert.That(bonus.Contexto, Is.EqualTo("agindo pelas respostas da avaliação"));
            Assert.That(mesa.RetirarBonus(bonus.Id, null).Ok, Is.True);
            Assert.That(mesa.BonusDe(heroi.Id), Is.Empty);
            mesa.Corrigir(mesa.Eventos.Last().Id, "não usou ainda");
            Assert.That(mesa.BonusDe(heroi.Id).Single().Id, Is.EqualTo(bonus.Id));
            Assert.That(mesa.DarBonus(heroi.Id, "outro", 1, null, null).Ok, Is.False);
            Assert.That(Mesa.Carregar(armazenamento, personagens, sala.Id).BonusDe(heroi.Id).Count, Is.EqualTo(1)); // gravado
        }

        [Test]
        public void Sozinha_nao_entra_em_batalha_como_equipe()
        {
            var e = new Mesa.EntradaEmBatalha { Lider = heroi.Id, Colegas = new[] { heroi.Id } };
            Assert.That(mesa.EntrarEmBatalha(e, null).Ok, Is.False);
        }

        [Test]
        public void Sala_pronta_para_a_mesa_so_com_todas_em_carreira()
        {
            // O Preparar deixou uma colega em aprovação na sala (a formação pede equipe).
            var (_, pronta, motivo) = salas.ParaAMesa().Single();
            Assert.That(pronta, Is.False);
            Assert.That(motivo, Does.Contain("prontas"));
            foreach (var v in sala.Slots.Where(v => v.Personagem != null))
                personagens.Buscar(v.Personagem).Status = StatusCriacao.EmCarreira; // atalho: aprovadas
            Assert.That(salas.ParaAMesa().Single().pronta, Is.True);
        }

        [Test]
        public void Sempre_ha_uma_rodada_corrente()
        {
            Assert.That(mesa.Rodada, Is.EqualTo(1));
            mesa.NovaRodada();
            Assert.That(mesa.Rodada, Is.EqualTo(2));
        }

        [Test]
        public void Dar_potencial_vira_evento_com_rodada_e_motivo()
        {
            Assert.That(mesa.DarPotencial(heroi.Id, 1, "falhou ao enfrentar").Ok, Is.True);
            Assert.That(heroi.Potencial, Is.EqualTo(1));
            var e = mesa.Eventos.Single();
            Assert.That(e.Rodada, Is.EqualTo(1));
            Assert.That(e.Motivo, Is.EqualTo("falhou ao enfrentar"));
            Assert.That(e.Mudancas.Single().Antes, Is.EqualTo(0));
            Assert.That(e.Mudancas.Single().Depois, Is.EqualTo(1));
            Assert.That(mesa.DarPotencial(heroi.Id, -5, null).Ok, Is.False);
        }

        [Test]
        public void Condicao_marca_e_desmarca()
        {
            mesa.AlternarCondicao(heroi.Id, Condicoes.Culpado, null);
            mesa.AlternarCondicao(heroi.Id, Condicoes.Irritado, null);
            Assert.That(heroi.CondicoesMarcadas, Is.EqualTo(new[] { Condicoes.Irritado, Condicoes.Culpado }));
            mesa.AlternarCondicao(heroi.Id, Condicoes.Culpado, null);
            Assert.That(heroi.CondicoesMarcadas.Single(), Is.EqualTo(Condicoes.Irritado));
        }

        [Test]
        public void Mudar_rotulos_sobe_um_e_desce_outro()
        {
            var antes = personagens.RotulosAtuais(heroi).ToDictionary(kv => kv.Key, kv => kv.Value);
            Assert.That(mesa.MudarRotulos(heroi.Id, Rotulos.Perigoso, Rotulos.Mundano, "provocou").Ok, Is.True);
            var depois = personagens.RotulosAtuais(heroi);
            Assert.That(depois[Rotulos.Perigoso], Is.EqualTo(antes[Rotulos.Perigoso] + 1));
            Assert.That(depois[Rotulos.Mundano], Is.EqualTo(antes[Rotulos.Mundano] - 1));
            // A foto da criação não muda.
            Assert.That(heroi.Foto.Rotulos[Rotulos.Perigoso], Is.EqualTo(antes[Rotulos.Perigoso]));
        }

        [Test]
        public void Estouro_nao_muda_rotulos_e_marca_a_condicao_escolhida()
        {
            // Leva Superior ao máximo.
            while (personagens.RotulosAtuais(heroi)[Rotulos.Superior] < Rotulos.Maximo)
            {
                var alvoDesce = Rotulos.Todos.First(r => r != Rotulos.Superior &&
                                                        personagens.RotulosAtuais(heroi)[r] > Rotulos.Minimo);
                mesa.MudarRotulos(heroi.Id, Rotulos.Superior, alvoDesce, null);
            }
            var antes = personagens.RotulosAtuais(heroi).ToDictionary(kv => kv.Key, kv => kv.Value);
            var semEscolha = mesa.MudarRotulos(heroi.Id, Rotulos.Superior, Rotulos.Mundano, null);
            Assert.That(semEscolha.Ok, Is.False);
            Assert.That(semEscolha.Erro, Is.EqualTo(Mesa.MensagemDeEstouro));

            Assert.That(mesa.MudarRotulos(heroi.Id, Rotulos.Superior, Rotulos.Mundano, null, Condicoes.Inseguro).Ok, Is.True);
            Assert.That(personagens.RotulosAtuais(heroi), Is.EqualTo(antes));
            Assert.That(heroi.CondicoesMarcadas, Has.Member(Condicoes.Inseguro));
        }

        [Test]
        public void Corrigir_lanca_o_inverso_e_nao_apaga()
        {
            mesa.DarPotencial(heroi.Id, 1, null);
            mesa.DarPotencial(heroi.Id, 1, null);
            var primeiro = mesa.Eventos.First();
            Assert.That(mesa.Corrigir(primeiro.Id, "foi engano").Ok, Is.True);
            Assert.That(heroi.Potencial, Is.EqualTo(1));
            Assert.That(mesa.Eventos, Has.Count.EqualTo(3));
            Assert.That(mesa.Corrigido(primeiro), Is.True);
            Assert.That(mesa.Corrigir(primeiro.Id, null).Ok, Is.False);
            Assert.That(mesa.Corrigir(mesa.Eventos.Last().Id, null).Ok, Is.False);
        }

        [Test]
        public void So_instancia_em_jogo_desta_sala()
        {
            var original = personagens.Buscar(heroi.Origem);
            Assert.That(mesa.DarPotencial(original.Id, 1, null).Ok, Is.False); // laboratório

            // Outra instância, ainda em aprovação (e depois em revisão): não joga.
            var outra = FichaPronta.Criada(personagens, "Cometa");
            salas.PorPersonagem(sala.Id, 1, outra.Id, false);
            var emAprovacao = personagens.Buscar(sala.Slots[1].Personagem);
            Assert.That(mesa.DarPotencial(emAprovacao.Id, 1, null).Ok, Is.False);
            personagens.ReabrirCriacao(emAprovacao.Id);
            Assert.That(emAprovacao.Status, Is.EqualTo(StatusCriacao.EmRevisao));
            Assert.That(mesa.DarPotencial(emAprovacao.Id, 1, null).Ok, Is.False);
        }

        [Test]
        public void Log_e_estado_sobrevivem_a_recarga()
        {
            mesa.DarPotencial(heroi.Id, 1, "fim de sessão");
            mesa.AlternarCondicao(heroi.Id, Condicoes.Assustado, null);
            mesa.NovaRodada();

            var p2 = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos());
            var m2 = Mesa.Carregar(armazenamento, p2, sala.Id);
            Assert.That(m2.Rodada, Is.EqualTo(2));
            Assert.That(m2.Eventos, Has.Count.EqualTo(2));
            var h2 = p2.Buscar(heroi.Id);
            Assert.That(h2.Potencial, Is.EqualTo(1));
            Assert.That(h2.CondicoesMarcadas.Single(), Is.EqualTo(Condicoes.Assustado));
        }
    }
}
