using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// Exportar e importar uma personagem em JSON, para levar a outra máquina (outra Torre, ou uma
    /// ferramenta que analise a ficha). O arquivo é um envelope com formato e versão em volta da
    /// personagem como a Torre a grava.
    /// </summary>
    public sealed partial class CadastroPersonagens
    {
        public const string FormatoDeTroca = "torre-de-controle/personagem";
        const int VersaoDeTroca = 1;

        sealed class Envelope
        {
            [JsonProperty("formato")] public string Formato { get; set; }
            [JsonProperty("versao")] public int Versao { get; set; }
            [JsonProperty("exportado_em")] public string ExportadoEm { get; set; }
            /// <summary>Para quem lê o arquivo fora da Torre: o arquétipo por extenso.</summary>
            [JsonProperty("arquetipo_nome")] public string ArquetipoNome { get; set; }
            [JsonProperty("personagem")] public Personagem Personagem { get; set; }
        }

        /// <summary>O JSON de troca da personagem (em modo teste, a real, não o que o teste mexeu).</summary>
        public string Exportar(string id)
        {
            var p = Buscar(id);
            if (p == null)
                return null;
            var real = emTeste.TryGetValue(id, out var guardada) ? JsonConvert.DeserializeObject<Personagem>(guardada) : p;
            var envelope = new Envelope
            {
                Formato = FormatoDeTroca,
                Versao = VersaoDeTroca,
                ExportadoEm = DateTime.Now.ToString("s"),
                ArquetipoNome = Arquetipos.Nome(real.Foto?.Arquetipo ?? real.Identidade.Arquetipo),
                Personagem = real,
            };
            return JsonConvert.SerializeObject(envelope, Formatting.Indented);
        }

        /// <summary>
        /// Traz uma personagem exportada para o laboratório, como personagem nova (id novo, sem sala).
        /// O que era da campanha de lá (sala, colegas, Influência) não vem; quem já tinha a criação
        /// concluída chega "em aprovação", e o jogo marcado vem junto.
        /// </summary>
        public Resultado Importar(string json, out Personagem nova)
        {
            nova = null;
            var lido = LerTroca(json, out var p);
            if (!lido.Ok)
                return lido;
            if (p.Controle != Controle.Jogador)
                return Resultado.Falha("Importar NPC ainda não: NPCs nascem na sala.");
            if (Arquetipos.Buscar(p.Identidade.Arquetipo) == null)
                return Resultado.Falha("Arquétipo desconhecido nesta Torre.");

            p.Id = Guid.NewGuid().ToString("N");
            p.Sala = null;
            p.Origem = null;
            p.Ativo = true;
            p.Ordem = Ordenacao.Fim(personagens);
            // A campanha de lá não vem: colegas e Influência apontam para personagens de outra Torre.
            p.Relacionamentos = new List<string>();
            p.Influencias = new Dictionary<string, EstadoInfluencia>();
            p.InfluenciaPorque = new Dictionary<string, string>();
            p.InfluenciaPorqueNinguem = string.Empty;
            p.InfluenciaComportamento = null;
            p.EquipeResposta = string.Empty;
            p.Status = p.Foto != null ? StatusCriacao.EmAprovacao : StatusCriacao.Novo;
            // O jogador é um registro desta Torre: o de lá não existe aqui.
            p.Identidade.Jogador = null;

            personagens.Add(p);
            Gravar();
            nova = p;
            return Resultado.Sucesso();
        }

        /// <summary>O id da personagem dentro de um JSON de troca (null se não for um).</summary>
        public string IdNaTroca(string json) => LerTroca(json, out var p).Ok ? p.Id : null;

        /// <summary>
        /// O caminho de volta: atualiza uma ficha existente com um JSON exportado (ajustado em outra
        /// máquina). Vem do arquivo a ficha em si (identidade, criação, descrições, observações, jogo);
        /// fica o que é desta Torre: id, sala, origem, ativo, ordem, status do ciclo e jogador. Se o JSON
        /// for de outra personagem, também ficam a equipe e a Influência daqui (a tela pergunta antes).
        /// </summary>
        public Resultado Reimportar(string id, string json)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (EmTeste(id))
                return Resultado.Falha("Desligue o modo teste antes de importar por cima.");
            var lido = LerTroca(json, out var lida);
            if (!lido.Ok)
                return lido;
            if (lida.Controle != p.Controle)
                return Resultado.Falha("O arquivo é de outro tipo de personagem (herói × NPC).");
            if (Arquetipos.Buscar(lida.Identidade.Arquetipo) == null)
                return Resultado.Falha("Arquétipo desconhecido nesta Torre.");
            // A foto da criação segue o ciclo daqui: em criação não tem; concluída precisa ter.
            if (p.EmCriacao)
                lida.Foto = null;
            else if (lida.Foto == null)
                return Resultado.Falha("O arquivo não tem a criação concluída, e esta ficha tem: não dá para importar por cima.");

            var mesma = lida.Id == p.Id;
            var (sala, origem, ativo, ordem, status, jogador) =
                (p.Sala, p.Origem, p.Ativo, p.Ordem, p.Status, p.Identidade.Jogador);
            var (relacionamentos, influencia, comportamento, equipe) =
                (p.Relacionamentos, p.Influencias, p.InfluenciaComportamento, p.EquipeResposta);

            JsonConvert.PopulateObject(JsonConvert.SerializeObject(lida), p, Restaurar);
            Normalizar(p);
            p.Id = id;
            (p.Sala, p.Origem, p.Ativo, p.Ordem, p.Status) = (sala, origem, ativo, ordem, status);
            p.Identidade.Jogador = jogador;
            if (!mesma)
                (p.Relacionamentos, p.Influencias, p.InfluenciaComportamento, p.EquipeResposta) =
                    (relacionamentos, influencia, comportamento, equipe);
            Gravar();
            return Resultado.Sucesso();
        }

        Resultado LerTroca(string json, out Personagem personagem)
        {
            personagem = null;
            Envelope envelope;
            try
            {
                envelope = JsonConvert.DeserializeObject<Envelope>(json ?? string.Empty);
            }
            catch (JsonException)
            {
                return Resultado.Falha("Isso não é um JSON de personagem da Torre.");
            }
            if (envelope?.Formato != FormatoDeTroca || envelope.Personagem == null)
                return Resultado.Falha("Isso não é um JSON de personagem da Torre.");
            if (envelope.Versao > VersaoDeTroca)
                return Resultado.Falha("Exportado por uma Torre mais nova: atualize esta antes de importar.");
            personagem = envelope.Personagem;
            Normalizar(personagem);
            return Resultado.Sucesso();
        }
    }
}
