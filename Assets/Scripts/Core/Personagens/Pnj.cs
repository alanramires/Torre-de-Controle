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
