using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace TorreDeControle.Core.Salas
{
    /// <summary>
    /// Uma mudança de um valor da ficha: "potencial", "condicao:irritado" (0/1) ou "rotulo:salvador".
    /// Guarda o antes e o depois: dá para ler o log sem recalcular nada, e corrigir aplicando o inverso.
    /// </summary>
    public sealed class Mudanca
    {
        [JsonProperty("campo")] public string Campo { get; set; }
        [JsonProperty("antes")] public int Antes { get; set; }
        [JsonProperty("depois")] public int Depois { get; set; }
    }

    /// <summary>
    /// O que aconteceu na mesa: um comando do mestre aplicado a uma personagem, na rodada, com motivo.
    /// O log nunca se apaga: corrigir é outro evento (com <see cref="Corrige"/> apontando o original).
    /// </summary>
    public sealed class Evento
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("rodada")] public int Rodada { get; set; }
        [JsonProperty("quando")] public string Quando { get; set; }
        /// <summary>Id da cena em que aconteceu (o log da mesa mostra a cena atual), ou null: antes de haver cena.</summary>
        [JsonProperty("cena", NullValueHandling = NullValueHandling.Ignore)] public string Cena { get; set; }
        [JsonProperty("personagem")] public string Personagem { get; set; }
        /// <summary>O comando em texto, para o log e a colheita ("+1 potencial", "marcou Irritado"...).</summary>
        [JsonProperty("descricao")] public string Descricao { get; set; }
        [JsonProperty("mudancas")] public List<Mudanca> Mudancas { get; set; } = new List<Mudanca>();
        [JsonProperty("motivo")] public string Motivo { get; set; } = string.Empty;
        /// <summary>Id do evento que este corrige (desfaz), ou null.</summary>
        [JsonProperty("corrige")] public string Corrige { get; set; }
        /// <summary>Id do desfazer (correção) que este evento refaz, ou null.</summary>
        [JsonProperty("refaz")] public string Refaz { get; set; }
        /// <summary>Apagado pelo mestre: some do log (e do post), mas fica no arquivo (nada se exclui).</summary>
        [JsonProperty("oculto")] public bool Oculto { get; set; }
        /// <summary>O texto (descrição ou motivo) foi editado pelo mestre depois de lançado.</summary>
        [JsonProperty("editado")] public bool Editado { get; set; }
        /// <summary>Se o evento é uma rolagem de 2d6: os dados, o modificador e as intervenções. Senão, null.</summary>
        [JsonProperty("rolagem", NullValueHandling = NullValueHandling.Ignore)] public RolagemDaMesa Rolagem { get; set; }
        /// <summary>
        /// Id da rolagem cuja pendência este evento resolve (o movimento de condição do vilão que a troca marcou), ou null.
        /// </summary>
        [JsonProperty("resolve", NullValueHandling = NullValueHandling.Ignore)] public string Resolve { get; set; }
        /// <summary>Se o evento é um movimento do MJ (o deck): qual e com que peso. Senão, null.</summary>
        [JsonProperty("movimento_do_mj", NullValueHandling = NullValueHandling.Ignore)] public MovimentoDoMjFeito MovimentoDoMj { get; set; }
        /// <summary>Se o evento é um pedido de rolagem para depois (/eda, /tgp no console): o que rolar. Senão, null.</summary>
        [JsonProperty("pedido", NullValueHandling = NullValueHandling.Ignore)] public PedidoDeRolagem Pedido { get; set; }
    }

    /// <summary>Uma cena da campanha: nome, o local do Cenário (opcional) e quando começou. Quem está nela é da mesa.</summary>
    public sealed class Cena
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("nome")] public string Nome { get; set; }
        /// <summary>Id do Local no Cenário da campanha, ou null.</summary>
        [JsonProperty("local", NullValueHandling = NullValueHandling.Ignore)] public string Local { get; set; }
        [JsonProperty("rodada")] public int Rodada { get; set; }
        [JsonProperty("quando")] public string Quando { get; set; }
        /// <summary>
        /// Quem está nesta cena (heroínas e NPCs). A da cena aberta acompanha a mesa; as outras ficam como estavam
        /// quando o mestre saiu delas (ir para outra cena não tira ninguém de lugar nenhum).
        /// </summary>
        [JsonProperty("herois")] public List<string> Herois { get; set; } = new List<string>();
        [JsonProperty("npcs")] public List<string> Npcs { get; set; } = new List<string>();
    }

    /// <summary>Um movimento do MJ feito na mesa: o id (movimentos_mj.json), o nome de quando foi feito e o peso escolhido.</summary>
    public sealed class MovimentoDoMjFeito
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("nome")] public string Nome { get; set; }
        /// <summary>"leve" ou "pesado": o MJ escolhe na hora (p.177).</summary>
        [JsonProperty("peso")] public string Peso { get; set; }
    }

    /// <summary>Uma parte do modificador: "rotulo:perigoso" +1, "condicao:assustado" −2, "condicoes" +2 (golpe poderoso).</summary>
    public sealed class ParteDoModificador
    {
        [JsonProperty("origem")] public string Origem { get; set; }
        /// <summary>O nome como estava ao rolar ("Perigoso", "Assustado").</summary>
        [JsonProperty("texto")] public string Texto { get; set; }
        [JsonProperty("valor")] public int Valor { get; set; }
    }

    /// <summary>
    /// Uma rolagem anotada para depois: quem rola é a personagem do evento; o motivo é o do evento. Fica como botão no
    /// log até alguém rolar com a origem apontando para ele (o MJ resolve na ordem que couber).
    /// </summary>
    public sealed class PedidoDeRolagem
    {
        [JsonProperty("movimento")] public string Movimento { get; set; }
        [JsonProperty("nome_do_movimento")] public string NomeDoMovimento { get; set; }
        [JsonProperty("alvo", NullValueHandling = NullValueHandling.Ignore)] public string Alvo { get; set; }
    }

    /// <summary>
    /// Uma rolagem de 2d6 na mesa. O dado cai e o resultado ainda não vale: fica pendente enquanto alguém pode
    /// intervir (ajuda da pilha, egoísta, renúncia de Influência, Defender, carta custom). O mestre aceita e aí
    /// ela vale. O teto (−3 a +4) só vale no modificador da rolagem; as intervenções deslizam livres.
    /// </summary>
    public sealed class RolagemDaMesa
    {
        [JsonProperty("dados")] public int[] Dados { get; set; } = new int[2];
        /// <summary>O modificador da rolagem, já dentro do teto.</summary>
        [JsonProperty("mod")] public int Mod { get; set; }
        /// <summary>
        /// De onde veio o modificador, antes do teto: o Rótulo (ou as condições, no golpe poderoso) e cada condição marcada
        /// que pesa no movimento (−2). Vazio na rolagem livre do console e nas rolagens antigas.
        /// </summary>
        [JsonProperty("partes")] public List<ParteDoModificador> Partes { get; set; } = new List<ParteDoModificador>();
        [JsonProperty("intervencoes")] public List<Intervencao> Intervencoes { get; set; } = new List<Intervencao>();
        /// <summary>O mestre aceitou: o resultado vale e ninguém mais intervém.</summary>
        [JsonProperty("aceita")] public bool Aceita { get; set; }
        /// <summary>O movimento rolado (id de movimentos_basicos.json), ou null na rolagem livre do console.</summary>
        [JsonProperty("movimento", NullValueHandling = NullValueHandling.Ignore)] public string Movimento { get; set; }
        /// <summary>O nome do movimento quando rolou (o log não muda se o nome mudar depois).</summary>
        [JsonProperty("nome_do_movimento", NullValueHandling = NullValueHandling.Ignore)] public string NomeDoMovimento { get; set; }
        /// <summary>O Rótulo que a rolagem somou.</summary>
        [JsonProperty("rotulo", NullValueHandling = NullValueHandling.Ignore)] public string Rotulo { get; set; }
        /// <summary>Id da personagem ou do NPC que é o alvo, ou null (ninguém).</summary>
        [JsonProperty("alvo", NullValueHandling = NullValueHandling.Ignore)] public string Alvo { get; set; }
        /// <summary>
        /// Id do evento da rolagem que pediu esta (o golpe poderoso da troca do Enfrentar), ou null: pedida pelo MJ
        /// (movimento numa falha, golpe emocional) ou rolada por conta própria.
        /// </summary>
        [JsonProperty("origem", NullValueHandling = NullValueHandling.Ignore)] public string Origem { get; set; }

        // Gravados no Aceitar (antes, as intervenções ainda mudam o total). Regra que diz "em um sucesso"
        // lê o resultado; regra do 7–9 lê o tipo. Ninguém recalcula.
        /// <summary>O binário: "sucesso" (7+) ou "falha" (6−). Null enquanto pendente.</summary>
        [JsonProperty("resultado", NullValueHandling = NullValueHandling.Ignore)] public string Resultado { get; set; }
        /// <summary>"total" (10+), "drawback" (7–9) ou "falha" (6−). Null enquanto pendente.</summary>
        [JsonProperty("tipo", NullValueHandling = NullValueHandling.Ignore)] public string Tipo { get; set; }
        /// <summary>As opções do movimento escolhidas no resultado (ids do movimento, com o texto de quando rolou).</summary>
        [JsonProperty("escolhas")] public List<EscolhaDaRolagem> Escolhas { get; set; } = new List<EscolhaDaRolagem>();
        /// <summary>A troca de golpes do sucesso (Enfrentar), como o mestre marcou; null se não houve troca.</summary>
        [JsonProperty("troca", NullValueHandling = NullValueHandling.Ignore)] public TrocaDeGolpes Troca { get; set; }
        /// <summary>
        /// A falha pede um movimento do MJ (o movimento não descreve a própria falha): fica a pendência no log até o MJ
        /// fazer. Gravado no Aceitar.
        /// </summary>
        [JsonProperty("pede_movimento_do_mj", DefaultValueHandling = DefaultValueHandling.Ignore)] public bool PedeMovimentoDoMj { get; set; }
        /// <summary>Quando o mestre aceitou (a hora da rolagem é o "quando" do evento).</summary>
        [JsonProperty("aceita_em", NullValueHandling = NullValueHandling.Ignore)] public string AceitaEm { get; set; }

        public const string ResultadoSucesso = "sucesso", ResultadoFalha = "falha";
        public const string TipoTotal = "total", TipoDrawback = "drawback", TipoFalha = "falha";

        /// <summary>Sucesso é 7+ (10+ e 7–9 são sucessos); falha é 6−.</summary>
        public static string ResultadoDe(int total) => total >= 7 ? ResultadoSucesso : ResultadoFalha;
        public static string TipoDe(int total) => total >= 10 ? TipoTotal : total >= 7 ? TipoDrawback : TipoFalha;
        public static string NomeDoTipo(string tipo) => tipo switch
        {
            TipoTotal => "sucesso total",
            TipoDrawback => "sucesso com drawback",
            _ => "falha",
        };

        /// <summary>O que o dado deu, com o modificador (antes das intervenções).</summary>
        [JsonIgnore] public int Inicial => Dados.Sum() + Mod;
        /// <summary>O resultado com as intervenções.</summary>
        [JsonIgnore] public int Total => Inicial + Intervencoes.Sum(i => i.Mod);

        public static string Faixa(int total) => total >= 10 ? "10+" : total >= 7 ? "7–9" : "6−";
    }

    /// <summary>
    /// A troca de golpes num sucesso do Enfrentar (livro p.57). Contra NPC: o alvo marca 1 condição (o MJ escolhe) e
    /// quem rolou recebe o retorno que o MJ escolher: uma condição OU tomar um golpe poderoso. "Resistir" deixa quem
    /// rolou ileso. Contra PJ: os dois tomam golpe poderoso (resistir livra quem escolheu). Gravada no Aceitar; marcar
    /// as condições na mesa e abrir o golpe poderoso são o passo seguinte.
    /// </summary>
    public sealed class TrocaDeGolpes
    {
        public const string RetornoCondicao = "condicao", RetornoGolpePoderoso = "golpe_poderoso", RetornoIleso = "ileso";

        /// <summary>
        /// A condição que o alvo marca: no NPC, a da troca (obrigatória se ele tem alguma livre); no PJ, a opcional do
        /// "impressionar" (o MJ pode dar uma pela nova percepção, p.58). Null se não houve.
        /// </summary>
        [JsonProperty("condicao_do_alvo", NullValueHandling = NullValueHandling.Ignore)] public string CondicaoDoAlvo { get; set; }
        /// <summary>O alvo é PJ: toma um golpe poderoso.</summary>
        [JsonProperty("alvo_golpe_poderoso")] public bool AlvoGolpePoderoso { get; set; }
        /// <summary>O que quem rolou recebe: "condicao", "golpe_poderoso" ou "ileso".</summary>
        [JsonProperty("retorno")] public string Retorno { get; set; }
        [JsonProperty("condicao_de_quem_rolou", NullValueHandling = NullValueHandling.Ignore)] public string CondicaoDeQuemRolou { get; set; }
    }

    /// <summary>
    /// Uma opção escolhida no resultado ("resistir" no Enfrentar) e, se a opção tem alternativas dentro (o MJ decide
    /// entre um aliado ganhar a chance ou +1 Equipe), qual foi marcada. Os textos ficam como estavam ao rolar.
    /// </summary>
    public sealed class EscolhaDaRolagem
    {
        [JsonProperty("opcao")] public string Opcao { get; set; }
        [JsonProperty("texto")] public string Texto { get; set; }
        /// <summary>Índice da alternativa marcada dentro da opção, ou null se a opção não tem alternativas.</summary>
        [JsonProperty("subopcao", NullValueHandling = NullValueHandling.Ignore)] public int? Subopcao { get; set; }
        [JsonProperty("subtexto", NullValueHandling = NullValueHandling.Ignore)] public string Subtexto { get; set; }
        /// <summary>As condições que a opção marcou em quem rolou ("aguentar a dor": duas, à escolha), ou null.</summary>
        [JsonProperty("condicoes", NullValueHandling = NullValueHandling.Ignore)] public List<string> Condicoes { get; set; }
        /// <summary>Quantas condições a opção pedia e não havia livres (precisa marcar e não tem: sai de cena). 0 se nenhuma.</summary>
        [JsonProperty("faltaram_condicoes", DefaultValueHandling = DefaultValueHandling.Ignore)] public int FaltaramCondicoes { get; set; }
        /// <summary>
        /// Quem ganha com a opção ("ceder terreno": quem ganha a abertura) e recebe o lembrete no cartão; null = ninguém
        /// (o mestre anota à mão).
        /// </summary>
        [JsonProperty("para", NullValueHandling = NullValueHandling.Ignore)] public string Para { get; set; }
        /// <summary>As escolhas de dentro da opção ("duas opções da lista de 7–9"), ou null.</summary>
        [JsonProperty("dentro", NullValueHandling = NullValueHandling.Ignore)] public List<EscolhaDaRolagem> Dentro { get; set; }

        /// <summary>Esta escolha e as de dentro dela.</summary>
        [JsonIgnore] public IEnumerable<EscolhaDaRolagem> ComAsDeDentro =>
            new[] { this }.Concat(Dentro?.SelectMany(d => d.ComAsDeDentro) ?? Enumerable.Empty<EscolhaDaRolagem>());
    }

    /// <summary>
    /// Quem mexeu no resultado depois do dado, de onde veio e quanto. Intervenção simples só tem o modificador;
    /// a de rolagem (Defender) guarda também a segunda rolagem em texto. O custo da pilha é cobrado ao intervir
    /// e devolvido se a intervenção sair.
    /// </summary>
    public sealed class Intervencao
    {
        public const string AjudaDaPilha = "ajuda (pilha)", EgoistaDaPilha = "egoísta (pilha)";
        /// <summary>Gastar da pilha: a mesa grava como ajuda (outra pessoa) ou egoísta (quem rola).</summary>
        public const string DaPilha = "pilha de Equipe";

        /// <summary>Id da personagem ou do NPC que intervém.</summary>
        [JsonProperty("quem")] public string Quem { get; set; }
        /// <summary>De onde veio: "ajuda (pilha)", "egoísta (pilha)", "renúncia de Influência", "defender"...</summary>
        [JsonProperty("de_onde")] public string DeOnde { get; set; }
        [JsonProperty("mod")] public int Mod { get; set; }
        [JsonProperty("custo_equipe")] public int CustoEquipe { get; set; }
        /// <summary>A segunda rolagem (Defender), em texto; null na intervenção simples.</summary>
        [JsonProperty("rolagem", NullValueHandling = NullValueHandling.Ignore)] public string Rolagem { get; set; }
        [JsonProperty("detalhe")] public string Detalhe { get; set; } = string.Empty;
    }

    /// <summary>
    /// Um bônus dado pelo mestre na mesa: adiante, constante ou controle, com valor e o contexto em que vale
    /// ("agindo pelas respostas da avaliação"). Usar não apaga: desativa (o log aponta para ele).
    /// </summary>
    public sealed class BonusDeMesa
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("personagem")] public string Personagem { get; set; }
        [JsonProperty("tipo")] public string Tipo { get; set; }
        [JsonProperty("valor")] public int Valor { get; set; }
        [JsonProperty("contexto")] public string Contexto { get; set; } = string.Empty;
        [JsonProperty("ativo")] public bool Ativo { get; set; }
    }
}
