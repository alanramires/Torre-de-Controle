using System.Collections.Generic;
using System.Linq;
using TorreDeControle.Core.Regras;
using Regra = TorreDeControle.Core.Regras.RegrasArquetipos.RegraHabilidades;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// Subseção Habilidades da ficha de criação. O modelo vem de arquetipos.json (RegraHabilidades);
    /// aqui só se aplicam as escolhas e se valida conforme o tipo da regra.
    /// </summary>
    public sealed partial class CadastroPersonagens
    {
        public Regra RegraDeHabilidades(Personagem p) =>
            regras.Buscar(p.Foto?.Arquetipo ?? p.Identidade.Arquetipo)?.Habilidades ?? new Regra();

        /// <summary>Habilidades como ficam na ficha: a foto, se aprovada; senão, o que as escolhas montam.</summary>
        public IReadOnlyList<Habilidade> HabilidadesAtuais(Personagem p)
        {
            if (p.Foto != null)
                return p.Foto.Habilidades;

            var regra = RegraDeHabilidades(p);
            var e = p.Criacao.Habilidades;
            var lista = new List<Habilidade>();

            foreach (var f in regra.FixasDadas)
                lista.Add(Nova(f, e, fixa: true));

            switch (regra.Tipo)
            {
                case Regra.Lista:
                    foreach (var o in regra.Opcoes.Where(o => e.Escolhidas.Contains(o.Id)))
                        lista.Add(Nova(o, e, fixa: false));
                    break;
                case Regra.Linha:
                    var linha = regra.Linhas.FirstOrDefault(l => l.Id == e.Linha);
                    if (linha != null)
                        foreach (var poder in linha.Poderes.Where(x => !e.Negadas.Contains(x.Id)))
                            lista.Add(Nova(poder, e, fixa: false));
                    break;
                case Regra.Mentora:
                    foreach (var vaga in regra.Vagas)
                    {
                        if (!e.Vagas.TryGetValue(vaga.Id, out var escolhida))
                            continue;
                        var o = regra.Opcoes.FirstOrDefault(x => x.Id == escolhida);
                        if (o == null)
                            continue;
                        var h = Nova(o, e, fixa: false);
                        h.Nome = $"{o.Nome} ({vaga.Nome.ToLowerInvariant()})";
                        lista.Add(h);
                    }
                    break;
            }
            return lista;
        }

        static Habilidade Nova(RegrasArquetipos.Opcao o, EscolhasHabilidades e, bool fixa) => new Habilidade
        {
            Id = o.Id,
            Nome = o.Nome,
            Fixa = fixa,
            Descricao = e.Descricoes.TryGetValue(o.Id, out var d) ? d : string.Empty,
        };

        // ---------- escolhas ----------

        /// <summary>Lista: marca ou desmarca uma opção, sem passar do máximo.</summary>
        public Resultado AlternarHabilidade(string id, string opcao)
        {
            if (!EmCriacao(id, out var p, out var erro))
                return erro;
            var regra = RegraDeHabilidades(p);
            if (regra.Tipo != Regra.Lista || regra.Opcoes.All(o => o.Id != opcao))
                return Resultado.Falha("Essa habilidade não é escolha deste arquétipo.");

            var escolhidas = p.Criacao.Habilidades.Escolhidas;
            if (escolhidas.Remove(opcao))
            {
                p.Criacao.Habilidades.Descricoes.Remove(opcao);
            }
            else
            {
                if (escolhidas.Count >= regra.MaximoEscolhas)
                    return Resultado.Falha($"Este arquétipo escolhe {regra.MaximoEscolhas}.");
                escolhidas.Add(opcao);
            }
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Linha (Legado): escolhe a linha de poderes; zera as negações da linha anterior.</summary>
        public Resultado EscolherLinha(string id, string linha)
        {
            if (!EmCriacao(id, out var p, out var erro))
                return erro;
            var regra = RegraDeHabilidades(p);
            if (regra.Tipo != Regra.Linha || (linha != null && regra.Linhas.All(l => l.Id != linha)))
                return Resultado.Falha("Linha de poderes desconhecida.");

            var e = p.Criacao.Habilidades;
            if (e.Linha == linha)
                return Resultado.Sucesso();
            e.Linha = linha;
            e.Negadas.Clear();
            e.Descricoes.Clear();
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Linha (Legado): nega ou devolve um poder da linha, até o número que a regra manda.</summary>
        public Resultado AlternarNegada(string id, string poder)
        {
            if (!EmCriacao(id, out var p, out var erro))
                return erro;
            var regra = RegraDeHabilidades(p);
            var e = p.Criacao.Habilidades;
            var linha = regra.Linhas.FirstOrDefault(l => l.Id == e.Linha);
            if (linha == null || linha.Poderes.All(x => x.Id != poder))
                return Resultado.Falha("Escolha a linha antes de negar poderes.");

            if (!e.Negadas.Remove(poder))
            {
                if (e.Negadas.Count >= regra.Negar)
                    return Resultado.Falha($"O legado nega {regra.Negar} poderes da linha.");
                e.Negadas.Add(poder);
                e.Descricoes.Remove(poder);
            }
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Mentora (Discípula): põe uma opção numa vaga (null esvazia). As três precisam ser diferentes.</summary>
        public Resultado DefinirVaga(string id, string vaga, string opcao)
        {
            if (!EmCriacao(id, out var p, out var erro))
                return erro;
            var regra = RegraDeHabilidades(p);
            if (regra.Tipo != Regra.Mentora || regra.Vagas.All(v => v.Id != vaga))
                return Resultado.Falha("Vaga desconhecida.");
            if (opcao != null && regra.Opcoes.All(o => o.Id != opcao))
                return Resultado.Falha("Habilidade desconhecida.");

            var vagas = p.Criacao.Habilidades.Vagas;
            if (opcao != null && vagas.Any(kv => kv.Key != vaga && kv.Value == opcao))
                return Resultado.Falha("Essa habilidade já está em outra vaga; as três são diferentes.");

            if (vagas.TryGetValue(vaga, out var anterior))
                p.Criacao.Habilidades.Descricoes.Remove(anterior);
            if (opcao == null)
                vagas.Remove(vaga);
            else
                vagas[vaga] = opcao;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>"Como se manifesta" de uma habilidade que está na ficha.</summary>
        public Resultado DescreverHabilidade(string id, string habilidade, string texto)
        {
            if (!EmCriacao(id, out var p, out var erro))
                return erro;
            if (HabilidadesAtuais(p).All(h => h.Id != habilidade))
                return Resultado.Falha("Essa habilidade não está na ficha.");

            var limpo = (texto ?? string.Empty).Trim();
            var descricoes = p.Criacao.Habilidades.Descricoes;
            if ((descricoes.TryGetValue(habilidade, out var atual) ? atual : string.Empty) == limpo)
                return Resultado.Sucesso();
            if (limpo.Length == 0)
                descricoes.Remove(habilidade);
            else
                descricoes[habilidade] = limpo;
            Gravar();
            return Resultado.Sucesso();
        }

        // ---------- validação ----------

        /// <summary>
        /// Coisas válidas que vale lembrar (não são pendência e não pedem confirmação para aprovar).
        /// Ex.: "escolha até 3" com 2 escolhidas.
        /// </summary>
        public IReadOnlyList<string> Observacoes(Personagem p)
        {
            var notas = new List<string>();
            if (p.Status != StatusCriacao.Novo)
                return notas;
            var regra = RegraDeHabilidades(p);
            var escolhidas = p.Criacao.Habilidades.Escolhidas.Count;
            if (regra.Tipo == Regra.Lista && regra.EscolherAte.HasValue && escolhidas > 0 && escolhidas < regra.EscolherAte)
            {
                var resta = regra.EscolherAte.Value - escolhidas;
                notas.Add($"Habilidades: {escolhidas} de até {regra.EscolherAte} (ainda pode escolher mais {resta}).");
            }
            ObservarModulos(p, notas);
            return notas;
        }

        void ValidarHabilidades(Personagem p, List<string> avisos)
        {
            var regra = RegraDeHabilidades(p);
            var e = p.Criacao.Habilidades;
            switch (regra.Tipo)
            {
                case Regra.Lista when regra.Escolher.HasValue && e.Escolhidas.Count < regra.Escolher:
                    avisos.Add($"Habilidades: escolha {regra.Escolher} (faltam {regra.Escolher - e.Escolhidas.Count}).");
                    break;
                case Regra.Lista when regra.EscolherAte.HasValue && e.Escolhidas.Count == 0:
                    avisos.Add($"Habilidades: escolha até {regra.EscolherAte}.");
                    break;
                case Regra.Linha when e.Linha == null:
                    avisos.Add("Habilidades: escolha a linha do legado.");
                    break;
                case Regra.Linha when e.Negadas.Count < regra.Negar:
                    avisos.Add($"Habilidades: negue {regra.Negar} poderes da linha (faltam {regra.Negar - e.Negadas.Count}).");
                    break;
                case Regra.Mentora when e.Vagas.Count < regra.Vagas.Count:
                    avisos.Add("Habilidades: preencha a compartilhada, a sua e a da mentora.");
                    break;
            }

            if (regra.Descrever == null)
                return;
            var semDescricao = HabilidadesAtuais(p)
                .Where(h => regra.Descrever == Regra.DescreverCada || !h.Fixa)
                .Where(h => string.IsNullOrWhiteSpace(h.Descricao))
                .Select(h => h.Nome)
                .ToList();
            if (semDescricao.Count > 0)
                avisos.Add("Descreva como se manifesta: " + string.Join(", ", semDescricao) + ".");
        }

        bool EmCriacao(string id, out Personagem p, out Resultado erro)
        {
            p = Buscar(id);
            erro = p == null ? Resultado.Falha("Personagem não encontrada.")
                : p.Status != StatusCriacao.Novo ? Resultado.Falha("A ficha já foi aprovada; isso é da criação.")
                : Resultado.Sucesso();
            return erro.Ok;
        }
    }
}
