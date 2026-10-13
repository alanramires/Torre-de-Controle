using System.Collections.Generic;
using TorreDeControle.Core.Salas;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// Os valores de jogo que a mesa muda (potencial, condições, Rótulos), lidos e escritos por campo.
    /// Quem valida é a Mesa (pela regra); aqui só se lê, aplica e grava.
    /// </summary>
    public sealed partial class CadastroPersonagens
    {
        public int ValorDeJogo(Personagem p, string campo)
        {
            if (campo == Mesa.CampoPotencial)
                return p.Potencial;
            if (campo.StartsWith("condicao:"))
                return p.CondicoesMarcadas.Contains(campo.Substring("condicao:".Length)) ? 1 : 0;
            if (campo.StartsWith("rotulo:"))
                return RotulosAtuais(p).TryGetValue(campo.Substring("rotulo:".Length), out var v) ? v : 0;
            return 0;
        }

        internal void AplicarDeJogo(Personagem p, IEnumerable<Mudanca> mudancas)
        {
            foreach (var m in mudancas)
            {
                if (m.Campo == Mesa.CampoPotencial)
                    p.Potencial = m.Depois;
                else if (m.Campo.StartsWith("condicao:"))
                {
                    var c = m.Campo.Substring("condicao:".Length);
                    p.CondicoesMarcadas.Remove(c);
                    if (m.Depois > 0)
                    {
                        p.CondicoesMarcadas.Add(c);
                        p.CondicoesMarcadas.Sort((a, b) => IndiceDaCondicao(a).CompareTo(IndiceDaCondicao(b)));
                    }
                }
                else if (m.Campo.StartsWith("rotulo:"))
                {
                    // Primeira mudança de jogo: parte da foto da criação.
                    p.RotulosJogo ??= new Dictionary<string, int>(RotulosAtuais(p));
                    p.RotulosJogo[m.Campo.Substring("rotulo:".Length)] = m.Depois;
                }
            }
            Gravar();
        }

        static int IndiceDaCondicao(string c)
        {
            for (var k = 0; k < Condicoes.Todas.Count; k++)
                if (Condicoes.Todas[k] == c)
                    return k;
            return int.MaxValue;
        }
    }
}
