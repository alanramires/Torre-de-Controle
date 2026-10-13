using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TorreDeControle.Core;
using TorreDeControle.Core.Jogadores;
using TorreDeControle.Core.Personagens;
using UnityEngine;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// A ficha da personagem como um fichário: uma aba por seção (Identidade, Núcleo, Crescimento,
    /// Módulos). Em "novo" é o assistente de criação, com validação e Aprovar no rodapé.
    /// </summary>
    public sealed class PainelFicha : MonoBehaviour
    {
        enum Aba { Identidade, Nucleo, Crescimento, Modulos }

        static readonly (Aba aba, string nome)[] Abas =
        {
            (Aba.Identidade, "Identidade"),
            (Aba.Nucleo, "Núcleo"),
            (Aba.Crescimento, "Crescimento"),
            (Aba.Modulos, "Módulos"),
        };

        Personagem p;
        CadastroPersonagens cadastro;
        CadastroJogadores jogadores;
        Action aoFechar;
        Aba abaAtual = Aba.Identidade;

        TextMeshProUGUI titulo;
        TextMeshProUGUI subtitulo;
        TextMeshProUGUI status;
        RectTransform filaAbas;
        RectTransform conteudo;
        RectTransform rodape;

        public static PainelFicha Criar(Transform pai)
        {
            var veu = Estilo.Rect("PainelFicha", pai);
            Estilo.Esticar(veu);
            veu.gameObject.AddComponent<Image>().color = Estilo.Veu;
            veu.gameObject.AddComponent<NavegacaoTab>();
            var painel = veu.gameObject.AddComponent<PainelFicha>();
            painel.Montar(veu);
            return painel;
        }

        public void Abrir(Personagem personagem, CadastroPersonagens cadastroPersonagens,
            CadastroJogadores cadastroJogadores, Action aoFecharFicha)
        {
            p = personagem;
            cadastro = cadastroPersonagens;
            jogadores = cadastroJogadores;
            aoFechar = aoFecharFicha;
            abaAtual = p.Status == StatusCriacao.Novo ? Aba.Nucleo : Aba.Identidade;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Atualizar();
        }

        void Fechar()
        {
            gameObject.SetActive(false);
            aoFechar?.Invoke();
        }

        // ---------- montagem fixa ----------

        void Montar(RectTransform veu)
        {
            var cartao = Estilo.Rect("Cartao", veu);
            cartao.anchorMin = cartao.anchorMax = new Vector2(0.5f, 0.5f);
            cartao.sizeDelta = new Vector2(1680, 940);
            Estilo.Moldura(cartao.gameObject, Estilo.Cartao, Estilo.Borda);

            var coluna = cartao.gameObject.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(56, 56, 34, 30);
            coluna.spacing = 12;
            coluna.childControlWidth = true;
            coluna.childControlHeight = true;
            coluna.childForceExpandHeight = false;

            var cabecalho = Estilo.Fila(cartao, 76);
            var titulos = Estilo.Rect("Titulos", cabecalho);
            Estilo.ColunaElastica(titulos);
            var pilha = titulos.gameObject.AddComponent<VerticalLayoutGroup>();
            pilha.childControlWidth = true;
            pilha.childControlHeight = true;
            pilha.childForceExpandHeight = false;
            pilha.childAlignment = TextAnchor.MiddleLeft;
            titulo = Estilo.Rotulo(titulos, string.Empty, 44, Estilo.Dourado, FontStyles.Bold);
            titulo.characterSpacing = 4;
            subtitulo = Estilo.Rotulo(titulos, string.Empty, 24, Estilo.TextoSuave);

            status = Estilo.Rotulo(cabecalho, string.Empty, 26, Estilo.Dourado, FontStyles.Bold);
            status.alignment = TextAlignmentOptions.Center;
            status.characterSpacing = 4;
            status.gameObject.AddComponent<LayoutElement>().preferredWidth = 220;
            Estilo.BotaoContorno(cabecalho, "Voltar", 190, Estilo.TextoSuave, Fechar);

            filaAbas = Estilo.Fila(cartao, 56, 8);

            conteudo = Estilo.Rect("Conteudo", cartao);
            var lc = conteudo.gameObject.AddComponent<LayoutElement>();
            lc.flexibleHeight = 1;
            Estilo.Moldura(conteudo.gameObject, new Color(0, 0, 0, 0.18f), Estilo.BordaApagada);
            var vc = conteudo.gameObject.AddComponent<VerticalLayoutGroup>();
            vc.padding = new RectOffset(36, 36, 22, 22);
            vc.spacing = 8;
            vc.childControlWidth = true;
            vc.childControlHeight = true;
            vc.childForceExpandHeight = false;

            rodape = Estilo.Fila(cartao, 60);
        }

        // ---------- atualização ----------

        void Atualizar()
        {
            Cabecalho();
            MontarAbas();
            MontarConteudo();
            MontarRodape();
        }

        void Cabecalho()
        {
            var i = p.Identidade;
            titulo.text = p.NomeExibicao.ToUpperInvariant();
            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(i.Codinome) && !string.IsNullOrWhiteSpace(i.Nome))
                partes.Add(i.Nome);
            partes.Add(Arquetipos.Nome(i.Arquetipo));
            partes.Add(jogadores.Buscar(i.Jogador)?.Nome ?? "sem jogador");
            subtitulo.text = string.Join(" · ", partes);
            status.text = p.Status switch
            {
                StatusCriacao.Novo => "NOVO",
                StatusCriacao.Criado => "CRIADO",
                _ => "EM CARREIRA",
            };
        }

        void MontarAbas()
        {
            Limpar(filaAbas);
            foreach (var (aba, nome) in Abas)
            {
                var a = aba;
                if (aba == abaAtual)
                    Estilo.BotaoPrincipal(filaAbas, nome, 250, () => { });
                else
                    Estilo.BotaoContorno(filaAbas, nome, 250, Estilo.TextoSuave, () => TrocarAba(a));
            }
        }

        void TrocarAba(Aba aba)
        {
            abaAtual = aba;
            MontarAbas();
            MontarConteudo();
        }

        void MontarConteudo()
        {
            Limpar(conteudo);
            switch (abaAtual)
            {
                case Aba.Identidade: AbaIdentidade(); break;
                case Aba.Nucleo: AbaNucleo(); break;
                case Aba.Crescimento: EmConstrucao("Avanços marcados e cartas compradas (movimentos de outros arquétipos, adultos)."); break;
                case Aba.Modulos: EmConstrucao("Extras do arquétipo (Impulsos, Perdição, Chamas, Máscara...) e os comprados por avanço."); break;
            }
        }

        void MontarRodape()
        {
            Limpar(rodape);
            var mensagem = Estilo.Rotulo(rodape, string.Empty, 22, Estilo.TextoSuave);
            mensagem.textWrappingMode = TextWrappingModes.Normal;
            Estilo.ColunaElastica(mensagem);

            if (p.Status != StatusCriacao.Novo)
            {
                var data = DateTime.TryParse(p.Foto?.AprovadaEm, out var d) ? d.ToString("dd/MM/yyyy HH:mm") : "?";
                mensagem.text = $"Foto da criação congelada em {data}.";
                mensagem.color = Estilo.Dourado;
                return;
            }

            var (erros, avisos) = cadastro.Validar(p);
            if (erros.Count > 0)
            {
                mensagem.text = string.Join("  ", erros);
                mensagem.color = Estilo.Erro;
            }
            else if (avisos.Count > 0)
            {
                mensagem.text = "Pendências: " + string.Join("  ", avisos);
            }
            else
            {
                mensagem.text = "Tudo certo para aprovar.";
                mensagem.color = Estilo.Dourado;
            }

            Estilo.BotaoPrincipal(rodape, "Aprovar", 260, () => Aprovar(erros, avisos));
        }

        void Aprovar(IReadOnlyList<string> erros, IReadOnlyList<string> avisos)
        {
            if (erros.Count > 0)
                return;
            if (avisos.Count == 0)
            {
                Concluir(cadastro.Aprovar(p.Id, aceitarAvisos: false));
                return;
            }
            Estilo.Confirmar(transform,
                "Aprovar mesmo assim? A foto da criação será congelada com estas pendências:\n• " +
                string.Join("\n• ", avisos),
                "Aprovar",
                () => Concluir(cadastro.Aprovar(p.Id, aceitarAvisos: true)));
        }

        void Concluir(Resultado resultado)
        {
            Atualizar();
            if (!resultado.Ok)
                Aviso(resultado.Erro);
        }

        void Aviso(string texto)
        {
            Limpar(rodape);
            var m = Estilo.Rotulo(rodape, texto, 22, Estilo.Erro);
            Estilo.ColunaElastica(m);
        }

        // ---------- aba Identidade ----------

        void AbaIdentidade()
        {
            var i = p.Identidade;
            var codinome = CampoRotulado("CODINOME", i.Codinome);
            var nome = CampoRotulado("NOME CIVIL", i.Nome);
            foreach (var campo in new[] { codinome, nome })
            {
                campo.onSubmit.AddListener(_ => SalvarIdentidade(nome.text, codinome.text, p.Identidade.Arquetipo, p.Identidade.Jogador));
                campo.onDeselect.AddListener(_ => SalvarIdentidade(nome.text, codinome.text, p.Identidade.Arquetipo, p.Identidade.Jogador));
            }

            var filaArquetipo = FilaRotulada("ARQUÉTIPO");
            Estilo.ListaSuspensa(filaArquetipo, 420,
                Arquetipos.Todos.Select(a => (a.Id, a.Nome)).ToList(), i.Arquetipo, TrocarArquetipo,
                editavel: p.Status == StatusCriacao.Novo);
            if (p.Status == StatusCriacao.Novo)
                Nota(filaArquetipo, "Trocar de arquétipo zera as escolhas de criação.");
            else
                Nota(filaArquetipo, "Depois de aprovada, trocar de arquétipo é um avanço.");

            var filaJogador = FilaRotulada("JOGADOR");
            var opcoes = new List<(string id, string nome)> { (string.Empty, "sem jogador") };
            foreach (var j in jogadores.Listar())
                if (j.Ativo || j.Id == i.Jogador)
                    opcoes.Add((j.Id, j.Ativo ? j.Nome : j.Nome + " (inativo)"));
            Estilo.ListaSuspensa(filaJogador, 420, opcoes, i.Jogador ?? string.Empty,
                id => SalvarIdentidade(p.Identidade.Nome, p.Identidade.Codinome, p.Identidade.Arquetipo, id));

            var vao = Estilo.Rect("Vao", conteudo);
            vao.gameObject.AddComponent<LayoutElement>().minHeight = 20;
            var futuro = Estilo.Rotulo(conteudo, "Aparência, habilidades e histórico entram numa próxima etapa.", 22,
                Estilo.TextoApagado, FontStyles.Italic);
            futuro.gameObject.AddComponent<LayoutElement>().minHeight = 30;
        }

        TMP_InputField CampoRotulado(string rotulo, string valor)
        {
            var fila = FilaRotulada(rotulo);
            var campo = Estilo.CampoTexto(fila, rotulo.ToLowerInvariant());
            campo.text = valor;
            Estilo.ColunaElastica(campo);
            return campo;
        }

        RectTransform FilaRotulada(string rotulo)
        {
            var fila = Estilo.Fila(conteudo, 58);
            var r = Estilo.Rotulo(fila, rotulo, 20, Estilo.TextoSuave, FontStyles.Bold);
            r.characterSpacing = 4;
            r.gameObject.AddComponent<LayoutElement>().preferredWidth = 240;
            return fila;
        }

        static void Nota(Transform fila, string texto)
        {
            var n = Estilo.Rotulo(fila, texto, 20, Estilo.TextoApagado, FontStyles.Italic);
            Estilo.ColunaElastica(n);
        }

        /// <summary>Grava sem redesenhar a aba (o Tab não pode perder o campo de destino); só o cabeçalho muda.</summary>
        void SalvarIdentidade(string nome, string codinome, string arquetipo, string jogador)
        {
            var i = p.Identidade;
            if (nome.Trim() == i.Nome && codinome.Trim() == i.Codinome && arquetipo == i.Arquetipo &&
                (jogador ?? string.Empty) == (i.Jogador ?? string.Empty))
                return;

            var resultado = cadastro.AlterarIdentidade(p.Id, nome, codinome, arquetipo, jogador);
            Cabecalho();
            if (!resultado.Ok)
            {
                MontarConteudo();
                Aviso(resultado.Erro);
            }
        }

        void TrocarArquetipo(string novo)
        {
            if (novo == p.Identidade.Arquetipo)
                return;

            void Aplicar()
            {
                var i = p.Identidade;
                cadastro.AlterarIdentidade(p.Id, i.Nome, i.Codinome, novo, i.Jogador);
                Atualizar();
            }

            if (!p.Criacao.TemEscolhas)
            {
                Aplicar();
                return;
            }

            Estilo.Confirmar(transform,
                $"Trocar para {Arquetipos.Nome(novo)} apaga as escolhas de criação de " +
                $"{Arquetipos.Nome(p.Identidade.Arquetipo)} (o +1 livre). A identidade fica. Continuar?",
                "Trocar",
                Aplicar,
                aoCancelar: MontarConteudo);
        }

        // ---------- aba Núcleo ----------

        void AbaNucleo()
        {
            Secao("RÓTULOS", p.Status == StatusCriacao.Novo
                ? "azul: valor do arquétipo  ·  dourado: com o +1 livre (máximo +3)"
                : "foto da criação");

            var atuais = cadastro.RotulosAtuais(p);
            foreach (var rotulo in Rotulos.Todos)
                LinhaRotulo(rotulo, atuais[rotulo]);

            var vao = Estilo.Rect("Vao", conteudo);
            vao.gameObject.AddComponent<LayoutElement>().minHeight = 6;

            Secao("CONDIÇÕES", "começam desmarcadas");
            var condicoes = Estilo.Fila(conteudo, 44, 10);
            foreach (var c in new[] { "Assustado", "Irritado", "Culpado", "Desesperançoso", "Inseguro" })
                Selo(condicoes, c, 230);

            Secao("POTENCIAL E HORA DA VERDADE", null);
            var outros = Estilo.Fila(conteudo, 44, 10);
            for (var k = 0; k < 5; k++)
                Selo(outros, string.Empty, 56);
            var vao2 = Estilo.Rect("Vao", outros);
            vao2.gameObject.AddComponent<LayoutElement>().preferredWidth = 40;
            Selo(outros, "Hora da Verdade: bloqueada", 400);
        }

        void Secao(string nome, string legenda)
        {
            var fila = Estilo.Fila(conteudo, 36);
            var t = Estilo.Rotulo(fila, nome, 22, Estilo.Dourado, FontStyles.Bold);
            t.characterSpacing = 5;
            t.gameObject.AddComponent<LayoutElement>().preferredWidth = 520;
            if (legenda != null)
            {
                var l = Estilo.Rotulo(fila, legenda, 20, Estilo.TextoSuave, FontStyles.Italic);
                Estilo.ColunaElastica(l);
            }
        }

        void LinhaRotulo(string rotulo, int valor)
        {
            var fila = Estilo.Fila(conteudo, 48, 8);
            var nome = Estilo.Rotulo(fila, Rotulos.Nome(rotulo).ToUpperInvariant(), 24, Estilo.Texto, FontStyles.Bold);
            nome.characterSpacing = 3;
            nome.gameObject.AddComponent<LayoutElement>().preferredWidth = 240;

            var comBonus = p.Status == StatusCriacao.Novo && p.Criacao.BonusLivre == rotulo;
            for (var v = Rotulos.Minimo; v <= Rotulos.Maximo; v++)
            {
                var celula = Estilo.Rect("Valor", fila);
                celula.gameObject.AddComponent<LayoutElement>().preferredWidth = 96;
                var marcado = v == valor;
                var fundo = !marcado ? Estilo.Campo : comBonus ? Estilo.Dourado : Estilo.Azul;
                Estilo.Moldura(celula.gameObject, fundo, marcado ? fundo : Estilo.BordaApagada);
                var t = Estilo.Rotulo(celula, v > 0 ? "+" + v : v.ToString(), 26,
                    marcado ? (comBonus ? Estilo.Cartao : Color.white) : Estilo.TextoApagado,
                    marcado ? FontStyles.Bold : FontStyles.Normal);
                t.alignment = TextAlignmentOptions.Center;
                Estilo.Esticar(t.rectTransform);
            }

            var vao = Estilo.Rect("Vao", fila);
            vao.gameObject.AddComponent<LayoutElement>().preferredWidth = 24;

            if (p.Status != StatusCriacao.Novo)
                return;

            // Um só +1 livre: escolhido, os outros apagam; clicar no dourado libera para escolher outro.
            var r = rotulo;
            var outroEscolhido = p.Criacao.BonusLivre != null && !comBonus;
            if (comBonus)
                Estilo.BotaoPrincipal(fila, "+1 livre", 220, () => EscolherBonus(null));
            else if (cadastro.RotuloDoArquetipo(p, rotulo) + 1 > Rotulos.Maximo)
                Selo(fila, "já no máximo", 220);
            else if (outroEscolhido)
                Estilo.BotaoContorno(fila, "+1 livre", 220, Estilo.TextoApagado, () => { }).interactable = false;
            else
                Estilo.BotaoContorno(fila, "+1 livre", 220, Estilo.Dourado, () => EscolherBonus(r));
        }

        void EscolherBonus(string rotulo)
        {
            var resultado = cadastro.EscolherBonusLivre(p.Id, rotulo);
            MontarConteudo();
            MontarRodape();
            if (!resultado.Ok)
                Aviso(resultado.Erro);
        }

        static void Selo(Transform pai, string texto, float largura)
        {
            var rect = Estilo.Rect("Selo", pai);
            rect.gameObject.AddComponent<LayoutElement>().preferredWidth = largura;
            Estilo.Moldura(rect.gameObject, Estilo.Campo, Estilo.BordaApagada);
            var t = Estilo.Rotulo(rect, texto, 22, Estilo.TextoApagado);
            t.alignment = TextAlignmentOptions.Center;
            Estilo.Esticar(t.rectTransform);
        }

        void EmConstrucao(string oQueVem)
        {
            var t = Estilo.Rotulo(conteudo, "Em construção", 30, Estilo.TextoSuave, FontStyles.Bold);
            t.gameObject.AddComponent<LayoutElement>().minHeight = 44;
            var d = Estilo.Rotulo(conteudo, oQueVem, 24, Estilo.TextoApagado, FontStyles.Italic);
            d.textWrappingMode = TextWrappingModes.Normal;
        }

        static void Limpar(Transform alvo)
        {
            for (var k = alvo.childCount - 1; k >= 0; k--)
            {
                var filho = alvo.GetChild(k);
                // A borda (filetes) é parte da moldura do próprio alvo, não do conteúdo.
                if (filho.name == "Borda")
                    continue;
                Destroy(filho.gameObject);
            }
        }
    }
}
