using Newtonsoft.Json;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// Um elemento do cenário de uma campanha: lugar, organização ou outra coisa que não é gente (por isso não
    /// é NPC: sem Influência, maturidade nem ficha de vilão). Começa simples, só nome, tipo e descrição; pode
    /// nascer da ficha de uma heroína (ex.: o local destruído na formação da Nova), com o mesmo laço dos NPCs.
    /// </summary>
    public sealed class ElementoDeCenario
    {
        public const string Local = "local";
        public const string Organizacao = "organizacao";
        public const string Outro = "outro";

        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("sala")] public string Sala { get; set; }
        [JsonProperty("nome")] public string Nome { get; set; } = string.Empty;
        /// <summary>local | organizacao | outro.</summary>
        [JsonProperty("tipo")] public string Tipo { get; set; } = Local;
        [JsonProperty("descricao")] public string Descricao { get; set; } = string.Empty;
        [JsonProperty("ativo")] public bool Ativo { get; set; } = true;
        [JsonProperty("ordem")] public int Ordem { get; set; }
        /// <summary>De qual ficha e de qual parte dela nasceu (null: criado pelo mestre).</summary>
        [JsonProperty("vinculo")] public VinculoNpc Vinculo { get; set; }
        /// <summary>O texto da jogadora já foi copiado para a descrição (uma vez, ao nascer).</summary>
        [JsonProperty("descricao_copiada")] public bool DescricaoCopiada { get; set; }

        public static string NomeDoTipo(string tipo) => tipo switch
        {
            Organizacao => "Organização",
            Outro => "Outros",
            _ => "Local",
        };
    }
}
