using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// Exportar e importar o cenário de uma campanha em JSON (por enquanto cada campanha é uma cidade): leva a
    /// cidade para outra campanha, outra máquina ou outra Torre. Vem o que o elemento é (nome, tipo,
    /// descrição, ativo); fica o laço com a heroína de lá.
    /// </summary>
    public sealed partial class CadastroPersonagens
    {
        public const string FormatoDeTrocaCenario = "torre-de-controle/cenario";
        const int VersaoDeTrocaCenario = 1;

        sealed class EnvelopeCenario
        {
            [JsonProperty("formato")] public string Formato { get; set; }
            [JsonProperty("versao")] public int Versao { get; set; }
            [JsonProperty("exportado_em")] public string ExportadoEm { get; set; }
            /// <summary>De onde saiu, por extenso (ex.: "Campanha Halcyon City").</summary>
            [JsonProperty("origem")] public string Origem { get; set; }
            [JsonProperty("cenario")] public List<ElementoDeCenario> Cenario { get; set; } = new List<ElementoDeCenario>();
        }

        /// <summary>O JSON de troca do cenário da sala (ativos e inativos, na ordem); null se não houver nenhum.</summary>
        public string ExportarCenario(string sala, string origem)
        {
            var elementos = CenarioDaSala(sala);
            if (elementos.Count == 0)
                return null;
            return JsonConvert.SerializeObject(new EnvelopeCenario
            {
                Formato = FormatoDeTrocaCenario,
                Versao = VersaoDeTrocaCenario,
                ExportadoEm = DateTime.Now.ToString("s"),
                Origem = origem,
                Cenario = elementos.Select(e => new ElementoDeCenario
                {
                    Nome = e.Nome, Tipo = e.Tipo, Descricao = e.Descricao, Ativo = e.Ativo,
                }).ToList(),
            }, Formatting.Indented);
        }

        /// <summary>
        /// Traz o cenário de um JSON de troca para a sala, como elementos novos (id novo, sem laço com heroína).
        /// O que já existe na sala com o mesmo nome fica como está e não é duplicado (importar duas vezes o
        /// mesmo arquivo não dobra a cidade); <paramref name="repetidos"/> conta esses.
        /// </summary>
        public Resultado ImportarCenario(string json, string sala, out int novos, out int repetidos)
        {
            novos = repetidos = 0;
            if (string.IsNullOrWhiteSpace(sala))
                return Resultado.Falha("O cenário é de uma campanha.");
            EnvelopeCenario envelope;
            try
            {
                envelope = JsonConvert.DeserializeObject<EnvelopeCenario>(json ?? string.Empty);
            }
            catch (JsonException)
            {
                envelope = null;
            }
            if (envelope?.Formato != FormatoDeTrocaCenario || envelope.Cenario == null)
                return Resultado.Falha("Isso não é um JSON de cenário da Torre.");
            if (envelope.Versao > VersaoDeTrocaCenario)
                return Resultado.Falha("Exportado por uma Torre mais nova: atualize esta antes de importar.");

            foreach (var lido in envelope.Cenario.Where(e => e != null && !string.IsNullOrWhiteSpace(e.Nome)))
            {
                var nome = lido.Nome.Trim();
                if (cenario.Any(e => e.Sala == sala && string.Equals(e.Nome, nome, StringComparison.OrdinalIgnoreCase)))
                {
                    repetidos++;
                    continue;
                }
                cenario.Add(new ElementoDeCenario
                {
                    Id = Guid.NewGuid().ToString("N"), Sala = sala, Nome = nome, Tipo = TipoValido(lido.Tipo),
                    Descricao = (lido.Descricao ?? string.Empty).Trim(), Ativo = lido.Ativo,
                    Ordem = cenario.Count(e => e.Sala == sala),
                });
                novos++;
            }
            if (novos > 0)
                Gravar();
            return Resultado.Sucesso();
        }
    }
}
