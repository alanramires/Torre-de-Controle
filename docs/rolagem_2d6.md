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
        Resultado2D6 resultado = await dados.Rolar2D6Async();
        return resultado.Soma + modificador;
    }
}
```

O retorno contém `Dado1`, `Dado2` e `Soma` (de 2 a 12), só depois que ambos se acomodam.
Chamadas durante um arremesso aguardam a mesma rolagem. Desativar o controlador
ou descarregar a cena cancela a tarefa pendente. O consumidor deve tratar
`OperationCanceledException` se sobreviver à cena. O componente `RolagemDados`
também oferece o evento `RolagemConcluida(Resultado2D6)`.

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

## Rolagem dirigida para debug

No console da mesa, `/fixo 5` prepara a próxima soma bruta.
O par é sorteado entre todas as combinações válidas:
`/fixo 6` pode mostrar 1+5, 2+4, 3+3, 4+2 ou 5+1, com chances iguais.
Pedir a mesma soma novamente não obriga repetir nem trocar o par anterior.
Ao executar `/roll` ou um movimento, os dados visuais reproduzem um arremesso que termina
nos valores escolhidos pela fila de debug. `/roll +2`, por exemplo, mostra
dados somando 5 e registra o total 7. `/fixo 5 12` prepara duas rolagens;
`/fixo` sem números limpa a fila e volta à física livre. Sem dados visuais
na cena, permanece o comportamento de debug por números.

A API explícita também pode ser usada em testes:

```csharp
Resultado2D6 resultado = await dados.Rolar2D6ForcadoAsync(5);
// Ou preserve um par específico:
resultado = await dados.Rolar2D6ForcadoAsync(2, 3);
```

Esse modo calcula uma trajetória numa cena de física 3D local, sem imagens
nem sons, copiando os colliders, massas, amortecimento e materiais da mesa.
Depois reproduz as posições, rotações e impactos. A simetria geométrica do
cubo permite escolher a orientação das faces desde o primeiro quadro, sem
trocar números ou virar o dado à força no pouso. A física global não é avançada
pela simulação de debug. Há limites de tentativas e de tempo simulado; se o
dado não se acomodar, a tarefa falha, em vez de devolver um valor divergente.

Uma chamada dirigida durante outro arremesso é recusada. Desativar o
controlador cancela a tarefa e descarrega a cena de simulação. A rolagem
normal seguinte volta a usar rigidbodies dinâmicos, sem resultado imposto.

Testes de PlayMode em `Assets/Tests/Dice` cobrem as 36 combinações de
orientação, entradas inválidas, somas 2/5/12, correspondência entre as faces
visíveis e o retorno, cancelamento e retorno à rolagem normal.

Validação em 2026-10-19: quatro testes de PlayMode passaram numa cópia
isolada com a mesma versão da Unity e o prefab do projeto (somas 2/5/12,
36 combinações de orientação, entradas inválidas, cancelamento e volta à
física livre). Core, Infra, Dados e UI também compilaram com todos os arquivos
atuais. A execução sem gráficos confere a física e as poses; a aparência e o
áudio devem ser conferidos no Play da cena Principal.
