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
            // Relacionamento é entre membros da equipe: sozinha na sala, ganha uma colega na última vaga livre.
            if (personagens.Colegas(copia).Count == 0)
            {
                var livre = sala.Slots.FindLastIndex(s => s.Personagem == null && s.Jogador == null);
                var colega = Criada(personagens, "Colega de " + copia.NomeExibicao);
                salas.PorPersonagem(sala.Id, livre, colega.Id, false);
            }
            // A Nova escolhe "cara fechada"; os outros arquétipos têm outra regra de Influência (a chamada falha e não muda nada).
            personagens.EscolherComportamento(copia.Id, "cara_fechada");
            var quantas = personagens.QuantasInfluencias(copia) ?? 0;
            personagens.DarInfluencia(copia.Id, personagens.Colegas(copia).Take(quantas).Select(c => c.Id));
            var primeiro = personagens.OpcoesDeRelacionamento(copia).First();
            for (var k = 0; k < personagens.RegraDoArquetipo(copia).Equipe.Relacionamentos.Count; k++)
                personagens.DefinirRelacionamento(copia.Id, k, primeiro.Id);
            NUnit.Framework.Assert.That(personagens.AprovarParaCampanha(copia.Id).Ok, NUnit.Framework.Is.True);
        }

        /// <summary>
        /// Os arquétipos das fichas prontas, na ordem: a primeira de cada teste é uma Nova (vários testes contam com isso);
        /// as seguintes mudam, porque uma equipe não repete arquétipo (livro p.16). Só os de habilidade "escolha N da lista",
        /// que se preenchem de forma genérica.
        /// </summary>
        static readonly string[] Rodizio = { "nova", "farol", "janus", "delinquente", "transformado", "forasteira", "amaldicoada" };

        /// <summary>
        /// Uma ficha criada (foto congelada) no laboratório. Sem <paramref name="arquetipo"/>, a primeira do cadastro é uma
        /// Nova e as seguintes seguem o rodízio, para caberem juntas numa sala.
        /// </summary>
        public static Personagem Criada(CadastroPersonagens cadastro, string codinome, string jogador = null, string arquetipo = null)
        {
            arquetipo ??= Rodizio[cadastro.Listar().Count(x => x.Controle == Controle.Jogador) % Rodizio.Length];
            cadastro.Criar("", codinome, arquetipo, jogador);
            var p = cadastro.Listar().Last();
            if (arquetipo == "nova")
            {
                cadastro.EscolherBonusLivre(p.Id, Rotulos.Superior);
                cadastro.AlternarHabilidade(p.Id, "feiticaria");
            }
            else
            {
                foreach (var r in Rotulos.Todos)
                    if (cadastro.EscolherBonusLivre(p.Id, r).Ok)
                        break;
                var regra = cadastro.RegraDeHabilidades(p);
                foreach (var o in regra.Opcoes.Take(regra.Escolher ?? regra.EscolherAte ?? 1))
                {
                    cadastro.AlternarHabilidade(p.Id, o.Id);
                    cadastro.DescreverHabilidade(p.Id, o.Id, "como se manifesta");
                }
            }
            for (var k = 0; k < 5; k++)
                cadastro.ResponderHistorico(p.Id, k, "resposta " + k);
            CompletarModulos(cadastro, p);
            var aprovada = cadastro.Aprovar(p.Id);
            NUnit.Framework.Assert.That(aprovada.Ok, NUnit.Framework.Is.True, arquetipo + ": " + aprovada.Erro);
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
