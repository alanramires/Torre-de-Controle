using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TorreDeControle.Core;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Regras;
using UnityEngine;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Escolher a Influência inicial de uma personagem (na campanha, em aprovação): o comportamento, quando
    /// a regra tem, e os colegas a quem dar, com o confirmar ("Dar Influência a todos" quando a regra é
    /// todos). Usado na ficha (onde o jogador marca) e na aba Influência da sala (a visão do mestre).
    /// </summary>
    public static class SeletorDeInfluencia
    {
        /// <summary>Colegas marcados antes de confirmar, por personagem (some ao confirmar).</summary>
        static readonly Dictionary<string, HashSet<string>> preSelecao = new Dictionary<string, HashSet<string>>();

        /// <summary>Play sem recarregar o domínio: a pré-seleção começa vazia a cada Play.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimparAoEntrarNoPlay() => preSelecao.Clear();

        /// <summary>Os botões de comportamento (Nova, Forasteira, Discípula), com a quantidade de cada um.</summary>
        public static void MontarComportamento(Transform pai, Personagem p, CadastroPersonagens cadastro,
            Action<Resultado> aoMudar)
        {
            var regra = cadastro.RegraDoArquetipo(p)?.Influencia;
            if (regra?.Regra != RegrasArquetipos.RegraInfluencia.Comportamento)
                return;
            var fila = Estilo.Fila(pai, 52, 10);
            foreach (var o in regra.Opcoes)
            {
                var id = o.Id;
                var rotulo = $"{o.Nome} · {(o.ParaTodos ? "todos" : $"{o.QuantidadeNumero}")}";
                if (p.InfluenciaComportamento == o.Id)
                    Estilo.BotaoPrincipal(fila, rotulo, 300, () => { });
                else
                    Estilo.BotaoContorno(fila, rotulo, 300, Estilo.Dourado, () =>
                    {
                        preSelecao.Remove(p.Id);
                        aoMudar(cadastro.EscolherComportamento(p.Id, id));
                    });
            }
        }

        /// <summary>
        /// Os colegas para marcar e o confirmar, conforme a regra do arquétipo. Os colegas são chips numa grade
        /// (como as habilidades): no limite, os que sobram travam. <paramref name="colunas"/>: 4 na ficha, 1 na
        /// caixa estreita da sala.
        /// </summary>
        public static void MontarEscolha(Transform pai, Personagem p, CadastroPersonagens cadastro,
            Action<Resultado> aoMudar, int colunas = 4, float larguraDoChip = 335)
        {
            var colegas = cadastro.Colegas(p);
            var quantas = cadastro.QuantasInfluencias(p);
            void Confirmar(IEnumerable<string> ids)
            {
                var r = cadastro.DarInfluencia(p.Id, ids);
                if (r.Ok)
                    preSelecao.Remove(p.Id);
                aoMudar(r);
            }

            if (colegas.Count == 0)
                Nota(pai, "Ainda não há colegas nas vagas.");
            else if (quantas == null)
                Nota(pai, "Escolha o comportamento para saber a quantos colegas dar.");
            else if (cadastro.InfluenciaParaTodos(p))
            {
                Nota(pai, $"A regra é dar a todos os colegas ({colegas.Count}).");
                BotaoConfirmar(pai, "Dar Influência a todos", () => Confirmar(colegas.Select(c => c.Id)));
            }
            else if (quantas.Value == 0)
            {
                Nota(pai, "A regra é não dar Influência a ninguém.");
                BotaoConfirmar(pai, "Confirmar: a ninguém", () => Confirmar(new string[0]));
            }
            else
            {
                if (!preSelecao.TryGetValue(p.Id, out var marcados))
                    preSelecao[p.Id] = marcados = new HashSet<string>(cadastro.InfluenciaDada(p));
                Nota(pai, $"Marque {quantas.Value} colega{(quantas.Value == 1 ? "" : "s")} " +
                          $"({marcados.Count}/{quantas.Value}).");
                var grade = Estilo.Rect("Grade", pai);
                var g = grade.gameObject.AddComponent<GridLayoutGroup>();
                g.cellSize = new Vector2(larguraDoChip, 46);
                g.spacing = new Vector2(10, 10);
                g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                g.constraintCount = colunas;
                var cheio = marcados.Count >= quantas.Value;
                foreach (var c in colegas)
                {
                    var id = c.Id;
                    var marcado = marcados.Contains(id);
                    Chip(grade, c.NomeExibicao, marcado, !marcado && cheio, () =>
                    {
                        if (!marcados.Remove(id))
                            marcados.Add(id);
                        aoMudar(Resultado.Sucesso());
                    });
                }
                var confirmar = BotaoConfirmar(pai, "Dar Influência  ›", () => Confirmar(marcados));
                confirmar.interactable = marcados.Count == quantas.Value; // só com a conta certa
            }
        }

        /// <summary>
        /// Para cada colega a quem a personagem deu Influência, uma caixinha opcional: por que essa pessoa tem
        /// Influência sobre ela (roleplay). Grava enquanto digita; ••• para texto longo.
        /// </summary>
        public static void MontarPorques(Transform pai, Personagem p, CadastroPersonagens cadastro)
        {
            var dados = cadastro.InfluenciaDada(p);
            var perdidas = cadastro.InfluenciaPerdida(p);
            if (dados.Count + perdidas.Count == 0)
            {
                // Confirmou "a ninguém" (profissional, arrogante...): também tem um porquê, para o roleplay.
                if (cadastro.InfluenciaConfirmada(p))
                {
                    var fila = Estilo.Fila(pai, 90, 12);
                    var r = Estilo.Rotulo(fila, "Ninguém", 22, Estilo.Dourado, FontStyles.Bold);
                    r.gameObject.AddComponent<LayoutElement>().preferredWidth = 260;
                    var campo = Estilo.AreaTexto(fila, "por que ninguém tem Influência sobre você? (opcional)", 90);
                    campo.text = p.InfluenciaPorqueNinguem;
                    Estilo.ColunaElastica(campo);
                    campo.onValueChanged.AddListener(t => cadastro.DescreverInfluenciaDeNinguem(p.Id, t));
                    Estilo.ComExpandir(campo, $"Por que ninguém tem Influência sobre {p.NomeExibicao}");
                }
                return;
            }
            Nota(pai, "Por que cada um tem Influência sobre você (opcional):");
            foreach (var id in dados.Concat(perdidas))
            {
                var colega = id;
                var perdida = perdidas.Contains(id);
                var nome = (cadastro.Buscar(colega)?.NomeExibicao ?? "?") + (perdida ? " (perdida)" : string.Empty);
                var fila = Estilo.Fila(pai, 90, 12);
                var r = Estilo.Rotulo(fila, nome, 22, perdida ? Estilo.TextoApagado : Estilo.Dourado, FontStyles.Bold);
                r.textWrappingMode = TextWrappingModes.NoWrap;
                r.overflowMode = TextOverflowModes.Ellipsis;
                r.gameObject.AddComponent<LayoutElement>().preferredWidth = 260;
                // Várias linhas: um texto colado com quebras não vaza da caixa (rola dentro; ••• para ler inteiro).
                var campo = Estilo.AreaTexto(fila, "por quê? (opcional)", 90);
                campo.text = cadastro.PorqueDaInfluencia(p, colega);
                Estilo.ColunaElastica(campo);
                campo.onValueChanged.AddListener(t => cadastro.DescreverInfluencia(p.Id, colega, t));
                Estilo.ComExpandir(campo, $"Por que {nome} tem Influência sobre {p.NomeExibicao}");
            }
        }

        /// <summary>Chip de colega, no visual das habilidades: marcado (dourado), livre (contorno), travado (cinza).</summary>
        static void Chip(Transform pai, string texto, bool marcado, bool travado, UnityEngine.Events.UnityAction aoClicar)
        {
            var rect = Estilo.Rect("Chip " + texto, pai);
            var (fundo, borda, cor) = marcado ? (Estilo.Dourado, Estilo.Dourado, Estilo.Cartao)
                : travado ? (Estilo.Campo, Estilo.BordaApagada, Estilo.TextoApagado)
                : (new Color(Estilo.Dourado.r, Estilo.Dourado.g, Estilo.Dourado.b, 0.08f), Estilo.Dourado, Estilo.Dourado);
            var imagem = Estilo.Moldura(rect.gameObject, fundo, borda);
            var botao = rect.gameObject.AddComponent<Button>();
            botao.targetGraphic = imagem;
            botao.interactable = !travado;
            botao.onClick.AddListener(aoClicar);
            rect.gameObject.AddComponent<Tabulavel>();
            var t = Estilo.Rotulo(rect, texto, 22, cor, marcado ? FontStyles.Bold : FontStyles.Normal);
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            Estilo.Esticar(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(10, 0);
            t.rectTransform.offsetMax = new Vector2(-10, 0);
        }

        /// <summary>O botão de confirmar numa linha própria, com largura fixa (não estica na coluna).</summary>
        static Button BotaoConfirmar(Transform pai, string texto, UnityEngine.Events.UnityAction acao)
        {
            var fila = Estilo.Fila(pai, 50);
            var botao = Estilo.BotaoPrincipal(fila, texto, 320, acao);
            Estilo.Rect("Vao", fila).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            return botao;
        }

        static void Nota(Transform pai, string texto)
        {
            var n = Estilo.Rotulo(pai, texto, 20, Estilo.TextoSuave, FontStyles.Italic);
            n.textWrappingMode = TextWrappingModes.Normal;
        }
    }
}
