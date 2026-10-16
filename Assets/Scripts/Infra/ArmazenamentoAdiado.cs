using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TorreDeControle.Core.Armazenamento;

namespace TorreDeControle.Infra
{
    /// <summary>
    /// Junta as gravações: cada chave agendada grava uma vez só, depois de <c>espera</c> sem nova
    /// mudança (cliques seguidos viram uma gravação). Tudo na thread principal: quem chama
    /// <see cref="Pulsar"/> é a tela, a cada quadro; ao sair (fechar, perder o foco) chama
    /// <see cref="Descarregar"/> e nada fica para trás.
    /// </summary>
    public sealed class ArmazenamentoAdiado : IArmazenamento, IGravacaoAdiada, ICopiaDeSeguranca
    {
        readonly IArmazenamento destino;
        readonly long espera;
        readonly Dictionary<string, Func<string>> pendentes = new Dictionary<string, Func<string>>();
        readonly Stopwatch desdeAUltima = new Stopwatch();

        public ArmazenamentoAdiado(IArmazenamento destino, int esperaEmMs = 500)
        {
            this.destino = destino;
            espera = esperaEmMs;
        }

        public bool TemPendencia => pendentes.Count > 0;

        public string Ler(string chave) =>
            pendentes.TryGetValue(chave, out var gerar) ? gerar() : destino.Ler(chave);

        public void Gravar(string chave, string conteudo) => Agendar(chave, () => conteudo);

        public void Agendar(string chave, Func<string> conteudo)
        {
            pendentes[chave] = conteudo;
            desdeAUltima.Restart();
        }

        /// <summary>Chamado a cada quadro: grava se a última mudança já tem <c>espera</c> de idade.</summary>
        public void Pulsar()
        {
            if (pendentes.Count > 0 && desdeAUltima.ElapsedMilliseconds >= espera)
                Descarregar();
        }

        /// <summary>Grava agora tudo o que está pendente.</summary>
        public void Descarregar()
        {
            if (pendentes.Count == 0)
                return;
            // Cada chave só sai da fila depois de gravada: se uma gravação falhar, ela e as que vinham depois
            // continuam pendentes (a próxima pulsação tenta de novo) e o erro sobe para quem chamou.
            var lote = new List<KeyValuePair<string, Func<string>>>(pendentes);
            if (destino is IGravacaoEmLote emLote)
            {
                // Tudo o que está pendente (ficha, mesa, salas...) vira uma operação só.
                var itens = lote.Select(i => new KeyValuePair<string, string>(i.Key, i.Value())).ToList();
                emLote.GravarLote(itens);
                foreach (var item in lote)
                    if (pendentes.TryGetValue(item.Key, out var atual) && atual == item.Value)
                        pendentes.Remove(item.Key); // se foi reagendada no meio, fica a versão nova
                return;
            }
            foreach (var item in lote)
            {
                destino.Gravar(item.Key, item.Value());
                if (pendentes.TryGetValue(item.Key, out var atual) && atual == item.Value)
                    pendentes.Remove(item.Key); // se foi reagendada no meio, fica a versão nova
            }
        }

        /// <summary>Cópia de segurança: grava o pendente primeiro, para a cópia sair com o estado de agora.</summary>
        public void CopiaDeSeguranca(string rotulo)
        {
            Descarregar();
            (destino as ICopiaDeSeguranca)?.CopiaDeSeguranca(rotulo);
        }
    }
}
