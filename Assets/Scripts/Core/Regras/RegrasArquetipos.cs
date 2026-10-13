using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TorreDeControle.Core.Regras
{
    /// <summary>Regras de criação lidas de arquetipos.json. Hoje usa os Rótulos iniciais e o +1 livre.</summary>
    public sealed class RegrasArquetipos
    {
        public const string Arquivo = "arquetipos";

        readonly Dictionary<string, RegraArquetipo> porId = new Dictionary<string, RegraArquetipo>();

        public int BonusLivre { get; }
        public int MaximoNaCriacao { get; }

        RegrasArquetipos(ArquivoJson arquivo)
        {
            BonusLivre = arquivo.Comum?.BonusLivre?.Valor ?? 1;
            MaximoNaCriacao = arquivo.Comum?.BonusLivre?.RespeitaMaximo ?? 3;
            foreach (var a in arquivo.Arquetipos ?? new List<RegraArquetipo>())
                porId[a.Id] = a;
        }

        public static RegrasArquetipos Carregar(IFonteDeRegras fonte)
        {
            var texto = fonte.Ler(Arquivo)
                        ?? throw new InvalidOperationException($"Regras/{Arquivo}.json não encontrado.");
            return new RegrasArquetipos(JsonConvert.DeserializeObject<ArquivoJson>(texto));
        }

        public RegraArquetipo Buscar(string id) =>
            id != null && porId.TryGetValue(id, out var regra) ? regra : null;

        public IEnumerable<RegraArquetipo> Todos => porId.Values;

        public sealed class RegraArquetipo
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("nome")] public string Nome { get; set; }
            [JsonProperty("rotulos")] public Dictionary<string, int> Rotulos { get; set; } = new Dictionary<string, int>();
            [JsonProperty("habilidades")] public RegraHabilidades Habilidades { get; set; } = new RegraHabilidades();
            /// <summary>Grupos de tags de aparência da ficha: só inspiração (a aparência é texto livre).</summary>
            [JsonProperty("visual")] public List<List<string>> Visual { get; set; } = new List<List<string>>();
            /// <summary>Perguntas de histórico da ficha: guia (o histórico é texto livre).</summary>
            [JsonProperty("historico")] public List<string> Historico { get; set; } = new List<string>();
        }

        /// <summary>Uma opção nomeada (habilidade, linha, vaga).</summary>
        public sealed class Opcao
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("nome")] public string Nome { get; set; }
        }

        public sealed class LinhaDePoderes
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("nome")] public string Nome { get; set; }
            [JsonProperty("poderes")] public List<Opcao> Poderes { get; set; } = new List<Opcao>();
        }

        /// <summary>
        /// Modelo da subseção Habilidades. <see cref="Tipo"/> diz como a tela desenha e como validar:
        /// lista (escolher N das opções, com fixas opcionais), fixas, linha (escolhe a linha e nega N)
        /// e mentora (uma opção por vaga, todas diferentes).
        /// </summary>
        public sealed class RegraHabilidades
        {
            public const string Lista = "lista";
            public const string Fixas = "fixas";
            public const string Linha = "linha";
            public const string Mentora = "mentora";
            public const string DescreverCada = "cada";
            public const string DescreverEscolhidas = "escolhidas";

            [JsonProperty("tipo")] public string Tipo { get; set; } = Lista;
            [JsonProperty("escolher")] public int? Escolher { get; set; }
            [JsonProperty("escolher_ate")] public int? EscolherAte { get; set; }
            [JsonProperty("opcoes")] public List<Opcao> Opcoes { get; set; } = new List<Opcao>();
            [JsonProperty("fixas")] public List<Opcao> FixasDadas { get; set; } = new List<Opcao>();
            [JsonProperty("linhas")] public List<LinhaDePoderes> Linhas { get; set; } = new List<LinhaDePoderes>();
            [JsonProperty("negar")] public int Negar { get; set; }
            [JsonProperty("vagas")] public List<Opcao> Vagas { get; set; } = new List<Opcao>();
            [JsonProperty("descrever")] public string Descrever { get; set; }
            [JsonProperty("nota")] public string Nota { get; set; }

            /// <summary>Máximo de escolhas da lista (escolher ou escolher_ate).</summary>
            [JsonIgnore] public int MaximoEscolhas => Escolher ?? EscolherAte ?? 0;
        }

        sealed class ArquivoJson
        {
            [JsonProperty("criacao_comum")] public Comum Comum { get; set; }
            [JsonProperty("arquetipos")] public List<RegraArquetipo> Arquetipos { get; set; }
        }

        sealed class Comum
        {
            [JsonProperty("bonus_livre")] public Bonus BonusLivre { get; set; }
        }

        sealed class Bonus
        {
            [JsonProperty("valor")] public int Valor { get; set; }
            [JsonProperty("respeita_maximo")] public int RespeitaMaximo { get; set; }
        }
    }
}
