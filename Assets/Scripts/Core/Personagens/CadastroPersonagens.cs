using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TorreDeControle.Core.Armazenamento;
using TorreDeControle.Core.Regras;

namespace TorreDeControle.Core.Personagens
{
    /// <summary>
    /// Laboratório de personagens (fora de sala): criar, editar a identidade, ativar/inativar,
    /// montar a ficha de criação (hoje: o +1 livre) e aprovar. Toda mudança grava na hora.
    /// </summary>
    public sealed partial class CadastroPersonagens
    {
        public const string Chave = "personagens";
        const int VersaoArquivo = 1;

        readonly IArmazenamento armazenamento;
        readonly RegrasArquetipos regras;
        readonly List<Personagem> personagens;

        CadastroPersonagens(IArmazenamento armazenamento, RegrasArquetipos regras, List<Personagem> personagens)
        {
            this.armazenamento = armazenamento;
            this.regras = regras;
            this.personagens = personagens;
        }

        public static CadastroPersonagens Carregar(IArmazenamento armazenamento, RegrasArquetipos regras)
        {
            var texto = armazenamento.Ler(Chave);
            var arquivo = string.IsNullOrWhiteSpace(texto)
                ? new Arquivo()
                : JsonConvert.DeserializeObject<Arquivo>(texto) ?? new Arquivo();
            var lista = arquivo.Personagens ?? new List<Personagem>();
            foreach (var p in lista)
                Normalizar(p);
            return new CadastroPersonagens(armazenamento, regras, lista);
        }

        /// <summary>Completa as coleções que um JSON antigo (ou de outra máquina) não trouxe.</summary>
        static void Normalizar(Personagem p)
        {
            p.Identidade ??= new Identidade();
            p.Criacao ??= new RascunhoCriacao();
            p.Criacao.Habilidades ??= new EscolhasHabilidades();
            p.Criacao.Movimentos ??= new List<string>();
            p.Criacao.ExtrasEscolhas ??= new Dictionary<string, List<string>>();
            p.Criacao.ExtrasTextos ??= new Dictionary<string, string>();
            p.AvancosMarcados ??= new List<string>();
            p.Cumpridos ??= new Dictionary<string, string>();
            p.Contadores ??= new Dictionary<string, int>();
            p.EscolhasDeJogo ??= new Dictionary<string, List<string>>();
            p.TextosDeJogo ??= new Dictionary<string, string>();
            p.ItensDeJogo ??= new Dictionary<string, List<ItemDeJogo>>();
            p.ObsMovimentos ??= new Dictionary<string, string>();
            p.CondicoesMarcadas ??= new List<string>();
            p.Relacoes ??= new List<Relacao>();
            foreach (var r in p.Relacoes)
            {
                r.Anteriores ??= new List<string>();
                r.Porque ??= string.Empty;
            }
            if (p.RelacionamentosAntigos != null)
            {
                if (p.Relacoes.Count == 0)
                    p.Relacoes = p.RelacionamentosAntigos.Select(id => new Relacao { Colega = id }).ToList();
                p.RelacionamentosAntigos = null;
            }
            p.Influencias ??= new Dictionary<string, EstadoInfluencia>();
            if (p.InfluenciaParaAntiga != null)
            {
                // Lista do formato anterior: quem estava nela recebeu Influência (dada).
                foreach (var id in p.InfluenciaParaAntiga.Where(id => !p.Influencias.ContainsKey(id)))
                    p.Influencias[id] = EstadoInfluencia.Dada;
                p.InfluenciaParaAntiga = null;
            }
            p.InfluenciaPorque ??= new Dictionary<string, string>();
            p.InfluenciaPorqueNinguem ??= string.Empty;
            if (p.Vilao != null)
            {
                p.Vilao.Movimentos ??= new List<string>();
                p.Vilao.Condicoes ??= new List<string>();
            }
        }

        /// <summary>Laboratório (fora de sala): ativas primeiro; cada bloco na ordem que o mestre arrastou.</summary>
        public IReadOnlyList<Personagem> Listar() => Ordenacao.Ordenar(DoLaboratorio, p => p.NomeExibicao);

        IEnumerable<Personagem> DoLaboratorio => personagens.Where(p => p.Sala == null);

        /// <summary>As cópias que estão (ou estiveram, se inativas) na sala.</summary>
        public IReadOnlyList<Personagem> DaSala(string sala) => personagens.Where(p => p.Sala == sala).ToList();

        /// <summary>Leva a personagem para a posição <paramref name="indice"/> do bloco dela (ativas ou inativas).</summary>
        public Resultado Mover(string id, int indice)
        {
            var personagem = Buscar(id);
            if (personagem == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (personagem.Sala == null)
            {
                var bloco = DoLaboratorio.Where(p => personagem.Favorito ? p.Favorito
                    : !p.Favorito && p.Ativo == personagem.Ativo)
                    .OrderBy(p => p.Ordem)
                    .ThenBy(p => p.NomeExibicao, StringComparer.CurrentCultureIgnoreCase).ToList();
                bloco.Remove(personagem);
                bloco.Insert(Math.Max(0, Math.Min(indice, bloco.Count)), personagem);
                for (var k = 0; k < bloco.Count; k++)
                    bloco[k].Ordem = k;
                Gravar();
                return Resultado.Sucesso();
            }
            // Na sala, heróis e NPCs são listas separadas.
            var lista = personagem.Sala == null
                ? DoLaboratorio
                : personagens.Where(p => p.Sala == personagem.Sala && p.Controle == personagem.Controle);
            Ordenacao.Mover(lista, personagem, indice, p => p.NomeExibicao);
            Gravar();
            return Resultado.Sucesso();
        }

        public Personagem Buscar(string id) => personagens.FirstOrDefault(p => p.Id == id);

        /// <summary>Novo herói no laboratório: status novo, controle jogador, jovem, herói.</summary>
        public Resultado Criar(string nome, string codinome, string arquetipo, string jogador)
        {
            var identidade = Montar(nome, codinome, arquetipo, jogador);
            var erro = Validar(identidade);
            if (erro != null)
                return Resultado.Falha(erro);

            personagens.Add(new Personagem
            {
                Id = Guid.NewGuid().ToString("N"), Identidade = identidade, Ordem = Ordenacao.Fim(personagens),
            });
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado AlterarIdentidade(string id, string nome, string codinome, string arquetipo, string jogador)
        {
            var personagem = Buscar(id);
            if (personagem == null)
                return Resultado.Falha("Personagem não encontrada.");

            var identidade = Montar(nome, codinome, arquetipo, jogador);
            var erro = Validar(identidade);
            if (erro != null)
                return Resultado.Falha(erro);

            // Depois de criada, trocar de arquétipo é um avanço (tardio), não uma edição.
            if (!personagem.EmCriacao && identidade.Arquetipo != personagem.Identidade.Arquetipo)
                return Resultado.Falha("Depois de criada, trocar de arquétipo é um avanço, não uma edição.");

            // Em "novo", trocar de arquétipo reseta a ficha de criação: as escolhas eram do arquétipo antigo.
            // (A tela pede confirmação antes, se houver escolhas.)
            if (identidade.Arquetipo != personagem.Identidade.Arquetipo)
                personagem.Criacao = new RascunhoCriacao();

            identidade.Aparencia = personagem.Identidade.Aparencia;
            identidade.Resumo = personagem.Identidade.Resumo;
            identidade.FotoHeroica = personagem.Identidade.FotoHeroica;
            identidade.FotoCivil = personagem.Identidade.FotoCivil;
            identidade.FotoHeroicaAprovacao = personagem.Identidade.FotoHeroicaAprovacao;
            identidade.FotoCivilAprovacao = personagem.Identidade.FotoCivilAprovacao;
            // As respostas são das perguntas do arquétipo: trocar de arquétipo as apaga.
            if (identidade.Arquetipo == personagem.Identidade.Arquetipo)
                identidade.RespostasHistorico = personagem.Identidade.RespostasHistorico;
            personagem.Identidade = identidade;
            Gravar();
            return Resultado.Sucesso();
        }

        // ---------- ficha de criação: Núcleo ----------

        /// <summary>Valor do Rótulo que o arquétipo dá (o azul da ficha).</summary>
        public int RotuloDoArquetipo(Personagem p, string rotulo)
        {
            var regra = regras.Buscar(p.Foto?.Arquetipo ?? p.Identidade.Arquetipo);
            return regra != null && regra.Rotulos.TryGetValue(rotulo, out var v) ? v : 0;
        }

        /// <summary>
        /// Rótulos de agora: no jogo, os mudados pela mesa (ou os da foto, se nada mudou); em criação,
        /// arquétipo + o +1 livre do rascunho.
        /// </summary>
        public IReadOnlyDictionary<string, int> RotulosAtuais(Personagem p)
        {
            if (p.Foto != null)
                return p.RotulosJogo ?? p.Foto.Rotulos;

            var atuais = new Dictionary<string, int>();
            foreach (var r in Rotulos.Todos)
                atuais[r] = RotuloDoArquetipo(p, r);
            if (p.Criacao.BonusLivre != null && atuais.ContainsKey(p.Criacao.BonusLivre))
                atuais[p.Criacao.BonusLivre] += regras.BonusLivre;
            return atuais;
        }

        /// <summary>Põe o +1 livre num Rótulo (null tira). Só em "novo", e nunca acima do máximo da criação.</summary>
        public Resultado EscolherBonusLivre(string id, string rotulo)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (!p.EmCriacao)
                return Resultado.Falha("A ficha já foi aprovada; o +1 livre é da criação.");
            if (rotulo != null)
            {
                if (!Rotulos.Todos.Contains(rotulo))
                    return Resultado.Falha("Rótulo desconhecido.");
                if (RotuloDoArquetipo(p, rotulo) + regras.BonusLivre > regras.MaximoNaCriacao)
                    return Resultado.Falha($"{Rotulos.Nome(rotulo)} já começa em +{regras.MaximoNaCriacao}; o +1 livre não pode ir para ele.");
            }

            p.Criacao.BonusLivre = rotulo;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// O que a validação aponta. Erros impedem aprovar; avisos o mestre pode aceitar
        /// (a validação avisa, quem decide é o mestre).
        /// </summary>
        public (IReadOnlyList<string> erros, IReadOnlyList<string> avisos) Validar(Personagem p)
        {
            var erros = new List<string>();
            var avisos = new List<string>();
            if (regras.Buscar(p.Identidade.Arquetipo) == null)
                erros.Add("Arquétipo sem regras de criação.");
            if (p.Criacao.BonusLivre == null)
                avisos.Add("Falta escolher o +1 livre.");
            ValidarHabilidades(p, avisos);
            ValidarModulos(p, avisos);
            var semResposta = PerguntasSemResposta(p);
            if (semResposta > 0)
                avisos.Add($"História: responda as perguntas da ficha (faltam {semResposta}).");
            return (erros, avisos);
        }

        /// <summary>Novo → criado: congela a foto da criação. Com avisos, só se o mestre aceitar.</summary>
        /// <summary>
        /// Novo → criado: congela a foto da criação. Só sem pendências (decidido em 2026-10-13:
        /// ficha incompleta congelada só gera conserto). Depois de aprovada, o mestre ainda corrige
        /// por ajuste manual com motivo. Observações não travam.
        /// </summary>
        public Resultado Aprovar(string id)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (!p.EmCriacao)
                return Resultado.Falha("Essa ficha já foi aprovada.");

            var (erros, avisos) = Validar(p);
            if (erros.Count > 0)
                return Resultado.Falha(string.Join(" ", erros));
            if (avisos.Count > 0)
                return Resultado.Falha("Ainda há pendências: " + string.Join(" ", avisos));

            // Monta a foto inteira ANTES de pô-la na personagem: com p.Foto preenchida, as leituras
            // (MovimentosNaFicha etc.) passam a ler da foto, e ela ainda estaria vazia.
            var foto = new FotoCriacao
            {
                AprovadaEm = DateTime.Now.ToString("s"),
                Arquetipo = p.Identidade.Arquetipo,
                Rotulos = new Dictionary<string, int>(RotulosAtuais(p)),
                Habilidades = HabilidadesAtuais(p).ToList(),
            };
            CongelarModulos(p, foto);
            p.Foto = foto;
            // Concluir leva a "em aprovação" (também a instância reaberta na sala): quem põe em carreira
            // é o mestre, na campanha (AprovarParaCampanha).
            p.Status = StatusCriacao.EmAprovacao;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Pode voltar para "novo"? (null = pode; senão, o motivo)</summary>
        public string MotivoParaNaoReabrir(Personagem p)
        {
            if (p.Controle != Controle.Jogador)
                return "NPC não tem ciclo de criação.";
            if (p.Sala == null)
            {
                if (p.Status != StatusCriacao.EmAprovacao)
                    return "Só ficha concluída (em aprovação) volta para novo.";
                var emVaga = CopiasDe(p.Id).Where(c => c.Ativo).ToList();
                return emVaga.Count > 0
                    ? "Tem instância numa vaga: corrija a instância, pela sala."
                    : null;
            }
            if (!p.Ativo)
                return "Ficha do arquivo da sala (vaga liberada): só consulta.";
            // Em aprovação ou em carreira (erro que passou despercebido): vai para revisão, pela aba Mestre,
            // e continua na vaga. Aposentado não volta.
            return p.Status == StatusCriacao.EmAprovacao || p.Status == StatusCriacao.EmCarreira
                ? null
                : "Só ficha em aprovação ou em carreira vai para revisão.";
        }

        /// <summary>
        /// Devolve a ficha para a criação (descarta a foto) para refazer as escolhas.
        /// No laboratório: a original volta a "novo", se não tiver instância numa vaga. Na sala: a
        /// instância (em aprovação ou em carreira) vai para "em revisão", continua na vaga e guarda o jogo;
        /// concluída de novo, volta a "em aprovação", e o mestre aprova outra vez.
        /// </summary>
        public Resultado ReabrirCriacao(string id)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            var motivo = MotivoParaNaoReabrir(p);
            if (motivo != null)
                return Resultado.Falha(motivo);

            p.Foto = null;
            // Refazer a criação refaz os Rótulos: os de jogo voltam a partir da nova foto.
            p.RotulosJogo = null;
            // Na sala, a instância reaberta fica em revisão (na vaga); no laboratório, volta a novo.
            p.Status = p.Sala != null ? StatusCriacao.EmRevisao : StatusCriacao.Novo;
            Gravar();
            return Resultado.Sucesso();
        }

        // ---------- sala: a cópia (instância) ----------

        /// <summary>
        /// Cria a cópia de uma personagem do laboratório para jogar na sala (a original fica intacta).
        /// Só ficha criada (foto congelada) entra. Sem <paramref name="comJogo"/>, a cópia nasce só da
        /// criação: o que o laboratório marcou de jogo (potencial, avanços, trilhas...) fica para trás.
        /// </summary>
        public Personagem CopiarParaSala(string origemId, string sala, string jogador, bool comJogo, out Resultado resultado)
        {
            var origem = Buscar(origemId);
            if (origem == null || origem.Sala != null)
            {
                resultado = Resultado.Falha("Só personagem do laboratório vai para a sala.");
                return null;
            }
            if (origem.Status != StatusCriacao.EmAprovacao)
            {
                resultado = Resultado.Falha("Conclua a criação no laboratório antes de pôr na sala.");
                return null;
            }

            // Em modo teste, copia a real (o que está no disco), não o que o teste mexeu.
            var json = emTeste.TryGetValue(origemId, out var real) ? real : JsonConvert.SerializeObject(origem);
            var copia = JsonConvert.DeserializeObject<Personagem>(json);
            copia.Id = Guid.NewGuid().ToString("N");
            copia.Origem = origem.Id;
            copia.Sala = sala;
            // Na campanha ela ainda completa o que é da equipe; o mestre aprova depois.
            copia.Status = StatusCriacao.EmAprovacao;
            copia.Ativo = true;
            copia.Ordem = Ordenacao.Fim(DaSala(sala));
            copia.Identidade.Jogador = jogador;
            if (!comJogo)
            {
                copia.Potencial = 0;
                copia.AvancosMarcados = new List<string>();
                copia.Contadores = new Dictionary<string, int>();
                copia.EscolhasDeJogo = new Dictionary<string, List<string>>();
                copia.TextosDeJogo = new Dictionary<string, string>();
                copia.ItensDeJogo = new Dictionary<string, List<ItemDeJogo>>();
                copia.Cumpridos = new Dictionary<string, string>();
            }
            personagens.Add(copia);
            Gravar();
            resultado = Resultado.Sucesso();
            return copia;
        }

        /// <summary>
        /// O mestre aprova a instância na campanha: em aprovação → em carreira. A partir daqui ela joga
        /// (a mesa só manda comando para quem está em carreira).
        /// </summary>
        public Resultado AprovarParaCampanha(string id)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (p.Sala == null)
                return Resultado.Falha("A aprovação é na campanha: ponha a personagem numa vaga.");
            if (!p.Ativo)
                return Resultado.Falha("Ficha do arquivo da sala: só consulta.");
            if (p.Status != StatusCriacao.EmAprovacao)
                return Resultado.Falha("Só ficha em aprovação se aprova.");
            var pendencias = PendenciasParaCarreira(p);
            if (pendencias.Count > 0)
                return Resultado.Falha("Ainda falta: " + string.Join(" ", pendencias));
            p.Status = StatusCriacao.EmCarreira;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Saiu do jogo (tirada da vaga) ou encerrou a carreira: aposentada, só consulta.</summary>
        public void Aposentar(string id)
        {
            var p = Buscar(id);
            if (p == null)
                return;
            p.Status = StatusCriacao.Aposentado;
            p.Ativo = false;
            p.Ordem = Ordenacao.Fim(personagens);
            Gravar();
        }

        /// <summary>As cópias (instâncias nas salas) feitas desta personagem; ativas primeiro.</summary>
        public IReadOnlyList<Personagem> CopiasDe(string origemId) =>
            personagens.Where(p => p.Origem == origemId && p.Sala != null).OrderByDescending(p => p.Ativo).ToList();

        /// <summary>O jogador da cópia acompanha o do slot (quem decide é a sala).</summary>
        public void VincularJogador(string id, string jogador)
        {
            var p = Buscar(id);
            if (p == null || p.Identidade.Jogador == jogador)
                return;
            p.Identidade.Jogador = jogador;
            Gravar();
        }

        public Resultado DefinirFavorito(string id, bool favorito)
        {
            var personagem = Buscar(id);
            if (personagem == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (personagem.Favorito == favorito)
                return Resultado.Sucesso();
            personagem.Favorito = favorito;
            Gravar();
            return Resultado.Sucesso();
        }

        public Resultado DefinirAtivo(string id, bool ativo)
        {
            var personagem = Buscar(id);
            if (personagem == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (personagem.Ativo == ativo)
                return Resultado.Sucesso();

            personagem.Ativo = ativo;
            personagem.Ordem = Ordenacao.Fim(personagens); // entra no fim do outro bloco
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// Aparência e breve resumo: texto livre, editável a qualquer momento (não fazem parte da foto
        /// mecânica; descrever a personagem não é regra).
        /// </summary>
        public Resultado AlterarTextos(string id, string aparencia, string resumo)
        {
            var personagem = Buscar(id);
            if (personagem == null)
                return Resultado.Falha("Personagem não encontrada.");
            var a = (aparencia ?? string.Empty).Trim();
            var r = (resumo ?? string.Empty).Trim();
            if (a == personagem.Identidade.Aparencia && r == personagem.Identidade.Resumo)
                return Resultado.Sucesso();
            personagem.Identidade.Aparencia = a;
            personagem.Identidade.Resumo = r;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>
        /// Links das fotos (de herói e civil): páginas web. Vazio tira. Como aparência e resumo,
        /// não fazem parte da foto da criação: editáveis a qualquer momento.
        /// </summary>
        public Resultado AlterarFotos(string id, string heroica, string civil)
        {
            var personagem = Buscar(id);
            if (personagem == null)
                return Resultado.Falha("Personagem não encontrada.");
            var h = (heroica ?? string.Empty).Trim();
            var c = (civil ?? string.Empty).Trim();
            foreach (var link in new[] { h, c })
                if (link.Length > 0 && !LinkValido(link))
                    return Resultado.Falha("O link da foto precisa ser http(s) e terminar em .jpg, .jpeg ou .png.");
            var i = personagem.Identidade;
            if (h == i.FotoHeroica && c == i.FotoCivil)
                return Resultado.Sucesso();
            // Link novo é imagem nova: volta a aguardar o olho do mestre.
            if (h != i.FotoHeroica)
                i.FotoHeroicaAprovacao = AprovacaoFoto.Aguardando;
            if (c != i.FotoCivil)
                i.FotoCivilAprovacao = AprovacaoFoto.Aguardando;
            i.FotoHeroica = h;
            i.FotoCivil = c;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>O mestre aprova ou recusa a foto (<paramref name="heroica"/>: a de herói; senão, a civil).</summary>
        public Resultado AvaliarFoto(string id, bool heroica, AprovacaoFoto aprovacao)
        {
            var personagem = Buscar(id);
            if (personagem == null)
                return Resultado.Falha("Personagem não encontrada.");
            var i = personagem.Identidade;
            if (string.IsNullOrEmpty(heroica ? i.FotoHeroica : i.FotoCivil))
                return Resultado.Falha("Sem link, não há foto para avaliar.");
            if (heroica)
                i.FotoHeroicaAprovacao = aprovacao;
            else
                i.FotoCivilAprovacao = aprovacao;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Formatos de foto aceitos (os que a Torre consegue mostrar).</summary>
        public static readonly IReadOnlyList<string> FormatosDeFoto = new[] { ".jpg", ".jpeg", ".png" };

        /// <summary>Link http(s) direto para uma imagem .jpg, .jpeg ou .png (o "?..." no fim não conta).</summary>
        public static bool LinkValido(string link) =>
            Uri.TryCreate(link, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
            FormatosDeFoto.Any(f => uri.AbsolutePath.EndsWith(f, StringComparison.OrdinalIgnoreCase));

        /// <summary>Resposta à pergunta de histórico de número <paramref name="indice"/> (ordem da ficha).</summary>
        public Resultado ResponderHistorico(string id, int indice, string resposta)
        {
            var personagem = Buscar(id);
            if (personagem == null)
                return Resultado.Falha("Personagem não encontrada.");
            var perguntas = RegraDoArquetipo(personagem)?.Historico ?? new List<string>();
            if (indice < 0 || indice >= perguntas.Count)
                return Resultado.Falha("Pergunta desconhecida.");

            var respostas = personagem.Identidade.RespostasHistorico;
            while (respostas.Count < perguntas.Count)
                respostas.Add(string.Empty);
            var limpa = (resposta ?? string.Empty).Trim();
            if (respostas[indice] == limpa)
                return Resultado.Sucesso();
            respostas[indice] = limpa;
            Gravar();
            return Resultado.Sucesso();
        }

        /// <summary>Quantas perguntas de histórico ainda estão sem resposta.</summary>
        public int PerguntasSemResposta(Personagem p)
        {
            var perguntas = RegraDoArquetipo(p)?.Historico ?? new List<string>();
            var respostas = p.Identidade.RespostasHistorico;
            var faltam = 0;
            for (var k = 0; k < perguntas.Count; k++)
                if (k >= respostas.Count || string.IsNullOrWhiteSpace(respostas[k]))
                    faltam++;
            return faltam;
        }

        /// <summary>Trocar de arquétipo agora apagaria algo? (escolhas de criação ou respostas de histórico)</summary>
        public bool TrocaDeArquetipoApaga(Personagem p) =>
            p.Criacao.TemEscolhas || p.Identidade.RespostasHistorico.Any(r => !string.IsNullOrWhiteSpace(r));

        /// <summary>Regras do arquétipo da personagem (o da foto, se aprovada).</summary>
        public RegrasArquetipos.RegraArquetipo RegraDoArquetipo(Personagem p) =>
            regras.Buscar(p.Foto?.Arquetipo ?? p.Identidade.Arquetipo);

        static Identidade Montar(string nome, string codinome, string arquetipo, string jogador) =>
            new Identidade
            {
                Nome = (nome ?? string.Empty).Trim(),
                Codinome = (codinome ?? string.Empty).Trim(),
                Arquetipo = arquetipo,
                Jogador = string.IsNullOrWhiteSpace(jogador) ? null : jogador,
            };

        static string Validar(Identidade identidade)
        {
            if (identidade.Nome.Length == 0 && identidade.Codinome.Length == 0)
                return "Dê pelo menos um nome ou um codinome.";
            if (Arquetipos.Buscar(identidade.Arquetipo) == null)
                return "Escolha um arquétipo.";
            return null;
        }

        /// <summary>
        /// Grava (ou agenda, se o armazenamento junta gravações: cliques seguidos viram uma só, e o texto
        /// só é gerado na hora de gravar).
        /// </summary>
        void Gravar()
        {
            if (armazenamento is IGravacaoAdiada adiada)
                adiada.Agendar(Chave, Serializar);
            else
                armazenamento.Gravar(Chave, Serializar());
        }

        string Serializar()
        {
            // Personagem em modo teste grava a foto real, não o que o teste mexeu.
            var gravar = personagens
                .Select(p => emTeste.TryGetValue(p.Id, out var real) ? JsonConvert.DeserializeObject<Personagem>(real) : p)
                .ToList();
            var arquivo = new Arquivo { Versao = VersaoArquivo, Personagens = gravar };
            return JsonConvert.SerializeObject(arquivo, Formatting.Indented);
        }

        // ---------- modo teste (playground do mestre, para validar a ficha) ----------

        /// <summary>Id → a personagem real (JSON) guardada ao ligar o modo teste.</summary>
        readonly Dictionary<string, string> emTeste = new Dictionary<string, string>();

        static readonly JsonSerializerSettings Restaurar =
            new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };

        public bool EmTeste(string id) => emTeste.ContainsKey(id);

        /// <summary>
        /// Liga o modo teste: guarda a personagem real. Daqui até desligar, tudo o que mudar fica só na
        /// memória (o disco continua com a real), e desligar desfaz tudo. Serve para validar a ficha
        /// antes de existirem as fontes de verdade (painel do mestre, eventos com motivo).
        /// </summary>
        public Resultado LigarTeste(string id)
        {
            var p = Buscar(id);
            if (p == null)
                return Resultado.Falha("Personagem não encontrada.");
            if (!emTeste.ContainsKey(id))
                emTeste[id] = JsonConvert.SerializeObject(p);
            return Resultado.Sucesso();
        }

        /// <summary>Desliga o modo teste: a personagem volta exatamente ao que era (mesmo objeto).</summary>
        public Resultado DesligarTeste(string id)
        {
            var p = Buscar(id);
            if (p == null || !emTeste.TryGetValue(id, out var real))
                return Resultado.Sucesso();
            JsonConvert.PopulateObject(real, p, Restaurar);
            emTeste.Remove(id);
            return Resultado.Sucesso();
        }

        sealed class Arquivo
        {
            [JsonProperty("versao")] public int Versao { get; set; } = VersaoArquivo;
            [JsonProperty("personagens")] public List<Personagem> Personagens { get; set; } = new List<Personagem>();
        }
    }
}
