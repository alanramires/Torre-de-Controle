using System.Collections.Generic;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>Os cinco Rótulos, na ordem da ficha. Ids iguais aos de Regras/*.json.</summary>
    public static class Rotulos
    {
        public const string Perigoso = "perigoso";
        public const string Aberracao = "aberracao";
        public const string Salvador = "salvador";
        public const string Superior = "superior";
        public const string Mundano = "mundano";

        public const int Minimo = -2;
        public const int Maximo = 3;

        public static readonly IReadOnlyList<string> Todos = new[] { Perigoso, Aberracao, Salvador, Superior, Mundano };

        public static string Nome(string id) => id switch
        {
            Perigoso => "Perigoso",
            Aberracao => "Aberração",
            Salvador => "Salvador",
            Superior => "Superior",
            Mundano => "Mundano",
            _ => id,
        };
    }
}
