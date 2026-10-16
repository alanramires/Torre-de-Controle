using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TorreDeControle.Core.Armazenamento;

namespace TorreDeControle.Infra
{
    /// <summary>
    /// Fase 1: cada chave vira <c>&lt;pasta&gt;/&lt;chave&gt;.json</c>.
    /// Único lugar do projeto que usa System.IO (WebGL não grava arquivo local).
    /// </summary>
    public sealed class ArmazenamentoLocalJson : IArmazenamento, ICopiaDeSeguranca, IGravacaoEmLote
    {
        readonly string pasta;

        public ArmazenamentoLocalJson(string pasta)
        {
            this.pasta = pasta;
        }

        public string Pasta => pasta;

        public string Ler(string chave)
        {
            var caminho = Caminho(chave);
            return File.Exists(caminho) ? File.ReadAllText(caminho, Encoding.UTF8) : null;
        }

        public void Gravar(string chave, string conteudo)
        {
            Directory.CreateDirectory(pasta);
            var caminho = Caminho(chave);
            var temporario = caminho + ".tmp";

            // Grava ao lado e troca: se a Torre cair no meio, o arquivo antigo continua inteiro.
            File.WriteAllText(temporario, conteudo, new UTF8Encoding(false));
            if (File.Exists(caminho))
                File.Replace(temporario, caminho, null);
            else
                File.Move(temporario, caminho);
        }

        const string Diario = "_lote_em_andamento.txt";
        const string SufixoNovo = ".novo";

        /// <summary>
        /// Grava várias chaves como uma operação só. 1) escreve todas as versões novas ao lado (".novo");
        /// 2) grava o diário com a lista; 3) troca uma a uma; 4) apaga o diário. Se a Torre cair entre 2 e 4,
        /// <see cref="RecuperarLoteInterrompido"/> termina a troca ao abrir: os ".novo" já estão completos.
        /// Se cair antes do diário, nada foi trocado e os ".novo" soltos são descartados.
        /// </summary>
        public void GravarLote(IReadOnlyList<KeyValuePair<string, string>> itens)
        {
            if (itens.Count == 0)
                return;
            if (itens.Count == 1)
            {
                Gravar(itens[0].Key, itens[0].Value);
                return;
            }
            Directory.CreateDirectory(pasta);
            // Um lote anterior já decidido (diário presente) e não terminado: termina antes de preparar outro.
            // Sem isso, a nova tentativa reescreveria os ".novo" de que o diário antigo ainda depende. Se terminar
            // falhar, o erro sobe e este lote nem começa (a fila guarda as pendências).
            if (File.Exists(Path.Combine(pasta, Diario)))
                RecuperarLoteInterrompido();
            foreach (var item in itens)
                File.WriteAllText(Caminho(item.Key) + SufixoNovo, item.Value, new UTF8Encoding(false));
            var diario = Path.Combine(pasta, Diario);
            File.WriteAllText(diario + ".tmp", string.Join("\n", itens.Select(i => i.Key)), new UTF8Encoding(false));
            if (File.Exists(diario))
                File.Delete(diario);
            File.Move(diario + ".tmp", diario); // a partir daqui o lote está "decidido"
            Trocar(itens.Select(i => i.Key));
            File.Delete(diario);
        }

        /// <summary>
        /// Chamado ao abrir, antes de ler qualquer coisa: termina um lote interrompido (há diário) ou descarta
        /// versões novas soltas de um lote que não chegou a ser decidido (sem diário). Devolve se terminou um.
        /// </summary>
        public bool RecuperarLoteInterrompido()
        {
            if (!Directory.Exists(pasta))
                return false;
            var diario = Path.Combine(pasta, Diario);
            if (File.Exists(diario))
            {
                var chaves = File.ReadAllText(diario, Encoding.UTF8)
                    .Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                Trocar(chaves);
                File.Delete(diario);
                return true;
            }
            foreach (var solto in Directory.GetFiles(pasta, "*.json" + SufixoNovo))
                File.Delete(solto);
            return false;
        }

        void Trocar(IEnumerable<string> chaves)
        {
            foreach (var chave in chaves)
            {
                var caminho = Caminho(chave);
                var novo = caminho + SufixoNovo;
                if (!File.Exists(novo))
                    continue; // já trocado antes da interrupção
                if (File.Exists(caminho))
                    File.Replace(novo, caminho, null);
                else
                    File.Move(novo, caminho);
            }
        }

        const int CopiasGuardadas = 15;

        /// <summary>
        /// Copia os .json da pasta para <c>backups/&lt;data&gt; &lt;rótulo&gt;/</c> e guarda as últimas
        /// <see cref="CopiasGuardadas"/>. Restaurar é copiar de volta com a Torre fechada.
        /// </summary>
        public void CopiaDeSeguranca(string rotulo)
        {
            if (!Directory.Exists(pasta))
                return;
            var raiz = Path.Combine(pasta, "backups");
            var limpo = string.Concat((rotulo ?? "copia").Split(Path.GetInvalidFileNameChars()));
            var nome = System.DateTime.Now.ToString("yyyy-MM-dd_HHmmss") + " " + limpo;
            var destino = Path.Combine(raiz, nome);
            for (var n = 2; Directory.Exists(destino); n++)
                destino = Path.Combine(raiz, nome + " (" + n + ")"); // nunca sobrescreve outra cópia
            Directory.CreateDirectory(destino);
            foreach (var arquivo in Directory.GetFiles(pasta, "*.json"))
                File.Copy(arquivo, Path.Combine(destino, Path.GetFileName(arquivo)), false);
            // Marca de cópia completa: pasta sem este arquivo foi interrompida e não serve para restaurar.
            File.WriteAllText(Path.Combine(destino, "COPIA_COMPLETA.txt"), System.DateTime.Now.ToString("s"));
            // Retenção: as últimas cópias **completas**; as interrompidas (sem o marcador) mais antigas que a
            // última completa saem, e não ocupam o lugar das boas.
            var todas = Directory.GetDirectories(raiz);
            System.Array.Sort(todas);
            var completas = todas.Where(d => File.Exists(Path.Combine(d, "COPIA_COMPLETA.txt"))).ToList();
            var incompletas = todas.Except(completas).ToList(); // separadas antes de apagar qualquer coisa
            var ultimaCompleta = completas.Count > 0 ? completas[completas.Count - 1] : null;
            for (var k = 0; k < completas.Count - CopiasGuardadas; k++)
                Directory.Delete(completas[k], true);
            foreach (var d in incompletas)
                if (ultimaCompleta != null && string.CompareOrdinal(d, ultimaCompleta) < 0)
                    Directory.Delete(d, true);
        }

        string Caminho(string chave) => Path.Combine(pasta, chave + ".json");
    }
}
