using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using TorreDeControle.Core.Cartas;
using TorreDeControle.Infra;
using UnityEngine;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Editor de uma carta (desenho do Alan): cabeçalho (ícone de uma lista + nome), texto (corrido, com
    /// [barra] começando uma faixa nova e [cor=...]/[b] para destacar) e rodapé (benefícios, vários).
    /// Prévia ao vivo à esquerda. Salvar grava no projeto: nome e texto em Ajuda/catalogo.json, ícone e
    /// benefícios em Regras/cartas.json. Só rodando no Editor da Unity (num build não há pasta Assets).
    /// </summary>
    public sealed class PainelEditorDeCarta : MonoBehaviour
    {
        const string Barra = "[barra]";
        static readonly (string id, string nome)[] Beneficios =
        {
            ("equipe", "Equipe"), ("potencial", "Potencial"), ("influencia", "Influência"), ("adiante", "Adiante"),
            ("constante", "Constante"), ("controle", "Controle"), ("condicao", "−Condição"),
        };

        Carta carta;
        JObject entrada;
        Action aoSalvar;

        string icone;
        string categoria;
        string classe;
        string origem;
        RectTransform filaOrigem;
        string borda;
        RectTransform filaBorda;

        /// <summary>Cores de borda da casa (as mesmas convenções do help) e "automático" (pelo tipo da carta).</summary>
        static readonly (string hex, string nome)[] Bordas =
        {
            ("", "automático"), ("#E0B561", "dourado"), ("#D65353", "vermelho"), ("#73CC80", "verde (Influência)"),
            ("#5C9EF2", "azul (Equipe)"), ("#E8C547", "amarelo (Condições)"), ("#A06BD8", "roxo"),
            ("#C7CCD8", "prata"), ("#E08A3C", "laranja"), ("#5ED8E5", "ciano"),
        };
        RectTransform filaRota;
        TMP_InputField campoNome;
        TMP_InputField campoTexto;
        readonly HashSet<string> rodape = new HashSet<string>();

        RectTransform previa;
        RectTransform formulario;
        RectTransform fichas;
        TextMeshProUGUI mensagem;

        public static PainelEditorDeCarta Criar(Transform pai)
        {
            var veu = Estilo.Rect("PainelEditorDeCarta", pai);
            Estilo.Esticar(veu);
            veu.gameObject.AddComponent<Image>().color = Estilo.Veu;
            veu.gameObject.AddComponent<NavegacaoTab>();
            var painel = veu.gameObject.AddComponent<PainelEditorDeCarta>();
            painel.Montar(veu);
            return painel;
        }

        public void Abrir(Carta cartaDoJogo, Action depoisDeSalvar)
        {
            carta = cartaDoJogo;
            aoSalvar = depoisDeSalvar;
            entrada = (JObject)(DesenhoDeCarta.EntradaDoHelpAtual(carta.Ajuda)?.DeepClone() ?? new JObject
            {
                ["id"] = carta.Ajuda ?? carta.Id, ["titulo"] = carta.Titulo, ["secoes"] = new JArray(),
            });
            icone = carta.Icone ?? string.Empty;
            categoria = (string)entrada["categoria"] ?? PainelAjuda.categorias[0];
            origem = carta.Origem ?? "todos";
            borda = (string)entrada["borda"] ?? string.Empty;
            classe = (string)entrada["arquetipo"];
            rodape.Clear();
            foreach (var r in carta.Rodape)
                rodape.Add(r);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            MontarFormulario();
            Mostrar(null);
            AtualizarPrevia();
        }

        void Fechar() => gameObject.SetActive(false);

        // ---------- texto: seções <-> texto corrido com [barra] ----------

        static string ParaTexto(JArray secoes)
        {
            var partes = new List<string>();
            foreach (var s in secoes ?? new JArray())
                partes.Add($"{Barra} {(string)s["titulo"]}\n{(string)s["texto"]}");
            return string.Join("\n\n", partes);
        }

        static JArray ParaSecoes(string texto)
        {
            var secoes = new JArray();
            string titulo = null;
            var corpo = new List<string>();
            void Fechar()
            {
                var t = string.Join("\n", corpo).Trim('\n', ' ');
                if (titulo != null || t.Length > 0)
                    secoes.Add(new JObject { ["titulo"] = titulo ?? string.Empty, ["texto"] = t });
                corpo.Clear();
            }
            foreach (var linha in (texto ?? string.Empty).Replace("\r", string.Empty).Split('\n'))
            {
                if (linha.TrimStart().StartsWith(Barra, StringComparison.OrdinalIgnoreCase))
                {
                    Fechar();
                    titulo = linha.TrimStart().Substring(Barra.Length).Trim();
                }
                else
                    corpo.Add(linha);
            }
            Fechar();
            return secoes;
        }

        // ---------- prévia ----------

        (JObject entradaEditada, Carta cartaEditada) Editadas()
        {
            var e = (JObject)entrada.DeepClone();
            e["titulo"] = campoNome.text.Trim();
            e["categoria"] = categoria;
            if (categoria == "movimentos-arquetipo" && !string.IsNullOrEmpty(classe))
                e["arquetipo"] = classe;
            else
                e.Remove("arquetipo");
            e["secoes"] = ParaSecoes(campoTexto.text);
            var c = JsonConvert.DeserializeObject<Carta>(JsonConvert.SerializeObject(carta));
            c.Icone = string.IsNullOrEmpty(icone) ? null : icone;
            c.Rodape = Beneficios.Select(b => b.id).Where(rodape.Contains).ToList();
            c.Titulo = e["titulo"].ToString();
            c.Origem = origem;
            if (string.IsNullOrEmpty(borda)) e.Remove("borda"); else e["borda"] = borda;
            return (e, c);
        }

        void AtualizarPrevia()
        {
            for (var i = previa.childCount - 1; i >= 0; i--)
                Destroy(previa.GetChild(i).gameObject);
            var (e, c) = Editadas();
            DesenhoDeCarta.CriarDaAjuda(previa, e, rolavel: true, carta: c);
        }

        // ---------- salvar ----------

        void Salvar()
        {
            var arquivos = new ArquivosDoProjeto(Application.isEditor ? Application.dataPath : null);
            if (!arquivos.Disponivel)
            {
                Mostrar("Gravar o catálogo só funciona rodando no Editor da Unity.");
                return;
            }
            if (string.IsNullOrWhiteSpace(campoNome.text))
            {
                Mostrar("Dê um nome à carta.");
                return;
            }
            var (e, c) = Editadas();
            try
            {
                var help = JObject.Parse(DesenhoDeCarta.TextoDoHelp());
                var lista = (JArray)help["cartas"];
                var atual = lista.Cast<JObject>().FirstOrDefault(x => (string)x["id"] == (string)e["id"]);
                if (atual == null)
                    lista.Add(e);
                else
                {
                    atual["titulo"] = e["titulo"];
                    atual["secoes"] = e["secoes"];
                    atual["categoria"] = e["categoria"];
                    if (e["borda"] != null) atual["borda"] = e["borda"]; else atual.Remove("borda");
                    if (e["arquetipo"] != null) atual["arquetipo"] = e["arquetipo"]; else atual.Remove("arquetipo");
                }
                var cartas = JObject.Parse(DesenhoDeCarta.TextoDasCartas());
                var registro = ((JArray)cartas["cartas"]).Cast<JObject>().First(x => (string)x["id"] == carta.Id);
                registro["titulo"] = c.Titulo;
                registro["origem"] = c.Origem;
                if (c.Icone == null) registro.Remove("icone"); else registro["icone"] = c.Icone;
                if (c.Rodape.Count == 0) registro.Remove("rodape"); else registro["rodape"] = new JArray(c.Rodape);
                if (atual == null) registro["ajuda"] = e["id"];

                var textoHelp = help.ToString(Formatting.Indented) + "\n";
                var textoCartas = cartas.ToString(Formatting.Indented) + "\n";
                arquivos.Gravar("Resources/Ajuda/catalogo.json", textoHelp);
                arquivos.Gravar("Resources/Regras/cartas.json", textoCartas);
                DesenhoDeCarta.Recarregar(textoHelp, textoCartas);
            }
            catch (Exception ex)
            {
                Mostrar("Não deu para gravar: " + ex.Message);
                return;
            }
            Fechar();
            aoSalvar?.Invoke();
        }

        void Mostrar(string texto)
        {
            mensagem.color = Estilo.Erro;
            mensagem.text = texto ?? "[barra] Título começa uma faixa nova · [cor=verde]texto[/cor] · [b]negrito[/b] · cores: verde, azul, vermelho, dourado, laranja ou #hex";
            if (texto == null)
                mensagem.color = Estilo.TextoSuave;
        }

        // ---------- montagem ----------

        void Montar(RectTransform veu)
        {
            var cartao = Estilo.Rect("Cartao", veu);
            cartao.anchorMin = cartao.anchorMax = new Vector2(0.5f, 0.5f);
            cartao.sizeDelta = new Vector2(1600, 940);
            Estilo.Moldura(cartao.gameObject, Estilo.Cartao, Estilo.Borda);
            var coluna = cartao.gameObject.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(40, 40, 28, 28);
            coluna.spacing = 12;
            coluna.childControlWidth = true;
            coluna.childControlHeight = true;
            coluna.childForceExpandHeight = false;

            var cabecalho = Estilo.Fila(cartao, 60);
            var titulo = Estilo.Rotulo(cabecalho, "EDITAR CARTA", 36, Estilo.Dourado, FontStyles.Bold);
            titulo.characterSpacing = 5;
            Estilo.ColunaElastica(titulo);
            Estilo.BotaoPrincipal(cabecalho, "Salvar", 200, Salvar);
            Estilo.BotaoContorno(cabecalho, "Cancelar", 200, Estilo.TextoSuave, Fechar);

            var corpo = Estilo.Fila(cartao, 0, 24);
            var lc = corpo.GetComponent<LayoutElement>();
            lc.minHeight = lc.preferredHeight = 0;
            lc.flexibleHeight = 1;

            // Esquerda: a prévia, na proporção da carta.
            var esquerda = Estilo.Rect("Previa", corpo);
            var le = esquerda.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 520;
            le.flexibleWidth = 0;
            le.layoutPriority = 2;
            previa = Estilo.Rect("Area", esquerda);
            Estilo.Esticar(previa);
            previa.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            previa.GetComponent<AspectRatioFitter>().aspectRatio = DesenhoDeCarta.Largura / DesenhoDeCarta.Altura;

            // Direita: o formulário.
            formulario = Estilo.Rect("Formulario", corpo);
            formulario.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var vf = formulario.gameObject.AddComponent<VerticalLayoutGroup>();
            vf.spacing = 8;
            vf.childControlWidth = true;
            vf.childControlHeight = true;
            vf.childForceExpandHeight = false;

            mensagem = Estilo.Rotulo(cartao, string.Empty, 18, Estilo.TextoSuave);
            mensagem.textWrappingMode = TextWrappingModes.Normal;
        }

        void MontarFormulario()
        {
            for (var i = formulario.childCount - 1; i >= 0; i--)
                Destroy(formulario.GetChild(i).gameObject);

            Rotulo("ROTA NA ÁRVORE");
            filaRota = Estilo.Fila(formulario, 50, 12);
            MontarRota();

            Rotulo("CABEÇALHO");
            var filaIcone = Estilo.Fila(formulario, 50, 12);
            Estilo.Rotulo(filaIcone, "Ícone", 20, Estilo.TextoSuave).gameObject.AddComponent<LayoutElement>().preferredWidth = 150;
            Estilo.ListaSuspensa(filaIcone, 420, OpcoesDeIcone(), icone, id =>
            {
                icone = id;
                AtualizarPrevia();
            });
            Estilo.Rect("Vao", filaIcone).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Exclusiva de uma classe (o selo no rodapé) ou de todos.
            filaOrigem = Estilo.Fila(formulario, 50, 12);
            MontarOrigem();

            filaBorda = Estilo.Fila(formulario, 50, 12);
            MontarBorda();

            var filaNome = Estilo.Fila(formulario, 50, 12);
            Estilo.Rotulo(filaNome, "Nome", 20, Estilo.TextoSuave).gameObject.AddComponent<LayoutElement>().preferredWidth = 150;
            campoNome = Estilo.CampoTexto(filaNome, "nome da carta");
            campoNome.text = (string)entrada["titulo"] ?? carta.Titulo;
            Estilo.ColunaElastica(campoNome);
            campoNome.onValueChanged.AddListener(_ => AtualizarPrevia());

            Rotulo("TEXTO");
            campoTexto = Estilo.AreaTexto(formulario, "[barra] QUANDO\ntexto da faixa…", 330);
            campoTexto.text = ParaTexto(entrada["secoes"] as JArray);
            campoTexto.onValueChanged.AddListener(_ => AtualizarPrevia());
            Estilo.ComExpandir(campoTexto, "Texto da carta");

            Rotulo("RODAPÉ · BENEFÍCIOS");
            fichas = Estilo.Fila(formulario, 46, 8);
            MontarFichas();
        }

        /// <summary>Categoria da Biblioteca e, em Movimentos de arquétipo, a classe (o segundo nível).</summary>
        void MontarRota()
        {
            for (var i = filaRota.childCount - 1; i >= 0; i--)
                Destroy(filaRota.GetChild(i).gameObject);
            Estilo.Rotulo(filaRota, "Categoria", 20, Estilo.TextoSuave).gameObject.AddComponent<LayoutElement>().preferredWidth = 150;
            var opcoes = PainelAjuda.categorias.Select((c, k) => (c, PainelAjuda.categoriasNomes[k])).ToList();
            Estilo.ListaSuspensa(filaRota, 340, opcoes, categoria, id =>
            {
                categoria = id;
                MontarRota();
                AtualizarPrevia();
            });
            if (categoria == "movimentos-arquetipo")
            {
                var classes = Core.Personagens.Arquetipos.Todos.Select(a => (a.Nome, a.Nome)).ToList();
                classe ??= classes.FirstOrDefault().Item1;
                Estilo.ListaSuspensa(filaRota, 300, classes, classe, id =>
                {
                    classe = id;
                    // Movimento de uma classe é, em geral, exclusivo dela: acompanha (dá para trocar abaixo).
                    var arquetipo = Core.Personagens.Arquetipos.Todos.FirstOrDefault(a => a.Nome == id);
                    if (arquetipo != null)
                    {
                        origem = arquetipo.Id;
                        MontarOrigem();
                    }
                    AtualizarPrevia();
                });
            }
            Estilo.Rect("Vao", filaRota).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        /// <summary>Cor da borda: uma das da casa, ou um hex próprio (#RRGGBB) digitado ao lado.</summary>
        void MontarBorda()
        {
            for (var i = filaBorda.childCount - 1; i >= 0; i--)
                Destroy(filaBorda.GetChild(i).gameObject);
            Estilo.Rotulo(filaBorda, "Borda", 20, Estilo.TextoSuave).gameObject.AddComponent<LayoutElement>().preferredWidth = 150;
            var opcoes = Bordas.Select(b => (b.hex, b.nome)).ToList();
            // A cor gravada pode vir em minúsculas (#d65353): casa com a da casa ignorando a caixa.
            var daCasa = Bordas.FirstOrDefault(b => string.Equals(b.hex, borda ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            var selecionada = daCasa.nome != null ? daCasa.hex : borda ?? string.Empty;
            if (daCasa.nome == null && !string.IsNullOrEmpty(borda))
                opcoes.Add((borda, "própria: " + borda));
            Estilo.ListaSuspensa(filaBorda, 300, opcoes, selecionada, id =>
            {
                borda = id;
                MontarBorda();
                AtualizarPrevia();
            });
            // Amostra da cor escolhida.
            var amostra = Estilo.Rect("Amostra", filaBorda);
            var la = amostra.gameObject.AddComponent<LayoutElement>();
            la.preferredWidth = 40;
            la.flexibleWidth = 0;
            var cor = ColorUtility.TryParseHtmlString(borda ?? string.Empty, out var c) ? c : new Color(0, 0, 0, 0);
            Estilo.Moldura(amostra.gameObject, cor, Estilo.Borda);
            var hex = Estilo.CampoTexto(filaBorda, "#RRGGBB");
            hex.text = borda;
            if (!hex.TryGetComponent<LayoutElement>(out var lh)) lh = hex.gameObject.AddComponent<LayoutElement>();
            lh.preferredWidth = 160;
            lh.flexibleWidth = 0;
            hex.onEndEdit.AddListener(t =>
            {
                var limpo = t.Trim();
                if (limpo.Length > 0 && !limpo.StartsWith("#"))
                    limpo = "#" + limpo;
                if (limpo.Length > 0 && !ColorUtility.TryParseHtmlString(limpo, out _))
                {
                    Mostrar("Cor inválida: use #RRGGBB (ex.: #A06BD8).");
                    return;
                }
                borda = limpo.ToUpperInvariant();
                MontarBorda();
                AtualizarPrevia();
            });
            Estilo.Rect("Vao", filaBorda).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        void MontarOrigem()
        {
            if (filaOrigem == null)
                return;
            for (var i = filaOrigem.childCount - 1; i >= 0; i--)
                Destroy(filaOrigem.GetChild(i).gameObject);
            Estilo.Rotulo(filaOrigem, "Exclusiva de", 20, Estilo.TextoSuave).gameObject.AddComponent<LayoutElement>().preferredWidth = 150;
            var opcoes = new List<(string, string)> { ("todos", "todos (sem selo)"), ("mestre", "mestre") };
            opcoes.AddRange(Core.Personagens.Arquetipos.Todos.Select(a => (a.Id, a.Nome)));
            Estilo.ListaSuspensa(filaOrigem, 420, opcoes, origem, id =>
            {
                origem = id;
                AtualizarPrevia();
            });
            Estilo.Rect("Vao", filaOrigem).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        void MontarFichas()
        {
            for (var i = fichas.childCount - 1; i >= 0; i--)
                Destroy(fichas.GetChild(i).gameObject);
            foreach (var (id, nome) in Beneficios)
            {
                var alvo = id;
                var marcado = rodape.Contains(id);
                UnityEngine.Events.UnityAction alternar = () =>
                {
                    if (!rodape.Remove(alvo))
                        rodape.Add(alvo);
                    MontarFichas();
                    AtualizarPrevia();
                };
                var b = marcado
                    ? Estilo.BotaoPrincipal(fichas, nome, 0, alternar)
                    : Estilo.BotaoContorno(fichas, nome, 0, Estilo.Dourado, alternar);
                b.GetComponent<LayoutElement>().flexibleWidth = 1;
                var t = b.GetComponentInChildren<TextMeshProUGUI>();
                t.enableAutoSizing = true;
                t.fontSizeMin = 12;
                t.fontSizeMax = 18;
                t.characterSpacing = 1;
            }
        }

        /// <summary>Título de seção do formulário, com um respiro antes (menos no primeiro).</summary>
        void Rotulo(string texto)
        {
            if (formulario.childCount > 0)
            {
                var vao = Estilo.Rect("Vao", formulario).gameObject.AddComponent<LayoutElement>();
                vao.minHeight = vao.preferredHeight = 12;
            }
            var r = Estilo.Rotulo(formulario, texto, 20, Estilo.Dourado, FontStyles.Bold);
            r.characterSpacing = 4;
            var l = r.gameObject.AddComponent<LayoutElement>();
            l.minHeight = l.preferredHeight = 30;
        }

        /// <summary>Automático, nenhum, e todos os ícones de movimento e moedas que existem em Resources/Icones.</summary>
        static IReadOnlyList<(string id, string nome)> OpcoesDeIcone()
        {
            var opcoes = new List<(string, string)> { (string.Empty, "automático"), ("nenhum", "nenhum") };
            foreach (var t in Resources.LoadAll<Texture2D>("Icones/Movimentos").OrderBy(t => t.name))
                opcoes.Add(("Movimentos/" + t.name, "movimento: " + t.name));
            foreach (var t in Resources.LoadAll<Texture2D>("Icones/Tokens").OrderBy(t => t.name))
                opcoes.Add(("Tokens/" + t.name, "token: " + t.name));
            return opcoes;
        }
    }
}
