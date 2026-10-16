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
        /// <summary>Id do desfazer (correção) que este evento refaz, ou null.</summary>
        [JsonProperty("refaz")] public string Refaz { get; set; }
    }

    /// <summary>
    /// Um bônus dado pelo mestre na mesa: adiante, constante ou controle, com valor e o contexto em que vale
    /// ("agindo pelas respostas da avaliação"). Usar não apaga: desativa (o log aponta para ele).
    /// </summary>
    public sealed class BonusDeMesa
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("personagem")] public string Personagem { get; set; }
        [JsonProperty("tipo")] public string Tipo { get; set; }
        [JsonProperty("valor")] public int Valor { get; set; }
        [JsonProperty("contexto")] public string Contexto { get; set; } = string.Empty;
        [JsonProperty("ativo")] public bool Ativo { get; set; }
    }
}
