using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TorreDeControle.Core;
using TorreDeControle.Core.Jogadores;
using TorreDeControle.Core.Personagens;
using UnityEngine;
using UnityEngine.EventSystems;
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
        enum Aba { Identidade, Historico, Nucleo, Crescimento, Modulos, Variaveis }

        static readonly (Aba aba, string nome)[] Abas =
        {
            (Aba.Identidade, "Identidade"),
            (Aba.Nucleo, "Núcleo"),
            (Aba.Historico, "História"),
            (Aba.Modulos, "Módulos"),
            (Aba.Crescimento, "Evolução"),
            (Aba.Variaveis, "Mestre"),
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
        RectTransform vagaTeste;

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

        /// <param name="nomeDaSala">Nome da campanha, quando a ficha é a instância de uma sala.</param>
        /// <param name="abertaPelaSala">
        /// A instância só volta para "novo" quando aberta de dentro da sala (pelo laboratório é só consulta).
        /// </param>
        public void Abrir(Personagem personagem, CadastroPersonagens cadastroPersonagens,
            CadastroJogadores cadastroJogadores, Action aoFecharFicha, string nomeDaSala = null,
            bool abertaPelaSala = false)
        {
            p = personagem;
            campanha = nomeDaSala;
            pelaSala = abertaPelaSala;
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
            // Fechar a ficha desliga o modo teste: o que o mestre testou é desfeito.
            cadastro.DesligarTeste(p.Id);
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
            var molduraRetrato = Estilo.Rect("Retrato", cabecalho);
            molduraRetrato.gameObject.AddComponent<LayoutElement>().preferredWidth = 76;
            Estilo.Moldura(molduraRetrato.gameObject, Estilo.Campo, Estilo.Dourado);
            var miolo = Estilo.Rect("Imagem", molduraRetrato);
            Estilo.Esticar(miolo);
            miolo.offsetMin = new Vector2(2, 2);
            miolo.offsetMax = new Vector2(-2, -2);
            retrato = miolo.gameObject.AddComponent<RawImage>();
            retrato.raycastTarget = false;
            proporcaoRetrato = miolo.gameObject.AddComponent<AspectRatioFitter>();
            proporcaoRetrato.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fotoRetrato = molduraRetrato.gameObject.AddComponent<FotoDaWeb>();
            avisoRetrato = Estilo.Rotulo(molduraRetrato, string.Empty, 12, Estilo.TextoSuave);
            avisoRetrato.gameObject.SetActive(false);
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
            // O status só mostra: voltar para novo / pôr em revisão é um botão na aba Mestre.
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

        /// <summary>Nome da sala, se a ficha aberta é a instância de uma; null no laboratório.</summary>
        string campanha;
        bool pelaSala;
        RawImage retrato;
        AspectRatioFitter proporcaoRetrato;
        FotoDaWeb fotoRetrato;
        TextMeshProUGUI avisoRetrato;

        /// <summary>A arte quadrada do arquétipo da personagem (null se não houver).</summary>
        Texture2D RetratoDoArquetipo => Retratos.Buscar(cadastro.RegraDoArquetipo(p)?.Retrato);

        /// <summary>
        /// Ficha do arquivo da sala (cópia tirada da vaga): só consulta. Nada se edita, sem modo teste
        /// nem Variáveis.
        /// </summary>
        bool Consulta => p.Sala != null && !p.Ativo;

        void Cabecalho()
        {
            var i = p.Identidade;
            var arte = RetratoDoArquetipo;
            var temFotoHeroica = !string.IsNullOrWhiteSpace(i.FotoHeroica);
            retrato.transform.parent.gameObject.SetActive(temFotoHeroica || arte != null);
            if (temFotoHeroica || arte != null)
                fotoRetrato.Mostrar(i.FotoHeroica, retrato, proporcaoRetrato, avisoRetrato,
                    temFotoHeroica ? null : arte);
            titulo.text = p.NomeExibicao.ToUpperInvariant();
            // Instância de sala: a campanha ao lado do nome. Laboratório: só o nome.
            if (p.Sala != null && !string.IsNullOrEmpty(campanha))
            {
                var onde = p.Ativo ? campanha : campanha + " · vaga liberada";
                titulo.text += $"   <size=60%><color=#{ColorUtility.ToHtmlStringRGB(Estilo.TextoSuave)}>(Campanha: {onde})</color></size>";
            }
            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(i.Codinome) && !string.IsNullOrWhiteSpace(i.Nome))
                partes.Add(i.Nome);
            partes.Add(Arquetipos.Nome(i.Arquetipo));
            partes.Add(jogadores.Buscar(i.Jogador)?.Nome ?? "sem jogador");
            subtitulo.text = string.Join(" · ", partes);
            // Lembrete em qualquer aba: o interruptor fica na aba Mestre.
            if (cadastro.EmTeste(p.Id))
                subtitulo.text += $"  <color=#{ColorUtility.ToHtmlStringRGB(Estilo.Azul)}>· modo teste ligado</color>";
            status.text = p.Status switch
            {
                StatusCriacao.Novo => "NOVO",
                StatusCriacao.EmRevisao => "EM REVISÃO",
                StatusCriacao.EmAprovacao => "EM APROVAÇÃO",
                StatusCriacao.Aposentado => "APOSENTADO",
                _ => "EM CARREIRA",
            };
        }

        /// <summary>A instância só vai para revisão aberta de dentro da sala (pelo laboratório é consulta).</summary>
        bool PodeReabrir => cadastro.MotivoParaNaoReabrir(p) == null && (p.Sala == null || pelaSala);

        /// <summary>
        /// Botão da aba Mestre: volta a ficha para "novo" (laboratório) ou põe a instância em revisão (sala).
        /// </summary>
        void Reabrir()
        {
            var mensagem = p.Sala == null
                ? "Voltar a ficha para \"novo\"? A foto da criação é descartada e a criação fica editável de novo."
                : $"Pôr {p.NomeExibicao} em revisão? Ela continua na vaga e guarda o que foi marcado em jogo; a foto " +
                  "da criação é descartada para refazer as escolhas. Ao concluir, volta a \"em aprovação\" e precisa " +
                  "ser aprovada de novo para a campanha.";
            Estilo.Confirmar(transform, mensagem, p.Sala == null ? "Voltar para novo" : "Pôr em revisão", () =>
            {
                var r = cadastro.ReabrirCriacao(p.Id);
                Atualizar();
                if (!r.Ok)
                    Aviso(r.Erro);
            });
        }

        void MontarAbas()
        {
            Limpar(filaAbas);
            if (Consulta && abaAtual == Aba.Variaveis)
                abaAtual = Aba.Identidade;
            foreach (var (aba, nome) in Abas)
            {
                if (Consulta && aba == Aba.Variaveis)
                    continue;
                var a = aba;
                // Variáveis é do mestre: aparece com outra cor para não se confundir com a ficha do jogador.
                var corContorno = aba == Aba.Variaveis ? Estilo.Azul : Estilo.TextoSuave;
                if (aba == abaAtual)
                    Estilo.BotaoPrincipal(filaAbas, nome, 240, () => { });
                else
                    Estilo.BotaoContorno(filaAbas, nome, 240, corContorno, () => TrocarAba(a));
            }
        }

        void TrocarAba(Aba aba)
        {
            abaAtual = aba;
            MontarAbas();
            MontarConteudo();
        }

        /// <summary>
        /// Refaz a aba. Com <paramref name="manterRolagem"/>, volta para o mesmo ponto da rolagem (escolher
        /// algo no meio da aba não pode jogar a tela para o topo).
        /// </summary>
        void MontarConteudo(bool manterRolagem = false)
        {
            var posicao = manterRolagem && conteudo != null ? conteudo.anchoredPosition.y : 0f;
            Limpar(moldura);
            conteudo = Estilo.ListaRolavel(moldura, 8);
            MontarAbaAtual();
            if (Consulta)
                foreach (var s in conteudo.GetComponentsInChildren<Selectable>(true))
                    if (!(s is Scrollbar))
                        s.interactable = false;
            if (manterRolagem && posicao > 0)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(moldura);
                LayoutRebuilder.ForceRebuildLayoutImmediate(conteudo);
                var viewport = (RectTransform)conteudo.parent;
                var maximo = Mathf.Max(0, conteudo.rect.height - viewport.rect.height);
                conteudo.anchoredPosition = new Vector2(conteudo.anchoredPosition.x, Mathf.Min(posicao, maximo));
            }
        }

        void MontarAbaAtual()
        {
            switch (abaAtual)
            {
                case Aba.Identidade: AbaIdentidade(); break;
                case Aba.Historico: AbaHistorico(); break;
                case Aba.Nucleo: AbaNucleo(); break;
                case Aba.Crescimento: AbaCrescimento(); break;
                case Aba.Modulos: AbaModulos(); break;
                case Aba.Variaveis: AbaVariaveis(); break;
            }
        }

        void MontarRodape()
        {
            Limpar(rodape);
            var mensagem = Estilo.Rotulo(rodape, string.Empty, 22, Estilo.TextoSuave);
            mensagem.textWrappingMode = TextWrappingModes.Normal;
            Estilo.ColunaElastica(mensagem);
            Estilo.BotaoContorno(rodape, "Exportar", 220, Estilo.TextoSuave, Exportar);
            if (!Consulta)
                Estilo.BotaoContorno(rodape, "Importar", 220, Estilo.TextoSuave, Importar);
            if (Consulta)
            {
                mensagem.text = "Arquivo da sala (vaga liberada): só consulta.";
                mensagem.color = Estilo.TextoApagado;
                return;
            }
            // O modo teste mora na aba Mestre (o rodapé fica para pendências e ações da ficha).
            detalheDoRodape = null;
            MensagemDoRodape(mensagem);
            // Uma linha só (cortada com reticências); o texto inteiro fica no balão, com o mouse em cima.
            mensagem.textWrappingMode = TextWrappingModes.NoWrap;
            mensagem.overflowMode = TextOverflowModes.Ellipsis;
            if (!string.IsNullOrEmpty(mensagem.text))
            {
                mensagem.raycastTarget = true;
                mensagem.gameObject.AddComponent<Dica>().Configurar(detalheDoRodape ?? mensagem.text);
            }
        }

        /// <summary>O texto inteiro do rodapé, para o balão (null = o próprio texto da linha).</summary>
        string detalheDoRodape;

        void MensagemDoRodape(TextMeshProUGUI mensagem)
        {
            // Instância em aprovação, aberta pela sala: completa o que é da equipe e o mestre aprova.
            if (p.Sala != null && p.Status == StatusCriacao.EmAprovacao && pelaSala)
            {
                mensagem.text = "Em aprovação na campanha: complete o que é da equipe e aprove para entrar em carreira.";
                mensagem.color = Estilo.Dourado;
                Estilo.BotaoPrincipal(rodape, "Aprovar para a campanha", 360, AprovarParaCampanha);
                return;
            }
            if (!p.EmCriacao)
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
                mensagem.text = $"{avisos.Count} pendência{(avisos.Count == 1 ? "" : "s")}: {avisos[0]}";
                detalheDoRodape = "Pendências:" + Itens(avisos);
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
                detalheDoRodape = (detalheDoRodape ?? mensagem.text) +
                                  System.Environment.NewLine + System.Environment.NewLine + "Observações:" + Itens(observacoes);
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
                "Concluir a criação? A foto da criação será congelada (Rótulos, habilidades e arquétipo) e a " +
                "ficha fica \"em aprovação\": o mestre a leva para a campanha, onde se completa o que é da equipe.",
                "Concluir",
                () => Concluir(cadastro.Aprovar(p.Id)));
        }

        /// <summary>
        /// Exporta a ficha em JSON para levar a outra máquina: grava em dados/exportacoes e copia o texto
        /// para a área de transferência.
        /// </summary>
        void Exportar()
        {
            var json = cadastro.Exportar(p.Id);
            if (json == null || Troca.Arquivos == null)
                return;
            string caminho;
            try
            {
                caminho = Troca.Arquivos.Exportar(p.NomeExibicao, json);
            }
            catch (Exception e)
            {
                Aviso("Não deu para exportar: " + e.Message);
                return;
            }
            GUIUtility.systemCopyBuffer = json;
            Estilo.Confirmar(transform,
                $"{p.NomeExibicao} exportada em\n<size=80%>{caminho}</size>\nO JSON também foi copiado (Ctrl+V para colar).",
                "Abrir a pasta",
                () => Application.OpenURL("file:///" + Troca.Arquivos.PastaExportacoes.Replace('\\', '/')));
        }

        /// <summary>
        /// O caminho de volta do Exportar: atualiza esta ficha com um JSON (da área de transferência ou de
        /// dados/importar). Confirma antes, e avisa se o JSON é de outra personagem.
        /// </summary>
        void Importar()
        {
            if (Troca.Arquivos == null)
                return;
            var opcoes = new List<(string, UnityEngine.Events.UnityAction, bool)>
            {
                ("Colar da área de transferência", () => ConfirmarImportar(GUIUtility.systemCopyBuffer), false),
            };
            foreach (var caminho in Troca.Arquivos.Importaveis())
            {
                var arquivo = caminho;
                opcoes.Add((System.IO.Path.GetFileName(arquivo), () => ConfirmarImportar(Troca.Arquivos.Ler(arquivo)), false));
            }
            opcoes.Add(("Abrir a pasta de importar", () =>
                Application.OpenURL("file:///" + Troca.Arquivos.PastaImportar.Replace('\\', '/')), true));
            Estilo.Escolher(transform, $"Importar por cima de {p.NomeExibicao}", opcoes.ToArray());
        }

        void ConfirmarImportar(string json)
        {
            var id = cadastro.IdNaTroca(json);
            if (id == null)
            {
                Aviso("Isso não é um JSON de personagem da Torre.");
                return;
            }
            var mensagem = id == p.Id
                ? $"Atualizar {p.NomeExibicao} com o JSON? A ficha é substituída pelo arquivo; ficam a sala, o jogador e o status daqui."
                : $"Esse JSON é de OUTRA personagem. Substituir a ficha de {p.NomeExibicao} por ele mesmo assim? " +
                  "Ficam a sala, o jogador, o status, a equipe e a Influência daqui.";
            Estilo.Confirmar(transform, mensagem, "Importar", () =>
            {
                var r = cadastro.Reimportar(p.Id, json);
                Atualizar();
                if (!r.Ok)
                    Aviso(r.Erro);
            });
        }

        void AprovarParaCampanha()
        {
            Estilo.Confirmar(transform,
                $"Aprovar {p.NomeExibicao} para a campanha {campanha}? Ela passa a \"em carreira\": joga na mesa, " +
                "e o que muda vem do painel do mestre.",
                "Aprovar",
                () => Concluir(cadastro.AprovarParaCampanha(p.Id)));
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

            // Topo: os quatro campos à esquerda, as duas fotos (links) à direita.
            var topo = Estilo.Fila(conteudo, 300, 18);
            topo.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
            var campos = Estilo.Rect("Campos", topo);
            Estilo.ColunaElastica(campos);
            var pilha = campos.gameObject.AddComponent<VerticalLayoutGroup>();
            pilha.spacing = 8;
            pilha.childControlWidth = true;
            pilha.childControlHeight = true;
            pilha.childForceExpandHeight = false;
            Fotos(topo);

            var codinome = CampoRotulado("CODINOME", i.Codinome, campos);
            var nome = CampoRotulado("NOME CIVIL", i.Nome, campos);
            foreach (var campo in new[] { codinome, nome })
            {
                campo.onSubmit.AddListener(_ => SalvarIdentidade(nome.text, codinome.text, p.Identidade.Arquetipo, p.Identidade.Jogador));
                campo.onDeselect.AddListener(_ => SalvarIdentidade(nome.text, codinome.text, p.Identidade.Arquetipo, p.Identidade.Jogador));
            }

            var filaArquetipo = FilaRotulada("ARQUÉTIPO", campos);
            var listaArquetipo = Estilo.ListaSuspensa(filaArquetipo, 420,
                Arquetipos.Todos.Select(a => (a.Id, a.Nome)).ToList(), i.Arquetipo, TrocarArquetipo,
                editavel: p.EmCriacao);
            // A nota vira balão com o mouse em cima (como as regras dos chips), para não apertar a linha.
            listaArquetipo.gameObject.AddComponent<Dica>().Configurar(p.EmCriacao
                ? "Trocar de arquétipo zera as escolhas de criação."
                : "Depois de aprovada, trocar de arquétipo é um avanço.");

            var filaJogador = FilaRotulada("JOGADOR", campos);
            var opcoes = new List<(string id, string nome)> { (string.Empty, "sem jogador") };
            foreach (var j in jogadores.Listar())
                if (j.Ativo || j.Id == i.Jogador)
                    opcoes.Add((j.Id, j.Ativo ? j.Nome : j.Nome + " (inativo)"));
            // Na sala, quem decide o jogador é o slot.
            var listaJogador = Estilo.ListaSuspensa(filaJogador, 420, opcoes, i.Jogador ?? string.Empty,
                id => SalvarIdentidade(p.Identidade.Nome, p.Identidade.Codinome, p.Identidade.Arquetipo, id),
                editavel: p.Sala == null);
            if (p.Sala != null)
                listaJogador.gameObject.AddComponent<Dica>().Configurar("Na sala, o jogador se troca na vaga.");

            var vao = Estilo.Rect("Vao", conteudo);
            vao.gameObject.AddComponent<LayoutElement>().minHeight = 12;
            Refazivel(SecaoHabilidades);

            var vao2 = Estilo.Rect("Vao", conteudo);
            vao2.gameObject.AddComponent<LayoutElement>().minHeight = 12;
            SecaoTextosLivres();
        }

        // ---------- fotos: links para páginas web (de herói e civil) ----------

        /// <summary>
        /// Dois quadros (de herói e civil), cada um com o campo do link embaixo. Com link, clicar no
        /// quadro abre a página no navegador. Os quadros se chamam "FotoHeroica" e "FotoCivil" para a
        /// prévia da imagem ser encaixada neles depois.
        /// </summary>
        void Fotos(Transform topo)
        {
            var i = p.Identidade;
            TMP_InputField heroica = null, civil = null;
            void Gravar()
            {
                var antes = (p.Identidade.FotoHeroica, p.Identidade.FotoCivil);
                var r = cadastro.AlterarFotos(p.Id, heroica.text, civil.text);
                if (!r.Ok)
                {
                    Aviso(r.Erro);
                    return;
                }
                // Link trocado volta a "aguardando": refaz a aba para o quadro mostrar. Sem mudança,
                // não refaz (o Tab precisa achar vivo o próximo campo).
                if (antes != (p.Identidade.FotoHeroica, p.Identidade.FotoCivil))
                {
                    Cabecalho();
                    MontarConteudo(manterRolagem: true);
                }
            }
            heroica = Foto(topo, "FotoHeroica", "FOTO DE HERÓI", i.FotoHeroica, i.FotoHeroicaAprovacao, true, Gravar,
                padrao: RetratoDoArquetipo);
            civil = Foto(topo, "FotoCivil", "FOTO CIVIL", i.FotoCivil, i.FotoCivilAprovacao, false, Gravar);
        }

        /// <param name="padrao">Sem link, mostra esta imagem (o retrato do arquétipo) no lugar de "sem foto".</param>
        TMP_InputField Foto(Transform topo, string nomeDoQuadro, string titulo, string link,
            AprovacaoFoto aprovacao, bool ehHeroica, Action aoMudar, Texture2D padrao = null)
        {
            var coluna = Estilo.Rect("Coluna " + nomeDoQuadro, topo);
            // Largura fixa: sem isto a coluna herda "esticável" do campo do link e divide a sobra com os
            // campos da esquerda, espremendo-os.
            var lcol = coluna.gameObject.AddComponent<LayoutElement>();
            lcol.layoutPriority = 2;
            lcol.minWidth = 200;
            lcol.preferredWidth = 200;
            lcol.flexibleWidth = 0;
            var v = coluna.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 6;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;

            var quadro = Estilo.Rect(nomeDoQuadro, coluna);
            var le = quadro.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 196;
            le.preferredHeight = 196;
            var temLink = !string.IsNullOrEmpty(link);
            // Aprovar é na instância da sala (o mestre aceita a foto para a campanha). No laboratório
            // a imagem só aparece.
            var naSala = p.Sala != null;
            var borda = !temLink ? Estilo.BordaApagada
                : !naSala || aprovacao == AprovacaoFoto.Aprovada ? Estilo.Dourado
                : aprovacao == AprovacaoFoto.Recusada ? Estilo.Erro
                : Estilo.TextoSuave;
            var fundo = Estilo.Moldura(quadro.gameObject, Estilo.Campo, borda);
            var botao = quadro.gameObject.AddComponent<Button>();
            botao.targetGraphic = fundo;
            botao.interactable = temLink;
            var cores = botao.colors;
            cores.disabledColor = Color.white;
            botao.colors = cores;
            if (temLink)
                botao.onClick.AddListener(() => Application.OpenURL(link));

            // A imagem do link, dentro do quadro (sem pegar o clique: o quadro abre o link).
            var area = Estilo.Rect("Imagem", quadro);
            Estilo.Esticar(area);
            area.offsetMin = new Vector2(4, 4);
            area.offsetMax = new Vector2(-4, -4);
            var miolo = Estilo.Rect("Miolo", area);
            var imagem = miolo.gameObject.AddComponent<RawImage>();
            imagem.raycastTarget = false;
            var proporcao = miolo.gameObject.AddComponent<AspectRatioFitter>();
            proporcao.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

            var usaPadrao = !temLink && padrao != null;
            var texto = Estilo.Rotulo(quadro, temLink || usaPadrao ? string.Empty : $"{titulo}\n\n<size=80%>sem foto</size>",
                18, temLink ? Estilo.TextoSuave : Estilo.TextoApagado, FontStyles.Bold);
            texto.alignment = TextAlignmentOptions.Center;
            texto.textWrappingMode = TextWrappingModes.Normal;
            texto.raycastTarget = false;
            Estilo.Esticar(texto.rectTransform);
            texto.rectTransform.offsetMin = new Vector2(10, 10);
            texto.rectTransform.offsetMax = new Vector2(-10, -10);
            if (temLink)
                quadro.gameObject.AddComponent<FotoDaWeb>().Mostrar(link, imagem, proporcao, texto, padrao);
            else if (usaPadrao)
            {
                imagem.texture = padrao;
                proporcao.aspectRatio = (float)padrao.width / Mathf.Max(1, padrao.height);
            }
            else
                imagem.enabled = false;

            var campo = Estilo.CampoTexto(coluna, "link .jpg / .png");
            campo.text = link;
            foreach (var t in new[] { (TextMeshProUGUI)campo.textComponent, (TextMeshProUGUI)campo.placeholder })
                t.fontSize = 18;
            var lc = campo.GetComponent<LayoutElement>() ?? campo.gameObject.AddComponent<LayoutElement>();
            lc.minHeight = 44;
            lc.preferredHeight = 44;
            campo.onSubmit.AddListener(_ => aoMudar());
            campo.onDeselect.AddListener(_ => aoMudar());

            // O olho do mestre, na sala: aprovar ou recusar (clicar de novo no escolhido volta a aguardar).
            if (temLink && naSala)
            {
                var botoes = Estilo.Fila(coluna, 40, 6);
                foreach (var (alvo, nome, cor) in new[]
                         {
                             (AprovacaoFoto.Aprovada, "Aprovar", Estilo.Dourado),
                             (AprovacaoFoto.Recusada, "Recusar", Estilo.Erro),
                         })
                {
                    var novo = aprovacao == alvo ? AprovacaoFoto.Aguardando : alvo;
                    UnityEngine.Events.UnityAction clicar = () =>
                    {
                        cadastro.AvaliarFoto(p.Id, ehHeroica, novo);
                        MontarConteudo(manterRolagem: true);
                    };
                    var b = aprovacao == alvo
                        ? Estilo.BotaoPrincipal(botoes, nome, 0, clicar)
                        : Estilo.BotaoContorno(botoes, nome, 0, cor, clicar);
                    b.GetComponent<LayoutElement>().flexibleWidth = 1;
                    var t = b.GetComponentInChildren<TextMeshProUGUI>();
                    t.fontSize = 16;
                    t.characterSpacing = 1;
                }
            }
            return campo;
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
            Estilo.ComExpandir(aparencia, "Aparência");
            Estilo.ComExpandir(resumo, "Breve resumo");
        }

        // ---------- aba Módulos: o deck do arquétipo (movimentos) e os extras ----------

        void AbaModulos()
        {
            // Dois blocos que se refazem sozinhos: clicar num movimento não refaz os extras, e vice-versa.
            Refazivel(SecaoMovimentos);
            Refazivel(SecaoExtras);
        }

        void SecaoMovimentos()
        {
            var emCriacao = p.EmCriacao;
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
                    Chip(grade, o.Nome, estado, () => Aplicar(cadastro.AlternarMovimento(p.Id, id)), o.Regra);
                }
            }

            // Observação livre do jogador em cada movimento da ficha (anotação: editável sempre).
            if (naFicha.Count > 0)
            {
                Subtitulo("Observações sobre os movimentos (opcional)");
                foreach (var o in mov.Opcoes.Where(o => naFicha.Contains(o.Id)))
                {
                    var id = o.Id;
                    var fila = FilaRotulada(o.Nome);
                    fila.GetComponentInChildren<TextMeshProUGUI>().textWrappingMode = TextWrappingModes.Normal;
                    var obs = Estilo.CampoTexto(fila, "observação (opcional)");
                    obs.text = cadastro.ObsDoMovimento(p, id);
                    Estilo.ColunaElastica(obs);
                    // Grava sem redesenhar (o Tab precisa achar o próximo campo vivo).
                    obs.onSubmit.AddListener(t => cadastro.AnotarMovimento(p.Id, id, t));
                    obs.onDeselect.AddListener(t => cadastro.AnotarMovimento(p.Id, id, t));
                    Estilo.ComExpandir(obs, $"{o.Nome}: observação");
                }
            }

            // Movimentos que abrem subseção (ex.: o equipamento, o bicho do "Vamos, Sortudo!").
            foreach (var m in cadastro.MovimentosComSubsecao(p))
            {
                var vao = Estilo.Rect("Vao", conteudo);
                vao.gameObject.AddComponent<LayoutElement>().minHeight = 10;
                Secao(m.Nome.ToUpperInvariant(), m.Regra);
                foreach (var c in m.Campos)
                    CampoDeExtra(CadastroPersonagens.ExtraDoMovimento(m.Id), c, emCriacao);
            }

        }

        void SecaoExtras()
        {
            var emCriacao = p.EmCriacao;
            foreach (var extra in cadastro.ExtrasDoArquetipo(p))
            {
                var vao = Estilo.Rect("Vao", conteudo);
                vao.gameObject.AddComponent<LayoutElement>().minHeight = 10;
                Secao(extra.Nome.ToUpperInvariant(), extra.Nota);
                foreach (var c in extra.Campos)
                {
                    if (c.Id == extra.CumpreCampo)
                        ListaDeCumprimento(extra, c, emCriacao);
                    else
                        CampoDeExtra(extra.Id, c, emCriacao);
                }
            }
        }

        /// <summary>
        /// Lista "de cumprir" (ex.: impulsos do Farol) em 2 colunas, uma linha por opção com tudo dela:
        /// [marcar] nome [cumprido] [benefício ▾]. Na criação só a marca funciona; no jogo (protótipo,
        /// depois o painel do mestre) a marca trava e os marcados ganham cumprido e benefício.
        /// </summary>
        void ListaDeCumprimento(Core.Regras.RegrasArquetipos.RegraExtra extra,
            Core.Regras.RegrasArquetipos.CampoExtra c, bool emCriacao)
        {
            var escolhidas = cadastro.Escolhas(p, extra.Id, c.Id);
            var max = c.MaximoEscolhas;
            if (emCriacao)
            {
                Contador(c.Titulo, escolhidas.Count, max);
                Subtitulo("Depois de concluir a criação, cada marcado ganha aqui na linha o \"cumprido\" e o benefício.");
            }
            else
            {
                Subtitulo("Cumprido e benefício: só leitura aqui; quem marca é o painel do mestre, no jogo.");
            }

            var grade = Estilo.Rect("Grade", conteudo);
            var g = grade.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(720, 48); // 2 colunas cabem com a barra de rolagem
            g.spacing = new Vector2(24, 6);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 2;

            var beneficios = new List<(string id, string nome)> { (string.Empty, "benefício") };
            beneficios.AddRange(extra.AoCumprir.Select(b => (b.Id, b.Nome)));

            foreach (var o in c.Opcoes)
            {
                var id = o.Id;
                var marcada = escolhidas.Contains(id);
                var cumprido = marcada && cadastro.Cumprido(p, extra.Id, id);

                var linha = Estilo.Rect("Linha " + o.Nome, grade);
                var h = linha.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 10;
                h.childControlWidth = true;
                h.childControlHeight = true;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = true;
                h.padding = new RectOffset(0, 0, 4, 4);

                var podeMarcar = emCriacao && (marcada || escolhidas.Count < max);
                Caixa(linha, marcada, podeMarcar, () => Aplicar(cadastro.AlternarOpcaoDeExtra(p.Id, extra.Id, c.Id, id)));

                var cor = marcada ? Estilo.Texto : podeMarcar ? Estilo.TextoSuave : Estilo.TextoApagado;
                var nome = Estilo.Rotulo(linha, o.Nome, 21, cor,
                    cumprido ? FontStyles.Strikethrough : marcada ? FontStyles.Bold : FontStyles.Normal);
                nome.textWrappingMode = TextWrappingModes.NoWrap;
                nome.overflowMode = TextOverflowModes.Ellipsis;
                Estilo.ColunaElastica(nome);

                if (emCriacao || !marcada)
                    continue;

                // Só leitura: quem cumpre e escolhe o benefício é o painel do mestre, durante o jogo.
                Caixa(linha, cumprido, false, () => { });
                var beneficio = beneficios.FirstOrDefault(b => b.id.Length > 0 && b.id == cadastro.BeneficioEscolhido(p, extra.Id, id)).nome;
                var rotuloBeneficio = Estilo.Rotulo(linha, cumprido ? beneficio ?? "sem benefício" : string.Empty, 20,
                    Estilo.Dourado, FontStyles.Italic);
                rotuloBeneficio.gameObject.AddComponent<LayoutElement>().preferredWidth = 240;
            }

            DescricoesDasEscolhidas(extra.Id, c, escolhidas, emCriacao);
        }

        /// <summary>
        /// Lista que pede descrição: cada escolhida ganha o seu campo (ex.: como o bicho ajuda, qual herói
        /// você admira). Texto por escolhida: todas (descrever / descrever_opcional) ou só as com lacuna.
        /// </summary>
        void DescricoesDasEscolhidas(string extra, Core.Regras.RegrasArquetipos.CampoExtra c,
            IReadOnlyList<string> escolhidas, bool emCriacao)
        {
            // Na ordem em que foram marcadas (a mesma do JSON), não na ordem da lista: preencher de cima
            // para baixo pensando na ordem da marcação trocava os textos de lugar.
            var naOrdem = escolhidas
                .Select(id => c.Opcoes.FirstOrDefault(o => o.Id == id))
                .Where(o => o != null && (c.Descrever || c.DescreverOpcional || o.Lacuna));
            foreach (var o in naOrdem)
            {
                var fila = FilaRotulada(o.Nome);
                fila.GetComponentInChildren<TextMeshProUGUI>().textWrappingMode = TextWrappingModes.Normal;
                var dica = o.Lacuna ? "complete o ___"
                    : c.Descrever ? c.Nota ?? "descreva"
                    : "descrição (opcional)";
                var campo = CampoTextoDeExtra(extra, c, o.Id, emCriacao, fila, dica, tituloExpandir: o.Nome);
                Estilo.ColunaElastica(campo);
            }
        }

        /// <summary>Caixinha de marcar (quadrado dourado quando marcada).</summary>
        static void Caixa(Transform pai, bool marcada, bool ativa, UnityEngine.Events.UnityAction aoClicar)
        {
            var rect = Estilo.Rect("Caixa", pai);
            rect.gameObject.AddComponent<LayoutElement>().preferredWidth = 38;
            var fundo = marcada ? Estilo.Dourado : Estilo.Campo;
            var borda = ativa || marcada ? Estilo.Dourado : Estilo.BordaApagada;
            var imagem = Estilo.Moldura(rect.gameObject, fundo, borda);
            var botao = rect.gameObject.AddComponent<Button>();
            botao.targetGraphic = imagem;
            botao.interactable = ativa;
            var cores = botao.colors;
            cores.disabledColor = Color.white; // travada mantém o desenho (marcada continua dourada)
            botao.colors = cores;
            if (ativa)
            {
                botao.onClick.AddListener(aoClicar);
                rect.gameObject.AddComponent<Tabulavel>();
            }
        }

        void CampoDeExtra(string extra, Core.Regras.RegrasArquetipos.CampoExtra c, bool emCriacao)
        {
            switch (c.Tipo)
            {
                case Core.Regras.RegrasArquetipos.CampoExtra.Lista when c.CresceNoJogo && !emCriacao:
                    ListaQueCresce(extra, c);
                    break;
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
                        // A opção final (ex.: "Sua perdição chega") nunca se escolhe na criação: riscada.
                        var nomeChip = id == c.Final ? $"<s>{o.Nome}</s>" : o.Nome;
                        if (id == c.Final)
                            estado = EstadoChip.Bloqueada;
                        Chip(grade, nomeChip, estado, () => Aplicar(cadastro.AlternarOpcaoDeExtra(p.Id, extra, c.Id, id)), o.Regra);
                    }

                    DescricoesDasEscolhidas(extra, c, escolhidas, emCriacao);
                    break;
                }
                case Core.Regras.RegrasArquetipos.CampoExtra.Rotulo:
                {
                    // Como na ficha impressa: uma fila de caixas de seleção, marca-se uma só.
                    var escolhido = cadastro.Escolhas(p, extra, c.Id).FirstOrDefault();
                    Subtitulo(c.Titulo);
                    var fila = Estilo.Fila(conteudo, 46, 12);
                    foreach (var r in c.Rotulos)
                    {
                        var id = r;
                        var marcado = escolhido == id;
                        Caixa(fila, marcado, emCriacao, () => Aplicar(cadastro.AlternarOpcaoDeExtra(p.Id, extra, c.Id, id)));
                        var nome = Estilo.Rotulo(fila, Rotulos.Nome(r), 22,
                            marcado ? Estilo.Texto : emCriacao ? Estilo.TextoSuave : Estilo.TextoApagado,
                            marcado ? FontStyles.Bold : FontStyles.Normal);
                        nome.gameObject.AddComponent<LayoutElement>().preferredWidth = 190;
                    }
                    break;
                }
                case Core.Regras.RegrasArquetipos.CampoExtra.Texto when c.NoJogo:
                    Subtitulo(c.Titulo);
                    TextoDeJogo(extra, c);
                    break;
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
                {
                    // Pontos corridos (como o potencial): as caixinhas mostram o caminho até o próximo múltiplo.
                    var valor = emCriacao ? 0 : cadastro.Contador(p, extra, c.Id);
                    var tamanho = c.Tamanho > 0 ? c.Tamanho : CadastroPersonagens.PontosPorEscolha;
                    var resto = valor % tamanho;
                    var cheias = resto == 0 && valor > 0 ? tamanho : resto;
                    Subtitulo(c.Nota != null ? $"{c.Titulo}  ·  {c.Nota}" : c.Titulo);
                    var trilha = Estilo.Fila(conteudo, 44, 10);
                    for (var k = 0; k < tamanho; k++)
                    {
                        var marca = Estilo.Rect("Marca", trilha);
                        marca.gameObject.AddComponent<LayoutElement>().preferredWidth = 30;
                        var dentro = Estilo.Rect("Dentro", marca);
                        dentro.anchorMin = dentro.anchorMax = new Vector2(0.5f, 0.5f);
                        dentro.sizeDelta = new Vector2(26, 26);
                        Estilo.Moldura(dentro.gameObject, k < cheias ? Estilo.Dourado : Estilo.Campo, Estilo.Dourado).raycastTarget = false;
                    }
                    break;
                }
                case Core.Regras.RegrasArquetipos.CampoExtra.Condicoes:
                    CondicoesSoLeitura(extra, c, !cadastro.LiberadoNoJogo(p));
                    break;
                case Core.Regras.RegrasArquetipos.CampoExtra.Registro:
                    RegistroSoLeitura(extra, c);
                    break;
                case Core.Regras.RegrasArquetipos.CampoExtra.Itens:
                    ItensSoLeitura(extra, c);
                    break;
                case Core.Regras.RegrasArquetipos.CampoExtra.Reserva:
                {
                    // Recurso de jogo (chamas do Nova): só aparece em sala ou no modo teste.
                    if (!cadastro.MostraRecursosDeJogo(p))
                    {
                        Subtitulo($"{c.Titulo}: aparecem na sala (ou no modo teste)");
                        break;
                    }
                    var fila = Estilo.Fila(conteudo, 46, 12);
                    var t = Estilo.Rotulo(fila, $"{c.Titulo}: {cadastro.Contador(p, extra, c.Id)}", 26, Estilo.Dourado, FontStyles.Bold);
                    t.gameObject.AddComponent<LayoutElement>().preferredWidth = 260;
                    var n = Estilo.Rotulo(fila, c.Nota ?? string.Empty, 20, Estilo.TextoApagado, FontStyles.Italic);
                    Estilo.ColunaElastica(n);
                    break;
                }
            }
        }

        /// <summary>
        /// Lista que cresce no jogo (sinais da perdição): a da criação fica travada; a cada escolha
        /// rendida pela trilha, o jogador toma mais uma. A opção final fica riscada até liberar.
        /// </summary>
        void ListaQueCresce(string extra, Core.Regras.RegrasArquetipos.CampoExtra c)
        {
            var daCriacao = cadastro.Escolhas(p, extra, c.Id);
            var doJogo = cadastro.EscolhasNoJogo(p, extra, c.Id);
            var pendentes = cadastro.EscolhasPendentesNoJogo(p, extra, c.Id);
            var tomadas = daCriacao.Count + doJogo.Count;
            var direito = daCriacao.Count + cadastro.EscolhasRendidas(p, extra, c.Id);
            Contador(pendentes > 0 ? $"{c.Titulo}: escolha o novo" : c.Titulo, tomadas, direito);

            var finalLiberado = cadastro.FinalLiberado(p, extra, c);
            var grade = Grade();
            foreach (var o in c.Opcoes)
            {
                var id = o.Id;
                var ehFinal = id == c.Final;
                var estado = daCriacao.Contains(id) ? EstadoChip.Fixa
                    : doJogo.Contains(id) ? EstadoChip.Escolhida
                    : ehFinal && !finalLiberado ? EstadoChip.Bloqueada
                    : pendentes > 0 ? EstadoChip.Livre : EstadoChip.Bloqueada;
                var nome = ehFinal && !finalLiberado ? $"<s>{o.Nome}</s>" : o.Nome;
                Chip(grade, nome, estado, () => Aplicar(cadastro.AlternarEscolhaNoJogo(p.Id, extra, c.Id, id)), o.Regra);
            }
        }

        /// <summary>Condições do jogo (uso do santuário): só leitura aqui; o mestre mexe na aba Variáveis.</summary>
        void CondicoesSoLeitura(string extra, Core.Regras.RegrasArquetipos.CampoExtra c, bool emCriacao)
        {
            if (emCriacao)
            {
                Subtitulo($"{c.Titulo}: no jogo, o mestre marca as condições");
                return;
            }
            var pedido = cadastro.TextoDeJogo(p, extra, c.Id, "pedido");
            if (c.Pedido)
                Subtitulo(string.IsNullOrEmpty(pedido) ? $"{c.Titulo}: nenhum pedido em andamento" : $"{c.Titulo}: {pedido}");
            else
                Subtitulo(c.Nota != null ? $"{c.Titulo}  ·  {c.Nota}" : c.Titulo);
            var selecionadas = cadastro.EscolhasNoJogo(p, extra, c.Id);
            if (selecionadas.Count == 0)
                Subtitulo("nada pendente");
            foreach (var o in c.Opcoes.Where(o => selecionadas.Contains(o.Id)))
            {
                var concluida = cadastro.CondicaoConcluida(p, extra, c.Id, o.Id);
                var fila = Estilo.Fila(conteudo, 44, 12);
                Caixa(fila, concluida, false, () => { });
                var texto = TextoComLacuna(o, cadastro.TextoDeJogo(p, extra, c.Id, o.Id));
                var t = Estilo.Rotulo(fila, texto, 22, concluida ? Estilo.TextoSuave : Estilo.Texto,
                    concluida ? FontStyles.Strikethrough : FontStyles.Normal);
                Estilo.ColunaElastica(t);
            }
        }

        /// <summary>Registro (ex.: usos do controle): lista dos lançamentos, só leitura.</summary>
        void RegistroSoLeitura(string extra, Core.Regras.RegrasArquetipos.CampoExtra c)
        {
            if (!cadastro.MostraRecursosDeJogo(p))
            {
                Subtitulo($"{c.Titulo}: aparecem na sala (ou no modo teste)");
                return;
            }
            var lancamentos = cadastro.ItensNoJogo(p, extra, c.Id);
            Subtitulo(lancamentos.Count == 0 ? $"{c.Titulo}: nenhum ainda" : c.Titulo);
            foreach (var l in lancamentos)
            {
                var o = c.Opcoes.FirstOrDefault(x => x.Id == l.Opcao);
                var texto = o == null ? l.Texto : string.IsNullOrEmpty(l.Texto) ? o.Nome : $"{o.Nome}: {l.Texto}";
                var t = Estilo.Rotulo(conteudo, "• " + texto, 22, Estilo.Texto);
                t.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        /// <summary>Itens com cargas (ex.: dispositivos): nome e cargas; apagados aparecem riscados.</summary>
        void ItensSoLeitura(string extra, Core.Regras.RegrasArquetipos.CampoExtra c)
        {
            if (!cadastro.MostraRecursosDeJogo(p))
            {
                Subtitulo($"{c.Titulo}: aparecem na sala (ou no modo teste)");
                return;
            }
            var itens = cadastro.ItensNoJogo(p, extra, c.Id);
            Subtitulo(itens.Count == 0 ? $"{c.Titulo}: nenhum ainda" : $"{c.Titulo}  ·  {c.Nota}");
            foreach (var i in itens)
            {
                var apagou = i.Cargas == 0;
                var t = Estilo.Rotulo(conteudo, $"• {i.Texto}  ({(apagou ? "apagou" : $"{i.Cargas} carga(s)")})", 22,
                    apagou ? Estilo.TextoApagado : Estilo.Texto, apagou ? FontStyles.Strikethrough : FontStyles.Normal);
                t.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        static string TextoComLacuna(Core.Regras.RegrasArquetipos.Opcao o, string preenchido) =>
            o.Lacuna && !string.IsNullOrEmpty(preenchido) ? o.Nome.Replace("___", preenchido) : o.Nome;

        TMP_InputField CampoTextoDeExtra(string extra, Core.Regras.RegrasArquetipos.CampoExtra c, string vaga,
            bool emCriacao, Transform pai = null, string dica = null, string tituloExpandir = null)
        {
            var alvo = pai != null ? pai : conteudo;
            var marcador = c.NaSala ? "na sala" : dica ?? c.Titulo.ToLowerInvariant();
            // Texto livre de várias linhas (ex.: descrição da nave): área, grava ao sair.
            var campo = c.VariasLinhas && pai == null ? Estilo.AreaTexto(alvo, marcador, 120) : Estilo.CampoTexto(alvo, marcador);
            campo.text = cadastro.TextoDoExtra(p, extra, c.Id, vaga);
            if (pai == null && !c.VariasLinhas)
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
            campo.onValueChanged.AddListener(t => cadastro.DefinirTextoDeExtra(p.Id, extra, c.Id, vaga, t));
            campo.onDeselect.AddListener(t => { cadastro.DefinirTextoDeExtra(p.Id, extra, c.Id, vaga, t); MontarRodape(); });
            campo.onSubmit.AddListener(t => { cadastro.DefinirTextoDeExtra(p.Id, extra, c.Id, vaga, t); MontarRodape(); });
            Estilo.ComExpandir(campo, tituloExpandir ?? c.Titulo);
            return campo;
        }

        /// <summary>
        /// Texto preenchido no jogo (ex.: o equipamento do Farol). Na criação, apagado; no jogo, só leitura —
        /// quem escreve é o painel do mestre. Até o painel existir, o modo teste deixa escrever.
        /// </summary>
        void TextoDeJogo(string extra, Core.Regras.RegrasArquetipos.CampoExtra c)
        {
            var valor = cadastro.TextoDeJogo(p, extra, c.Id, CadastroPersonagens.SubTexto);
            var campo = Estilo.CampoTexto(conteudo, cadastro.LiberadoNoJogo(p) ? "nada ainda (o painel do mestre registra)" : "preenchido no jogo");
            campo.text = valor;
            var layout = campo.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 50;
            layout.preferredHeight = 50;
            if (!cadastro.EmTeste(p.Id) || !cadastro.LiberadoNoJogo(p))
            {
                CampoDaSala(campo);
                return;
            }
            campo.onDeselect.AddListener(t => cadastro.DefinirTextoDeJogo(p.Id, extra, c.Id, CadastroPersonagens.SubTexto, t));
            campo.onSubmit.AddListener(t => cadastro.DefinirTextoDeJogo(p.Id, extra, c.Id, CadastroPersonagens.SubTexto, t));
        }

        void Subtitulo(string texto)
        {
            var t = Estilo.Rotulo(conteudo, texto, 20, Estilo.TextoSuave, FontStyles.Italic);
            t.gameObject.AddComponent<LayoutElement>().minHeight = 30;
        }

        // ---------- aba Variáveis: só do mestre (o que pertence à ficha mas o jogador não vê) ----------

        void AbaVariaveis()
        {
            // O interruptor do modo teste (playground do mestre), no topo da aba do mestre.
            var filaTeste = Estilo.Fila(conteudo, 60);
            vagaTeste = Estilo.Rect("ModoTeste", filaTeste);
            vagaTeste.gameObject.AddComponent<LayoutElement>().preferredWidth = 330;
            BotaoModoTeste();
            var explica = Estilo.Rotulo(filaTeste,
                "ligado, tudo o que mudar é só para testar a ficha: desligar desfaz", 20, Estilo.TextoSuave, FontStyles.Italic);
            explica.textWrappingMode = TextWrappingModes.Normal;
            Estilo.ColunaElastica(explica);

            // Corrigir a criação: no laboratório volta a novo; na sala, vai para revisão (fica na vaga).
            if (PodeReabrir)
            {
                var filaRevisao = Estilo.Fila(conteudo, 60);
                Estilo.BotaoContorno(filaRevisao, p.Sala == null ? "Voltar para novo" : "Pôr em revisão", 330,
                    Estilo.Dourado, Reabrir);
                var notaRevisao = Estilo.Rotulo(filaRevisao, p.Sala == null
                        ? "reabre a criação desta ficha do laboratório"
                        : "corrige a criação sem tirar da vaga (ex.: um erro que passou despercebido)",
                    20, Estilo.TextoSuave, FontStyles.Italic);
                notaRevisao.textWrappingMode = TextWrappingModes.Normal;
                Estilo.ColunaElastica(notaRevisao);
            }

            Secao("VARIÁVEIS", "só do mestre: o jogador não vê esta aba");

            var porAvanco = CadastroPersonagens.PotencialPorAvanco;
            var usado = p.AvancosMarcados.Count * porAvanco;
            var teste = cadastro.EmTeste(p.Id);
            Variavel("Potencial atingido", $"{p.Potencial}", "total corrido, desde o começo",
                teste ? () => Aplicar(cadastro.AjustarPotencial(p.Id, 1)) : null,
                teste ? () => Aplicar(cadastro.AjustarPotencial(p.Id, -1)) : null);
            Variavel("Potencial usado", $"{usado}", $"avanços já pegos × {porAvanco}");
            Variavel("Potencial atual", $"{p.Potencial - usado}", "saldo ainda não convertido em avanço");
            Variavel("Até o próximo avanço", ProgressoDoPotencial(), null);

            var vao = Estilo.Rect("Vao", conteudo);
            vao.gameObject.AddComponent<LayoutElement>().minHeight = 8;
            Variavel("Avanços ganhos", $"{cadastro.AvancosGanhos(p)}", $"potencial ÷ {porAvanco}");
            Variavel("Avanços pegos", $"{p.AvancosMarcados.Count} de 14", FaixaDaCarreira());
            Variavel("Avanços pendentes", $"{cadastro.AvancosPendentes(p)}", null);
            Variavel("Em cima / tardios", $"{cadastro.MarcadosEmCima(p)} / {p.AvancosMarcados.Count - cadastro.MarcadosEmCima(p)}",
                cadastro.BaixoLiberado(p) ? "tardios liberados" : "tardios liberam com 5 em cima");

            var vao2 = Estilo.Rect("Vao", conteudo);
            vao2.gameObject.AddComponent<LayoutElement>().minHeight = 8;
            var data = System.DateTime.TryParse(p.Foto?.AprovadaEm, out var d) ? d.ToString("dd/MM/yyyy HH:mm") : "—";
            Variavel("Status", p.Status.ToString(), $"criação concluída em {data}");
            Variavel("Cumpridos", $"{p.Cumpridos.Count}", "impulsos e afins marcados como cumpridos");

            if (cadastro.EmTeste(p.Id))
            {
                Playground();
                return;
            }
            var nota = Estilo.Rotulo(conteudo,
                "Ligue o MODO TESTE (no alto) para mexer em potencial, trilhas, impulsos e santuário. " +
                "Nada do teste é gravado: desligar desfaz tudo.", 22, Estilo.TextoApagado, FontStyles.Italic);
            nota.textWrappingMode = TextWrappingModes.Normal;
        }

        // ---------- modo teste ----------

        void BotaoModoTeste()
        {
            Limpar(vagaTeste);
            var ligado = cadastro.EmTeste(p.Id);
            var botao = ligado
                ? Estilo.BotaoPrincipal(vagaTeste, "Modo teste: ligado", 330, AlternarModoTeste)
                : Estilo.BotaoContorno(vagaTeste, "Modo teste: desligado", 330, Estilo.Azul, AlternarModoTeste);
            Estilo.Esticar((RectTransform)botao.transform);
        }

        void AlternarModoTeste()
        {
            if (cadastro.EmTeste(p.Id))
                cadastro.DesligarTeste(p.Id); // desfaz tudo o que foi testado
            else
                cadastro.LigarTeste(p.Id);
            Atualizar();
        }

        // ---------- playground do mestre (provisório, até o painel de controle existir) ----------

        void Playground()
        {
            var vao = Estilo.Rect("Vao", conteudo);
            vao.gameObject.AddComponent<LayoutElement>().minHeight = 12;
            Secao("PLAYGROUND DO MESTRE", "provisório: o que o painel de controle vai fazer no jogo");
            BotaoFimDeCena();

            // Extras e subseções de movimento (ex.: Entre dois mundos) passam pelos mesmos controles.
            foreach (var extra in cadastro.ExtrasDoArquetipo(p))
            foreach (var c in extra.Campos)
                ControleDoMestre(extra.Id, c, extra);
            foreach (var m in cadastro.MovimentosComSubsecao(p))
            foreach (var c in m.Campos.Where(c => c.NoJogo))
                ControleDoMestre(CadastroPersonagens.ExtraDoMovimento(m.Id), c, null);
        }

        void ControleDoMestre(string e, Core.Regras.RegrasArquetipos.CampoExtra c, Core.Regras.RegrasArquetipos.RegraExtra extra)
        {
            var campo = c.Id;
            switch (c.Tipo)
            {
                case Core.Regras.RegrasArquetipos.CampoExtra.Trilha:
                case Core.Regras.RegrasArquetipos.CampoExtra.Reserva:
                    Variavel(c.Titulo, $"{cadastro.Contador(p, e, campo)}", c.Nota,
                        () => Aplicar(cadastro.AjustarContador(p.Id, e, campo, 1)),
                        () => Aplicar(cadastro.AjustarContador(p.Id, e, campo, -1)));
                    break;
                case Core.Regras.RegrasArquetipos.CampoExtra.Condicoes:
                    CondicoesDoMestre(e, c);
                    break;
                case Core.Regras.RegrasArquetipos.CampoExtra.Registro:
                    RegistroDoMestre(e, c);
                    break;
                case Core.Regras.RegrasArquetipos.CampoExtra.Itens:
                    ItensDoMestre(e, c);
                    break;
                default:
                    if (extra != null && c.Id == extra.CumpreCampo)
                        CumpridosDoMestre(extra, c);
                    break;
            }
        }

        /// <summary>Lançar um uso (opção + texto) e desfazer lançamentos; cada um gasta a reserva indicada.</summary>
        void RegistroDoMestre(string extra, Core.Regras.RegrasArquetipos.CampoExtra c)
        {
            Subtitulo($"{c.Titulo}: {c.Nota}");
            var opcoes = c.Opcoes.Select(o => (o.Id, o.Nome)).ToList();
            var escolhida = opcoes.Count > 0 ? opcoes[0].Id : null;
            var fila = Estilo.Fila(conteudo, 50, 12);
            Estilo.ListaSuspensa(fila, 620, opcoes, escolhida, id => escolhida = id);
            var texto = Estilo.CampoTexto(fila, "qual habilidade / qual pergunta");
            Estilo.ColunaElastica(texto);
            Estilo.BotaoPrincipal(fila, "Lançar", 160, () => Aplicar(cadastro.Lancar(p.Id, extra, c.Id, escolhida, texto.text)));

            foreach (var l in cadastro.ItensNoJogo(p, extra, c.Id))
            {
                var item = l.Id;
                var nome = c.Opcoes.FirstOrDefault(o => o.Id == l.Opcao)?.Nome ?? l.Opcao;
                var linha = Estilo.Fila(conteudo, 44, 12);
                var t = Estilo.Rotulo(linha, string.IsNullOrEmpty(l.Texto) ? nome : $"{nome}: {l.Texto}", 21, Estilo.Texto);
                Estilo.ColunaElastica(t);
                Estilo.BotaoContorno(linha, "Desfazer", 170, Estilo.TextoSuave,
                    () => Aplicar(cadastro.DesfazerLancamento(p.Id, extra, c.Id, item)));
            }
        }

        /// <summary>Criar objeto com cargas, gastar/devolver carga e descartar.</summary>
        void ItensDoMestre(string extra, Core.Regras.RegrasArquetipos.CampoExtra c)
        {
            Subtitulo($"{c.Titulo}: {c.Nota}");
            var cargas = 1;
            var fila = Estilo.Fila(conteudo, 50, 12);
            var nome = Estilo.CampoTexto(fila, "o que o dispositivo faz");
            Estilo.ColunaElastica(nome);
            Estilo.ListaSuspensa(fila, 200, new List<(string, string)> { ("1", "1 carga"), ("2", "2 cargas") }, "1",
                id => cargas = int.Parse(id));
            Estilo.BotaoPrincipal(fila, "Criar", 140, () => Aplicar(cadastro.CriarItem(p.Id, extra, c.Id, nome.text, cargas)));

            foreach (var i in cadastro.ItensNoJogo(p, extra, c.Id))
            {
                var item = i.Id;
                var linha = Estilo.Fila(conteudo, 44, 8);
                var t = Estilo.Rotulo(linha, i.Texto, 21, i.Cargas == 0 ? Estilo.TextoApagado : Estilo.Texto,
                    i.Cargas == 0 ? FontStyles.Strikethrough : FontStyles.Normal);
                Estilo.ColunaElastica(t);
                BotaoSinal(linha, "+", Verde, () => Aplicar(cadastro.AjustarCarga(p.Id, extra, c.Id, item, 1)));
                var v = Estilo.Rotulo(linha, $"{i.Cargas}", 24, Estilo.Dourado, FontStyles.Bold);
                v.alignment = TextAlignmentOptions.Center;
                v.gameObject.AddComponent<LayoutElement>().preferredWidth = 50;
                BotaoSinal(linha, "−", Vermelho, () => Aplicar(cadastro.AjustarCarga(p.Id, extra, c.Id, item, -1)));
                Estilo.BotaoContorno(linha, "Descartar", 170, Estilo.TextoSuave,
                    () => Aplicar(cadastro.DescartarItem(p.Id, extra, c.Id, item)));
            }
        }

        /// <summary>Se há reserva que some no fim da cena (chamas), oferece o botão "Fim de cena".</summary>
        void BotaoFimDeCena()
        {
            var temReserva = cadastro.ExtrasDoArquetipo(p).Any(e => e.Campos.Any(c =>
                c.Tipo == Core.Regras.RegrasArquetipos.CampoExtra.Reserva && c.ZeraFimDeCena));
            if (!temReserva)
                return;
            var fila = Estilo.Fila(conteudo, 52, 12);
            Estilo.BotaoContorno(fila, "Fim de cena", 260, Estilo.Azul, () => Aplicar(cadastro.FimDeCena(p.Id)));
            var t = Estilo.Rotulo(fila, "zera as reservas da cena (chamas)", 20, Estilo.TextoApagado, FontStyles.Italic);
            Estilo.ColunaElastica(t);
        }

        /// <summary>Impulsos e afins: o mestre marca o cumprido e o benefício de cada escolhido.</summary>
        void CumpridosDoMestre(Core.Regras.RegrasArquetipos.RegraExtra extra, Core.Regras.RegrasArquetipos.CampoExtra c)
        {
            Subtitulo($"{extra.Nome}: cumprido e benefício");
            var beneficios = new List<(string id, string nome)> { (string.Empty, "benefício") };
            beneficios.AddRange(extra.AoCumprir.Select(b => (b.Id, b.Nome)));
            foreach (var o in c.Opcoes.Where(o => cadastro.Escolhas(p, extra.Id, c.Id).Contains(o.Id)))
            {
                var id = o.Id;
                var cumprido = cadastro.Cumprido(p, extra.Id, id);
                var fila = Estilo.Fila(conteudo, 48, 12);
                Caixa(fila, cumprido, true, () => Aplicar(cadastro.AlternarCumprido(p.Id, extra.Id, id)));
                var t = Estilo.Rotulo(fila, o.Nome, 21, cumprido ? Estilo.TextoSuave : Estilo.Texto,
                    cumprido ? FontStyles.Strikethrough : FontStyles.Normal);
                Estilo.ColunaElastica(t);
                Estilo.ListaSuspensa(fila, 320, beneficios, cadastro.BeneficioEscolhido(p, extra.Id, id),
                    b => Aplicar(cadastro.EscolherBeneficio(p.Id, extra.Id, id, b)), editavel: cumprido);
            }
        }

        /// <summary>O mestre escreve o pedido, seleciona as condições, preenche as lacunas e conclui.</summary>
        void CondicoesDoMestre(string extra, Core.Regras.RegrasArquetipos.CampoExtra c)
        {
            Subtitulo($"{c.Titulo}: {c.Nota}");
            if (c.Pedido)
            {
                var filaPedido = Estilo.Fila(conteudo, 50, 12);
                var rotulo = Estilo.Rotulo(filaPedido, "Pedido", 22, Estilo.TextoSuave);
                rotulo.gameObject.AddComponent<LayoutElement>().preferredWidth = 120;
                var pedido = Estilo.CampoTexto(filaPedido, "o que ela quer resolver");
                pedido.text = cadastro.TextoDeJogo(p, extra, c.Id, "pedido");
                Estilo.ColunaElastica(pedido);
                pedido.onDeselect.AddListener(t => cadastro.DefinirTextoDeJogo(p.Id, extra, c.Id, "pedido", t));
            }

            var selecionadas = cadastro.EscolhasNoJogo(p, extra, c.Id);
            foreach (var o in c.Opcoes)
            {
                var id = o.Id;
                var selecionada = selecionadas.Contains(id);
                var concluida = cadastro.CondicaoConcluida(p, extra, c.Id, id);
                var fila = Estilo.Fila(conteudo, 48, 12);
                Caixa(fila, selecionada, true, () => Aplicar(cadastro.AlternarCondicao(p.Id, extra, c.Id, id)));
                var t = Estilo.Rotulo(fila, o.Nome, 21, selecionada ? Estilo.Texto : Estilo.TextoSuave);
                t.gameObject.AddComponent<LayoutElement>().preferredWidth = 620;
                if (o.Lacuna && selecionada)
                {
                    var lacuna = Estilo.CampoTexto(fila, "preencha o ___");
                    lacuna.text = cadastro.TextoDeJogo(p, extra, c.Id, id);
                    Estilo.ColunaElastica(lacuna);
                    lacuna.onDeselect.AddListener(x => cadastro.DefinirTextoDeJogo(p.Id, extra, c.Id, id, x));
                }
                else
                {
                    var vazio = Estilo.Rect("Vao", fila);
                    Estilo.ColunaElastica(vazio);
                }
                var ok = Estilo.Rotulo(fila, "concluída", 20, Estilo.TextoApagado);
                ok.gameObject.AddComponent<LayoutElement>().preferredWidth = 110;
                Caixa(fila, concluida, selecionada, () => Aplicar(cadastro.AlternarCondicaoConcluida(p.Id, extra, c.Id, id)));
            }
        }

        static readonly Color Verde = new Color(0.35f, 0.85f, 0.45f);
        static readonly Color Vermelho = new Color(0.95f, 0.38f, 0.38f);

        /// <summary>
        /// Uma variável: nome, valor, nota. Com <paramref name="mais"/>/<paramref name="menos"/> (modo teste),
        /// ganha + verde antes e − vermelho depois do valor; sem eles, o espaço fica vazio e tudo alinha.
        /// </summary>
        void Variavel(string nome, string valor, string nota,
            UnityEngine.Events.UnityAction mais = null, UnityEngine.Events.UnityAction menos = null)
        {
            var fila = Estilo.Fila(conteudo, 40, 8);
            var n = Estilo.Rotulo(fila, nome, 22, Estilo.TextoSuave);
            n.gameObject.AddComponent<LayoutElement>().preferredWidth = 320;
            BotaoSinal(fila, "+", Verde, mais);
            var v = Estilo.Rotulo(fila, valor, 24, Estilo.Dourado, FontStyles.Bold);
            v.alignment = TextAlignmentOptions.Center;
            v.gameObject.AddComponent<LayoutElement>().preferredWidth = 200;
            BotaoSinal(fila, "−", Vermelho, menos);
            var t = Estilo.Rotulo(fila, nota ?? string.Empty, 20, Estilo.TextoApagado, FontStyles.Italic);
            Estilo.ColunaElastica(t);
        }

        static void BotaoSinal(Transform fila, string sinal, Color cor, UnityEngine.Events.UnityAction aoClicar)
        {
            var rect = Estilo.Rect("Sinal " + sinal, fila);
            rect.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
            if (aoClicar == null)
                return; // sem ação: só ocupa o lugar, para os valores alinharem
            var fundo = rect.gameObject.AddComponent<Image>();
            fundo.color = new Color(1, 1, 1, 0);
            var botao = rect.gameObject.AddComponent<Button>();
            botao.targetGraphic = fundo;
            var cores = botao.colors;
            cores.highlightedColor = new Color(1, 1, 1, 0.12f);
            botao.colors = cores;
            botao.onClick.AddListener(aoClicar);
            var t = Estilo.Rotulo(rect, sinal, 34, cor, FontStyles.Bold);
            t.alignment = TextAlignmentOptions.Center;
            Estilo.Esticar(t.rectTransform);
        }

        // ---------- aba Crescimento (protótipo): as casas de avanço, marcáveis ----------

        enum EstadoAvanco { Marcado, Livre, Travado }

        void AbaCrescimento()
        {
            var avancos = cadastro.AvancosDoArquetipo(p);
            var emCriacao = !cadastro.LiberadoNoJogo(p); // no modo teste, libera até em "novo"
            var emCima = cadastro.MarcadosEmCima(p);
            var baixoLiberado = cadastro.BaixoLiberado(p);

            // Só texto por enquanto (as estrelas do Alan entram no fim, junto com as outras marcas visuais).
            var pendentes = cadastro.AvancosPendentes(p);
            var topo = Estilo.Fila(conteudo, 44, 14);
            var resumo = cadastro.CarreiraEncerrada(p)
                ? "Carreira encerrada: aposentou ou virou modelo para a cidade."
                : emCriacao ? "Avanços vêm depois de concluir a criação."
                : pendentes > 0 ? "Avanço pendente: escolha abaixo."
                : "Sem avanço pendente.";
            var t = Estilo.Rotulo(topo, resumo, 24, pendentes > 0 ? Estilo.Dourado : Estilo.TextoSuave,
                pendentes > 0 ? FontStyles.Bold : FontStyles.Normal);
            Estilo.ColunaElastica(t);

            Secao("AVANÇOS", emCriacao ? null : $"{emCima}/{avancos.Cima.Count} marcados");
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

        /// <summary>
        /// Faixa da carreira pelos avanços (em texto por enquanto; as estrelas do Alan entram no fim):
        /// 1–5 sem tardios ainda, 6–10 veterano, 11–14 fim de carreira; aposentou encerra.
        /// </summary>
        string FaixaDaCarreira()
        {
            if (cadastro.CarreiraEncerrada(p))
                return "carreira encerrada";
            var a = p.AvancosMarcados.Count;
            return a == 0 ? "sem avanços ainda" : a <= 5 ? "iniciante" : a <= 10 ? "veterano" : "fim de carreira";
        }

        /// <summary>Cinco caixinhas: o caminho do potencial até o próximo avanço.</summary>
        void MarcadoresDePotencial(Transform fila)
        {
            var porAvanco = CadastroPersonagens.PotencialPorAvanco;
            var resto = p.Potencial % porAvanco;
            // Bateu um múltiplo de 5 com avanço por pegar: mostra as cinco cheias.
            var cheias = resto == 0 && p.Potencial > 0 && cadastro.AvancosPendentes(p) > 0 ? porAvanco : resto;
            for (var k = 0; k < porAvanco; k++)
            {
                var marca = Estilo.Rect("Marca", fila);
                marca.gameObject.AddComponent<LayoutElement>().preferredWidth = 30;
                var dentro = Estilo.Rect("Dentro", marca);
                dentro.anchorMin = dentro.anchorMax = new Vector2(0.5f, 0.5f);
                dentro.sizeDelta = new Vector2(26, 26);
                Estilo.Moldura(dentro.gameObject, k < cheias ? Estilo.Dourado : Estilo.Campo, Estilo.Dourado).raycastTarget = false;
            }
        }

        /// <summary>Caminho do potencial até o próximo avanço, em texto ("2/5").</summary>
        string ProgressoDoPotencial()
        {
            var porAvanco = CadastroPersonagens.PotencialPorAvanco;
            var resto = p.Potencial % porAvanco;
            var cheio = resto == 0 && p.Potencial > 0 && cadastro.AvancosPendentes(p) > 0;
            return $"{(cheio ? porAvanco : resto)}/{porAvanco} para o próximo avanço";
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
            // Marcar exige avanço pendente; desmarcar (desfazer) sempre pode.
            botao.interactable = cadastro.LiberadoNoJogo(p) && estado != EstadoAvanco.Travado &&
                                 (marcado || cadastro.AvancosPendentes(p) > 0);
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
                Estilo.ComExpandir(campo, regra.Historico[k]);
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
            var emCriacao = p.EmCriacao;
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

            // "Como se manifesta" para toda habilidade da ficha: obrigatório onde a ficha do arquétipo pede
            // (Bruta: cada uma; Transformado: as escolhidas), opcional nas demais.
            // Depois de aprovada, a lista aparece sempre, só para leitura.
            var pede = regra.Descrever;
            var naFicha = cadastro.HabilidadesAtuais(p);
            foreach (var h in naFicha)
            {
                var fila = Estilo.Fila(conteudo, 50, 10);
                var nome = Estilo.Rotulo(fila, h.Nome, 22, h.Fixa ? Estilo.Dourado : Estilo.Texto, FontStyles.Bold);
                nome.textWrappingMode = TextWrappingModes.NoWrap;
                nome.overflowMode = TextOverflowModes.Ellipsis;
                nome.gameObject.AddComponent<LayoutElement>().preferredWidth = 460;

                if (!emCriacao)
                {
                    // Só leitura: uma linha (reticências); o texto inteiro no balão, com o mouse em cima.
                    var d = Estilo.Rotulo(fila, h.Descricao, 22, Estilo.TextoSuave, FontStyles.Italic);
                    d.textWrappingMode = TextWrappingModes.NoWrap;
                    d.overflowMode = TextOverflowModes.Ellipsis;
                    Estilo.ColunaElastica(d);
                    if (!string.IsNullOrWhiteSpace(h.Descricao))
                    {
                        d.raycastTarget = true;
                        d.gameObject.AddComponent<Dica>().Configurar(h.Nome + System.Environment.NewLine + h.Descricao);
                    }
                    continue;
                }

                var obrigatoria = pede == Regra.DescreverCada || (pede == Regra.DescreverEscolhidas && !h.Fixa);
                var campo = Estilo.CampoTexto(fila,
                    !obrigatoria ? "como se manifesta (opcional)"
                    : pede == Regra.DescreverEscolhidas ? "como a deixa grotesca" : "como se manifesta");
                campo.text = h.Descricao;
                Estilo.ColunaElastica(campo);
                var hid = h.Id;
                // Não redesenha: o Tab precisa achar o próximo campo vivo. Só o rodapé muda.
                // Grava a cada tecla (barato: o disco é adiado); só sair do campo dependia de o foco sair.
                campo.onValueChanged.AddListener(t => cadastro.DescreverHabilidade(p.Id, hid, t));
                campo.onSubmit.AddListener(t => { cadastro.DescreverHabilidade(p.Id, hid, t); MontarRodape(); });
                campo.onDeselect.AddListener(t => { cadastro.DescreverHabilidade(p.Id, hid, t); MontarRodape(); });
                Estilo.ComExpandir(campo, $"{h.Nome}: como se manifesta");
            }
        }

        /// <summary>
        /// Depois de um clique que mudou a ficha: refaz só o bloco onde o clique aconteceu (SecaoRefazivel),
        /// ou a aba inteira se o clique não estava num bloco desses. O rodapé (pendências) sempre.
        /// </summary>
        void Aplicar(Resultado resultado)
        {
            var clicado = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            var secao = clicado != null ? clicado.GetComponentInParent<SecaoRefazivel>() : null;
            if (secao != null && secao.transform.IsChildOf(conteudo))
                secao.Refazer();
            else
                MontarConteudo(manterRolagem: true);
            MontarRodape();
            if (!resultado.Ok)
                Aviso(resultado.Erro);
        }

        /// <summary>
        /// Monta um bloco da aba que sabe se refazer sozinho (ver Aplicar). Enquanto monta, "conteudo"
        /// aponta para o bloco: os ajudantes (Secao, Grade, Chip...) nem precisam saber.
        /// </summary>
        void Refazivel(Action montar)
        {
            var bloco = Estilo.Rect("Secao", conteudo);
            var v = bloco.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 8;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;

            void Montar()
            {
                var fora = conteudo;
                conteudo = bloco;
                try
                {
                    montar();
                }
                finally
                {
                    conteudo = fora;
                }
            }

            bloco.gameObject.AddComponent<SecaoRefazivel>().Refazer = () =>
            {
                for (var k = bloco.childCount - 1; k >= 0; k--)
                {
                    var filho = bloco.GetChild(k).gameObject;
                    filho.SetActive(false); // sai do layout já, antes do Destroy (que espera o fim do quadro)
                    Destroy(filho);
                }
                Montar();
            };
            Montar();
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

        static void Chip(Transform pai, string texto, EstadoChip estado, UnityEngine.Events.UnityAction aoClicar,
            string dica = null)
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

            // Regra da opção num balão (mouse em cima). Funciona até travada: o fundo recebe o mouse.
            if (!string.IsNullOrEmpty(dica))
                rect.gameObject.AddComponent<Dica>().Configurar(texto + System.Environment.NewLine + dica);
        }


        TMP_InputField CampoRotulado(string rotulo, string valor, Transform pai = null)
        {
            var fila = FilaRotulada(rotulo, pai);
            var campo = Estilo.CampoTexto(fila, rotulo.ToLowerInvariant());
            campo.text = valor;
            Estilo.ColunaElastica(campo);
            return campo;
        }

        RectTransform FilaRotulada(string rotulo, Transform pai = null)
        {
            var fila = Estilo.Fila(pai != null ? pai : conteudo, 58);
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
                aoCancelar: () => MontarConteudo());
        }

        // ---------- aba Núcleo ----------

        void AbaNucleo()
        {
            Secao("RÓTULOS", p.EmCriacao
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

            Secao("POTENCIAL E HORA DA VERDADE", "pontos corridos: a cada 5, um avanço");
            var outros = Estilo.Fila(conteudo, 56, 12);
            MarcadoresDePotencial(outros);
            var pendentes = cadastro.AvancosPendentes(p);
            // O jogador vê as caixinhas e o aviso de avanço; os números ficam na aba Variáveis (do mestre).
            var textoPotencial = Estilo.Rotulo(outros,
                pendentes > 0 ? "Avanço pendente: escolha na aba Evolução" : "potencial até o próximo avanço",
                24, pendentes > 0 ? Estilo.Dourado : Estilo.TextoSuave, pendentes > 0 ? FontStyles.Bold : FontStyles.Normal);
            textoPotencial.gameObject.AddComponent<LayoutElement>().preferredWidth = 470;
            var vao2 = Estilo.Rect("Vao", outros);
            vao2.gameObject.AddComponent<LayoutElement>().preferredWidth = 30;
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
                // Uma linha (reticências) com o texto inteiro no balão: legenda longa não invade a linha de baixo.
                var l = Estilo.Rotulo(fila, legenda, 20, Estilo.TextoSuave, FontStyles.Italic);
                l.textWrappingMode = TextWrappingModes.NoWrap;
                l.overflowMode = TextOverflowModes.Ellipsis;
                l.raycastTarget = true;
                l.gameObject.AddComponent<Dica>().Configurar(legenda);
                Estilo.ColunaElastica(l);
            }
        }

        void LinhaRotulo(string rotulo, int valor)
        {
            var fila = Estilo.Fila(conteudo, 48, 8);
            var nome = Estilo.Rotulo(fila, Rotulos.Nome(rotulo).ToUpperInvariant(), 24, Estilo.Texto, FontStyles.Bold);
            nome.characterSpacing = 3;
            nome.gameObject.AddComponent<LayoutElement>().preferredWidth = 240;

            var comBonus = p.EmCriacao && p.Criacao.BonusLivre == rotulo;
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

            if (!p.EmCriacao)
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
