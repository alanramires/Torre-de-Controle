using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// Exportar e importar, juntos, a Influência e os relacionamentos das heroínas de uma campanha (os laços
    /// entre elas), para revisar fora da Torre e trazer de volta. Importar só mexe nesses campos, e só nas
    /// heroínas que estão na campanha (pelo id); o resto da ficha fica.
    /// </summary>
    public sealed partial class CadastroPersonagens
    {
        public const string FormatoDeTrocaVinculos = "torre-de-controle/vinculos";

        sealed class VinculosDeHeroina
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("nome")] public string Nome { get; set; }
            [JsonProperty("influencia_comportamento")] public string InfluenciaComportamento { get; set; }
            [JsonProperty("influencias")] public Dictionary<string, EstadoInfluencia> Influencias { get; set; }
            [JsonProperty("influencia_porque")] public Dictionary<string, string> InfluenciaPorque { get; set; }
            [JsonProperty("influencia_porque_ninguem")] public string InfluenciaPorqueNinguem { get; set; }
            [JsonProperty("relacoes")] public List<Relacao> Relacoes { get; set; }
        }

        sealed class EnvelopeVinculos
        {
            [JsonProperty("formato")] public string Formato { get; set; }
            [JsonProperty("versao")] public int Versao { get; set; } = 1;
            [JsonProperty("exportado_em")] public string ExportadoEm { get; set; }
            [JsonProperty("origem")] public string Origem { get; set; }
            [JsonProperty("heroinas")] public List<VinculosDeHeroina> Heroinas { get; set; } = new List<VinculosDeHeroina>();
        }

        public string ExportarVinculos(IEnumerable<Personagem> herois, string origem) =>
            JsonConvert.SerializeObject(new EnvelopeVinculos
            {
                Formato = FormatoDeTrocaVinculos,
                ExportadoEm = DateTime.Now.ToString("s"),
                Origem = origem,
                Heroinas = herois.Select(h => new VinculosDeHeroina
                {
                    Id = h.Id, Nome = h.NomeExibicao, InfluenciaComportamento = h.InfluenciaComportamento,
                    Influencias = h.Influencias, InfluenciaPorque = h.InfluenciaPorque,
                    InfluenciaPorqueNinguem = h.InfluenciaPorqueNinguem, Relacoes = h.Relacoes,
                }).ToList(),
            }, Formatting.Indented);

        /// <summary>
        /// Traz Influência e relacionamentos por cima, só nas heroínas de <paramref name="herois"/> (pelo id).
        /// Campo ausente no arquivo fica como está. <paramref name="atualizadas"/> conta quem mudou.
        /// </summary>
        public Resultado ImportarVinculos(string json, IReadOnlyCollection<Personagem> herois, out int atualizadas)
        {
            atualizadas = 0;
            EnvelopeVinculos lido;
            try
            {
                lido = JsonConvert.DeserializeObject<EnvelopeVinculos>(json ?? string.Empty);
            }
            catch (JsonException)
            {
                lido = null;
            }
            if (lido?.Formato != FormatoDeTrocaVinculos || lido.Heroinas == null)
                return Resultado.Falha("Isso não é um JSON de Influência e relacionamentos da Torre.");
            if (lido.Versao > 1)
                return Resultado.Falha("Exportado por uma Torre mais nova: atualize esta antes de importar.");
            foreach (var v in lido.Heroinas.Where(v => v != null))
            {
                var h = herois.FirstOrDefault(x => x.Id == v.Id);
                if (h == null)
                    continue;
                if (v.InfluenciaComportamento != null) h.InfluenciaComportamento = v.InfluenciaComportamento;
                if (v.Influencias != null) h.Influencias = v.Influencias;
                if (v.InfluenciaPorque != null) h.InfluenciaPorque = v.InfluenciaPorque;
                if (v.InfluenciaPorqueNinguem != null) h.InfluenciaPorqueNinguem = v.InfluenciaPorqueNinguem;
                if (v.Relacoes != null) h.Relacoes = v.Relacoes;
                atualizadas++;
            }
            if (atualizadas == 0)
                return Resultado.Falha("Nenhuma heroína do arquivo está nesta campanha.");
            Gravar();
            return Resultado.Sucesso();
        }
    }
}
