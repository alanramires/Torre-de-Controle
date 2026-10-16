using System;
using System.Linq;
using TMPro;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Salas;
using UnityEngine;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Área do mestre de uma sala. Primeira seção: a primeira sessão (livro, cap. 8), com o preparo que a
    /// Torre confere nos dados (fichas, vilão ligado a uma heroína, ficha do vilão, NPCs e cenário das
    /// fichas) e o roteiro que o mestre marca durante a sessão. A mesa (Rótulos, condições, Equipe,
    /// rolador, log) entra aqui depois; quando ela existir, o log marca o roteiro sozinho.
    /// </summary>
    public sealed class PainelMestre : MonoBehaviour
    {
        static PrimeiraSessao roteiro;

        Sala sala;
        CadastroSalas salas;
        CadastroPersonagens personagens;
        Action aoFechar;

        TextMeshProUGUI titulo;
        TextMeshProUGUI subtitulo;
        RectTransform lista;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Limpar() => roteiro = null; // Play sem domain reload: relê o JSON

        public static PainelMestre Criar(Transform pai)
        {
            var veu = Estilo.Rect("PainelMestre", pai);
            Estilo.Esticar(veu);
            veu.gameObject.AddComponent<Image>().color = Estilo.Veu;
            veu.gameObject.AddComponent<NavegacaoTab>();
            var painel = veu.gameObject.AddComponent<PainelMestre>();
            painel.Montar(veu);
            return painel;
        }

        /// <summary>A mesa da sala, quando aberta pela mesa (a aba Vilões põe e tira NPCs de cena nela).</summary>
        Mesa mesa;

        public void Abrir(Sala salaAberta, CadastroSalas cadastroSalas, CadastroPersonagens cadastroPersonagens, Action aoFecharArea,
            Mesa mesaAberta = null)
        {
            mesa = mesaAberta;
            if (mesa == null && aba == Aba.Viloes)
                aba = Aba.PrimeiraSessao;
            sala = salaAberta;
            salas = cadastroSalas;
            personagens = cadastroPersonagens;
            aoFechar = aoFecharArea;
            roteiro ??= PrimeiraSessao.Carregar(new FonteDeRegrasResources());
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Redesenhar();
        }

        void Fechar()
        {
            gameObject.SetActive(false);
            aoFechar?.Invoke();
        }

        void Montar(RectTransform veu)
        {
            var cartao = Estilo.Rect("Cartao", veu);
            cartao.anchorMin = new Vector2(0.5f, 0.5f);
            cartao.anchorMax = new Vector2(0.5f, 0.5f);
            cartao.sizeDelta = new Vector2(1680, 900);
            Estilo.Moldura(cartao.gameObject, Estilo.Cartao, Estilo.Borda);

            var coluna = cartao.gameObject.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(56, 56, 44, 44);
            coluna.spacing = 12;
            coluna.childControlWidth = true;
            coluna.childControlHeight = true;
            coluna.childForceExpandHeight = false;

            var cabecalho = Estilo.Fila(cartao, 84);
            var titulos = Estilo.Rect("Titulos", cabecalho);
            titulos.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var pilha = titulos.gameObject.AddComponent<VerticalLayoutGroup>();
            pilha.childControlWidth = true;
            pilha.childControlHeight = true;
            pilha.childForceExpandHeight = false;
            pilha.childAlignment = TextAnchor.MiddleLeft;
            titulo = Estilo.Rotulo(titulos, "ÁREA DO MESTRE", 44, Estilo.Dourado, FontStyles.Bold);
            titulo.characterSpacing = 6;
            subtitulo = Estilo.Rotulo(titulos, string.Empty, 24, Estilo.TextoSuave);
            Estilo.BotaoContorno(cabecalho, "Voltar", 190, Estilo.TextoSuave, Fechar);

            var divisoria = Estilo.Rect("Divisoria", cartao);
            divisoria.gameObject.AddComponent<Image>().color =
                new Color(Estilo.Dourado.r, Estilo.Dourado.g, Estilo.Dourado.b, 0.35f);
            var ld = divisoria.gameObject.AddComponent<LayoutElement>();
            ld.minHeight = 2;
            ld.preferredHeight = 2;

            // Primeira sessão e Trama; a mesa e o log entram ao lado.
            abas = Estilo.Fila(cartao, 52, 10);

            lista = Estilo.ListaRolavel(cartao);
        }

        RectTransform abas;
        enum Aba { PrimeiraSessao, Anotacoes, Viloes }
        Aba aba = Aba.PrimeiraSessao;

        void MontarAbas()
        {
            for (var i = abas.childCount - 1; i >= 0; i--)
                Destroy(abas.GetChild(i).gameObject);
            var lista = new System.Collections.Generic.List<(Aba, string)> { (Aba.PrimeiraSessao, "Primeira sessão"), (Aba.Anotacoes, "Anotações") };
            if (mesa != null)
                lista.Add((Aba.Viloes, "Vilões em cena"));
            foreach (var (qual, nome) in lista)
            {
                var alvo = qual;
                var botao = qual == aba
                    ? Estilo.BotaoPrincipal(abas, nome, 300, () => { })
                    : Estilo.BotaoContorno(abas, nome, 300, Estilo.Dourado, () =>
                    {
                        aba = alvo;
                        Redesenhar();
                    });
                // Como nas abas da sala: uma linha, a fonte encolhe até caber (sem isso o rótulo some).
                var rotulo = botao.GetComponentInChildren<TextMeshProUGUI>();
                rotulo.textWrappingMode = TextWrappingModes.NoWrap;
                rotulo.enableAutoSizing = true;
                rotulo.fontSizeMin = 14;
                rotulo.fontSizeMax = 24;
                rotulo.rectTransform.offsetMin = new Vector2(10, 0);
                rotulo.rectTransform.offsetMax = new Vector2(-10, 0);
            }
            Estilo.Rect("Vao", abas).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            if (aba == Aba.Anotacoes)
            {
                Estilo.BotaoContorno(abas, "Exportar", 190, Estilo.Dourado, ExportarAnotacoes);
                Estilo.BotaoContorno(abas, "Importar", 190, Estilo.Dourado, ImportarAnotacoes);
            }
        }

        /// <summary>
        /// As anotações do mestre da campanha, num lugar só: a equipe (o que a formação deixou), os NPCs e a
        /// trama (mais tarde, ganchos e arcos, livro cap. 9). Texto livre e opcional; grava ao sair do campo.
        /// </summary>
        void SecaoAnotacoes()
        {
            subtitulo.text = $"{sala.Nome} · anotações do mestre (opcionais)";
            Anotacao("EQUIPE", "inimigos, consequências e NPCs relevantes que a formação deixou",
                CadastroSalas.Anotacao.Formacao, 150);
            Anotacao("NPCS", "quem se conhece, quem deve a quem, o que cada um quer agora",
                CadastroSalas.Anotacao.Npcs, 150);
            Anotacao("TRAMA", "o plano do vilão, quem sabe o quê, pontas soltas, próximos passos",
                CadastroSalas.Anotacao.Trama, 150);
        }

        void Anotacao(string titulo, string legenda, CadastroSalas.Anotacao qual, float altura)
        {
            Secao(titulo, legenda, comAjuda: false);
            var texto = Estilo.AreaTexto(lista, "opcional", altura);
            texto.text = CadastroSalas.LerAnotacao(sala, qual);
            texto.onDeselect.AddListener(t => salas.DefinirAnotacao(sala.Id, qual, t));
            Estilo.ComExpandir(texto, $"{titulo.Substring(0, 1)}{titulo.Substring(1).ToLowerInvariant()} · {sala.Nome}");
        }

        void ExportarAnotacoes()
        {
            var json = salas.ExportarAnotacoes(sala.Id);
            if (json == null || Troca.Arquivos == null)
                return;
            string caminho;
            try
            {
                caminho = Troca.Arquivos.Exportar("Anotações - " + sala.Nome, json);
            }
            catch (Exception e)
            {
                Estilo.Confirmar(transform, "Não deu para exportar: " + e.Message, "Ok", () => { });
                return;
            }
            GUIUtility.systemCopyBuffer = json;
            Estilo.Confirmar(transform,
                $"Anotações exportadas em\n<size=80%>{caminho}</size>\nO JSON também foi copiado (Ctrl+V para colar).",
                "Abrir a pasta",
                () => Application.OpenURL("file:///" + Troca.Arquivos.PastaExportacoes.Replace('\\', '/')));
        }

        void ImportarAnotacoes()
        {
            if (Troca.Arquivos == null)
                return;
            var opcoes = new System.Collections.Generic.List<(string, UnityEngine.Events.UnityAction, bool)>();
            foreach (var caminho in Troca.Importaveis(CadastroSalas.FormatoDeTrocaAnotacoes))
            {
                var arquivo = caminho;
                opcoes.Add((System.IO.Path.GetFileName(arquivo), () => ConfirmarImportacao(Troca.Arquivos.Ler(arquivo)), false));
            }
            opcoes.Add(("Colar da área de transferência", () => ConfirmarImportacao(GUIUtility.systemCopyBuffer), false));
            opcoes.Add(("Abrir a pasta de importar", () =>
                Application.OpenURL("file:///" + Troca.Arquivos.PastaImportar.Replace('\\', '/')), true));
            Estilo.Escolher(transform, $"Importar anotações para {sala.Nome}", opcoes.ToArray());
        }

        void ConfirmarImportacao(string json) =>
            Estilo.Confirmar(transform, $"Substituir as anotações de {sala.Nome} pelas do arquivo?", "Substituir", () =>
            {
                var r = salas.ImportarAnotacoes(sala.Id, json);
                if (!r.Ok)
                    Estilo.Confirmar(transform, r.Erro, "Ok", () => { });
                Redesenhar();
            });

        /// <summary>
        /// Os vilões e oponentes da campanha, para pôr e tirar da mesa (pode haver mais de um em cena). A mesa
        /// mostra os que estão, com as condições para marcar na luta e os movimentos de vilão.
        /// </summary>
        void SecaoViloes()
        {
            subtitulo.text = $"{sala.Nome} · quem está em cena na mesa";
            var viloes = personagens.NpcsDaSala(sala.Id).Where(n => n.Ativo && n.Papel == Papel.Vilao).ToList();
            Secao("VILÕES E OPONENTES", $"{mesa.EmCena().Count} em cena · papel Vilão / Oponente nesta campanha", comAjuda: false);
            if (viloes.Count == 0)
            {
                var vazio = Estilo.Rotulo(lista, "Nenhum NPC com papel Vilão / Oponente nesta campanha (aba NPCs da sala).", 20,
                    Estilo.TextoApagado, FontStyles.Italic);
                vazio.textWrappingMode = TextWrappingModes.Normal;
                return;
            }
            foreach (var v in viloes)
            {
                var emCena = mesa.EstaEmCena(v.Id);
                var linha = Estilo.Fila(lista, 60);
                linha.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(14, 14, 6, 6);
                Estilo.Moldura(linha.gameObject, Estilo.Linha, emCena ? Estilo.Dourado : Estilo.BordaApagada);
                var nome = Estilo.Rotulo(linha, $"<b>{v.NomeCompleto}</b>", 24, emCena ? Estilo.Texto : Estilo.TextoSuave);
                nome.textWrappingMode = TextWrappingModes.NoWrap;
                nome.overflowMode = TextOverflowModes.Ellipsis;
                Estilo.ColunaElastica(nome);
                var ficha = v.Vilao;
                var resumo = Estilo.Rotulo(linha,
                    $"{ficha?.Movimentos.Count(m => !string.IsNullOrWhiteSpace(m)) ?? 0} movimentos · {ficha?.Condicoes.Count ?? 0} condições",
                    20, Estilo.TextoSuave);
                resumo.gameObject.AddComponent<LayoutElement>().preferredWidth = 330;
                var id = v.Id;
                var b = emCena
                    ? Estilo.BotaoPrincipal(linha, "Tirar da mesa", 240, () => { mesa.AlternarEmCena(id); Redesenhar(); })
                    : Estilo.BotaoContorno(linha, "Pôr na mesa", 240, Estilo.Dourado, () => { mesa.AlternarEmCena(id); Redesenhar(); });
            }
        }

        void Redesenhar()
        {
            for (var i = lista.childCount - 1; i >= 0; i--)
                Destroy(lista.GetChild(i).gameObject);
            MontarAbas();
            if (aba == Aba.Anotacoes)
            {
                SecaoAnotacoes();
                return;
            }
            if (aba == Aba.Viloes)
            {
                SecaoViloes();
                return;
            }
            subtitulo.text = $"{sala.Nome} · a primeira sessão abre já na luta (livro, cap. 8)";

            // ---------- preparo: a Torre confere ----------
            var conferidos = roteiro.Conferir(sala, personagens);
            Secao("ANTES DE ABRIR", $"{conferidos.Count(c => c.Pronto)} de {conferidos.Count} prontos · conferido nos dados da sala");
            foreach (var c in conferidos)
                LinhaDePreparo(c);

            // ---------- roteiro: o mestre marca ----------
            var marcados = sala.PrimeiraSessao ?? new System.Collections.Generic.List<string>();
            var feitos = roteiro.Roteiro.Count(i => marcados.Contains(i.Id));
            Secao("NA SESSÃO", $"{feitos} de {roteiro.Roteiro.Count} · marque quando acontecer (depois, o log da mesa marca sozinho)");
            foreach (var (grupo, nomeGrupo) in roteiro.Grupos)
            {
                var itens = roteiro.Roteiro.Where(i => i.Grupo == grupo).ToList();
                var sub = Estilo.Rotulo(lista, $"{nomeGrupo}  ·  {itens.Count(i => marcados.Contains(i.Id))}/{itens.Count}",
                    20, Estilo.TextoSuave, FontStyles.Italic);
                sub.gameObject.AddComponent<LayoutElement>().minHeight = 30;
                var grade = Estilo.Rect("Grade", lista);
                var g = grade.gameObject.AddComponent<GridLayoutGroup>();
                g.cellSize = new Vector2(380, 50);
                g.spacing = new Vector2(12, 10);
                g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                g.constraintCount = 4;
                foreach (var item in itens)
                    Chip(grade, item, marcados.Contains(item.Id));
            }
        }

        void Secao(string nome, string legenda, bool comAjuda = true)
        {
            var vao = Estilo.Rect("Vao", lista);
            vao.gameObject.AddComponent<LayoutElement>().minHeight = 6;
            var fila = Estilo.Fila(lista, 40);
            var t = Estilo.Rotulo(fila, nome, 24, Estilo.Dourado, FontStyles.Bold);
            t.characterSpacing = 5;
            t.gameObject.AddComponent<LayoutElement>().preferredWidth = 240;
            // "?" abre as cartas da Primeira sessão no help (só essa categoria).
            if (comAjuda)
                Estilo.BotaoContorno(fila, "?", 40, Estilo.Dourado, AbrirAjuda)
                    .gameObject.AddComponent<Dica>().Configurar("Cartas da primeira sessão (help).");
            var l = Estilo.Rotulo(fila, legenda, 20, Estilo.TextoSuave, FontStyles.Italic);
            Estilo.ColunaElastica(l);
        }

        PainelAjuda ajuda;

        void AbrirAjuda()
        {
            if (ajuda == null)
                ajuda = PainelAjuda.Criar(transform.parent);
            ajuda.AbrirCategoria("primeira-sessao");
        }

        void LinhaDePreparo(PrimeiraSessao.Conferido c)
        {
            var linha = Estilo.Fila(lista, 60);
            linha.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(14, 14, 6, 6);
            Estilo.Moldura(linha.gameObject, Estilo.Linha, c.Pronto ? Estilo.Dourado : Estilo.BordaApagada);
            // O detalhe inteiro também no balão: na linha ele corta com reticências.
            linha.gameObject.AddComponent<Dica>().Configurar(
                c.Item.Texto + Environment.NewLine + c.Item.Dica + Environment.NewLine + Environment.NewLine + c.Detalhe);

            var selo = Estilo.Rotulo(linha, c.Pronto ? "PRONTO" : "FALTA", 18, c.Pronto ? Estilo.Dourado : Estilo.Erro, FontStyles.Bold);
            selo.characterSpacing = 4;
            selo.gameObject.AddComponent<LayoutElement>().preferredWidth = 110;
            var nome = Estilo.Rotulo(linha, c.Item.Texto, 24, Estilo.Texto, FontStyles.Bold);
            nome.gameObject.AddComponent<LayoutElement>().preferredWidth = 420;
            var detalhe = Estilo.Rotulo(linha, c.Detalhe ?? string.Empty, 20, Estilo.TextoSuave);
            detalhe.textWrappingMode = TextWrappingModes.NoWrap;
            detalhe.overflowMode = TextOverflowModes.Ellipsis;
            Estilo.ColunaElastica(detalhe);
        }

        void Chip(Transform grade, PrimeiraSessao.Item item, bool marcado)
        {
            var rect = Estilo.Rect("Chip " + item.Id, grade);
            var imagem = Estilo.Moldura(rect.gameObject,
                marcado ? Estilo.Dourado : new Color(Estilo.Dourado.r, Estilo.Dourado.g, Estilo.Dourado.b, 0.08f), Estilo.Dourado);
            var botao = rect.gameObject.AddComponent<Button>();
            SomBotao.Ligar(botao);
            botao.targetGraphic = imagem;
            botao.onClick.AddListener(() =>
            {
                salas.AlternarItemDaPrimeiraSessao(sala.Id, item.Id);
                Redesenhar();
            });
            rect.gameObject.AddComponent<Tabulavel>();
            var t = Estilo.Rotulo(rect, item.Texto, 21, marcado ? Estilo.Cartao : Estilo.Dourado,
                marcado ? FontStyles.Bold : FontStyles.Normal);
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            Estilo.Esticar(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(10, 0);
            t.rectTransform.offsetMax = new Vector2(-10, 0);
            if (!string.IsNullOrEmpty(item.Dica))
                rect.gameObject.AddComponent<Dica>().Configurar(item.Texto + Environment.NewLine + item.Dica);
        }
    }
}
