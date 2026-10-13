using System.Collections.Generic;
using Newtonsoft.Json;

namespace TorreDeControle.Core.Salas
{
    /// <summary>
    /// Uma mudança de um valor da ficha: "potencial", "condicao:irritado" (0/1) ou "rotulo:salvador".
    /// Guarda o antes e o depois: dá para ler o log sem recalcular nada, e corrigir aplicando o inverso.
    /// </summary>
    public sealed class Mudanca
    {
        [JsonProperty("campo")] public string Campo { get; set; }
        [JsonProperty("antes")] public int Antes { get; set; }
        [JsonProperty("depois")] public int Depois { get; set; }
    }

    /// <summary>
    /// O que aconteceu na mesa: um comando do mestre aplicado a uma personagem, na rodada, com motivo.
    /// O log nunca se apaga: corrigir é outro evento (com <see cref="Corrige"/> apontando o original).
    /// </summary>
    public sealed class Evento
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("rodada")] public int Rodada { get; set; }
        [JsonProperty("quando")] public string Quando { get; set; }
        [JsonProperty("personagem")] public string Personagem { get; set; }
        /// <summary>O comando em texto, para o log e a colheita ("+1 potencial", "marcou Irritado"...).</summary>
        [JsonProperty("descricao")] public string Descricao { get; set; }
        [JsonProperty("mudancas")] public List<Mudanca> Mudancas { get; set; } = new List<Mudanca>();
        [JsonProperty("motivo")] public string Motivo { get; set; } = string.Empty;
        /// <summary>Id do evento que este corrige (desfaz), ou null.</summary>
        [JsonProperty("corrige")] public string Corrige { get; set; }
    }
}
