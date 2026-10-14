using System.Linq;
using TorreDeControle.Core.Personagens;

namespace TorreDeControle.Tests
{
    /// <summary>Preenche a ficha de criação a partir do JSON, até ela poder ser concluída.</summary>
    static class FichaPronta
    {
        /// <summary>
        /// Cumpre os requisitos da campanha (formação enviada, Influência pela regra) e aprova a instância
        /// para em carreira. A ficha pronta é uma Nova: escolhe "cara fechada" (1 colega, ou 0 se sozinha).
        /// </summary>
        public static void ParaCarreira(TorreDeControle.Core.Salas.CadastroSalas salas, CadastroPersonagens personagens,
            TorreDeControle.Core.Salas.Sala sala, Personagem copia)
        {
            salas.EnviarFormacao(sala.Id, copia.Id, "a formação da " + copia.NomeExibicao);
            personagens.EscolherComportamento(copia.Id, "cara_fechada");
            var quantas = personagens.QuantasInfluencias(copia) ?? 0;
            personagens.DarInfluencia(copia.Id, personagens.Colegas(copia).Take(quantas).Select(c => c.Id));
            NUnit.Framework.Assert.That(personagens.AprovarParaCampanha(copia.Id).Ok, NUnit.Framework.Is.True);
        }

        /// <summary>Uma Nova criada (foto congelada) no laboratório.</summary>
        public static Personagem Criada(CadastroPersonagens cadastro, string codinome, string jogador = null)
        {
            cadastro.Criar("", codinome, "nova", jogador);
            var p = cadastro.Listar().Last();
            cadastro.EscolherBonusLivre(p.Id, Rotulos.Superior);
            cadastro.AlternarHabilidade(p.Id, "feiticaria");
            for (var k = 0; k < 5; k++)
                cadastro.ResponderHistorico(p.Id, k, "resposta " + k);
            CompletarModulos(cadastro, p);
            NUnit.Framework.Assert.That(cadastro.Aprovar(p.Id).Ok, NUnit.Framework.Is.True);
            return p;
        }

        public static void CompletarModulos(CadastroPersonagens cadastro, Personagem p)
        {
            var mov = cadastro.MovimentosDoArquetipo(p);
            foreach (var o in mov.Opcoes.Where(o => !mov.Obrigatorios.Contains(o.Id)).Take(mov.Escolher))
                cadastro.AlternarMovimento(p.Id, o.Id);
            var secoes = cadastro.ExtrasDoArquetipo(p).Select(e => (e.Id, e.Campos))
                .Concat(cadastro.MovimentosComSubsecao(p).Select(m => (CadastroPersonagens.ExtraDoMovimento(m.Id), m.Campos)))
                .ToList();
            foreach (var (extra, campos) in secoes)
            foreach (var c in campos)
            {
                switch (c.Tipo)
                {
                    case "lista":
                        foreach (var o in c.Opcoes.Take(c.Escolher ?? 1))
                        {
                            cadastro.AlternarOpcaoDeExtra(p.Id, extra, c.Id, o.Id);
                            if (c.Descrever)
                                cadastro.DefinirTextoDeExtra(p.Id, extra, c.Id, o.Id, "descrição");
                        }
                        break;
                    case "rotulo":
                        cadastro.AlternarOpcaoDeExtra(p.Id, extra, c.Id, c.Rotulos[0]);
                        break;
                    case "texto" when !c.NaSala:
                        cadastro.DefinirTextoDeExtra(p.Id, extra, c.Id, null, "texto");
                        break;
                    case "textos":
                        foreach (var v in c.Vagas.Take(c.Minimo))
                            cadastro.DefinirTextoDeExtra(p.Id, extra, c.Id, v.Id, "nome");
                        break;
                }
            }
        }
    }
}
