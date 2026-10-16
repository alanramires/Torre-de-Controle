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
    /// Uma resposta da formação da equipe: quem enviou, o texto e quando. A posição na lista é a ordem
    /// de chegada (quem envia primeiro dita a narrativa).
    /// </summary>
    public sealed class EnvioDeEquipe
    {
        [JsonProperty("personagem")] public string Personagem { get; set; }
        [JsonProperty("texto")] public string Texto { get; set; } = string.Empty;
        [JsonProperty("enviado_em")] public string EnviadoEm { get; set; }
        /// <summary>O texto antes do último reenvio (para desfazer um engano); null se não houve reenvio.</summary>
        [JsonProperty("anterior")] public string Anterior { get; set; }
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
        /// <summary>A formação da equipe, na ordem em que as respostas chegaram.</summary>
        [JsonProperty("formacao_equipe")] public List<EnvioDeEquipe> FormacaoEquipe { get; set; } = new List<EnvioDeEquipe>();
        /// <summary>Itens do roteiro da primeira sessão que o mestre já marcou (ids de primeira_sessao.json).</summary>
        [JsonProperty("primeira_sessao")] public List<string> PrimeiraSessao { get; set; } = new List<string>();
        /// <summary>Anotações do mestre sobre a formação da equipe (inimigos, consequências, NPCs relevantes). Opcional.</summary>
        [JsonProperty("anotacoes_formacao")] public string AnotacoesFormacao { get; set; } = string.Empty;
        /// <summary>Anotações do mestre sobre os NPCs da campanha (aba NPCs). Opcional.</summary>
        [JsonProperty("anotacoes_npcs")] public string AnotacoesNpcs { get; set; } = string.Empty;
        /// <summary>A trama da campanha (Área do mestre): o que está acontecendo, planos, ganchos. Opcional.</summary>
        [JsonProperty("trama")] public string Trama { get; set; } = string.Empty;
        /// <summary>Quando a mesa desta sala foi aberta pela última vez (ordena a lista de "Entrar na sala").</summary>
        [JsonProperty("ultima_mesa")] public string UltimaMesa { get; set; }
    }
}
