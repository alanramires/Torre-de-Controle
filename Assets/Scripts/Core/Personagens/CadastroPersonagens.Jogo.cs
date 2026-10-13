using System.Collections.Generic;
using System.Linq;
using Campo = TorreDeControle.Core.Regras.RegrasArquetipos.CampoExtra;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// Estado de jogo dos extras: contadores corridos (trilha da perdição), listas que crescem no jogo
    /// (sinais da perdição) e condições que o mestre seleciona e conclui (uso do santuário).
    /// Tudo depois de concluir a criação. Hoje o mestre mexe pela aba Variáveis; depois, pelo painel.
    /// </summary>
    public sealed partial class CadastroPersonagens
    {
        public const int PontosPorEscolha = 5;
        const string Concluidas = "concluidas";

        static string Sub(string extra, string campo, string sub) => $"{extra}|{campo}|{sub}";

        public int Contador(Personagem p, string extra, string campo) =>
            p.Contadores.TryGetValue(ChaveExtra(extra, campo), out var v) ? v : 0;

        public IReadOnlyList<string> EscolhasNoJogo(Personagem p, string extra, string campo) =>
            p.EscolhasDeJogo.TryGetValue(ChaveExtra(extra, campo), out var v) ? v : new List<string>();

        public string TextoDeJogo(Personagem p, string extra, string campo, string sub) =>
            p.TextosDeJogo.TryGetValue(Sub(extra, campo, sub), out var v) ? v : string.Empty;

        // ---------- trilhas que rendem escolhas ----------

        /// <summary>Quantas escolhas de jogo a trilha já rendeu para este campo (uma a cada 5 pontos).</summary>
        public int EscolhasRendidas(Personagem p, string extra, string campo)
        {
            var alvo = ChaveExtra(extra, campo);
            var rendidas = 0;
            foreach (var e in ExtrasDoArquetipo(p))
            foreach (var t in e.Campos.Where(c => c.Tipo == Campo.Trilha && c.Rende == alvo))
                rendidas += Contador(p, e.Id, t.Id) / PontosPorEscolha;
            return rendidas;
        }

        /// <summary>Escolhas de jogo ainda por fazer neste campo.</summary>
        public int EscolhasPendentesNoJogo(Personagem p, string extra, string campo) =>
            System.Math.Max(0, EscolhasRendidas(p, extra, campo) - EscolhasNoJogo(p, extra, campo).Count);

        /// <summary>Soma ou tira pontos de uma trilha. Não tira o que já virou escolha.</summary>
        public Resultado AjustarContador(string id, string extra, string campo, int delta)
        {
            if (!NoJogo(id, out var p, out var erro))
                return erro;
            var c = BuscarCampo(p, extra, campo);
            if (c == null || (c.Tipo != Campo.Trilha && c.Tipo != Campo.Reserva))
                return Resultado.Falha("Contador desconhecido.");
            var novo = Contador(p, extra, campo) + delta;
            if (novo < 0)
                return Resultado.Falha(c.Tipo == Campo.Reserva ? "Sem reserva para gastar." : "A trilha não fica negativa.");
            if (c.Rende != null)
            {
                var partes = c.Rende.Split('|');
                if (novo / PontosPorEscolha < EscolhasNoJogo(p, partes[0], partes[1]).Count)
                    return Resultado.Falha("Esses pontos já viraram escolha: desfaça a escolha antes.");
            }
            p.Contadores[ChaveExtra(extra, campo)] = novo;
            Gravar();
            return Resultado.Sucesso();
        }

        // ---------- listas que crescem no jogo ----------

        /// <summary>Tudo o que foi tomado no campo: o da criação (foto) e o do jogo.</summary>
        public IReadOnlyList<string> TomadasAoTodo(Personagem p, string extra, string campo) =>
            Escolhas(p, extra, campo).Concat(EscolhasNoJogo(p, extra, campo)).ToList();

        /// <summary>A opção final libera com todas as outras tomadas, ou pelo avanço indicado.</summary>
        public bool FinalLiberado(Personagem p, string extra, Campo c)
        {
            if (c.Final == null)
                return true;
            var tomadas = TomadasAoTodo(p, extra, c.Id).Count(o => o != c.Final);
            var outras = c.Opcoes.Count(o => o.Id != c.Final);
            return tomadas >= outras || (c.FinalLiberaComAvanco != null && AvancoMarcado(p, c.FinalLiberaComAvanco));
        }

        public Resultado AlternarEscolhaNoJogo(string id, string extra, string campo, string opcao)
        {
            if (!NoJogo(id, out var p, out var erro))
                return erro;
            var c = BuscarCampo(p, extra, campo);
            if (c == null || !c.CresceNoJogo || c.Opcoes.All(o => o.Id != opcao))
                return Resultado.Falha("Escolha de jogo desconhecida.");
            if (Escolhas(p, extra, campo).Contains(opcao))
                return Resultado.Falha("Essa já veio da criação.");

            var chave = ChaveExtra(extra, campo);
            if (!p.EscolhasDeJogo.TryGetValue(chave, out var lista))
                p.EscolhasDeJogo[chave] = lista = new List<string>();
            if (!lista.Remove(opcao))
            {
                if (EscolhasPendentesNoJogo(p, extra, campo) == 0)
                    return Resultado.Falha("Nada pendente: a cada 5 na trilha, uma escolha nova.");
                if (opcao == c.Final && !FinalLiberado(p, extra, c))
                    return Resultado.Falha("Ainda não: só com todos os outros tomados, ou pelo avanço.");
                lista.Add(opcao);
            }
            Gravar();
            return Resultado.Sucesso();
        }

        // ---------- condições (uso do santuário) ----------

        public bool CondicaoConcluida(Personagem p, string extra, string campo, string opcao) =>
            p.EscolhasDeJogo.TryGetValue(Sub(extra, campo, Concluidas), out var v) && v.Contains(opcao);

        /// <summary>O mestre seleciona/tira uma condição. Tirar apaga a conclusão e a lacuna dela.</summary>
        public Resultado AlternarCondicao(string id, string extra, string campo, string opcao)
        {
            if (!PodeMexerNaCondicao(id, extra, campo, opcao, out var p, out var erro))
                return erro;
            var chave = ChaveExtra(extra, campo);
            if (!p.EscolhasDeJogo.TryGetValue(chave, out var selecionadas))
                p.EscolhasDeJogo[chave] = selecionadas = new List<string>();
            if (selecionadas.Remove(opcao))
            {
                if (p.EscolhasDeJogo.TryGetValue(Sub(extra, campo, Concluidas), out var concluidas))
                    concluidas.Remove(opcao);
                p.TextosDeJogo.Remove(Sub(extra, campo, opcao));
            }
            else
            {
                selecionadas.Add(opcao);
            }
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado AlternarCondicaoConcluida(string id, string extra, string campo, string opcao)
        {
            if (!PodeMexerNaCondicao(id, extra, campo, opcao, out var p, out var erro))
                return erro;
            if (!EscolhasNoJogo(p, extra, campo).Contains(opcao))
                return Resultado.Falha("Selecione a condição antes de concluí-la.");
            var chave = Sub(extra, campo, Concluidas);
            if (!p.EscolhasDeJogo.TryGetValue(chave, out var concluidas))
                p.EscolhasDeJogo[chave] = concluidas = new List<string>();
            if (!concluidas.Remove(opcao))
                concluidas.Add(opcao);
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Texto de jogo: o pedido (sub = "pedido") ou a lacuna de uma condição (sub = id da condição).</summary>
        public Resultado DefinirTextoDeJogo(string id, string extra, string campo, string sub, string texto)
        {
            if (!NoJogo(id, out var p, out var erro))
                return erro;
            var c = BuscarCampo(p, extra, campo);
            if (c == null || c.Tipo != Campo.Condicoes)
                return Resultado.Falha("Campo de jogo desconhecido.");
            var chave = Sub(extra, campo, sub);
            var limpo = (texto ?? string.Empty).Trim();
            if ((p.TextosDeJogo.TryGetValue(chave, out var atual) ? atual : string.Empty) == limpo)
                return Resultado.Sucesso();
            if (limpo.Length == 0)
                p.TextosDeJogo.Remove(chave);
            else
                p.TextosDeJogo[chave] = limpo;
            Gravar();
            return Resultado.Sucesso();
        }

        bool PodeMexerNaCondicao(string id, string extra, string campo, string opcao, out Personagem p, out Resultado erro)
        {
            if (!NoJogo(id, out p, out erro))
                return false;
            var c = BuscarCampo(p, extra, campo);
            if (c == null || c.Tipo != Campo.Condicoes || c.Opcoes.All(o => o.Id != opcao))
            {
                erro = Resultado.Falha("Condição desconhecida.");
                return false;
            }
            return true;
        }

        // ---------- registros e itens ----------

        public IReadOnlyList<ItemDeJogo> ItensNoJogo(Personagem p, string extra, string campo) =>
            p.ItensDeJogo.TryGetValue(ChaveExtra(extra, campo), out var v) ? v : new List<ItemDeJogo>();

        List<ItemDeJogo> ListaDeItens(Personagem p, string extra, string campo)
        {
            var chave = ChaveExtra(extra, campo);
            if (!p.ItensDeJogo.TryGetValue(chave, out var lista))
                p.ItensDeJogo[chave] = lista = new List<ItemDeJogo>();
            return lista;
        }

        /// <summary>Lança um uso no registro (ex.: gastar controle). Se o campo consome uma reserva, gasta 1.</summary>
        public Resultado Lancar(string id, string extra, string campo, string opcao, string texto)
        {
            if (!NoJogo(id, out var p, out var erro))
                return erro;
            var c = BuscarCampo(p, extra, campo);
            if (c == null || c.Tipo != Campo.Registro || c.Opcoes.All(o => o.Id != opcao))
                return Resultado.Falha("Lançamento desconhecido.");
            if (c.Consome != null)
            {
                var gasto = AjustarContador(id, extra, c.Consome, -1);
                if (!gasto.Ok)
                    return Resultado.Falha("Sem controle para gastar.");
            }
            ListaDeItens(p, extra, campo).Add(new ItemDeJogo
            {
                Id = System.Guid.NewGuid().ToString("N"), Opcao = opcao, Texto = (texto ?? string.Empty).Trim(),
            });
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Desfaz um lançamento (devolve a reserva que ele gastou).</summary>
        public Resultado DesfazerLancamento(string id, string extra, string campo, string item)
        {
            if (!NoJogo(id, out var p, out var erro))
                return erro;
            var c = BuscarCampo(p, extra, campo);
            var lista = ListaDeItens(p, extra, campo);
            if (c == null || lista.RemoveAll(i => i.Id == item) == 0)
                return Resultado.Falha("Lançamento não encontrado.");
            if (c.Consome != null)
                p.Contadores[ChaveExtra(extra, c.Consome)] = Contador(p, extra, c.Consome) + 1;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Cria um objeto com cargas (ex.: um dispositivo alienígena).</summary>
        public Resultado CriarItem(string id, string extra, string campo, string nome, int cargas)
        {
            if (!NoJogo(id, out var p, out var erro))
                return erro;
            var c = BuscarCampo(p, extra, campo);
            if (c == null || c.Tipo != Campo.Itens)
                return Resultado.Falha("Lista de objetos desconhecida.");
            var limpo = (nome ?? string.Empty).Trim();
            if (limpo.Length == 0)
                return Resultado.Falha("Dê um nome ao objeto.");
            if (cargas < 1)
                return Resultado.Falha("Pelo menos 1 carga.");
            ListaDeItens(p, extra, campo).Add(new ItemDeJogo { Id = System.Guid.NewGuid().ToString("N"), Texto = limpo, Cargas = cargas });
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Gasta (delta −1) ou devolve (+1) uma carga do objeto.</summary>
        public Resultado AjustarCarga(string id, string extra, string campo, string item, int delta)
        {
            if (!NoJogo(id, out var p, out var erro))
                return erro;
            var objeto = ListaDeItens(p, extra, campo).FirstOrDefault(i => i.Id == item);
            if (objeto == null)
                return Resultado.Falha("Objeto não encontrado.");
            if (objeto.Cargas + delta < 0)
                return Resultado.Falha("Sem carga: o objeto já apagou.");
            objeto.Cargas += delta;
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado DescartarItem(string id, string extra, string campo, string item)
        {
            if (!NoJogo(id, out var p, out var erro))
                return erro;
            if (ListaDeItens(p, extra, campo).RemoveAll(i => i.Id == item) == 0)
                return Resultado.Falha("Objeto não encontrado.");
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Fim de cena: as reservas que somem no fim da cena voltam a zero (ex.: chamas do Nova).</summary>
        public Resultado FimDeCena(string id)
        {
            if (!NoJogo(id, out var p, out var erro))
                return erro;
            foreach (var e in ExtrasDoArquetipo(p))
            foreach (var c in e.Campos.Where(c => c.Tipo == Campo.Reserva && c.ZeraFimDeCena))
                p.Contadores.Remove(ChaveExtra(e.Id, c.Id));
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Recursos de jogo (ex.: chamas) aparecem na ficha só em sala (em carreira) ou no modo teste.</summary>
        public bool MostraRecursosDeJogo(Personagem p) =>
            p.Status == StatusCriacao.EmCarreira || p.Status == StatusCriacao.Aposentado || EmTeste(p.Id);

        /// <summary>
        /// Operações de jogo valem depois de concluir a criação, ou a qualquer hora no modo teste
        /// (o teste é desfeito ao desligar, então pode validar a ficha ainda em "novo").
        /// </summary>
        public bool LiberadoNoJogo(Personagem p) => p.Status != StatusCriacao.Novo || EmTeste(p.Id);

        /// <summary>Leva um contador a um valor exato (digitado no playground), respeitando as mesmas regras.</summary>
        public Resultado DefinirContador(string id, string extra, string campo, int valor)
        {
            var p = Buscar(id);
            return p == null ? Resultado.Falha("Personagem não encontrada.")
                : AjustarContador(id, extra, campo, valor - Contador(p, extra, campo));
        }

        bool NoJogo(string id, out Personagem p, out Resultado erro)
        {
            p = Buscar(id);
            erro = p == null ? Resultado.Falha("Personagem não encontrada.")
                : !LiberadoNoJogo(p) ? Resultado.Falha("Isso é do jogo, depois de concluir a criação.")
                : Resultado.Sucesso();
            return erro.Ok;
        }

        /// <summary>Algum avanço marcado é a operação indicada (ex.: "enfrentar-perdicao").</summary>
        public bool AvancoMarcado(Personagem p, string operacao)
        {
            var avancos = AvancosDoArquetipo(p);
            foreach (var casa in p.AvancosMarcados)
            {
                var partes = casa.Split(':');
                if (!int.TryParse(partes[1], out var k))
                    continue;
                var lista = partes[0] == Cima ? avancos.Cima : avancos.Baixo;
                if (k < lista.Count && lista[k].Id == operacao)
                    return true;
            }
            return false;
        }
    }
}
