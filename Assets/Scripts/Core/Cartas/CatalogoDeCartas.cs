using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TorreDeControle.Core.Regras;

namespace TorreDeControle.Core.Cartas
{
    /// <summary>
    /// Uma carta: o que ela É. Sem texto de regra (ele mora em Regras/*.json e no help): só a identidade, de
    /// onde vem a regra e qual carta de help a explica. Onde ela ESTÁ (mão, mesa, descarte) é da partida.
    /// </summary>
    public sealed class Carta
    {
        public const string Basico = "basico";
        public const string Pvp = "pvp";
        public const string Arquetipo = "arquetipo";
        public const string Equipe = "equipe";
        public const string Extra = "extra";
        public const string Adulto = "adulto";
        public const string Referencia = "referencia";
        public const string Mestre = "mj";
        public const string Personalizado = "personalizado";

        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("tipo")] public string Tipo { get; set; }
        [JsonProperty("titulo")] public string Titulo { get; set; }
        /// <summary>"todos", o id de um arquétipo, ou "mestre".</summary>
        [JsonProperty("origem")] public string Origem { get; set; }
        /// <summary>De onde vem a regra: "arquivo.json#id" (rastreio, como no help).</summary>
        [JsonProperty("regra")] public string Regra { get; set; }
        /// <summary>Id da carta de help que explica esta (Ajuda/catalogo.json), se houver.</summary>
        [JsonProperty("ajuda")] public string Ajuda { get; set; }
        /// <summary>Rótulo da rolagem, quando o movimento rola um Rótulo fixo.</summary>
        [JsonProperty("rotulo")] public string Rotulo { get; set; }
        /// <summary>Extras: o módulo (cartas + tokens) a que a carta pertence.</summary>
        [JsonProperty("modulo")] public string Modulo { get; set; }
        /// <summary>A carta que fica no verso (ex.: a variante PvP de um básico). Em aberto no CLAUDE.md.</summary>
        [JsonProperty("verso")] public string Verso { get; set; }
        /// <summary>modificador | substituto | novo (como mexe nas outras cartas). Em aberto no CLAUDE.md.</summary>
        [JsonProperty("modo")] public string Modo { get; set; }
        [JsonProperty("modifica")] public List<string> Modifica { get; set; } = new List<string>();
        /// <summary>
        /// O que a carta pode render, em tokens (rodapé do desenho): equipe, potencial, influencia, adiante,
        /// constante, controle, condicao. Vazio: o desenho mostra só o "potencial na falha" de quem rola.
        /// </summary>
        [JsonProperty("rodape")] public List<string> Rodape { get; set; } = new List<string>();
    }

    /// <summary>
    /// O catálogo de cartas (Regras/cartas.json): compartilhado entre todas as partidas. Lido pela mesma
    /// fonte das regras (no jogo, Resources; nos testes, o arquivo).
    /// </summary>
    public sealed class CatalogoDeCartas
    {
        sealed class Arquivo
        {
            [JsonProperty("versao")] public int Versao { get; set; }
            [JsonProperty("cartas")] public List<Carta> Cartas { get; set; } = new List<Carta>();
        }

        readonly List<Carta> cartas;
        readonly Dictionary<string, Carta> porId;

        CatalogoDeCartas(List<Carta> cartas)
        {
            this.cartas = cartas;
            porId = cartas.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First());
        }

        public static CatalogoDeCartas Carregar(IFonteDeRegras fonte)
        {
            var texto = fonte.Ler("cartas");
            var arquivo = string.IsNullOrWhiteSpace(texto) ? new Arquivo() : JsonConvert.DeserializeObject<Arquivo>(texto) ?? new Arquivo();
            return new CatalogoDeCartas(arquivo.Cartas ?? new List<Carta>());
        }

        public IReadOnlyList<Carta> Todas => cartas;

        public Carta Buscar(string id) => id != null && porId.TryGetValue(id, out var c) ? c : null;

        public IReadOnlyList<Carta> DoTipo(string tipo) => cartas.Where(c => c.Tipo == tipo).ToList();

        /// <summary>Cartas de um tipo vindas de um arquétipo (ex.: os movimentos da Bruta).</summary>
        public IReadOnlyList<Carta> DoArquetipo(string tipo, string arquetipo) =>
            cartas.Where(c => c.Tipo == tipo && c.Origem == arquetipo).ToList();

        /// <summary>Ids repetidos no arquivo (erro de quem popula): o primeiro vale, os outros são ignorados.</summary>
        public IReadOnlyList<string> Repetidos() =>
            cartas.GroupBy(c => c.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
    }
}
