using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TorreDeControle.Core.Armazenamento;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// Laboratório de personagens (fora de sala): criar, editar a identidade, ativar/inativar.
    /// Toda mudança grava na hora. A ficha de criação (Rótulos, cartas, módulos) vem na parte 2.
    /// </summary>
    public sealed class CadastroPersonagens
    {
        public const string Chave = "personagens";
        const int VersaoArquivo = 1;

        readonly IArmazenamento armazenamento;
        readonly List<Personagem> personagens;

        CadastroPersonagens(IArmazenamento armazenamento, List<Personagem> personagens)
        {
            this.armazenamento = armazenamento;
            this.personagens = personagens;
        }

        public static CadastroPersonagens Carregar(IArmazenamento armazenamento)
        {
            var texto = armazenamento.Ler(Chave);
            var arquivo = string.IsNullOrWhiteSpace(texto)
                ? new Arquivo()
                : JsonConvert.DeserializeObject<Arquivo>(texto) ?? new Arquivo();
            return new CadastroPersonagens(armazenamento, arquivo.Personagens ?? new List<Personagem>());
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

            personagem.Identidade = identidade;
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
