using System;
using System.Linq;
using TMPro;
using TorreDeControle.Core;
using TorreDeControle.Core.Personagens;
using UnityEngine;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Ficha de NPC (livro p.193–195): nome, aparência marcante, modo de agir, motivação e notas;
    /// se for vilão, a ficha de vilão (geração, movimentos, condições). Tudo grava na hora.
    /// Marcar condição na luta é do painel do mestre (ainda não existe).
    /// </summary>
    public sealed class PainelNpc : MonoBehaviour
    {
        Personagem npc;
        CadastroPersonagens cadastro;
        Action aoFechar;
        PainelAjuda ajudaCondicoes;

        TextMeshProUGUI titulo;
        TextMeshProUGUI subtitulo;
        RectTransform moldura;
        RectTransform conteudo;
        TextMeshProUGUI rodape;

        /// <summary>
        /// Como a tela chama cada papel. "Vilão / Oponente": um aliado com quem a equipe briga (a babá
        /// que apanhou) usa a ficha de vilão sem ser vilão. O id gravado é o do enum (vilao).
        /// </summary>
        public static string NomeDoPapel(Papel p) =>
            p switch { Papel.Vilao => "Vilão / Oponente", Papel.Aliado => "Aliado", _ => "Neutro" };

        public static System.Collections.Generic.IReadOnlyList<(string id, string nome)> OpcoesPapel() =>
            new[] { Papel.Neutro, Papel.Aliado, Papel.Vilao }.Select(p => (p.ToString(), NomeDoPapel(p))).ToList();

        static System.Collections.Generic.IReadOnlyList<(string id, string nome)> OpcoesMaturidade() =>
            new[] { (Maturidade.Adulto.ToString(), "Adulto"), (Maturidade.Jovem.ToString(), "Jovem") };

        public static PainelNpc Criar(Transform pai)
        {
            var veu = Estilo.Rect("PainelNpc", pai);
            Estilo.Esticar(veu);
            veu.gameObject.AddComponent<Image>().color = Estilo.Veu;
            veu.gameObject.AddComponent<NavegacaoTab>();
            var painel = veu.gameObject.AddComponent<PainelNpc>();
            painel.Montar(veu);
            return painel;
        }

        public void Abrir(Personagem personagem, CadastroPersonagens cadastroPersonagens, Action aoFecharFicha)
        {
            npc = personagem;
            cadastro = cadastroPersonagens;
            aoFechar = aoFecharFicha;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Redesenhar();
        }

        void Fechar()
        {
            gameObject.SetActive(false);
            aoFechar?.Invoke();
        }

        // ---------- exportar e importar este NPC ----------

        /// <summary>Grava em dados/exportacoes e copia o JSON (lista de um NPC: entra também na tela de NPCs).</summary>
        void Exportar()
        {
            var json = cadastro.ExportarNpc(npc.Id);
            if (json == null || Troca.Arquivos == null)
                return;
            string caminho;
            try
            {
                caminho = Troca.Arquivos.Exportar("NPC - " + npc.NomeExibicao, json);
            }
            catch (Exception e)
            {
                rodape.color = Estilo.Erro;
                rodape.text = "Não deu para exportar: " + e.Message;
                return;
            }
            GUIUtility.systemCopyBuffer = json;
            Estilo.Confirmar(transform,
                $"{npc.NomeExibicao} exportado em\n<size=80%>{caminho}</size>\nO JSON também foi copiado (Ctrl+V para colar).",
                "Abrir a pasta",
                () => Application.OpenURL("file:///" + Troca.Arquivos.PastaExportacoes.Replace('\\', '/')));
        }

        /// <summary>Por cima deste NPC: um .json de dados/importar, ou a área de transferência.</summary>
        void Importar()
        {
            if (Troca.Arquivos == null)
                return;
            var opcoes = new System.Collections.Generic.List<(string, UnityEngine.Events.UnityAction, bool)>();
            foreach (var caminho in Troca.Importaveis(CadastroPersonagens.FormatoDeTrocaNpcs))
            {
                var arquivo = caminho;
                opcoes.Add((System.IO.Path.GetFileName(arquivo), () => ImportarPorCima(Troca.Arquivos.Ler(arquivo)), false));
            }
            opcoes.Add(("Colar da área de transferência", () => ImportarPorCima(GUIUtility.systemCopyBuffer), false));
            opcoes.Add(("Abrir a pasta de importar", () =>
                Application.OpenURL("file:///" + Troca.Arquivos.PastaImportar.Replace('\\', '/')), true));
            Estilo.Escolher(transform, $"Importar por cima de {npc.NomeExibicao}", opcoes.ToArray());
        }

        void ImportarPorCima(string json) =>
            Estilo.Confirmar(transform,
                $"Gravar o arquivo por cima de {npc.NomeExibicao}?\n<size=80%>Ficam a campanha, o vínculo com a heroína e as Influências daqui.</size>",
                "Importar", () =>
                {
                    var resultado = cadastro.ReimportarNpc(npc.Id, json);
                    Redesenhar();
                    if (!resultado.Ok)
                    {
                        rodape.color = Estilo.Erro;
                        rodape.text = resultado.Erro;
                    }
                });

        // ---------- montagem ----------

        void Montar(RectTransform veu)
        {
            var cartao = Estilo.Rect("Cartao", veu);
            cartao.anchorMin = new Vector2(0.5f, 0.5f);
            cartao.anchorMax = new Vector2(0.5f, 0.5f);
            cartao.sizeDelta = new Vector2(1400, 940);
            Estilo.Moldura(cartao.gameObject, Estilo.Cartao, Estilo.Borda);

            var coluna = cartao.gameObject.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(56, 56, 40, 36);
            coluna.spacing = 12;
            coluna.childControlWidth = true;
            coluna.childControlHeight = true;
            coluna.childForceExpandHeight = false;

            var cabecalho = Estilo.Fila(cartao, 84);
            var titulos = Estilo.Rect("Titulos", cabecalho);
            titulos.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var pilha = titulos.gameObject.AddComponent<VerticalLayoutGroup>();
            pilha.childControlWidth = true;
            pilha.childControlHeight = true;
            pilha.childForceExpandHeight = false;
            pilha.childAlignment = TextAnchor.MiddleLeft;
            titulo = Estilo.Rotulo(titulos, string.Empty, 40, Estilo.Dourado, FontStyles.Bold);
            titulo.characterSpacing = 5;
            // Nome comprido ("Sentinela Solar (pai: …)"): numa linha só, a fonte encolhe; o resto vira "…".
            titulo.textWrappingMode = TextWrappingModes.NoWrap;
            titulo.enableAutoSizing = true;
            titulo.fontSizeMin = 22;
            titulo.fontSizeMax = 40;
            titulo.overflowMode = TextOverflowModes.Ellipsis;
            subtitulo = Estilo.Rotulo(titulos, string.Empty, 22, Estilo.TextoSuave);
            subtitulo.textWrappingMode = TextWrappingModes.NoWrap;
            subtitulo.overflowMode = TextOverflowModes.Ellipsis;
            foreach (var (nome, acao) in new (string, UnityEngine.Events.UnityAction)[] { ("Exportar", Exportar), ("Importar", Importar), ("Fechar", Fechar) })
            {
                var b = Estilo.BotaoContorno(cabecalho, nome, 190, Estilo.TextoSuave, acao);
                var t = b.GetComponentInChildren<TextMeshProUGUI>();
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.enableAutoSizing = true;
                t.fontSizeMin = 14;
                t.fontSizeMax = 24;
                t.characterSpacing = 2;
                t.rectTransform.offsetMin = new Vector2(8, 0);
                t.rectTransform.offsetMax = new Vector2(-8, 0);
            }

            var divisoria = Estilo.Rect("Divisoria", cartao);
            divisoria.gameObject.AddComponent<Image>().color =
                new Color(Estilo.Dourado.r, Estilo.Dourado.g, Estilo.Dourado.b, 0.35f);
            var ld = divisoria.gameObject.AddComponent<LayoutElement>();
            ld.minHeight = 2;
            ld.preferredHeight = 2;

            moldura = Estilo.Rect("Moldura", cartao);
            moldura.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            rodape = Estilo.Rotulo(cartao, string.Empty, 20, Estilo.TextoSuave, FontStyles.Italic);
            rodape.textWrappingMode = TextWrappingModes.Normal;
            rodape.gameObject.AddComponent<LayoutElement>().minHeight = 30;
        }

        /// <summary>Refaz a ficha mantendo a rolagem (clicar numa condição lá embaixo não volta ao topo).</summary>
        void Redesenhar()
        {
            var posicao = conteudo != null ? conteudo.anchoredPosition.y : 0f;
            for (var i = moldura.childCount - 1; i >= 0; i--)
                Destroy(moldura.GetChild(i).gameObject);
            var lista = Estilo.ListaRolavel(moldura, 8);
            Estilo.Esticar((RectTransform)lista.parent.parent);
            conteudo = lista;

            Cabecalho();
            MontarFicha();
            Rodape();

            if (posicao > 0)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(moldura);
                LayoutRebuilder.ForceRebuildLayoutImmediate(conteudo);
                var viewport = (RectTransform)conteudo.parent;
                var maximo = Mathf.Max(0, conteudo.rect.height - viewport.rect.height);
                conteudo.anchoredPosition = new Vector2(conteudo.anchoredPosition.x, Mathf.Min(posicao, maximo));
            }
        }

        void Cabecalho()
        {
            titulo.text = npc.NomeCompleto.ToUpperInvariant();
            var maturidade = npc.Maturidade == Maturidade.Adulto ? "adulto" : "jovem";
            var papel = NomeDoPapel(npc.Papel).ToLowerInvariant();
            var origem = cadastro.OrigemDoNpc(npc);
            subtitulo.text = $"NPC · {maturidade} · {papel}" +
                             (origem is { } o ? $" · {o.dono.NomeExibicao} · {o.vinculo}" : string.Empty) +
                             (npc.Ativo ? string.Empty : " · inativo");
        }

        void Rodape()
        {
            var avisos = cadastro.AvisosDoNpc(npc);
            rodape.color = avisos.Count > 0 ? Estilo.Dourado : Estilo.TextoSuave;
            rodape.text = avisos.Count > 0 ? string.Join("   ", avisos) : "Ficha completa.";
        }

        void MontarFicha()
        {
            if (cadastro.OrigemDoNpc(npc) is { } origem)
            {
                Secao($"{origem.dono.NomeExibicao.ToUpperInvariant()} · {origem.vinculo.ToUpperInvariant()}", null);
                Nota("Nasceu da ficha dela. O texto dela foi copiado para as Notas (lá embaixo), e daqui em diante " +
                     "são suas: editar as Notas não mexe na história dela, e o contrário também. O nome é o mesmo dos dois lados: " +
                     "mudar aqui muda na ficha dela.");
                Vao();
            }
            Secao("IDENTIDADE", "clique na foto para abrir o link");
            var topo = Estilo.Fila(conteudo, 262, 16);
            QuadroDaFoto(topo);
            var dados = Estilo.Rect("Dados", topo);
            var ld = dados.gameObject.AddComponent<LayoutElement>();
            ld.flexibleWidth = 1;
            var vd = dados.gameObject.AddComponent<VerticalLayoutGroup>();
            vd.spacing = 6;
            vd.childControlWidth = true;
            vd.childControlHeight = true;
            vd.childForceExpandHeight = false;

            CampoDaFoto(dados);
            var nome = CampoRotulado("NOME", npc.Identidade.Nome, "nome", dados);
            var codinome = CampoRotulado(npc.Papel == Papel.Vilao ? "CODINOME / NOME DE VILÃO" : "CODINOME",
                npc.Identidade.Codinome, "opcional", dados);
            foreach (var campo in new[] { nome, codinome })
            {
                campo.onSubmit.AddListener(_ => Renomear(nome.text, codinome.text));
                campo.onDeselect.AddListener(_ => Renomear(nome.text, codinome.text));
            }
            // Papel e maturidade aqui também: promover a Vilão / Oponente na hora da briga, e voltar depois
            // (a ficha de vilão fica guardada).
            var perfil = FilaRotulada("PAPEL", dados);
            Estilo.ListaSuspensa(perfil, 300, OpcoesPapel(), npc.Papel.ToString(), id =>
            {
                cadastro.DefinirPapel(npc.Id, (Papel)Enum.Parse(typeof(Papel), id));
                Redesenhar();
            });
            var rotuloMaturidade = Estilo.Rotulo(perfil, "MATURIDADE", 20, Estilo.TextoSuave, FontStyles.Bold);
            rotuloMaturidade.characterSpacing = 4;
            rotuloMaturidade.alignment = TextAlignmentOptions.Right;
            rotuloMaturidade.gameObject.AddComponent<LayoutElement>().preferredWidth = 190;
            Estilo.ListaSuspensa(perfil, 220, OpcoesMaturidade(), npc.Maturidade.ToString(), id =>
            {
                cadastro.DefinirMaturidade(npc.Id, (Maturidade)Enum.Parse(typeof(Maturidade), id));
                Redesenhar();
            });
            if (npc.Papel == Papel.Vilao)
                Nota("O nome de vilão sinaliza a geração; o nome real é opcional.");

            Secao("APARÊNCIA MARCANTE", "um maneirismo ou traço físico bem distinto");
            var aparencia = Estilo.AreaTexto(conteudo, "como reconhecer de longe", 90);
            aparencia.text = npc.Identidade.Aparencia;
            Estilo.ComExpandir(aparencia, "Aparência marcante");

            Secao("MODO DE AGIR", "uma palavra que resume como age");
            var modo = Estilo.CampoTexto(conteudo, "ex.: arrogante, cuidadosa, explosivo");
            modo.text = npc.Pnj?.ModoDeAgir ?? string.Empty;
            Altura(modo, 52);

            Secao("MOTIVAÇÃO", "verbo no infinitivo + o que quer");
            var motivacao = Estilo.CampoTexto(conteudo, "ex.: proteger a nação, vingar minha irmã");
            motivacao.text = npc.Pnj?.Motivacao ?? string.Empty;
            Altura(motivacao, 52);
            Nota(npc.Papel == Papel.Vilao
                ? "Motivação de vilão deixa a humanidade à mostra: algo fácil de entender e de sentir junto."
                : "Na dúvida sobre o que o NPC faz, a motivação responde.");
            void GravarPerfil()
            {
                cadastro.DefinirPerfilPnj(npc.Id, modo.text, motivacao.text);
                Rodape();
            }
            foreach (var campo in new[] { modo, motivacao })
            {
                campo.onSubmit.AddListener(_ => GravarPerfil());
                campo.onDeselect.AddListener(_ => GravarPerfil());
            }

            Secao("NOTAS", "quem é, de onde vem, o que esconde");
            var notas = Estilo.AreaTexto(conteudo, "anotações do mestre", 110);
            notas.text = npc.Identidade.Resumo;
            Estilo.ComExpandir(notas, "Notas");
            aparencia.onDeselect.AddListener(_ => cadastro.AlterarTextos(npc.Id, aparencia.text, notas.text));
            notas.onDeselect.AddListener(_ => cadastro.AlterarTextos(npc.Id, aparencia.text, notas.text));

            // "Ligado a" é coisa de vilão (o da primeira sessão, livro cap. 8): aliado e neutro não mostram.
            // Trocar o papel não apaga as ligações; elas voltam se ele virar vilão de novo.
            if (npc.Papel == Papel.Vilao)
            {
                Secao("LIGADO A", "heroínas da campanha ligadas a este vilão, e como");
                Ligacoes();
            }

            Secao("INFLUÊNCIA", null);
            Nota(npc.Maturidade == Maturidade.Adulto
                ? "Adulto: tem Influência sobre os heróis por padrão; só importa saber quando ganha ou perde."
                : "Jovem: começa sem Influência sobre os heróis; pergunte aos jogadores se os colegas têm.");
            Influencias();

            if (npc.Papel == Papel.Vilao && npc.Vilao != null)
                FichaDeVilao();
            else
                Nota("NPC só marca condição se um herói o enfrentar diretamente. Para ter ficha de vilão, mude o papel para Vilão / Oponente (lá em cima).");
        }

        /// <summary>
        /// A foto do NPC: uma por link (qualquer link da web), no mesmo campo da foto heroica das personagens.
        /// Sem aprovar/recusar: quem põe a foto do NPC é o próprio mestre. Clicar no quadro abre o link.
        /// </summary>
        void QuadroDaFoto(Transform pai)
        {
            var link = npc.Identidade.FotoHeroica ?? string.Empty;
            var temLink = link.Length > 0;
            var quadro = Estilo.Rect("Foto", pai);
            var lq = quadro.gameObject.AddComponent<LayoutElement>();
            lq.preferredWidth = 190;
            lq.flexibleWidth = 0;
            lq.layoutPriority = 2;
            var fundo = Estilo.Moldura(quadro.gameObject, Estilo.Campo, temLink ? Estilo.Dourado : Estilo.BordaApagada);
            var botao = quadro.gameObject.AddComponent<Button>();
            SomBotao.Ligar(botao);
            botao.targetGraphic = fundo;
            botao.interactable = temLink;
            var cores = botao.colors;
            cores.disabledColor = Color.white;
            botao.colors = cores;
            if (temLink)
                botao.onClick.AddListener(() => Application.OpenURL(link));

            var area = Estilo.Rect("Imagem", quadro);
            Estilo.Esticar(area);
            area.offsetMin = new Vector2(4, 4);
            area.offsetMax = new Vector2(-4, -4);
            var miolo = Estilo.Rect("Miolo", area);
            var imagem = miolo.gameObject.AddComponent<RawImage>();
            imagem.raycastTarget = false;
            var proporcao = miolo.gameObject.AddComponent<AspectRatioFitter>();
            proporcao.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            var texto = Estilo.Rotulo(quadro, temLink ? string.Empty : "sem foto", 18, Estilo.TextoApagado, FontStyles.Bold);
            texto.alignment = TextAlignmentOptions.Center;
            texto.textWrappingMode = TextWrappingModes.Normal;
            texto.raycastTarget = false;
            Estilo.Esticar(texto.rectTransform);
            texto.rectTransform.offsetMin = new Vector2(10, 10);
            texto.rectTransform.offsetMax = new Vector2(-10, -10);
            if (temLink)
                quadro.gameObject.AddComponent<FotoDaWeb>().Mostrar(link, imagem, proporcao, texto);
            else
                imagem.enabled = false;
        }

        void CampoDaFoto(Transform pai)
        {
            var campo = CampoRotulado("FOTO (LINK)", npc.Identidade.FotoHeroica ?? string.Empty, "link da imagem (web)", pai);
            void Gravar()
            {
                if (campo.text.Trim() == (npc.Identidade.FotoHeroica ?? string.Empty))
                    return;
                var r = cadastro.AlterarFotos(npc.Id, campo.text, npc.Identidade.FotoCivil);
                if (!r.Ok)
                {
                    rodape.color = Estilo.Erro;
                    rodape.text = r.Erro;
                    return;
                }
                Redesenhar();
            }
            campo.onSubmit.AddListener(_ => Gravar());
            campo.onDeselect.AddListener(_ => Gravar());
        }

        /// <summary>
        /// Os dois sentidos com as heroínas da campanha, em chips (clicar liga/desliga; desligar fica como
        /// "perdeu"). Quem marca é o mestre; a jogadora só vê na ficha dela.
        /// </summary>
        void Influencias()
        {
            if (npc.Sala == null)
            {
                Nota("No laboratório não há heroínas: a Influência é marcada na campanha (cópia na sala).");
                return;
            }
            var herois = cadastro.HeroisDaSala(npc.Sala).Where(h => h.Ativo).OrderBy(h => h.NomeExibicao).ToList();
            if (herois.Count == 0)
            {
                Nota("Nenhuma heroína ativa nesta campanha ainda.");
                return;
            }
            var nome = npc.NomeExibicao.ToUpperInvariant();
            // As mesmas frases da ficha das heroínas: "deu Influência a X" = X tem Influência sobre ele.
            LinhaDeInfluencia($"{nome} DEU INFLUÊNCIA A", herois, h => (h, npc));
            LinhaDeInfluencia($"{nome} EXERCE INFLUÊNCIA SOBRE", herois, h => (npc, h));
        }

        /// <summary>
        /// "Ligado a": chips das heroínas da campanha (clicar liga/desliga) e, para cada ligada, uma linha de
        /// "como". Quem nasceu da ficha já vem ligado à dona (o laço aparece no topo); aqui entram as outras.
        /// </summary>
        void Ligacoes()
        {
            if (npc.Sala == null)
            {
                Nota("No laboratório não há heroínas: ligue na campanha (cópia na sala).");
                return;
            }
            var herois = cadastro.HeroisDaSala(npc.Sala).Where(h => h.Ativo).OrderBy(h => h.NomeExibicao).ToList();
            if (herois.Count == 0)
            {
                Nota("Nenhuma heroína ativa nesta campanha ainda.");
                return;
            }
            var ligadas = cadastro.LigacoesDoNpc(npc);
            for (var k = 0; k < herois.Count; k += 4)
            {
                var fila = Estilo.Fila(conteudo, 62, 10);
                foreach (var h in herois.Skip(k).Take(4))
                {
                    var heroi = h;
                    Chip(fila, h.NomeExibicao, ligadas.Any(l => l.heroi == h), () =>
                    {
                        var r = cadastro.AlternarLigacao(npc.Id, heroi.Id);
                        if (!r.Ok)
                        {
                            rodape.color = Estilo.Erro;
                            rodape.text = r.Erro;
                            return;
                        }
                        Redesenhar();
                    });
                }
                for (var vazio = herois.Count - k; vazio < 4; vazio++)
                    Estilo.Rect("Vao", fila).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            }
            foreach (var (heroi, como) in ligadas)
            {
                var fila = Estilo.Fila(conteudo, 52, 12);
                var r = Estilo.Rotulo(fila, heroi.NomeExibicao, 20, Estilo.TextoSuave, FontStyles.Bold);
                r.textWrappingMode = TextWrappingModes.NoWrap;
                r.overflowMode = TextOverflowModes.Ellipsis;
                var lr = r.gameObject.AddComponent<LayoutElement>();
                lr.preferredWidth = 260;
                lr.flexibleWidth = 0;
                var campo = Estilo.CampoTexto(fila, "como? ex.: destruiu o laboratório onde ela foi criada");
                campo.text = como;
                Estilo.ColunaElastica(campo);
                var alvo = heroi;
                void Gravar(string t) => cadastro.DescreverLigacao(npc.Id, alvo.Id, t);
                campo.onSubmit.AddListener(Gravar);
                campo.onDeselect.AddListener(Gravar);
            }
            if (ligadas.Count == 0 && cadastro.OrigemDoNpc(npc) == null)
                Nota("Sem ligação, este NPC não conta como \"vilão ligado a uma heroína\" na primeira sessão (Área do mestre).");
        }

        void LinhaDeInfluencia(string titulo, System.Collections.Generic.IReadOnlyList<Personagem> herois,
            Func<Personagem, (Personagem quem, Personagem sobre)> par)
        {
            var t = Estilo.Rotulo(conteudo, titulo, 20, Estilo.TextoSuave, FontStyles.Bold);
            t.characterSpacing = 4;
            for (var k = 0; k < herois.Count; k += 4)
            {
                var fila = Estilo.Fila(conteudo, 62, 10);
                foreach (var h in herois.Skip(k).Take(4))
                {
                    var (quem, sobre) = par(h);
                    var tem = cadastro.TemInfluencia(quem, sobre);
                    var nota = tem && cadastro.InfluenciaPorPadrao(quem, sobre) ? "por ser adulto"
                        : !tem && cadastro.PerdeuInfluencia(quem, sobre) ? "perdeu"
                        : null;
                    var texto = nota == null ? h.NomeExibicao : $"{h.NomeExibicao}\n<size=70%>{nota}</size>";
                    Chip(fila, texto, tem, () =>
                    {
                        var r = cadastro.AlternarInfluencia(quem.Id, sobre.Id);
                        if (!r.Ok)
                        {
                            rodape.color = Estilo.Erro;
                            rodape.text = r.Erro;
                            return;
                        }
                        Redesenhar();
                    });
                }
                for (var vazio = herois.Count - k; vazio < 4; vazio++)
                    Estilo.Rect("Vao", fila).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            }
        }

        void FichaDeVilao()
        {
            var v = npc.Vilao;
            Vao();
            Secao("FICHA DE VILÃO / OPONENTE", null);

            var filaGeracao = FilaRotulada("GERAÇÃO");
            var opcoes = new System.Collections.Generic.List<(string id, string nome)> { (string.Empty, "escolher...") };
            opcoes.AddRange(Geracoes.Todas.Select(g => (g.Id, g.Nome)));
            Estilo.ListaSuspensa(filaGeracao, 300, opcoes, v.Geracao ?? string.Empty, id =>
            {
                cadastro.DefinirGeracao(npc.Id, string.IsNullOrEmpty(id) ? null : id);
                Redesenhar();
            });
            var g = Geracoes.Buscar(v.Geracao);
            var dica = Estilo.Rotulo(filaGeracao, g == null ? string.Empty : g.Dica, 20, Estilo.TextoApagado, FontStyles.Italic);
            Estilo.ColunaElastica(dica);
            Estilo.SeloDica(filaGeracao, "o que é",
                "Sabor, sem efeito mecânico (livro p.194): diz a época do vilão e há quanto tempo ele está no jogo. " +
                "Ouro surgiu nos anos 1920–40; Prata, 1950–60; Bronze, 1970–80; Moderna, dos anos 1990 até hoje " +
                "(a mesma das PJs). O livro pede um nome que sinalize a geração.");

            Secao("MOVIMENTOS DE VILÃO", "frases ativas, com o que muda na cena (o livro sugere três ou mais)");
            for (var k = 0; k <= v.Movimentos.Count; k++)
            {
                var indice = k;
                var novo = k == v.Movimentos.Count;
                var campo = Estilo.CampoTexto(conteudo, novo ? "novo movimento e Enter" : "apague o texto para tirar");
                campo.text = novo ? string.Empty : v.Movimentos[k];
                Altura(campo, 52);
                Estilo.ComExpandir(campo, "Movimento de vilão");
                void Gravar(string t)
                {
                    var antes = v.Movimentos.Count;
                    cadastro.DefinirMovimentoDeVilao(npc.Id, indice, t);
                    // Acrescentou ou tirou: a lista mudou de tamanho, refaz. Só editou: não (Tab).
                    if (v.Movimentos.Count != antes)
                        Redesenhar();
                    else
                        Rodape();
                }
                campo.onSubmit.AddListener(Gravar);
                campo.onDeselect.AddListener(Gravar);
            }

            Secao("CONDIÇÕES", FichaVilao.Perigo(v.Condicoes.Count), AbrirAjudaCondicoes);
            var fila = Estilo.Fila(conteudo, 54, 10);
            foreach (var c in Condicoes.Todas)
            {
                var id = c;
                Chip(fila, Condicoes.Nome(c), v.Condicoes.Contains(c), () =>
                {
                    cadastro.AlternarCondicaoDeVilao(npc.Id, id);
                    Redesenhar();
                });
            }
            Nota("Ao levar um golpe forte, marca uma delas e age por ela; sem condição livre para marcar, está derrotado. " +
                 "Marcar na luta fica com o painel do mestre.");
        }

        // ---------- ações ----------

        void Renomear(string nome, string codinome)
        {
            var r = cadastro.RenomearNpc(npc.Id, nome, codinome);
            if (!r.Ok)
            {
                rodape.color = Estilo.Erro;
                rodape.text = r.Erro;
                return;
            }
            Cabecalho();
        }

        // ---------- peças ----------

        void AbrirAjudaCondicoes()
        {
            if (ajudaCondicoes == null)
                ajudaCondicoes = PainelAjuda.Criar(transform.parent);
            ajudaCondicoes.AbrirCondicoesVilao();
        }

        void Secao(string nome, string legenda, Action ajuda = null)
        {
            var fila = Estilo.Fila(conteudo, 40);
            var t = Estilo.Rotulo(fila, nome, 22, Estilo.Dourado, FontStyles.Bold);
            t.characterSpacing = 5;
            t.textWrappingMode = TextWrappingModes.NoWrap; // "FICHA DE VILÃO / OPONENTE" numa linha só
            t.gameObject.AddComponent<LayoutElement>().preferredWidth = ajuda == null ? 360 : 170;
            if (ajuda != null)
            {
                var b = Estilo.BotaoContorno(fila, "?", 36, Estilo.Dourado, () => ajuda());
                b.gameObject.AddComponent<Dica>().Configurar("Consultar Vilão na luta e os cinco movimentos de condição.");
            }
            if (legenda != null)
            {
                var l = Estilo.Rotulo(fila, legenda, 20, Estilo.TextoSuave, FontStyles.Italic);
                Estilo.ColunaElastica(l);
            }
        }

        void Nota(string texto)
        {
            var n = Estilo.Rotulo(conteudo, texto, 20, Estilo.TextoApagado, FontStyles.Italic);
            n.textWrappingMode = TextWrappingModes.Normal;
        }

        void Vao()
        {
            var vao = Estilo.Rect("Vao", conteudo);
            vao.gameObject.AddComponent<LayoutElement>().minHeight = 14;
        }

        RectTransform FilaRotulada(string rotulo, Transform pai = null)
        {
            var fila = Estilo.Fila(pai != null ? pai : conteudo, 58);
            var r = Estilo.Rotulo(fila, rotulo, 20, Estilo.TextoSuave, FontStyles.Bold);
            r.characterSpacing = 4;
            r.gameObject.AddComponent<LayoutElement>().preferredWidth = 240;
            return fila;
        }

        TMP_InputField CampoRotulado(string rotulo, string valor, string dica, Transform pai = null)
        {
            var fila = FilaRotulada(rotulo, pai);
            var campo = Estilo.CampoTexto(fila, dica);
            campo.text = valor;
            Estilo.ColunaElastica(campo);
            return campo;
        }

        static void Altura(Component alvo, float altura)
        {
            if (!alvo.TryGetComponent<LayoutElement>(out var layout)) layout = alvo.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = altura;
            layout.preferredHeight = altura;
        }

        static void Chip(Transform pai, string texto, bool escolhida, UnityEngine.Events.UnityAction aoClicar)
        {
            var rect = Estilo.Rect("Chip " + texto, pai);
            var imagem = escolhida
                ? Estilo.Moldura(rect.gameObject, Estilo.Dourado, Estilo.Dourado)
                : Estilo.Moldura(rect.gameObject, new Color(Estilo.Dourado.r, Estilo.Dourado.g, Estilo.Dourado.b, 0.08f), Estilo.Dourado);
            var botao = rect.gameObject.AddComponent<Button>();
            SomBotao.Ligar(botao);
            botao.targetGraphic = imagem;
            botao.onClick.AddListener(aoClicar);
            rect.gameObject.AddComponent<Tabulavel>();
            rect.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var t = Estilo.Rotulo(rect, texto, 22, escolhida ? Estilo.Cartao : Estilo.Dourado,
                escolhida ? FontStyles.Bold : FontStyles.Normal);
            t.alignment = TextAlignmentOptions.Center;
            Estilo.Esticar(t.rectTransform);
        }
    }
}
