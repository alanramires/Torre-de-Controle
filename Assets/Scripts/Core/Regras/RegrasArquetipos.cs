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
            /// <summary>Perguntas de histórico que dão NPC (ex.: Nova: quem feriu, quem ajuda a controlar).</summary>
            [JsonProperty("npcs_historico")] public List<NpcDoExtra> NpcsHistorico { get; set; } = new List<NpcDoExtra>();
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
            /// <summary>NPCs que a subseção do movimento dá (ex.: o bicho do "Vamos, Sortudo!", a Nave Kirby).</summary>
            [JsonProperty("npcs")] public List<NpcDoExtra> Npcs { get; set; } = new List<NpcDoExtra>();
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
            /// <summary>Os NPCs que o extra dá na sala (ex.: o nêmesis da Amaldiçoada, os membros do Legado).</summary>
            [JsonProperty("npcs")] public List<NpcDoExtra> Npcs { get; set; } = new List<NpcDoExtra>();
            /// <summary>O cenário que o extra dá sozinho, sem o jogador escrever nome (ex.: o santuário da Amaldiçoada).</summary>
            [JsonProperty("cenario")] public List<CenarioDoExtra> Cenario { get; set; } = new List<CenarioDoExtra>();
        }

        /// <summary>
        /// Um elemento de cenário que nasce do extra com nome automático (o livro não pede nome): o mestre
        /// pode renomear depois. Com <see cref="Opcao"/>, só existe se essa opção de <see cref="Campo"/> estiver
        /// marcada (ex.: a base secreta entre os recursos da Discípula), e a descrição é o texto dela; sem, a
        /// descrição junta as escolhas dos campos em <see cref="DescricaoDe"/> (ex.: características do santuário).
        /// </summary>
        public sealed class CenarioDoExtra
        {
            /// <summary>Nome ao nascer; {personagem} vira o nome da heroína.</summary>
            [JsonProperty("nome")] public string Nome { get; set; }
            /// <summary>
            /// Se houver, a ficha ganha uma caixa de nome no título do extra (ex.: "nome do santuário (opcional)"):
            /// escrito, o nome vem da ficha; vazio, vale o nome automático.
            /// </summary>
            [JsonProperty("dica_nome")] public string DicaNome { get; set; }
            /// <summary>local, organizacao ou outro.</summary>
            [JsonProperty("tipo")] public string Tipo { get; set; }
            [JsonProperty("vinculo")] public string Vinculo { get; set; }
            [JsonProperty("campo")] public string Campo { get; set; }
            [JsonProperty("opcao")] public string Opcao { get; set; }
            [JsonProperty("descricao_de")] public List<string> DescricaoDe { get; set; } = new List<string>();
        }

        /// <summary>
        /// Um NPC que nasce do extra: com a heroína numa sala, preencher o nome cria (ou reativa) o NPC lá;
        /// apagar o nome inativa. O jogador escreve nome e descrição; o resto da ficha é do mestre.
        /// O nome é o texto de <see cref="Campo"/> (na <see cref="Vaga"/>, se for de vagas); a descrição é
        /// o <see cref="CampoDescricao"/> ou, nas vagas com descrição, a descrição da vaga.
        /// </summary>
        /// <summary>Papel, maturidade e dica de uma opção que dá pessoa (ver NpcDoExtra.Opcoes).</summary>
        public sealed class NpcDaOpcao
        {
            [JsonProperty("papel")] public string Papel { get; set; }
            [JsonProperty("maturidade")] public string Maturidade { get; set; }
            /// <summary>O que a caixa diz quando vazia (ex.: "vazio: qualquer herói adulto").</summary>
            [JsonProperty("dica")] public string Dica { get; set; }
        }

        public sealed class NpcDoExtra
        {
            [JsonProperty("campo")] public string Campo { get; set; }
            [JsonProperty("vaga")] public string Vaga { get; set; }
            /// <summary>
            /// Lista: cada opção escolhida ganha uma caixa opcional de pessoa (ex.: obrigações do Janus:
            /// o chefe do estágio, a professora que fica no pé); preenchida, vira NPC. A descrição é a da opção
            /// e o laço é o nome da opção.
            /// </summary>
            [JsonProperty("por_escolha")] public bool PorEscolha { get; set; }
            /// <summary>
            /// Por escolha: se houver, só estas opções ganham a caixa de pessoa, cada uma com papel, maturidade e
            /// dica próprios (ex.: impulsos do Farol: "beijar alguém perigoso" → vilão). Vazio: todas, com os da regra.
            /// </summary>
            [JsonProperty("opcoes")] public Dictionary<string, NpcDaOpcao> Opcoes { get; set; } = new Dictionary<string, NpcDaOpcao>();

            /// <summary>Esta opção escolhida ganha caixa de pessoa?</summary>
            public bool DaPessoa(string opcao) => PorEscolha && (Opcoes == null || Opcoes.Count == 0 || Opcoes.ContainsKey(opcao));

            public string PapelDa(string opcao) => Opcoes != null && Opcoes.TryGetValue(opcao, out var o) && o.Papel != null ? o.Papel : Papel;
            public string MaturidadeDa(string opcao) => Opcoes != null && Opcoes.TryGetValue(opcao, out var o) && o.Maturidade != null ? o.Maturidade : Maturidade;
            public string DicaDa(string opcao) => Opcoes != null && Opcoes.TryGetValue(opcao, out var o) ? o.Dica : null;
            [JsonProperty("campo_descricao")] public string CampoDescricao { get; set; }
            /// <summary>neutro, aliado ou vilao.</summary>
            [JsonProperty("papel")] public string Papel { get; set; }
            /// <summary>
            /// Em vez de NPC, um elemento do cenário deste tipo (local, organizacao, outro): lugar não é gente
            /// (sem Influência nem maturidade). Ex.: o local destruído na formação da Nova.
            /// </summary>
            [JsonProperty("cenario")] public string Cenario { get; set; }
            /// <summary>
            /// Só é gente se esta opção estiver marcada (ex.: a Nave Kirby é NPC só se for telepática); senão
            /// vira o cenário de <see cref="SenaoCenario"/>. Formato "campo/opcao".
            /// </summary>
            [JsonProperty("npc_se")] public string NpcSe { get; set; }
            [JsonProperty("senao_cenario")] public string SenaoCenario { get; set; }

            /// <summary>A mesma regra, como cenário do tipo dado (mesmo campo e vaga: o laço continua o mesmo).</summary>
            public NpcDoExtra ComoCenario(string tipo)
            {
                var copia = (NpcDoExtra)MemberwiseClone();
                copia.Cenario = tipo;
                return copia;
            }
            /// <summary>Em npcs_historico: o número da pergunta (de 0), cuja resposta é a descrição.</summary>
            [JsonProperty("pergunta")] public int? Pergunta { get; set; }
            /// <summary>jovem, adulto (padrão) ou mestre (nasce adulto, com aviso até o mestre decidir).</summary>
            [JsonProperty("maturidade")] public string Maturidade { get; set; }
            /// <summary>Como a sala chama o laço (ex.: "nêmesis" → "Estrela Negra · nêmesis").</summary>
            [JsonProperty("vinculo")] public string Vinculo { get; set; }
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

            /// <summary>Textos: cada vaga ganha, abaixo do nome, uma descrição opcional (ex.: membros do Legado).</summary>
            [JsonProperty("com_descricao")] public bool ComDescricao { get; set; }

            /// <summary>A "vaga" onde mora a descrição da vaga (mesma chave de texto, sufixo próprio).</summary>
            public static string DescricaoDaVaga(string vaga) => vaga + "#descricao";

            const string SufixoPessoa = "#pessoa";

            /// <summary>A "vaga" onde mora a pessoa ligada a uma opção escolhida (ver NpcDoExtra.PorEscolha).</summary>
            public static string PessoaDaOpcao(string opcao) => opcao + SufixoPessoa;

            /// <summary>A opção de uma chave de pessoa; null se não for chave de pessoa.</summary>
            public static string OpcaoDaPessoa(string vaga) =>
                vaga != null && vaga.EndsWith(SufixoPessoa) ? vaga.Substring(0, vaga.Length - SufixoPessoa.Length) : null;

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
            /// <summary>
            /// A resposta da formação dá um NPC (ex.: o inimigo que a Bruta ajudou a derrotar, o herói que o Legado
            /// impressionou): nome opcional à parte, a resposta é a descrição.
            /// </summary>
            [JsonProperty("npc")] public NpcDoExtra Npc { get; set; }
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
