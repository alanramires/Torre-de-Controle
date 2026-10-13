using System.Collections.Generic;
using System.Linq;
using TorreDeControle.Core.Regras;
using Campo = TorreDeControle.Core.Regras.RegrasArquetipos.CampoExtra;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// Seção Módulos da ficha de criação: o deck do arquétipo (movimentos) e os extras, com os campos
    /// que cada ficha pede. O modelo vem de arquetipos.json; aqui só se aplicam e validam as escolhas.
    /// </summary>
    public sealed partial class CadastroPersonagens
    {
        public RegrasArquetipos.RegraMovimentos MovimentosDoArquetipo(Personagem p) =>
            RegraDoArquetipo(p)?.Movimentos ?? new RegrasArquetipos.RegraMovimentos();

        public IReadOnlyList<RegrasArquetipos.RegraExtra> ExtrasDoArquetipo(Personagem p) =>
            RegraDoArquetipo(p)?.Extras ?? new List<RegrasArquetipos.RegraExtra>();

        static string ChaveExtra(string extra, string campo, string vaga = null) =>
            vaga == null ? $"{extra}|{campo}" : $"{extra}|{campo}|{vaga}";

        /// <summary>Movimentos na ficha: obrigatórios + escolhidos (a foto, se concluída).</summary>
        public IReadOnlyList<string> MovimentosNaFicha(Personagem p)
        {
            if (p.Foto != null)
                return p.Foto.Movimentos;
            var regra = MovimentosDoArquetipo(p);
            return regra.Obrigatorios.Concat(p.Criacao.Movimentos).ToList();
        }

        public IReadOnlyList<string> Escolhas(Personagem p, string extra, string campo)
        {
            var fonte = p.Foto != null ? p.Foto.ExtrasEscolhas : p.Criacao.ExtrasEscolhas;
            return fonte.TryGetValue(ChaveExtra(extra, campo), out var v) ? v : new List<string>();
        }

        public string TextoDoExtra(Personagem p, string extra, string campo, string vaga = null)
        {
            var fonte = p.Foto != null ? p.Foto.ExtrasTextos : p.Criacao.ExtrasTextos;
            return fonte.TryGetValue(ChaveExtra(extra, campo, vaga), out var v) ? v : string.Empty;
        }

        // ---------- escolhas ----------

        public Resultado AlternarMovimento(string id, string movimento)
        {
            if (!EmCriacao(id, out var p, out var erro))
                return erro;
            var regra = MovimentosDoArquetipo(p);
            if (regra.Obrigatorios.Contains(movimento))
                return Resultado.Falha("Esse movimento já vem com o arquétipo.");
            if (regra.Opcoes.All(o => o.Id != movimento))
                return Resultado.Falha("Esse movimento não é deste arquétipo.");

            var escolhidos = p.Criacao.Movimentos;
            if (!escolhidos.Remove(movimento))
            {
                if (escolhidos.Count >= regra.Escolher)
                    return Resultado.Falha($"Este arquétipo escolhe {regra.Escolher} movimentos.");
                escolhidos.Add(movimento);
            }
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Lista: marca/desmarca uma opção (até o máximo). Rótulo: escolhe um (troca o anterior).</summary>
        public Resultado AlternarOpcaoDeExtra(string id, string extra, string campo, string opcao)
        {
            if (!EmCriacao(id, out var p, out var erro))
                return erro;
            var c = BuscarCampo(p, extra, campo);
            if (c == null)
                return Resultado.Falha("Campo desconhecido.");

            var chave = ChaveExtra(extra, campo);
            if (!p.Criacao.ExtrasEscolhas.TryGetValue(chave, out var atuais))
                p.Criacao.ExtrasEscolhas[chave] = atuais = new List<string>();

            switch (c.Tipo)
            {
                case Campo.Rotulo:
                    if (!c.Rotulos.Contains(opcao))
                        return Resultado.Falha("Rótulo fora das opções.");
                    var mesmo = atuais.Contains(opcao);
                    atuais.Clear();
                    if (!mesmo)
                        atuais.Add(opcao);
                    break;
                case Campo.Lista:
                    if (c.Opcoes.All(o => o.Id != opcao))
                        return Resultado.Falha("Opção desconhecida.");
                    if (!atuais.Remove(opcao))
                    {
                        if (atuais.Count >= c.MaximoEscolhas)
                            return Resultado.Falha($"{c.Titulo}: escolha {c.MaximoEscolhas}.");
                        atuais.Add(opcao);
                    }
                    break;
                default:
                    return Resultado.Falha("Esse campo não é de escolha.");
            }
            if (atuais.Count == 0)
                p.Criacao.ExtrasEscolhas.Remove(chave);
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado DefinirTextoDeExtra(string id, string extra, string campo, string vaga, string texto)
        {
            if (!EmCriacao(id, out var p, out var erro))
                return erro;
            var c = BuscarCampo(p, extra, campo);
            if (c == null || (c.Tipo != Campo.Texto && c.Tipo != Campo.Textos))
                return Resultado.Falha("Campo de texto desconhecido.");
            if (c.NaSala)
                return Resultado.Falha("Isso se preenche na sala, com a equipe.");

            var chave = ChaveExtra(extra, campo, c.Tipo == Campo.Textos ? vaga : null);
            var limpo = (texto ?? string.Empty).Trim();
            var atual = p.Criacao.ExtrasTextos.TryGetValue(chave, out var v) ? v : string.Empty;
            if (atual == limpo)
                return Resultado.Sucesso();
            if (limpo.Length == 0)
                p.Criacao.ExtrasTextos.Remove(chave);
            else
                p.Criacao.ExtrasTextos[chave] = limpo;
            Gravar();
            return Resultado.Sucesso();
        }

        Campo BuscarCampo(Personagem p, string extra, string campo) =>
            ExtrasDoArquetipo(p).FirstOrDefault(e => e.Id == extra)?.Campos.FirstOrDefault(c => c.Id == campo);

        // ---------- validação ----------

        void ValidarModulos(Personagem p, List<string> avisos)
        {
            var mov = MovimentosDoArquetipo(p);
            var faltamMov = mov.Escolher - p.Criacao.Movimentos.Count;
            if (faltamMov > 0)
                avisos.Add($"Movimentos: escolha {mov.Escolher} (faltam {faltamMov}).");

            foreach (var extra in ExtrasDoArquetipo(p))
            foreach (var c in extra.Campos)
            {
                switch (c.Tipo)
                {
                    case Campo.Lista when c.Escolher.HasValue && Escolhas(p, extra.Id, c.Id).Count < c.Escolher:
                        avisos.Add($"{extra.Nome}: {c.Titulo.ToLowerInvariant()} (faltam {c.Escolher - Escolhas(p, extra.Id, c.Id).Count}).");
                        break;
                    case Campo.Lista when c.EscolherAte.HasValue && Escolhas(p, extra.Id, c.Id).Count == 0:
                        avisos.Add($"{extra.Nome}: escolha até {c.EscolherAte}.");
                        break;
                    case Campo.Rotulo when Escolhas(p, extra.Id, c.Id).Count == 0:
                        avisos.Add($"{extra.Nome}: {c.Titulo.ToLowerInvariant()}.");
                        break;
                    case Campo.Texto when !c.NaSala && string.IsNullOrWhiteSpace(TextoDoExtra(p, extra.Id, c.Id)):
                        avisos.Add($"{extra.Nome}: {c.Titulo.ToLowerInvariant()}.");
                        break;
                    case Campo.Textos:
                        var preenchidas = c.Vagas.Count(v => !string.IsNullOrWhiteSpace(TextoDoExtra(p, extra.Id, c.Id, v.Id)));
                        if (preenchidas < c.Minimo)
                            avisos.Add($"{extra.Nome}: preencha pelo menos {c.Minimo} (faltam {c.Minimo - preenchidas}).");
                        break;
                }
            }
        }

        void ObservarModulos(Personagem p, List<string> notas)
        {
            foreach (var extra in ExtrasDoArquetipo(p))
            foreach (var c in extra.Campos.Where(c => c.Tipo == Campo.Lista && c.EscolherAte.HasValue))
            {
                var n = Escolhas(p, extra.Id, c.Id).Count;
                if (n > 0 && n < c.EscolherAte)
                    notas.Add($"{extra.Nome}: {n} de até {c.EscolherAte} (ainda pode escolher mais {c.EscolherAte - n}).");
            }
        }

        /// <summary>Copia as escolhas de Módulos para a foto da criação.</summary>
        void CongelarModulos(Personagem p, FotoCriacao foto)
        {
            foto.Movimentos = MovimentosNaFicha(p).ToList();
            foto.ExtrasEscolhas = p.Criacao.ExtrasEscolhas.ToDictionary(kv => kv.Key, kv => kv.Value.ToList());
            foto.ExtrasTextos = new Dictionary<string, string>(p.Criacao.ExtrasTextos);
        }
    }
}
