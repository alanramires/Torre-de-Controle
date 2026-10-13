using System.Collections.Generic;
using TMPro;
using TorreDeControle.Core;
using TorreDeControle.Core.Jogadores;
using UnityEngine;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Gestão de Jogadores. Montado por código (sem prefab) por cima da tela que o abre: um véu escuro
    /// sobre a tela e um cartão no centro, no visual do menu (ver <see cref="Estilo"/>).
    /// Digitar o nome + Enter cria; editar o nome na lista + Enter renomeia; o botão alterna ativo/inativo.
    /// </summary>
    public sealed class PainelJogadores : MonoBehaviour
    {
        CadastroJogadores cadastro;
        TMP_InputField campoNovo;
        TextMeshProUGUI mensagem;
        TextMeshProUGUI contagem;
        RectTransform lista;

        public static PainelJogadores Criar(Transform pai)
        {
            var veu = Estilo.Rect("PainelJogadores", pai);
            Estilo.Esticar(veu);
            // O véu bloqueia cliques na tela de baixo.
            veu.gameObject.AddComponent<Image>().color = Estilo.Veu;
            veu.gameObject.AddComponent<NavegacaoTab>();
            var painel = veu.gameObject.AddComponent<PainelJogadores>();
            painel.Montar(veu);
            return painel;
        }

        public void Abrir(CadastroJogadores cadastroJogadores)
        {
            cadastro = cadastroJogadores;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Mostrar(null);
            Redesenhar();
            campoNovo.ActivateInputField();
        }

        public void Fechar() => gameObject.SetActive(false);

        // ---------- ações ----------

        void Adicionar(string nome)
        {
            var resultado = cadastro.Criar(nome);
            Mostrar(resultado);
            if (resultado.Ok)
            {
                campoNovo.text = string.Empty;
                Redesenhar();
            }
            campoNovo.ActivateInputField();
        }

        void Renomear(Jogador jogador, TMP_InputField campo)
        {
            // Sair do campo sem mudar nada não grava nem redesenha.
            if (campo.text.Trim() == jogador.Nome)
                return;

            var resultado = cadastro.Renomear(jogador.Id, campo.text);
            Mostrar(resultado);
            // No sucesso não redesenha: o campo já mostra o nome, e redesenhar destruiria
            // o campo para onde o Tab acabou de ir.
            if (!resultado.Ok)
                campo.text = jogador.Nome;
        }

        void AlternarAtivo(Jogador jogador)
        {
            Mostrar(cadastro.DefinirAtivo(jogador.Id, !jogador.Ativo));
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
            cartao.sizeDelta = new Vector2(1180, 860);
            Estilo.Moldura(cartao.gameObject, Estilo.Cartao, Estilo.Borda);

            var coluna = cartao.gameObject.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(56, 56, 44, 44);
            coluna.spacing = 14;
            coluna.childControlWidth = true;
            coluna.childControlHeight = true;
            coluna.childForceExpandHeight = false;

            // Cabeçalho: título dourado + subtítulo, voltar à direita
            var cabecalho = Estilo.Fila(cartao, 84);
            var titulos = Estilo.Rect("Titulos", cabecalho);
            titulos.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var pilha = titulos.gameObject.AddComponent<VerticalLayoutGroup>();
            pilha.childControlWidth = true;
            pilha.childControlHeight = true;
            pilha.childForceExpandHeight = false;
            pilha.childAlignment = TextAnchor.MiddleLeft;
            var titulo = Estilo.Rotulo(titulos, "JOGADORES", 44, Estilo.Dourado, FontStyles.Bold);
            titulo.characterSpacing = 6;
            contagem = Estilo.Rotulo(titulos, string.Empty, 24, Estilo.TextoSuave);
            Estilo.BotaoContorno(cabecalho, "Voltar", 190, Estilo.TextoSuave, Fechar);

            Divisoria(cartao);

            // Novo jogador
            var novo = Estilo.Fila(cartao, 62);
            novo.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 0, 0);
            Estilo.VaoDaAlca(novo);
            campoNovo = Estilo.CampoTexto(novo, "Nome do novo jogador e Enter");
            Estilo.ColunaElastica(campoNovo);
            campoNovo.onSubmit.AddListener(Adicionar);
            Estilo.BotaoPrincipal(novo, "Adicionar", 230, () => Adicionar(campoNovo.text));

            mensagem = Estilo.Rotulo(cartao, string.Empty, 22, Estilo.Erro);
            mensagem.gameObject.AddComponent<LayoutElement>().minHeight = 28;

            lista = Estilo.ListaRolavel(cartao);
        }

        static void Divisoria(Transform pai)
        {
            var linha = Estilo.Rect("Divisoria", pai);
            linha.gameObject.AddComponent<Image>().color = new Color(Estilo.Dourado.r, Estilo.Dourado.g, Estilo.Dourado.b, 0.35f);
            var layout = linha.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 2;
            layout.preferredHeight = 2;
        }

        void Redesenhar()
        {
            for (var i = lista.childCount - 1; i >= 0; i--)
                Destroy(lista.GetChild(i).gameObject);

            var todos = cadastro.Listar();
            var ativos = 0;
            foreach (var j in todos)
                if (j.Ativo) ativos++;
            contagem.text = todos.Count == 0
                ? "Cadastros e status"
                : $"{ativos} ativo{(ativos == 1 ? "" : "s")} · {todos.Count - ativos} inativo{(todos.Count - ativos == 1 ? "" : "s")}";

            if (todos.Count == 0)
            {
                Estilo.Rotulo(lista, "Nenhum jogador ainda. Digite um nome acima.", 26, Estilo.TextoApagado, FontStyles.Italic);
                return;
            }

            // Dois blocos, cada um na ordem que o mestre arrastar.
            foreach (var (ativo, titulo) in new[] { (true, "ATIVOS"), (false, "INATIVOS") })
            {
                var doBloco = new List<Jogador>();
                foreach (var j in todos)
                    if (j.Ativo == ativo)
                        doBloco.Add(j);
                if (doBloco.Count == 0)
                    continue;
                var bloco = Estilo.Bloco(lista, titulo, doBloco.Count);
                foreach (var jogador in doBloco)
                    Linha(jogador, bloco);
            }
        }

        void Linha(Jogador jogador, RectTransform bloco)
        {
            var linha = Estilo.Fila(bloco, 64);
            linha.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 8, 8);
            Estilo.Moldura(linha.gameObject, Estilo.Linha, jogador.Ativo ? Estilo.Borda : Estilo.BordaApagada);
            // Não redesenha ao soltar: a linha já está no lugar novo.
            Estilo.Alca(linha, indice => Mostrar(cadastro.Mover(jogador.Id, indice)));

            var campo = Estilo.CampoTexto(linha, string.Empty);
            campo.text = jogador.Nome;
            if (!jogador.Ativo)
            {
                campo.textComponent.color = Estilo.TextoApagado;
                campo.textComponent.fontStyle = FontStyles.Italic;
            }
            Estilo.ColunaElastica(campo);
            campo.onSubmit.AddListener(_ => Renomear(jogador, campo));
            campo.onDeselect.AddListener(_ => Renomear(jogador, campo));

            var j = jogador;
            if (jogador.Ativo)
                Estilo.BotaoContorno(linha, "Ativo", 190, Estilo.Dourado, () => AlternarAtivo(j));
            else
                Estilo.BotaoContorno(linha, "Inativo", 190, Estilo.TextoApagado, () => AlternarAtivo(j));
        }
    }
}
