using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TorreDeControle.Core.Armazenamento;
using TorreDeControle.Core.Regras;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// Laboratório de personagens (fora de sala): criar, editar a identidade, ativar/inativar,
    /// montar a ficha de criação (hoje: o +1 livre) e aprovar. Toda mudança grava na hora.
    /// </summary>
    public sealed partial class CadastroPersonagens
    {
        public const string Chave = "personagens";
        const int VersaoArquivo = 1;

        readonly IArmazenamento armazenamento;
        readonly RegrasArquetipos regras;
        readonly List<Personagem> personagens;

        CadastroPersonagens(IArmazenamento armazenamento, RegrasArquetipos regras, List<Personagem> personagens)
        {
            this.armazenamento = armazenamento;
            this.regras = regras;
            this.personagens = personagens;
        }

        public static CadastroPersonagens Carregar(IArmazenamento armazenamento, RegrasArquetipos regras)
        {
            var texto = armazenamento.Ler(Chave);
            var arquivo = string.IsNullOrWhiteSpace(texto)
                ? new Arquivo()
                : JsonConvert.DeserializeObject<Arquivo>(texto) ?? new Arquivo();
            var lista = arquivo.Personagens ?? new List<Personagem>();
            foreach (var p in lista)
            {
                p.Criacao ??= new RascunhoCriacao();
                p.Criacao.Habilidades ??= new EscolhasHabilidades();
                p.Criacao.Movimentos ??= new List<string>();
                p.Criacao.ExtrasEscolhas ??= new Dictionary<string, List<string>>();
                p.Criacao.ExtrasTextos ??= new Dictionary<string, string>();
                p.AvancosMarcados ??= new List<string>();
                p.Cumpridos ??= new Dictionary<string, string>();
                p.Contadores ??= new Dictionary<string, int>();
                p.EscolhasDeJogo ??= new Dictionary<string, List<string>>();
                p.TextosDeJogo ??= new Dictionary<string, string>();
                p.ItensDeJogo ??= new Dictionary<string, List<ItemDeJogo>>();
                p.ObsMovimentos ??= new Dictionary<string, string>();
            }
            return new CadastroPersonagens(armazenamento, regras, lista);
        }

        /// <summary>Ativas primeiro; cada bloco na ordem que o mestre arrastou.</summary>
        public IReadOnlyList<Personagem> Listar() => Ordenacao.Ordenar(personagens, p => p.NomeExibicao);

        /// <summary>Leva a personagem para a posição <paramref name="indice"/> do bloco dela (ativas ou inativas).</summary>
        public Resultado Mover(string id, int indice)
        {
            var personagem = Buscar(id);
            if (personagem == null)
                return Resultado.Falha("Personagem não encontrada.");
            Ordenacao.Mover(personagens, personagem, indice, p => p.NomeExibicao);
            Gravar();
            return Resultado.Sucesso();
        }

        public Personagem Buscar(string id) => personagens.FirstOrDefault(p => p.Id == id);

        /// <summary>Novo herói no laboratório: status novo, controle jogador, jovem, herói.</summary>
        public Resultado Criar(string nome, string codinome, string arquetipo, string jogador)
        {
            var identidade = Montar(nome, codinome, arquetipo, jogador);
            var erro = Validar(identidade);
            if (erro != null)
                return Resultado.Falha(erro);

            personagens.Add(new Personagem
            {
                Id = Guid.NewGuid().ToString("N"), Identidade = identidade, Ordem = Ordenacao.Fim(personagens),
            });
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado AlterarIdentidade(string id, string nome, string codinome, string arquetipo, string jogador)
        {
            var personagem = Buscar(id);
            if (personagem == null)
                return Resultado.Falha("Personagem não encontrada.");

            var identidade = Montar(nome, codinome, arquetipo, jogador);
            var erro = Validar(identidade);
            if (erro != null)
                return Resultado.Falha(erro);

            // Depois de criada, trocar de arquétipo é um avanço (tardio), não uma edição.
            if (personagem.Status != StatusCriacao.Novo && identidade.Arquetipo != personagem.Identidade.Arquetipo)
                return Resultado.Falha("Depois de criada, trocar de arquétipo é um avanço, não uma edição.");

            // Em "novo", trocar de arquétipo reseta a ficha de criação: as escolhas eram do arquétipo antigo.
            // (A tela pede confirmação antes, se houver escolhas.)
            if (identidade.Arquetipo != personagem.Identidade.Arquetipo)
                personagem.Criacao = new RascunhoCriacao();

            identidade.Aparencia = personagem.Identidade.Aparencia;
            identidade.Resumo = personagem.Identidade.Resumo;
            // As respostas são das perguntas do arquétipo: trocar de arquétipo as apaga.
            if (identidade.Arquetipo == personagem.Identidade.Arquetipo)
                identidade.RespostasHistorico = personagem.Identidade.RespostasHistorico;
            personagem.Identidade = identidade;
            Gravar();
            return Resultado.Sucesso();
        }

        // ---------- ficha de criação: Núcleo ----------

        /// <summary>Valor do Rótulo que o arquétipo dá (o azul da ficha).</summary>
        public int RotuloDoArquetipo(Personagem p, string rotulo)
        {
            var regra = regras.Buscar(p.Foto?.Arquetipo ?? p.Identidade.Arquetipo);
            return regra != null && regra.Rotulos.TryGetValue(rotulo, out var v) ? v : 0;
        }

        /// <summary>Rótulos de agora: a foto, se aprovada; senão, arquétipo + o +1 livre do rascunho.</summary>
        public IReadOnlyDictionary<string, int> RotulosAtuais(Personagem p)
        {
            if (p.Foto != null)
                return p.Foto.Rotulos;

            var atuais = new Dictionary<string, int>();
            foreach (var r in Rotulos.Todos)
                atuais[r] = RotuloDoArquetipo(p, r);
            if (p.Criacao.BonusLivre != null && atuais.ContainsKey(p.Criacao.BonusLivre))
                atuais[p.Criacao.BonusLivre] += regras.BonusLivre;
            return atuais;
        }

        /// <summary>Põe o +1 livre num Rótulo (null tira). Só em "novo", e nunca acima do máximo da criação.</summary>
        public Resultado EscolherBonusLivre(string id, string rotulo)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (p.Status != StatusCriacao.Novo)
                return Resultado.Falha("A ficha já foi aprovada; o +1 livre é da criação.");
            if (rotulo != null)
            {
                if (!Rotulos.Todos.Contains(rotulo))
                    return Resultado.Falha("Rótulo desconhecido.");
                if (RotuloDoArquetipo(p, rotulo) + regras.BonusLivre > regras.MaximoNaCriacao)
                    return Resultado.Falha($"{Rotulos.Nome(rotulo)} já começa em +{regras.MaximoNaCriacao}; o +1 livre não pode ir para ele.");
            }

            p.Criacao.BonusLivre = rotulo;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// O que a validação aponta. Erros impedem aprovar; avisos o mestre pode aceitar
        /// (a validação avisa, quem decide é o mestre).
        /// </summary>
        public (IReadOnlyList<string> erros, IReadOnlyList<string> avisos) Validar(Personagem p)
        {
            var erros = new List<string>();
            var avisos = new List<string>();
            if (regras.Buscar(p.Identidade.Arquetipo) == null)
                erros.Add("Arquétipo sem regras de criação.");
            if (p.Criacao.BonusLivre == null)
                avisos.Add("Falta escolher o +1 livre.");
            ValidarHabilidades(p, avisos);
            ValidarModulos(p, avisos);
            var semResposta = PerguntasSemResposta(p);
            if (semResposta > 0)
                avisos.Add($"Histórico: responda as perguntas da ficha (faltam {semResposta}).");
            return (erros, avisos);
        }

        /// <summary>Novo → criado: congela a foto da criação. Com avisos, só se o mestre aceitar.</summary>
        /// <summary>
        /// Novo → criado: congela a foto da criação. Só sem pendências (decidido em 2026-10-13:
        /// ficha incompleta congelada só gera conserto). Depois de aprovada, o mestre ainda corrige
        /// por ajuste manual com motivo. Observações não travam.
        /// </summary>
        public Resultado Aprovar(string id)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (p.Status != StatusCriacao.Novo)
                return Resultado.Falha("Essa ficha já foi aprovada.");

            var (erros, avisos) = Validar(p);
            if (erros.Count > 0)
                return Resultado.Falha(string.Join(" ", erros));
            if (avisos.Count > 0)
                return Resultado.Falha("Ainda há pendências: " + string.Join(" ", avisos));

            p.Foto = new FotoCriacao
            {
                AprovadaEm = DateTime.Now.ToString("s"),
                Arquetipo = p.Identidade.Arquetipo,
                Rotulos = new Dictionary<string, int>(RotulosAtuais(p)),
                Habilidades = HabilidadesAtuais(p).ToList(),
            };
            CongelarModulos(p, p.Foto);
            p.Status = StatusCriacao.Criado;
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado DefinirAtivo(string id, bool ativo)
        {
            var personagem = Buscar(id);
            if (personagem == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (personagem.Ativo == ativo)
                return Resultado.Sucesso();

            personagem.Ativo = ativo;
            personagem.Ordem = Ordenacao.Fim(personagens); // entra no fim do outro bloco
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// Aparência e breve resumo: texto livre, editável a qualquer momento (não fazem parte da foto
        /// mecânica; descrever a personagem não é regra).
        /// </summary>
        public Resultado AlterarTextos(string id, string aparencia, string resumo)
        {
            var personagem = Buscar(id);
            if (personagem == null)
                return Resultado.Falha("Personagem não encontrada.");
            var a = (aparencia ?? string.Empty).Trim();
            var r = (resumo ?? string.Empty).Trim();
            if (a == personagem.Identidade.Aparencia && r == personagem.Identidade.Resumo)
                return Resultado.Sucesso();
            personagem.Identidade.Aparencia = a;
            personagem.Identidade.Resumo = r;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Resposta à pergunta de histórico de número <paramref name="indice"/> (ordem da ficha).</summary>
        public Resultado ResponderHistorico(string id, int indice, string resposta)
        {
            var personagem = Buscar(id);
            if (personagem == null)
                return Resultado.Falha("Personagem não encontrada.");
            var perguntas = RegraDoArquetipo(personagem)?.Historico ?? new List<string>();
            if (indice < 0 || indice >= perguntas.Count)
                return Resultado.Falha("Pergunta desconhecida.");

            var respostas = personagem.Identidade.RespostasHistorico;
            while (respostas.Count < perguntas.Count)
                respostas.Add(string.Empty);
            var limpa = (resposta ?? string.Empty).Trim();
            if (respostas[indice] == limpa)
                return Resultado.Sucesso();
            respostas[indice] = limpa;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Quantas perguntas de histórico ainda estão sem resposta.</summary>
        public int PerguntasSemResposta(Personagem p)
        {
            var perguntas = RegraDoArquetipo(p)?.Historico ?? new List<string>();
            var respostas = p.Identidade.RespostasHistorico;
            var faltam = 0;
            for (var k = 0; k < perguntas.Count; k++)
                if (k >= respostas.Count || string.IsNullOrWhiteSpace(respostas[k]))
                    faltam++;
            return faltam;
        }

        /// <summary>Trocar de arquétipo agora apagaria algo? (escolhas de criação ou respostas de histórico)</summary>
        public bool TrocaDeArquetipoApaga(Personagem p) =>
            p.Criacao.TemEscolhas || p.Identidade.RespostasHistorico.Any(r => !string.IsNullOrWhiteSpace(r));

        /// <summary>Regras do arquétipo da personagem (o da foto, se aprovada).</summary>
        public RegrasArquetipos.RegraArquetipo RegraDoArquetipo(Personagem p) =>
            regras.Buscar(p.Foto?.Arquetipo ?? p.Identidade.Arquetipo);

        static Identidade Montar(string nome, string codinome, string arquetipo, string jogador) =>
            new Identidade
            {
                Nome = (nome ?? string.Empty).Trim(),
                Codinome = (codinome ?? string.Empty).Trim(),
                Arquetipo = arquetipo,
                Jogador = string.IsNullOrWhiteSpace(jogador) ? null : jogador,
            };

        static string Validar(Identidade identidade)
        {
            if (identidade.Nome.Length == 0 && identidade.Codinome.Length == 0)
                return "Dê pelo menos um nome ou um codinome.";
            if (Arquetipos.Buscar(identidade.Arquetipo) == null)
                return "Escolha um arquétipo.";
            return null;
        }

        void Gravar()
        {
            // Personagem em modo teste grava a foto real, não o que o teste mexeu.
            var gravar = personagens
                .Select(p => emTeste.TryGetValue(p.Id, out var real) ? JsonConvert.DeserializeObject<Personagem>(real) : p)
                .ToList();
            var arquivo = new Arquivo { Versao = VersaoArquivo, Personagens = gravar };
            armazenamento.Gravar(Chave, JsonConvert.SerializeObject(arquivo, Formatting.Indented));
        }

        // ---------- modo teste (playground do mestre, para validar a ficha) ----------

        /// <summary>Id → a personagem real (JSON) guardada ao ligar o modo teste.</summary>
        readonly Dictionary<string, string> emTeste = new Dictionary<string, string>();

        static readonly JsonSerializerSettings Restaurar =
            new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };

        public bool EmTeste(string id) => emTeste.ContainsKey(id);

        /// <summary>
        /// Liga o modo teste: guarda a personagem real. Daqui até desligar, tudo o que mudar fica só na
        /// memória (o disco continua com a real), e desligar desfaz tudo. Serve para validar a ficha
        /// antes de existirem as fontes de verdade (painel do mestre, eventos com motivo).
        /// </summary>
        public Resultado LigarTeste(string id)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (!emTeste.ContainsKey(id))
                emTeste[id] = JsonConvert.SerializeObject(p);
            return Resultado.Sucesso();
        }

        /// <summary>Desliga o modo teste: a personagem volta exatamente ao que era (mesmo objeto).</summary>
        public Resultado DesligarTeste(string id)
        {
            var p = Buscar(id);
            if (p == null || !emTeste.TryGetValue(id, out var real))
                return Resultado.Sucesso();
            JsonConvert.PopulateObject(real, p, Restaurar);
            emTeste.Remove(id);
            return Resultado.Sucesso();
        }

        sealed class Arquivo
        {
            [JsonProperty("versao")] public int Versao { get; set; } = VersaoArquivo;
            [JsonProperty("personagens")] public List<Personagem> Personagens { get; set; } = new List<Personagem>();
        }
    }
}
