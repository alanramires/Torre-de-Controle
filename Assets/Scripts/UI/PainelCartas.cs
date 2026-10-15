using System.Collections.Generic;
using System.Linq;
using TMPro;
using TorreDeControle.Core.Cartas;
using UnityEngine;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Folhear o catálogo de cartas desenhadas pela Unity (DesenhoDeCarta), por tipo. É a bancada para
    /// popular o Regras/cartas.json e ver o resultado na hora; a mesa (GameSetup) usa o mesmo desenho.
    /// </summary>
    public sealed class PainelCartas : MonoBehaviour
    {
        static readonly (string tipo, string nome)[] Tipos =
        {
            (Carta.Basico, "Básicos"), (Carta.Pvp, "PvP"), (Carta.Arquetipo, "Arquétipo"), (Carta.Equipe, "Equipe"),
            (Carta.Extra, "Extras"), (Carta.Adulto, "Adulto"), (Carta.Mestre, "Mestre"), (Carta.Referencia, "Referência"),
        };

        CatalogoDeCartas catalogo;
        string tipoAtual = Carta.Basico;
        RectTransform abas;
        RectTransform grade;
        TextMeshProUGUI contagem;

        public static PainelCartas Criar(Transform pai)
        {
            var veu = Estilo.Rect("PainelCartas", pai);
            Estilo.Esticar(veu);
            veu.gameObject.AddComponent<Image>().color = Estilo.Veu;
            var painel = veu.gameObject.AddComponent<PainelCartas>();
            painel.Montar(veu);
            return painel;
        }

        public void Abrir(CatalogoDeCartas catalogoDeCartas)
        {
            catalogo = catalogoDeCartas;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Redesenhar();
        }

        void Fechar() => gameObject.SetActive(false);

        void Montar(RectTransform veu)
        {
            var cartao = Estilo.Rect("Cartao", veu);
            cartao.anchorMin = cartao.anchorMax = new Vector2(0.5f, 0.5f);
            cartao.sizeDelta = new Vector2(1720, 960);
            Estilo.Moldura(cartao.gameObject, Estilo.Cartao, Estilo.Borda);
            var coluna = cartao.gameObject.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(40, 40, 30, 30);
            coluna.spacing = 12;
            coluna.childControlWidth = true;
            coluna.childControlHeight = true;
            coluna.childForceExpandHeight = false;

            var cabecalho = Estilo.Fila(cartao, 70);
            var titulos = Estilo.Rect("Titulos", cabecalho);
            titulos.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var pilha = titulos.gameObject.AddComponent<VerticalLayoutGroup>();
            pilha.childControlWidth = true;
            pilha.childControlHeight = true;
            pilha.childForceExpandHeight = false;
            var titulo = Estilo.Rotulo(titulos, "CARTAS", 40, Estilo.Dourado, FontStyles.Bold);
            titulo.characterSpacing = 6;
            contagem = Estilo.Rotulo(titulos, string.Empty, 22, Estilo.TextoSuave);
            Estilo.BotaoContorno(cabecalho, "Voltar", 190, Estilo.TextoSuave, Fechar);

            abas = Estilo.Fila(cartao, 46, 8);

            var lista = Estilo.ListaRolavel(cartao, 10);
            grade = Estilo.Rect("Grade", lista);
            var g = grade.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(DesenhoDeCarta.Largura, DesenhoDeCarta.Altura);
            g.spacing = new Vector2(16, 16);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 5;
            grade.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        void Redesenhar()
        {
            for (var i = abas.childCount - 1; i >= 0; i--)
                Destroy(abas.GetChild(i).gameObject);
            foreach (var (tipo, nome) in Tipos)
            {
                var alvo = tipo;
                var n = catalogo.DoTipo(tipo).Count;
                var rotulo = $"{nome} · {n}";
                var botao = tipo == tipoAtual
                    ? Estilo.BotaoPrincipal(abas, rotulo, 0, () => { })
                    : Estilo.BotaoContorno(abas, rotulo, 0, n == 0 ? Estilo.TextoApagado : Estilo.Dourado, () =>
                    {
                        tipoAtual = alvo;
                        Redesenhar();
                    });
                // As abas dividem a largura; o nome numa linha, a fonte encolhe se precisar.
                botao.GetComponent<LayoutElement>().flexibleWidth = 1;
                var t = botao.GetComponentInChildren<TextMeshProUGUI>();
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.enableAutoSizing = true;
                t.fontSizeMin = 12;
                t.fontSizeMax = 20;
                t.characterSpacing = 1;
            }

            for (var i = grade.childCount - 1; i >= 0; i--)
                Destroy(grade.GetChild(i).gameObject);
            var cartas = catalogo.DoTipo(tipoAtual).OrderBy(c => c.Origem).ThenBy(c => c.Titulo).ToList();
            foreach (var c in cartas)
                DesenhoDeCarta.Criar(grade, c);
            contagem.text = $"{catalogo.Todas.Count} cartas no catálogo · {cartas.Count(c => c.Ajuda == null)} sem texto aqui (popular o help)";
        }
    }
}
