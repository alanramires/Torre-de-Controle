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

    /// <summary>
    /// Ciclo da ficha de herói (decidido com o Alan, 2026-10-13):
    /// laboratório: novo ⇄ em aprovação;
    /// sala: em aprovação → em carreira → aposentado; em aprovação e em carreira → em revisão (o mestre,
    /// pela aba Mestre: erro que passou despercebido) → em aprovação. Aposentado não volta.
    /// Em revisão é a instância reaberta para corrigir a criação: fica na vaga e guarda o jogo.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
    public enum StatusCriacao { Novo, EmAprovacao, EmCarreira, Aposentado, EmRevisao }

    /// <summary>
    /// O mestre olhou a foto (link)? Avalia na instância da sala, vendo a imagem. Trocar o link volta
    /// para "aguardando": é outra imagem.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
    public enum AprovacaoFoto { Aguardando, Aprovada, Recusada }

    /// <summary>
    /// Estado de uma Influência que a personagem deu: dada, ou perdida no jogo (amizades se perdem e podem
    /// voltar). Uma vez dada, não sai da ficha.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
    public enum EstadoInfluencia { Dada, Perdida }

    /// <summary>
    /// Um relacionamento da ficha (o "___" de uma frase do arquétipo): o colega de agora, quem ocupou a
    /// frase antes (no jogo, trocado ou perdido: nada sai da ficha) e o porquê (opcional, roleplay).
    /// </summary>
    public sealed class Relacao
    {
        /// <summary>Id do colega de agora; null = vazio (não escolhido, ou perdido no jogo).</summary>
        [JsonProperty("colega")] public string Colega { get; set; }
        [JsonProperty("anteriores")] public List<string> Anteriores { get; set; } = new List<string>();
        [JsonProperty("porque")] public string Porque { get; set; } = string.Empty;
    }

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
        /// <summary>Link (página web) da foto de herói; vazio se não houver. Não é regra: editável sempre.</summary>
        [JsonProperty("foto_heroica")] public string FotoHeroica { get; set; } = string.Empty;
        /// <summary>Link (página web) da foto civil; vazio se não houver.</summary>
        [JsonProperty("foto_civil")] public string FotoCivil { get; set; } = string.Empty;
        [JsonProperty("foto_heroica_aprovacao")] public AprovacaoFoto FotoHeroicaAprovacao { get; set; }
        [JsonProperty("foto_civil_aprovacao")] public AprovacaoFoto FotoCivilAprovacao { get; set; }
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
        [JsonProperty("favorito")] public bool Favorito { get; set; }
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
        /// <summary>Condições marcadas no jogo (ids, na ordem da ficha). Quem marca é a mesa.</summary>
        [JsonProperty("condicoes")] public List<string> CondicoesMarcadas { get; set; } = new List<string>();
        /// <summary>
        /// Rótulos depois das mudanças de jogo; null enquanto nada mudou (valem os da foto da criação).
        /// </summary>
        [JsonProperty("rotulos_jogo")] public Dictionary<string, int> RotulosJogo { get; set; }

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
        /// <summary>Sala onde a cópia joga; null = laboratório.</summary>
        [JsonProperty("sala")] public string Sala { get; set; }
        // ---------- segunda etapa, na campanha (instância em aprovação) ----------

        /// <summary>Resposta a "quando a equipe se reuniu pela primeira vez" (pergunta do arquétipo).</summary>
        [JsonProperty("equipe_resposta")] public string EquipeResposta { get; set; } = string.Empty;
        /// <summary>O colega (id) de cada relacionamento da ficha, na ordem das frases; null = não escolhido.</summary>
        /// <summary>Os relacionamentos, na ordem das frases do arquétipo.</summary>
        [JsonProperty("relacoes")] public List<Relacao> Relacoes { get; set; } = new List<Relacao>();
        /// <summary>Formato anterior (uma lista de ids, na ordem das frases): lido só para converter.</summary>
        [JsonProperty("relacionamentos")] public List<string> RelacionamentosAntigos { get; set; }
        public bool ShouldSerializeRelacionamentosAntigos() => false;
        /// <summary>Comportamento escolhido, quando a regra de Influência é por comportamento.</summary>
        [JsonProperty("influencia_comportamento")] public string InfluenciaComportamento { get; set; }
        /// <summary>Colegas (ids) a quem deu Influência inicial, quando a regra deixa escolher.</summary>
        /// <summary>
        /// A Influência que a personagem deu, por colega (id): dada ou perdida. A leitura reversa (quem tem
        /// Influência sobre quem) lê este estado nas fichas. Na criação, desmarcar tira o colega (nunca valeu);
        /// no jogo, perder vira "perdida" e fica na ficha.
        /// </summary>
        [JsonProperty("influencias")] public Dictionary<string, EstadoInfluencia> Influencias { get; set; } = new Dictionary<string, EstadoInfluencia>();
        /// <summary>Formato de 2026-10-14 (uma lista de ids): lido só para converter, não é mais gravado.</summary>
        [JsonProperty("influencia_para")] public List<string> InfluenciaParaAntiga { get; set; }
        public bool ShouldSerializeInfluenciaParaAntiga() => false;
        /// <summary>
        /// Por que cada colega (id) tem Influência sobre a personagem: texto livre, opcional, para roleplay.
        /// Anotação, não regra: editável sempre.
        /// </summary>
        [JsonProperty("influencia_porque")] public Dictionary<string, string> InfluenciaPorque { get; set; } = new Dictionary<string, string>();
        /// <summary>Por que ninguém tem Influência sobre ela (quando não deu a ninguém): opcional, roleplay.</summary>
        [JsonProperty("influencia_porque_ninguem")] public string InfluenciaPorqueNinguem { get; set; } = string.Empty;

        /// <summary>Perfil PNJ (controle do mestre); null em herói.</summary>
        [JsonProperty("pnj")] public PerfilPnj Pnj { get; set; }
        /// <summary>Ficha de vilão; nasce ao virar vilão e fica guardada se o papel mudar.</summary>
        [JsonProperty("vilao")] public FichaVilao Vilao { get; set; }

        /// <summary>A criação está aberta (novo no laboratório, ou em revisão na sala).</summary>
        [JsonIgnore]
        public bool EmCriacao => Status == StatusCriacao.Novo || Status == StatusCriacao.EmRevisao;

        /// <summary>Codinome se houver, senão o nome civil.</summary>
        [JsonIgnore]
        public string NomeExibicao =>
            string.IsNullOrWhiteSpace(Identidade.Codinome) ? Identidade.Nome : Identidade.Codinome;
    }
}
