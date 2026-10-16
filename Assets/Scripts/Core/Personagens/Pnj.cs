using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>As cinco condições (ids do nucleo.json), com o nome que a tela mostra.</summary>
    public static class Condicoes
    {
        public const string Irritado = "irritado";
        public const string Assustado = "assustado";
        public const string Culpado = "culpado";
        public const string Desesperancoso = "desesperancoso";
        public const string Inseguro = "inseguro";

        public static readonly IReadOnlyList<string> Todas = new[] { Irritado, Assustado, Culpado, Desesperancoso, Inseguro };

        public static string Nome(string id) => id switch
        {
            Irritado => "Irritado",
            Assustado => "Assustado",
            Culpado => "Culpado",
            Desesperancoso => "Desesperançoso",
            Inseguro => "Inseguro",
            _ => id,
        };
    }

    /// <summary>Geração do vilão (livro p.194): o tom do nome e da história.</summary>
    public static class Geracoes
    {
        public sealed class Geracao
        {
            public string Id { get; }
            public string Nome { get; }
            /// <summary>Que tipo de nome combina (texto nosso).</summary>
            public string Dica { get; }

            public Geracao(string id, string nome, string dica)
            {
                Id = id;
                Nome = nome;
                Dica = dica;
            }
        }

        public static readonly IReadOnlyList<Geracao> Todas = new[]
        {
            new Geracao("ouro", "Ouro", "nome bobo, leve, divertido"),
            new Geracao("prata", "Prata", "nome chamativo, cósmico, épico"),
            new Geracao("bronze", "Bronze", "nome realista, simples, que gruda"),
            new Geracao("moderna", "Moderna", "nome viral, piada, trocadilho esperto"),
        };

        public static Geracao Buscar(string id) => Todas.FirstOrDefault(g => g.Id == id);
    }

    /// <summary>
    /// Perfil PNJ (controle do mestre), pelo livro p.193: aparência marcante (na Identidade),
    /// uma palavra para o modo de agir e uma motivação (verbo no infinitivo + substantivo).
    /// </summary>
    public sealed class PerfilPnj
    {
        [JsonProperty("modo_de_agir")] public string ModoDeAgir { get; set; } = string.Empty;
        [JsonProperty("motivacao")] public string Motivacao { get; set; } = string.Empty;
        /// <summary>NPC criado pela ficha de uma heroína (ex.: o nêmesis da Amaldiçoada); null se o mestre criou.</summary>
        [JsonProperty("vinculo")] public VinculoNpc Vinculo { get; set; }
        /// <summary>Nasceu da ficha sem saber se é adulto ou jovem: o mestre decide (aviso até lá).</summary>
        [JsonProperty("maturidade_a_definir")] public bool MaturidadeADefinir { get; set; }
        /// <summary>
        /// O texto da jogadora já foi copiado para as Notas (uma vez, ao nascer): daí em diante a ficha dela é
        /// história narrada e as Notas são do mestre; uma não mexe na outra.
        /// </summary>
        [JsonProperty("descricao_copiada")] public bool DescricaoCopiada { get; set; }
        /// <summary>Heroínas da campanha a que o mestre ligou este NPC ("Ligado a"), cada uma com o como.</summary>
        [JsonProperty("ligacoes")] public List<LigacaoNpc> Ligacoes { get; set; } = new List<LigacaoNpc>();
    }

    /// <summary>Uma ligação feita pelo mestre: a heroína (instância na sala) e, em uma linha, como.</summary>
    public sealed class LigacaoNpc
    {
        [JsonProperty("personagem")] public string Personagem { get; set; }
        [JsonProperty("como")] public string Como { get; set; } = string.Empty;
    }

    /// <summary>De quem o NPC é (a instância na sala) e de qual extra ele nasceu.</summary>
    public sealed class VinculoNpc
    {
        [JsonProperty("personagem")] public string Personagem { get; set; }
        [JsonProperty("extra")] public string Extra { get; set; }
        /// <summary>Qual dos NPCs do extra (campo e vaga), quando o extra dá mais de um.</summary>
        [JsonProperty("campo")] public string Campo { get; set; }
        [JsonProperty("vaga")] public string Vaga { get; set; }
    }

    /// <summary>
    /// Ficha de vilão (papel vilão), pelo livro p.194–195: geração, movimentos de vilão e de 1 a 5
    /// condições. Não some se o papel mudar: fica guardada e volta junto.
    /// </summary>
    public sealed class FichaVilao
    {
        [JsonProperty("geracao")] public string Geracao { get; set; }
        /// <summary>Movimentos de vilão: frases ativas, escritas pelo mestre.</summary>
        [JsonProperty("movimentos")] public List<string> Movimentos { get; set; } = new List<string>();
        /// <summary>Condições que o vilão tem (ids). Quantas mais, mais tempo ele aguenta a luta.</summary>
        [JsonProperty("condicoes")] public List<string> Condicoes { get; set; } = new List<string>();

        /// <summary>O peso do vilão pela quantidade de condições (texto nosso, livro p.195).</summary>
        public static string Perigo(int condicoes) => condicoes switch
        {
            0 => "sem condições: escolha de 1 a 5",
            1 => "1 condição: cai com um golpe",
            2 => "2 condições: dá algum trabalho",
            3 => "3 condições: ameaçador",
            4 => "4 condições: perigoso",
            _ => "5 condições: arquivilão",
        };
    }
}
