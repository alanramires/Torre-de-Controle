using System.Linq;
using TorreDeControle.Core.Regras;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// Seção Crescimento, protótipo marcável: as casas de avanço da ficha do arquétipo.
    /// Por enquanto só a marca (sem aplicar o efeito, sem gastar potencial, sem evento).
    /// </summary>
    public sealed partial class CadastroPersonagens
    {
        public const string Cima = "cima";
        public const string Baixo = "baixo";

        public RegrasArquetipos.ListaDeAvancos AvancosDoArquetipo(Personagem p) =>
            RegraDoArquetipo(p)?.Avancos ?? new RegrasArquetipos.ListaDeAvancos();

        public static string Casa(string lista, int indice) => $"{lista}:{indice}";

        // ---------- potencial corrido e avanços pendentes ----------

        public const int PotencialPorAvanco = 5;

        /// <summary>Avanços que o potencial já rendeu (um a cada múltiplo de 5).</summary>
        public int AvancosGanhos(Personagem p) => p.Potencial / PotencialPorAvanco;

        /// <summary>Avanços ganhos e ainda não marcados ("level up" pendente).</summary>
        public int AvancosPendentes(Personagem p) => System.Math.Max(0, AvancosGanhos(p) - p.AvancosMarcados.Count);

        /// <summary>Marcou o avanço que encerra a carreira (aposentar / virar modelo).</summary>
        public bool CarreiraEncerrada(Personagem p)
        {
            var baixo = AvancosDoArquetipo(p).Baixo;
            return p.AvancosMarcados.Any(c =>
            {
                var partes = c.Split(':');
                return partes[0] == Baixo && int.TryParse(partes[1], out var k) && k < baixo.Count &&
                       (baixo[k].Id == "aposentar" || baixo[k].Id == "modelo-ate-o-fim");
            });
        }

        /// <summary>Leva o potencial a um valor exato (digitado no playground), com as mesmas regras.</summary>
        public Resultado DefinirPotencial(string id, int valor)
        {
            var p = Buscar(id);
            return p == null ? Resultado.Falha("Personagem não encontrada.") : AjustarPotencial(id, valor - p.Potencial);
        }

        /// <summary>
        /// PROVISÓRIO, até o painel do mestre: soma ou tira potencial. Não deixa tirar o potencial que
        /// já virou avanço marcado.
        /// </summary>
        public Resultado AjustarPotencial(string id, int delta)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (!LiberadoNoJogo(p))
                return Resultado.Falha("Potencial é do jogo, depois de concluir a criação.");
            var novo = p.Potencial + delta;
            if (novo < 0)
                return Resultado.Falha("Potencial não fica negativo.");
            if (novo / PotencialPorAvanco < p.AvancosMarcados.Count)
                return Resultado.Falha("Esse potencial já virou avanço marcado: desmarque o avanço antes.");
            p.Potencial = novo;
            Gravar();
            return Resultado.Sucesso();
        }

        public bool Marcado(Personagem p, string lista, int indice) => p.AvancosMarcados.Contains(Casa(lista, indice));

        public int MarcadosEmCima(Personagem p) => p.AvancosMarcados.Count(c => c.StartsWith(Cima + ":"));

        /// <summary>Os de baixo liberam com 5 marcados em cima.</summary>
        public bool BaixoLiberado(Personagem p) =>
            MarcadosEmCima(p) >= RegrasArquetipos.ListaDeAvancos.ParaLiberarBaixo;

        // ---------- cumprir (impulsos do Farol e afins) ----------

        static string ChaveCumprido(string extra, string opcao) => $"{extra}|{opcao}";

        public bool Cumprido(Personagem p, string extra, string opcao) =>
            p.Cumpridos.ContainsKey(ChaveCumprido(extra, opcao));

        public string BeneficioEscolhido(Personagem p, string extra, string opcao) =>
            p.Cumpridos.TryGetValue(ChaveCumprido(extra, opcao), out var b) ? b : string.Empty;

        /// <summary>Marca/desmarca como cumprida uma escolha do extra (ex.: um impulso marcado).</summary>
        public Resultado AlternarCumprido(string id, string extra, string opcao)
        {
            if (!PodeCumprir(id, extra, opcao, out var p, out var erro))
                return erro;
            var chave = ChaveCumprido(extra, opcao);
            if (!p.Cumpridos.Remove(chave))
                p.Cumpridos[chave] = string.Empty;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Benefício escolhido ao cumprir (vazio tira a escolha).</summary>
        public Resultado EscolherBeneficio(string id, string extra, string opcao, string beneficio)
        {
            if (!PodeCumprir(id, extra, opcao, out var p, out var erro))
                return erro;
            var chave = ChaveCumprido(extra, opcao);
            if (!p.Cumpridos.ContainsKey(chave))
                return Resultado.Falha("Marque como cumprido antes de escolher o benefício.");
            var regra = ExtrasDoArquetipo(p).First(e => e.Id == extra);
            beneficio ??= string.Empty;
            if (beneficio.Length > 0 && regra.AoCumprir.All(b => b.Id != beneficio))
                return Resultado.Falha("Benefício desconhecido.");
            p.Cumpridos[chave] = beneficio;
            Gravar();
            return Resultado.Sucesso();
        }

        bool PodeCumprir(string id, string extra, string opcao, out Personagem p, out Resultado erro)
        {
            p = Buscar(id);
            erro = Resultado.Sucesso();
            if (p == null)
                erro = Resultado.Falha("Personagem não encontrada.");
            else if (!LiberadoNoJogo(p))
                erro = Resultado.Falha("Cumprir é durante o jogo, depois de concluir a criação.");
            else
            {
                var regra = ExtrasDoArquetipo(p).FirstOrDefault(e => e.Id == extra);
                if (regra?.CumpreCampo == null || !Escolhas(p, extra, regra.CumpreCampo).Contains(opcao))
                    erro = Resultado.Falha("Só se cumpre o que foi escolhido na ficha.");
            }
            return erro.Ok;
        }

        /// <summary>Marca ou desmarca uma casa de avanço.</summary>
        public Resultado AlternarAvanco(string id, string lista, int indice)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (!LiberadoNoJogo(p))
                return Resultado.Falha("Avanços vêm depois de concluir a criação.");

            var avancos = AvancosDoArquetipo(p);
            var itens = lista == Cima ? avancos.Cima : lista == Baixo ? avancos.Baixo : null;
            if (itens == null || indice < 0 || indice >= itens.Count)
                return Resultado.Falha("Avanço desconhecido.");

            var casa = Casa(lista, indice);
            var marcando = !p.AvancosMarcados.Contains(casa);
            if (marcando && AvancosPendentes(p) == 0)
                return Resultado.Falha("Sem avanço pendente: a cada 5 de potencial, um avanço.");
            if (marcando && lista == Baixo && !BaixoLiberado(p))
                return Resultado.Falha("Os avanços de baixo liberam com 5 marcados em cima.");

            var baixoMarcado = p.AvancosMarcados.Any(c => c.StartsWith(Baixo + ":"));
            if (!marcando && lista == Cima && baixoMarcado &&
                MarcadosEmCima(p) - 1 < RegrasArquetipos.ListaDeAvancos.ParaLiberarBaixo)
                return Resultado.Falha("Há avanço de baixo marcado: desmarque-o antes de ficar com menos de 5 em cima.");

            if (marcando)
                p.AvancosMarcados.Add(casa);
            else
                p.AvancosMarcados.Remove(casa);
            Gravar();
            return Resultado.Sucesso();
        }
    }
}
