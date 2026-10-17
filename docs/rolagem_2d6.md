# Rolagem visual de 2d6

A cena `Assets/Scenes/Principal.unity` tem o objeto `Rolagem 2D6`, com
`SobreposicaoDados`. Ele instancia `Assets/Prefabs/Palco2D6.prefab`: dois dados,
mesa sem renderizador e paredes invisíveis. A câmera exclusiva envia os dados
para uma RenderTexture transparente exibida acima da interface existente.
O plano da física fica afastado do jogo e usa a layer `Dados`.

## Receber o resultado

Ligue o componente `SobreposicaoDados` a um campo do consumidor pelo Inspector:

```csharp
using System.Threading.Tasks;
using UnityEngine;

public class ExemploDeTeste : MonoBehaviour
{
    [SerializeField] private SobreposicaoDados dados;

    public async Task<int> TestarAsync(int modificador)
    {
        int soma = await dados.Rolar2D6Async();
        return soma + modificador;
    }
}
```

A soma é um `int` de 2 a 12 e só chega após os dois dados se acomodarem.
Chamadas durante um arremesso aguardam a mesma rolagem. Desativar o controlador
ou descarregar a cena cancela a tarefa pendente. O consumidor deve tratar
`OperationCanceledException` se sobreviver à cena. O componente `RolagemDados`
também oferece o evento `RolagemConcluida(int)`.

Os scripts do jogo que usam essa API podem referenciar a assembly
`TorreDeControle.Dados`; a assembly de UI já tem essa referência.

## Controles temporários

O botão no canto inferior direito lança os dois dados. O placar no canto
superior direito apresenta a soma. Desmarque `Mostrar Controles Temporarios`
em `SobreposicaoDados` para retirar ambos sem alterar a API ou a física.
Após o resultado, os dados e o placar ficam visíveis por 3 segundos e somem
com um fade de 0,5 segundo. Os tempos são ajustáveis no mesmo componente.
O botão permanece disponível; a próxima rolagem reapresenta os dados e o placar.
O desaparecimento visual não altera o valor já entregue ao jogo.
Não há rolagem automática ao entrar na cena principal. Números, pontinhos,
sons e desempate de dados inclinados continuam nos componentes do prefab.

## Conferência no Unity

Abra `Principal` e entre no Play. A interface e o vídeo devem continuar
visíveis e clicáveis. Clique em `Rolar 2d6`: aparecem dois dados sobre a tela,
ouvem-se o arremesso e as colisões, e o placar só mostra a soma depois que os
dois param. Repita a rolagem e redimensione a janela Game para conferir a
imagem transparente. `TesteDados` continua sendo a cena de experimentação.
