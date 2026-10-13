using Newtonsoft.Json;
using TorreDeControle.Core;

namespace TorreDeControle.Core.Jogadores
{
    /// <summary>
    /// Uma pessoa cadastrada pelo mestre. Só registro: não é conta, não tem login.
    /// Inativo = sumiu, AFK ou placeholder. Jogador não se exclui, se inativa (o histórico aponta para ele).
    /// </summary>
    public sealed class Jogador : IOrdenavel
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("nome")] public string Nome { get; set; }
        [JsonProperty("ativo")] public bool Ativo { get; set; } = true;
        /// <summary>Posição na lista, dentro do bloco ativo/inativo (o mestre arrasta).</summary>
        [JsonProperty("ordem")] public int Ordem { get; set; }
    }
}
