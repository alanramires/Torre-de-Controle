using System;
using System.Linq;
using TMPro;
using TorreDeControle.Core;
using TorreDeControle.Core.Personagens;
using UnityEngine;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Ficha de NPC (livro p.193–195): nome, aparência marcante, modo de agir, motivação e notas;
    /// se for vilão, a ficha de vilão (geração, movimentos, condições). Tudo grava na hora.
    /// Marcar condição na luta é do painel do mestre (ainda não existe).
    /// </summary>
    public sealed class PainelNpc : MonoBehaviour
    {
        Personagem npc;
        CadastroPersonagens cadastro;
        Action aoFechar;

        TextMeshProUGUI titulo;
        TextMeshProUGUI subtitulo;
        RectTransform moldura;
        RectTransform conteudo;
        TextMeshProUGUI rodape;

        public static PainelNpc Criar(Transform pai)
        {
            var veu = Estilo.Rect("PainelNpc", pai);
            Estilo.Esticar(veu);
            veu.gameObject.AddComponent<Image>().color = Estilo.Veu;
            veu.gameObject.AddComponent<NavegacaoTab>();
            var painel = veu.gameObject.AddComponent<PainelNpc>();
            painel.Montar(veu);
            return painel;
        }

        public void Abrir(Personagem personagem, CadastroPersonagens cadastroPersonagens, Action aoFecharFicha)
        {
            npc = personagem;
            cadastro = cadastroPersonagens;
            aoFechar = aoFecharFicha;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Redesenhar();
        }

        void Fechar()
        {
            gameObject.SetActive(false);
            aoFechar?.Invoke();
        }

        // ---------- montagem ----------

        void Montar(RectTransform veu)
        {
            var cartao = Estilo.Rect("Cartao", veu);
            cartao.anchorMin = new Vector2(0.5f, 0.5f);
            cartao.anchorMax = new Vector2(0.5f, 0.5f);
            cartao.sizeDelta = new Vector2(1400, 940);
            Estilo.Moldura(cartao.gameObject, Estilo.Cartao, Estilo.Borda);

            var coluna = cartao.gameObject.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(56, 56, 40, 36);
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
            titulo = Estilo.Rotulo(titulos, string.Empty, 40, Estilo.Dourado, FontStyles.Bold);
            titulo.characterSpacing = 5;
            subtitulo = Estilo.Rotulo(titulos, string.Empty, 22, Estilo.TextoSuave);
            Estilo.BotaoContorno(cabecalho, "Fechar", 190, Estilo.TextoSuave, Fechar);

            var divisoria = Estilo.Rect("Divisoria", cartao);
            divisoria.gameObject.AddComponent<Image>().color =
                new Color(Estilo.Dourado.r, Estilo.Dourado.g, Estilo.Dourado.b, 0.35f);
            var ld = divisoria.gameObject.AddComponent<LayoutElement>();
            ld.minHeight = 2;
            ld.preferredHeight = 2;

            moldura = Estilo.Rect("Moldura", cartao);
            moldura.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            rodape = Estilo.Rotulo(cartao, string.Empty, 20, Estilo.TextoSuave, FontStyles.Italic);
            rodape.textWrappingMode = TextWrappingModes.Normal;
            rodape.gameObject.AddComponent<LayoutElement>().minHeight = 30;
        }

        /// <summary>Refaz a ficha mantendo a rolagem (clicar numa condição lá embaixo não volta ao topo).</summary>
        void Redesenhar()
        {
            var posicao = conteudo != null ? conteudo.anchoredPosition.y : 0f;
            for (var i = moldura.childCount - 1; i >= 0; i--)
                Destroy(moldura.GetChild(i).gameObject);
            var lista = Estilo.ListaRolavel(moldura, 8);
            Estilo.Esticar((RectTransform)lista.parent.parent);
            conteudo = lista;

            Cabecalho();
            MontarFicha();
            Rodape();

            if (posicao > 0)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(moldura);
                LayoutRebuilder.ForceRebuildLayoutImmediate(conteudo);
                var viewport = (RectTransform)conteudo.parent;
                var maximo = Mathf.Max(0, conteudo.rect.height - viewport.rect.height);
                conteudo.anchoredPosition = new Vector2(conteudo.anchoredPosition.x, Mathf.Min(posicao, maximo));
            }
        }

        void Cabecalho()
        {
            titulo.text = npc.NomeExibicao.ToUpperInvariant();
            var maturidade = npc.Maturidade == Maturidade.Adulto ? "adulto" : "jovem";
            var papel = npc.Papel switch { Papel.Vilao => "vilão", Papel.Heroi => "herói", _ => "neutro" };
            subtitulo.text = $"NPC · {maturidade} · {papel}" + (npc.Ativo ? string.Empty : " · inativo");
        }

        void Rodape()
        {
            var avisos = cadastro.AvisosDoNpc(npc);
            rodape.color = avisos.Count > 0 ? Estilo.Dourado : Estilo.TextoSuave;
            rodape.text = avisos.Count > 0 ? string.Join("   ", avisos) : "Ficha completa.";
        }

        void MontarFicha()
        {
            Secao("IDENTIDADE", null);
            var nome = CampoRotulado("NOME", npc.Identidade.Nome, "nome");
            var codinome = CampoRotulado(npc.Papel == Papel.Vilao ? "NOME DE VILÃO" : "CODINOME",
                npc.Identidade.Codinome, "opcional");
            foreach (var campo in new[] { nome, codinome })
            {
                campo.onSubmit.AddListener(_ => Renomear(nome.text, codinome.text));
                campo.onDeselect.AddListener(_ => Renomear(nome.text, codinome.text));
            }
            if (npc.Papel == Papel.Vilao)
                Nota("O nome de vilão sinaliza a geração; o nome real é opcional.");

            Secao("APARÊNCIA MARCANTE", "um maneirismo ou traço físico bem distinto");
            var aparencia = Estilo.AreaTexto(conteudo, "como reconhecer de longe", 90);
            aparencia.text = npc.Identidade.Aparencia;
            Estilo.ComExpandir(aparencia, "Aparência marcante");

            Secao("MODO DE AGIR", "uma palavra que resume como age");
            var modo = Estilo.CampoTexto(conteudo, "ex.: arrogante, cuidadosa, explosivo");
            modo.text = npc.Pnj?.ModoDeAgir ?? string.Empty;
            Altura(modo, 52);

            Secao("MOTIVAÇÃO", "verbo no infinitivo + o que quer");
            var motivacao = Estilo.CampoTexto(conteudo, "ex.: proteger a nação, vingar minha irmã");
            motivacao.text = npc.Pnj?.Motivacao ?? string.Empty;
            Altura(motivacao, 52);
            Nota(npc.Papel == Papel.Vilao
                ? "Motivação de vilão deixa a humanidade à mostra: algo fácil de entender e de sentir junto."
                : "Na dúvida sobre o que o NPC faz, a motivação responde.");
            void GravarPerfil()
            {
                cadastro.DefinirPerfilPnj(npc.Id, modo.text, motivacao.text);
                Rodape();
            }
            foreach (var campo in new[] { modo, motivacao })
            {
                campo.onSubmit.AddListener(_ => GravarPerfil());
                campo.onDeselect.AddListener(_ => GravarPerfil());
            }

            Secao("NOTAS", "quem é, de onde vem, o que esconde");
            var notas = Estilo.AreaTexto(conteudo, "anotações do mestre", 110);
            notas.text = npc.Identidade.Resumo;
            Estilo.ComExpandir(notas, "Notas");
            aparencia.onDeselect.AddListener(_ => cadastro.AlterarTextos(npc.Id, aparencia.text, notas.text));
            notas.onDeselect.AddListener(_ => cadastro.AlterarTextos(npc.Id, aparencia.text, notas.text));

            Secao("INFLUÊNCIA", null);
            Nota(npc.Maturidade == Maturidade.Adulto
                ? "Adulto: tem Influência sobre os heróis por padrão; só importa saber quando ganha ou perde."
                : "Jovem: começa sem Influência sobre os heróis; pergunte aos jogadores se os colegas têm.");

            if (npc.Papel == Papel.Vilao && npc.Vilao != null)
                FichaDeVilao();
            else
                Nota("NPC só marca condição se um herói o enfrentar diretamente. Para ter ficha de vilão, mude o papel para Vilão.");
        }

        void FichaDeVilao()
        {
            var v = npc.Vilao;
            Vao();
            Secao("FICHA DE VILÃO", null);

            var filaGeracao = FilaRotulada("GERAÇÃO");
            var opcoes = new System.Collections.Generic.List<(string id, string nome)> { (string.Empty, "escolher...") };
            opcoes.AddRange(Geracoes.Todas.Select(g => (g.Id, g.Nome)));
            Estilo.ListaSuspensa(filaGeracao, 300, opcoes, v.Geracao ?? string.Empty, id =>
            {
                cadastro.DefinirGeracao(npc.Id, string.IsNullOrEmpty(id) ? null : id);
                Redesenhar();
            });
            var g = Geracoes.Buscar(v.Geracao);
            var dica = Estilo.Rotulo(filaGeracao, g == null ? string.Empty : g.Dica, 20, Estilo.TextoApagado, FontStyles.Italic);
            Estilo.ColunaElastica(dica);

            Secao("MOVIMENTOS DE VILÃO", "frases ativas, com o que muda na cena (o livro sugere três ou mais)");
            for (var k = 0; k <= v.Movimentos.Count; k++)
            {
                var indice = k;
                var novo = k == v.Movimentos.Count;
                var campo = Estilo.CampoTexto(conteudo, novo ? "novo movimento e Enter" : "apague o texto para tirar");
                campo.text = novo ? string.Empty : v.Movimentos[k];
                Altura(campo, 52);
                Estilo.ComExpandir(campo, "Movimento de vilão");
                void Gravar(string t)
                {
                    var antes = v.Movimentos.Count;
                    cadastro.DefinirMovimentoDeVilao(npc.Id, indice, t);
                    // Acrescentou ou tirou: a lista mudou de tamanho, refaz. Só editou: não (Tab).
                    if (v.Movimentos.Count != antes)
                        Redesenhar();
                    else
                        Rodape();
                }
                campo.onSubmit.AddListener(Gravar);
                campo.onDeselect.AddListener(Gravar);
            }

            Secao("CONDIÇÕES", FichaVilao.Perigo(v.Condicoes.Count));
            var fila = Estilo.Fila(conteudo, 54, 10);
            foreach (var c in Condicoes.Todas)
            {
                var id = c;
                Chip(fila, Condicoes.Nome(c), v.Condicoes.Contains(c), () =>
                {
                    cadastro.AlternarCondicaoDeVilao(npc.Id, id);
                    Redesenhar();
                });
            }
            Nota("Ao levar um golpe forte, marca uma delas e age por ela; sem condição livre para marcar, está derrotado. " +
                 "Marcar na luta fica com o painel do mestre.");
        }

        // ---------- ações ----------

        void Renomear(string nome, string codinome)
        {
            var r = cadastro.RenomearNpc(npc.Id, nome, codinome);
            if (!r.Ok)
            {
                rodape.color = Estilo.Erro;
                rodape.text = r.Erro;
                return;
            }
            Cabecalho();
        }

        // ---------- peças ----------

        void Secao(string nome, string legenda)
        {
            var fila = Estilo.Fila(conteudo, 40);
            var t = Estilo.Rotulo(fila, nome, 22, Estilo.Dourado, FontStyles.Bold);
            t.characterSpacing = 5;
            t.gameObject.AddComponent<LayoutElement>().preferredWidth = 360;
            if (legenda != null)
            {
                var l = Estilo.Rotulo(fila, legenda, 20, Estilo.TextoSuave, FontStyles.Italic);
                Estilo.ColunaElastica(l);
            }
        }

        void Nota(string texto)
        {
            var n = Estilo.Rotulo(conteudo, texto, 20, Estilo.TextoApagado, FontStyles.Italic);
            n.textWrappingMode = TextWrappingModes.Normal;
        }

        void Vao()
        {
            var vao = Estilo.Rect("Vao", conteudo);
            vao.gameObject.AddComponent<LayoutElement>().minHeight = 14;
        }

        RectTransform FilaRotulada(string rotulo)
        {
            var fila = Estilo.Fila(conteudo, 58);
            var r = Estilo.Rotulo(fila, rotulo, 20, Estilo.TextoSuave, FontStyles.Bold);
            r.characterSpacing = 4;
            r.gameObject.AddComponent<LayoutElement>().preferredWidth = 240;
            return fila;
        }

        TMP_InputField CampoRotulado(string rotulo, string valor, string dica)
        {
            var fila = FilaRotulada(rotulo);
            var campo = Estilo.CampoTexto(fila, dica);
            campo.text = valor;
            Estilo.ColunaElastica(campo);
            return campo;
        }

        static void Altura(Component alvo, float altura)
        {
            var layout = alvo.GetComponent<LayoutElement>() ?? alvo.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = altura;
            layout.preferredHeight = altura;
        }

        static void Chip(Transform pai, string texto, bool escolhida, UnityEngine.Events.UnityAction aoClicar)
        {
            var rect = Estilo.Rect("Chip " + texto, pai);
            var imagem = escolhida
                ? Estilo.Moldura(rect.gameObject, Estilo.Dourado, Estilo.Dourado)
                : Estilo.Moldura(rect.gameObject, new Color(Estilo.Dourado.r, Estilo.Dourado.g, Estilo.Dourado.b, 0.08f), Estilo.Dourado);
            var botao = rect.gameObject.AddComponent<Button>();
            botao.targetGraphic = imagem;
            botao.onClick.AddListener(aoClicar);
            rect.gameObject.AddComponent<Tabulavel>();
            rect.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var t = Estilo.Rotulo(rect, texto, 22, escolhida ? Estilo.Cartao : Estilo.Dourado,
                escolhida ? FontStyles.Bold : FontStyles.Normal);
            t.alignment = TextAlignmentOptions.Center;
            Estilo.Esticar(t.rectTransform);
        }
    }
}
