using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TorreDeControle.Core.Armazenamento;

namespace TorreDeControle.Core.Jogadores
{
    /// <summary>
    /// Gestão de Jogadores: criar, renomear, ativar/inativar. Toda mudança grava na hora
    /// (confirmar é um Enter; não há "salvar").
    /// </summary>
    public sealed class CadastroJogadores
    {
        public const string Chave = "jogadores";
        const int VersaoArquivo = 1;

        readonly IArmazenamento armazenamento;
        readonly List<Jogador> jogadores;

        CadastroJogadores(IArmazenamento armazenamento, List<Jogador> jogadores)
        {
            this.armazenamento = armazenamento;
            this.jogadores = jogadores;
        }

        public static CadastroJogadores Carregar(IArmazenamento armazenamento)
        {
            var texto = armazenamento.Ler(Chave);
            var arquivo = string.IsNullOrWhiteSpace(texto)
                ? new Arquivo()
                : JsonConvert.DeserializeObject<Arquivo>(texto) ?? new Arquivo();
            return new CadastroJogadores(armazenamento, arquivo.Jogadores ?? new List<Jogador>());
        }

        /// <summary>Ativos primeiro; cada bloco na ordem que o mestre arrastou.</summary>
        public IReadOnlyList<Jogador> Listar() => Ordenacao.Ordenar(jogadores, j => j.Nome);

        /// <summary>Leva o jogador para a posição <paramref name="indice"/> do bloco dele (ativos ou inativos).</summary>
        public Resultado Mover(string id, int indice)
        {
            var jogador = Buscar(id);
            if (jogador == null)
                return Resultado.Falha("Jogador não encontrado.");
            Ordenacao.Mover(jogadores, jogador, indice, j => j.Nome);
            Gravar();
            return Resultado.Sucesso();
        }

        public Jogador Buscar(string id) => jogadores.FirstOrDefault(j => j.Id == id);

        public Resultado Criar(string nome)
        {
            var limpo = Limpar(nome);
            var erro = ValidarNome(limpo, idIgnorado: null);
            if (erro != null)
                return Resultado.Falha(erro);

            jogadores.Add(new Jogador
            {
                Id = Guid.NewGuid().ToString("N"), Nome = limpo, Ativo = true, Ordem = Ordenacao.Fim(jogadores),
            });
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado Renomear(string id, string nome)
        {
            var jogador = Buscar(id);
            if (jogador == null)
                return Resultado.Falha("Jogador não encontrado.");

            var limpo = Limpar(nome);
            if (limpo == jogador.Nome)
                return Resultado.Sucesso();

            var erro = ValidarNome(limpo, idIgnorado: id);
            if (erro != null)
                return Resultado.Falha(erro);

            jogador.Nome = limpo;
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado DefinirAtivo(string id, bool ativo)
        {
            var jogador = Buscar(id);
            if (jogador == null)
                return Resultado.Falha("Jogador não encontrado.");
            if (jogador.Ativo == ativo)
                return Resultado.Sucesso();

            jogador.Ativo = ativo;
            jogador.Ordem = Ordenacao.Fim(jogadores); // entra no fim do outro bloco
            Gravar();
            return Resultado.Sucesso();
        }

        string ValidarNome(string nome, string idIgnorado)
        {
            if (nome.Length == 0)
                return "O nome não pode ficar vazio.";

            var repetido = jogadores.Any(j =>
                j.Id != idIgnorado &&
                string.Equals(j.Nome, nome, StringComparison.CurrentCultureIgnoreCase));
            return repetido ? $"Já existe um jogador chamado \"{nome}\"." : null;
        }

        static string Limpar(string nome) => (nome ?? string.Empty).Trim();

        void Gravar()
        {
            var arquivo = new Arquivo { Versao = VersaoArquivo, Jogadores = jogadores };
            armazenamento.Gravar(Chave, JsonConvert.SerializeObject(arquivo, Formatting.Indented));
        }

        sealed class Arquivo
        {
            [JsonProperty("versao")] public int Versao { get; set; } = VersaoArquivo;
            [JsonProperty("jogadores")] public List<Jogador> Jogadores { get; set; } = new List<Jogador>();
        }
    }
}
