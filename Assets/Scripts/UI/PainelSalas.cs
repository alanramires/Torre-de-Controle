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
    /// Gestão de Salas: criar (nome + slots), renomear, mudar slots, ativar/inativar, entrar.
    /// Mesmo esquema dos outros cadastros: tudo grava na hora; editar na própria linha.
    /// </summary>
    public sealed class PainelSalas : MonoBehaviour
    {
        const float LarguraSlots = 200;
        const float LarguraOcupacao = 220;
        const float LarguraAtivo = 170;
        const float LarguraEntrar = 150;
        const int SlotsPadrao = 6;

        CadastroSalas cadastro;
        CadastroPersonagens personagens;
        CadastroJogadores jogadores;
        PainelSala painelSala;

        TMP_InputField campoNome;
        int slotsNovo = SlotsPadrao;
        RectTransform filaNovo;
        TextMeshProUGUI mensagem;
        TextMeshProUGUI contagem;
        RectTransform lista;

        public static PainelSalas Criar(Transform pai)
        {
            var veu = Estilo.Rect("PainelSalas", pai);
            Estilo.Esticar(veu);
            veu.gameObject.AddComponent<Image>().color = Estilo.Veu;
            veu.gameObject.AddComponent<NavegacaoTab>();
            var painel = veu.gameObject.AddComponent<PainelSalas>();
            painel.Montar(veu);
            return painel;
        }

        public void Abrir(CadastroSalas cadastroSalas, CadastroPersonagens cadastroPersonagens, CadastroJogadores cadastroJogadores)
        {
            cadastro = cadastroSalas;
            personagens = cadastroPersonagens;
            jogadores = cadastroJogadores;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Mostrar(null);
            MontarFilaNovo();
            Redesenhar();
            campoNome.ActivateInputField();
        }

        public void Fechar() => gameObject.SetActive(false);

        static IReadOnlyList<(string id, string nome)> OpcoesSlots() =>
            Enumerable.Range(CadastroSalas.MinimoDeSlots, CadastroSalas.MaximoDeSlots - CadastroSalas.MinimoDeSlots + 1)
                .Select(n => (n.ToString(), n == 1 ? "1 vaga" : $"{n} vagas"))
                .ToList();

        // ---------- ações ----------

        void Adicionar()
        {
            var resultado = cadastro.Criar(campoNome.text, slotsNovo);
            Mostrar(resultado);
            if (resultado.Ok)
            {
                campoNome.text = string.Empty;
                Redesenhar();
            }
            campoNome.ActivateInputField();
        }

        void Mostrar(Resultado? resultado)
        {
            var erro = resultado.HasValue && !resultado.Value.Ok ? resultado.Value.Erro : null;
            mensagem.text = erro ?? string.Empty;
        }

        void Entrar(Sala sala)
        {
            if (painelSala == null)
                painelSala = PainelSala.Criar(transform.parent);
            gameObject.SetActive(false);
            painelSala.Abrir(sala, cadastro, personagens, jogadores, () =>
            {
                gameObject.SetActive(true);
                Redesenhar();
            });
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
            var titulo = Estilo.Rotulo(titulos, "SALAS", 44, Estilo.Dourado, FontStyles.Bold);
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
            Coluna(fila, "NOME DA SALA", -1);
            Coluna(fila, "VAGAS", LarguraSlots);
            Coluna(fila, "OCUPAÇÃO", LarguraOcupacao);
            Coluna(fila, string.Empty, LarguraAtivo);
            Coluna(fila, string.Empty, LarguraEntrar);
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
            campoNome = Estilo.CampoTexto(filaNovo, "Nome da nova sala e Enter");
            Estilo.ColunaElastica(campoNome);
            campoNome.onSubmit.AddListener(_ => Adicionar());
            Estilo.ListaSuspensa(filaNovo, LarguraSlots, OpcoesSlots(), slotsNovo.ToString(), id => slotsNovo = int.Parse(id));
            Estilo.BotaoPrincipal(filaNovo, "Criar", LarguraOcupacao + LarguraAtivo + LarguraEntrar + 28, Adicionar);
        }

        void Redesenhar()
        {
            for (var i = lista.childCount - 1; i >= 0; i--)
                Destroy(lista.GetChild(i).gameObject);

            var todas = cadastro.Listar();
            var ativas = todas.Count(s => s.Ativo);
            contagem.text = todas.Count == 0
                ? "Campanhas: cada sala tem suas vagas (jogador + personagem)"
                : $"{ativas} ativa{(ativas == 1 ? "" : "s")} · {todas.Count - ativas} inativa{(todas.Count - ativas == 1 ? "" : "s")}";

            if (todas.Count == 0)
            {
                Estilo.Rotulo(lista, "Nenhuma sala ainda. Dê um nome na linha acima e Enter.", 26,
                    Estilo.TextoApagado, FontStyles.Italic);
                return;
            }

            foreach (var (ativo, titulo) in new[] { (true, "ATIVAS"), (false, "INATIVAS") })
            {
                var doBloco = todas.Where(s => s.Ativo == ativo).ToList();
                if (doBloco.Count == 0)
                    continue;
                var bloco = Estilo.Bloco(lista, titulo, doBloco.Count);
                foreach (var sala in doBloco)
                    Linha(sala, bloco);
            }
        }

        void Linha(Sala sala, RectTransform bloco)
        {
            var linha = Estilo.Fila(bloco, 64);
            linha.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 8, 8);
            Estilo.Moldura(linha.gameObject, Estilo.Linha, sala.Ativo ? Estilo.Borda : Estilo.BordaApagada);
            Estilo.Alca(linha, indice => Mostrar(cadastro.Mover(sala.Id, indice)));

            var nome = Estilo.CampoTexto(linha, "nome da sala");
            nome.text = sala.Nome;
            Estilo.ColunaElastica(nome);
            if (!sala.Ativo)
            {
                nome.textComponent.color = Estilo.TextoApagado;
                nome.textComponent.fontStyle = FontStyles.Italic;
            }
            // No sucesso não redesenha (o Tab já foi para o próximo campo); no erro volta ao gravado.
            void Renomear()
            {
                var r = cadastro.Renomear(sala.Id, nome.text);
                Mostrar(r);
                if (!r.Ok)
                    Redesenhar();
            }
            nome.onSubmit.AddListener(_ => Renomear());
            nome.onDeselect.AddListener(_ => Renomear());

            Estilo.ListaSuspensa(linha, LarguraSlots, OpcoesSlots(), sala.Slots.Count.ToString(), id =>
            {
                Mostrar(cadastro.DefinirSlots(sala.Id, int.Parse(id)));
                Redesenhar();
            });

            var jogadoresNaSala = sala.Slots.Count(s => s.Jogador != null);
            var personagensNaSala = sala.Slots.Count(s => s.Personagem != null);
            var ocupacao = Estilo.Rotulo(linha,
                $"{jogadoresNaSala} jog. · {personagensNaSala} pers.", 22, Estilo.TextoSuave);
            ocupacao.alignment = TextAlignmentOptions.Center;
            ocupacao.gameObject.AddComponent<LayoutElement>().preferredWidth = LarguraOcupacao;

            if (sala.Ativo)
                Estilo.BotaoContorno(linha, "Ativa", LarguraAtivo, Estilo.Dourado, () => AlternarAtivo(sala));
            else
                Estilo.BotaoContorno(linha, "Inativa", LarguraAtivo, Estilo.TextoApagado, () => AlternarAtivo(sala));

            Estilo.BotaoPrincipal(linha, "Entrar", LarguraEntrar, () => Entrar(sala));
        }

        void AlternarAtivo(Sala sala)
        {
            Mostrar(cadastro.DefinirAtivo(sala.Id, !sala.Ativo));
            Redesenhar();
        }
    }
}
