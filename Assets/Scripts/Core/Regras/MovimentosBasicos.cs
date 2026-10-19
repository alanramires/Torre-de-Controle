using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TorreDeControle.Core.Regras
{
    /// <summary>
    /// Os movimentos básicos de Regras/movimentos_basicos.json: id, nome, gatilho e o que a rolagem soma
    /// (um Rótulo, ou as condições marcadas no golpe poderoso). Renomear lá muda aqui.
    /// </summary>
    public sealed class MovimentosBasicos
    {
        public const string Arquivo = "movimentos_basicos";

        public IReadOnlyList<Movimento> Todos { get; }

        /// <summary>Os que rolam com um Rótulo (todos menos o golpe poderoso, que soma condições).</summary>
        public IReadOnlyList<Movimento> ComRotulo => Todos.Where(m => m.Rotulo != null).ToList();

        MovimentosBasicos(IReadOnlyList<Movimento> todos) => Todos = todos;

        public static MovimentosBasicos Carregar(IFonteDeRegras fonte)
        {
            var texto = fonte.Ler(Arquivo) ?? throw new InvalidOperationException($"Regras/{Arquivo}.json não encontrado.");
            var arquivo = JsonConvert.DeserializeObject<ArquivoJson>(texto) ?? new ArquivoJson();
            return new MovimentosBasicos(arquivo.Movimentos);
        }

        public sealed class Movimento
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("nome")] public string Nome { get; set; }
            [JsonProperty("gatilho")] public string Gatilho { get; set; }
            /// <summary>O comando do console que deixa a rolagem pendente no log ("eda", "tgp"), ou null.</summary>
            [JsonProperty("atalho")] public string Atalho { get; set; }
            [JsonProperty("rolagem")] public RegraDaRolagem Rolagem { get; set; }
            /// <summary>O movimento precisa de "contra quem" (o Enfrentar: sem oponente, é liberar os poderes).</summary>
            [JsonProperty("pede_alvo")] public bool PedeAlvo { get; set; }
            /// <summary>O que vale em qualquer sucesso (7+), antes das escolhas.</summary>
            [JsonProperty("sucesso_base")] public JToken SucessoBase { get; set; }
            [JsonProperty("forte")] public JToken Forte { get; set; }
            [JsonProperty("parcial")] public JToken Parcial { get; set; }
            /// <summary>Sem falha própria, vale a falha padrão (nucleo.json): potencial e movimento do MJ.</summary>
            [JsonProperty("falha")] public JToken Falha { get; set; }
            [JsonProperty("opcoes")] public JToken Opcoes { get; set; }
            [JsonProperty("perguntas")] public JToken Perguntas { get; set; }

            /// <summary>As faixas são invertidas: rolar alto é pior (o golpe poderoso).</summary>
            [JsonProperty("invertido")] public bool Invertido { get; set; }

            /// <summary>A rolagem não aceita intervenção depois do dado (o golpe poderoso).</summary>
            [JsonProperty("sem_intervencao")] public bool SemIntervencao { get; set; }

            /// <summary>O Rótulo que a rolagem soma, ou null (golpe poderoso).</summary>
            [JsonIgnore] public string Rotulo => Rolagem?.Rotulo;

            /// <summary>A rolagem soma as condições marcadas de quem rola, no lugar de um Rótulo (o golpe poderoso).</summary>
            [JsonIgnore] public bool SomaCondicoes => Rolagem?.Base == "condicoes_marcadas";

            /// <summary>
            /// O efeito esperado em linhas para a tela: o que vale em qualquer sucesso, no 10+, no 7–9 e na falha.
            /// Cada linha diz a faixa em que vale (a tela apaga as que não valem para o resultado).
            /// Quando 10+ e 7–9 escolhem da mesma lista, a lista aparece uma vez só.
            /// </summary>
            public IReadOnlyList<LinhaDeEfeito> EfeitoEsperado()
            {
                var linhas = new List<LinhaDeEfeito>();
                if (SucessoBase != null)
                    Bloco(linhas, Faixa.Sucesso, "Num sucesso (7+):", SucessoBase);
                var listaForte = ListaDeEscolha(Forte);
                if (listaForte != null && listaForte == ListaDeEscolha(Parcial))
                {
                    linhas.Add(new LinhaDeEfeito(Faixa.Sucesso,
                        $"Num 10+, escolha {QuantasEscolhas(Forte)}. Num 7–9, escolha {QuantasEscolhas(Parcial)}:", 0));
                    foreach (var item in Itens(Lista(listaForte)))
                        linhas.Add(new LinhaDeEfeito(Faixa.Sucesso, item, 1));
                }
                else
                {
                    if (Forte != null)
                        Bloco(linhas, Faixa.Forte, "Num 10+:", Forte);
                    if (Parcial != null)
                        Bloco(linhas, Faixa.Parcial, "Num 7–9:", Parcial);
                }
                Bloco(linhas, Faixa.Falha, "Na falha (6−):", Falha ?? FalhaPadrao);
                return linhas;
            }

            static readonly JToken FalhaPadrao = JArray.Parse("[{\"op\":\"marcar_potencial\"},{\"op\":\"movimento_do_mj\"}]");

            void Bloco(List<LinhaDeEfeito> linhas, Faixa faixa, string titulo, JToken efeito)
            {
                linhas.Add(new LinhaDeEfeito(faixa, titulo, 0));
                Formatar(efeito, faixa, 1, linhas);
            }

            void Formatar(JToken t, Faixa faixa, int nivel, List<LinhaDeEfeito> linhas)
            {
                if (t is JArray lista)
                {
                    foreach (var x in lista)
                        Formatar(x, faixa, nivel, linhas);
                    return;
                }
                if (!(t is JObject o))
                    return;
                switch ((string)o["op"])
                {
                    case "escolher":
                        linhas.Add(new LinhaDeEfeito(faixa, $"escolha {(int?)o["n"] ?? 1}:", nivel));
                        var itens = o["opcoes"] != null ? Itens(o["opcoes"])
                            : o["lista"] != null ? Itens(Lista((string)o["lista"]))
                            : o["opcoes_ref"] is JArray refs ? refs.Select(r => TextoDaOpcao((string)r)).ToList()
                            : new List<string>();
                        foreach (var item in itens)
                            linhas.Add(new LinhaDeEfeito(faixa, item, nivel + 1));
                        return;
                    case "perguntar":
                        var n = (int?)o["n"] ?? 1;
                        linhas.Add(new LinhaDeEfeito(faixa, $"faça {n} pergunta{(n == 1 ? "" : "s")}:", nivel));
                        foreach (var item in Itens(Lista((string)o["lista"] ?? "perguntas")))
                            linhas.Add(new LinhaDeEfeito(faixa, item, nivel + 1));
                        return;
                    case "alvo_decide_se_abre":
                        linhas.Add(new LinhaDeEfeito(faixa, "se o alvo se abrir:", nivel));
                        Formatar(o["se_abrir"], faixa, nivel + 1, linhas);
                        return;
                    case "marcar_potencial":
                        linhas.Add(new LinhaDeEfeito(faixa, "marque potencial", nivel));
                        return;
                    case "movimento_do_mj":
                        linhas.Add(new LinhaDeEfeito(faixa, "o MJ faz um movimento", nivel));
                        return;
                }
                if (o["texto"] != null)
                    linhas.Add(new LinhaDeEfeito(faixa, (string)o["texto"], nivel));
            }

            /// <summary>Num sucesso, quem rolou e o alvo trocam golpes (op "trocar_golpes" no sucesso_base).</summary>
            [JsonIgnore] public bool TrocaGolpes => TrocaDoSucesso != null;

            /// <summary>A condição que o alvo marca de costume na troca ("irritado"), ou null.</summary>
            [JsonIgnore] public string CondicaoPadraoDoAlvo =>
                (string)(TrocaDoSucesso?["alvo"] as JArray)?.OfType<JObject>().FirstOrDefault()?["padrao"];

            JObject TrocaDoSucesso => SucessoBase is JObject o
                ? ((string)o["op"] == "trocar_golpes" ? o : null)
                : (SucessoBase as JArray)?.OfType<JObject>().FirstOrDefault(x => (string)x["op"] == "trocar_golpes");

            /// <summary>Quanto a alternativa marcada de uma opção muda a pilha (a oportunidade: +1 Equipe); 0 se não muda.</summary>
            public int EquipeDaAlternativa(string opcao, int alternativa)
            {
                var efeitos = (Opcoes as JArray)?.OfType<JObject>().FirstOrDefault(x => (string)x["id"] == opcao)?["efeitos"] as JArray;
                var decide = efeitos?.OfType<JObject>().FirstOrDefault(e => (string)e["op"] == "mj_decide");
                if (!(decide?["entre"] is JArray entre) || alternativa < 0 || alternativa >= entre.Count)
                    return 0;
                return ((entre[alternativa] as JArray) ?? new JArray(entre[alternativa])).OfType<JObject>()
                    .Where(e => (string)e["op"] == "equipe").Sum(e => (int?)e["delta"] ?? 1);
            }

            /// <summary>
            /// A alternativa vale contra um alvo PJ? A opção com "contra_pj": "sem_equipe" não dá pontos à pilha
            /// brigando com colega (a oportunidade: só a chance a um aliado).
            /// </summary>
            public bool AlternativaValeContraPj(string opcao, int alternativa)
            {
                var o = (Opcoes as JArray)?.OfType<JObject>().FirstOrDefault(x => (string)x["id"] == opcao);
                return (string)o?["contra_pj"] != "sem_equipe" || EquipeDaAlternativa(opcao, alternativa) == 0;
            }

            /// <summary>A opção deixa quem rolou ileso na troca ("resistir").</summary>
            public bool AnulaRetorno(string opcao) => TemEfeito(opcao, "anular_retorno_da_troca");

            /// <summary>A opção faz a condição da troca mostrar a nova percepção do alvo ("impressionar").</summary>
            public bool ReflectePercepcao(string opcao) => TemEfeito(opcao, "condicao_da_troca_reflete_percepcao");

            bool TemEfeito(string opcao, string op) => EfeitoDaOpcao(opcao, op) != null;

            JObject EfeitoDaOpcao(string opcao, string op) =>
                (OpcaoPorId(opcao)?["efeitos"] as JArray)?.OfType<JObject>().FirstOrDefault(e => (string)e["op"] == op);

            /// <summary>A opção pelo id, nas opções do movimento ou dentro de uma escolha de faixa (inline).</summary>
            JObject OpcaoPorId(string id) =>
                (Opcoes as JArray)?.OfType<JObject>().FirstOrDefault(x => (string)x["id"] == id)
                ?? new[] { Faixa.Forte, Faixa.Parcial }.Select(EscolhaDaFaixa).Where(o => o?["opcoes"] is JArray)
                    .SelectMany(o => ((JArray)o["opcoes"]).OfType<JObject>()).FirstOrDefault(x => (string)x["id"] == id);

            /// <summary>
            /// Quantas condições a opção manda quem rolou marcar ("aguentar a dor": 2); 0 se não manda. Quem marca escolhe quais.
            /// </summary>
            public int CondicoesQueMarca(string opcao)
            {
                var e = EfeitoDaOpcao(opcao, "marcar_condicao");
                return e != null && (string)e["quem"] == "rolou" ? (int?)e["n"] ?? 1 : 0;
            }

            /// <summary>
            /// A escolha de dentro da opção ("duas opções da lista de 7–9": escolha 2 entre aquelas), ou nenhuma.
            /// </summary>
            public (int n, IReadOnlyList<OpcaoDeEscolha> opcoes) OpcoesDentro(string opcao)
            {
                var e = EfeitoDaOpcao(opcao, "escolher");
                return e == null ? (0, new List<OpcaoDeEscolha>()) : ((int?)e["n"] ?? 1, OpcoesDe(e));
            }

            /// <summary>Quantas opções o resultado escolhe nesta faixa (Forte = 10+, Parcial = 7–9); 0 se não escolhe.</summary>
            public int EscolhasNo(Faixa faixa) => EscolhaDaFaixa(faixa) is JObject o ? (int?)o["n"] ?? 1 : 0;

            /// <summary>As opções que o resultado pode escolher nesta faixa, com as alternativas de dentro de cada uma.</summary>
            public IReadOnlyList<OpcaoDeEscolha> OpcoesNo(Faixa faixa)
            {
                return EscolhaDaFaixa(faixa) is JObject o ? OpcoesDe(o) : new List<OpcaoDeEscolha>();
            }

            /// <summary>As opções de uma escolha (op "escolher"): inline, de uma lista do movimento ou por referência.</summary>
            IReadOnlyList<OpcaoDeEscolha> OpcoesDe(JObject o)
            {
                IEnumerable<JObject> brutas = o["opcoes"] is JArray inline ? inline.OfType<JObject>()
                    : o["lista"] != null ? (Lista((string)o["lista"]) as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>()
                    : o["opcoes_ref"] is JArray refs ? refs.Select(r => (Opcoes as JArray)?.OfType<JObject>().FirstOrDefault(x => (string)x["id"] == (string)r)).Where(x => x != null)
                    : Enumerable.Empty<JObject>();
                return brutas.Select(x => new OpcaoDeEscolha((string)x["id"], (string)x["texto"], Alternativas(x["efeitos"]))).ToList();
            }

            /// <summary>A escolha (op "escolher") de uma faixa, no topo ou dentro da lista de efeitos dela.</summary>
            JObject EscolhaDaFaixa(Faixa faixa)
            {
                var t = faixa == Faixa.Forte ? Forte : faixa == Faixa.Parcial ? Parcial : null;
                if (t is JObject o)
                    return (string)o["op"] == "escolher" ? o : null;
                return (t as JArray)?.OfType<JObject>().FirstOrDefault(x => (string)x["op"] == "escolher");
            }

            /// <summary>
            /// As alternativas dentro de uma opção: "mj_decide" entre efeitos (uma por alternativa) ou "mj_pode"
            /// (sim, com o efeito, ou não). Sem isso, a opção não tem alternativas.
            /// </summary>
            static List<string> Alternativas(JToken efeitos)
            {
                foreach (var e in (efeitos as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                {
                    if ((string)e["op"] == "mj_decide" && e["entre"] is JArray entre)
                        return entre.Select(Descrever).ToList();
                    if ((string)e["op"] == "mj_pode")
                        return new List<string> { "sim: " + Descrever(e["efeitos"]), "não" };
                }
                return new List<string>();
            }

            static string Descrever(JToken efeitos) => string.Join("; ", ((efeitos as JArray) ?? new JArray(efeitos))
                .OfType<JObject>().Select(e => (string)e["op"] switch
                {
                    "equipe" => $"{((int?)e["delta"] ?? 1):+0;−0} Equipe",
                    "marcar_condicao" => (string)e["quem"] == "alvo" ? "o alvo marca uma condição" : "marca uma condição",
                    _ => (string)e["texto"] ?? (string)e["op"],
                }));

            JToken Lista(string nome) => nome == "perguntas" ? Perguntas : nome == "opcoes" ? Opcoes : null;

            string TextoDaOpcao(string id) =>
                (Opcoes as JArray)?.OfType<JObject>().FirstOrDefault(x => (string)x["id"] == id)?["texto"]?.ToString() ?? id;

            static List<string> Itens(JToken lista) =>
                (lista as JArray)?.Select(x => x is JObject o ? (string)(o["texto"] ?? o["pergunta"] ?? o["id"]) : x.ToString()).ToList()
                ?? new List<string>();

            /// <summary>O nome da lista de onde o resultado escolhe ("opcoes"), se for uma escolha de lista.</summary>
            static string ListaDeEscolha(JToken t) => (string)Escolha(t)?["lista"];

            static int QuantasEscolhas(JToken t) => (int?)Escolha(t)?["n"] ?? 1;

            static JObject Escolha(JToken t) => t is JObject o && (string)o["op"] == "escolher" ? o
                : t is JArray a && a.Count == 1 ? Escolha(a[0]) : null;
        }

        /// <summary>Uma opção que o resultado pode escolher, com as alternativas de dentro dela (pode não ter).</summary>
        public sealed class OpcaoDeEscolha
        {
            public string Id { get; }
            public string Texto { get; }
            public IReadOnlyList<string> Alternativas { get; }

            public OpcaoDeEscolha(string id, string texto, IReadOnlyList<string> alternativas)
            {
                Id = id;
                Texto = texto;
                Alternativas = alternativas;
            }
        }

        /// <summary>Em que resultado uma linha do efeito vale.</summary>
        public enum Faixa { Sucesso, Forte, Parcial, Falha }

        /// <summary>A faixa vale para este total? Sucesso é 7+ (10+ e 7–9 são sucessos), falha é 6−.</summary>
        public static bool Vale(Faixa faixa, int total) => faixa switch
        {
            Faixa.Sucesso => total >= 7,
            Faixa.Forte => total >= 10,
            Faixa.Parcial => total >= 7 && total <= 9,
            _ => total <= 6,
        };

        public sealed class LinhaDeEfeito
        {
            public Faixa Faixa { get; }
            public string Texto { get; }
            /// <summary>0: cabeçalho da faixa; 1+: item (recuado).</summary>
            public int Nivel { get; }

            public LinhaDeEfeito(Faixa faixa, string texto, int nivel)
            {
                Faixa = faixa;
                Texto = texto;
                Nivel = nivel;
            }
        }

        public sealed class RegraDaRolagem
        {
            [JsonProperty("rotulo")] public string Rotulo { get; set; }
            /// <summary>Quando não é Rótulo: o que soma ("condicoes_marcadas").</summary>
            [JsonProperty("base")] public string Base { get; set; }
        }

        sealed class ArquivoJson
        {
            [JsonProperty("movimentos")] public List<Movimento> Movimentos { get; set; } = new List<Movimento>();
        }
    }
}
