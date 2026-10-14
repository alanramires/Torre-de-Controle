using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TorreDeControle.Core.Regras
{
    /// <summary>Regras de criação lidas de arquetipos.json. Hoje usa os Rótulos iniciais e o +1 livre.</summary>
    public sealed class RegrasArquetipos
    {
        public const string Arquivo = "arquetipos";

        readonly Dictionary<string, RegraArquetipo> porId = new Dictionary<string, RegraArquetipo>();

        public int BonusLivre { get; }
        public int MaximoNaCriacao { get; }

        RegrasArquetipos(ArquivoJson arquivo)
        {
            BonusLivre = arquivo.Comum?.BonusLivre?.Valor ?? 1;
            MaximoNaCriacao = arquivo.Comum?.BonusLivre?.RespeitaMaximo ?? 3;
            foreach (var a in arquivo.Arquetipos ?? new List<RegraArquetipo>())
                porId[a.Id] = a;
        }

        public static RegrasArquetipos Carregar(IFonteDeRegras fonte)
        {
            var texto = fonte.Ler(Arquivo)
                        ?? throw new InvalidOperationException($"Regras/{Arquivo}.json não encontrado.");
            return new RegrasArquetipos(JsonConvert.DeserializeObject<ArquivoJson>(texto));
        }

        public RegraArquetipo Buscar(string id) =>
            id != null && porId.TryGetValue(id, out var regra) ? regra : null;

        public IEnumerable<RegraArquetipo> Todos => porId.Values;

        public sealed class RegraArquetipo
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("nome")] public string Nome { get; set; }
            /// <summary>Nome da arte quadrada do arquétipo (a tela acha a textura por esse nome).</summary>
            [JsonProperty("retrato")] public string Retrato { get; set; }
            [JsonProperty("rotulos")] public Dictionary<string, int> Rotulos { get; set; } = new Dictionary<string, int>();
            [JsonProperty("habilidades")] public RegraHabilidades Habilidades { get; set; } = new RegraHabilidades();
            /// <summary>Grupos de tags de aparência da ficha: só inspiração (a aparência é texto livre).</summary>
            [JsonProperty("visual")] public List<List<string>> Visual { get; set; } = new List<List<string>>();
            /// <summary>Perguntas de histórico da ficha, respondidas na criação.</summary>
            [JsonProperty("historico")] public List<string> Historico { get; set; } = new List<string>();
            /// <summary>"Quando a equipe se reuniu" e relacionamentos: respondidos na sala, com a equipe.</summary>
            [JsonProperty("equipe")] public RegraEquipe Equipe { get; set; } = new RegraEquipe();
            [JsonProperty("influencia")] public RegraInfluencia Influencia { get; set; } = new RegraInfluencia();
            [JsonProperty("avancos")] public ListaDeAvancos Avancos { get; set; } = new ListaDeAvancos();
            /// <summary>Deck do arquétipo: movimentos (escolher N além dos obrigatórios).</summary>
            [JsonProperty("movimentos")] public RegraMovimentos Movimentos { get; set; } = new RegraMovimentos();
            /// <summary>Extras do arquétipo, com os campos que a ficha pede na criação.</summary>
            [JsonProperty("extras")] public List<RegraExtra> Extras { get; set; } = new List<RegraExtra>();
        }

        public sealed class RegraMovimentos
        {
            [JsonProperty("escolher")] public int Escolher { get; set; }
            [JsonProperty("obrigatorios")] public List<string> Obrigatorios { get; set; } = new List<string>();
            [JsonProperty("opcoes")] public List<MovimentoDoArquetipo> Opcoes { get; set; } = new List<MovimentoDoArquetipo>();
            [JsonProperty("nota")] public string Nota { get; set; }
        }

        /// <summary>
        /// Um movimento do deck. Alguns abrem uma subseção na ficha quando escolhidos (<see cref="Campos"/>,
        /// os mesmos tipos de campo dos extras), e trazem a regra em uma frase para a tela e, depois, o motor.
        /// </summary>
        public sealed class MovimentoDoArquetipo
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("nome")] public string Nome { get; set; }
            [JsonProperty("regra")] public string Regra { get; set; }
            [JsonProperty("campos")] public List<CampoExtra> Campos { get; set; } = new List<CampoExtra>();
        }

        public sealed class RegraExtra
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("nome")] public string Nome { get; set; }
            [JsonProperty("nota")] public string Nota { get; set; }
            [JsonProperty("campos")] public List<CampoExtra> Campos { get; set; } = new List<CampoExtra>();
            /// <summary>No jogo, as escolhas deste campo podem ser cumpridas (ex.: os impulsos marcados do Farol).</summary>
            [JsonProperty("cumpre_campo")] public string CumpreCampo { get; set; }
            /// <summary>Benefícios para escolher ao cumprir.</summary>
            [JsonProperty("ao_cumprir")] public List<Opcao> AoCumprir { get; set; } = new List<Opcao>();
        }

        /// <summary>
        /// Um campo de extra. <see cref="Tipo"/>: lista (escolher/escolher_ate das opções), texto, textos
        /// (vagas com mínimo preenchido), rotulo (um dos ids em Rotulos) e trilha (só exibe).
        /// </summary>
        public sealed class CampoExtra
        {
            public const string Lista = "lista";
            public const string Texto = "texto";
            public const string Textos = "textos";
            public const string Rotulo = "rotulo";
            public const string Trilha = "trilha";

            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("tipo")] public string Tipo { get; set; }
            [JsonProperty("rotulo")] public string Titulo { get; set; }
            [JsonProperty("opcoes")] public List<Opcao> Opcoes { get; set; } = new List<Opcao>();
            [JsonProperty("rotulos")] public List<string> Rotulos { get; set; } = new List<string>();
            [JsonProperty("vagas")] public List<Opcao> Vagas { get; set; } = new List<Opcao>();
            [JsonProperty("escolher")] public int? Escolher { get; set; }
            [JsonProperty("escolher_ate")] public int? EscolherAte { get; set; }
            [JsonProperty("minimo")] public int Minimo { get; set; }
            [JsonProperty("tamanho")] public int Tamanho { get; set; }
            /// <summary>Preenchido na sala, com a equipe (ex.: amor e rival da Bruta).</summary>
            [JsonProperty("na_sala")] public bool NaSala { get; set; }
            /// <summary>Lista: cada escolhida ganha um texto opcional (ex.: forças e fraquezas da nave).</summary>
            [JsonProperty("descrever_opcional")] public bool DescreverOpcional { get; set; }
            /// <summary>
            /// Na sala, o valor é o colega do relacionamento deste número (não se digita de novo).
            /// Ex.: Amor e Rival da Bruta = os dois relacionamentos dela.
            /// </summary>
            [JsonProperty("de_relacionamento")] public int? DeRelacionamento { get; set; }

            /// <summary>Texto que não entra nas pendências (ex.: descrição livre da nave).</summary>
            [JsonProperty("opcional")] public bool Opcional { get; set; }
            /// <summary>Texto de várias linhas.</summary>
            [JsonProperty("varias_linhas")] public bool VariasLinhas { get; set; }

            /// <summary>Lista: cada opção escolhida pede um texto (ex.: como o bicho ajuda em cada movimento).</summary>
            [JsonProperty("descrever")] public bool Descrever { get; set; }

            // ---------- campos de jogo (o mestre mexe; a ficha mostra) ----------

            public const string Condicoes = "condicoes";
            /// <summary>Recurso de jogo que sobe e desce (ex.: chamas do Nova).</summary>
            public const string Reserva = "reserva";

            /// <summary>Lançamentos feitos no jogo (opção + texto), ex.: usos do controle da Forasteira.</summary>
            public const string Registro = "registro";
            /// <summary>Objetos criados no jogo, cada um com cargas (ex.: dispositivos alienígenas).</summary>
            public const string Itens = "itens";

            /// <summary>Registro: cada lançamento gasta 1 desta reserva (id de campo do mesmo extra).</summary>
            [JsonProperty("consome")] public string Consome { get; set; }
            /// <summary>Condições com um pedido em texto (ex.: o que se quer do santuário).</summary>
            [JsonProperty("pedido")] public bool Pedido { get; set; }

            /// <summary>Reserva que volta a zero no fim da cena.</summary>
            [JsonProperty("zera_fim_de_cena")] public bool ZeraFimDeCena { get; set; }

            /// <summary>Trilha: a cada 5 pontos, uma escolha a mais no campo "extra|campo" indicado.</summary>
            [JsonProperty("rende")] public string Rende { get; set; }
            /// <summary>Lista que ganha escolhas durante o jogo, além das da criação (ex.: sinais da perdição).</summary>
            [JsonProperty("cresce_no_jogo")] public bool CresceNoJogo { get; set; }
            /// <summary>Opção final: só libera com todas as outras tomadas, ou pelo avanço indicado.</summary>
            [JsonProperty("final")] public string Final { get; set; }
            [JsonProperty("final_libera_com_avanco")] public string FinalLiberaComAvanco { get; set; }
            /// <summary>
            /// Campo preenchido no jogo, não na criação (ex.: uso do santuário; o equipamento do Farol, que
            /// começa vazio). Não é pendência da criação; no jogo, quem escreve é o painel do mestre.
            /// </summary>
            [JsonProperty("no_jogo")] public bool NoJogo { get; set; }
            [JsonProperty("nota")] public string Nota { get; set; }

            [JsonIgnore] public int MaximoEscolhas => Escolher ?? EscolherAte ?? 0;
        }

        /// <summary>Lista de avanços da ficha: 'cima' e 'baixo' (tardios, liberam com 5 de cima).</summary>
        public sealed class ListaDeAvancos
        {
            public const int ParaLiberarBaixo = 5;
            [JsonProperty("cima")] public List<ItemAvanco> Cima { get; set; } = new List<ItemAvanco>();
            [JsonProperty("baixo")] public List<ItemAvanco> Baixo { get; set; } = new List<ItemAvanco>();
        }

        public sealed class ItemAvanco
        {
            /// <summary>A operação (ver avancos.json). Itens repetidos têm o mesmo id e são casas diferentes.</summary>
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("texto")] public string Texto { get; set; }
        }

        public sealed class RegraEquipe
        {
            [JsonProperty("pergunta")] public string Pergunta { get; set; }
            /// <summary>Duas frases; "___" marca o nome do colega.</summary>
            [JsonProperty("relacionamentos")] public List<string> Relacionamentos { get; set; } = new List<string>();
        }

        public sealed class RegraInfluencia
        {
            public const string Fixo = "fixo";
            public const string Todos = "todos";
            public const string Comportamento = "comportamento";
            public const string AmorERival = "amor_e_rival";

            [JsonProperty("regra")] public string Regra { get; set; }
            /// <summary>A regra de Influência inicial em uma frase, para a tela.</summary>
            [JsonProperty("texto")] public string Texto { get; set; }
            /// <summary>Regra "fixo": a quantos colegas.</summary>
            [JsonProperty("quantidade")] public int? QuantidadeNumero { get; set; }
            /// <summary>Regra "comportamento": cada comportamento diz a quantos (um número ou "todos").</summary>
            [JsonProperty("opcoes")] public List<OpcaoInfluencia> Opcoes { get; set; } = new List<OpcaoInfluencia>();
        }

        public sealed class OpcaoInfluencia
        {
            [JsonProperty("id")] public string Id { get; set; }
            /// <summary>Número de colegas, ou "todos".</summary>
            [JsonProperty("quantidade")] public object Quantidade { get; set; }

            [JsonIgnore] public bool ParaTodos => Quantidade is string s && s == "todos";
            [JsonIgnore] public int? QuantidadeNumero => ParaTodos ? (int?)null : System.Convert.ToInt32(Quantidade);
            /// <summary>"fachada_feliz" → "Fachada feliz".</summary>
            [JsonIgnore] public string Nome =>
                string.IsNullOrEmpty(Id) ? Id : char.ToUpperInvariant(Id[0]) + Id.Substring(1).Replace('_', ' ');
        }

        /// <summary>Uma opção nomeada (habilidade, linha, vaga).</summary>
        public sealed class Opcao
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("nome")] public string Nome { get; set; }
            /// <summary>Regra em uma frase, quando a opção tem efeito (ex.: papéis do Coração da Bruta).</summary>
            [JsonProperty("regra")] public string Regra { get; set; }
            /// <summary>O texto tem uma lacuna ("___") que se preenche (ex.: condições do santuário).</summary>
            [JsonProperty("lacuna")] public bool Lacuna { get; set; }
        }

        public sealed class LinhaDePoderes
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("nome")] public string Nome { get; set; }
            [JsonProperty("poderes")] public List<Opcao> Poderes { get; set; } = new List<Opcao>();
        }

        /// <summary>
        /// Modelo da subseção Habilidades. <see cref="Tipo"/> diz como a tela desenha e como validar:
        /// lista (escolher N das opções, com fixas opcionais), fixas, linha (escolhe a linha e nega N)
        /// e mentora (uma opção por vaga, todas diferentes).
        /// </summary>
        public sealed class RegraHabilidades
        {
            public const string Lista = "lista";
            public const string Fixas = "fixas";
            public const string Linha = "linha";
            public const string Mentora = "mentora";
            public const string DescreverCada = "cada";
            public const string DescreverEscolhidas = "escolhidas";

            [JsonProperty("tipo")] public string Tipo { get; set; } = Lista;
            [JsonProperty("escolher")] public int? Escolher { get; set; }
            [JsonProperty("escolher_ate")] public int? EscolherAte { get; set; }
            [JsonProperty("opcoes")] public List<Opcao> Opcoes { get; set; } = new List<Opcao>();
            [JsonProperty("fixas")] public List<Opcao> FixasDadas { get; set; } = new List<Opcao>();
            [JsonProperty("linhas")] public List<LinhaDePoderes> Linhas { get; set; } = new List<LinhaDePoderes>();
            [JsonProperty("negar")] public int Negar { get; set; }
            [JsonProperty("vagas")] public List<Opcao> Vagas { get; set; } = new List<Opcao>();
            [JsonProperty("descrever")] public string Descrever { get; set; }
            [JsonProperty("nota")] public string Nota { get; set; }

            /// <summary>Máximo de escolhas da lista (escolher ou escolher_ate).</summary>
            [JsonIgnore] public int MaximoEscolhas => Escolher ?? EscolherAte ?? 0;
        }

        sealed class ArquivoJson
        {
            [JsonProperty("criacao_comum")] public Comum Comum { get; set; }
            [JsonProperty("arquetipos")] public List<RegraArquetipo> Arquetipos { get; set; }
        }

        sealed class Comum
        {
            [JsonProperty("bonus_livre")] public Bonus BonusLivre { get; set; }
        }

        sealed class Bonus
        {
            [JsonProperty("valor")] public int Valor { get; set; }
            [JsonProperty("respeita_maximo")] public int RespeitaMaximo { get; set; }
        }
    }
}
