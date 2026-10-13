using System;
using System.Collections.Generic;
using System.Linq;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// NPCs (controle do mestre). Mesma entidade da personagem: sem arquétipo nem ciclo de criação;
    /// no lugar, o perfil PNJ e, se for vilão, a ficha de vilão. Por ora nascem direto na sala.
    /// </summary>
    public sealed partial class CadastroPersonagens
    {
        /// <summary>NPCs da sala: ativos primeiro, na ordem que o mestre arrastou.</summary>
        public IReadOnlyList<Personagem> NpcsDaSala(string sala) =>
            Ordenacao.Ordenar(personagens.Where(p => p.Sala == sala && p.Controle == Controle.Mestre), p => p.NomeExibicao);

        /// <summary>Heróis da sala (as cópias), inclusive os tirados das vagas.</summary>
        public IReadOnlyList<Personagem> HeroisDaSala(string sala) =>
            personagens.Where(p => p.Sala == sala && p.Controle == Controle.Jogador).ToList();

        public Resultado CriarNpc(string sala, string nome, Maturidade maturidade, Papel papel)
        {
            if (string.IsNullOrWhiteSpace(sala))
                return Resultado.Falha("NPC nasce numa sala.");
            var limpo = (nome ?? string.Empty).Trim();
            if (limpo.Length == 0)
                return Resultado.Falha("Dê um nome ao NPC.");

            var npc = new Personagem
            {
                Id = Guid.NewGuid().ToString("N"),
                Identidade = new Identidade { Nome = limpo },
                Controle = Controle.Mestre,
                Maturidade = maturidade,
                Sala = sala,
                Pnj = new PerfilPnj(),
                Ordem = Ordenacao.Fim(personagens.Where(p => p.Sala == sala && p.Controle == Controle.Mestre)),
            };
            DefinirPapelEm(npc, papel);
            personagens.Add(npc);
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Nome (o real, ou o único) e codinome / nome de vilão (opcional).</summary>
        public Resultado RenomearNpc(string id, string nome, string codinome)
        {
            if (!AcharNpc(id, out var p, out var erro))
                return erro;
            var n = (nome ?? string.Empty).Trim();
            var c = (codinome ?? string.Empty).Trim();
            if (n.Length == 0 && c.Length == 0)
                return Resultado.Falha("Dê pelo menos um nome ou um codinome.");
            if (n == p.Identidade.Nome && c == p.Identidade.Codinome)
                return Resultado.Sucesso();
            p.Identidade.Nome = n;
            p.Identidade.Codinome = c;
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado DefinirPerfilPnj(string id, string modoDeAgir, string motivacao)
        {
            if (!AcharNpc(id, out var p, out var erro))
                return erro;
            var m = (modoDeAgir ?? string.Empty).Trim();
            var mot = (motivacao ?? string.Empty).Trim();
            if (m == p.Pnj.ModoDeAgir && mot == p.Pnj.Motivacao)
                return Resultado.Sucesso();
            p.Pnj.ModoDeAgir = m;
            p.Pnj.Motivacao = mot;
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado DefinirMaturidade(string id, Maturidade maturidade)
        {
            if (!AcharNpc(id, out var p, out var erro))
                return erro;
            p.Maturidade = maturidade;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Papel vilão dá a ficha de vilão (promover). Voltar o papel guarda a ficha, não apaga.</summary>
        public Resultado DefinirPapel(string id, Papel papel)
        {
            if (!AcharNpc(id, out var p, out var erro))
                return erro;
            DefinirPapelEm(p, papel);
            Gravar();
            return Resultado.Sucesso();
        }

        static void DefinirPapelEm(Personagem p, Papel papel)
        {
            p.Papel = papel;
            if (papel == Papel.Vilao)
                p.Vilao ??= new FichaVilao();
        }

        public Resultado DefinirGeracao(string id, string geracao)
        {
            if (!AcharVilao(id, out var p, out var erro))
                return erro;
            if (geracao != null && Geracoes.Buscar(geracao) == null)
                return Resultado.Falha("Geração desconhecida.");
            p.Vilao.Geracao = geracao;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// Escreve o movimento de vilão de número <paramref name="indice"/>; o índice logo depois do
        /// último acrescenta um novo. Texto vazio tira o movimento (é texto do mestre, não histórico).
        /// </summary>
        public Resultado DefinirMovimentoDeVilao(string id, int indice, string texto)
        {
            if (!AcharVilao(id, out var p, out var erro))
                return erro;
            var movimentos = p.Vilao.Movimentos;
            if (indice < 0 || indice > movimentos.Count)
                return Resultado.Falha("Movimento inexistente.");
            var limpo = (texto ?? string.Empty).Trim();
            if (indice == movimentos.Count)
            {
                if (limpo.Length == 0)
                    return Resultado.Sucesso();
                movimentos.Add(limpo);
            }
            else if (limpo.Length == 0)
                movimentos.RemoveAt(indice);
            else if (movimentos[indice] == limpo)
                return Resultado.Sucesso();
            else
                movimentos[indice] = limpo;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Liga/desliga uma das condições que o vilão tem (de 1 a 5).</summary>
        public Resultado AlternarCondicaoDeVilao(string id, string condicao)
        {
            if (!AcharVilao(id, out var p, out var erro))
                return erro;
            if (!Condicoes.Todas.Contains(condicao))
                return Resultado.Falha("Condição desconhecida.");
            var lista = p.Vilao.Condicoes;
            if (lista.Contains(condicao))
                lista.Remove(condicao);
            else
            {
                lista.Add(condicao);
                // Na ordem da ficha, não na ordem do clique.
                lista.Sort((a, b) => Condicoes.Todas.ToList().IndexOf(a).CompareTo(Condicoes.Todas.ToList().IndexOf(b)));
            }
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>O que falta na ficha do NPC. Só aviso: NPC não tem ciclo de criação, nada trava.</summary>
        public IReadOnlyList<string> AvisosDoNpc(Personagem p)
        {
            var avisos = new List<string>();
            if (p.Pnj == null)
                return avisos;
            if (string.IsNullOrWhiteSpace(p.Pnj.Motivacao))
                avisos.Add("Sem motivação: é ela que diz o que o NPC faz.");
            if (p.Papel == Papel.Vilao && p.Vilao != null)
            {
                if (p.Vilao.Geracao == null)
                    avisos.Add("Vilão sem geração.");
                if (p.Vilao.Movimentos.Count == 0)
                    avisos.Add("Vilão sem movimentos de vilão (o livro sugere três ou mais).");
                if (p.Vilao.Condicoes.Count == 0)
                    avisos.Add("Vilão sem condições: escolha de 1 a 5.");
            }
            return avisos;
        }

        bool AcharNpc(string id, out Personagem p, out Resultado erro)
        {
            p = Buscar(id);
            if (p == null || p.Controle != Controle.Mestre)
            {
                erro = Resultado.Falha("NPC não encontrado.");
                return false;
            }
            p.Pnj ??= new PerfilPnj();
            erro = Resultado.Sucesso();
            return true;
        }

        bool AcharVilao(string id, out Personagem p, out Resultado erro)
        {
            if (!AcharNpc(id, out p, out erro))
                return false;
            if (p.Papel != Papel.Vilao || p.Vilao == null)
            {
                erro = Resultado.Falha("Só vilão tem ficha de vilão.");
                return false;
            }
            return true;
        }
    }
}
