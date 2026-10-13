using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TorreDeControle.Core.Armazenamento;
using TorreDeControle.Core.Personagens;

namespace TorreDeControle.Core.Salas
{
    /// <summary>
    /// A mesa de uma sala: a rodada corrente e o log de eventos. Cada comando do mestre (dar potencial,
    /// marcar condição, mudar Rótulos) valida pela regra, muda a ficha da instância e vira um evento
    /// com rodada e motivo. Corrigir não apaga: lança o inverso.
    /// Grava em "mesa_&lt;sala&gt;" (separado de salas.json, que fica pequeno).
    /// </summary>
    public sealed class Mesa
    {
        const int VersaoArquivo = 1;

        public const string CampoPotencial = "potencial";
        public static string CampoCondicao(string condicao) => "condicao:" + condicao;
        public static string CampoRotulo(string rotulo) => "rotulo:" + rotulo;

        readonly IArmazenamento armazenamento;
        readonly CadastroPersonagens personagens;
        readonly string sala;
        readonly Arquivo arquivo;

        Mesa(IArmazenamento armazenamento, CadastroPersonagens personagens, string sala, Arquivo arquivo)
        {
            this.armazenamento = armazenamento;
            this.personagens = personagens;
            this.sala = sala;
            this.arquivo = arquivo;
        }

        static string Chave(string sala) => "mesa_" + sala;

        public static Mesa Carregar(IArmazenamento armazenamento, CadastroPersonagens personagens, string sala)
        {
            var texto = armazenamento.Ler(Chave(sala));
            var arquivo = string.IsNullOrWhiteSpace(texto)
                ? new Arquivo()
                : JsonConvert.DeserializeObject<Arquivo>(texto) ?? new Arquivo();
            arquivo.Eventos ??= new List<Evento>();
            if (arquivo.Rodada < 1)
                arquivo.Rodada = 1; // sempre há uma rodada corrente
            return new Mesa(armazenamento, personagens, sala, arquivo);
        }

        public int Rodada => arquivo.Rodada;

        /// <summary>Do mais antigo ao mais novo.</summary>
        public IReadOnlyList<Evento> Eventos => arquivo.Eventos;

        public bool Corrigido(Evento e) => arquivo.Eventos.Any(x => x.Corrige == e.Id);

        /// <summary>Fecha a rodada corrente e abre a próxima.</summary>
        public void NovaRodada()
        {
            arquivo.Rodada++;
            Gravar();
        }

        // ---------- comandos ----------

        /// <summary>Soma (ou tira) potencial. Não fica negativo nem desfaz potencial que já virou avanço.</summary>
        public Resultado DarPotencial(string personagem, int delta, string motivo)
        {
            if (!EmJogo(personagem, out var p, out var erro))
                return erro;
            var novo = p.Potencial + delta;
            if (novo < 0)
                return Resultado.Falha("Potencial não fica negativo.");
            if (novo / CadastroPersonagens.PotencialPorAvanco < p.AvancosMarcados.Count)
                return Resultado.Falha("Esse potencial já virou avanço marcado.");
            var texto = delta >= 0 ? $"+{delta} potencial" : $"{delta} potencial";
            return Registrar(p, texto, motivo, new Mudanca { Campo = CampoPotencial, Antes = p.Potencial, Depois = novo });
        }

        /// <summary>
        /// Marca ou desmarca uma condição. Marcar com todas já marcadas não muda nada: a personagem
        /// sai de cena (o resultado diz isso).
        /// </summary>
        public Resultado AlternarCondicao(string personagem, string condicao, string motivo)
        {
            if (!EmJogo(personagem, out var p, out var erro))
                return erro;
            if (!Condicoes.Todas.Contains(condicao))
                return Resultado.Falha("Condição desconhecida.");
            var marcada = p.CondicoesMarcadas.Contains(condicao);
            var texto = (marcada ? "desmarcou " : "marcou ") + Condicoes.Nome(condicao);
            return Registrar(p, texto, motivo,
                new Mudanca { Campo = CampoCondicao(condicao), Antes = marcada ? 1 : 0, Depois = marcada ? 0 : 1 });
        }

        /// <summary>
        /// Mudar Rótulos: <paramref name="sobe"/> +1 e <paramref name="desce"/> −1 (livro p.46). Se algum
        /// sairia de [−2, +3], nenhum muda e a personagem marca uma condição, que o mestre escolhe:
        /// sem <paramref name="condicaoSeEstourar"/>, o resultado pede a escolha e nada é feito.
        /// </summary>
        public Resultado MudarRotulos(string personagem, string sobe, string desce, string motivo,
            string condicaoSeEstourar = null)
        {
            if (!EmJogo(personagem, out var p, out var erro))
                return erro;
            if (!Rotulos.Todos.Contains(sobe) || !Rotulos.Todos.Contains(desce))
                return Resultado.Falha("Rótulo desconhecido.");
            if (sobe == desce)
                return Resultado.Falha("Um Rótulo sobe e outro desce: escolha dois diferentes.");

            var atuais = personagens.RotulosAtuais(p);
            var estoura = atuais[sobe] + 1 > Rotulos.Maximo || atuais[desce] - 1 < Rotulos.Minimo;
            if (!estoura)
                return Registrar(p, $"{Rotulos.Nome(sobe)} +1, {Rotulos.Nome(desce)} −1", motivo,
                    new Mudanca { Campo = CampoRotulo(sobe), Antes = atuais[sobe], Depois = atuais[sobe] + 1 },
                    new Mudanca { Campo = CampoRotulo(desce), Antes = atuais[desce], Depois = atuais[desce] - 1 });

            if (condicaoSeEstourar == null)
                return Resultado.Falha(MensagemDeEstouro);
            if (!Condicoes.Todas.Contains(condicaoSeEstourar))
                return Resultado.Falha("Condição desconhecida.");
            if (p.CondicoesMarcadas.Contains(condicaoSeEstourar))
                return Resultado.Falha("Essa condição já está marcada: escolha outra.");
            return Registrar(p,
                $"{Rotulos.Nome(sobe)} +1 / {Rotulos.Nome(desce)} −1 estourou o limite: nenhum Rótulo muda; marcou {Condicoes.Nome(condicaoSeEstourar)}",
                motivo, new Mudanca { Campo = CampoCondicao(condicaoSeEstourar), Antes = 0, Depois = 1 });
        }

        /// <summary>A tela reconhece este texto para pedir a condição do estouro.</summary>
        public const string MensagemDeEstouro = "Estoura o limite dos Rótulos: nenhum muda e a personagem marca uma condição. Qual?";

        /// <summary>
        /// Desfaz um evento lançando o inverso (o original fica no log, marcado como corrigido).
        /// É ajuste do mestre: aplica o inverso do que mudou, sem as regras do comando.
        /// </summary>
        public Resultado Corrigir(string eventoId, string motivo)
        {
            var original = arquivo.Eventos.FirstOrDefault(e => e.Id == eventoId);
            if (original == null)
                return Resultado.Falha("Evento não encontrado.");
            if (original.Corrige != null)
                return Resultado.Falha("Uma correção não se corrige: lance o comando de novo.");
            if (Corrigido(original))
                return Resultado.Falha("Esse evento já foi corrigido.");
            var p = personagens.Buscar(original.Personagem);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");

            var inversas = new List<Mudanca>();
            foreach (var m in original.Mudancas)
            {
                var agora = personagens.ValorDeJogo(p, m.Campo);
                var depois = Limitar(m.Campo, agora - (m.Depois - m.Antes));
                if (depois != agora)
                    inversas.Add(new Mudanca { Campo = m.Campo, Antes = agora, Depois = depois });
            }
            var evento = NovoEvento(p, "corrigiu: " + original.Descricao, motivo);
            evento.Corrige = original.Id;
            evento.Mudancas = inversas;
            personagens.AplicarDeJogo(p, inversas);
            arquivo.Eventos.Add(evento);
            Gravar();
            return Resultado.Sucesso();
        }

        // ---------- por dentro ----------

        /// <summary>Só as instâncias desta sala, em carreira e numa vaga, recebem comando de jogo.</summary>
        bool EmJogo(string id, out Personagem p, out Resultado erro)
        {
            p = personagens.Buscar(id);
            erro = Resultado.Sucesso();
            if (p == null || p.Sala != sala)
                erro = Resultado.Falha("Personagem fora desta sala.");
            else if (!p.Ativo)
                erro = Resultado.Falha("Ficha do arquivo da sala: só consulta.");
            else if (p.Status != StatusCriacao.EmCarreira)
                erro = Resultado.Falha("Ficha em revisão (novo): conclua a criação antes de jogar.");
            return erro.Ok;
        }

        Resultado Registrar(Personagem p, string descricao, string motivo, params Mudanca[] mudancas)
        {
            var evento = NovoEvento(p, descricao, motivo);
            evento.Mudancas = mudancas.ToList();
            personagens.AplicarDeJogo(p, evento.Mudancas);
            arquivo.Eventos.Add(evento);
            Gravar();
            return Resultado.Sucesso();
        }

        Evento NovoEvento(Personagem p, string descricao, string motivo) => new Evento
        {
            Id = Guid.NewGuid().ToString("N"),
            Rodada = arquivo.Rodada,
            Quando = DateTime.Now.ToString("s"),
            Personagem = p.Id,
            Descricao = descricao,
            Motivo = (motivo ?? string.Empty).Trim(),
        };

        static int Limitar(string campo, int valor) =>
            campo.StartsWith("condicao:") ? Math.Max(0, Math.Min(1, valor))
            : campo == CampoPotencial ? Math.Max(0, valor)
            : valor;

        void Gravar()
        {
            arquivo.Versao = VersaoArquivo;
            armazenamento.Gravar(Chave(sala), JsonConvert.SerializeObject(arquivo, Formatting.Indented));
        }

        sealed class Arquivo
        {
            [JsonProperty("versao")] public int Versao { get; set; } = VersaoArquivo;
            [JsonProperty("rodada")] public int Rodada { get; set; } = 1;
            [JsonProperty("eventos")] public List<Evento> Eventos { get; set; } = new List<Evento>();
        }
    }
}
