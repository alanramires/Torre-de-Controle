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
            {
                s.Slots ??= new List<Slot>();
                s.FormacaoEquipe ??= new List<EnvioDeEquipe>();
            }
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

        // ---------- formação da equipe ----------

        /// <summary>
        /// Envia (ou reenvia) a resposta da personagem à pergunta de formação da equipe do arquétipo dela.
        /// O primeiro envio entra no fim da fila; reenviar troca o texto e mantém a posição. A resposta
        /// também vai para a ficha (EquipeResposta), para a aprovação na campanha cobrar.
        /// </summary>
        public Resultado EnviarFormacao(string salaId, string personagemId, string texto)
        {
            var sala = Buscar(salaId);
            if (sala == null)
                return Resultado.Falha("Sala não encontrada.");
            if (sala.Slots.All(s => s.Personagem != personagemId))
                return Resultado.Falha("Essa personagem não está numa vaga desta sala.");
            var limpo = (texto ?? string.Empty).Trim();
            if (limpo.Length == 0)
                return Resultado.Falha("Escreva a resposta antes de enviar.");
            var naFicha = personagens.ResponderEquipe(personagemId, limpo);
            if (!naFicha.Ok)
                return naFicha;

            var envio = sala.FormacaoEquipe.FirstOrDefault(e => e.Personagem == personagemId);
            if (envio == null)
                sala.FormacaoEquipe.Add(new EnvioDeEquipe
                {
                    Personagem = personagemId, Texto = limpo, EnviadoEm = DateTime.Now.ToString("s"),
                });
            else if (envio.Texto != limpo)
            {
                envio.Anterior = envio.Texto;
                envio.Texto = limpo;
            }
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// Desfaz o último envio da personagem (engano do mestre: respondeu pela personagem errada).
        /// Se foi um reenvio, volta o texto anterior e a posição fica; se foi o primeiro envio, sai da fila
        /// (quem vinha depois sobe). Um nível só.
        /// </summary>
        public Resultado DesfazerFormacao(string salaId, string personagemId)
        {
            var sala = Buscar(salaId);
            if (sala == null)
                return Resultado.Falha("Sala não encontrada.");
            var envio = sala.FormacaoEquipe.FirstOrDefault(e => e.Personagem == personagemId);
            if (envio == null)
                return Resultado.Falha("Essa personagem não enviou nada.");
            if (envio.Anterior != null)
            {
                envio.Texto = envio.Anterior;
                envio.Anterior = null;
            }
            else
                sala.FormacaoEquipe.Remove(envio);
            var naFicha = personagens.ResponderEquipe(personagemId, sala.FormacaoEquipe.Contains(envio) ? envio.Texto : string.Empty);
            if (!naFicha.Ok)
                return naFicha;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>A posição (1, 2, ...) do envio da personagem, ou 0 se ainda não enviou.</summary>
        public int PosicaoNaFormacao(Sala sala, string personagemId)
        {
            var k = sala.FormacaoEquipe.FindIndex(e => e.Personagem == personagemId);
            return k + 1;
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

        /// <summary>
        /// Marca ou desmarca um item do roteiro da primeira sessão (o mestre, durante a sessão). Não é evento
        /// de jogo: é a lista de conferência dele, e desmarcar não precisa de correção.
        /// </summary>
        public Resultado AlternarItemDaPrimeiraSessao(string salaId, string item)
        {
            var sala = Buscar(salaId);
            if (sala == null)
                return Resultado.Falha("Sala não encontrada.");
            if (string.IsNullOrWhiteSpace(item))
                return Resultado.Falha("Item sem id.");
            sala.PrimeiraSessao ??= new List<string>();
            if (!sala.PrimeiraSessao.Remove(item))
                sala.PrimeiraSessao.Add(item);
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// As campanhas ativas para "Entrar na sala": pronta = todas as heroínas das vagas em carreira (e ao
        /// menos uma). As outras vêm com o motivo ("Mayane em aprovação"), para entrar mesmo assim. A última
        /// jogada primeiro, depois as prontas.
        /// </summary>
        public IReadOnlyList<(Sala sala, bool pronta, string motivo)> ParaAMesa()
        {
            var lista = new List<(Sala, bool, string)>();
            foreach (var s in salas.Where(s => s.Ativo))
            {
                var herois = s.Slots.Where(v => v.Personagem != null).Select(v => personagens.Buscar(v.Personagem))
                    .Where(p => p != null && p.Ativo).ToList();
                // Só quantas já estão em carreira ("0/5 prontas", pronta = em carreira): o resto se vê na sala.
                var emCarreira = herois.Count(p => p.Status == StatusCriacao.EmCarreira);
                var pronta = herois.Count > 0 && emCarreira == herois.Count;
                var motivo = herois.Count == 0 ? "sem personagens nas vagas" : $"{emCarreira}/{herois.Count} prontos";
                lista.Add((s, pronta, motivo));
            }
            return lista.OrderByDescending(x => x.Item1.UltimaMesa ?? string.Empty).ThenByDescending(x => x.Item2).ToList();
        }


        /// <summary>Lembra quando a mesa da sala foi aberta (a lista de "Entrar na sala" sugere a última).</summary>
        public void MarcarMesaAberta(string salaId)
        {
            var sala = Buscar(salaId);
            if (sala == null)
                return;
            sala.UltimaMesa = DateTime.Now.ToString("s");
            Gravar();
        }

        public enum Anotacao { Formacao, Npcs, Trama }

        /// <summary>
        /// As anotações do mestre na sala (formação da equipe, NPCs, trama): texto livre, opcional, em qualquer
        /// status. Cada uma mora ao lado do que descreve.
        /// </summary>
        public Resultado DefinirAnotacao(string salaId, Anotacao qual, string texto)
        {
            var sala = Buscar(salaId);
            if (sala == null)
                return Resultado.Falha("Sala não encontrada.");
            var limpo = (texto ?? string.Empty).Trim();
            if (limpo == (LerAnotacao(sala, qual) ?? string.Empty))
                return Resultado.Sucesso();
            switch (qual)
            {
                case Anotacao.Formacao: sala.AnotacoesFormacao = limpo; break;
                case Anotacao.Npcs: sala.AnotacoesNpcs = limpo; break;
                default: sala.Trama = limpo; break;
            }
            Gravar();
            return Resultado.Sucesso();
        }

        public const string FormatoDeTrocaAnotacoes = "torre-de-controle/anotacoes";

        sealed class EnvelopeAnotacoes
        {
            [JsonProperty("formato")] public string Formato { get; set; }
            [JsonProperty("versao")] public int Versao { get; set; } = 1;
            [JsonProperty("exportado_em")] public string ExportadoEm { get; set; }
            [JsonProperty("origem")] public string Origem { get; set; }
            [JsonProperty("formacao")] public string Formacao { get; set; }
            [JsonProperty("npcs")] public string Npcs { get; set; }
            [JsonProperty("trama")] public string Trama { get; set; }
        }

        /// <summary>As três anotações do mestre da sala num JSON próprio (para revisar fora ou levar a outra campanha).</summary>
        public string ExportarAnotacoes(string salaId)
        {
            var sala = Buscar(salaId);
            if (sala == null)
                return null;
            return JsonConvert.SerializeObject(new EnvelopeAnotacoes
            {
                Formato = FormatoDeTrocaAnotacoes,
                ExportadoEm = DateTime.Now.ToString("s"),
                Origem = "Campanha " + sala.Nome,
                Formacao = sala.AnotacoesFormacao ?? string.Empty,
                Npcs = sala.AnotacoesNpcs ?? string.Empty,
                Trama = sala.Trama ?? string.Empty,
            }, Formatting.Indented);
        }

        /// <summary>
        /// Traz as anotações de um JSON de troca por cima das desta sala. Campo ausente no arquivo (null)
        /// fica como está; presente (mesmo vazio) substitui.
        /// </summary>
        public Resultado ImportarAnotacoes(string salaId, string json)
        {
            var sala = Buscar(salaId);
            if (sala == null)
                return Resultado.Falha("Sala não encontrada.");
            EnvelopeAnotacoes lido;
            try
            {
                lido = JsonConvert.DeserializeObject<EnvelopeAnotacoes>(json ?? string.Empty);
            }
            catch (JsonException)
            {
                lido = null;
            }
            if (lido?.Formato != FormatoDeTrocaAnotacoes)
                return Resultado.Falha("Isso não é um JSON de anotações da Torre.");
            if (lido.Versao > 1)
                return Resultado.Falha("Exportado por uma Torre mais nova: atualize esta antes de importar.");
            if (lido.Formacao != null) sala.AnotacoesFormacao = lido.Formacao.Trim();
            if (lido.Npcs != null) sala.AnotacoesNpcs = lido.Npcs.Trim();
            if (lido.Trama != null) sala.Trama = lido.Trama.Trim();
            Gravar();
            return Resultado.Sucesso();
        }

        public static string LerAnotacao(Sala sala, Anotacao qual) => qual switch
        {
            Anotacao.Formacao => sala.AnotacoesFormacao,
            Anotacao.Npcs => sala.AnotacoesNpcs,
            _ => sala.Trama,
        } ?? string.Empty;

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
