using System.Collections.Generic;
using System.Linq;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// Os dez arquétipos do livro base: só identidade (id e nome). Os ids batem com o catálogo de
    /// cartas do acervo (equipe/farol, ...). Rótulos iniciais e regras de criação virão de
    /// Assets/Resources/Regras/arquetipos.json (parte 2 da ficha).
    /// </summary>
    public static class Arquetipos
    {
        public sealed class Arquetipo
        {
            public string Id { get; }
            public string Nome { get; }

            public Arquetipo(string id, string nome)
            {
                Id = id;
                Nome = nome;
            }
        }

        public static readonly IReadOnlyList<Arquetipo> Todos = new[]
        {
            new Arquetipo("amaldicoada", "Amaldiçoada"),
            new Arquetipo("bruta", "Bruta"),
            new Arquetipo("delinquente", "Delinquente"),
            new Arquetipo("discipula", "Discípula"),
            new Arquetipo("farol", "Farol"),
            new Arquetipo("forasteira", "Forasteira"),
            new Arquetipo("janus", "Janus"),
            new Arquetipo("legado", "Legado"),
            new Arquetipo("nova", "Nova"),
            new Arquetipo("transformado", "Transformado"),
        };

        public static Arquetipo Buscar(string id) => Todos.FirstOrDefault(a => a.Id == id);

        public static string Nome(string id) => Buscar(id)?.Nome ?? "—";
    }
}
