using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TorreDeControle.Core;
using TorreDeControle.Core.Jogadores;
using TorreDeControle.Core.Personagens;
using UnityEngine;
using UnityEngine.UI;
using Regra = TorreDeControle.Core.Regras.RegrasArquetipos.RegraHabilidades;

namespace TorreDeControle.UI
{
    /// <summary>
    /// A ficha da personagem como um fichário: uma aba por seção (Identidade, Núcleo, Crescimento,
    /// Módulos). Em "novo" é o assistente de criação, com validação e Aprovar no rodapé.
    /// </summary>
    public sealed class PainelFicha : MonoBehaviour
    {
        enum Aba { Identidade, Historico, Nucleo, Crescimento, Modulos }

        static readonly (Aba aba, string nome)[] Abas =
        {
            (Aba.Identidade, "Identidade"),
            (Aba.Historico, "Histórico"),
            (Aba.Nucleo, "Núcleo"),
            (Aba.Crescimento, "Crescimento"),
            (Aba.Modulos, "Módulos"),
        };

        Personagem p;
        CadastroPersonagens cadastro;
        CadastroJogadores jogadores;
        Action aoFechar;
        Aba abaAtual = Aba.Identidade;

        TextMeshProUGUI titulo;
        TextMeshProUGUI subtitulo;
        TextMeshProUGUI status;
        RectTransform filaAbas;
        /// <summary>A moldura fixa da aba; dentro dela, a área rolável <see cref="conteudo"/>, refeita a cada troca.</summary>
        RectTransform moldura;
        RectTransform conteudo;
        RectTransform rodape;

        public static PainelFicha Criar(Transform pai)
        {
            var veu = Estilo.Rect("PainelFicha", pai);
            Estilo.Esticar(veu);
            veu.gameObject.AddComponent<Image>().color = Estilo.Veu;
            veu.gameObject.AddComponent<NavegacaoTab>();
            var painel = veu.gameObject.AddComponent<PainelFicha>();
            painel.Montar(veu);
            return painel;
        }

        public void Abrir(Personagem personagem, CadastroPersonagens cadastroPersonagens,
            CadastroJogadores cadastroJogadores, Action aoFecharFicha)
        {
            p = personagem;
            cadastro = cadastroPersonagens;
            jogadores = cadastroJogadores;
            aoFechar = aoFecharFicha;
            abaAtual = Aba.Identidade; // a ficha sempre abre pela Identidade
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Atualizar();
        }

        void Fechar()
        {
            gameObject.SetActive(false);
            aoFechar?.Invoke();
        }

        // ---------- montagem fixa ----------

        void Montar(RectTransform veu)
        {
            var cartao = Estilo.Rect("Cartao", veu);
            cartao.anchorMin = cartao.anchorMax = new Vector2(0.5f, 0.5f);
            cartao.sizeDelta = new Vector2(1680, 940);
            // Opaco: a ficha abre por cima da lista de personagens, que não deve vazar.
            Estilo.Moldura(cartao.gameObject, new Color(Estilo.Cartao.r, Estilo.Cartao.g, Estilo.Cartao.b, 1f), Estilo.Borda);

            var coluna = cartao.gameObject.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(56, 56, 34, 30);
            coluna.spacing = 12;
            coluna.childControlWidth = true;
            coluna.childControlHeight = true;
            coluna.childForceExpandHeight = false;

            var cabecalho = Estilo.Fila(cartao, 76);
            var titulos = Estilo.Rect("Titulos", cabecalho);
            Estilo.ColunaElastica(titulos);
            var pilha = titulos.gameObject.AddComponent<VerticalLayoutGroup>();
            pilha.childControlWidth = true;
            pilha.childControlHeight = true;
            pilha.childForceExpandHeight = false;
            pilha.childAlignment = TextAnchor.MiddleLeft;
            titulo = Estilo.Rotulo(titulos, string.Empty, 44, Estilo.Dourado, FontStyles.Bold);
            titulo.characterSpacing = 4;
            subtitulo = Estilo.Rotulo(titulos, string.Empty, 24, Estilo.TextoSuave);

            status = Estilo.Rotulo(cabecalho, string.Empty, 26, Estilo.Dourado, FontStyles.Bold);
            status.alignment = TextAlignmentOptions.Center;
            status.characterSpacing = 4;
            status.gameObject.AddComponent<LayoutElement>().preferredWidth = 220;
            Estilo.BotaoContorno(cabecalho, "Voltar", 190, Estilo.TextoSuave, Fechar);

            filaAbas = Estilo.Fila(cartao, 56, 8);

            moldura = Estilo.Rect("Moldura", cartao);
            var lc = moldura.gameObject.AddComponent<LayoutElement>();
            lc.flexibleHeight = 1;
            Estilo.Moldura(moldura.gameObject, new Color(0, 0, 0, 0.18f), Estilo.BordaApagada);
            var vc = moldura.gameObject.AddComponent<VerticalLayoutGroup>();
            vc.padding = new RectOffset(30, 30, 18, 18);
            vc.childControlWidth = true;
            vc.childControlHeight = true;
            vc.childForceExpandHeight = true;

            rodape = Estilo.Fila(cartao, 60);
        }

        // ---------- atualização ----------

        void Atualizar()
        {
            Cabecalho();
            MontarAbas();
            MontarConteudo();
            MontarRodape();
        }

        void Cabecalho()
        {
            var i = p.Identidade;
            titulo.text = p.NomeExibicao.ToUpperInvariant();
            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(i.Codinome) && !string.IsNullOrWhiteSpace(i.Nome))
                partes.Add(i.Nome);
            partes.Add(Arquetipos.Nome(i.Arquetipo));
            partes.Add(jogadores.Buscar(i.Jogador)?.Nome ?? "sem jogador");
            subtitulo.text = string.Join(" · ", partes);
            status.text = p.Status switch
            {
                StatusCriacao.Novo => "NOVO",
                StatusCriacao.Criado => "CRIADO",
                _ => "EM CARREIRA",
            };
        }

        void MontarAbas()
        {
            Limpar(filaAbas);
            foreach (var (aba, nome) in Abas)
            {
                var a = aba;
                if (aba == abaAtual)
                    Estilo.BotaoPrincipal(filaAbas, nome, 250, () => { });
                else
                    Estilo.BotaoContorno(filaAbas, nome, 250, Estilo.TextoSuave, () => TrocarAba(a));
            }
        }

        void TrocarAba(Aba aba)
        {
            abaAtual = aba;
            MontarAbas();
            MontarConteudo();
        }

        void MontarConteudo()
        {
            Limpar(moldura);
            conteudo = Estilo.ListaRolavel(moldura, 8);
            switch (abaAtual)
            {
                case Aba.Identidade: AbaIdentidade(); break;
                case Aba.Historico: AbaHistorico(); break;
                case Aba.Nucleo: AbaNucleo(); break;
                case Aba.Crescimento: AbaCrescimento(); break;
                case Aba.Modulos: AbaModulos(); break;
            }
        }

        void MontarRodape()
        {
            Limpar(rodape);
            var mensagem = Estilo.Rotulo(rodape, string.Empty, 22, Estilo.TextoSuave);
            mensagem.textWrappingMode = TextWrappingModes.Normal;
            Estilo.ColunaElastica(mensagem);

            if (p.Status != StatusCriacao.Novo)
            {
                var data = DateTime.TryParse(p.Foto?.AprovadaEm, out var d) ? d.ToString("dd/MM/yyyy HH:mm") : "?";
                mensagem.text = $"Foto da criação congelada em {data}.";
                mensagem.color = Estilo.Dourado;
                return;
            }

            var (erros, avisos) = cadastro.Validar(p);
            if (erros.Count > 0)
            {
                mensagem.text = string.Join("  ", erros);
                mensagem.color = Estilo.Erro;
            }
            else if (avisos.Count > 0)
            {
                mensagem.text = "Pendências: " + string.Join("  ", avisos);
            }
            else
            {
                mensagem.text = "Tudo certo para concluir a criação.";
                mensagem.color = Estilo.Dourado;
            }

            // Observações: válidas, só lembram (não entram na confirmação de aprovar).
            var observacoes = cadastro.Observacoes(p);
            if (observacoes.Count > 0)
            {
                var cor = ColorUtility.ToHtmlStringRGB(Estilo.TextoApagado);
                mensagem.text += $"   <color=#{cor}><i>Observação: {string.Join("  ", observacoes)}</i></color>";
            }

            // Com pendência não aprova: o botão aparece apagado, sem clique.
            if (erros.Count == 0 && avisos.Count == 0)
                Estilo.BotaoPrincipal(rodape, "Concluir criação", 320, Aprovar);
            else
                Estilo.BotaoContorno(rodape, "Concluir criação", 320, Estilo.TextoApagado, () => { }).interactable = false;
        }

        /// <summary>
        /// Só chega aqui sem pendências (o botão fica apagado). No laboratório é "concluir criação":
        /// a aprovação do mestre de verdade é pôr a personagem num slot de sala. Congela: pede confirmação.
        /// </summary>
        void Aprovar()
        {
            Estilo.Confirmar(transform,
                "Concluir a criação? A foto da criação será congelada: Rótulos, habilidades e arquétipo " +
                "passam a mudar só pelas regras do jogo (ou por ajuste do mestre, com motivo).",
                "Concluir",
                () => Concluir(cadastro.Aprovar(p.Id)));
        }

        void Concluir(Resultado resultado)
        {
            Atualizar();
            if (!resultado.Ok)
                Aviso(resultado.Erro);
        }

        void Aviso(string texto)
        {
            Limpar(rodape);
            var m = Estilo.Rotulo(rodape, texto, 22, Estilo.Erro);
            Estilo.ColunaElastica(m);
        }

        // ---------- aba Identidade ----------

        void AbaIdentidade()
        {
            var i = p.Identidade;
            var codinome = CampoRotulado("CODINOME", i.Codinome);
            var nome = CampoRotulado("NOME CIVIL", i.Nome);
            foreach (var campo in new[] { codinome, nome })
            {
                campo.onSubmit.AddListener(_ => SalvarIdentidade(nome.text, codinome.text, p.Identidade.Arquetipo, p.Identidade.Jogador));
                campo.onDeselect.AddListener(_ => SalvarIdentidade(nome.text, codinome.text, p.Identidade.Arquetipo, p.Identidade.Jogador));
            }

            var filaArquetipo = FilaRotulada("ARQUÉTIPO");
            Estilo.ListaSuspensa(filaArquetipo, 420,
                Arquetipos.Todos.Select(a => (a.Id, a.Nome)).ToList(), i.Arquetipo, TrocarArquetipo,
                editavel: p.Status == StatusCriacao.Novo);
            if (p.Status == StatusCriacao.Novo)
                Nota(filaArquetipo, "Trocar de arquétipo zera as escolhas de criação.");
            else
                Nota(filaArquetipo, "Depois de aprovada, trocar de arquétipo é um avanço.");

            var filaJogador = FilaRotulada("JOGADOR");
            var opcoes = new List<(string id, string nome)> { (string.Empty, "sem jogador") };
            foreach (var j in jogadores.Listar())
                if (j.Ativo || j.Id == i.Jogador)
                    opcoes.Add((j.Id, j.Ativo ? j.Nome : j.Nome + " (inativo)"));
            Estilo.ListaSuspensa(filaJogador, 420, opcoes, i.Jogador ?? string.Empty,
                id => SalvarIdentidade(p.Identidade.Nome, p.Identidade.Codinome, p.Identidade.Arquetipo, id));

            var vao = Estilo.Rect("Vao", conteudo);
            vao.gameObject.AddComponent<LayoutElement>().minHeight = 12;
            SecaoHabilidades();

            var vao2 = Estilo.Rect("Vao", conteudo);
            vao2.gameObject.AddComponent<LayoutElement>().minHeight = 12;
            SecaoTextosLivres();
        }

        // ---------- aparência e histórico: texto livre, a ficha só inspira ----------

        void SecaoTextosLivres()
        {
            var regra = cadastro.RegraDoArquetipo(p);

            var tags = regra == null ? string.Empty
                : "Tags da ficha (só inspiração):" + Itens(regra.Visual.Select(g => string.Join(", ", g)));
            SecaoComDica("APARÊNCIA", "dicas da ficha", tags);
            var aparencia = Estilo.AreaTexto(conteudo, "Como ela é, como se veste, o uniforme... escreva do seu jeito.", 130);
            aparencia.text = p.Identidade.Aparencia;

            SecaoComDica("BREVE RESUMO", null, null);
            var resumo = Estilo.AreaTexto(conteudo, "Quem é a personagem e de onde veio, do seu jeito.", 150);
            resumo.text = p.Identidade.Resumo;

            // Grava ao sair do campo (Enter aqui quebra linha). Sem redesenhar, por causa do Tab.
            aparencia.onDeselect.AddListener(_ => cadastro.AlterarTextos(p.Id, aparencia.text, resumo.text));
            resumo.onDeselect.AddListener(_ => cadastro.AlterarTextos(p.Id, aparencia.text, resumo.text));
        }

        // ---------- aba Módulos: o deck do arquétipo (movimentos) e os extras ----------

        void AbaModulos()
        {
            var emCriacao = p.Status == StatusCriacao.Novo;
            var mov = cadastro.MovimentosDoArquetipo(p);
            var naFicha = cadastro.MovimentosNaFicha(p);

            Secao("MOVIMENTOS DO ARQUÉTIPO", mov.Escolher == 0 && mov.Opcoes.Count == 0
                ? "não escolhe movimentos na criação: os extras abaixo fazem esse papel"
                : mov.Obrigatorios.Count > 0 ? "os marcados vêm com o arquétipo; escolha os outros" : null);
            if (mov.Opcoes.Count > 0)
            {
                if (emCriacao)
                    Contador($"escolha {mov.Escolher}", p.Criacao.Movimentos.Count, mov.Escolher);
                var grade = Grade();
                foreach (var o in mov.Opcoes)
                {
                    var id = o.Id;
                    var estado = mov.Obrigatorios.Contains(id) ? EstadoChip.Fixa
                        : !emCriacao ? (naFicha.Contains(id) ? EstadoChip.Fixa : EstadoChip.Bloqueada)
                        : p.Criacao.Movimentos.Contains(id) ? EstadoChip.Escolhida
                        : p.Criacao.Movimentos.Count >= mov.Escolher ? EstadoChip.Bloqueada : EstadoChip.Livre;
                    Chip(grade, o.Nome, estado, () => Aplicar(cadastro.AlternarMovimento(p.Id, id)));
                }
            }

            foreach (var extra in cadastro.ExtrasDoArquetipo(p))
            {
                var vao = Estilo.Rect("Vao", conteudo);
                vao.gameObject.AddComponent<LayoutElement>().minHeight = 10;
                Secao(extra.Nome.ToUpperInvariant(), extra.Nota);
                foreach (var c in extra.Campos)
                    CampoDeExtra(extra.Id, c, emCriacao);
            }
        }

        void CampoDeExtra(string extra, Core.Regras.RegrasArquetipos.CampoExtra c, bool emCriacao)
        {
            switch (c.Tipo)
            {
                case Core.Regras.RegrasArquetipos.CampoExtra.Lista:
                {
                    var escolhidas = cadastro.Escolhas(p, extra, c.Id);
                    var max = c.MaximoEscolhas;
                    if (emCriacao)
                        Contador(c.Titulo, escolhidas.Count, max);
                    else
                        Subtitulo(c.Titulo);
                    var grade = Grade();
                    foreach (var o in c.Opcoes)
                    {
                        var id = o.Id;
                        var estado = !emCriacao ? (escolhidas.Contains(id) ? EstadoChip.Fixa : EstadoChip.Bloqueada)
                            : escolhidas.Contains(id) ? EstadoChip.Escolhida
                            : escolhidas.Count >= max ? EstadoChip.Bloqueada : EstadoChip.Livre;
                        Chip(grade, o.Nome, estado, () => Aplicar(cadastro.AlternarOpcaoDeExtra(p.Id, extra, c.Id, id)));
                    }
                    break;
                }
                case Core.Regras.RegrasArquetipos.CampoExtra.Rotulo:
                {
                    var escolhido = cadastro.Escolhas(p, extra, c.Id).FirstOrDefault();
                    Subtitulo(c.Titulo);
                    var grade = Grade();
                    foreach (var r in c.Rotulos)
                    {
                        var id = r;
                        var estado = !emCriacao ? (escolhido == id ? EstadoChip.Fixa : EstadoChip.Bloqueada)
                            : escolhido == id ? EstadoChip.Escolhida : EstadoChip.Livre;
                        Chip(grade, Rotulos.Nome(r), estado, () => Aplicar(cadastro.AlternarOpcaoDeExtra(p.Id, extra, c.Id, id)));
                    }
                    break;
                }
                case Core.Regras.RegrasArquetipos.CampoExtra.Texto:
                    Subtitulo(c.NaSala ? $"{c.Titulo} ({NotaDaSala})" : c.Titulo);
                    CampoTextoDeExtra(extra, c, null, emCriacao);
                    break;
                case Core.Regras.RegrasArquetipos.CampoExtra.Textos:
                    Subtitulo(c.Titulo);
                    foreach (var vaga in c.Vagas)
                    {
                        var fila = FilaRotulada(vaga.Nome);
                        fila.GetComponentInChildren<TextMeshProUGUI>().textWrappingMode = TextWrappingModes.Normal;
                        var campo = CampoTextoDeExtra(extra, c, vaga.Id, emCriacao, fila);
                        Estilo.ColunaElastica(campo);
                    }
                    break;
                case Core.Regras.RegrasArquetipos.CampoExtra.Trilha:
                    Subtitulo(c.Titulo);
                    var trilha = Estilo.Fila(conteudo, 44, 10);
                    for (var k = 0; k < c.Tamanho; k++)
                        Selo(trilha, string.Empty, 56);
                    break;
            }
        }

        TMP_InputField CampoTextoDeExtra(string extra, Core.Regras.RegrasArquetipos.CampoExtra c, string vaga,
            bool emCriacao, Transform pai = null)
        {
            var campo = Estilo.CampoTexto(pai != null ? pai : conteudo, c.NaSala ? "na sala" : c.Titulo.ToLowerInvariant());
            campo.text = cadastro.TextoDoExtra(p, extra, c.Id, vaga);
            if (pai == null)
            {
                var layout = campo.gameObject.AddComponent<LayoutElement>();
                layout.minHeight = 50;
                layout.preferredHeight = 50;
            }
            if (c.NaSala || !emCriacao)
            {
                CampoDaSala(campo);
                return campo;
            }
            campo.onDeselect.AddListener(t => { cadastro.DefinirTextoDeExtra(p.Id, extra, c.Id, vaga, t); MontarRodape(); });
            campo.onSubmit.AddListener(t => { cadastro.DefinirTextoDeExtra(p.Id, extra, c.Id, vaga, t); MontarRodape(); });
            return campo;
        }

        void Subtitulo(string texto)
        {
            var t = Estilo.Rotulo(conteudo, texto, 20, Estilo.TextoSuave, FontStyles.Italic);
            t.gameObject.AddComponent<LayoutElement>().minHeight = 30;
        }

        // ---------- aba Crescimento (protótipo): as casas de avanço, marcáveis ----------

        enum EstadoAvanco { Marcado, Livre, Travado }

        void AbaCrescimento()
        {
            var avancos = cadastro.AvancosDoArquetipo(p);
            var emCriacao = p.Status == StatusCriacao.Novo;
            var emCima = cadastro.MarcadosEmCima(p);
            var baixoLiberado = cadastro.BaixoLiberado(p);

            Secao("AVANÇOS", emCriacao
                ? "liberam depois de concluir a criação"
                : $"marque ao ganhar um avanço  ·  {emCima}/{avancos.Cima.Count} marcados");
            for (var k = 0; k < avancos.Cima.Count; k++)
                LinhaAvanco(CadastroPersonagens.Cima, k, avancos.Cima[k].Texto, emCriacao);

            var vao = Estilo.Rect("Vao", conteudo);
            vao.gameObject.AddComponent<LayoutElement>().minHeight = 10;
            Secao("AVANÇOS TARDIOS", baixoLiberado
                ? "liberados"
                : $"liberam com {Core.Regras.RegrasArquetipos.ListaDeAvancos.ParaLiberarBaixo} marcados em cima");
            for (var k = 0; k < avancos.Baixo.Count; k++)
                LinhaAvanco(CadastroPersonagens.Baixo, k, avancos.Baixo[k].Texto, emCriacao || !baixoLiberado);

            var nota = Estilo.Rotulo(conteudo,
                "Protótipo: no jogo, os avanços são feitos pelo painel de controle do mestre; aqui é só a marca.", 20,
                Estilo.TextoApagado, FontStyles.Italic);
            nota.gameObject.AddComponent<LayoutElement>().minHeight = 34;
        }

        void LinhaAvanco(string lista, int indice, string texto, bool travadaPorRegra)
        {
            var marcado = cadastro.Marcado(p, lista, indice);
            // Marcado aparece marcado mesmo se a lista travou (ex.: desmarcou um de cima).
            var estado = marcado ? EstadoAvanco.Marcado : travadaPorRegra ? EstadoAvanco.Travado : EstadoAvanco.Livre;

            var fila = Estilo.Fila(conteudo, 44, 14);
            var fundo = fila.gameObject.AddComponent<Image>();
            fundo.color = new Color(1, 1, 1, 0);
            var botao = fila.gameObject.AddComponent<Button>();
            botao.targetGraphic = fundo;
            var cores = botao.colors;
            cores.highlightedColor = new Color(1, 1, 1, 0.06f);
            cores.disabledColor = new Color(1, 1, 1, 0);
            botao.colors = cores;
            botao.interactable = p.Status != StatusCriacao.Novo && estado != EstadoAvanco.Travado;
            botao.onClick.AddListener(() => Aplicar(cadastro.AlternarAvanco(p.Id, lista, indice)));
            if (botao.interactable)
                fila.gameObject.AddComponent<Tabulavel>();

            var caixa = Estilo.Rect("Caixa", fila);
            caixa.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
            var corCaixa = estado == EstadoAvanco.Marcado ? Estilo.Dourado : Estilo.Campo;
            var bordaCaixa = estado == EstadoAvanco.Travado ? Estilo.BordaApagada : Estilo.Dourado;
            Estilo.Moldura(caixa.gameObject, corCaixa, bordaCaixa).raycastTarget = false;

            var corTexto = estado == EstadoAvanco.Marcado ? Estilo.Texto
                : estado == EstadoAvanco.Livre ? Estilo.TextoSuave : Estilo.TextoApagado;
            var t = Estilo.Rotulo(fila, texto, 22, corTexto,
                estado == EstadoAvanco.Marcado ? FontStyles.Bold : FontStyles.Normal);
            Estilo.ColunaElastica(t);
        }

        // ---------- aba Histórico: as perguntas da ficha e a equipe ----------

        void AbaHistorico()
        {
            var regra = cadastro.RegraDoArquetipo(p);
            if (regra == null)
                return;

            // As perguntas de histórico da ficha, cada uma com a sua resposta (a validação cobra).
            SecaoComDica("PERGUNTAS DE HISTÓRICO", null, null);
            var respostas = p.Identidade.RespostasHistorico;
            for (var k = 0; k < regra.Historico.Count; k++)
            {
                var pergunta = Estilo.Rotulo(conteudo, regra.Historico[k], 22, Estilo.TextoSuave, FontStyles.Italic);
                pergunta.textWrappingMode = TextWrappingModes.Normal;
                var campo = Estilo.AreaTexto(conteudo, "resposta", 64);
                campo.text = k < respostas.Count ? respostas[k] : string.Empty;
                var indice = k;
                campo.onDeselect.AddListener(t => { cadastro.ResponderHistorico(p.Id, indice, t); MontarRodape(); });
            }

            SecaoEquipe(regra);
        }

        // ---------- equipe: depende da sala; fora dela aparece desativada, só para ler ----------

        const string NotaDaSala = "preenchido na sala, com a equipe";

        void SecaoEquipe(Core.Regras.RegrasArquetipos.RegraArquetipo regra)
        {
            var vao = Estilo.Rect("Vao", conteudo);
            vao.gameObject.AddComponent<LayoutElement>().minHeight = 12;
            Secao("EQUIPE", NotaDaSala);

            if (!string.IsNullOrEmpty(regra.Equipe.Pergunta))
            {
                var titulo = Estilo.Rotulo(conteudo, "Quando a equipe se reuniu pela primeira vez...", 22,
                    Estilo.TextoApagado, FontStyles.Bold);
                titulo.gameObject.AddComponent<LayoutElement>().minHeight = 30;
                var pergunta = Estilo.Rotulo(conteudo, regra.Equipe.Pergunta, 22, Estilo.TextoApagado, FontStyles.Italic);
                pergunta.textWrappingMode = TextWrappingModes.Normal;
                CampoDaSala(Estilo.AreaTexto(conteudo, "resposta da equipe (na sala)", 64));
            }

            if (regra.Equipe.Relacionamentos.Count > 0)
            {
                var titulo = Estilo.Rotulo(conteudo, "Relacionamentos", 22, Estilo.TextoApagado, FontStyles.Bold);
                titulo.gameObject.AddComponent<LayoutElement>().minHeight = 30;
                foreach (var frase in regra.Equipe.Relacionamentos)
                {
                    var fila = Estilo.Fila(conteudo, 50, 12);
                    var t = Estilo.Rotulo(fila, frase, 22, Estilo.TextoApagado, FontStyles.Italic);
                    t.textWrappingMode = TextWrappingModes.Normal;
                    Estilo.ColunaElastica(t);
                    var colega = Estilo.CampoTexto(fila, "colega (na sala)");
                    colega.gameObject.AddComponent<LayoutElement>().preferredWidth = 320;
                    CampoDaSala(colega);
                }
            }
        }

        static void CampoDaSala(TMP_InputField campo)
        {
            campo.interactable = false; // também sai do Tab (NavegacaoTab só para em interativos)
            var cores = campo.colors;
            cores.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            campo.colors = cores;
        }

        static string Itens(IEnumerable<string> itens) =>
            string.Concat(itens.Select(i => System.Environment.NewLine + "• " + i));

        void SecaoComDica(string nome, string rotuloDica, string dica)
        {
            var fila = Estilo.Fila(conteudo, 40);
            var t = Estilo.Rotulo(fila, nome, 22, Estilo.Dourado, FontStyles.Bold);
            t.characterSpacing = 5;
            Estilo.ColunaElastica(t);
            if (!string.IsNullOrEmpty(dica))
                Estilo.SeloDica(fila, rotuloDica, dica);
        }

        // ---------- habilidades (o modelo vem de arquetipos.json) ----------

        enum EstadoChip { Fixa, Escolhida, Livre, Bloqueada, Negada }

        void SecaoHabilidades()
        {
            var regra = cadastro.RegraDeHabilidades(p);
            var e = p.Criacao.Habilidades;
            var emCriacao = p.Status == StatusCriacao.Novo;
            Secao("HABILIDADES", regra.Nota);

            if (emCriacao)
            {
                switch (regra.Tipo)
                {
                    case Regra.Fixas:
                        var gradeFixas = Grade();
                        foreach (var f in regra.FixasDadas)
                            Chip(gradeFixas, f.Nome, EstadoChip.Fixa, () => { });
                        break;

                    case Regra.Lista:
                        var max = regra.MaximoEscolhas;
                        var alem = regra.FixasDadas.Count > 0 ? " além das que já vêm marcadas" : string.Empty;
                        Contador(regra.EscolherAte.HasValue ? $"escolha até {max}{alem}" : $"escolha {max}{alem}", e.Escolhidas.Count, max);
                        var grade = Grade();
                        // As que o arquétipo dá: marcadas e travadas, na mesma grade das escolhas.
                        foreach (var f in regra.FixasDadas)
                            Chip(grade, f.Nome, EstadoChip.Fixa, () => { });
                        foreach (var o in regra.Opcoes)
                        {
                            var id = o.Id;
                            var estado = e.Escolhidas.Contains(id) ? EstadoChip.Escolhida
                                : e.Escolhidas.Count >= max ? EstadoChip.Bloqueada : EstadoChip.Livre;
                            Chip(grade, o.Nome, estado, () => Aplicar(cadastro.AlternarHabilidade(p.Id, id)));
                        }
                        break;

                    case Regra.Linha:
                        var filaLinha = FilaRotulada("LINHA");
                        var opcoesLinha = new List<(string id, string nome)> { (string.Empty, "escolha a linha") };
                        opcoesLinha.AddRange(regra.Linhas.Select(l => (l.Id, l.Nome)));
                        Estilo.ListaSuspensa(filaLinha, 420, opcoesLinha, e.Linha ?? string.Empty,
                            id => Aplicar(cadastro.EscolherLinha(p.Id, id.Length == 0 ? null : id)));
                        var linha = regra.Linhas.FirstOrDefault(l => l.Id == e.Linha);
                        if (linha == null)
                            break;
                        Contador($"negue {regra.Negar} (clique para negar ou devolver)", e.Negadas.Count, regra.Negar);
                        var gradeLinha = Grade();
                        foreach (var poder in linha.Poderes)
                        {
                            var id = poder.Id;
                            // No legado, o que não foi negado é poder que a personagem TEM: dourado.
                            // Completas as negações, os que ficaram travam como marca (não cinza).
                            var estado = e.Negadas.Contains(id) ? EstadoChip.Negada
                                : e.Negadas.Count >= regra.Negar ? EstadoChip.Fixa : EstadoChip.Escolhida;
                            Chip(gradeLinha, poder.Nome, estado, () => Aplicar(cadastro.AlternarNegada(p.Id, id)));
                        }
                        break;

                    case Regra.Mentora:
                        foreach (var vaga in regra.Vagas)
                        {
                            var v = vaga.Id;
                            var fila = FilaRotulada(vaga.Nome.ToUpperInvariant());
                            var opcoes = new List<(string id, string nome)> { (string.Empty, "—") };
                            opcoes.AddRange(regra.Opcoes.Select(o => (o.Id, o.Nome)));
                            Estilo.ListaSuspensa(fila, 420, opcoes, e.Vagas.TryGetValue(v, out var atual) ? atual : string.Empty,
                                id => Aplicar(cadastro.DefinirVaga(p.Id, v, id.Length == 0 ? null : id)));
                        }
                        break;
                }
            }

            // Descrição só onde a ficha do arquétipo pede (Bruta: cada uma; Transformado: as escolhidas).
            // Depois de aprovada, a lista aparece sempre, só para leitura.
            var pede = regra.Descrever;
            if (emCriacao && pede == null)
                return;
            var naFicha = cadastro.HabilidadesAtuais(p)
                .Where(h => !emCriacao || pede == Regra.DescreverCada || !h.Fixa);
            foreach (var h in naFicha)
            {
                var fila = Estilo.Fila(conteudo, 50, 10);
                var nome = Estilo.Rotulo(fila, h.Nome, 22, h.Fixa ? Estilo.Dourado : Estilo.Texto, FontStyles.Bold);
                nome.textWrappingMode = TextWrappingModes.NoWrap;
                nome.overflowMode = TextOverflowModes.Ellipsis;
                nome.gameObject.AddComponent<LayoutElement>().preferredWidth = 460;

                if (!emCriacao)
                {
                    var d = Estilo.Rotulo(fila, h.Descricao, 22, Estilo.TextoSuave, FontStyles.Italic);
                    Estilo.ColunaElastica(d);
                    continue;
                }

                var campo = Estilo.CampoTexto(fila,
                    pede == Regra.DescreverEscolhidas ? "como a deixa grotesca" : "como se manifesta");
                campo.text = h.Descricao;
                Estilo.ColunaElastica(campo);
                var hid = h.Id;
                // Não redesenha: o Tab precisa achar o próximo campo vivo. Só o rodapé muda.
                campo.onSubmit.AddListener(t => { cadastro.DescreverHabilidade(p.Id, hid, t); MontarRodape(); });
                campo.onDeselect.AddListener(t => { cadastro.DescreverHabilidade(p.Id, hid, t); MontarRodape(); });
            }
        }

        void Aplicar(Resultado resultado)
        {
            MontarConteudo();
            MontarRodape();
            if (!resultado.Ok)
                Aviso(resultado.Erro);
        }

        void Contador(string regra, int atual, int maximo)
        {
            var fila = Estilo.Fila(conteudo, 30);
            var t = Estilo.Rotulo(fila, $"{regra}  ·  {atual}/{maximo}", 20,
                atual == maximo ? Estilo.Dourado : Estilo.TextoSuave, FontStyles.Italic);
            Estilo.ColunaElastica(t);
        }

        RectTransform Grade()
        {
            var grade = Estilo.Rect("Grade", conteudo);
            var g = grade.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(335, 46); // 4 colunas cabem com a barra de rolagem
            g.spacing = new Vector2(10, 10);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 4;
            return grade;
        }

        static void Chip(Transform pai, string texto, EstadoChip estado, UnityEngine.Events.UnityAction aoClicar)
        {
            var rect = Estilo.Rect("Chip " + texto, pai);
            var (fundo, borda, cor) = estado switch
            {
                EstadoChip.Fixa => (Estilo.DouradoEscuro, Estilo.Dourado, Estilo.Dourado),
                EstadoChip.Escolhida => (Estilo.Dourado, Estilo.Dourado, Estilo.Cartao),
                EstadoChip.Livre => (new Color(Estilo.Dourado.r, Estilo.Dourado.g, Estilo.Dourado.b, 0.08f), Estilo.Dourado, Estilo.Dourado),
                _ => (Estilo.Campo, Estilo.BordaApagada, Estilo.TextoApagado),
            };
            var imagem = Estilo.Moldura(rect.gameObject, fundo, borda);
            var botao = rect.gameObject.AddComponent<Button>();
            botao.targetGraphic = imagem;
            botao.interactable = estado != EstadoChip.Bloqueada && estado != EstadoChip.Fixa;
            if (estado == EstadoChip.Fixa)
            {
                // Travada, mas sem o cinza de "desligado": é uma marca que já vem com o arquétipo.
                var cores = botao.colors;
                cores.disabledColor = Color.white;
                botao.colors = cores;
            }
            else
            {
                botao.onClick.AddListener(aoClicar);
                rect.gameObject.AddComponent<Tabulavel>();
            }
            var rotulo = estado switch
            {
                EstadoChip.Negada => $"<s>{texto}</s>",
                EstadoChip.Fixa => texto,
                _ => texto,
            };
            var t = Estilo.Rotulo(rect, rotulo, 22, cor,
                estado == EstadoChip.Escolhida || estado == EstadoChip.Fixa ? FontStyles.Bold : FontStyles.Normal);
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            Estilo.Esticar(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(10, 0);
            t.rectTransform.offsetMax = new Vector2(-10, 0);
        }


        TMP_InputField CampoRotulado(string rotulo, string valor)
        {
            var fila = FilaRotulada(rotulo);
            var campo = Estilo.CampoTexto(fila, rotulo.ToLowerInvariant());
            campo.text = valor;
            Estilo.ColunaElastica(campo);
            return campo;
        }

        RectTransform FilaRotulada(string rotulo)
        {
            var fila = Estilo.Fila(conteudo, 58);
            var r = Estilo.Rotulo(fila, rotulo, 20, Estilo.TextoSuave, FontStyles.Bold);
            r.characterSpacing = 4;
            r.gameObject.AddComponent<LayoutElement>().preferredWidth = 240;
            return fila;
        }

        static void Nota(Transform fila, string texto)
        {
            var n = Estilo.Rotulo(fila, texto, 20, Estilo.TextoApagado, FontStyles.Italic);
            Estilo.ColunaElastica(n);
        }

        /// <summary>Grava sem redesenhar a aba (o Tab não pode perder o campo de destino); só o cabeçalho muda.</summary>
        void SalvarIdentidade(string nome, string codinome, string arquetipo, string jogador)
        {
            var i = p.Identidade;
            if (nome.Trim() == i.Nome && codinome.Trim() == i.Codinome && arquetipo == i.Arquetipo &&
                (jogador ?? string.Empty) == (i.Jogador ?? string.Empty))
                return;

            var resultado = cadastro.AlterarIdentidade(p.Id, nome, codinome, arquetipo, jogador);
            Cabecalho();
            if (!resultado.Ok)
            {
                MontarConteudo();
                Aviso(resultado.Erro);
            }
        }

        void TrocarArquetipo(string novo)
        {
            if (novo == p.Identidade.Arquetipo)
                return;

            void Aplicar()
            {
                var i = p.Identidade;
                cadastro.AlterarIdentidade(p.Id, i.Nome, i.Codinome, novo, i.Jogador);
                Atualizar();
            }

            if (!cadastro.TrocaDeArquetipoApaga(p))
            {
                Aplicar();
                return;
            }

            Estilo.Confirmar(transform,
                $"Trocar para {Arquetipos.Nome(novo)} apaga o que foi feito para " +
                $"{Arquetipos.Nome(p.Identidade.Arquetipo)}: +1 livre, habilidades e respostas de histórico. " +
                "Nome, codinome, jogador, aparência e resumo ficam. Continuar?",
                "Trocar",
                Aplicar,
                aoCancelar: MontarConteudo);
        }

        // ---------- aba Núcleo ----------

        void AbaNucleo()
        {
            Secao("RÓTULOS", p.Status == StatusCriacao.Novo
                ? "azul: valor do arquétipo  ·  dourado: com o +1 livre (máximo +3)"
                : "foto da criação");

            var atuais = cadastro.RotulosAtuais(p);
            foreach (var rotulo in Rotulos.Todos)
                LinhaRotulo(rotulo, atuais[rotulo]);

            var vao = Estilo.Rect("Vao", conteudo);
            vao.gameObject.AddComponent<LayoutElement>().minHeight = 6;

            Secao("CONDIÇÕES", "começam desmarcadas");
            var condicoes = Estilo.Fila(conteudo, 44, 10);
            foreach (var c in new[] { "Assustado", "Irritado", "Culpado", "Desesperançoso", "Inseguro" })
                Selo(condicoes, c, 230);

            Secao("POTENCIAL E HORA DA VERDADE", null);
            var outros = Estilo.Fila(conteudo, 44, 10);
            for (var k = 0; k < 5; k++)
                Selo(outros, string.Empty, 56);
            var vao2 = Estilo.Rect("Vao", outros);
            vao2.gameObject.AddComponent<LayoutElement>().preferredWidth = 40;
            Selo(outros, "Hora da Verdade: bloqueada", 400);

            // Influência é Núcleo (sobrevive à troca de arquétipo), mas a inicial se dá aos colegas: na sala.
            var regra = cadastro.RegraDoArquetipo(p);
            Secao("INFLUÊNCIA", NotaDaSala);
            var texto = regra?.Influencia?.Texto ?? string.Empty;
            var influencia = Estilo.Rotulo(conteudo, texto, 22, Estilo.TextoApagado, FontStyles.Italic);
            influencia.textWrappingMode = TextWrappingModes.Normal;
            influencia.gameObject.AddComponent<LayoutElement>().minHeight = 34;
        }

        void Secao(string nome, string legenda)
        {
            var fila = Estilo.Fila(conteudo, 36);
            var t = Estilo.Rotulo(fila, nome, 22, Estilo.Dourado, FontStyles.Bold);
            t.characterSpacing = 5;
            t.gameObject.AddComponent<LayoutElement>().preferredWidth = 520;
            if (legenda != null)
            {
                var l = Estilo.Rotulo(fila, legenda, 20, Estilo.TextoSuave, FontStyles.Italic);
                Estilo.ColunaElastica(l);
            }
        }

        void LinhaRotulo(string rotulo, int valor)
        {
            var fila = Estilo.Fila(conteudo, 48, 8);
            var nome = Estilo.Rotulo(fila, Rotulos.Nome(rotulo).ToUpperInvariant(), 24, Estilo.Texto, FontStyles.Bold);
            nome.characterSpacing = 3;
            nome.gameObject.AddComponent<LayoutElement>().preferredWidth = 240;

            var comBonus = p.Status == StatusCriacao.Novo && p.Criacao.BonusLivre == rotulo;
            for (var v = Rotulos.Minimo; v <= Rotulos.Maximo; v++)
            {
                var celula = Estilo.Rect("Valor", fila);
                celula.gameObject.AddComponent<LayoutElement>().preferredWidth = 96;
                var marcado = v == valor;
                var fundo = !marcado ? Estilo.Campo : comBonus ? Estilo.Dourado : Estilo.Azul;
                Estilo.Moldura(celula.gameObject, fundo, marcado ? fundo : Estilo.BordaApagada);
                var t = Estilo.Rotulo(celula, v > 0 ? "+" + v : v.ToString(), 26,
                    marcado ? (comBonus ? Estilo.Cartao : Color.white) : Estilo.TextoApagado,
                    marcado ? FontStyles.Bold : FontStyles.Normal);
                t.alignment = TextAlignmentOptions.Center;
                Estilo.Esticar(t.rectTransform);
            }

            var vao = Estilo.Rect("Vao", fila);
            vao.gameObject.AddComponent<LayoutElement>().preferredWidth = 24;

            if (p.Status != StatusCriacao.Novo)
                return;

            // Um só +1 livre: escolhido, os outros apagam; clicar no dourado libera para escolher outro.
            var r = rotulo;
            var outroEscolhido = p.Criacao.BonusLivre != null && !comBonus;
            if (comBonus)
                Estilo.BotaoPrincipal(fila, "+1 livre", 220, () => EscolherBonus(null));
            else if (cadastro.RotuloDoArquetipo(p, rotulo) + 1 > Rotulos.Maximo)
                Selo(fila, "já no máximo", 220);
            else if (outroEscolhido)
                Estilo.BotaoContorno(fila, "+1 livre", 220, Estilo.TextoApagado, () => { }).interactable = false;
            else
                Estilo.BotaoContorno(fila, "+1 livre", 220, Estilo.Dourado, () => EscolherBonus(r));
        }

        void EscolherBonus(string rotulo)
        {
            var resultado = cadastro.EscolherBonusLivre(p.Id, rotulo);
            MontarConteudo();
            MontarRodape();
            if (!resultado.Ok)
                Aviso(resultado.Erro);
        }

        static void Selo(Transform pai, string texto, float largura)
        {
            var rect = Estilo.Rect("Selo", pai);
            rect.gameObject.AddComponent<LayoutElement>().preferredWidth = largura;
            Estilo.Moldura(rect.gameObject, Estilo.Campo, Estilo.BordaApagada);
            var t = Estilo.Rotulo(rect, texto, 22, Estilo.TextoApagado);
            t.alignment = TextAlignmentOptions.Center;
            Estilo.Esticar(t.rectTransform);
        }

        void EmConstrucao(string oQueVem)
        {
            var t = Estilo.Rotulo(conteudo, "Em construção", 30, Estilo.TextoSuave, FontStyles.Bold);
            t.gameObject.AddComponent<LayoutElement>().minHeight = 44;
            var d = Estilo.Rotulo(conteudo, oQueVem, 24, Estilo.TextoApagado, FontStyles.Italic);
            d.textWrappingMode = TextWrappingModes.Normal;
        }

        static void Limpar(Transform alvo)
        {
            for (var k = alvo.childCount - 1; k >= 0; k--)
            {
                var filho = alvo.GetChild(k);
                // A borda (filetes) é parte da moldura do próprio alvo, não do conteúdo.
                if (filho.name == "Borda")
                    continue;
                Destroy(filho.gameObject);
            }
        }
    }
}
