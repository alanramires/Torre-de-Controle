using System.Collections.Generic;
using System.Linq;
using TorreDeControle.Core.Regras;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// Segunda etapa da criação, na campanha (instância em aprovação): "quando a equipe se reuniu",
    /// os relacionamentos com os colegas da sala e a Influência inicial pela regra do arquétipo.
    /// O mestre só aprova para a campanha sem pendências desta etapa.
    /// </summary>
    public sealed partial class CadastroPersonagens
    {
        /// <summary>Os outros heróis da sala, nas vagas (instâncias ativas de controle do jogador).</summary>
        public IReadOnlyList<Personagem> Colegas(Personagem p) =>
            p.Sala == null
                ? new List<Personagem>()
                : personagens.Where(x => x.Sala == p.Sala && x.Ativo && x.Controle == Controle.Jogador && x.Id != p.Id)
                    .ToList();

        /// <summary>A parte da equipe só se preenche na campanha, enquanto em aprovação.</summary>
        public bool EquipeEditavel(Personagem p) =>
            p.Sala != null && p.Ativo && p.Status == StatusCriacao.EmAprovacao;

        public Resultado ResponderEquipe(string id, string resposta)
        {
            if (!NaEquipe(id, out var p, out var erro))
                return erro;
            var limpa = (resposta ?? string.Empty).Trim();
            if (limpa == p.EquipeResposta)
                return Resultado.Sucesso();
            p.EquipeResposta = limpa;
            SincronizarNpcsDaFicha(p);
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// Na criação (em aprovação, na sala): o colega da frase de número <paramref name="indice"/> (null tira).
        /// Trocar aqui é escolher, não histórico. O mesmo colega pode estar nas duas frases (não é recomendado:
        /// vira observação).
        /// </summary>
        public Resultado DefinirRelacionamento(string id, int indice, string colega)
        {
            if (!NaEquipe(id, out var p, out var erro))
                return erro;
            if (!FraseValida(p, indice))
                return Resultado.Falha("Relacionamento desconhecido.");
            colega = string.IsNullOrEmpty(colega) ? null : colega;
            if (colega != null && OpcoesDeRelacionamento(p).All(c => c.Id != colega))
                return Resultado.Falha(RelacionamentoVemDaInfluencia(p)
                    ? "O amor e o rival saem de quem recebeu a Influência inicial."
                    : "Escolha um colega desta sala.");
            var influenciaJaFeita = RelacionamentoVemDaInfluencia(p) && InfluenciaConfirmada(p);
            Relacao(p, indice).Colega = colega;
            // Bruta, amor e rival escolhidos antes da Influência: eles viram a Influência dela.
            if (RelacionamentoVemDaInfluencia(p) && !influenciaJaFeita)
            {
                RetirarDadas(p);
                foreach (var c in p.Relacoes.Select(r => r.Colega).Where(c => c != null).Distinct())
                    p.Influencias[c] = EstadoInfluencia.Dada;
            }
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// No jogo (o painel do mestre): troca o colega da frase (null = perdeu). Quem estava vai para os
        /// anteriores: nada sai da ficha. Só personagem em carreira.
        /// </summary>
        public Resultado TrocarRelacionamento(string id, int indice, string colega)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (p.Status != StatusCriacao.EmCarreira)
                return Resultado.Falha("Trocar ou perder relacionamento é do jogo (personagem em carreira).");
            if (!FraseValida(p, indice))
                return Resultado.Falha("Relacionamento desconhecido.");
            colega = string.IsNullOrEmpty(colega) ? null : colega;
            if (colega != null && Colegas(p).All(c => c.Id != colega))
                return Resultado.Falha("Escolha um colega desta sala.");
            var r = Relacao(p, indice);
            if (r.Colega == colega)
                return Resultado.Sucesso();
            if (r.Colega != null)
                r.Anteriores.Add(r.Colega);
            r.Colega = colega;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>O porquê de um relacionamento (opcional, roleplay). Anotação: editável sempre; a Torre não apaga.</summary>
        public Resultado DescreverRelacionamento(string id, int indice, string porque)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (!FraseValida(p, indice))
                return Resultado.Falha("Relacionamento desconhecido.");
            var limpo = (porque ?? string.Empty).Trim();
            var r = Relacao(p, indice);
            if (r.Porque == limpo)
                return Resultado.Sucesso();
            r.Porque = limpo;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// A Bruta (Influência "amor e rival"): na criação, o amor e o rival saem de quem recebeu a Influência
        /// inicial dela (Alan, 2026-10-14).
        /// </summary>
        public bool RelacionamentoVemDaInfluencia(Personagem p) =>
            RegraDoArquetipo(p)?.Influencia?.Regra == RegrasArquetipos.RegraInfluencia.AmorERival;

        /// <summary>
        /// Os colegas que podem ocupar uma frase: todos da sala; a Bruta, com a Influência já feita, só os que a
        /// receberam (feita a Influência antes, ela filtra; escolhidos amor e rival antes, eles viram a Influência).
        /// </summary>
        public IReadOnlyList<Personagem> OpcoesDeRelacionamento(Personagem p)
        {
            var colegas = Colegas(p);
            if (!RelacionamentoVemDaInfluencia(p) || !InfluenciaConfirmada(p))
                return colegas;
            var dados = InfluenciaDada(p);
            return colegas.Where(c => dados.Contains(c.Id)).ToList();
        }

        bool FraseValida(Personagem p, int indice) =>
            indice >= 0 && indice < (RegraDoArquetipo(p)?.Equipe.Relacionamentos.Count ?? 0);

        /// <summary>A relação da frase (cria as vagas que faltarem).</summary>
        public Relacao Relacao(Personagem p, int indice)
        {
            while (p.Relacoes.Count <= indice)
                p.Relacoes.Add(new Relacao());
            return p.Relacoes[indice];
        }

        /// <summary>
        /// A leitura reversa: em quais frases dos colegas esta personagem aparece agora (colega, índice da frase).
        /// </summary>
        public IReadOnlyList<(Personagem colega, int frase)> CitacoesEmRelacionamentos(Personagem p)
        {
            var citacoes = new List<(Personagem, int)>();
            foreach (var c in Colegas(p))
                for (var k = 0; k < c.Relacoes.Count; k++)
                    if (c.Relacoes[k].Colega == p.Id)
                        citacoes.Add((c, k));
            return citacoes;
        }

        /// <summary>
        /// Comportamento da Influência (Discípula, Forasteira, Nova), que diz a quantos colegas dar.
        /// Trocar de comportamento desfaz a Influência dada (a quantidade mudou).
        /// </summary>
        public Resultado EscolherComportamento(string id, string comportamento)
        {
            if (!NaEquipe(id, out var p, out var erro))
                return erro;
            var regra = RegraDoArquetipo(p)?.Influencia;
            if (regra?.Regra != RegrasArquetipos.RegraInfluencia.Comportamento ||
                (comportamento != null && regra.Opcoes.All(o => o.Id != comportamento)))
                return Resultado.Falha("Comportamento desconhecido.");
            if (p.InfluenciaComportamento == comportamento)
                return Resultado.Sucesso();
            p.InfluenciaComportamento = comportamento;
            RetirarDadas(p);
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// A regra pede "todos os colegas"? (regra todos, ou o comportamento escolhido diz "todos").
        /// Mesmo assim o jogador confirma (o botão "Dar Influência a todos").
        /// </summary>
        public bool InfluenciaParaTodos(Personagem p)
        {
            var regra = RegraDoArquetipo(p)?.Influencia;
            return regra?.Regra == RegrasArquetipos.RegraInfluencia.Todos ||
                   (regra?.Regra == RegrasArquetipos.RegraInfluencia.Comportamento &&
                    regra.Opcoes.FirstOrDefault(o => o.Id == p.InfluenciaComportamento)?.ParaTodos == true);
        }

        /// <summary>
        /// A quantos colegas dar Influência, pela regra (nunca mais que os colegas da sala). Null: ainda
        /// não dá para saber (comportamento não escolhido). Amor e rival: 2 (até os relacionamentos ligarem).
        /// </summary>
        public int? QuantasInfluencias(Personagem p)
        {
            var regra = RegraDoArquetipo(p)?.Influencia;
            if (regra == null)
                return null;
            var colegas = Colegas(p).Count;
            if (InfluenciaParaTodos(p))
                return colegas;
            int? n = regra.Regra switch
            {
                RegrasArquetipos.RegraInfluencia.Fixo => regra.QuantidadeNumero,
                RegrasArquetipos.RegraInfluencia.AmorERival => 2,
                RegrasArquetipos.RegraInfluencia.Comportamento =>
                    regra.Opcoes.FirstOrDefault(o => o.Id == p.InfluenciaComportamento)?.QuantidadeNumero,
                _ => null,
            };
            return n.HasValue ? System.Math.Min(n.Value, colegas) : (int?)null;
        }

        /// <summary>
        /// Confirma a quem a personagem dá Influência (a lista inteira de uma vez). Precisa bater com a regra:
        /// exatamente a quantidade do arquétipo; "todos" é todos os colegas.
        /// </summary>
        public Resultado DarInfluencia(string id, IEnumerable<string> colegas)
        {
            if (!NaEquipe(id, out var p, out var erro))
                return erro;
            var quantas = QuantasInfluencias(p);
            if (quantas == null)
                return Resultado.Falha("Escolha o comportamento antes de dar Influência.");
            var daSala = Colegas(p).Select(c => c.Id).ToList();
            var escolhidos = (colegas ?? Enumerable.Empty<string>()).Distinct().ToList();
            if (escolhidos.Any(c => !daSala.Contains(c)))
                return Resultado.Falha("Escolha colegas desta sala.");
            if (escolhidos.Count != quantas.Value)
                return Resultado.Falha(quantas.Value == 0
                    ? "Pela regra, não dá Influência a ninguém."
                    : $"Pela regra, Influência a {quantas.Value} colega{(quantas.Value == 1 ? "" : "s")} (marcou {escolhidos.Count}).");
            // Na criação, a escolha substitui a anterior (quem foi desmarcado nunca valeu; o porquê fica guardado).
            RetirarDadas(p);
            foreach (var c in escolhidos)
                p.Influencias[c] = EstadoInfluencia.Dada;
            // Bruta: amor ou rival que apontava para quem saiu da Influência fica vazio (ajuste nos dois sentidos).
            if (RelacionamentoVemDaInfluencia(p))
                foreach (var r in p.Relacoes.Where(r => r.Colega != null && !escolhidos.Contains(r.Colega)))
                    r.Colega = null;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Refazer a Influência inicial mexe só nos colegas: a Influência com NPCs é do jogo.</summary>
        void RetirarDadas(Personagem p)
        {
            foreach (var c in p.Influencias.Where(kv => kv.Value == EstadoInfluencia.Dada).Select(kv => kv.Key).ToList())
                if (Buscar(c)?.Controle != Controle.Mestre)
                    p.Influencias.Remove(c);
        }

        /// <summary>
        /// No jogo (o painel do mestre): dar Influência a um colega (nova, ou de volta) ou perdê-la. Nada sai da
        /// ficha: quem perdeu fica "perdida", o porquê fica. Só personagem em carreira; dar só a colegas da sala,
        /// perder só o que ela tinha dado. O histórico com rodada e motivo virá do log da mesa.
        /// </summary>
        public Resultado DefinirEstadoDaInfluencia(string id, string colega, EstadoInfluencia estado)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (p.Status != StatusCriacao.EmCarreira)
                return Resultado.Falha("Dar e perder Influência no jogo é com a personagem em carreira.");
            if (estado == EstadoInfluencia.Perdida && !p.Influencias.ContainsKey(colega))
                return Resultado.Falha("Ela nunca deu Influência a esse colega: não há o que perder.");
            if (estado == EstadoInfluencia.Dada && Colegas(p).All(c => c.Id != colega))
                return Resultado.Falha("Escolha um colega desta sala.");
            p.Influencias[colega] = estado;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// Por que o colega tem Influência sobre a personagem (opcional, roleplay). Só para quem ela deu
        /// Influência; vazio tira. Anotação: vale em qualquer status, como as observações dos movimentos.
        /// </summary>
        public Resultado DescreverInfluencia(string id, string colega, string porque)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (!p.Influencias.ContainsKey(colega))
                return Resultado.Falha("Ela não deu Influência a esse colega.");
            var limpo = (porque ?? string.Empty).Trim();
            if ((p.InfluenciaPorque.TryGetValue(colega, out var atual) ? atual : string.Empty) == limpo)
                return Resultado.Sucesso();
            if (limpo.Length == 0)
                p.InfluenciaPorque.Remove(colega);
            else
                p.InfluenciaPorque[colega] = limpo;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// Por que ninguém tem Influência sobre ela (comportamento profissional, arrogante...): opcional,
        /// roleplay. Anotação: editável sempre; a Torre não apaga.
        /// </summary>
        public Resultado DescreverInfluenciaDeNinguem(string id, string porque)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            var limpo = (porque ?? string.Empty).Trim();
            if (limpo == p.InfluenciaPorqueNinguem)
                return Resultado.Sucesso();
            p.InfluenciaPorqueNinguem = limpo;
            Gravar();
            return Resultado.Sucesso();
        }

        public string PorqueDaInfluencia(Personagem p, string colega) =>
            p.InfluenciaPorque.TryGetValue(colega, out var t) ? t : string.Empty;

        /// <summary>Os colegas da sala com o estado dado (dada ou perdida), na ficha desta personagem.</summary>
        IReadOnlyList<string> ComEstado(Personagem p, EstadoInfluencia estado)
        {
            var colegas = Colegas(p).Select(c => c.Id).ToList();
            return p.Influencias.Where(kv => kv.Value == estado && colegas.Contains(kv.Key)).Select(kv => kv.Key).ToList();
        }

        /// <summary>A quem a personagem deu Influência que vale agora (dada; só colegas ainda na sala).</summary>
        public IReadOnlyList<string> InfluenciaDada(Personagem p) => ComEstado(p, EstadoInfluencia.Dada);

        /// <summary>A quem ela deu Influência e, no jogo, perdeu (fica na ficha; pode voltar).</summary>
        public IReadOnlyList<string> InfluenciaPerdida(Personagem p) => ComEstado(p, EstadoInfluencia.Perdida);

        /// <summary>
        /// A leitura reversa, pelo estado nas fichas dos colegas: quem deu Influência (dada) a esta personagem,
        /// ou seja, sobre quem ELA exerce Influência (essas pessoas se importam com o que ela diz).
        /// </summary>
        public IReadOnlyList<Personagem> QuemDeuInfluencia(Personagem p) =>
            Colegas(p).Where(c => c.Influencias.TryGetValue(p.Id, out var e) && e == EstadoInfluencia.Dada).ToList();

        /// <summary>A leitura reversa das perdidas: quem já deu Influência a ela e, no jogo, a tirou.</summary>
        public IReadOnlyList<Personagem> QuemPerdeuInfluencia(Personagem p) =>
            Colegas(p).Where(c => c.Influencias.TryGetValue(p.Id, out var e) && e == EstadoInfluencia.Perdida).ToList();

        /// <summary>A personagem já confirmou a Influência pela regra?</summary>
        public bool InfluenciaConfirmada(Personagem p)
        {
            var quantas = QuantasInfluencias(p);
            return quantas.HasValue && InfluenciaDada(p).Count == quantas.Value &&
                   (quantas.Value > 0 || p.InfluenciaComportamento != null ||
                    RegraDoArquetipo(p)?.Influencia?.Regra != RegrasArquetipos.RegraInfluencia.Comportamento);
        }

        /// <summary>O colega de agora de um relacionamento (o "___" da frase), se ainda está na sala; senão null.</summary>
        public Personagem ColegaDoRelacionamento(Personagem p, int indice) =>
            indice < p.Relacoes.Count && p.Relacoes[indice].Colega != null
                ? Colegas(p).FirstOrDefault(c => c.Id == p.Relacoes[indice].Colega)
                : null;

        /// <summary>Observações da equipe (não travam): o mesmo colega nas duas frases.</summary>
        public IReadOnlyList<string> ObservacoesDaEquipe(Personagem p)
        {
            var obs = new List<string>();
            var repetidos = p.Relacoes.Where(r => r.Colega != null).GroupBy(r => r.Colega).Where(g => g.Count() > 1);
            foreach (var g in repetidos)
                obs.Add($"Relacionamentos: {Buscar(g.Key)?.NomeExibicao ?? "?"} está em mais de uma frase (permitido, mas não recomendado).");
            return obs;
        }

        /// <summary>O que falta na segunda etapa (na campanha). Vazio = o mestre pode aprovar.</summary>
        public IReadOnlyList<string> PendenciasDaEquipe(Personagem p)
        {
            var pendencias = new List<string>();
            var regra = RegraDoArquetipo(p);
            if (regra == null)
                return pendencias;

            if (!string.IsNullOrEmpty(regra.Equipe.Pergunta) && string.IsNullOrWhiteSpace(p.EquipeResposta))
                pendencias.Add("Responda \"quando a equipe se reuniu\".");

            var colegas = Colegas(p);
            if (regra.Equipe.Relacionamentos.Count > 0)
            {
                if (colegas.Count == 0)
                    pendencias.Add("Relacionamentos: precisa de colegas nas vagas da sala.");
                else
                {
                    var faltam = Enumerable.Range(0, regra.Equipe.Relacionamentos.Count)
                        .Count(k => ColegaDoRelacionamento(p, k) == null);
                    if (faltam > 0)
                        pendencias.Add($"Relacionamentos: escolha o colega (faltam {faltam}).");
                }
            }

            var influencia = regra.Influencia;
            if (influencia?.Regra == RegrasArquetipos.RegraInfluencia.Comportamento && p.InfluenciaComportamento == null)
                pendencias.Add("Influência: escolha o comportamento.");
            else if (QuantasInfluencias(p) is int quantas && InfluenciaDada(p).Count != quantas)
                pendencias.Add($"Influência: dê a {quantas} colega{(quantas == 1 ? "" : "s")} (deu a {InfluenciaDada(p).Count}).");
            return pendencias;
        }

        /// <summary>
        /// O que falta para a personagem subir de em aprovação a em carreira (trava o "Aprovar para a
        /// campanha"). Hoje: a formação da equipe enviada, os relacionamentos escolhidos e a Influência
        /// inicial distribuída pela regra. Os NPCs de cada um entram aqui quando forem feitos.
        /// </summary>
        public IReadOnlyList<string> PendenciasParaCarreira(Personagem p)
        {
            var pendencias = new List<string>();
            if (string.IsNullOrWhiteSpace(p.EquipeResposta))
                pendencias.Add("Formação da equipe: envie a resposta.");
            var frases = RegraDoArquetipo(p)?.Equipe.Relacionamentos.Count ?? 0;
            if (frases > 0)
            {
                if (Colegas(p).Count == 0)
                    pendencias.Add("Relacionamentos: precisa de colegas nas vagas da sala.");
                else
                {
                    var faltam = Enumerable.Range(0, frases).Count(k => ColegaDoRelacionamento(p, k) == null);
                    if (faltam > 0)
                        pendencias.Add($"Relacionamentos: escolha o colega (faltam {faltam}).");
                }
            }
            var influencia = RegraDoArquetipo(p)?.Influencia;
            if (influencia?.Regra == RegrasArquetipos.RegraInfluencia.Comportamento && p.InfluenciaComportamento == null)
                pendencias.Add("Influência: escolha o comportamento.");
            else if (!InfluenciaConfirmada(p))
                pendencias.Add($"Influência: dê a {QuantasInfluencias(p)} colega(s) (deu a {InfluenciaDada(p).Count}).");
            return pendencias;
        }

        bool NaEquipe(string id, out Personagem p, out Resultado erro)
        {
            p = Buscar(id);
            erro = Resultado.Sucesso();
            if (p == null)
                erro = Resultado.Falha("Personagem não encontrada.");
            else if (!EquipeEditavel(p))
                erro = Resultado.Falha("A parte da equipe se preenche na campanha, com a ficha em aprovação.");
            return erro.Ok;
        }
    }
}
