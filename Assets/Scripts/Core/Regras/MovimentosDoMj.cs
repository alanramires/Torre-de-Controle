using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace TorreDeControle.Core.Regras
{
    /// <summary>
    /// Os movimentos do MJ de Regras/movimentos_mj.json (o deck do MJ na mesa). O peso não é do movimento: o MJ escolhe
    /// ao jogar (p.177); a tendência só existe onde o livro aponta uma.
    /// </summary>
    public sealed class MovimentosDoMj
    {
        public const string Arquivo = "movimentos_mj";
        public const string Leve = "leve", Pesado = "pesado";
        public const string EfeitoMarcarCondicao = "marcar_condicao", EfeitoMovimentoDeVilao = "movimento_de_vilao";

        public IReadOnlyList<Movimento> Todos { get; }

        MovimentosDoMj(IReadOnlyList<Movimento> todos) => Todos = todos;

        public static MovimentosDoMj Carregar(IFonteDeRegras fonte)
        {
            var texto = fonte.Ler(Arquivo) ?? throw new InvalidOperationException($"Regras/{Arquivo}.json não encontrado.");
            var arquivo = JsonConvert.DeserializeObject<ArquivoJson>(texto) ?? new ArquivoJson();
            return new MovimentosDoMj(arquivo.Movimentos);
        }

        public Movimento Buscar(string id) => Todos.FirstOrDefault(m => m.Id == id);

        public sealed class Movimento
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("nome")] public string Nome { get; set; }
            /// <summary>Id da carta no help (Ajuda/catalogo.json).</summary>
            [JsonProperty("ajuda")] public string Ajuda { get; set; }
            /// <summary>"leve", "pesado" ou null: o que o livro aponta; a tela pré-marca, o MJ decide.</summary>
            [JsonProperty("tendencia")] public string Tendencia { get; set; }
            /// <summary>O que a mesa faz junto ("marcar_condicao", "movimento_de_vilao"), ou null (narrativo).</summary>
            [JsonProperty("efeito")] public string Efeito { get; set; }
            [JsonProperty("nota")] public string Nota { get; set; }

            [JsonIgnore] public bool MarcaCondicao => Efeito == EfeitoMarcarCondicao;
        }

        sealed class ArquivoJson
        {
            [JsonProperty("movimentos")] public List<Movimento> Movimentos { get; set; } = new List<Movimento>();
        }
    }
}
