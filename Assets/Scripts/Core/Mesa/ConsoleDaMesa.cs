using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TorreDeControle.Core.Personagens;

namespace TorreDeControle.Core.Salas
{
    /// <summary>
    /// O console do mestre na mesa: comandos com barra ("/roll +2", "/npc_create \"Seu Valdir\" neutral \"vigia\"")
    /// para o que daria vários cliques. Cada comando usa os mesmos caminhos da tela (Mesa, cadastro), então vai
    /// para o log e se desfaz do mesmo jeito. Nomes de personagem aceitam o começo do nome, com ou sem aspas.
    /// </summary>
    public sealed class ConsoleDaMesa
    {
        readonly Mesa mesa;
        readonly CadastroPersonagens personagens;
        readonly Sala sala;
        readonly Random dados;

        public ConsoleDaMesa(Mesa mesa, CadastroPersonagens personagens, Sala sala, Random dados = null)
        {
            this.mesa = mesa;
            this.personagens = personagens;
            this.sala = sala;
            this.dados = dados ?? new Random();
        }

        /// <summary>Os comandos, para a janela de ajuda: exemplo e o que faz.</summary>
        public static readonly IReadOnlyList<(string exemplo, string oQueFaz)> Comandos = new[]
        {
            ("/roll +2", "rola 2d6 + modificador (teto de −3 a +4) e anota no log"),
            ("/potencial Aurora o soco foi absorvido", "+1 potencial, com o motivo obrigatório"),
            ("/pilha +2 questionário", "soma ou tira da pilha de Equipe (motivo opcional)"),
            ("/cond Aurora inseguro", "marca ou desmarca a condição de uma heroína ou de um vilão em cena"),
            ("/lembrete Ocaso abertura: Aurora cedeu terreno", "lembrete no cartão"),
            ("/nota a cena vai para o hospital", "anotação livre no log"),
            ("/npc_create \"Seu Valdir\" neutral \"vigia noturno\"", "cria NPC na campanha: ally, neutral ou villain; a nota vai para as Notas"),
            ("/location_create \"Usina Halcyon Sul\" \"galpão destruído\"", "cria um Local no cenário, com a descrição"),
            ("/rodada", "abre a próxima rodada do log"),
            ("/clearlog", "esconde todas as entradas do log (ficam no arquivo; o efeito na mesa continua)"),
            ("/help", "abre esta janela"),
        };

        public const string Ajuda =
            "/roll [mod] · /potencial Nome motivo · /pilha +N|-N [motivo] · /cond Nome condição · " +
            "/lembrete Nome texto · /nota texto · /npc_create \"Nome\" ally|neutral|villain [\"nota\"] · " +
            "/location_create \"Nome\" [\"nota\"] · /rodada · /clearlog · /help";

        /// <summary>Executa a linha. Devolve o resultado e a mensagem para mostrar (sucesso ou erro).</summary>
        public (bool ok, string mensagem) Executar(string linha)
        {
            var partes = Partes(linha);
            if (partes.Count == 0)
                return (false, "Digite um comando (ex.: /help).");
            var comando = partes[0].TrimStart('/').ToLowerInvariant();
            var args = partes.Skip(1).ToList();
            try
            {
                switch (comando)
                {
                    case "help": case "ajuda": return (true, Ajuda);
                    case "roll": case "rolar": return Rolar(args);
                    case "potencial": return Potencial(args);
                    case "pilha": case "equipe": return Pilha(args);
                    case "cond": case "condicao": return Condicao(args);
                    case "lembrete": return Lembrete(args);
                    case "nota": return Nota(args);
                    case "npc_create": case "npc": return CriarNpc(args);
                    case "location_create": case "local": return CriarLocal(args);
                    case "rodada": mesa.NovaRodada(); return (true, $"Rodada {mesa.Rodada}.");
                    case "clearlog": case "limparlog":
                        var n = mesa.EsconderTudo();
                        return (true, n == 0 ? "O log já está vazio." : $"{n} entrada{(n == 1 ? "" : "s")} escondida{(n == 1 ? "" : "s")} (ficam no arquivo; o efeito na mesa continua).");
                    default: return (false, $"Comando desconhecido: /{comando}. Veja /help.");
                }
            }
            catch (Exception e)
            {
                return (false, "Erro no comando: " + e.Message);
            }
        }

        // ---------- comandos ----------

        /// <summary>A linha é uma rolagem (/roll, /rolar)? A tela usa para jogar os dados físicos antes.</summary>
        public static bool EhRolagem(string linha)
        {
            var p = Partes(linha);
            return p.Count > 0 && (p[0].TrimStart('/').ToLowerInvariant() is "roll" or "rolar");
        }

        /// <summary>
        /// A rolagem com os dois dados vindos de fora (os dados físicos da mesa): só aplica o
        /// modificador (teto de −3 a +4), a faixa e anota no log.
        /// </summary>
        public (bool ok, string mensagem) RolarComDados(string linha, int dado1, int dado2)
        {
            return Anotar2D6(Partes(linha).Skip(1).ToList(), dado1, dado2);
        }

        (bool, string) Rolar(List<string> args) => Anotar2D6(args, dados.Next(1, 7), dados.Next(1, 7));

        /// <summary>Lança a rolagem na mesa (pendente: Intervir/Aceitar no log). O teto vale aqui.</summary>
        (bool, string) Anotar2D6(List<string> args, int a, int b)
        {
            var mod = 0;
            if (args.Count > 0 && !int.TryParse(args[0].Replace("+", string.Empty), out mod))
                return (false, "Use /roll ou /roll +2 (o modificador é um número).");
            var motivo = args.Count > 1 ? string.Join(" ", args.Skip(1)) : null;
            var r = mesa.Rolar(null, a, b, mod, motivo);
            return r.Ok ? (true, mesa.Eventos.Last().Descricao) : (false, r.Erro);
        }

        (bool, string) Potencial(List<string> args)
        {
            if (args.Count < 2)
                return (false, "Use /potencial Nome motivo (o motivo é obrigatório).");
            var (p, erro) = Achar(args[0], incluirViloes: false);
            if (p == null)
                return (false, erro);
            var r = mesa.DarPotencial(p.Id, 1, string.Join(" ", args.Skip(1)));
            return r.Ok ? (true, $"{p.NomeExibicao}: +1 potencial ({p.Potencial}).") : (false, r.Erro);
        }

        (bool, string) Pilha(List<string> args)
        {
            if (args.Count < 1 || !int.TryParse(args[0].Replace("+", string.Empty), out var delta) || delta == 0)
                return (false, "Use /pilha +1 ou /pilha -2 [motivo].");
            var motivo = args.Count > 1 ? string.Join(" ", args.Skip(1)) : null;
            var r = mesa.MudarEquipe(delta, delta > 0 ? $"+{delta} Equipe" : $"{delta} Equipe", motivo);
            return r.Ok ? (true, $"Pilha: {mesa.Equipe}.") : (false, r.Erro);
        }

        (bool, string) Condicao(List<string> args)
        {
            if (args.Count < 2)
                return (false, "Use /cond Nome condição (irritado, assustado, culpado, desesperancoso, inseguro).");
            var cond = Condicoes.Todas.FirstOrDefault(c => c.StartsWith(Sem(args[1])));
            if (cond == null)
                return (false, $"Condição desconhecida: {args[1]}.");
            var (p, erro) = Achar(args[0], incluirViloes: true);
            if (p == null)
                return (false, erro);
            var r = p.Controle == Controle.Mestre
                ? mesa.AlternarCondicaoDoVilao(p.Id, cond, null)
                : mesa.AlternarCondicao(p.Id, cond, null);
            return r.Ok ? (true, $"{p.NomeExibicao}: {Condicoes.Nome(cond)} alternado.") : (false, r.Erro);
        }

        (bool, string) Lembrete(List<string> args)
        {
            if (args.Count < 2)
                return (false, "Use /lembrete Nome texto.");
            var (p, erro) = Achar(args[0], incluirViloes: true);
            if (p == null)
                return (false, erro);
            var texto = string.Join(" ", args.Skip(1));
            var r = mesa.DarBonus(p.Id, Mesa.BonusLembrete, 0, texto, null);
            return r.Ok ? (true, $"Lembrete em {p.NomeExibicao}: {texto}") : (false, r.Erro);
        }

        (bool, string) Nota(List<string> args)
        {
            if (args.Count == 0)
                return (false, "Use /nota texto.");
            var texto = string.Join(" ", args);
            mesa.Anotar(texto, null);
            return (true, "Nota no log.");
        }

        (bool, string) CriarNpc(List<string> args)
        {
            if (args.Count < 2)
                return (false, "Use /npc_create \"Nome\" ally|neutral|villain [\"nota\"].");
            var papel = Sem(args[1]) switch
            {
                "ally" or "aliado" => (Papel?)Papel.Aliado,
                "neutral" or "neutro" => Papel.Neutro,
                "villain" or "vilao" or "oponente" => Papel.Vilao,
                _ => null,
            };
            if (papel == null)
                return (false, $"Papel desconhecido: {args[1]} (ally, neutral ou villain).");
            var nome = args[0];
            var r = personagens.CriarNpc(sala.Id, nome, Maturidade.Adulto, papel.Value);
            if (!r.Ok)
                return (false, r.Erro);
            var npc = personagens.NpcsDaSala(sala.Id).LastOrDefault(n => n.NomeExibicao == nome);
            if (npc != null && args.Count > 2)
                personagens.AlterarTextos(npc.Id, npc.Identidade.Aparencia, string.Join(" ", args.Skip(2)));
            return (true, $"NPC criado: {nome} ({papel}). Ajuste maturidade e ficha na aba NPCs.");
        }

        (bool, string) CriarLocal(List<string> args)
        {
            if (args.Count < 1)
                return (false, "Use /location_create \"Nome\" [\"nota\"].");
            var nome = args[0];
            var r = personagens.CriarElemento(sala.Id, nome, ElementoDeCenario.Local);
            if (!r.Ok)
                return (false, r.Erro);
            var local = personagens.CenarioDaSala(sala.Id).LastOrDefault(e => e.Nome == nome);
            if (local != null && args.Count > 1)
                personagens.AlterarElemento(local.Id, nome, ElementoDeCenario.Local, string.Join(" ", args.Skip(1)));
            return (true, $"Local criado no cenário: {nome}.");
        }

        // ---------- por dentro ----------

        /// <summary>Personagem da mesa (e vilões em cena) pelo começo do nome, sem acento nem maiúscula.</summary>
        (Personagem, string) Achar(string nome, bool incluirViloes)
        {
            var candidatos = sala.Slots.Where(v => v.Personagem != null).Select(v => personagens.Buscar(v.Personagem))
                .Where(p => p != null && p.Ativo && p.Status == StatusCriacao.EmCarreira).ToList();
            if (incluirViloes)
                candidatos.AddRange(mesa.EmCena());
            var alvo = Sem(nome);
            var achados = candidatos.Where(p => Sem(p.NomeExibicao).StartsWith(alvo) ||
                                                Sem(p.NomeCompleto).StartsWith(alvo)).Distinct().ToList();
            if (achados.Count == 1)
                return (achados[0], null);
            if (achados.Count == 0)
                return (null, $"Ninguém na mesa chamado \"{nome}\".");
            return (null, $"\"{nome}\" serve para {string.Join(", ", achados.Select(p => p.NomeExibicao))}: escreva mais do nome.");
        }

        static readonly Regex Token = new Regex("\"([^\"]*)\"|(\\S+)");

        /// <summary>Divide a linha em partes, respeitando aspas ("Seu Valdir" vira uma parte só).</summary>
        public static List<string> Partes(string linha) =>
            Token.Matches(linha ?? string.Empty).Cast<Match>()
                .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value).ToList();

        /// <summary>Minúsculas e sem acento, para comparar nomes e palavras.</summary>
        static string Sem(string s)
        {
            var normal = (s ?? string.Empty).ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            return new string(normal.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) !=
                                                System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());
        }
    }
}
