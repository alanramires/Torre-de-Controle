using System.Collections.Generic;
using System.Linq;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// "Ligado a": o mestre liga um NPC que ele criou (ou copiou do laboratório) a heroínas da campanha, com
    /// uma linha dizendo como. É o outro caminho do livro para o vilão da primeira sessão (cap. 8): "crie um
    /// vinculado a pelo menos uma PJ". O laço de quem nasceu da ficha continua sendo o vínculo; este é do mestre.
    /// </summary>
    public sealed partial class CadastroPersonagens
    {
        /// <summary>As heroínas ativas da sala a que o NPC está ligado pelo mestre, com o "como".</summary>
        public IReadOnlyList<(Personagem heroi, string como)> LigacoesDoNpc(Personagem npc)
        {
            var ligacoes = npc?.Pnj?.Ligacoes;
            if (ligacoes == null || npc.Sala == null)
                return new List<(Personagem, string)>();
            var herois = HeroisDaSala(npc.Sala).Where(h => h.Ativo).ToList();
            return ligacoes.Select(l => (heroi: herois.FirstOrDefault(h => h.Id == l.Personagem), como: l.Como ?? string.Empty))
                .Where(x => x.heroi != null).ToList();
        }

        /// <summary>Nasceu da ficha de uma heroína ou o mestre o ligou a uma.</summary>
        public bool LigadoAHeroina(Personagem npc) => OrigemDoNpc(npc) != null || LigacoesDoNpc(npc).Count > 0;

        /// <summary>Liga ou desliga o NPC de uma heroína da campanha. Desligar esquece o "como".</summary>
        public Resultado AlternarLigacao(string npcId, string heroiId)
        {
            if (!AcharNpc(npcId, out var npc, out var erro))
                return erro;
            if (npc.Sala == null)
                return Resultado.Falha("No laboratório não há heroínas: ligue na campanha.");
            if (!HeroisDaSala(npc.Sala).Any(h => h.Id == heroiId))
                return Resultado.Falha("Essa heroína não está nesta campanha.");
            npc.Pnj.Ligacoes ??= new List<LigacaoNpc>();
            if (npc.Pnj.Ligacoes.RemoveAll(l => l.Personagem == heroiId) == 0)
                npc.Pnj.Ligacoes.Add(new LigacaoNpc { Personagem = heroiId });
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>O "como" de uma ligação (ex.: "destruiu o laboratório onde ela foi criada").</summary>
        public Resultado DescreverLigacao(string npcId, string heroiId, string como)
        {
            if (!AcharNpc(npcId, out var npc, out var erro))
                return erro;
            var ligacao = npc.Pnj.Ligacoes?.FirstOrDefault(l => l.Personagem == heroiId);
            if (ligacao == null)
                return Resultado.Falha("Ligue o NPC à heroína antes de dizer como.");
            var limpo = (como ?? string.Empty).Trim();
            if (ligacao.Como == limpo)
                return Resultado.Sucesso();
            ligacao.Como = limpo;
            Gravar();
            return Resultado.Sucesso();
        }
    }
}
