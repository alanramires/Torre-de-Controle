using System;
using System.Collections.Generic;
using System.Diagnostics;
using TorreDeControle.Core.Armazenamento;

namespace TorreDeControle.Infra
{
    /// <summary>
    /// Junta as gravações: cada chave agendada grava uma vez só, depois de <c>espera</c> sem nova
    /// mudança (cliques seguidos viram uma gravação). Tudo na thread principal: quem chama
    /// <see cref="Pulsar"/> é a tela, a cada quadro; ao sair (fechar, perder o foco) chama
    /// <see cref="Descarregar"/> e nada fica para trás.
    /// </summary>
    public sealed class ArmazenamentoAdiado : IArmazenamento, IGravacaoAdiada
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
            var lote = new List<KeyValuePair<string, Func<string>>>(pendentes);
            pendentes.Clear();
            foreach (var item in lote)
                destino.Gravar(item.Key, item.Value());
        }
    }
}
