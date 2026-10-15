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

        /// <summary>Subseção de um movimento usa os mesmos campos dos extras, com este "extra".</summary>
        public static string ExtraDoMovimento(string movimento) => "mov:" + movimento;

        /// <summary>Movimentos na ficha que abrem subseção (ex.: o bicho do "Vamos, Sortudo!").</summary>
        public IEnumerable<RegrasArquetipos.MovimentoDoArquetipo> MovimentosComSubsecao(Personagem p)
        {
            var naFicha = MovimentosNaFicha(p);
            return MovimentosDoArquetipo(p).Opcoes.Where(m => m.Campos.Count > 0 && naFicha.Contains(m.Id));
        }

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
            if (escolhidos.Remove(movimento))
            {
                // Desmarcou: a subseção do movimento some, e o que estava nela também (e a observação).
                Apagar(p, ExtraDoMovimento(movimento));
                p.ObsMovimentos.Remove(movimento);
            }
            else
            {
                if (escolhidos.Count >= regra.Escolher)
                    return Resultado.Falha($"Este arquétipo escolhe {regra.Escolher} movimentos.");
                escolhidos.Add(movimento);
            }
            Gravar();
            return Resultado.Sucesso();
        }

        public string ObsDoMovimento(Personagem p, string movimento) =>
            p.ObsMovimentos.TryGetValue(movimento, out var v) ? v : string.Empty;

        /// <summary>Observação do jogador sobre um movimento da ficha (qualquer status; vazio apaga).</summary>
        public Resultado AnotarMovimento(string id, string movimento, string texto)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (!MovimentosNaFicha(p).Contains(movimento))
                return Resultado.Falha("Esse movimento não está na ficha.");
            var limpo = (texto ?? string.Empty).Trim();
            if (ObsDoMovimento(p, movimento) == limpo)
                return Resultado.Sucesso();
            if (limpo.Length == 0)
                p.ObsMovimentos.Remove(movimento);
            else
                p.ObsMovimentos[movimento] = limpo;
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
                    if (opcao == c.Final)
                        return Resultado.Falha("Essa não se escolhe na criação.");
                    if (atuais.Remove(opcao))
                    {
                        p.Criacao.ExtrasTextos.Remove(ChaveExtra(extra, campo, opcao));
                        p.Criacao.ExtrasTextos.Remove(ChaveExtra(extra, campo, Campo.PessoaDaOpcao(opcao)));
                    }
                    else
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
            SincronizarNpcsDaFicha(p); // opção desmarcada inativa o NPC da pessoa dela
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado DefinirTextoDeExtra(string id, string extra, string campo, string vaga, string texto)
        {
            // Pessoa de uma opção escolhida (ex.: o chefe do estágio do Janus): vira NPC na sala.
            var opcaoDaPessoa = Campo.OpcaoDaPessoa(vaga);
            var alvo = Buscar(id);
            var pessoa = alvo != null && opcaoDaPessoa != null && PessoaPorEscolha(alvo, extra, campo, opcaoDaPessoa);
            var naSala = alvo != null && (pessoa ? PessoaEditavel(alvo) : ExtraEditavelNaSala(alvo, extra, campo));
            if (opcaoDaPessoa != null && !pessoa)
                return Resultado.Falha("Essa opção não tem pessoa ligada.");
            if (!EmCriacao(id, out var p, out var erro) && !naSala)
                return erro;
            p ??= alvo;
            var c = BuscarCampo(p, extra, campo);
            // Lista com descrição: todas as escolhidas (descrever) ou só as que têm lacuna ("Suscetível a ___").
            var descricaoDeLista = c != null && c.Tipo == Campo.Lista &&
                                   (pessoa || c.Descrever || c.DescreverOpcional || c.Opcoes.Any(o => o.Id == vaga && o.Lacuna));
            if (c == null || (c.Tipo != Campo.Texto && c.Tipo != Campo.Textos && !descricaoDeLista))
                return Resultado.Falha("Campo de texto desconhecido.");
            if (c.NaSala)
                return Resultado.Falha("Isso se preenche na sala, com a equipe.");
            if (descricaoDeLista && !Escolhas(p, extra, campo).Contains(opcaoDaPessoa ?? vaga))
                return Resultado.Falha("Escolha a opção antes de descrevê-la.");

            var chave = ChaveExtra(extra, campo, c.Tipo == Campo.Texto ? null : vaga);
            var limpo = (texto ?? string.Empty).Trim();
            var atual = p.Criacao.ExtrasTextos.TryGetValue(chave, out var v) ? v : string.Empty;
            if (atual == limpo)
                return Resultado.Sucesso();
            if (limpo.Length == 0)
                p.Criacao.ExtrasTextos.Remove(chave);
            else
                p.Criacao.ExtrasTextos[chave] = limpo;
            // Em aprovação a ficha lê da foto: o ajuste vale nas duas (a criação refeita mantém).
            if (p.Foto != null)
            {
                if (limpo.Length == 0)
                    p.Foto.ExtrasTextos.Remove(chave);
                else
                    p.Foto.ExtrasTextos[chave] = limpo;
            }
            SincronizarNpcsDaFicha(p);
            Gravar();
            return Resultado.Sucesso();
        }

        Campo BuscarCampo(Personagem p, string extra, string campo)
        {
            if (extra.StartsWith("mov:"))
            {
                // Campo de subseção de movimento: só existe enquanto o movimento está na ficha.
                var mov = MovimentosComSubsecao(p).FirstOrDefault(m => ExtraDoMovimento(m.Id) == extra);
                return mov?.Campos.FirstOrDefault(c => c.Id == campo);
            }
            return ExtrasDoArquetipo(p).FirstOrDefault(e => e.Id == extra)?.Campos.FirstOrDefault(c => c.Id == campo);
        }

        static void Apagar(Personagem p, string extra)
        {
            var prefixo = extra + "|";
            foreach (var k in p.Criacao.ExtrasEscolhas.Keys.Where(k => k.StartsWith(prefixo)).ToList())
                p.Criacao.ExtrasEscolhas.Remove(k);
            foreach (var k in p.Criacao.ExtrasTextos.Keys.Where(k => k.StartsWith(prefixo)).ToList())
                p.Criacao.ExtrasTextos.Remove(k);
        }

        // ---------- validação ----------

        void ValidarModulos(Personagem p, List<string> avisos)
        {
            var mov = MovimentosDoArquetipo(p);
            var faltamMov = mov.Escolher - p.Criacao.Movimentos.Count;
            if (faltamMov > 0)
                avisos.Add($"Movimentos: escolha {mov.Escolher} (faltam {faltamMov}).");

            foreach (var extra in ExtrasDoArquetipo(p))
                ValidarCampos(p, extra.Id, extra.Nome, extra.Campos, avisos);
            foreach (var m in MovimentosComSubsecao(p))
                ValidarCampos(p, ExtraDoMovimento(m.Id), m.Nome, m.Campos, avisos);
        }

        void ValidarCampos(Personagem p, string extra, string nome, IEnumerable<Campo> campos, List<string> avisos)
        {
            foreach (var c in campos)
            {
                if (c.NoJogo)
                    continue; // preenchido no jogo, não na criação
                var escolhas = Escolhas(p, extra, c.Id);
                switch (c.Tipo)
                {
                    case Campo.Lista when c.Escolher.HasValue && escolhas.Count < c.Escolher:
                        avisos.Add($"{nome}: {c.Titulo.ToLowerInvariant()} (faltam {c.Escolher - escolhas.Count}).");
                        break;
                    case Campo.Lista when c.EscolherAte.HasValue && escolhas.Count == 0:
                        avisos.Add($"{nome}: escolha até {c.EscolherAte}.");
                        break;
                    case Campo.Rotulo when escolhas.Count == 0:
                        avisos.Add($"{nome}: {c.Titulo.ToLowerInvariant()}.");
                        break;
                    case Campo.Texto when !c.NaSala && !c.Opcional && !c.NoJogo && string.IsNullOrWhiteSpace(TextoDoExtra(p, extra, c.Id)):
                        avisos.Add($"{nome}: {c.Titulo.ToLowerInvariant()}.");
                        break;
                    case Campo.Textos:
                        var preenchidas = c.Vagas.Count(v => !string.IsNullOrWhiteSpace(TextoDoExtra(p, extra, c.Id, v.Id)));
                        if (preenchidas < c.Minimo)
                            avisos.Add($"{nome}: preencha pelo menos {c.Minimo} (faltam {c.Minimo - preenchidas}).");
                        break;
                }

                if (c.Tipo == Campo.Lista && c.Descrever)
                {
                    var semTexto = escolhas.Count(o => string.IsNullOrWhiteSpace(TextoDoExtra(p, extra, c.Id, o)));
                    if (semTexto > 0)
                        avisos.Add($"{nome}: {c.Nota ?? "descreva cada escolhida"} (faltam {semTexto}).");
                }
                else if (c.Tipo == Campo.Lista)
                {
                    foreach (var o in c.Opcoes.Where(o => o.Lacuna && escolhas.Contains(o.Id)))
                        if (string.IsNullOrWhiteSpace(TextoDoExtra(p, extra, c.Id, o.Id)))
                            avisos.Add($"{nome}: complete \"{o.Nome}\".");
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
