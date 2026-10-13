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
            CompletarModulos(p);
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
            CompletarModulos(p);
            Assert.That(cadastro.Aprovar(p.Id).Ok, Is.True);
            Assert.That(p.Foto.Habilidades.Single().Nome, Is.EqualTo("Feitiçaria"));
            Assert.That(p.Foto.ExtrasEscolhas["nova/chamas|faiscas"], Has.Count.EqualTo(4));

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

        /// <summary>Preenche movimentos e extras seguindo as regras do próprio JSON (o mínimo pedido).</summary>
        void CompletarModulos(Personagem p)
        {
            var mov = cadastro.MovimentosDoArquetipo(p);
            foreach (var o in mov.Opcoes.Where(o => !mov.Obrigatorios.Contains(o.Id)).Take(mov.Escolher))
                cadastro.AlternarMovimento(p.Id, o.Id);
            var secoes = cadastro.ExtrasDoArquetipo(p).Select(e => (e.Id, e.Campos))
                .Concat(cadastro.MovimentosComSubsecao(p).Select(m => (CadastroPersonagens.ExtraDoMovimento(m.Id), m.Campos)))
                .ToList();
            foreach (var (extra, campos) in secoes)
            foreach (var c in campos)
            {
                switch (c.Tipo)
                {
                    case "lista":
                        foreach (var o in c.Opcoes.Take(c.Escolher ?? 1))
                        {
                            cadastro.AlternarOpcaoDeExtra(p.Id, extra, c.Id, o.Id);
                            if (c.Descrever)
                                cadastro.DefinirTextoDeExtra(p.Id, extra, c.Id, o.Id, "descrição");
                        }
                        break;
                    case "rotulo":
                        cadastro.AlternarOpcaoDeExtra(p.Id, extra, c.Id, c.Rotulos[0]);
                        break;
                    case "texto" when !c.NaSala:
                        cadastro.DefinirTextoDeExtra(p.Id, extra, c.Id, null, "texto");
                        break;
                    case "textos":
                        foreach (var v in c.Vagas.Take(c.Minimo))
                            cadastro.DefinirTextoDeExtra(p.Id, extra, c.Id, v.Id, "nome");
                        break;
                }
            }
        }

        [Test]
        public void Movimento_com_subsecao_cobra_e_desmarcar_apaga()
        {
            var p = Nova("farol");
            const string sortudo = "mov:vamos-sortudo";
            cadastro.AlternarMovimento(p.Id, "vamos-sortudo");
            Assert.That(cadastro.MovimentosComSubsecao(p).Select(m => m.Id), Is.EqualTo(new[] { "vamos-sortudo" }));
            Assert.That(cadastro.Validar(p).avisos, Has.Some.Contains("Vamos, Sortudo!"));

            cadastro.DefinirTextoDeExtra(p.Id, sortudo, "bicho", null, "Sortudo, um vira-lata de três patas");
            cadastro.AlternarOpcaoDeExtra(p.Id, sortudo, "movimentos", "basico/defender");
            Assert.That(cadastro.DefinirTextoDeExtra(p.Id, sortudo, "movimentos", "basico/provocar", "late").Ok, Is.False);
            cadastro.DefinirTextoDeExtra(p.Id, sortudo, "movimentos", "basico/defender", "pula na frente");
            Assert.That(cadastro.TextoDoExtra(p, sortudo, "movimentos", "basico/defender"), Is.EqualTo("pula na frente"));

            cadastro.AlternarMovimento(p.Id, "vamos-sortudo");
            Assert.That(p.Criacao.ExtrasTextos.Keys, Has.None.StartsWith(sortudo));
            Assert.That(p.Criacao.ExtrasEscolhas.Keys, Has.None.StartsWith(sortudo));
        }

        // ---------- módulos ----------

        [Test]
        public void Movimentos_escolhe_o_numero_do_arquetipo_e_janus_ja_tem_a_mascara()
        {
            var p = Nova("janus");
            Assert.That(cadastro.MovimentosNaFicha(p), Is.EqualTo(new[] { "a-mascara" }));
            Assert.That(cadastro.AlternarMovimento(p.Id, "a-mascara").Ok, Is.False);
            cadastro.AlternarMovimento(p.Id, "teia-perigosa");
            Assert.That(cadastro.Validar(p).avisos, Has.Some.Contains("Movimentos: escolha 2 (faltam 1)"));
            cadastro.AlternarMovimento(p.Id, "discreta");
            Assert.That(cadastro.AlternarMovimento(p.Id, "vou-te-salvar").Ok, Is.False);
            Assert.That(cadastro.MovimentosNaFicha(p), Has.Count.EqualTo(3));
        }

        [Test]
        public void Amaldicoada_e_nova_nao_escolhem_movimentos()
        {
            Assert.That(cadastro.Validar(Nova("amaldicoada")).avisos, Has.None.Contains("Movimentos"));
            Assert.That(cadastro.Validar(Nova("nova")).avisos, Has.None.Contains("Movimentos"));
        }

        [Test]
        public void Extras_cobram_o_que_a_ficha_pede()
        {
            var p = Nova("farol");
            Assert.That(cadastro.Validar(p).avisos, Has.Some.Contains("Impulsos"));
            CompletarModulos(p);
            Assert.That(cadastro.Validar(p).avisos, Has.None.Contains("Impulsos"));
            Assert.That(cadastro.AlternarOpcaoDeExtra(p.Id, "farol/impulsos", "marcados", "terminar-com-alguem").Ok, Is.False);
        }

        [Test]
        public void Rotulo_do_extra_escolhe_um_so_e_amor_da_bruta_e_da_sala()
        {
            var p = Nova("janus");
            cadastro.AlternarOpcaoDeExtra(p.Id, "janus/mascara", "rotulo", "perigoso");
            cadastro.AlternarOpcaoDeExtra(p.Id, "janus/mascara", "rotulo", "salvador");
            Assert.That(cadastro.Escolhas(p, "janus/mascara", "rotulo"), Is.EqualTo(new[] { "salvador" }));

            var bruta = Nova("bruta");
            Assert.That(cadastro.DefinirTextoDeExtra(bruta.Id, "bruta/coracao", "amor", null, "Hyperwoman").Ok, Is.False);
            Assert.That(cadastro.Validar(bruta).avisos, Has.None.Contains("Amor"));
        }

        [Test]
        public void Legado_pede_pelo_menos_dois_membros()
        {
            var p = Nova("legado");
            cadastro.DefinirTextoDeExtra(p.Id, "legado/membros", "membros", "aposentado-e-critico", "Shahin Attar");
            Assert.That(cadastro.Validar(p).avisos, Has.Some.Contains("faltam 1"));
            cadastro.DefinirTextoDeExtra(p.Id, "legado/membros", "membros", "maior-inimigo-ainda-a-solta", "Alma Demoníaca");
            Assert.That(cadastro.Validar(p).avisos, Has.None.Contains("Legado:"));
        }

        // ---------- crescimento (protótipo) ----------

        Personagem Concluida(string arquetipo)
        {
            var p = Nova(arquetipo);
            p.Status = StatusCriacao.Criado; // atalho: o teste é do Crescimento, não da criação
            p.Potencial = 70;                // potencial de sobra: 14 avanços
            return p;
        }

        [Test]
        public void Potencial_corrido_rende_um_avanco_a_cada_cinco()
        {
            var p = Nova("farol");
            Assert.That(cadastro.AjustarPotencial(p.Id, 1).Ok, Is.False, "potencial é do jogo");
            p.Status = StatusCriacao.Criado;

            for (var k = 0; k < 4; k++)
                cadastro.AjustarPotencial(p.Id, 1);
            Assert.That(cadastro.AvancosPendentes(p), Is.EqualTo(0));
            Assert.That(cadastro.AlternarAvanco(p.Id, CadastroPersonagens.Cima, 0).Ok, Is.False);

            cadastro.AjustarPotencial(p.Id, 1); // 5
            cadastro.AjustarPotencial(p.Id, 1); // 6: a marcação continua
            Assert.That(cadastro.AvancosPendentes(p), Is.EqualTo(1));
            Assert.That(cadastro.AlternarAvanco(p.Id, CadastroPersonagens.Cima, 0).Ok, Is.True);
            Assert.That(cadastro.AvancosPendentes(p), Is.EqualTo(0));
            Assert.That(cadastro.AlternarAvanco(p.Id, CadastroPersonagens.Cima, 1).Ok, Is.False);

            // Não tira o potencial que já virou avanço.
            Assert.That(cadastro.AjustarPotencial(p.Id, -2).Ok, Is.False);
            Assert.That(cadastro.AjustarPotencial(p.Id, -1).Ok, Is.True); // 5
        }

        [Test]
        public void Aposentar_encerra_a_carreira()
        {
            var p = Concluida("farol");
            for (var k = 0; k < 5; k++)
                cadastro.AlternarAvanco(p.Id, CadastroPersonagens.Cima, k);
            Assert.That(cadastro.CarreiraEncerrada(p), Is.False);
            cadastro.AlternarAvanco(p.Id, CadastroPersonagens.Baixo, 5); // "Aposente-se ou torne-se um modelo"
            Assert.That(cadastro.CarreiraEncerrada(p), Is.True);
        }

        [Test]
        public void Impulso_marcado_se_cumpre_com_um_beneficio()
        {
            var p = Nova("farol");
            cadastro.AlternarOpcaoDeExtra(p.Id, "farol/impulsos", "marcados", "salvar-a-vida-de-um-colega");
            Assert.That(cadastro.AlternarCumprido(p.Id, "farol/impulsos", "salvar-a-vida-de-um-colega").Ok, Is.False);

            CompletarModulos(p);
            p.Status = StatusCriacao.Criado; // atalho: o teste é do jogo
            p.Foto = new FotoCriacao { ExtrasEscolhas = p.Criacao.ExtrasEscolhas };
            Assert.That(cadastro.AlternarCumprido(p.Id, "farol/impulsos", "terminar-com-alguem").Ok, Is.False);
            Assert.That(cadastro.EscolherBeneficio(p.Id, "farol/impulsos", "salvar-a-vida-de-um-colega", "potencial").Ok, Is.False);

            cadastro.AlternarCumprido(p.Id, "farol/impulsos", "salvar-a-vida-de-um-colega");
            cadastro.EscolherBeneficio(p.Id, "farol/impulsos", "salvar-a-vida-de-um-colega", "potencial");
            Assert.That(cadastro.BeneficioEscolhido(p, "farol/impulsos", "salvar-a-vida-de-um-colega"), Is.EqualTo("potencial"));
        }

        // ---------- jogo dos extras (Amaldiçoada) ----------

        const string Perdicao = "amaldicoada/perdicao";
        const string Sinais = "amaldicoada/sinais";
        const string Santuario = "amaldicoada/santuario";

        Personagem AmaldicoadaEmJogo()
        {
            var p = Nova("amaldicoada");
            CompletarModulos(p); // pega "visoes-sombrias" como sinal inicial
            p.Status = StatusCriacao.Criado;
            p.Foto = new FotoCriacao { ExtrasEscolhas = p.Criacao.ExtrasEscolhas };
            return p;
        }

        [Test]
        public void Final_nao_se_escolhe_na_criacao()
        {
            var p = Nova("amaldicoada");
            Assert.That(cadastro.AlternarOpcaoDeExtra(p.Id, Sinais, "sinais", "sua-perdicao-chega").Ok, Is.False);
        }

        [Test]
        public void Trilha_da_perdicao_rende_um_sinal_a_cada_cinco()
        {
            var p = AmaldicoadaEmJogo();
            Assert.That(cadastro.TomadasAoTodo(p, Sinais, "sinais"), Has.Count.EqualTo(1));
            Assert.That(cadastro.AlternarEscolhaNoJogo(p.Id, Sinais, "sinais", "portal").Ok, Is.False);

            cadastro.AjustarContador(p.Id, Perdicao, "trilha", 5);
            Assert.That(cadastro.EscolhasPendentesNoJogo(p, Sinais, "sinais"), Is.EqualTo(1));
            Assert.That(cadastro.AlternarEscolhaNoJogo(p.Id, Sinais, "sinais", "visoes-sombrias").Ok, Is.False, "já veio da criação");
            Assert.That(cadastro.AlternarEscolhaNoJogo(p.Id, Sinais, "sinais", "portal").Ok, Is.True);
            Assert.That(cadastro.AjustarContador(p.Id, Perdicao, "trilha", -1).Ok, Is.False, "ponto que virou sinal");
        }

        [Test]
        public void Perdicao_chega_so_com_os_cinco_ou_pelo_avanco()
        {
            var p = AmaldicoadaEmJogo();
            cadastro.AjustarContador(p.Id, Perdicao, "trilha", 30);
            Assert.That(cadastro.AlternarEscolhaNoJogo(p.Id, Sinais, "sinais", "sua-perdicao-chega").Ok, Is.False);
            foreach (var s in new[] { "poderes-infinitos", "portal", "brilho-intenso", "fortalecida" })
                cadastro.AlternarEscolhaNoJogo(p.Id, Sinais, "sinais", s);
            Assert.That(cadastro.AlternarEscolhaNoJogo(p.Id, Sinais, "sinais", "sua-perdicao-chega").Ok, Is.True);

            var q = AmaldicoadaEmJogo();
            cadastro.AjustarContador(q.Id, Perdicao, "trilha", 5);
            q.Potencial = 70;
            for (var k = 0; k < 5; k++)
                cadastro.AlternarAvanco(q.Id, CadastroPersonagens.Cima, k);
            cadastro.AlternarAvanco(q.Id, CadastroPersonagens.Baixo, 1); // "Enfrente sua perdição nos seus termos"
            Assert.That(cadastro.AlternarEscolhaNoJogo(q.Id, Sinais, "sinais", "sua-perdicao-chega").Ok, Is.True);
        }

        [Test]
        public void Santuario_o_mestre_seleciona_preenche_e_conclui()
        {
            var p = AmaldicoadaEmJogo();
            cadastro.DefinirTextoDeJogo(p.Id, Santuario, "uso", "pedido", "Achar a Lady Faust");
            Assert.That(cadastro.AlternarCondicaoConcluida(p.Id, Santuario, "uso", "ajuda").Ok, Is.False);
            cadastro.AlternarCondicao(p.Id, Santuario, "uso", "ajuda");
            cadastro.DefinirTextoDeJogo(p.Id, Santuario, "uso", "ajuda", "Vespa");
            cadastro.AlternarCondicaoConcluida(p.Id, Santuario, "uso", "ajuda");
            Assert.That(cadastro.CondicaoConcluida(p, Santuario, "uso", "ajuda"), Is.True);
            Assert.That(cadastro.TextoDeJogo(p, Santuario, "uso", "pedido"), Is.EqualTo("Achar a Lady Faust"));

            cadastro.AlternarCondicao(p.Id, Santuario, "uso", "ajuda"); // tira: apaga conclusão e lacuna
            Assert.That(cadastro.CondicaoConcluida(p, Santuario, "uso", "ajuda"), Is.False);
            Assert.That(cadastro.TextoDeJogo(p, Santuario, "uso", "ajuda"), Is.Empty);
        }

        [Test]
        public void Modo_teste_nao_grava_e_desligar_desfaz()
        {
            var p = Concluida("farol");
            p.Potencial = 0;
            cadastro.AjustarPotencial(p.Id, 2); // real: 2

            cadastro.LigarTeste(p.Id);
            cadastro.AjustarPotencial(p.Id, 10);
            cadastro.AlternarAvanco(p.Id, CadastroPersonagens.Cima, 0);
            Assert.That(p.Potencial, Is.EqualTo(12));

            // O disco continua com a personagem real enquanto o teste está ligado.
            var doDisco = CadastroPersonagens.Carregar(armazenamento, RegrasDeTeste.Arquetipos()).Buscar(p.Id);
            Assert.That(doDisco.Potencial, Is.EqualTo(2));
            Assert.That(doDisco.AvancosMarcados, Is.Empty);

            cadastro.DesligarTeste(p.Id);
            Assert.That(cadastro.EmTeste(p.Id), Is.False);
            Assert.That(p.Potencial, Is.EqualTo(2));
            Assert.That(p.AvancosMarcados, Is.Empty);
            Assert.That(cadastro.Buscar(p.Id), Is.SameAs(p));
        }

        [Test]
        public void Todo_arquetipo_tem_oito_em_cima_e_seis_embaixo()
        {
            foreach (var a in Arquetipos.Todos)
            {
                var avancos = RegrasDeTeste.Arquetipos().Buscar(a.Id).Avancos;
                Assert.That(avancos.Cima, Has.Count.EqualTo(8), a.Id);
                Assert.That(avancos.Baixo, Has.Count.EqualTo(6), a.Id);
            }
        }

        [Test]
        public void Avanco_so_depois_de_concluir_a_criacao()
        {
            var p = Nova("farol");
            Assert.That(cadastro.AlternarAvanco(p.Id, CadastroPersonagens.Cima, 0).Ok, Is.False);
        }

        [Test]
        public void Baixo_libera_com_cinco_em_cima_e_trava_o_desmarcar()
        {
            var p = Concluida("farol");
            Assert.That(cadastro.AlternarAvanco(p.Id, CadastroPersonagens.Baixo, 1).Ok, Is.False);
            for (var k = 0; k < 5; k++)
                cadastro.AlternarAvanco(p.Id, CadastroPersonagens.Cima, k);
            Assert.That(cadastro.BaixoLiberado(p), Is.True);
            Assert.That(cadastro.AlternarAvanco(p.Id, CadastroPersonagens.Baixo, 1).Ok, Is.True);

            Assert.That(cadastro.AlternarAvanco(p.Id, CadastroPersonagens.Cima, 0).Ok, Is.False);
            cadastro.AlternarAvanco(p.Id, CadastroPersonagens.Cima, 5);
            Assert.That(cadastro.AlternarAvanco(p.Id, CadastroPersonagens.Cima, 0).Ok, Is.True);
        }

        [Test]
        public void Itens_repetidos_sao_casas_diferentes()
        {
            var p = Concluida("farol");
            cadastro.AlternarAvanco(p.Id, CadastroPersonagens.Cima, 0);
            Assert.That(cadastro.Marcado(p, CadastroPersonagens.Cima, 0), Is.True);
            Assert.That(cadastro.Marcado(p, CadastroPersonagens.Cima, 1), Is.False);
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
