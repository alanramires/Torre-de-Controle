using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using TorreDeControle.Core;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>Quais regras de personagem valem: PJ ou PNJ. Não é quem está clicando.</summary>
    [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
    public enum Controle { Jogador, Mestre }

    [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
    public enum Maturidade { Jovem, Adulto }

    [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
    public enum Papel { Heroi, Vilao, Neutro }

    /// <summary>Ciclo de criação da ficha de herói: novo → criado (foto congelada) → em carreira (entrou numa sala).</summary>
    [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
    public enum StatusCriacao { Novo, Criado, EmCarreira }

    /// <summary>Seção Identidade da ficha.</summary>
    public sealed class Identidade
    {
        [JsonProperty("nome")] public string Nome { get; set; } = string.Empty;
        [JsonProperty("codinome")] public string Codinome { get; set; } = string.Empty;
        /// <summary>Id do jogador, ou null. Associação opcional: no laboratório ninguém é obrigado a ter dono.</summary>
        [JsonProperty("jogador")] public string Jogador { get; set; }
        [JsonProperty("arquetipo")] public string Arquetipo { get; set; }
        /// <summary>Texto livre. As tags de visual da ficha são só inspiração.</summary>
        [JsonProperty("aparencia")] public string Aparencia { get; set; } = string.Empty;
        /// <summary>Breve resumo da personagem e das origens: texto livre, sem regra.</summary>
        [JsonProperty("resumo")] public string Resumo { get; set; } = string.Empty;
        /// <summary>
        /// Respostas às perguntas de histórico do arquétipo, na ordem da ficha. Dependem das perguntas,
        /// então trocar de arquétipo em "novo" as apaga.
        /// </summary>
        [JsonProperty("respostas_historico")] public List<string> RespostasHistorico { get; set; } = new List<string>();
    }

    /// <summary>
    /// Escolhas da ficha de criação enquanto a personagem é "novo". Trocar de arquétipo zera tudo isto.
    /// </summary>
    public sealed class RascunhoCriacao
    {
        /// <summary>Rótulo que recebe o +1 livre, ou null se ainda não escolhido.</summary>
        [JsonProperty("bonus_livre")] public string BonusLivre { get; set; }
        [JsonProperty("habilidades")] public EscolhasHabilidades Habilidades { get; set; } = new EscolhasHabilidades();

        /// <summary>Módulos: movimentos escolhidos do deck do arquétipo (sem os obrigatórios, que vêm dados).</summary>
        [JsonProperty("movimentos")] public List<string> Movimentos { get; set; } = new List<string>();
        /// <summary>Módulos: escolhas dos extras (listas e rótulos), por "extra|campo".</summary>
        [JsonProperty("extras_escolhas")] public Dictionary<string, List<string>> ExtrasEscolhas { get; set; } = new Dictionary<string, List<string>>();
        /// <summary>Módulos: textos dos extras, por "extra|campo" ou "extra|campo|vaga".</summary>
        [JsonProperty("extras_textos")] public Dictionary<string, string> ExtrasTextos { get; set; } = new Dictionary<string, string>();

        [JsonIgnore]
        public bool TemEscolhas =>
            BonusLivre != null || (Habilidades?.TemAlgo ?? false) || Movimentos.Count > 0 ||
            ExtrasEscolhas.Count > 0 || ExtrasTextos.Count > 0;
    }

    /// <summary>
    /// O que foi escolhido na subseção Habilidades. Cada tipo de regra usa uma parte:
    /// lista → Escolhidas; linha → Linha + Negadas; mentora → Vagas. Descrições valem para todos.
    /// </summary>
    public sealed class EscolhasHabilidades
    {
        [JsonProperty("escolhidas")] public List<string> Escolhidas { get; set; } = new List<string>();
        [JsonProperty("linha")] public string Linha { get; set; }
        [JsonProperty("negadas")] public List<string> Negadas { get; set; } = new List<string>();
        [JsonProperty("vagas")] public Dictionary<string, string> Vagas { get; set; } = new Dictionary<string, string>();
        /// <summary>"Como se manifesta", por id de habilidade.</summary>
        [JsonProperty("descricoes")] public Dictionary<string, string> Descricoes { get; set; } = new Dictionary<string, string>();

        [JsonIgnore]
        public bool TemAlgo =>
            Escolhidas.Count > 0 || Linha != null || Negadas.Count > 0 || Vagas.Count > 0 || Descricoes.Count > 0;
    }

    /// <summary>
    /// Um lançamento (registro: opção escolhida + texto) ou objeto (itens: nome em Texto + cargas) do jogo.
    /// </summary>
    public sealed class ItemDeJogo
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("opcao")] public string Opcao { get; set; }
        [JsonProperty("texto")] public string Texto { get; set; } = string.Empty;
        [JsonProperty("cargas")] public int Cargas { get; set; }
    }

    /// <summary>Uma habilidade como fica na ficha: nome e como se manifesta.</summary>
    public sealed class Habilidade
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("nome")] public string Nome { get; set; }
        [JsonProperty("descricao")] public string Descricao { get; set; } = string.Empty;
        /// <summary>Veio dada pelo arquétipo (não foi escolha).</summary>
        [JsonProperty("fixa")] public bool Fixa { get; set; }
    }

    /// <summary>
    /// Foto da criação: o estado aprovado, congelado. Daqui em diante, tudo muda por eventos por cima dela.
    /// </summary>
    public sealed class FotoCriacao
    {
        [JsonProperty("aprovada_em")] public string AprovadaEm { get; set; }
        [JsonProperty("arquetipo")] public string Arquetipo { get; set; }
        [JsonProperty("rotulos")] public Dictionary<string, int> Rotulos { get; set; } = new Dictionary<string, int>();
        [JsonProperty("habilidades")] public List<Habilidade> Habilidades { get; set; } = new List<Habilidade>();
        /// <summary>Movimentos do deck ao concluir (obrigatórios + escolhidos).</summary>
        [JsonProperty("movimentos")] public List<string> Movimentos { get; set; } = new List<string>();
        [JsonProperty("extras_escolhas")] public Dictionary<string, List<string>> ExtrasEscolhas { get; set; } = new Dictionary<string, List<string>>();
        [JsonProperty("extras_textos")] public Dictionary<string, string> ExtrasTextos { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Uma personagem: identidade persistente + três dimensões independentes (controle, maturidade, papel).
    /// Núcleo, Crescimento e Módulos entram nas próximas partes da ficha.
    /// Não se exclui: se inativa.
    /// </summary>
    public sealed class Personagem : IOrdenavel
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("identidade")] public Identidade Identidade { get; set; } = new Identidade();
        [JsonProperty("controle")] public Controle Controle { get; set; } = Controle.Jogador;
        [JsonProperty("maturidade")] public Maturidade Maturidade { get; set; } = Maturidade.Jovem;
        [JsonProperty("papel")] public Papel Papel { get; set; } = Papel.Heroi;
        [JsonProperty("status")] public StatusCriacao Status { get; set; } = StatusCriacao.Novo;
        [JsonProperty("ativo")] public bool Ativo { get; set; } = true;
        /// <summary>Posição na lista, dentro do bloco ativo/inativo (o mestre arrasta).</summary>
        [JsonProperty("ordem")] public int Ordem { get; set; }
        [JsonProperty("criacao")] public RascunhoCriacao Criacao { get; set; } = new RascunhoCriacao();
        /// <summary>Null até a aprovação.</summary>
        [JsonProperty("foto")] public FotoCriacao Foto { get; set; }
        /// <summary>
        /// Seção Crescimento (protótipo): casas de avanço marcadas, como "cima:3" ou "baixo:0"
        /// (posição na lista do arquétipo; itens repetidos são casas diferentes). Ainda sem efeito
        /// nas outras seções e sem evento: é só a marca.
        /// </summary>
        [JsonProperty("avancos_marcados")] public List<string> AvancosMarcados { get; set; } = new List<string>();
        /// <summary>
        /// Observações do jogador sobre os movimentos, por id do movimento. Anotação, não regra:
        /// editável sempre, fora da foto da criação.
        /// </summary>
        [JsonProperty("obs_movimentos")] public Dictionary<string, string> ObsMovimentos { get; set; } = new Dictionary<string, string>();
        /// <summary>
        /// Núcleo: potencial em pontos corridos (não zera). A cada múltiplo de 5 nasce um avanço;
        /// pendentes = potencial / 5 − avanços marcados. Quem dá potencial é o mestre (painel, no jogo).
        /// </summary>
        [JsonProperty("potencial")] public int Potencial { get; set; }

        // ---------- estado de jogo dos extras (o mestre mexe; a ficha mostra) ----------

        /// <summary>Contadores corridos, por "extra|campo" (ex.: trilha da perdição).</summary>
        [JsonProperty("contadores")] public Dictionary<string, int> Contadores { get; set; } = new Dictionary<string, int>();
        /// <summary>Escolhas feitas no jogo, por "extra|campo" (ex.: sinais além do inicial, condições do santuário).</summary>
        [JsonProperty("escolhas_de_jogo")] public Dictionary<string, List<string>> EscolhasDeJogo { get; set; } = new Dictionary<string, List<string>>();
        /// <summary>Textos do jogo, por "extra|campo|sub" (ex.: o pedido ao santuário, a lacuna de uma condição).</summary>
        [JsonProperty("textos_de_jogo")] public Dictionary<string, string> TextosDeJogo { get; set; } = new Dictionary<string, string>();
        /// <summary>Lançamentos e objetos do jogo, por "extra|campo" (ex.: usos do controle, dispositivos).</summary>
        [JsonProperty("itens_de_jogo")] public Dictionary<string, List<ItemDeJogo>> ItensDeJogo { get; set; } = new Dictionary<string, List<ItemDeJogo>>();
        /// <summary>
        /// No jogo (protótipo): escolhas de extra cumpridas, por "extra|opcao", com o benefício escolhido
        /// (vazio enquanto não escolheu). Ex.: impulsos do Farol. Só a marca, sem efeito ainda.
        /// </summary>
        [JsonProperty("cumpridos")] public Dictionary<string, string> Cumpridos { get; set; } = new Dictionary<string, string>();
        /// <summary>De onde foi copiada (laboratório → sala); null no laboratório.</summary>
        [JsonProperty("origem")] public string Origem { get; set; }

        /// <summary>Codinome se houver, senão o nome civil.</summary>
        [JsonIgnore]
        public string NomeExibicao =>
            string.IsNullOrWhiteSpace(Identidade.Codinome) ? Identidade.Nome : Identidade.Codinome;
    }
}
