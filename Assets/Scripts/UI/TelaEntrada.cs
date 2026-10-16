using System.Linq;
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
        [Tooltip("Laboratório de NPCs (docs/npc_lab.md): originais, NPCs das campanhas e copiar entre elas.")]
        [SerializeField] Button botaoNpcs;
        /// <summary>"Entrar na sala": a mesa de jogo da campanha (direto, se só uma estiver pronta).</summary>
        [SerializeField] Button botaoMesa;
        [Header("Sons da interface")]
        [SerializeField] AudioClip somCursor;
        [SerializeField] AudioClip somConfirmar;
        [SerializeField] AudioClip somCancelar;
        [Header("Ajuda")]
        [SerializeField] Button botaoAjuda;
        [Tooltip("Opcional: Rótulos, sucessos e falhas, movimentos, recursos e bônus. Vazio carrega de Resources.")]
        [SerializeField] Texture2D[] cardsAjuda;
        [Tooltip("Retratos opcionais; sem eles, carrega automaticamente de Resources/Retratos. " +
                 "A Torre acha cada um pelo nome dado em arquetipos.json.")]
        [SerializeField] Texture2D[] retratos;

        CadastroJogadores jogadores;
        CadastroPersonagens personagens;
        PainelJogadores painelJogadores;
        PainelPersonagens painelPersonagens;
        CadastroSalas salas;
        PainelSalas painelSalas;
        PainelAjuda painelAjuda;
        PainelNpcs painelNpcs;
        PainelMesa painelMesa;

        ArmazenamentoAdiado armazenamento;

        void Update() => armazenamento?.Pulsar();

        // Nada fica sem gravar: ao perder o foco (ir digitar em outro lugar), sair do Play ou fechar.
        void OnApplicationFocus(bool comFoco)
        {
            if (!comFoco)
                armazenamento?.Descarregar();
        }

        void OnApplicationQuit() => armazenamento?.Descarregar();

        void OnDestroy() => armazenamento?.Descarregar();

        void Awake()
        {
            var sons = gameObject.AddComponent<SonsUI>();
            sons.Configurar(somCursor, somConfirmar, somCancelar);
            foreach (var botao in GetComponentsInChildren<Button>(true))
                SomBotao.Ligar(botao);
            var arquivos = new ArmazenamentoLocalJson(Application.persistentDataPath + "/dados");
            Debug.Log($"[Torre] Dados em: {arquivos.Pasta}");
            // Junta as gravações (cliques seguidos gravam uma vez); Update pulsa, sair descarrega.
            armazenamento = new ArmazenamentoAdiado(arquivos);
            Troca.Arquivos = new ArquivosDeTroca(arquivos.Pasta);

            jogadores = CadastroJogadores.Carregar(armazenamento);
            personagens = CadastroPersonagens.Carregar(armazenamento, RegrasArquetipos.Carregar(new FonteDeRegrasResources()));

            Ligar(botaoJogadores, nameof(botaoJogadores), AbrirJogadores);
            salas = CadastroSalas.Carregar(armazenamento, personagens);
            Retratos.Registrar(retratos);

            Ligar(botaoPersonagens, nameof(botaoPersonagens), AbrirPersonagens);
            Ligar(botaoSalas, nameof(botaoSalas), AbrirSalas);
            Ligar(botaoNpcs, nameof(botaoNpcs), AbrirNpcs);
            Ligar(botaoMesa, nameof(botaoMesa), EntrarNaSala);
            Ligar(botaoAjuda, nameof(botaoAjuda), AbrirAjuda);
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

        void AbrirNpcs()
        {
            if (painelNpcs == null)
                painelNpcs = PainelNpcs.Criar(transform);
            painelNpcs.Abrir(personagens, salas);
        }

        /// <summary>
        /// Uma campanha pronta (todas em carreira) e nenhuma outra: entra direto. Senão, a lista das ativas
        /// (a última jogada em cima); as não prontas aparecem apagadas, com o motivo, e entram mesmo assim.
        /// </summary>
        void EntrarNaSala()
        {
            var opcoes = salas.ParaAMesa();
            if (opcoes.Count == 1 && opcoes[0].pronta)
            {
                AbrirMesa(opcoes[0].sala);
                return;
            }
            if (opcoes.Count == 0)
            {
                Estilo.Confirmar(transform, "Nenhuma campanha ativa. Crie uma em Salas.", "Ok", () => { });
                return;
            }
            Estilo.Escolher(transform, "Entrar na sala", opcoes.Select(o =>
            {
                var sala = o.sala;
                var texto = o.pronta ? sala.Nome : $"{sala.Nome}  <size=75%>({o.motivo})</size>\n<size=65%>entrar mesmo assim</size>";
                return (texto, (UnityEngine.Events.UnityAction)(() => AbrirMesa(sala)), !o.pronta);
            }).ToArray());
        }

        void AbrirMesa(TorreDeControle.Core.Salas.Sala sala)
        {
            if (painelMesa == null)
                painelMesa = PainelMesa.Criar(transform);
            painelMesa.Abrir(sala, salas, personagens, armazenamento, null);
        }

        void AbrirAjuda()
        {
            if (painelAjuda == null)
                painelAjuda = PainelAjuda.Criar(transform);
            painelAjuda.Abrir(cardsAjuda);
        }
    }
}
