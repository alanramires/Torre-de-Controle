using System;
using System.Collections.Generic;
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
            mesa = Mesa.Carregar(armazenamento, personagens, sala.Id, TorreDeControle.Core.Regras.RegrasDeCondicoes.Carregar(new RegrasDeTeste()));
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
        public void Trilha_da_perdicao_marca_pela_mesa_com_motivo_e_desfaz()
        {
            heroi.Identidade.Arquetipo = "amaldicoada"; // atalho de teste
            if (heroi.Foto != null) heroi.Foto.Arquetipo = "amaldicoada";
            var trilha = personagens.TrilhasDoArquetipo(heroi).Single();
            Assert.That(trilha.nome, Is.EqualTo("Trilha da perdição"));
            Assert.That(mesa.MarcarTrilha(heroi.Id, trilha.extra, trilha.campo, 1, "visões sombrias na usina").Ok, Is.True);
            Assert.That(personagens.Contador(heroi, trilha.extra, trilha.campo), Is.EqualTo(1));
            Assert.That(mesa.Eventos.Last().Motivo, Is.EqualTo("visões sombrias na usina"));
            mesa.Corrigir(mesa.Eventos.Last().Id, "engano");
            Assert.That(personagens.Contador(heroi, trilha.extra, trilha.campo), Is.EqualTo(0));
            Assert.That(mesa.MarcarTrilha(heroi.Id, trilha.extra, trilha.campo, -1, null).Ok, Is.False, "não fica negativa");
        }

        [Test]
        public void Refazer_devolve_o_que_um_desfazer_tirou()
        {
            mesa.MudarEquipe(3, null, "questionário");
            mesa.Corrigir(mesa.Eventos.Last().Id, "engano");
            Assert.That(mesa.Equipe, Is.EqualTo(0));
            var desfazer = mesa.Eventos.Last();
            Assert.That(mesa.Refazer(desfazer.Id, null).Ok, Is.True);
            Assert.That(mesa.Equipe, Is.EqualTo(3));
            Assert.That(mesa.Refazer(desfazer.Id, null).Ok, Is.False, "uma vez só");
            Assert.That(mesa.Corrigir(mesa.Eventos.Last().Id, "de novo").Ok, Is.True, "o refeito também se desfaz");
            Assert.That(mesa.Equipe, Is.EqualTo(0));
        }

        [Test]
        public void Lembrete_vai_no_vilao_em_cena_e_resolver_tira()
        {
            personagens.CriarNpc(sala.Id, "Doutor Ocaso", Maturidade.Adulto, Papel.Vilao);
            var ocaso = personagens.NpcsDaSala(sala.Id).Single();
            Assert.That(mesa.DarBonus(ocaso.Id, Mesa.BonusLembrete, 0, "abertura: Aurora cedeu terreno", null).Ok, Is.False, "fora de cena");
            mesa.AlternarEmCena(ocaso.Id);
            Assert.That(mesa.DarBonus(ocaso.Id, Mesa.BonusLembrete, 0, "  ", null).Ok, Is.False, "sem texto");
            Assert.That(mesa.DarBonus(ocaso.Id, Mesa.BonusLembrete, 0, "abertura: Aurora cedeu terreno", null).Ok, Is.True);
            var l = mesa.BonusDe(ocaso.Id).Single();
            Assert.That(mesa.RetirarBonus(l.Id, null).Ok, Is.True);
            Assert.That(mesa.BonusDe(ocaso.Id), Is.Empty);
            Assert.That(mesa.Eventos.Last().Descricao, Does.Contain("resolveu o lembrete"));
        }

        [Test]
        public void Desfazer_rotulo_antigo_respeita_o_limite()
        {
            var r = mesa.MudarRotulos(heroi.Id, Rotulos.Perigoso, Rotulos.Mundano, "teste", Condicoes.Irritado);
            Assume.That(r.Ok && mesa.Eventos.Last().Mudancas.Any(m => m.Campo == Mesa.CampoRotulo(Rotulos.Perigoso)));
            var subiu = mesa.Eventos.Last();
            heroi.RotulosJogo[Rotulos.Perigoso] = Rotulos.Minimo; // jogadas depois o levaram ao mínimo
            Assert.That(mesa.Corrigir(subiu.Id, "engano").Ok, Is.True);
            Assert.That(personagens.RotulosAtuais(heroi)[Rotulos.Perigoso], Is.EqualTo(Rotulos.Minimo), "não passa de −2");
        }

        [Test]
        public void Refazer_devolve_so_o_que_a_correcao_tirou_de_fato()
        {
            mesa.MudarRotulos(heroi.Id, Rotulos.Perigoso, Rotulos.Mundano, "teste", Condicoes.Irritado);
            var subiu = mesa.Eventos.Last();
            Assume.That(subiu.Mudancas.Any(m => m.Campo == Mesa.CampoRotulo(Rotulos.Perigoso)));
            heroi.RotulosJogo[Rotulos.Perigoso] = Rotulos.Minimo; // depois, o Rótulo foi ao mínimo
            var mundanoAntes = personagens.RotulosAtuais(heroi)[Rotulos.Mundano];
            mesa.Corrigir(subiu.Id, "engano");                   // Perigoso não desce (já no −2); Mundano volta +1
            mesa.Refazer(mesa.Eventos.Last().Id, null);
            Assert.That(personagens.RotulosAtuais(heroi)[Rotulos.Perigoso], Is.EqualTo(Rotulos.Minimo), "não sobe o que não desceu");
            Assert.That(personagens.RotulosAtuais(heroi)[Rotulos.Mundano], Is.EqualTo(mundanoAntes));
        }

        [Test]
        public void Mestre_edita_e_apaga_entradas_do_log()
        {
            mesa.MudarEquipe(2, "+2 Equipe", null);
            var e = mesa.Eventos.Last();
            Assert.That(mesa.EditarEvento(e.Id, "questionário: +2", "luta na usina").Ok, Is.True);
            Assert.That(e.Descricao, Is.EqualTo("questionário: +2"));
            Assert.That(e.Editado, Is.True);
            Assert.That(mesa.Equipe, Is.EqualTo(2), "editar não muda o efeito");

            Assert.That(mesa.ApagarEvento(e.Id, desfazerEfeito: false).Ok, Is.False, "tem efeito: precisa desfazer junto");
            Assert.That(mesa.ApagarEvento(e.Id, desfazerEfeito: true).Ok, Is.True);
            Assert.That(mesa.Equipe, Is.EqualTo(0));
            Assert.That(mesa.Visiveis, Is.Empty, "a entrada e o desfazer somem do log");
            Assert.That(mesa.Eventos.Count, Is.EqualTo(2), "mas ficam no arquivo");
        }

        [Test]
        public void Marcar_e_desmarcar_seguidos_somem_do_log_sem_mudar_a_mesa()
        {
            mesa.AlternarCondicao(heroi.Id, Condicoes.Irritado, null);
            mesa.AlternarCondicao(heroi.Id, Condicoes.Irritado, null);
            Assert.That(mesa.Visiveis, Is.Empty, "o par se anulou");
            Assert.That(heroi.CondicoesMarcadas, Has.No.Member(Condicoes.Irritado));
            mesa.AlternarCondicao(heroi.Id, Condicoes.Irritado, "cortaram o cabo"); // com motivo: fica
            Assert.That(mesa.Visiveis.Count, Is.EqualTo(1));
        }

        [Test]
        public void Limpar_esconde_idas_e_voltas_antigas()
        {
            mesa.MudarEquipe(1, "+1 Equipe", null);
            var ida = mesa.Eventos.Last();
            mesa.MudarEquipe(-1, "−1 Equipe", null); // virou par na hora
            Assert.That(mesa.Visiveis, Is.Empty);
            ida.Oculto = false;                     // simula um log antigo, de antes da limpeza automática
            mesa.Eventos.Last().Oculto = false;
            Assert.That(mesa.EsconderIdasEVoltas(), Is.EqualTo(1));
            Assert.That(mesa.Visiveis, Is.Empty);
            Assert.That(mesa.Equipe, Is.EqualTo(0));
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
            Assert.That(motivo, Does.Contain("prontos"));
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

        [Test]
        public void Rolagem_fica_pendente_e_intervencoes_deslizam_sem_teto()
        {
            var colega = Colega();
            mesa.MudarEquipe(2, null, null);
            Assert.That(mesa.Rolar(heroi.Id, 4, 5, 9, "enfrentar o chefe").Ok, Is.True);
            var e = mesa.Eventos.Last();
            Assert.That(e.Rolagem.Mod, Is.EqualTo(Mesa.TetoMaximo), "o teto vale na rolagem");
            Assert.That(e.Descricao, Is.EqualTo("rolou 2d6+4: 4 + 5 + 4 = 13 (10+)"));
            Assert.That(mesa.Pendente(e), Is.True);

            Assert.That(mesa.Intervir(e.Id, new Intervencao { Quem = colega.Id, DeOnde = "ajuda (pilha)", Mod = 2, CustoEquipe = 1 }).Ok, Is.True);
            Assert.That(e.Rolagem.Total, Is.EqualTo(15), "depois do dado passa do teto");
            Assert.That(mesa.Equipe, Is.EqualTo(1), "a ajuda gasta da pilha");
            Assert.That(mesa.Intervir(e.Id, new Intervencao { Quem = colega.Id, DeOnde = "outro", Mod = 3 }).Ok, Is.False, "de −2 a +2");
            Assert.That(mesa.Intervir(e.Id, new Intervencao { Quem = null, DeOnde = "outro", Mod = 1 }).Ok, Is.False, "precisa de quem");
            Assert.That(mesa.Intervir(e.Id, new Intervencao { Quem = heroi.Id, DeOnde = "egoísta (pilha)", Mod = 1, CustoEquipe = 1 }).Ok, Is.True,
                "quem rola também intervém");
            Assert.That(mesa.Equipe, Is.EqualTo(0));
            Assert.That(mesa.Intervir(e.Id, new Intervencao { Quem = colega.Id, DeOnde = "ajuda (pilha)", Mod = 1, CustoEquipe = 1 }).Ok, Is.False,
                "pilha vazia");
            Assert.That(e.Mudancas.Count(m => m.Campo == Mesa.CampoEquipe), Is.EqualTo(1), "uma só mudança da pilha no evento");

            Assert.That(mesa.RetirarIntervencao(e.Id, 1).Ok, Is.True);
            Assert.That(mesa.Equipe, Is.EqualTo(1), "retirar devolve o ponto");
            Assert.That(e.Descricao, Does.Contain("Pulsar (ajuda (pilha), −1 Equipe)").And.EndsWith("= 15 (10+)"));

            Assert.That(mesa.AceitarRolagem(e.Id).Ok, Is.True);
            Assert.That(mesa.Pendente(e), Is.False);
            Assert.That(e.Descricao, Does.EndWith("= 15 (10+) · sucesso total"), "rolagem livre: grava o tipo, sem escolhas");
            Assert.That(mesa.Intervir(e.Id, new Intervencao { Quem = colega.Id, DeOnde = "outro", Mod = 1 }).Ok, Is.False, "aceita, fecha");

            Assert.That(mesa.Corrigir(e.Id, "rolagem errada").Ok, Is.True);
            Assert.That(mesa.Equipe, Is.EqualTo(2), "desfazer a rolagem devolve a pilha");
            var gravada = Mesa.Carregar(armazenamento, personagens, sala.Id).Eventos.First(x => x.Id == e.Id);
            Assert.That(gravada.Rolagem.Intervencoes.Single().Mod, Is.EqualTo(2));
            Assert.That(gravada.Rolagem.Aceita, Is.True);
        }

        [Test]
        public void Rolar_movimento_soma_o_rotulo_de_quem_rola_e_o_generico_rola_mais_zero()
        {
            var colega = Colega();
            var basicos = TorreDeControle.Core.Regras.MovimentosBasicos.Carregar(new RegrasDeTeste());
            Assert.That(basicos.ComRotulo.Count, Is.EqualTo(7), "o golpe poderoso soma condições, não Rótulo");
            var enfrentar = basicos.ComRotulo.First(m => m.Id == "basico/enfrentar");
            var perigoso = personagens.ValorDeJogo(heroi, Mesa.CampoRotulo(Rotulos.Perigoso));

            Assert.That(mesa.RolarMovimento(heroi.Id, enfrentar, colega.Id, 3, 4, null).Ok, Is.True);
            var e = mesa.Eventos.Last();
            Assert.That(e.Rolagem.Mod, Is.EqualTo(perigoso));
            Assert.That(e.Rolagem.Total, Is.EqualTo(7 + perigoso));
            Assert.That(e.Descricao, Does.StartWith($"{enfrentar.Nome} → {colega.NomeExibicao} (Perigoso "));
            Assert.That(mesa.Pendente(e), Is.True, "fica pendente: Intervir / Aceitar");

            Assert.That(enfrentar.PedeAlvo, Is.True);
            Assert.That(mesa.RolarMovimento(null, enfrentar, null, 6, 6, null).Ok, Is.False, "Enfrentar contra o nada é liberar os poderes");
            Assert.That(mesa.RolarMovimento(null, enfrentar, colega.Id, 6, 6, null).Ok, Is.True);
            Assert.That(mesa.Eventos.Last().Descricao, Is.EqualTo($"{enfrentar.Nome} → {colega.NomeExibicao} (Perigoso +0): 6 + 6 = 12 (10+)"));
            var liberar = basicos.ComRotulo.First(m => m.Id == "basico/liberar-poderes");
            Assert.That(mesa.RolarMovimento(null, liberar, null, 3, 3, null).Ok, Is.True, "liberar os poderes não pede alvo");
            Assert.That(mesa.RolarMovimento(heroi.Id, enfrentar, heroi.Id, 1, 1, null).Ok, Is.False, "alvo não é quem rola");
        }

        [Test]
        public void Efeito_esperado_do_enfrentar_troca_golpes_no_sucesso_e_lista_as_opcoes_uma_vez()
        {
            var basicos = TorreDeControle.Core.Regras.MovimentosBasicos.Carregar(new RegrasDeTeste());
            var linhas = basicos.ComRotulo.First(m => m.Id == "basico/enfrentar").EfeitoEsperado();
            var sucesso = linhas.Where(l => l.Faixa == TorreDeControle.Core.Regras.MovimentosBasicos.Faixa.Sucesso).ToList();
            Assert.That(sucesso[0].Texto, Is.EqualTo("Num sucesso (7+):"));
            Assert.That(sucesso[1].Texto, Does.StartWith("Vocês trocam golpes"));
            Assert.That(sucesso.Any(l => l.Texto == "Num 10+, escolha 2. Num 7–9, escolha 1:"), Is.True);
            Assert.That(sucesso.Count(l => l.Nivel == 1 && l.Texto.StartsWith("Resistir ou evitar")), Is.EqualTo(1), "a lista aparece uma vez");
            Assert.That(linhas.Last().Texto, Is.EqualTo("o MJ faz um movimento"), "falha padrão");
            Assert.That(TorreDeControle.Core.Regras.MovimentosBasicos.Vale(TorreDeControle.Core.Regras.MovimentosBasicos.Faixa.Sucesso, 7), Is.True);
            Assert.That(TorreDeControle.Core.Regras.MovimentosBasicos.Vale(TorreDeControle.Core.Regras.MovimentosBasicos.Faixa.Forte, 9), Is.False);
            // Os outros movimentos também viram texto (o modelo serve para todos).
            foreach (var m in basicos.Todos)
                Assert.That(m.EfeitoEsperado().Count, Is.GreaterThan(2), m.Id);
        }

        [Test]
        public void Aceitar_grava_resultado_tipo_escolhas_e_a_hora()
        {
            var basicos = TorreDeControle.Core.Regras.MovimentosBasicos.Carregar(new RegrasDeTeste());
            var enfrentar = basicos.ComRotulo.First(m => m.Id == "basico/enfrentar");
            Assert.That(enfrentar.EscolhasNo(TorreDeControle.Core.Regras.MovimentosBasicos.Faixa.Forte), Is.EqualTo(2));
            Assert.That(enfrentar.EscolhasNo(TorreDeControle.Core.Regras.MovimentosBasicos.Faixa.Parcial), Is.EqualTo(1));
            var opcoes = enfrentar.OpcoesNo(TorreDeControle.Core.Regras.MovimentosBasicos.Faixa.Forte);
            Assert.That(opcoes.Select(o => o.Id), Is.EqualTo(new[] { "resistir", "tirar", "oportunidade", "impressionar" }));
            Assert.That(opcoes.First(o => o.Id == "oportunidade").Alternativas, Is.EqualTo(new[] { "um aliado ganha a chance", "+1 Equipe" }));
            Assert.That(opcoes.First(o => o.Id == "impressionar").Alternativas, Is.EqualTo(new[] { "impressionar", "surpreender", "assustar" }),
                "o MJ escolhe qual das três");

            var alvo = Colega(); // o Enfrentar pede contra quem
            mesa.RolarMovimento(null, enfrentar, alvo.Id, 6, 6, null); // 12: sucesso total
            var e = mesa.Eventos.Last();
            Assert.That(mesa.AceitarRolagem(e.Id, enfrentar, new[] { new EscolhaDaRolagem { Opcao = "resistir" } }).Ok, Is.False, "10+ escolhe 2");
            Assert.That(mesa.AceitarRolagem(e.Id, enfrentar, new[]
            {
                new EscolhaDaRolagem { Opcao = "resistir" }, new EscolhaDaRolagem { Opcao = "oportunidade" },
            }).Ok, Is.False, "a oportunidade pede a alternativa");
            Assert.That(mesa.AceitarRolagem(e.Id, enfrentar, new[]
            {
                new EscolhaDaRolagem { Opcao = "resistir" }, new EscolhaDaRolagem { Opcao = "oportunidade", Subopcao = 0 },
            }).Ok, Is.True, "contra PJ, a chance a um aliado (o +1 Equipe não vale)");
            var r = e.Rolagem;
            Assert.That(r.Resultado, Is.EqualTo(RolagemDaMesa.ResultadoSucesso));
            Assert.That(r.Tipo, Is.EqualTo(RolagemDaMesa.TipoTotal));
            Assert.That(r.Escolhas.Select(x => x.Opcao), Is.EqualTo(new[] { "resistir", "oportunidade" }));
            Assert.That(r.Escolhas[1].Subtexto, Is.EqualTo("um aliado ganha a chance"));
            Assert.That(r.AceitaEm, Is.Not.Null);
            Assert.That(e.Descricao, Does.Contain("· sucesso total: Resistir ou evitar os golpes, Abrir uma chance para os aliados (um aliado ganha a chance)").And.EndWith("toma golpe poderoso · herói genérico não recebe retorno (resistiu)"));

            mesa.RolarMovimento(null, enfrentar, alvo.Id, 1, 2, null); // 3: falha, nada a escolher
            var falha = mesa.Eventos.Last();
            Assert.That(mesa.AceitarRolagem(falha.Id, enfrentar, null).Ok, Is.True);
            Assert.That(falha.Rolagem.Resultado, Is.EqualTo(RolagemDaMesa.ResultadoFalha));
            Assert.That(falha.Rolagem.Tipo, Is.EqualTo(RolagemDaMesa.TipoFalha));
            var gravada = Mesa.Carregar(armazenamento, personagens, sala.Id).Eventos.First(x => x.Id == e.Id).Rolagem;
            Assert.That(gravada.Escolhas[1].Subopcao, Is.EqualTo(0));
        }

        [Test]
        public void Troca_de_golpes_alvo_marca_condicao_e_quem_rolou_recebe_um_retorno()
        {
            var basicos = TorreDeControle.Core.Regras.MovimentosBasicos.Carregar(new RegrasDeTeste());
            var enfrentar = basicos.ComRotulo.First(m => m.Id == "basico/enfrentar");
            Assert.That(enfrentar.TrocaGolpes, Is.True);
            Assert.That(enfrentar.CondicaoPadraoDoAlvo, Is.EqualTo("irritado"));
            Assert.That(enfrentar.AnulaRetorno("resistir"), Is.True);
            Assert.That(enfrentar.AnulaRetorno("tirar"), Is.False, "só resistir protege");
            Assert.That(enfrentar.ReflectePercepcao("impressionar"), Is.True);

            personagens.CriarNpc(sala.Id, "Slade", Maturidade.Adulto, Papel.Vilao);
            var slade = personagens.NpcsDaSala(sala.Id).Single();
            personagens.AlternarCondicaoDeVilao(slade.Id, "irritado");
            personagens.AlternarCondicaoDeVilao(slade.Id, "inseguro");
            mesa.AlternarEmCena(slade.Id);

            // 7: escolhe tirar; o MJ ainda precisa marcar a condição do Slade e o retorno.
            mesa.RolarMovimento(null, enfrentar, slade.Id, 3, 4, null);
            var e = mesa.Eventos.Last();
            var tirar = new[] { new EscolhaDaRolagem { Opcao = "tirar" } };
            Assert.That(mesa.AceitarRolagem(e.Id, enfrentar, tirar, new TrocaDeGolpes { Retorno = TrocaDeGolpes.RetornoGolpePoderoso }).Ok,
                Is.False, "a condição do alvo é obrigatória");
            Assert.That(mesa.AceitarRolagem(e.Id, enfrentar, tirar, new TrocaDeGolpes { CondicaoDoAlvo = "culpado", Retorno = TrocaDeGolpes.RetornoGolpePoderoso }).Ok,
                Is.False, "só uma condição que ele tem");
            Assert.That(mesa.AceitarRolagem(e.Id, enfrentar, tirar, new TrocaDeGolpes { CondicaoDoAlvo = "irritado" }).Ok,
                Is.False, "o retorno é obrigatório");
            Assert.That(mesa.AceitarRolagem(e.Id, enfrentar, tirar,
                new TrocaDeGolpes { CondicaoDoAlvo = "irritado", Retorno = TrocaDeGolpes.RetornoGolpePoderoso }).Ok, Is.True);
            Assert.That(e.Rolagem.Troca.CondicaoDoAlvo, Is.EqualTo("irritado"));
            Assert.That(e.Rolagem.Troca.Retorno, Is.EqualTo(TrocaDeGolpes.RetornoGolpePoderoso));
            Assert.That(e.Descricao, Does.EndWith("troca de golpes: Slade marca Irritado · herói genérico toma golpe poderoso (role à parte)"));

            // 7: resistir deixa quem rolou ileso; a condição do Slade continua.
            mesa.RolarMovimento(null, enfrentar, slade.Id, 3, 4, null);
            var r2 = mesa.Eventos.Last();
            Assert.That(mesa.AceitarRolagem(r2.Id, enfrentar, new[] { new EscolhaDaRolagem { Opcao = "resistir" } },
                new TrocaDeGolpes { CondicaoDoAlvo = "inseguro" }).Ok, Is.True);
            Assert.That(r2.Rolagem.Troca.Retorno, Is.EqualTo(TrocaDeGolpes.RetornoIleso));

            // Falha: não há troca.
            mesa.RolarMovimento(null, enfrentar, slade.Id, 1, 2, null);
            var r3 = mesa.Eventos.Last();
            Assert.That(mesa.AceitarRolagem(r3.Id, enfrentar, null, null).Ok, Is.True);
            Assert.That(r3.Rolagem.Troca, Is.Null);
        }

        [Test]
        public void Pilha_uma_vez_por_pessoa_e_quem_rola_gastando_e_egoista()
        {
            var colega = Colega();
            mesa.MudarEquipe(3, null, null);
            var basicos = TorreDeControle.Core.Regras.MovimentosBasicos.Carregar(new RegrasDeTeste());
            var enfrentar = basicos.ComRotulo.First(m => m.Id == "basico/enfrentar");
            mesa.RolarMovimento(heroi.Id, enfrentar, colega.Id, 2, 3, null);
            var e = mesa.Eventos.Last();
            Intervencao Pilha(string quem) => new Intervencao { Quem = quem, DeOnde = Intervencao.DaPilha, Mod = 1, CustoEquipe = 1 };

            Assert.That(mesa.Intervir(e.Id, Pilha(colega.Id)).Ok, Is.True);
            Assert.That(e.Rolagem.Intervencoes.Last().DeOnde, Is.EqualTo(Intervencao.AjudaDaPilha), "outra pessoa: ajuda");
            Assert.That(mesa.Intervir(e.Id, Pilha(colega.Id)).Ok, Is.False, "1 ponto por pessoa por jogada");
            Assert.That(mesa.Intervir(e.Id, Pilha(heroi.Id)).Ok, Is.True, "quem rola também gasta");
            Assert.That(e.Rolagem.Intervencoes.Last().DeOnde, Is.EqualTo(Intervencao.EgoistaDaPilha), "quem rola: egoísta");
            Assert.That(mesa.Intervir(e.Id, Pilha(heroi.Id)).Ok, Is.False, "o egoísta também é 1 por jogada");
            Assert.That(mesa.Equipe, Is.EqualTo(1));
        }

        [Test]
        public void Aceitar_aplica_condicoes_equipe_e_potencial_e_desfazer_volta_tudo()
        {
            var basicos = TorreDeControle.Core.Regras.MovimentosBasicos.Carregar(new RegrasDeTeste());
            var enfrentar = basicos.ComRotulo.First(m => m.Id == "basico/enfrentar");
            personagens.CriarNpc(sala.Id, "Slade", Maturidade.Adulto, Papel.Vilao);
            var slade = personagens.NpcsDaSala(sala.Id).Single();
            personagens.AlternarCondicaoDeVilao(slade.Id, "irritado");
            mesa.AlternarEmCena(slade.Id);

            // 7: oportunidade (+1 Equipe); Slade marca Irritado; o retorno é Assustado.
            var perigoso = personagens.ValorDeJogo(heroi, Mesa.CampoRotulo(Rotulos.Perigoso));
            mesa.RolarMovimento(heroi.Id, enfrentar, slade.Id, 4, 4 - perigoso, null); // total 8
            var e = mesa.Eventos.Last();
            Assert.That(e.Rolagem.Total, Is.EqualTo(8));
            Assert.That(mesa.AceitarRolagem(e.Id, enfrentar, new[] { new EscolhaDaRolagem { Opcao = "oportunidade", Subopcao = 1 } },
                new TrocaDeGolpes { CondicaoDoAlvo = "irritado", Retorno = TrocaDeGolpes.RetornoCondicao, CondicaoDeQuemRolou = "assustado" }).Ok, Is.True);
            Assert.That(mesa.CondicoesMarcadasDoVilao(slade.Id), Does.Contain("irritado"));
            Assert.That(heroi.CondicoesMarcadas, Does.Contain("assustado"));
            Assert.That(mesa.Equipe, Is.EqualTo(1), "a oportunidade deu +1 Equipe");

            Assert.That(mesa.Corrigir(e.Id, "rolagem errada").Ok, Is.True);
            Assert.That(mesa.CondicoesMarcadasDoVilao(slade.Id), Is.Empty, "desfazer tira a condição do vilão");
            Assert.That(heroi.CondicoesMarcadas, Does.Not.Contain("assustado"));
            Assert.That(mesa.Equipe, Is.EqualTo(0));

            // Falha: +1 potencial para quem rolou, sem troca.
            var potencial = heroi.Potencial;
            mesa.RolarMovimento(heroi.Id, enfrentar, slade.Id, 1, 1, null);
            var falha = mesa.Eventos.Last();
            Assert.That(mesa.AceitarRolagem(falha.Id, enfrentar, null, null).Ok, Is.True);
            Assert.That(heroi.Potencial, Is.EqualTo(potencial + 1));
            Assert.That(falha.Descricao, Does.EndWith("falha: +1 potencial"));
        }

        [Test]
        public void Pvp_resistir_livra_quem_escolheu_e_impressionar_pode_dar_condicao_ao_alvo()
        {
            var colega = Colega();
            var basicos = TorreDeControle.Core.Regras.MovimentosBasicos.Carregar(new RegrasDeTeste());
            var enfrentar = basicos.ComRotulo.First(m => m.Id == "basico/enfrentar");
            var perigoso = personagens.ValorDeJogo(heroi, Mesa.CampoRotulo(Rotulos.Perigoso));

            // 9 (como Vértice): resistir; só o alvo toma golpe poderoso.
            mesa.RolarMovimento(heroi.Id, enfrentar, colega.Id, 4, 5 - perigoso, null);
            var e = mesa.Eventos.Last();
            Assert.That(mesa.AceitarRolagem(e.Id, enfrentar, new[] { new EscolhaDaRolagem { Opcao = "resistir" } }, null).Ok, Is.True);
            Assert.That(e.Rolagem.Troca.AlvoGolpePoderoso, Is.True);
            Assert.That(e.Rolagem.Troca.Retorno, Is.EqualTo(TrocaDeGolpes.RetornoIleso));

            // 9: condição no alvo PJ sem impressionar é recusada; com impressionar, entra na ficha dele.
            mesa.RolarMovimento(heroi.Id, enfrentar, colega.Id, 4, 5 - perigoso, null);
            var e2 = mesa.Eventos.Last();
            Assert.That(mesa.AceitarRolagem(e2.Id, enfrentar, new[] { new EscolhaDaRolagem { Opcao = "tirar" } },
                new TrocaDeGolpes { CondicaoDoAlvo = "inseguro" }).Ok, Is.False, "só com impressionar");
            Assert.That(mesa.AceitarRolagem(e2.Id, enfrentar, new[] { new EscolhaDaRolagem { Opcao = "impressionar", Subopcao = 1 } },
                new TrocaDeGolpes { CondicaoDoAlvo = "inseguro" }).Ok, Is.True);
            Assert.That(colega.CondicoesMarcadas, Does.Contain("inseguro"));
            Assert.That(heroi.CondicoesMarcadas, Does.Not.Contain("inseguro"), "a condição é do alvo, não de quem rolou");
            Assert.That(e2.Rolagem.Troca.Retorno, Is.EqualTo(TrocaDeGolpes.RetornoGolpePoderoso), "sem resistir, quem rolou também toma");
            Assert.That(e2.Descricao, Does.Contain($"{colega.NomeExibicao} toma golpe poderoso e marca Inseguro"));

            // Contra PJ, a oportunidade não dá +1 Equipe; a chance a um aliado vale.
            Assert.That(enfrentar.AlternativaValeContraPj("oportunidade", 1), Is.False);
            Assert.That(enfrentar.AlternativaValeContraPj("oportunidade", 0), Is.True);
            mesa.RolarMovimento(heroi.Id, enfrentar, colega.Id, 4, 5 - perigoso, null);
            var e3 = mesa.Eventos.Last();
            Assert.That(mesa.AceitarRolagem(e3.Id, enfrentar, new[] { new EscolhaDaRolagem { Opcao = "oportunidade", Subopcao = 1 } },
                new TrocaDeGolpes()).Ok, Is.False, "nada de farmar a pilha em PvP");
            var aceite = mesa.AceitarRolagem(e3.Id, enfrentar, new[] { new EscolhaDaRolagem { Opcao = "oportunidade", Subopcao = 0 } },
                new TrocaDeGolpes());
            Assert.That(aceite.Ok, Is.True, aceite.Erro);
            Assert.That(mesa.Equipe, Is.EqualTo(0));
        }

        [Test]
        public void Golpe_poderoso_soma_condicoes_sem_alvo_e_com_teto()
        {
            var basicos = TorreDeControle.Core.Regras.MovimentosBasicos.Carregar(new RegrasDeTeste());
            var golpe = basicos.Todos.First(m => m.Id == "basico/golpe-poderoso");
            Assert.That(golpe.SomaCondicoes && golpe.Invertido, Is.True);
            Assert.That(basicos.ComRotulo, Has.No.Member(golpe));

            Assert.That(mesa.RolarMovimento(heroi.Id, golpe, null, 3, 4, null).Ok, Is.True);
            Assert.That(mesa.Eventos.Last().Rolagem.Mod, Is.EqualTo(0), "zero condições: +0");
            mesa.AlternarCondicao(heroi.Id, "irritado", null);
            mesa.RolarMovimento(heroi.Id, golpe, null, 3, 4, null);
            var e = mesa.Eventos.Last();
            Assert.That(e.Rolagem.Mod, Is.EqualTo(1), "Toro com Irritada rola +1 (p.78)");
            Assert.That(e.Descricao, Does.StartWith("Tomar um golpe poderoso (condições +1)"));
            foreach (var c in new[] { "assustado", "culpado", "inseguro", "desesperancoso" })
                mesa.AlternarCondicao(heroi.Id, c, null);
            mesa.RolarMovimento(heroi.Id, golpe, null, 3, 4, null);
            Assert.That(mesa.Eventos.Last().Rolagem.Mod, Is.EqualTo(4), "cinco condições, teto +4");

            Assert.That(mesa.RolarMovimento(heroi.Id, golpe, Colega().Id, 3, 4, null).Ok, Is.False, "o golpe não tem contra quem");

            mesa.MudarEquipe(2, null, null);
            Assert.That(golpe.SemIntervencao, Is.True);
            Assert.That(mesa.Intervir(e.Id, new Intervencao { Quem = heroi.Id, DeOnde = Intervencao.DaPilha, Mod = 1, CustoEquipe = 1 }, golpe).Ok,
                Is.False, "golpe poderoso não aceita intervenção (decisão do Alan)");
            Assert.That(mesa.Equipe, Is.EqualTo(2), "a recusa não gasta a pilha");
        }

        [Test]
        public void Troca_pede_golpe_poderoso_e_a_rolagem_com_origem_cumpre_o_pedido()
        {
            var colega = Colega();
            var basicos = TorreDeControle.Core.Regras.MovimentosBasicos.Carregar(new RegrasDeTeste());
            var enfrentar = basicos.Todos.First(m => m.Id == "basico/enfrentar");
            var golpe = basicos.Todos.First(m => m.Id == "basico/golpe-poderoso");
            var perigoso = personagens.ValorDeJogo(heroi, Mesa.CampoRotulo(Rotulos.Perigoso));

            // PvP, 9, tirar: os dois tomam golpe poderoso.
            mesa.RolarMovimento(heroi.Id, enfrentar, colega.Id, 4, 5 - perigoso, null);
            var e = mesa.Eventos.Last();
            Assert.That(mesa.GolpesPedidos(e), Is.Empty, "pendente ainda não pede nada");
            mesa.AceitarRolagem(e.Id, enfrentar, new[] { new EscolhaDaRolagem { Opcao = "tirar" } }, new TrocaDeGolpes());
            Assert.That(mesa.GolpesPedidos(e), Is.EqualTo(new[] { heroi.Id, colega.Id }));

            Assert.That(mesa.RolarMovimento(heroi.Id, golpe, null, 2, 2, null, origem: e.Id).Ok, Is.True);
            var g = mesa.Eventos.Last();
            Assert.That(g.Rolagem.Origem, Is.EqualTo(e.Id));
            Assert.That(g.Descricao, Does.Contain($"pedido por: Enfrentar diretamente a ameaça de {heroi.NomeExibicao} → {colega.NomeExibicao}"));
            Assert.That(mesa.GolpesPedidos(e), Is.EqualTo(new[] { colega.Id }), "falta o do alvo");
            Assert.That(mesa.RolarMovimento(heroi.Id, golpe, null, 2, 2, null, origem: e.Id).Ok, Is.False, "já rolou esse");

            // Desfazer o golpe devolve o pedido.
            mesa.Corrigir(g.Id, "engano");
            Assert.That(mesa.GolpesPedidos(e), Is.EqualTo(new[] { heroi.Id, colega.Id }));

            // Resistir: quem rolou não deve golpe; a origem não serve para ele.
            mesa.RolarMovimento(heroi.Id, enfrentar, colega.Id, 4, 5 - perigoso, null);
            var e2 = mesa.Eventos.Last();
            mesa.AceitarRolagem(e2.Id, enfrentar, new[] { new EscolhaDaRolagem { Opcao = "resistir" } }, new TrocaDeGolpes());
            Assert.That(mesa.GolpesPedidos(e2), Is.EqualTo(new[] { colega.Id }));
            Assert.That(mesa.RolarMovimento(heroi.Id, golpe, null, 2, 2, null, origem: e2.Id).Ok, Is.False);
        }

        [Test]
        public void Aceitar_golpe_poderoso_marca_as_condicoes_escolhidas_e_falha_da_potencial()
        {
            var basicos = TorreDeControle.Core.Regras.MovimentosBasicos.Carregar(new RegrasDeTeste());
            var golpe = basicos.Todos.First(m => m.Id == "basico/golpe-poderoso");

            // 8 (Huma, p.191): aguentar a dor, duas condições à escolha.
            mesa.RolarMovimento(heroi.Id, golpe, null, 4, 4, null);
            var e = mesa.Eventos.Last();
            var aguentar = new EscolhaDaRolagem { Opcao = "aguentar", Condicoes = new List<string> { "irritado" } };
            Assert.That(mesa.AceitarRolagem(e.Id, golpe, new[] { aguentar }).Ok, Is.False, "são duas");
            aguentar.Condicoes = new List<string> { "irritado", "irritado" };
            Assert.That(mesa.AceitarRolagem(e.Id, golpe, new[] { aguentar }).Ok, Is.False, "diferentes");
            aguentar.Condicoes = new List<string> { "irritado", "inseguro" };
            var r = mesa.AceitarRolagem(e.Id, golpe, new[] { aguentar });
            Assert.That(r.Ok, Is.True, r.Erro);
            Assert.That(heroi.CondicoesMarcadas, Is.EquivalentTo(new[] { "irritado", "inseguro" }));
            Assert.That(e.Descricao, Does.Contain("Aguentar a dor: marcar duas condições [Irritado, Inseguro]"));
            mesa.Corrigir(e.Id, "engano");
            Assert.That(heroi.CondicoesMarcadas, Is.Empty, "desfazer tira as duas");

            // 10+: duas da lista de 7–9, com o aguentar dentro (condições sem repetir).
            mesa.RolarMovimento(heroi.Id, golpe, null, 5, 6, null);
            var forte = mesa.Eventos.Last();
            var duas = new EscolhaDaRolagem
            {
                Opcao = "duas_parcial",
                Dentro = new List<EscolhaDaRolagem> { new EscolhaDaRolagem { Opcao = "ceder" } },
            };
            Assert.That(mesa.AceitarRolagem(forte.Id, golpe, new[] { duas }).Ok, Is.False, "escolha 2 dentro");
            duas.Dentro.Add(new EscolhaDaRolagem { Opcao = "aguentar", Condicoes = new List<string> { "culpado", "assustado" } });
            r = mesa.AceitarRolagem(forte.Id, golpe, new[] { duas });
            Assert.That(r.Ok, Is.True, r.Erro);
            Assert.That(heroi.CondicoesMarcadas, Is.EquivalentTo(new[] { "culpado", "assustado" }));
            Assert.That(forte.Rolagem.Escolhas.Single().Dentro.Select(x => x.Opcao), Is.EqualTo(new[] { "ceder", "aguentar" }));

            // Com uma condição livre só, o aguentar marca a que há e a personagem sai de cena.
            mesa.AlternarCondicao(heroi.Id, "irritado", null);
            mesa.AlternarCondicao(heroi.Id, "inseguro", null);
            mesa.RolarMovimento(heroi.Id, golpe, null, 1, 2, null); // 3 + 4 = 7
            var ultima = mesa.Eventos.Last();
            Assert.That(ultima.Rolagem.Total, Is.EqualTo(7));
            r = mesa.AceitarRolagem(ultima.Id, golpe, new[] { new EscolhaDaRolagem { Opcao = "aguentar", Condicoes = new List<string> { "desesperancoso" } } });
            Assert.That(r.Ok, Is.True, r.Erro);
            Assert.That(ultima.Rolagem.Escolhas.Single().FaltaramCondicoes, Is.EqualTo(1));
            Assert.That(ultima.Descricao, Does.Contain("sai de cena"));

            // Falha: se mantém firme, +1 potencial (p.78).
            foreach (var c in heroi.CondicoesMarcadas.ToList())
                mesa.AlternarCondicao(heroi.Id, c, null);
            var potencial = heroi.Potencial;
            mesa.RolarMovimento(heroi.Id, golpe, null, 1, 2, null);
            var falha = mesa.Eventos.Last();
            Assert.That(mesa.AceitarRolagem(falha.Id, golpe, null).Ok, Is.True);
            Assert.That(heroi.Potencial, Is.EqualTo(potencial + 1));
        }

        [Test]
        public void Descontar_num_colega_pede_provocar_ou_influencia_tambem_dentro_da_lista()
        {
            var basicos = TorreDeControle.Core.Regras.MovimentosBasicos.Carregar(new RegrasDeTeste());
            var golpe = basicos.Todos.First(m => m.Id == "basico/golpe-poderoso");
            mesa.RolarMovimento(heroi.Id, golpe, null, 5, 6, null); // 11
            var e = mesa.Eventos.Last();
            EscolhaDaRolagem Duas(int? sub) => new EscolhaDaRolagem
            {
                Opcao = "duas_parcial",
                Dentro = new List<EscolhaDaRolagem> { new EscolhaDaRolagem { Opcao = "atacar_verbal", Subopcao = sub }, new EscolhaDaRolagem { Opcao = "ceder" } },
            };
            Assert.That(mesa.AceitarRolagem(e.Id, golpe, new[] { Duas(null) }).Ok, Is.False, "provocar ou Influência: marque uma");
            var r = mesa.AceitarRolagem(e.Id, golpe, new[] { Duas(1) });
            Assert.That(r.Ok, Is.True, r.Erro);
            Assert.That(e.Descricao, Does.Contain("Descontar num colega (Influência: infligir condição)"));
            Assert.That(heroi.CondicoesMarcadas, Is.Empty, "por enquanto é só marca narrativa");
        }

        [Test]
        public void Ceder_terreno_poe_o_lembrete_da_abertura_em_quem_ganha_e_desfazer_tira()
        {
            var basicos = TorreDeControle.Core.Regras.MovimentosBasicos.Carregar(new RegrasDeTeste());
            var golpe = basicos.Todos.First(m => m.Id == "basico/golpe-poderoso");
            Assert.That(golpe.LembreteNaOposicao("ceder"), Is.Not.Null);
            personagens.CriarNpc(sala.Id, "Doutor Ocaso", Maturidade.Adulto, Papel.Vilao);
            var ocaso = personagens.NpcsDaSala(sala.Id).Single();

            mesa.RolarMovimento(heroi.Id, golpe, null, 4, 4, null);
            var e = mesa.Eventos.Last();
            Assert.That(mesa.AceitarRolagem(e.Id, golpe, new[] { new EscolhaDaRolagem { Opcao = "ceder", Para = ocaso.Id } }).Ok,
                Is.False, "fora de cena não ganha lembrete");
            Assert.That(mesa.AceitarRolagem(e.Id, golpe, new[] { new EscolhaDaRolagem { Opcao = "ceder", Para = heroi.Id } }).Ok,
                Is.False, "quem rolou não ganha a própria abertura");
            mesa.AlternarEmCena(ocaso.Id);
            var r = mesa.AceitarRolagem(e.Id, golpe, new[] { new EscolhaDaRolagem { Opcao = "ceder", Para = ocaso.Id } });
            Assert.That(r.Ok, Is.True, r.Erro);
            Assert.That(mesa.BonusDe(ocaso.Id).Single().Contexto, Is.EqualTo($"abertura: {heroi.NomeExibicao} cedeu terreno"));
            Assert.That(e.Descricao, Does.Contain("[lembrete em Doutor Ocaso]"));

            mesa.Corrigir(e.Id, "engano");
            Assert.That(mesa.BonusDe(ocaso.Id), Is.Empty, "desfazer o golpe tira o lembrete junto");

            // "Ninguém": sem lembrete, o mestre anota à mão.
            mesa.RolarMovimento(heroi.Id, golpe, null, 4, 4, null);
            var e2 = mesa.Eventos.Last();
            Assert.That(mesa.AceitarRolagem(e2.Id, golpe, new[] { new EscolhaDaRolagem { Opcao = "ceder" } }).Ok, Is.True);
            Assert.That(mesa.BonusDe(ocaso.Id), Is.Empty);
        }

        [Test]
        public void Condicao_do_vilao_na_troca_deixa_o_movimento_dela_pendente_ate_resolver()
        {
            var basicos = TorreDeControle.Core.Regras.MovimentosBasicos.Carregar(new RegrasDeTeste());
            var enfrentar = basicos.Todos.First(m => m.Id == "basico/enfrentar");
            personagens.CriarNpc(sala.Id, "Doutor Ocaso", Maturidade.Adulto, Papel.Vilao);
            var ocaso = personagens.NpcsDaSala(sala.Id).Single();
            personagens.AlternarCondicaoDeVilao(ocaso.Id, "irritado");
            mesa.AlternarEmCena(ocaso.Id);

            mesa.RolarMovimento(heroi.Id, enfrentar, ocaso.Id, 6, 6, null);
            var e = mesa.Eventos.Last();
            Assert.That(mesa.CondicaoPedida(e), Is.Null, "pendente ainda não marcou nada");
            mesa.AceitarRolagem(e.Id, enfrentar, new[] { new EscolhaDaRolagem { Opcao = "resistir" }, new EscolhaDaRolagem { Opcao = "tirar" } },
                new TrocaDeGolpes { CondicaoDoAlvo = "irritado" });
            Assert.That(mesa.CondicaoPedida(e), Is.EqualTo((ocaso.Id, "irritado")));

            Assert.That(mesa.ResolverMovimentoDeCondicao(e.Id, "quebra o lustre em cima da Aurora").Ok, Is.True);
            var resolvido = mesa.Eventos.Last();
            Assert.That(resolvido.Personagem, Is.EqualTo(ocaso.Id));
            Assert.That(resolvido.Descricao, Is.EqualTo("movimento de condição (Irritado): quebra o lustre em cima da Aurora"));
            Assert.That(mesa.CondicaoPedida(e), Is.Null, "resolvido: o botão some");
            Assert.That(mesa.ResolverMovimentoDeCondicao(e.Id, null).Ok, Is.False);

            mesa.Corrigir(resolvido.Id, "engano");
            Assert.That(mesa.CondicaoPedida(e), Is.Not.Null, "desfazer devolve a pendência");
        }

        [Test]
        public void Condicao_marcada_pesa_menos_dois_so_nos_movimentos_dela()
        {
            var basicos = TorreDeControle.Core.Regras.MovimentosBasicos.Carregar(new RegrasDeTeste());
            var enfrentar = basicos.Todos.First(m => m.Id == "basico/enfrentar");
            var liberar = basicos.Todos.First(m => m.Id == "basico/liberar-poderes");
            var golpe = basicos.Todos.First(m => m.Id == "basico/golpe-poderoso");
            var alvo = Colega();
            var perigoso = personagens.ValorDeJogo(heroi, Mesa.CampoRotulo(Rotulos.Perigoso));
            var aberracao = personagens.ValorDeJogo(heroi, Mesa.CampoRotulo(Rotulos.Aberracao));
            mesa.AlternarCondicao(heroi.Id, "assustado", null);
            mesa.AlternarCondicao(heroi.Id, "irritado", null);

            mesa.RolarMovimento(heroi.Id, enfrentar, alvo.Id, 3, 4, null);
            var e = mesa.Eventos.Last();
            Assert.That(e.Rolagem.Mod, Is.EqualTo(Math.Max(Mesa.TetoMinimo, perigoso - 2)), "Assustado: −2 no Enfrentar; Irritado não pesa");
            Assert.That(e.Rolagem.Partes.Select(x => x.Origem), Is.EqualTo(new[] { "rotulo:perigoso", "condicao:assustado" }));
            Assert.That(e.Descricao, Does.Contain("Assustado −2"));

            mesa.RolarMovimento(heroi.Id, liberar, null, 3, 4, null);
            Assert.That(mesa.Eventos.Last().Rolagem.Mod, Is.EqualTo(aberracao), "nenhuma das duas pesa no liberar");

            mesa.RolarMovimento(heroi.Id, golpe, null, 3, 4, null);
            Assert.That(mesa.Eventos.Last().Rolagem.Mod, Is.EqualTo(2), "o golpe soma as condições, sem −2");
        }

        [Test]
        public void Regras_de_condicoes_dizem_onde_pesam_e_como_saem()
        {
            var regras = TorreDeControle.Core.Regras.RegrasDeCondicoes.Carregar(new RegrasDeTeste());
            Assert.That(regras.Penalidade, Is.EqualTo(-2));
            Assert.That(regras.MovimentosAfetados("assustado"), Is.EqualTo(new[] { "basico/enfrentar" }));
            Assert.That(regras.MovimentosAfetados("desesperancoso"), Is.EqualTo(new[] { "basico/liberar-poderes" }));
            Assert.That(regras.ComoRemover("inseguro"), Does.Contain("sem consultar a equipe"));
            foreach (var c in Condicoes.Todas)
                Assert.That(regras.MovimentosAfetados(c), Is.Not.Empty, c);
        }

        [Test]
        public void Rolagem_desfeita_nao_aceita_intervencao()
        {
            mesa.Rolar(null, 1, 2, 0, null);
            var e = mesa.Eventos.Last();
            Assert.That(e.Descricao, Is.EqualTo("rolou 2d6: 1 + 2 = 3 (6−)"));
            mesa.ApagarEvento(e.Id, desfazerEfeito: true);
            Assert.That(mesa.Pendente(e), Is.False);
            Assert.That(mesa.Rolar(null, 0, 7, 0, null).Ok, Is.False);
        }
    }
}
