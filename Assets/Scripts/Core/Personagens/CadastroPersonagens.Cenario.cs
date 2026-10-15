using System;
using System.Collections.Generic;
using System.Linq;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// Cenário das campanhas (locais, organizações, outros): gravado no mesmo arquivo das personagens, porque
    /// nasce da ficha delas como os NPCs. Nada se exclui: inativa.
    /// </summary>
    public sealed partial class CadastroPersonagens
    {
        readonly List<ElementoDeCenario> cenario = new List<ElementoDeCenario>();

        /// <summary>O cenário da sala: ativos primeiro, cada bloco na ordem de criação.</summary>
        public IReadOnlyList<ElementoDeCenario> CenarioDaSala(string sala) =>
            cenario.Where(e => e.Sala == sala).OrderByDescending(e => e.Ativo).ThenBy(e => e.Ordem).ToList();

        public ElementoDeCenario BuscarElemento(string id) => cenario.FirstOrDefault(e => e.Id == id);

        static string TipoValido(string tipo) =>
            tipo == ElementoDeCenario.Organizacao || tipo == ElementoDeCenario.Outro ? tipo : ElementoDeCenario.Local;

        public Resultado CriarElemento(string sala, string nome, string tipo)
        {
            if (string.IsNullOrWhiteSpace(sala))
                return Resultado.Falha("O cenário é de uma campanha.");
            var limpo = (nome ?? string.Empty).Trim();
            if (limpo.Length == 0)
                return Resultado.Falha("Dê um nome ao elemento do cenário.");
            cenario.Add(new ElementoDeCenario
            {
                Id = Guid.NewGuid().ToString("N"), Sala = sala, Nome = limpo, Tipo = TipoValido(tipo),
                Ordem = cenario.Count(e => e.Sala == sala),
            });
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado AlterarElemento(string id, string nome, string tipo, string descricao)
        {
            var e = BuscarElemento(id);
            if (e == null)
                return Resultado.Falha("Elemento do cenário não encontrado.");
            var limpo = (nome ?? string.Empty).Trim();
            if (limpo.Length == 0)
                return Resultado.Falha("Dê um nome ao elemento do cenário.");
            (e.Nome, e.Tipo, e.Descricao) = (limpo, TipoValido(tipo), (descricao ?? string.Empty).Trim());
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado DefinirAtivoElemento(string id, bool ativo)
        {
            var e = BuscarElemento(id);
            if (e == null)
                return Resultado.Falha("Elemento do cenário não encontrado.");
            e.Ativo = ativo;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>De quem é o elemento e como a sala chama o laço; null se o mestre o criou.</summary>
        public (Personagem dono, string vinculo)? OrigemDoElemento(ElementoDeCenario e)
        {
            var v = e?.Vinculo;
            var dono = v == null ? null : Buscar(v.Personagem);
            if (dono == null)
                return null;
            var f = FontesDeCenario(dono).FirstOrDefault(x => x.fonte == v.Extra && x.campo == v.Campo && x.vaga == v.Vaga);
            return (dono, f.vinculo ?? "cenário");
        }

        /// <summary>
        /// O nome vem da ficha (muda lá, muda aqui; na sala só se lê)? Não para o que o mestre criou nem para o
        /// de nome automático (ex.: "Santuário da Crepúsculo"), que o mestre renomeia à vontade.
        /// </summary>
        public bool NomeDoElementoVemDaFicha(ElementoDeCenario e)
        {
            var v = e?.Vinculo;
            var dono = v == null ? null : Buscar(v.Personagem);
            return dono != null && FontesDeCenario(dono)
                .Any(x => x.fonte == v.Extra && x.campo == v.Campo && x.vaga == v.Vaga && !x.automatico);
        }

        /// <summary>
        /// Cada lugar da ficha que dá cenário: as fontes de NPC marcadas com "cenario" (nome escrito pelo jogador)
        /// e o "cenario" dos extras (nome automático; com opção, só se ela estiver marcada).
        /// </summary>
        IEnumerable<(string fonte, string campo, string vaga, string tipo, string nome, string descricao, string vinculo, bool automatico)>
            FontesDeCenario(Personagem p)
        {
            foreach (var f in FontesDeNpc(p).Where(f => f.regra.Cenario != null))
                yield return (f.fonte, f.campo, f.vaga, f.regra.Cenario, f.nome, f.descricao, f.vinculo, false);
            foreach (var extra in ExtrasDoArquetipo(p))
            for (var k = 0; k < extra.Cenario.Count; k++)
            {
                var regra = extra.Cenario[k];
                string descricao;
                if (regra.Opcao != null)
                {
                    if (!Escolhas(p, extra.Id, regra.Campo).Contains(regra.Opcao))
                        continue;
                    descricao = TextoDoExtra(p, extra.Id, regra.Campo, regra.Opcao);
                }
                else
                    descricao = DescreverEscolhas(p, extra, regra.DescricaoDe);
                var escrito = regra.DicaNome == null ? string.Empty : TextoDoExtra(p, extra.Id, CampoDoCenario, k.ToString());
                var automatico = string.IsNullOrWhiteSpace(escrito);
                var nome = automatico ? (regra.Nome ?? string.Empty).Replace("{personagem}", p.NomeExibicao) : escrito;
                yield return (extra.Id, CampoDoCenario, k.ToString(), regra.Tipo, nome, descricao, regra.Vinculo, automatico);
            }
        }

        const string CampoDoCenario = "cenario";

        /// <summary>A caixa de nome do cenário no título do extra (ex.: o santuário): a dica e o índice da regra, ou null.</summary>
        public (int indice, string dica)? NomeDeCenarioDoExtra(Personagem p, string extra)
        {
            var regras = ExtrasDoArquetipo(p).FirstOrDefault(e => e.Id == extra)?.Cenario;
            for (var k = 0; k < (regras?.Count ?? 0); k++)
                if (regras[k].DicaNome != null)
                    return (k, regras[k].DicaNome);
            return null;
        }

        public string NomeDoCenarioNaFicha(Personagem p, string extra, int indice) =>
            TextoDoExtra(p, extra, CampoDoCenario, indice.ToString());

        /// <summary>
        /// Nome do lugar escrito na ficha (opcional): na criação ou, na sala, em aprovação, como as caixas de
        /// pessoa. Vazio volta ao nome automático; o elemento da sala acompanha.
        /// </summary>
        public Resultado DefinirNomeDoCenarioNaFicha(string id, string extra, int indice, string nome)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (!PessoaEditavel(p))
                return Resultado.Falha("Isso se acerta na criação ou na sala, em aprovação.");
            if (NomeDeCenarioDoExtra(p, extra)?.indice != indice)
                return Resultado.Falha("Esse extra não tem nome de lugar.");
            var chave = ChaveExtra(extra, CampoDoCenario, indice.ToString());
            var limpo = (nome ?? string.Empty).Trim();
            foreach (var textos in new[] { p.Criacao.ExtrasTextos, p.Foto?.ExtrasTextos })
            {
                if (textos == null)
                    continue;
                if (limpo.Length == 0)
                    textos.Remove(chave);
                else
                    textos[chave] = limpo;
            }
            var elemento = cenario.FirstOrDefault(e => e.Vinculo is { } v && v.Personagem == p.Id && v.Extra == extra &&
                                                       v.Campo == CampoDoCenario && v.Vaga == indice.ToString());
            SincronizarNpcsDaFicha(p);
            // Apagado: volta ao nome automático (o sincronizar não mexe em nome automático depois de nascer).
            if (limpo.Length == 0 && elemento != null)
                elemento.Nome = FontesDeCenario(p).First(f => f.fonte == extra && f.vaga == indice.ToString()).nome;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>"Características: Trancas e armadilhas (portão de ferro), Relíquias antigas." uma linha por campo.</summary>
        string DescreverEscolhas(Personagem p, Regras.RegrasArquetipos.RegraExtra extra, IEnumerable<string> campos)
        {
            var linhas = new List<string>();
            foreach (var id in campos)
            {
                var campo = extra.Campos.FirstOrDefault(c => c.Id == id);
                var escolhas = campo == null ? new List<string>() : Escolhas(p, extra.Id, id).ToList();
                if (escolhas.Count == 0)
                    continue;
                var itens = escolhas.Select(o =>
                {
                    var nome = campo.Opcoes.FirstOrDefault(x => x.Id == o)?.Nome ?? o;
                    var texto = TextoDoExtra(p, extra.Id, id, o);
                    return string.IsNullOrWhiteSpace(texto) ? nome : $"{nome} ({texto.Trim()})";
                });
                linhas.Add($"{campo.Titulo}: {string.Join(", ", itens)}.");
            }
            return string.Join("\n", linhas);
        }

        /// <summary>
        /// A parte "cenário" da sincronização da ficha: fonte com tipo de cenário cria (ou reativa e renomeia) o
        /// elemento; nome vazio ou fonte que sumiu inativa. O texto dela vira a descrição uma vez (cópia).
        /// Nome automático só vale ao nascer: depois, o nome é do mestre.
        /// </summary>
        void SincronizarCenarioDaFicha(Personagem p)
        {
            var doCenario = FontesDeCenario(p).ToList();
            foreach (var orfao in cenario.Where(e => e.Ativo && e.Vinculo is { } v && v.Personagem == p.Id &&
                                                   !doCenario.Any(f => f.fonte == v.Extra && f.campo == v.Campo && f.vaga == v.Vaga)))
                orfao.Ativo = false;
            foreach (var f in doCenario)
            {
                var nome = (f.nome ?? string.Empty).Trim();
                var e = cenario.FirstOrDefault(x => x.Vinculo is { } v && v.Personagem == p.Id && v.Extra == f.fonte &&
                                                    v.Campo == f.campo && v.Vaga == f.vaga);
                if (nome.Length == 0)
                {
                    if (e != null)
                        e.Ativo = false;
                    continue;
                }
                if (e == null)
                {
                    e = new ElementoDeCenario
                    {
                        Id = Guid.NewGuid().ToString("N"), Sala = p.Sala, Nome = nome, Tipo = TipoValido(f.tipo),
                        Ordem = cenario.Count(x => x.Sala == p.Sala),
                        Vinculo = new VinculoNpc { Personagem = p.Id, Extra = f.fonte, Campo = f.campo, Vaga = f.vaga },
                    };
                    cenario.Add(e);
                }
                e.Ativo = true;
                if (!f.automatico)
                    e.Nome = nome;
                if (!e.DescricaoCopiada && !string.IsNullOrWhiteSpace(f.descricao))
                {
                    if (string.IsNullOrWhiteSpace(e.Descricao))
                        e.Descricao = f.descricao.Trim();
                    e.DescricaoCopiada = true;
                }
            }
        }
    }
}
