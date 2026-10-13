using TorreDeControle.Core.Jogadores;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Infra;
using UnityEngine;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Tela de entrada. Vai no Canvas da cena; ligue os botões no Inspector.
    /// Os dados ficam em <c>Application.persistentDataPath/dados</c> (o caminho sai no Console ao abrir).
    /// </summary>
    public sealed class TelaEntrada : MonoBehaviour
    {
        [SerializeField] Button botaoJogadores;
        [SerializeField] Button botaoPersonagens;

        CadastroJogadores jogadores;
        CadastroPersonagens personagens;
        PainelJogadores painelJogadores;
        PainelPersonagens painelPersonagens;

        void Awake()
        {
            var armazenamento = new ArmazenamentoLocalJson(Application.persistentDataPath + "/dados");
            Debug.Log($"[Torre] Dados em: {armazenamento.Pasta}");

            jogadores = CadastroJogadores.Carregar(armazenamento);
            personagens = CadastroPersonagens.Carregar(armazenamento);

            Ligar(botaoJogadores, nameof(botaoJogadores), AbrirJogadores);
            Ligar(botaoPersonagens, nameof(botaoPersonagens), AbrirPersonagens);
        }

        static void Ligar(Button botao, string campo, UnityEngine.Events.UnityAction acao)
        {
            if (botao != null)
                botao.onClick.AddListener(acao);
            else
                Debug.LogWarning($"[Torre] TelaEntrada sem o {campo} ligado no Inspector.");
        }

        void AbrirJogadores()
        {
            if (painelJogadores == null)
                painelJogadores = PainelJogadores.Criar(transform);
            painelJogadores.Abrir(jogadores);
        }

        void AbrirPersonagens()
        {
            if (painelPersonagens == null)
                painelPersonagens = PainelPersonagens.Criar(transform);
            painelPersonagens.Abrir(personagens, jogadores);
        }
    }
}
