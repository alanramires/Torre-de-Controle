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
    public sealed class CadastroPersonagens
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
                p.Criacao ??= new RascunhoCriacao();
            return new CadastroPersonagens(armazenamento, regras, lista);
        }

        /// <summary>Ativas primeiro, depois pelo nome de exibição.</summary>
        public IReadOnlyList<Personagem> Listar() =>
            personagens
                .OrderByDescending(p => p.Ativo)
                .ThenBy(p => p.NomeExibicao, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

        public Personagem Buscar(string id) => personagens.FirstOrDefault(p => p.Id == id);

        /// <summary>Novo herói no laboratório: status novo, controle jogador, jovem, herói.</summary>
        public Resultado Criar(string nome, string codinome, string arquetipo, string jogador)
        {
            var identidade = Montar(nome, codinome, arquetipo, jogador);
            var erro = Validar(identidade);
            if (erro != null)
                return Resultado.Falha(erro);

            personagens.Add(new Personagem { Id = Guid.NewGuid().ToString("N"), Identidade = identidade });
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
            return (erros, avisos);
        }

        /// <summary>Novo → criado: congela a foto da criação. Com avisos, só se o mestre aceitar.</summary>
        public Resultado Aprovar(string id, bool aceitarAvisos)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (p.Status != StatusCriacao.Novo)
                return Resultado.Falha("Essa ficha já foi aprovada.");

            var (erros, avisos) = Validar(p);
            if (erros.Count > 0)
                return Resultado.Falha(string.Join(" ", erros));
            if (avisos.Count > 0 && !aceitarAvisos)
                return Resultado.Falha("Há pendências: " + string.Join(" ", avisos));

            p.Foto = new FotoCriacao
            {
                AprovadaEm = DateTime.Now.ToString("s"),
                Arquetipo = p.Identidade.Arquetipo,
                Rotulos = new Dictionary<string, int>(RotulosAtuais(p)),
                AvisosAceitos = new List<string>(avisos),
            };
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
            Gravar();
            return Resultado.Sucesso();
        }

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
            var arquivo = new Arquivo { Versao = VersaoArquivo, Personagens = personagens };
            armazenamento.Gravar(Chave, JsonConvert.SerializeObject(arquivo, Formatting.Indented));
        }

        sealed class Arquivo
        {
            [JsonProperty("versao")] public int Versao { get; set; } = VersaoArquivo;
            [JsonProperty("personagens")] public List<Personagem> Personagens { get; set; } = new List<Personagem>();
        }
    }
}
