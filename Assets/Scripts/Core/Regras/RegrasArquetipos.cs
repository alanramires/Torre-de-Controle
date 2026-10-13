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
