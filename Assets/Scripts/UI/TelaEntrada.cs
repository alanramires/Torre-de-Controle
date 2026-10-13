using TorreDeControle.Core.Jogadores;
using TorreDeControle.Core.Personagens;
using TorreDeControle.Core.Regras;
using TorreDeControle.Core.Salas;
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
        [SerializeField] Button botaoSalas;
        [Tooltip("Retratos opcionais; sem eles, carrega automaticamente de Resources/Retratos. " +
                 "A Torre acha cada um pelo nome dado em arquetipos.json.")]
        [SerializeField] Texture2D[] retratos;

        CadastroJogadores jogadores;
        CadastroPersonagens personagens;
        PainelJogadores painelJogadores;
        PainelPersonagens painelPersonagens;
        CadastroSalas salas;
        PainelSalas painelSalas;

        void Awake()
        {
            var armazenamento = new ArmazenamentoLocalJson(Application.persistentDataPath + "/dados");
            Debug.Log($"[Torre] Dados em: {armazenamento.Pasta}");

            jogadores = CadastroJogadores.Carregar(armazenamento);
            personagens = CadastroPersonagens.Carregar(armazenamento, RegrasArquetipos.Carregar(new FonteDeRegrasResources()));

            Ligar(botaoJogadores, nameof(botaoJogadores), AbrirJogadores);
            salas = CadastroSalas.Carregar(armazenamento, personagens);
            Retratos.Registrar(retratos);

            Ligar(botaoPersonagens, nameof(botaoPersonagens), AbrirPersonagens);
            Ligar(botaoSalas, nameof(botaoSalas), AbrirSalas);
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
            painelPersonagens.Abrir(personagens, jogadores, salas);
        }

        void AbrirSalas()
        {
            if (painelSalas == null)
                painelSalas = PainelSalas.Criar(transform);
            painelSalas.Abrir(salas, personagens, jogadores);
        }
    }
}
