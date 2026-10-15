using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TorreDeControle.Core;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Salas;
using UnityEngine;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Laboratório de NPCs (docs/npc_lab.md), no estilo do laboratório de personagens: os originais, sem
    /// sala, editados na linha; embaixo, os NPCs de cada campanha, para abrir e copiar. "Copiar para…" leva
    /// um NPC ao laboratório ou a qualquer sala (reboot), como cópia independente.
    /// </summary>
    public sealed class PainelNpcs : MonoBehaviour
    {
        const float LarguraPerfil = 200;
        const float LarguraAtivo = 150;
        const float LarguraCopiar = 170;
        const float LarguraFicha = 140;
        const float LarguraFavorito = 36;

        CadastroPersonagens cadastro;
        CadastroSalas salas;
        PainelNpc painelNpc;

        TMP_InputField campoCodinome;
        TMP_InputField campoNome;
        Maturidade maturidadeNova = Maturidade.Adulto;
        Papel papelNovo = Papel.Neutro;
        RectTransform filaNovo;
        TextMeshProUGUI mensagem;
        TextMeshProUGUI contagem;
        RectTransform lista;
        static Texture2D texturaFavorito;

        public static PainelNpcs Criar(Transform pai)
        {
            var veu = Estilo.Rect("PainelNpcs", pai);
            Estilo.Esticar(veu);
            veu.gameObject.AddComponent<Image>().color = Estilo.Veu;
            veu.gameObject.AddComponent<NavegacaoTab>();
            var painel = veu.gameObject.AddComponent<PainelNpcs>();
            painel.Montar(veu);
            return painel;
        }

        public void Abrir(CadastroPersonagens cadastroPersonagens, CadastroSalas cadastroSalas)
        {
            cadastro = cadastroPersonagens;
            salas = cadastroSalas;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Mostrar(null);
            MontarFilaNovo();
            Redesenhar();
            campoCodinome.ActivateInputField();
        }

        public void Fechar() => gameObject.SetActive(false);

        // ---------- opções ----------

        static IReadOnlyList<(string id, string nome)> OpcoesMaturidade() =>
            new List<(string, string)> { (Maturidade.Adulto.ToString(), "Adulto"), (Maturidade.Jovem.ToString(), "Jovem") };

        static IReadOnlyList<(string id, string nome)> OpcoesPapel() => PainelNpc.OpcoesPapel();

        static string NomeDoPapel(Papel p) => PainelNpc.NomeDoPapel(p);

        // ---------- ações ----------

        void Adicionar()
        {
            var resultado = cadastro.CriarNpc(null, campoNome.text, maturidadeNova, papelNovo, campoCodinome.text);
            Mostrar(resultado);
            if (resultado.Ok)
            {
                campoNome.text = string.Empty;
                campoCodinome.text = string.Empty;
                Redesenhar();
            }
            campoCodinome.ActivateInputField();
        }

        /// <summary>Grava a mudança feita na linha; no sucesso não redesenha (o Tab já foi para o próximo campo).</summary>
        void Renomear(Personagem npc, string nome, string codinome)
        {
            if (nome.Trim() == npc.Identidade.Nome && codinome.Trim() == npc.Identidade.Codinome)
                return;
            var resultado = cadastro.RenomearNpc(npc.Id, nome, codinome);
            Mostrar(resultado);
            if (!resultado.Ok)
                Redesenhar();
        }

        void AlternarAtivo(Personagem npc)
        {
            Mostrar(cadastro.DefinirAtivo(npc.Id, !npc.Ativo));
            Redesenhar();
        }

        /// <summary>Escolhe o destino: o laboratório (se o NPC está numa sala) ou qualquer sala; salas inativas, em tom pálido.</summary>
        void CopiarPara(Personagem npc)
        {
            var opcoes = new List<(string, UnityEngine.Events.UnityAction, bool)>();
            if (npc.Sala != null)
                opcoes.Add(("Laboratório de NPCs", () => Copiar(npc, null, "o laboratório"), false));
            foreach (var s in salas.Listar())
            {
                var sala = s;
                var aqui = sala.Id == npc.Sala ? " (outra cópia aqui)" : string.Empty;
                opcoes.Add(($"{sala.Nome}{aqui}", () => Copiar(npc, sala.Id, sala.Nome), !sala.Ativo));
            }
            Estilo.Escolher(transform, $"Copiar {npc.NomeExibicao} para…", opcoes.ToArray());
        }

        void Copiar(Personagem npc, string destino, string nomeDoDestino)
        {
            void Fazer()
            {
                cadastro.CopiarNpc(npc.Id, destino, out var resultado);
                Mostrar(resultado, resultado.Ok ? $"{npc.NomeExibicao} copiado para {nomeDoDestino}." : null);
                Redesenhar();
            }

            if (cadastro.NomeRepetidoNoDestino(npc, destino))
                Estilo.Confirmar(transform, $"{nomeDoDestino} já tem um NPC ativo chamado {npc.NomeExibicao}.\nCopiar mesmo assim?",
                    "Copiar", Fazer);
            else
                Fazer();
        }

        // ---------- exportar e importar (para outra máquina ou outra Torre) ----------

        /// <summary>Escolhe o que exportar: o laboratório ou os NPCs de uma campanha (ativos e inativos).</summary>
        void Exportar()
        {
            var opcoes = new List<(string, UnityEngine.Events.UnityAction, bool)>();
            var noLab = cadastro.NpcsDoLaboratorio().Count;
            opcoes.Add(($"Laboratório de NPCs ({noLab})", () => ExportarDe(null, "Laboratório de NPCs"), noLab == 0));
            foreach (var s in salas.Listar())
            {
                var sala = s;
                var n = cadastro.NpcsDaSala(sala.Id).Count;
                opcoes.Add(($"Campanha {sala.Nome} ({n})", () => ExportarDe(sala.Id, "Campanha " + sala.Nome), n == 0));
            }
            Estilo.Escolher(transform, "Exportar NPCs", opcoes.ToArray());
        }

        void ExportarDe(string sala, string origem)
        {
            var json = cadastro.ExportarNpcs(sala, origem);
            if (json == null)
            {
                Mostrar(Resultado.Falha($"{origem}: nenhum NPC para exportar."));
                return;
            }
            if (Troca.Arquivos == null)
                return;
            string caminho;
            try
            {
                caminho = Troca.Arquivos.Exportar("NPCs - " + origem, json);
            }
            catch (Exception e)
            {
                Mostrar(Resultado.Falha("Não deu para exportar: " + e.Message));
                return;
            }
            GUIUtility.systemCopyBuffer = json;
            Estilo.Confirmar(transform,
                $"NPCs exportados ({origem}) em\n<size=80%>{caminho}</size>\nO JSON também foi copiado (Ctrl+V para colar).",
                "Abrir a pasta",
                () => Application.OpenURL("file:///" + Troca.Arquivos.PastaExportacoes.Replace('\\', '/')));
        }

        /// <summary>Um dos .json de dados/importar, ou a área de transferência; depois, para onde vão.</summary>
        void Importar()
        {
            if (Troca.Arquivos == null)
                return;
            var opcoes = new List<(string, UnityEngine.Events.UnityAction, bool)>();
            foreach (var caminho in Troca.Importaveis(CadastroPersonagens.FormatoDeTrocaNpcs))
            {
                var arquivo = caminho;
                opcoes.Add((System.IO.Path.GetFileName(arquivo), () => EscolherDestino(Troca.Arquivos.Ler(arquivo)), false));
            }
            opcoes.Add(("Colar da área de transferência", () => EscolherDestino(GUIUtility.systemCopyBuffer), false));
            opcoes.Add(("Abrir a pasta de importar", () =>
                Application.OpenURL("file:///" + Troca.Arquivos.PastaImportar.Replace('\\', '/')), true));
            Estilo.Escolher(transform, "Importar NPCs", opcoes.ToArray());
        }

        void EscolherDestino(string json)
        {
            var opcoes = new List<(string, UnityEngine.Events.UnityAction, bool)>
            {
                ("Laboratório de NPCs", () => ImportarPara(json, null, "o laboratório"), false),
            };
            foreach (var s in salas.Listar())
            {
                var sala = s;
                opcoes.Add(($"Campanha {sala.Nome}", () => ImportarPara(json, sala.Id, sala.Nome), !sala.Ativo));
            }
            Estilo.Escolher(transform, "Importar para…", opcoes.ToArray());
        }

        void ImportarPara(string json, string destino, string nomeDoDestino)
        {
            var resultado = cadastro.ImportarNpcs(json, destino, out var novos);
            Mostrar(resultado, resultado.Ok ? $"{novos.Count} NPC{(novos.Count == 1 ? "" : "s")} importado{(novos.Count == 1 ? "" : "s")} para {nomeDoDestino}." : null);
            Redesenhar();
        }

        void AbrirFicha(Personagem npc)
        {
            if (painelNpc == null)
                painelNpc = PainelNpc.Criar(transform.parent);
            painelNpc.Abrir(npc, cadastro, Redesenhar);
        }

        void Mostrar(Resultado? resultado, string sucesso = null)
        {
            var erro = resultado.HasValue && !resultado.Value.Ok ? resultado.Value.Erro : null;
            mensagem.color = erro != null ? Estilo.Erro : Estilo.TextoSuave;
            mensagem.text = erro ?? sucesso ?? string.Empty;
        }

        // ---------- montagem ----------

        void Montar(RectTransform veu)
        {
            var cartao = Estilo.Rect("Cartao", veu);
            cartao.anchorMin = new Vector2(0.5f, 0.5f);
            cartao.anchorMax = new Vector2(0.5f, 0.5f);
            cartao.sizeDelta = new Vector2(1680, 900);
            Estilo.Moldura(cartao.gameObject, Estilo.Cartao, Estilo.Borda);

            var coluna = cartao.gameObject.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(56, 56, 44, 44);
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
            var titulo = Estilo.Rotulo(titulos, "NPCS", 44, Estilo.Dourado, FontStyles.Bold);
            titulo.characterSpacing = 6;
            contagem = Estilo.Rotulo(titulos, string.Empty, 24, Estilo.TextoSuave);
            Estilo.BotaoContorno(cabecalho, "Exportar", 200, Estilo.Dourado, Exportar);
            Estilo.BotaoContorno(cabecalho, "Importar", 200, Estilo.Dourado, Importar);
            Estilo.BotaoContorno(cabecalho, "Voltar", 190, Estilo.TextoSuave, Fechar);

            var divisoria = Estilo.Rect("Divisoria", cartao);
            divisoria.gameObject.AddComponent<Image>().color =
                new Color(Estilo.Dourado.r, Estilo.Dourado.g, Estilo.Dourado.b, 0.35f);
            var ld = divisoria.gameObject.AddComponent<LayoutElement>();
            ld.minHeight = 2;
            ld.preferredHeight = 2;

            Cabecalhos(cartao);
            filaNovo = Estilo.Fila(cartao, 62);

            mensagem = Estilo.Rotulo(cartao, string.Empty, 22, Estilo.Erro);
            mensagem.gameObject.AddComponent<LayoutElement>().minHeight = 28;

            lista = Estilo.ListaRolavel(cartao);
        }

        static void Cabecalhos(Transform pai)
        {
            var fila = Estilo.Fila(pai, 30);
            fila.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 0, 0);
            Estilo.VaoDaAlca(fila);
            VaoFavorito(fila);
            Coluna(fila, "CODINOME / NOME DE VILÃO", -1);
            Coluna(fila, "NOME", -1);
            Coluna(fila, "MATURIDADE", LarguraPerfil);
            Coluna(fila, "PAPEL", LarguraPerfil);
            Coluna(fila, string.Empty, LarguraAtivo);
            Coluna(fila, string.Empty, LarguraCopiar);
            Coluna(fila, string.Empty, LarguraFicha);
        }

        static void Coluna(Transform pai, string texto, float largura)
        {
            var rotulo = Estilo.Rotulo(pai, texto, 18, Estilo.TextoSuave, FontStyles.Bold);
            rotulo.characterSpacing = 4;
            if (largura < 0)
                Estilo.ColunaElastica(rotulo);
            else
                rotulo.gameObject.AddComponent<LayoutElement>().preferredWidth = largura;
        }

        void MontarFilaNovo()
        {
            for (var i = filaNovo.childCount - 1; i >= 0; i--)
                Destroy(filaNovo.GetChild(i).gameObject);

            filaNovo.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 0, 0);
            Estilo.VaoDaAlca(filaNovo);
            VaoFavorito(filaNovo);
            campoCodinome = Estilo.CampoTexto(filaNovo, "Codinome / nome de vilão");
            Estilo.ColunaElastica(campoCodinome);
            campoCodinome.onSubmit.AddListener(_ => Adicionar());
            campoNome = Estilo.CampoTexto(filaNovo, "Nome");
            Estilo.ColunaElastica(campoNome);
            campoNome.onSubmit.AddListener(_ => Adicionar());
            Estilo.ListaSuspensa(filaNovo, LarguraPerfil, OpcoesMaturidade(), maturidadeNova.ToString(),
                id => maturidadeNova = (Maturidade)Enum.Parse(typeof(Maturidade), id));
            Estilo.ListaSuspensa(filaNovo, LarguraPerfil, OpcoesPapel(), papelNovo.ToString(),
                id => papelNovo = (Papel)Enum.Parse(typeof(Papel), id));
            Estilo.BotaoPrincipal(filaNovo, "Criar", LarguraAtivo + LarguraCopiar + LarguraFicha + 28, Adicionar);
        }

        void Redesenhar()
        {
            for (var i = lista.childCount - 1; i >= 0; i--)
                Destroy(lista.GetChild(i).gameObject);

            var doLab = cadastro.NpcsDoLaboratorio();
            var ativos = doLab.Count(n => n.Ativo);
            var nasSalas = salas.Listar().Sum(s => cadastro.NpcsDaSala(s.Id).Count);
            contagem.text = $"Laboratório · {ativos} ativo{(ativos == 1 ? "" : "s")} · {doLab.Count - ativos} inativo{(doLab.Count - ativos == 1 ? "" : "s")}" +
                            $" · {nasSalas} nas campanhas";

            if (doLab.Count == 0)
                Estilo.Rotulo(lista, "Nenhum NPC no laboratório ainda. Preencha a linha acima e Enter, ou copie um de uma campanha.",
                    24, Estilo.TextoApagado, FontStyles.Italic).textWrappingMode = TextWrappingModes.Normal;

            foreach (var titulo in new[] { "FAVORITOS", "ATIVOS", "INATIVOS" })
            {
                var doBloco = doLab.Where(n => titulo == "FAVORITOS" ? n.Favorito
                        : !n.Favorito && n.Ativo == (titulo == "ATIVOS"))
                    .OrderBy(n => n.Ordem)
                    .ThenBy(n => n.NomeExibicao, StringComparer.CurrentCultureIgnoreCase).ToList();
                if (doBloco.Count == 0)
                    continue;
                var bloco = Estilo.Bloco(lista, titulo, doBloco.Count);
                foreach (var npc in doBloco)
                    LinhaDoLaboratorio(npc, bloco);
            }

            // As campanhas: só para abrir e copiar (editar e ordenar é na aba NPCs de cada sala).
            foreach (var sala in salas.Listar())
            {
                var daSala = cadastro.NpcsDaSala(sala.Id);
                if (daSala.Count == 0)
                    continue;
                var bloco = Estilo.Bloco(lista, $"CAMPANHA · {sala.Nome.ToUpperInvariant()}{(sala.Ativo ? "" : " (inativa)")}", daSala.Count);
                foreach (var npc in daSala)
                    LinhaDaCampanha(npc, bloco);
            }
        }

        void LinhaDoLaboratorio(Personagem npc, RectTransform bloco)
        {
            var linha = Estilo.Fila(bloco, 64);
            linha.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 8, 8);
            Estilo.Moldura(linha.gameObject, Estilo.Linha, npc.Ativo ? Estilo.Borda : Estilo.BordaApagada);
            Estilo.Alca(linha, indice => Mostrar(cadastro.Mover(npc.Id, indice)));
            BotaoFavorito(npc, linha);

            var codinome = Estilo.CampoTexto(linha, "codinome / nome de vilão");
            codinome.text = npc.Identidade.Codinome;
            var nome = Estilo.CampoTexto(linha, "nome");
            nome.text = npc.Identidade.Nome;
            foreach (var campo in new[] { codinome, nome })
            {
                Estilo.ColunaElastica(campo);
                if (!npc.Ativo)
                {
                    campo.textComponent.color = Estilo.TextoApagado;
                    campo.textComponent.fontStyle = FontStyles.Italic;
                }
                campo.onSubmit.AddListener(_ => Renomear(npc, nome.text, codinome.text));
                campo.onDeselect.AddListener(_ => Renomear(npc, nome.text, codinome.text));
            }

            Estilo.ListaSuspensa(linha, LarguraPerfil, OpcoesMaturidade(), npc.Maturidade.ToString(), id =>
            {
                Mostrar(cadastro.DefinirMaturidade(npc.Id, (Maturidade)Enum.Parse(typeof(Maturidade), id)));
                Redesenhar();
            });
            Estilo.ListaSuspensa(linha, LarguraPerfil, OpcoesPapel(), npc.Papel.ToString(), id =>
            {
                Mostrar(cadastro.DefinirPapel(npc.Id, (Papel)Enum.Parse(typeof(Papel), id)));
                Redesenhar();
            });

            BotoesDaLinha(npc, linha);
        }

        /// <summary>NPC de uma campanha: nome, de onde veio (ficha de alguém ou reboot), perfil; abrir e copiar.</summary>
        void LinhaDaCampanha(Personagem npc, RectTransform bloco)
        {
            var linha = Estilo.Fila(bloco, 64);
            linha.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 8, 8);
            Estilo.Moldura(linha.gameObject, Estilo.Linha, npc.Ativo ? Estilo.Borda : Estilo.BordaApagada);
            Estilo.VaoDaAlca(linha);
            VaoFavorito(linha);

            var dourado = ColorUtility.ToHtmlStringRGB(Estilo.Dourado);
            var suave = ColorUtility.ToHtmlStringRGB(Estilo.TextoSuave);
            var de = cadastro.OrigemDoNpc(npc) is { } o ? $"{o.dono.NomeExibicao} · {o.vinculo}"
                : cadastro.OrigemDaCopia(npc) is { } origem ? $"reboot de {origem.NomeExibicao}{OndeEsta(origem)}"
                : null;
            var texto = $"<b>{npc.NomeExibicao}</b>" +
                        (de == null ? string.Empty : $"  <size=80%><color=#{dourado}>{de}</color></size>");
            var rotulo = Estilo.Rotulo(linha, texto, 24, npc.Ativo ? Estilo.Texto : Estilo.TextoApagado);
            rotulo.textWrappingMode = TextWrappingModes.NoWrap;
            rotulo.overflowMode = TextOverflowModes.Ellipsis;
            Estilo.ColunaElastica(rotulo, 2);

            foreach (var t in new[] { npc.Maturidade == Maturidade.Adulto ? "Adulto" : "Jovem", NomeDoPapel(npc.Papel) })
            {
                var r = Estilo.Rotulo(linha, $"<color=#{suave}>{t}</color>", 22, Estilo.TextoSuave);
                r.alignment = TextAlignmentOptions.Center;
                r.gameObject.AddComponent<LayoutElement>().preferredWidth = LarguraPerfil;
            }

            BotoesDaLinha(npc, linha);
        }

        string OndeEsta(Personagem npc) =>
            npc.Sala == null ? " (laboratório)" : salas.Buscar(npc.Sala) is { } s ? $" ({s.Nome})" : string.Empty;

        void BotoesDaLinha(Personagem npc, Transform linha)
        {
            if (npc.Ativo)
                Estilo.BotaoContorno(linha, "Ativo", LarguraAtivo, Estilo.Dourado, () => AlternarAtivo(npc));
            else
                Estilo.BotaoContorno(linha, "Inativo", LarguraAtivo, Estilo.TextoApagado, () => AlternarAtivo(npc));
            Estilo.BotaoContorno(linha, "Copiar…", LarguraCopiar, Estilo.Dourado, () => CopiarPara(npc));
            Estilo.BotaoPrincipal(linha, "Ficha", LarguraFicha, () => AbrirFicha(npc));
        }

        static void VaoFavorito(Transform pai)
        {
            var vao = Estilo.Rect("VaoFavorito", pai);
            vao.gameObject.AddComponent<LayoutElement>().preferredWidth = LarguraFavorito;
        }

        void BotaoFavorito(Personagem npc, Transform pai)
        {
            var rect = Estilo.Rect("Favorito", pai);
            rect.gameObject.AddComponent<LayoutElement>().preferredWidth = LarguraFavorito;
            var alvo = rect.gameObject.AddComponent<Image>();
            alvo.color = Color.clear;
            var botao = rect.gameObject.AddComponent<Button>();
            botao.targetGraphic = alvo;
            var quadrado = Estilo.Rect("Marca", rect);
            quadrado.anchorMin = quadrado.anchorMax = new Vector2(0.5f, 0.5f);
            quadrado.sizeDelta = new Vector2(28, 28);
            if (texturaFavorito == null)
                texturaFavorito = Resources.Load<Texture2D>("Icones/favorito");
            var imagem = quadrado.gameObject.AddComponent<RawImage>();
            imagem.texture = texturaFavorito;
            imagem.color = npc.Favorito ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.55f);
            imagem.raycastTarget = false;
            rect.gameObject.AddComponent<Dica>().Configurar(npc.Favorito ? "Remover dos favoritos" : "Marcar como favorito");
            botao.onClick.AddListener(() =>
            {
                var resultado = cadastro.DefinirFavorito(npc.Id, !npc.Favorito);
                Mostrar(resultado);
                if (resultado.Ok)
                    Redesenhar();
            });
        }
    }
}
