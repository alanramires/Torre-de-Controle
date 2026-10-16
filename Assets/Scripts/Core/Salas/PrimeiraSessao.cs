using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Regras;

namespace TorreDeControle.Core.Salas
{
    /// <summary>
    /// O roteiro da primeira sessão (livro, cap. 8), lido de Regras/primeira_sessao.json. O preparo a Torre
    /// confere nos dados da sala (<see cref="Conferir"/>); o roteiro o mestre marca na sessão
    /// (<see cref="CadastroSalas.AlternarItemDaPrimeiraSessao"/>). Os nomes dos movimentos básicos vêm de
    /// movimentos_basicos.json: renomear lá muda aqui.
    /// </summary>
    public sealed class PrimeiraSessao
    {
        public const string Arquivo = "primeira_sessao";

        public sealed class Item
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("grupo")] public string Grupo { get; set; }
            [JsonProperty("texto")] public string Texto { get; set; }
            [JsonProperty("dica")] public string Dica { get; set; }
            /// <summary>Item que é um movimento básico: nome e gatilho vêm de movimentos_basicos.json.</summary>
            [JsonProperty("movimento")] public string Movimento { get; set; }
        }

        /// <summary>Um item do preparo, conferido: pronto ou não, e o porquê em uma linha.</summary>
        public sealed class Conferido
        {
            public Item Item { get; set; }
            public bool Pronto { get; set; }
            public string Detalhe { get; set; }
        }

        sealed class ArquivoJson
        {
            [JsonProperty("preparo")] public List<Item> Preparo { get; set; } = new List<Item>();
            [JsonProperty("roteiro")] public List<Item> Roteiro { get; set; } = new List<Item>();
            [JsonProperty("grupos")] public Dictionary<string, string> Grupos { get; set; } = new Dictionary<string, string>();
        }

        sealed class MovimentosJson
        {
            [JsonProperty("movimentos")] public List<Movimento> Movimentos { get; set; } = new List<Movimento>();

            public sealed class Movimento
            {
                [JsonProperty("id")] public string Id { get; set; }
                [JsonProperty("nome")] public string Nome { get; set; }
                [JsonProperty("gatilho")] public string Gatilho { get; set; }
            }
        }

        public IReadOnlyList<Item> Preparo { get; }
        public IReadOnlyList<Item> Roteiro { get; }
        /// <summary>Os grupos do roteiro na ordem em que aparecem, com o título de cada um.</summary>
        public IReadOnlyList<(string id, string titulo)> Grupos { get; }

        PrimeiraSessao(ArquivoJson arquivo)
        {
            Preparo = arquivo.Preparo;
            Roteiro = arquivo.Roteiro;
            Grupos = arquivo.Roteiro.Select(i => i.Grupo).Distinct()
                .Select(g => (g, arquivo.Grupos.TryGetValue(g ?? string.Empty, out var t) ? t : g)).ToList();
        }

        public static PrimeiraSessao Carregar(IFonteDeRegras fonte)
        {
            var texto = fonte.Ler(Arquivo) ?? throw new InvalidOperationException($"Regras/{Arquivo}.json não encontrado.");
            var arquivo = JsonConvert.DeserializeObject<ArquivoJson>(texto) ?? new ArquivoJson();
            var basicos = JsonConvert.DeserializeObject<MovimentosJson>(fonte.Ler("movimentos_basicos") ?? "{}")?.Movimentos
                          ?? new List<MovimentosJson.Movimento>();
            foreach (var item in arquivo.Roteiro.Where(i => i.Movimento != null))
            {
                var m = basicos.FirstOrDefault(x => x.Id == item.Movimento);
                item.Texto ??= m?.Nome ?? item.Movimento;
                item.Dica ??= m?.Gatilho;
            }
            return new PrimeiraSessao(arquivo);
        }

        /// <summary>O preparo da sala, conferido nos dados (fichas, vilão, NPCs e cenário que nasceram das fichas).</summary>
        public IReadOnlyList<Conferido> Conferir(Sala sala, CadastroPersonagens personagens)
        {
            var herois = sala.Slots.Where(s => s.Personagem != null).Select(s => personagens.Buscar(s.Personagem))
                .Where(p => p != null).ToList();
            var npcs = personagens.NpcsDaSala(sala.Id).Where(n => n.Ativo).ToList();
            var viloes = npcs.Where(n => n.Papel == Papel.Vilao).ToList();
            var ligados = viloes.Where(personagens.LigadoAHeroina).ToList();

            var lista = new List<Conferido>();
            foreach (var item in Preparo)
            {
                var c = new Conferido { Item = item };
                switch (item.Id)
                {
                    case "fichas":
                        // Pronta = em carreira (aprovada pelo mestre). Em aprovação ainda pode mudar.
                        var prontas = herois.Where(p => p.Status == StatusCriacao.EmCarreira).ToList();
                        var faltam = herois.Except(prontas).Select(p => $"{p.NomeExibicao} {Situacao(p.Status)}").ToList();
                        c.Pronto = herois.Count > 0 && faltam.Count == 0;
                        c.Detalhe = herois.Count == 0 ? "nenhuma heroína nas vagas"
                            : $"{prontas.Count} de {herois.Count} prontas" + (faltam.Count > 0 ? $"; {string.Join(", ", faltam)}" : string.Empty);
                        break;
                    case "vilao":
                        c.Pronto = ligados.Count > 0;
                        c.Detalhe = ligados.Count > 0
                            ? string.Join("; ", ligados.Select(v => $"{v.NomeExibicao} ({Laco(personagens, v)})"))
                            : viloes.Count > 0
                                ? $"há vilão ({string.Join(", ", viloes.Select(v => v.NomeExibicao))}), sem ligação: use \"Ligado a\" na ficha dele"
                                : "nenhum NPC com papel Vilão / Oponente na sala";
                        break;
                    case "ficha-do-vilao":
                        var alvo = ligados.Count > 0 ? ligados : viloes;
                        var completos = alvo.Where(VilaoCompleto).ToList();
                        var incompletos = alvo.Except(completos).ToList();
                        // Basta um pronto para abrir; os outros aparecem como "falta" para o mestre saber quem usar.
                        c.Pronto = completos.Count > 0;
                        c.Detalhe = alvo.Count == 0 ? "sem vilão ainda"
                            : (completos.Count > 0 ? $"pronto: {string.Join(", ", completos.Select(v => v.NomeExibicao))}" : "nenhum pronto") +
                              (incompletos.Count > 0
                                  ? $" · falta: {string.Join("; ", incompletos.Select(v => $"{v.NomeExibicao} ({Contagem(v)})"))}"
                                  : string.Empty);
                        break;
                    case "materia-prima":
                        var daFicha = npcs.Count(n => n.Pnj?.Vinculo != null);
                        var lugares = personagens.CenarioDaSala(sala.Id).Count(e => e.Ativo);
                        c.Pronto = daFicha + lugares > 0;
                        c.Detalhe = $"{daFicha} NPC{(daFicha == 1 ? "" : "s")} das fichas, {lugares} no cenário";
                        break;
                    default:
                        c.Detalhe = "item sem conferência automática";
                        break;
                }
                lista.Add(c);
            }
            return lista;
        }

        /// <summary>"Aurora Dourada · maior inimigo do legado" (da ficha) ou "Fantasma · destruiu o laboratório" (do mestre).</summary>
        static string Laco(CadastroPersonagens personagens, Personagem v)
        {
            if (personagens.OrigemDoNpc(v) is { } o)
                return $"{o.dono.NomeExibicao} · {o.vinculo}";
            return string.Join(", ", personagens.LigacoesDoNpc(v).Select(l =>
                string.IsNullOrWhiteSpace(l.como) ? l.heroi.NomeExibicao : $"{l.heroi.NomeExibicao} · {l.como}"));
        }

        static string Contagem(Personagem v) =>
            $"{v.Vilao?.Movimentos.Count(m => !string.IsNullOrWhiteSpace(m)) ?? 0}/3 movimentos, {v.Vilao?.Condicoes.Count ?? 0} condições";

        /// <summary>Três ou mais movimentos de vilão e ao menos uma condição (livro, p.194–195).</summary>
        public static bool VilaoCompleto(Personagem v) =>
            v.Vilao != null && v.Vilao.Movimentos.Count(m => !string.IsNullOrWhiteSpace(m)) >= 3 && v.Vilao.Condicoes.Count >= 1;

        static string Situacao(StatusCriacao s) => s switch
        {
            StatusCriacao.Novo => "em criação",
            StatusCriacao.EmRevisao => "em revisão",
            StatusCriacao.EmAprovacao => "em aprovação",
            StatusCriacao.Aposentado => "aposentada",
            _ => s.ToString(),
        };
    }
}
