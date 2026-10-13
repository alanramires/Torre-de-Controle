using System;
using System.Collections.Generic;
using System.Linq;

namespace TorreDeControle.Core
{
    /// <summary>Item de cadastro que o mestre ordena à mão, em dois blocos: ativos e inativos.</summary>
    public interface IOrdenavel
    {
        string Id { get; }
        bool Ativo { get; }
        int Ordem { get; set; }
    }

    /// <summary>
    /// Ordem manual dos cadastros: ativos primeiro, cada bloco na ordem que o mestre arrastou.
    /// Empate de ordem (dados antigos, sem ordem gravada) desempata pelo nome.
    /// </summary>
    public static class Ordenacao
    {
        public static List<T> Ordenar<T>(IEnumerable<T> itens, Func<T, string> nome) where T : IOrdenavel =>
            itens
                .OrderByDescending(i => i.Ativo)
                .ThenBy(i => i.Ordem)
                .ThenBy(nome, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

        /// <summary>Próxima posição: o fim da lista (item novo, ou que mudou de bloco).</summary>
        public static int Fim<T>(IEnumerable<T> itens) where T : IOrdenavel =>
            itens.Any() ? itens.Max(i => i.Ordem) + 1 : 0;

        /// <summary>
        /// Move o item para <paramref name="indice"/> dentro do próprio bloco e renumera o bloco
        /// (a ordem gravada passa a ser exatamente a que aparece na tela).
        /// </summary>
        public static void Mover<T>(IEnumerable<T> itens, T item, int indice, Func<T, string> nome) where T : IOrdenavel
        {
            var bloco = Ordenar(itens.Where(i => i.Ativo == item.Ativo), nome);
            bloco.Remove(item);
            bloco.Insert(Math.Max(0, Math.Min(indice, bloco.Count)), item);
            for (var k = 0; k < bloco.Count; k++)
                bloco[k].Ordem = k;
        }
    }
}
