using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TorreDeControle.Core.Regras
{
    /// <summary>
    /// O bloco "condicoes" do nucleo.json: a penalidade, os movimentos que cada condição atrapalha e o que tira a
    /// condição no fim da cena (livro p.89–90). Serve a dica da tela e, depois, o modificador completo da rolagem.
    /// </summary>
    public sealed class RegrasDeCondicoes
    {
        public const string Arquivo = "nucleo";

        /// <summary>A penalidade de cada condição marcada nos movimentos ligados a ela (−2).</summary>
        public int Penalidade { get; }

        readonly Dictionary<string, List<string>> afeta;
        readonly Dictionary<string, string> remover;

        RegrasDeCondicoes(int penalidade, Dictionary<string, List<string>> afeta, Dictionary<string, string> remover)
        {
            Penalidade = penalidade;
            this.afeta = afeta;
            this.remover = remover;
        }

        public static RegrasDeCondicoes Carregar(IFonteDeRegras fonte)
        {
            var texto = fonte.Ler(Arquivo) ?? throw new InvalidOperationException($"Regras/{Arquivo}.json não encontrado.");
            var bloco = JObject.Parse(texto)["condicoes"] as JObject ?? new JObject();
            return new RegrasDeCondicoes(
                (int?)bloco["penalidade"] ?? -2,
                bloco["afeta"]?.ToObject<Dictionary<string, List<string>>>() ?? new Dictionary<string, List<string>>(),
                bloco["remover_no_fim_da_cena"]?.ToObject<Dictionary<string, string>>() ?? new Dictionary<string, string>());
        }

        /// <summary>Os ids dos movimentos que a condição atrapalha ("basico/enfrentar"); vazio se nenhum.</summary>
        public IReadOnlyList<string> MovimentosAfetados(string condicao) =>
            condicao != null && afeta.TryGetValue(condicao, out var lista) ? lista : (IReadOnlyList<string>)new List<string>();

        /// <summary>O que a personagem faz na cena para a condição sair no fim dela, ou null.</summary>
        public string ComoRemover(string condicao) =>
            condicao != null && remover.TryGetValue(condicao, out var texto) ? texto : null;
    }
}
