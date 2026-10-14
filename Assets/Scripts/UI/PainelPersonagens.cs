using System.Collections.Generic;
using System.Linq;
using TMPro;
using TorreDeControle.Core;
using TorreDeControle.Core.Jogadores;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Salas;
using UnityEngine;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Laboratório de personagens: criar sem vincular a sala, com jogador opcional.
    /// Mesmo esquema do painel de jogadores: tudo grava na hora; editar na própria linha.
    /// </summary>
    public sealed class PainelPersonagens : MonoBehaviour
    {
        const float LarguraArquetipo = 250;
        const float LarguraJogador = 250;
        const float LarguraStatus = 200;
        const float LarguraAtivo = 170;
        const float LarguraFicha = 150;
        const float LarguraFavorito = 36;
        const string SemJogador = "";

        CadastroPersonagens cadastro;
        CadastroJogadores jogadores;
        CadastroSalas salas;
        PainelFicha painelFicha;

        TMP_InputField campoCodinome;
        TMP_InputField campoNome;
        string arquetipoNovo;
        string jogadorNovo = SemJogador;
        RectTransform filaNovo;
        TextMeshProUGUI mensagem;
        TextMeshProUGUI contagem;
        RectTransform lista;
        static Texture2D texturaFavorito;

        public static PainelPersonagens Criar(Transform pai)
        {
            var veu = Estilo.Rect("PainelPersonagens", pai);
            Estilo.Esticar(veu);
            veu.gameObject.AddComponent<Image>().color = Estilo.Veu;
            veu.gameObject.AddComponent<NavegacaoTab>();
            var painel = veu.gameObject.AddComponent<PainelPersonagens>();
            painel.Montar(veu);
            return painel;
        }

        public void Abrir(CadastroPersonagens cadastroPersonagens, CadastroJogadores cadastroJogadores,
            CadastroSalas cadastroSalas)
        {
            cadastro = cadastroPersonagens;
            jogadores = cadastroJogadores;
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

        static IReadOnlyList<(string id, string nome)> OpcoesArquetipo() =>
            Arquetipos.Todos.Select(a => (a.Id, a.Nome)).ToList();

        /// <summary>Jogadores ativos; o atual entra mesmo se estiver inativo, para não sumir da linha.</summary>
        IReadOnlyList<(string id, string nome)> OpcoesJogador(string atual)
        {
            var opcoes = new List<(string id, string nome)> { (SemJogador, "sem jogador") };
            foreach (var j in jogadores.Listar())
                if (j.Ativo || j.Id == atual)
                    opcoes.Add((j.Id, j.Ativo ? j.Nome : j.Nome + " (inativo)"));
            return opcoes;
        }

        // ---------- ações ----------

        void Adicionar()
        {
            var resultado = cadastro.Criar(campoNome.text, campoCodinome.text, arquetipoNovo, jogadorNovo);
            Mostrar(resultado);
            if (resultado.Ok)
            {
                campoNome.text = string.Empty;
                campoCodinome.text = string.Empty;
                Redesenhar();
            }
            campoCodinome.ActivateInputField();
        }

        /// <summary>
        /// Grava uma mudança feita na linha. No sucesso NÃO redesenha: a linha já mostra o valor,
        /// e redesenhar destruiria o campo para onde o Tab acabou de ir. No erro redesenha,
        /// para a linha voltar ao valor gravado.
        /// </summary>
        void Alterar(Personagem p, string nome, string codinome, string arquetipo, string jogador)
        {
            var i = p.Identidade;
            if (nome.Trim() == i.Nome && codinome.Trim() == i.Codinome && arquetipo == i.Arquetipo &&
                (jogador ?? SemJogador) == (i.Jogador ?? SemJogador))
                return;

            var resultado = cadastro.AlterarIdentidade(p.Id, nome, codinome, arquetipo, jogador);
            Mostrar(resultado);
            if (!resultado.Ok)
                Redesenhar();
        }

        void AlternarAtivo(Personagem p)
        {
            Mostrar(cadastro.DefinirAtivo(p.Id, !p.Ativo));
            Redesenhar();
        }

        void Mostrar(Resultado? resultado)
        {
            var erro = resultado.HasValue && !resultado.Value.Ok ? resultado.Value.Erro : null;
            mensagem.text = erro ?? string.Empty;
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
            var titulo = Estilo.Rotulo(titulos, "PERSONAGENS", 44, Estilo.Dourado, FontStyles.Bold);
            titulo.characterSpacing = 6;
            contagem = Estilo.Rotulo(titulos, string.Empty, 24, Estilo.TextoSuave);
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
            Coluna(fila, "CODINOME", -1);
            Coluna(fila, "NOME CIVIL", -1);
            Coluna(fila, "ARQUÉTIPO", LarguraArquetipo);
            Coluna(fila, "JOGADOR", LarguraJogador);
            Coluna(fila, "STATUS", LarguraStatus);
            Coluna(fila, string.Empty, LarguraAtivo);
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

        /// <summary>Refeita ao abrir, para o seletor de jogador refletir o cadastro atual.</summary>
        void MontarFilaNovo()
        {
            for (var i = filaNovo.childCount - 1; i >= 0; i--)
                Destroy(filaNovo.GetChild(i).gameObject);

            filaNovo.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 0, 0);
            arquetipoNovo ??= Arquetipos.Todos[0].Id;
            if (jogadores.Buscar(jogadorNovo) == null)
                jogadorNovo = SemJogador;

            Estilo.VaoDaAlca(filaNovo);
            VaoFavorito(filaNovo);
            campoCodinome = Estilo.CampoTexto(filaNovo, "Codinome");
            Estilo.ColunaElastica(campoCodinome);
            campoCodinome.onSubmit.AddListener(_ => Adicionar());
            campoNome = Estilo.CampoTexto(filaNovo, "Nome civil");
            Estilo.ColunaElastica(campoNome);
            campoNome.onSubmit.AddListener(_ => Adicionar());
            Estilo.ListaSuspensa(filaNovo, LarguraArquetipo, OpcoesArquetipo(), arquetipoNovo, id => arquetipoNovo = id);
            Estilo.ListaSuspensa(filaNovo, LarguraJogador, OpcoesJogador(jogadorNovo), jogadorNovo, id => jogadorNovo = id);
            Estilo.BotaoPrincipal(filaNovo, "Criar", LarguraStatus + LarguraAtivo + LarguraFicha + 28, Adicionar);
        }

        void Redesenhar()
        {
            for (var i = lista.childCount - 1; i >= 0; i--)
                Destroy(lista.GetChild(i).gameObject);

            var todas = cadastro.Listar();
            var ativas = todas.Count(p => p.Ativo);
            contagem.text = todas.Count == 0
                ? "Laboratório: fichas sem sala"
                : $"Laboratório · {ativas} ativa{(ativas == 1 ? "" : "s")} · {todas.Count - ativas} inativa{(todas.Count - ativas == 1 ? "" : "s")}";

            if (todas.Count == 0)
            {
                Estilo.Rotulo(lista, "Nenhuma personagem ainda. Preencha a linha acima e Enter.", 26,
                    Estilo.TextoApagado, FontStyles.Italic);
                return;
            }

            // Favoritas têm seção própria; o estado ativo/inativo continua na linha.
            foreach (var titulo in new[] { "FAVORITOS", "ATIVAS", "INATIVAS" })
            {
                var doBloco = todas.Where(p => titulo == "FAVORITOS" ? p.Favorito
                    : !p.Favorito && p.Ativo == (titulo == "ATIVAS"))
                    .OrderBy(p => p.Ordem)
                    .ThenBy(p => p.NomeExibicao, System.StringComparer.CurrentCultureIgnoreCase).ToList();
                if (doBloco.Count == 0)
                    continue;
                var bloco = Estilo.Bloco(lista, titulo, doBloco.Count);
                foreach (var personagem in doBloco)
                    Linha(personagem, bloco);
            }
        }

        void Linha(Personagem p, RectTransform bloco)
        {
            var linha = Estilo.Fila(bloco, 64);
            linha.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 8, 8);
            Estilo.Moldura(linha.gameObject, Estilo.Linha, p.Ativo ? Estilo.Borda : Estilo.BordaApagada);
            // Não redesenha ao soltar: a linha já está no lugar novo.
            Estilo.Alca(linha, indice => Mostrar(cadastro.Mover(p.Id, indice)));
            BotaoFavorito(p, linha);

            var i = p.Identidade;
            var codinome = Estilo.CampoTexto(linha, "codinome");
            codinome.text = i.Codinome;
            var nome = Estilo.CampoTexto(linha, "nome civil");
            nome.text = i.Nome;
            foreach (var campo in new[] { codinome, nome })
            {
                Estilo.ColunaElastica(campo);
                if (!p.Ativo)
                {
                    campo.textComponent.color = Estilo.TextoApagado;
                    campo.textComponent.fontStyle = FontStyles.Italic;
                }
                // Lê p.Identidade na hora: ela é trocada a cada gravação, e capturar o objeto
                // antigo faria uma edição desfazer a anterior.
                campo.onSubmit.AddListener(_ => Alterar(p, nome.text, codinome.text, p.Identidade.Arquetipo, p.Identidade.Jogador));
                campo.onDeselect.AddListener(_ => Alterar(p, nome.text, codinome.text, p.Identidade.Arquetipo, p.Identidade.Jogador));
            }

            // Arquétipo só se escolhe enquanto novo; depois, trocar é o avanço "mudar de arquétipo".
            Estilo.ListaSuspensa(linha, LarguraArquetipo, OpcoesArquetipo(), i.Arquetipo,
                id => Alterar(p, p.Identidade.Nome, p.Identidade.Codinome, id, p.Identidade.Jogador),
                editavel: p.EmCriacao);
            Estilo.ListaSuspensa(linha, LarguraJogador, OpcoesJogador(i.Jogador), i.Jogador ?? SemJogador,
                id => Alterar(p, p.Identidade.Nome, p.Identidade.Codinome, p.Identidade.Arquetipo, id));

            if (p.Status == StatusCriacao.EmAprovacao && cadastro.MotivoParaNaoReabrir(p) == null)
            {
                // Provisório, enquanto a ficha está em construção: clicar volta para "novo".
                Estilo.BotaoContorno(linha, TextoStatus(p.Status), LarguraStatus, Estilo.Dourado, () => Reabrir(p));
            }
            else
            {
                var status = Estilo.Rotulo(linha, TextoStatus(p.Status), 22, Estilo.Dourado, FontStyles.Bold);
                status.alignment = TextAlignmentOptions.Center;
                status.characterSpacing = 3;
                status.gameObject.AddComponent<LayoutElement>().preferredWidth = LarguraStatus;
            }

            if (p.Ativo)
                Estilo.BotaoContorno(linha, "Ativa", LarguraAtivo, Estilo.Dourado, () => AlternarAtivo(p));
            else
                Estilo.BotaoContorno(linha, "Inativa", LarguraAtivo, Estilo.TextoApagado, () => AlternarAtivo(p));

            Estilo.BotaoPrincipal(linha, "Ficha", LarguraFicha, () => EscolherFicha(p));
        }

        static void VaoFavorito(Transform pai)
        {
            var vao = Estilo.Rect("VaoFavorito", pai);
            vao.gameObject.AddComponent<LayoutElement>().preferredWidth = LarguraFavorito;
        }

        void BotaoFavorito(Personagem p, Transform pai)
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
            imagem.color = p.Favorito ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.55f);
            imagem.raycastTarget = false;
            var dica = rect.gameObject.AddComponent<Dica>();
            dica.Configurar(p.Favorito ? "Remover dos favoritos" : "Marcar como favorito");
            botao.onClick.AddListener(() =>
            {
                var resultado = cadastro.DefinirFavorito(p.Id, !p.Favorito);
                Mostrar(resultado);
                if (!resultado.Ok) return;
                imagem.color = p.Favorito ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.55f);
                dica.Configurar(p.Favorito ? "Remover dos favoritos" : "Marcar como favorito");
                Redesenhar();
            });
        }

        /// <summary>
        /// Com cópias em salas, pergunta qual ficha abrir: as instâncias nas vagas, a original (laboratório)
        /// e, por último e em tom pálido, as tiradas das vagas (arquivo da sala, para consulta).
        /// Sem cópias, abre direto.
        /// </summary>
        void EscolherFicha(Personagem p)
        {
            var copias = cadastro.CopiasDe(p.Id);
            if (copias.Count == 0)
            {
                AbrirFicha(p);
                return;
            }
            var opcoes = new List<(string, UnityEngine.Events.UnityAction, bool)>();
            foreach (var c in copias.Where(c => c.Ativo))
                opcoes.Add((Instancia(c, null), () => AbrirFicha(c), false));
            opcoes.Add((p.NomeExibicao, () => AbrirFicha(p), false));
            foreach (var c in copias.Where(c => !c.Ativo))
                opcoes.Add((Instancia(c, "vaga liberada"), () => AbrirFicha(c), true));
            Estilo.Escolher(transform, $"Qual ficha de {p.NomeExibicao}?", opcoes.ToArray());
        }

        /// <summary>"Titânia (Nova Aeternia)", com o que mais houver a dizer da sala ou da vaga.</summary>
        string Instancia(Personagem copia, string nota)
        {
            var sala = salas?.Buscar(copia.Sala);
            var onde = sala?.Nome ?? "sala";
            if (sala != null && !sala.Ativo)
                onde += " · sala inativa";
            if (nota != null)
                onde += " · " + nota;
            return $"{copia.NomeExibicao} ({onde})";
        }

        void Reabrir(Personagem p)
        {
            Estilo.Confirmar(transform,
                "Voltar a ficha para \"novo\"? A foto da criação é descartada e a criação fica editável de novo.",
                "Voltar para novo",
                () =>
                {
                    cadastro.ReabrirCriacao(p.Id);
                    Redesenhar();
                });
        }

        void AbrirFicha(Personagem p)
        {
            if (painelFicha == null)
                painelFicha = PainelFicha.Criar(transform.parent);
            // Ao fechar a ficha, a lista volta atualizada (status, nomes e arquétipo podem ter mudado).
            painelFicha.Abrir(p, cadastro, jogadores, Redesenhar, p.Sala == null ? null : salas?.Buscar(p.Sala)?.Nome);
        }

        static string TextoStatus(StatusCriacao status) => status switch
        {
            StatusCriacao.Novo => "NOVO",
            StatusCriacao.EmRevisao => "EM REVISÃO",
            StatusCriacao.EmAprovacao => "EM APROVAÇÃO",
            StatusCriacao.Aposentado => "APOSENTADO",
            _ => "EM CARREIRA",
        };
    }
}
