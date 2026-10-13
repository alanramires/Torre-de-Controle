using TorreDeControle.Core.Jogadores;
using TorreDeControle.Infra;
using UnityEngine;
using UnityEngine.UI;

namespace TorreDeControle.UI
{
    /// <summary>
    /// Tela de entrada. Vai no Canvas da cena; ligue o botão "Jogadores" no Inspector.
    /// Os dados ficam em <c>Application.persistentDataPath/dados</c> (o caminho sai no Console ao abrir).
    /// </summary>
    public sealed class TelaEntrada : MonoBehaviour
    {
        [SerializeField] Button botaoJogadores;

        CadastroJogadores jogadores;
        PainelJogadores painelJogadores;

        void Awake()
        {
            var armazenamento = new ArmazenamentoLocalJson(Application.persistentDataPath + "/dados");
            Debug.Log($"[Torre] Dados em: {armazenamento.Pasta}");

            jogadores = CadastroJogadores.Carregar(armazenamento);

            if (botaoJogadores != null)
                botaoJogadores.onClick.AddListener(AbrirJogadores);
            else
                Debug.LogWarning("[Torre] TelaEntrada sem o botão Jogadores ligado no Inspector.");
        }

        void AbrirJogadores()
        {
            if (painelJogadores == null)
                painelJogadores = PainelJogadores.Criar(transform);
            painelJogadores.Abrir(jogadores);
        }
    }
}
