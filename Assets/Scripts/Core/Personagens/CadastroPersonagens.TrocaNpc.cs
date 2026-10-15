using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// Exportar e importar NPCs em JSON: o laboratório de NPCs ou os NPCs de uma campanha, para outra
    /// máquina ou outra Torre. Formato próprio (uma lista), separado do da personagem. Importar cria cópias
    /// novas no destino escolhido, com as mesmas regras do "copiar para…": vem o que o NPC é, não o que
    /// aconteceu com ele (Influências, vínculo com heroínas de lá).
    /// </summary>
    public sealed partial class CadastroPersonagens
    {
        public const string FormatoDeTrocaNpcs = "torre-de-controle/npcs";

        sealed class SoFormato
        {
            [JsonProperty("formato")] public string Formato { get; set; }
        }

        /// <summary>
        /// O formato de um JSON de troca (<see cref="FormatoDeTroca"/> ou <see cref="FormatoDeTrocaNpcs"/>),
        /// ou null se não for da Torre. Serve para cada tela listar só os arquivos que ela importa.
        /// </summary>
        public static string FormatoDaTroca(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<SoFormato>(json ?? string.Empty)?.Formato;
            }
            catch (JsonException)
            {
                return null;
            }
        }
        const int VersaoDeTrocaNpcs = 1;

        sealed class EnvelopeNpcs
        {
            [JsonProperty("formato")] public string Formato { get; set; }
            [JsonProperty("versao")] public int Versao { get; set; }
            [JsonProperty("exportado_em")] public string ExportadoEm { get; set; }
            /// <summary>De onde saíram, por extenso (ex.: "Campanha Halcyon City").</summary>
            [JsonProperty("origem")] public string Origem { get; set; }
            [JsonProperty("npcs")] public List<Personagem> Npcs { get; set; } = new List<Personagem>();
        }

        /// <summary>
        /// O JSON de troca dos NPCs de <paramref name="sala"/> (null = laboratório de NPCs), ativos e
        /// inativos, na ordem da lista. Null se não houver nenhum.
        /// </summary>
        public string ExportarNpcs(string sala, string origem)
        {
            var npcs = NpcsDaSala(sala);
            return npcs.Count == 0 ? null : Envelopar(npcs, origem);
        }

        /// <summary>Um NPC só, no mesmo formato (lista de um): entra também pelo importar da tela de NPCs.</summary>
        public string ExportarNpc(string id)
        {
            var npc = Buscar(id);
            return npc == null || npc.Controle != Controle.Mestre ? null : Envelopar(new[] { npc }, npc.NomeExibicao);
        }

        static string Envelopar(IEnumerable<Personagem> npcs, string origem) =>
            JsonConvert.SerializeObject(new EnvelopeNpcs
            {
                Formato = FormatoDeTrocaNpcs,
                Versao = VersaoDeTrocaNpcs,
                ExportadoEm = DateTime.Now.ToString("s"),
                Origem = origem,
                Npcs = npcs.ToList(),
            }, Formatting.Indented);

        /// <summary>
        /// O caminho de volta de um NPC (ajustado em outra máquina): o arquivo, com um NPC só, grava por cima
        /// deste. Vem o NPC em si (nomes, foto, perfil, papel, maturidade, ficha de vilão, notas); fica o que é
        /// desta campanha: id, sala, origem, vínculo com a heroína, Influências, ativo, favorito e ordem.
        /// </summary>
        public Resultado ReimportarNpc(string id, string json)
        {
            if (!AcharNpc(id, out var npc, out var erro))
                return erro;
            var lido = LerNpcs(json, out var envelope);
            if (!lido.Ok)
                return lido;
            if (envelope.Npcs.Count != 1)
                return Resultado.Falha($"O arquivo traz {envelope.Npcs.Count} NPCs; por cima de um, só arquivo de um NPC.");

            var (sala, origem, ativo, favorito, ordem, vinculo) = (npc.Sala, npc.Origem, npc.Ativo, npc.Favorito, npc.Ordem, npc.Pnj.Vinculo);
            var (influencias, porque, ninguem) = (npc.Influencias, npc.InfluenciaPorque, npc.InfluenciaPorqueNinguem);
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(envelope.Npcs[0]), npc, Restaurar);
            Normalizar(npc);
            npc.Id = id;
            (npc.Sala, npc.Origem, npc.Ativo, npc.Favorito, npc.Ordem) = (sala, origem, ativo, favorito, ordem);
            npc.Pnj ??= new PerfilPnj();
            npc.Pnj.Vinculo = vinculo;
            (npc.Influencias, npc.InfluenciaPorque, npc.InfluenciaPorqueNinguem) = (influencias, porque, ninguem);
            Gravar();
            return Resultado.Sucesso();
        }

        Resultado LerNpcs(string json, out EnvelopeNpcs envelope)
        {
            envelope = null;
            try
            {
                envelope = JsonConvert.DeserializeObject<EnvelopeNpcs>(json ?? string.Empty);
            }
            catch (JsonException)
            {
                return Resultado.Falha("Isso não é um JSON de NPCs da Torre.");
            }
            if (envelope?.Formato != FormatoDeTrocaNpcs || envelope.Npcs == null)
                return Resultado.Falha("Isso não é um JSON de NPCs da Torre.");
            if (envelope.Versao > VersaoDeTrocaNpcs)
                return Resultado.Falha("Exportado por uma Torre mais nova: atualize esta antes de importar.");
            if (envelope.Npcs.Any(n => n == null || n.Controle != Controle.Mestre))
                return Resultado.Falha("O arquivo traz algo que não é NPC.");
            return Resultado.Sucesso();
        }

        /// <summary>
        /// Traz os NPCs de um JSON de troca para <paramref name="destino"/> (null = laboratório), como
        /// NPCs novos: id novo, sem Influências, sem vínculo com heroína nem origem (são de outra Torre).
        /// Ativo e inativo vêm como estavam.
        /// </summary>
        public Resultado ImportarNpcs(string json, string destino, out IReadOnlyList<Personagem> novos)
        {
            novos = Array.Empty<Personagem>();
            var lido = LerNpcs(json, out var envelope);
            if (!lido.Ok)
                return lido;

            var lista = new List<Personagem>();
            foreach (var npc in envelope.Npcs)
            {
                Normalizar(npc);
                npc.Id = Guid.NewGuid().ToString("N");
                npc.Origem = null;
                npc.Sala = destino;
                npc.Favorito = false;
                npc.Ordem = Ordenacao.Fim(personagens.Where(n => n.Sala == destino && n.Controle == Controle.Mestre));
                npc.Pnj.Vinculo = null;
                npc.Influencias = new Dictionary<string, EstadoInfluencia>();
                npc.InfluenciaPorque = new Dictionary<string, string>();
                npc.InfluenciaPorqueNinguem = string.Empty;
                personagens.Add(npc);
                lista.Add(npc);
            }
            Gravar();
            novos = lista;
            return Resultado.Sucesso();
        }
    }
}
