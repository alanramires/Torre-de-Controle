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
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>O colega do relacionamento de número <paramref name="indice"/> (null tira).</summary>
        public Resultado DefinirRelacionamento(string id, int indice, string colega)
        {
            if (!NaEquipe(id, out var p, out var erro))
                return erro;
            var frases = RegraDoArquetipo(p)?.Equipe.Relacionamentos ?? new List<string>();
            if (indice < 0 || indice >= frases.Count)
                return Resultado.Falha("Relacionamento desconhecido.");
            colega = string.IsNullOrEmpty(colega) ? null : colega;
            if (colega != null && Colegas(p).All(c => c.Id != colega))
                return Resultado.Falha("Escolha um colega desta sala.");
            while (p.Relacionamentos.Count < frases.Count)
                p.Relacionamentos.Add(null);
            p.Relacionamentos[indice] = colega;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Comportamento da Influência (Discípula, Forasteira, Nova), que diz a quantos colegas dar.</summary>
        public Resultado EscolherComportamento(string id, string comportamento)
        {
            if (!NaEquipe(id, out var p, out var erro))
                return erro;
            var regra = RegraDoArquetipo(p)?.Influencia;
            if (regra?.Regra != RegrasArquetipos.RegraInfluencia.Comportamento ||
                (comportamento != null && regra.Opcoes.All(o => o.Id != comportamento)))
                return Resultado.Falha("Comportamento desconhecido.");
            p.InfluenciaComportamento = comportamento;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Dá (ou tira) Influência a um colega, quando a regra deixa escolher.</summary>
        public Resultado AlternarInfluencia(string id, string colega)
        {
            if (!NaEquipe(id, out var p, out var erro))
                return erro;
            if (!InfluenciaEscolhida(p))
                return Resultado.Falha("Pela regra do arquétipo, a Influência não se escolhe.");
            if (Colegas(p).All(c => c.Id != colega))
                return Resultado.Falha("Escolha um colega desta sala.");
            if (!p.InfluenciaPara.Remove(colega))
            {
                var maximo = QuantasInfluencias(p);
                if (maximo.HasValue && p.InfluenciaPara.Count >= maximo.Value)
                    return Resultado.Falha($"Pela regra, Influência a {maximo} colega(s).");
                p.InfluenciaPara.Add(colega);
            }
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// A quantos colegas dar Influência (null = a regra não diz um número: todos, ou amor e rival).
        /// Nunca mais que os colegas que existem.
        /// </summary>
        public int? QuantasInfluencias(Personagem p)
        {
            var regra = RegraDoArquetipo(p)?.Influencia;
            if (regra == null)
                return null;
            int? n = regra.Regra switch
            {
                RegrasArquetipos.RegraInfluencia.Fixo => regra.QuantidadeNumero,
                RegrasArquetipos.RegraInfluencia.Comportamento =>
                    regra.Opcoes.FirstOrDefault(o => o.Id == p.InfluenciaComportamento)?.QuantidadeNumero,
                _ => null,
            };
            return n.HasValue ? System.Math.Min(n.Value, Colegas(p).Count) : (int?)null;
        }

        /// <summary>A regra deixa escolher os colegas? (fixo, ou comportamento com um número)</summary>
        public bool InfluenciaEscolhida(Personagem p) => QuantasInfluencias(p).HasValue;

        /// <summary>
        /// A quem a personagem dá Influência, pela regra: os escolhidos; ou todos; ou o amor e o rival
        /// (os colegas dos dois relacionamentos).
        /// </summary>
        public IReadOnlyList<string> InfluenciaDada(Personagem p)
        {
            var regra = RegraDoArquetipo(p)?.Influencia;
            var colegas = Colegas(p).Select(c => c.Id).ToList();
            switch (regra?.Regra)
            {
                case RegrasArquetipos.RegraInfluencia.Todos:
                    return colegas;
                case RegrasArquetipos.RegraInfluencia.AmorERival:
                    return p.Relacionamentos.Where(r => r != null && colegas.Contains(r)).Distinct().ToList();
                case RegrasArquetipos.RegraInfluencia.Comportamento
                    when regra.Opcoes.FirstOrDefault(o => o.Id == p.InfluenciaComportamento)?.ParaTodos == true:
                    return colegas;
                default:
                    return p.InfluenciaPara.Where(colegas.Contains).ToList();
            }
        }

        /// <summary>O colega de um relacionamento (o "___" da frase), ou null.</summary>
        public Personagem ColegaDoRelacionamento(Personagem p, int indice) =>
            indice < p.Relacionamentos.Count && p.Relacionamentos[indice] != null
                ? Colegas(p).FirstOrDefault(c => c.Id == p.Relacionamentos[indice])
                : null;

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
            else if (InfluenciaEscolhida(p))
            {
                var quantas = QuantasInfluencias(p).Value;
                var dadas = InfluenciaDada(p).Count;
                if (dadas != quantas)
                    pendencias.Add($"Influência: dê a {quantas} colega(s) (deu a {dadas}).");
            }
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
