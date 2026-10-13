using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>Quais regras de personagem valem: PJ ou PNJ. Não é quem está clicando.</summary>
    [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
    public enum Controle { Jogador, Mestre }

    [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
    public enum Maturidade { Jovem, Adulto }

    [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
    public enum Papel { Heroi, Vilao, Neutro }

    /// <summary>Ciclo de criação da ficha de herói: novo → criado (foto congelada) → em carreira (entrou numa sala).</summary>
    [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
    public enum StatusCriacao { Novo, Criado, EmCarreira }

    /// <summary>Seção Identidade da ficha.</summary>
    public sealed class Identidade
    {
        [JsonProperty("nome")] public string Nome { get; set; } = string.Empty;
        [JsonProperty("codinome")] public string Codinome { get; set; } = string.Empty;
        /// <summary>Id do jogador, ou null. Associação opcional: no laboratório ninguém é obrigado a ter dono.</summary>
        [JsonProperty("jogador")] public string Jogador { get; set; }
        [JsonProperty("arquetipo")] public string Arquetipo { get; set; }
    }

    /// <summary>
    /// Uma personagem: identidade persistente + três dimensões independentes (controle, maturidade, papel).
    /// Núcleo, Crescimento e Módulos entram nas próximas partes da ficha.
    /// Não se exclui: se inativa.
    /// </summary>
    public sealed class Personagem
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("identidade")] public Identidade Identidade { get; set; } = new Identidade();
        [JsonProperty("controle")] public Controle Controle { get; set; } = Controle.Jogador;
        [JsonProperty("maturidade")] public Maturidade Maturidade { get; set; } = Maturidade.Jovem;
        [JsonProperty("papel")] public Papel Papel { get; set; } = Papel.Heroi;
        [JsonProperty("status")] public StatusCriacao Status { get; set; } = StatusCriacao.Novo;
        [JsonProperty("ativo")] public bool Ativo { get; set; } = true;
        /// <summary>De onde foi copiada (laboratório → sala); null no laboratório.</summary>
        [JsonProperty("origem")] public string Origem { get; set; }

        /// <summary>Codinome se houver, senão o nome civil.</summary>
        [JsonIgnore]
        public string NomeExibicao =>
            string.IsNullOrWhiteSpace(Identidade.Codinome) ? Identidade.Nome : Identidade.Codinome;
    }
}
