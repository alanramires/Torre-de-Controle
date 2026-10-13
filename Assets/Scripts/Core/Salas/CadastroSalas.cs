using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TorreDeControle.Core.Armazenamento;
using TorreDeControle.Core.Personagens;

namespace TorreDeControle.Core.Salas
{
    /// <summary>
    /// Gestão de Salas: criar, renomear, número de slots, ativar/inativar, e os slots
    /// (pôr jogador, pôr personagem do laboratório como cópia, tirar). Toda mudança grava na hora.
    /// </summary>
    public sealed class CadastroSalas
    {
        public const string Chave = "salas";
        const int VersaoArquivo = 1;
        public const int MinimoDeSlots = 1;
        public const int MaximoDeSlots = 12;

        readonly IArmazenamento armazenamento;
        readonly CadastroPersonagens personagens;
        readonly List<Sala> salas;

        CadastroSalas(IArmazenamento armazenamento, CadastroPersonagens personagens, List<Sala> salas)
        {
            this.armazenamento = armazenamento;
            this.personagens = personagens;
            this.salas = salas;
        }

        public static CadastroSalas Carregar(IArmazenamento armazenamento, CadastroPersonagens personagens)
        {
            var texto = armazenamento.Ler(Chave);
            var arquivo = string.IsNullOrWhiteSpace(texto)
                ? new Arquivo()
                : JsonConvert.DeserializeObject<Arquivo>(texto) ?? new Arquivo();
            var lista = arquivo.Salas ?? new List<Sala>();
            foreach (var s in lista)
                s.Slots ??= new List<Slot>();
            return new CadastroSalas(armazenamento, personagens, lista);
        }

        /// <summary>Ativas primeiro; cada bloco na ordem que o mestre arrastou.</summary>
        public IReadOnlyList<Sala> Listar() => Ordenacao.Ordenar(salas, s => s.Nome);

        public Sala Buscar(string id) => salas.FirstOrDefault(s => s.Id == id);

        public Resultado Mover(string id, int indice)
        {
            var sala = Buscar(id);
            if (sala == null)
                return Resultado.Falha("Sala não encontrada.");
            Ordenacao.Mover(salas, sala, indice, s => s.Nome);
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado Criar(string nome, int slots)
        {
            var limpo = Limpar(nome);
            var erro = ValidarNome(limpo, idIgnorado: null) ?? ValidarQuantidade(slots);
            if (erro != null)
                return Resultado.Falha(erro);

            var sala = new Sala { Id = Guid.NewGuid().ToString("N"), Nome = limpo, Ordem = Ordenacao.Fim(salas) };
            for (var k = 0; k < slots; k++)
                sala.Slots.Add(new Slot());
            salas.Add(sala);
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado Renomear(string id, string nome)
        {
            var sala = Buscar(id);
            if (sala == null)
                return Resultado.Falha("Sala não encontrada.");
            var limpo = Limpar(nome);
            if (limpo == sala.Nome)
                return Resultado.Sucesso();
            var erro = ValidarNome(limpo, idIgnorado: id);
            if (erro != null)
                return Resultado.Falha(erro);

            sala.Nome = limpo;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Muda o número de slots. Diminuir só tira slots vazios do fim.</summary>
        public Resultado DefinirSlots(string id, int quantidade)
        {
            var sala = Buscar(id);
            if (sala == null)
                return Resultado.Falha("Sala não encontrada.");
            var erro = ValidarQuantidade(quantidade);
            if (erro != null)
                return Resultado.Falha(erro);

            for (var k = quantidade; k < sala.Slots.Count; k++)
                if (sala.Slots[k].Jogador != null || sala.Slots[k].Personagem != null)
                    return Resultado.Falha($"A vaga {k + 1} está ocupada; esvazie-a antes de diminuir a sala.");

            while (sala.Slots.Count < quantidade)
                sala.Slots.Add(new Slot());
            sala.Slots.RemoveRange(quantidade, sala.Slots.Count - quantidade);
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado DefinirAtivo(string id, bool ativo)
        {
            var sala = Buscar(id);
            if (sala == null)
                return Resultado.Falha("Sala não encontrada.");
            if (sala.Ativo == ativo)
                return Resultado.Sucesso();

            sala.Ativo = ativo;
            sala.Ordem = Ordenacao.Fim(salas); // entra no fim do outro bloco
            Gravar();
            return Resultado.Sucesso();
        }

        // ---------- slots ----------

        /// <summary>Põe (ou troca, ou tira com null) o jogador do slot. Na sala, um slot por jogador.</summary>
        public Resultado DefinirJogador(string salaId, int indice, string jogador)
        {
            var (sala, slot, erro) = Achar(salaId, indice);
            if (erro != null)
                return Resultado.Falha(erro);

            jogador = string.IsNullOrWhiteSpace(jogador) ? null : jogador;
            if (jogador != null && sala.Slots.Any(s => s != slot && s.Jogador == jogador))
                return Resultado.Falha("Esse jogador já está em outra vaga desta sala.");

            slot.Jogador = jogador;
            if (slot.Personagem != null)
                personagens.VincularJogador(slot.Personagem, jogador);
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// Põe no slot uma cópia da personagem do laboratório (a original fica intacta).
        /// <paramref name="comJogo"/>: false = só a criação (a cópia nasce da foto); true = criação +
        /// o que o laboratório marcou de jogo (potencial, avanços, trilhas...).
        /// Slot sem jogador herda o da personagem, se ele estiver livre na sala.
        /// </summary>
        public Resultado PorPersonagem(string salaId, int indice, string origemId, bool comJogo)
        {
            var (sala, slot, erro) = Achar(salaId, indice);
            if (erro != null)
                return Resultado.Falha(erro);
            if (slot.Personagem != null)
                return Resultado.Falha("A vaga já tem personagem; tire-a antes.");

            var origem = personagens.Buscar(origemId);
            if (origem == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (sala.Slots.Any(s => s.Personagem != null && personagens.Buscar(s.Personagem)?.Origem == origemId))
                return Resultado.Falha("Essa personagem já está em outra vaga desta sala.");

            var jogador = slot.Jogador;
            if (jogador == null && origem.Identidade.Jogador != null &&
                sala.Slots.All(s => s.Jogador != origem.Identidade.Jogador))
                jogador = origem.Identidade.Jogador;

            var copia = personagens.CopiarParaSala(origemId, sala.Id, jogador, comJogo, out var resultado);
            if (!resultado.Ok)
                return resultado;

            slot.Jogador = jogador;
            slot.Personagem = copia.Id;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// Esvazia a personagem do slot. A cópia não se exclui: fica aposentada na sala, com o histórico.
        /// O jogador continua no slot.
        /// </summary>
        public Resultado TirarPersonagem(string salaId, int indice)
        {
            var (_, slot, erro) = Achar(salaId, indice);
            if (erro != null)
                return Resultado.Falha(erro);
            if (slot.Personagem == null)
                return Resultado.Sucesso();

            // Saiu do jogo: aposentada (fica no arquivo da sala, só consulta).
            personagens.Aposentar(slot.Personagem);
            slot.Personagem = null;
            Gravar();
            return Resultado.Sucesso();
        }

        (Sala sala, Slot slot, string erro) Achar(string salaId, int indice)
        {
            var sala = Buscar(salaId);
            if (sala == null)
                return (null, null, "Sala não encontrada.");
            if (indice < 0 || indice >= sala.Slots.Count)
                return (sala, null, "Vaga inexistente.");
            return (sala, sala.Slots[indice], null);
        }

        string ValidarNome(string nome, string idIgnorado)
        {
            if (nome.Length == 0)
                return "O nome não pode ficar vazio.";
            var repetido = salas.Any(s =>
                s.Id != idIgnorado && string.Equals(s.Nome, nome, StringComparison.CurrentCultureIgnoreCase));
            return repetido ? $"Já existe uma sala chamada \"{nome}\"." : null;
        }

        static string ValidarQuantidade(int slots) =>
            slots < MinimoDeSlots || slots > MaximoDeSlots
                ? $"A sala tem de {MinimoDeSlots} a {MaximoDeSlots} vagas."
                : null;

        static string Limpar(string nome) => (nome ?? string.Empty).Trim();

        void Gravar()
        {
            var arquivo = new Arquivo { Versao = VersaoArquivo, Salas = salas };
            armazenamento.Gravar(Chave, JsonConvert.SerializeObject(arquivo, Formatting.Indented));
        }

        sealed class Arquivo
        {
            [JsonProperty("versao")] public int Versao { get; set; } = VersaoArquivo;
            [JsonProperty("salas")] public List<Sala> Salas { get; set; } = new List<Sala>();
        }
    }
}
