using System.Collections.Generic;
using Newtonsoft.Json;
using TorreDeControle.Core;

namespace TorreDeControle.Core.Salas
{
    /// <summary>
    /// Lugar à mesa: um jogador + uma personagem da sala (a cópia, nunca a do laboratório).
    /// Os dois são independentes: o slot pode ter só o jogador, só a personagem, ou nada.
    /// </summary>
    public sealed class Slot
    {
        /// <summary>Id do jogador, ou null.</summary>
        [JsonProperty("jogador")] public string Jogador { get; set; }
        /// <summary>Id da personagem da sala (a cópia), ou null.</summary>
        [JsonProperty("personagem")] public string Personagem { get; set; }
    }

    /// <summary>
    /// Uma campanha. Número de slots definido pelo mestre; na sala, um slot por jogador.
    /// Não se exclui: se inativa (campanha encerrada).
    /// </summary>
    public sealed class Sala : IOrdenavel
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("nome")] public string Nome { get; set; }
        [JsonProperty("ativo")] public bool Ativo { get; set; } = true;
        /// <summary>Posição na lista, dentro do bloco ativo/inativo (o mestre arrasta).</summary>
        [JsonProperty("ordem")] public int Ordem { get; set; }
        [JsonProperty("slots")] public List<Slot> Slots { get; set; } = new List<Slot>();
    }
}
