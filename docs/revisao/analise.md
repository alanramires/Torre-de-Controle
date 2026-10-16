# Avaliação geral da Torre de Controle

Data: 16/10/2026.

## Escopo da avaliação

A Torre tem uma base boa e uma identidade clara, mas o principal desafio agora é fechar o ciclo de uma jogada com segurança e pouco trabalho manual. Ela já reúne cadastro, preparação, consulta e controle; falta consolidar essas partes numa experiência contínua de mestrar.

Esta avaliação foi feita pela leitura da documentação e de partes centrais da mesa, persistência, importação, cartas e interface. Não incluiu executar a Unity, rodar os testes ou medir desempenho. Os riscos identificados por inspeção não significam que já houve perda de dados ou falha em uma campanha. Nenhum código foi gerado ou modificado para esta avaliação.

## O que está funcionando bem

A separação entre Core, interface e armazenamento é uma boa decisão. As regras principais podem ser verificadas sem depender da montagem das telas, e o armazenamento local fica isolado.

Também são escolhas fortes:

- Laboratório separado da campanha: permite preparar personagens sem alterar diretamente uma partida.
- Identidades por ID: nomes e apresentação podem mudar preservando os vínculos.
- Histórico com correções: mantém o motivo das alterações e ajuda a reconstruir o que aconteceu.
- Cartas desenhadas a partir de texto: reduz muito o custo de corrigir regras.
- Exemplo de luta antes da implementação: aproxima os botões das necessidades reais da mesa.
- Testes do Core já presentes: existe uma base de verificação que vale preservar e ampliar onde houver risco.

A proposta de representar Masks como estado e transições funciona bem para Rótulos, condições, recursos e Influência. A interpretação da ficção continua precisando da decisão do mestre, e a interface deve facilitar essa decisão.

## Gargalos principais

| Prioridade | Gargalo | Consequência |
|---|---|---|
| Alta | Consistência entre ficha, mesa e histórico | Uma operação pode ficar parcialmente gravada |
| Alta | Desfazer eventos antigos sem analisar mudanças posteriores | Uma correção pode afetar o estado atual de forma inesperada |
| Alta | Resolver uma jogada exige várias ações separadas | O mestre precisa lembrar e conferir cada consequência |
| Média | Regras, cartas e ajuda podem divergir | O texto pode ensinar algo diferente do comportamento |
| Média | Layout depende de muitas medidas e exceções locais | Novos campos e resoluções de tela provocam novos ajustes |
| Média | Documentação mistura decisões atuais e antigas | Retomar o projeto exige interpretar contradições |

## 1. Persistência: proteger a operação completa

O armazenamento escreve em temporário e substitui o arquivo anterior, o que é uma boa proteção contra gravação incompleta. Entretanto, uma ação pode alterar `personagens.json` e `mesa_<sala>.json` separadamente. Uma interrupção entre essas gravações pode deixar a ficha atualizada e o histórico desatualizado, ou o contrário.

Há também um ponto concreto: a gravação adiada limpa a fila antes de concluir as escritas. Se uma delas lançar uma exceção, as pendências restantes deixam de estar na fila para nova tentativa. Isso não comprova perda já ocorrida, mas é uma fragilidade visível no código.

Referências:

- [ArmazenamentoLocalJson.cs](../../Assets/Scripts/Infra/ArmazenamentoLocalJson.cs): gravação individual em temporário e substituição do arquivo.
- [ArmazenamentoAdiado.cs](../../Assets/Scripts/Infra/ArmazenamentoAdiado.cs): `Descarregar`, especialmente a limpeza da fila antes das gravações.
- [Mesa.cs](../../Assets/Scripts/Core/Mesa/Mesa.cs): aplicação das mudanças e gravação do histórico.
- [CadastroPersonagens.Mesa.cs](../../Assets/Scripts/Core/Personagens/CadastroPersonagens.Mesa.cs): aplicação e gravação dos valores da ficha.

Sugestão: tratar como requisito de confiabilidade que uma operação termine inteira ou possa ser recuperada. Backup restaurável da campanha também deveria fazer parte dessa etapa.

## 2. Desfazer: distinguir cancelamento de compensação

Hoje, corrigir um evento aplica o inverso da diferença sobre o valor atual. Isso funciona bem em casos simples, mas fica ambíguo quando houve alterações posteriores.

Exemplo: um evento aumentou um Rótulo; outras jogadas depois o reduziram até −2. Corrigir aquele aumento antigo pode reduzi-lo novamente. O limitador usado nessa correção não aplica os limites dos Rótulos. Também há situações em que desfazer uma marcação antiga de condição pode interferir numa marcação mais recente.

Referência: [Mesa.cs](../../Assets/Scripts/Core/Mesa/Mesa.cs), métodos `Corrigir`, `Refazer` e `Limitar`.

Proposta:

- Cancelamento simples enquanto a jogada ainda está em preparação.
- Desfazer direto quando nenhuma alteração posterior interfere.
- Correção explícita, com prévia do resultado, quando houver dependências.

O histórico preservado é uma força do sistema; essa distinção o tornaria mais confiável.

## 3. Produto: resolver uma jogada inteira

Pelo estado descrito e pelo código consultado, várias peças já existem, mas ainda exigem coordenação manual: gastar Equipe, mudar Rótulos, marcar condição, conceder potencial, registrar o movimento do vilão e resolver lembretes.

A proposta é usar o lance como unidade principal de trabalho:

> Personagem e intenção → movimento → cálculo → ajuda e escolhas → consequências → confirmação → histórico.

Um lance poderia reunir várias alterações relacionadas. Assim, a falha da Aurora teria seu resultado, condição, golpe poderoso e movimento do Ocaso ligados entre si, mesmo que parte da resolução aconteça em etapas.

Isso também prepara melhor a exportação para o fórum: o post passa a mostrar acontecimentos compreensíveis, em vez de apenas uma sequência de alterações de campos.

Referências:

- [Exemplo da primeira luta](../exemplo_primeira_luta.md).
- [Mesa.cs](../../Assets/Scripts/Core/Mesa/Mesa.cs).
- [Evento.cs](../../Assets/Scripts/Core/Mesa/Evento.cs).
- [Resumo do projeto](../resumo.md), pendências do rolador e da exportação.

## 4. Estado de jogo: representar consequências além de ligar e desligar valores

`AlternarCondicao` marca ou desmarca uma condição. Essa operação é útil para edição, mas não representa sozinha a instrução de jogo de precisar marcar outra condição quando todas estão marcadas. Esse caso precisa produzir uma consequência própria, como sair de cena, em vez de depender apenas dos botões disponíveis.

Referência: [Mesa.cs](../../Assets/Scripts/Core/Mesa/Mesa.cs), método `AlternarCondicao`.

O mesmo raciocínio vale para distinguir:

- Condição marcada de personagem fora de cena.
- Vilão com todas as condições marcadas de vilão derrotado.
- Lembrete existente de consequência efetivamente resolvida.
- Gasto genérico de Equipe de ajuda vinculada a uma rolagem.

Essas diferenças são importantes para o sistema acompanhar o jogo sem exigir que o mestre mantenha tudo na memória.

## 5. Biblioteca e regras: conferir o conjunto

Separar texto explicativo de regra executável é correto. O problema é manter os vínculos consistentes conforme o conteúdo cresce.

Sugestão: uma verificação única que aponte:

- IDs duplicados e referências inexistentes.
- Cartas sem ajuda correspondente.
- Movimentos da ficha sem carta encontrada.
- Categorias sem navegação.
- Regras da casa sem identificação clara.
- Conteúdo editorial apresentado como se fosse automação implementada.

Há uma fragilidade específica no baralho: movimentos de outro arquétipo podem ser encontrados pelo final do ID, escolhendo a primeira correspondência. Isso pode ficar ambíguo se dois movimentos compartilharem o mesmo identificador curto. A recomendação é preservar a referência completa desde a aquisição.

Referências:

- [MontadorDeBaralho.cs](../../Assets/Scripts/Core/Cartas/MontadorDeBaralho.cs), busca de movimentos por sufixo.
- [CatalogoDeCartas.cs](../../Assets/Scripts/Core/Cartas/CatalogoDeCartas.cs).
- [PainelAjuda.cs](../../Assets/Scripts/UI/PainelAjuda.cs), categorias declaradas no código.
- [Catálogo de ajuda](../../Assets/Resources/Ajuda/catalogo.json).
- [Catálogo de cartas de jogo](../../Assets/Resources/Regras/cartas.json).

## 6. Interface: previsibilidade antes de otimização ampla

Os problemas recentes de sobreposição mostram que as telas precisam de regras comuns de composição: limites do modal, regiões roláveis, espaço de mensagens, tamanhos mínimos e comportamento de textos longos.

A recomendação é evitar resolver cada vazamento isoladamente. Uma pequena matriz de conferência visual teria mais valor que testar apenas a tela confortável:

- Janela menor.
- Tela padrão.
- Texto longo.
- Muitos personagens.
- Muitos lembretes.

Não há evidência suficiente nesta avaliação para afirmar que CPU ou memória sejam hoje o principal problema. Há reconstruções de interface e serializações que merecem medição futura, mas não se recomenda uma otimização ampla sem medir uma cena real.

Referências:

- [PainelEditorDeCarta.cs](../../Assets/Scripts/UI/PainelEditorDeCarta.cs).
- [PainelMesa.cs](../../Assets/Scripts/UI/PainelMesa.cs).
- [Estilo.cs](../../Assets/Scripts/UI/Estilo.cs).
- [Resumo do projeto](../resumo.md), tabela de armadilhas de layout e desempenho.

## Proposta de evolução

Organizar as próximas entregas nesta ordem:

1. Confiabilidade: gravações recuperáveis, correções seguras e validação dos dados.
2. Uma jogada completa: rolador, ajuda, escolhas, consequências e confirmação ligados pelo mesmo lance.
3. Uma rodada completa: pendências visíveis, histórico agrupado e exportação BBCode.
4. Uso confortável: menos trocas de painel, ajuda contextual e layout consistente.
5. Expansão: novos recursos de preparação, publicação e acesso online depois de validar o fluxo local.

Critério de conclusão proposto:

> O mestre consegue conduzir uma rodada da partida simulada, fechar a Torre, reabrir e produzir o post do fórum sem reconstruir acontecimentos de memória?

## Documentação e retomada do trabalho

Fazer uma revisão curta da documentação: o resumo ainda afirma que faltam campos de versão, enquanto os arquivos de mesa e personagens já os possuem. A existência desses campos ainda não significa que as versões sejam efetivamente migradas ou recusadas ao carregar.

Essa diferença entre intenção, implementação e pendência precisa ficar mais clara.

Referências:

- [Resumo](../resumo.md), seção de schema da ficha.
- [Mesa.cs](../../Assets/Scripts/Core/Mesa/Mesa.cs), campo de versão e carregamento.
- [CadastroPersonagens.cs](../../Assets/Scripts/Core/Personagens/CadastroPersonagens.cs), versão do arquivo persistido.
- [CadastroPersonagens.Troca.cs](../../Assets/Scripts/Core/Personagens/CadastroPersonagens.Troca.cs), versão e validação do formato de troca.

## Recomendação principal

Concentrar o próximo esforço em resolver e registrar uma jogada inteira. É onde as partes já construídas começam a devolver tempo ao mestre e onde os problemas de regra, interface e persistência podem ser avaliados juntos.


## Segunda leitura após as alterações — 16/10/2026

Esta seção atualiza os achados anteriores. A avaliação inicial fica preservada como histórico; onde houver divergência, prevalece esta segunda leitura. Foram relidos armazenamento, abertura da mesa, correção/refazimento, montagem de baralho, testes relacionados e resumo. Não foram executados Unity, compilação ou testes nesta rodada, nem alterado código.

### Melhorias confirmadas no código

1. **Fila de gravação corrigida.** `ArmazenamentoAdiado.Descarregar` só remove uma pendência depois da gravação bem-sucedida. Se ocorrer erro, a chave com falha e as seguintes permanecem pendentes. Também preserva a versão nova quando uma chave é reagendada durante o processamento. O achado anterior sobre esvaziamento prematuro da fila está resolvido por inspeção.
2. **Backup implementado.** `ICopiaDeSeguranca` desacopla a capacidade de backup do armazenamento. A implementação local copia os JSONs e mantém até 15 diretórios. A mesa pede uma cópia na primeira abertura de cada sala durante a execução, e o armazenamento adiado descarrega suas pendências antes da cópia.
3. **Limites dos Rótulos nas correções.** `Mesa.Limitar` agora limita campos `rotulo:` ao intervalo permitido. Existe o teste `Desfazer_rotulo_antigo_respeita_o_limite` em `MesaTests.cs`. O achado anterior de possível Rótulo abaixo de −2 ou acima de +3 por esse caminho está corrigido; o teste foi lido, não executado.

### Achados que ainda merecem ação

**Alta — Consistência entre arquivos continua pendente.** Backup reduz o impacto de uma falha, mas não torna atômicas as alterações em ficha e histórico. O descarregamento continua gravando cada chave separadamente. Uma interrupção pode deixar versões diferentes desses arquivos. Recomenda-se recuperação de uma operação completa, além das cópias de segurança.

**Alta — Desfazer/refazer ainda não considera dependências.** `Corrigir` continua aplicando o delta inverso ao estado atual. A limitação de Rótulos protege o intervalo, mas pode tornar a compensação parcial. `Refazer` usa o delta original, não necessariamente o que a correção efetivamente retirou. Exemplo: se desfazer um aumento antigo não reduz um Rótulo porque ele já está em −2, refazer ainda pode aumentá-lo para −1. Isso não é uma restauração exata do estado anterior ao desfazer. Condições também continuam sujeitas a interferência entre marcações antigas e recentes. Proposta: detectar conflitos e mostrar a alteração efetiva antes de confirmar; refazer deve ter semântica explícita para compensações parciais.

**Média — Falha de backup impede nova tentativa automática naquela execução.** Em `PainelMesa.Abrir`, `copiadas.Add(sala.Id)` acontece antes da cópia. Se ela falhar, a sala já permanece marcada como copiada; reabrir a mesma sala não tenta novamente. A falha é comunicada apenas por `Debug.LogWarning`. Recomenda-se marcar o sucesso depois de concluída a cópia e oferecer ao mestre um aviso visível com possibilidade de tentar novamente.

**Média — Identidade e conclusão do backup.** O diretório usa data com precisão de segundos e rótulo. Duas cópias com o mesmo rótulo no mesmo segundo usam o mesmo diretório, e `File.Copy(..., true)` permite sobrescrever arquivos. Uma cópia interrompida também pode deixar uma pasta incompleta sem indicação de conclusão. Recomenda-se identidade única por cópia e identificação das cópias concluídas antes de disponibilizá-las para restauração.

**Média — Cobertura de teste da gravação real.** O arquivo `GravacaoAdiadaTests.cs` consultado usa uma implementação auxiliar (`ArmazenamentoQueJunta`), não a implementação de produção `ArmazenamentoAdiado`. Esse teste verifica a integração do cadastro com gravação adiada, mas não prova a recuperação após falha recém-corrigida. Na busca nos testes não foi localizado teste direto de `ArmazenamentoAdiado` ou do novo backup. Valem testes dirigidos de falha no meio do lote, nova tentativa, reagendamento e restauração de backup. Ausência encontrada por busca estática, sem execução da suíte.

### Pontos anteriores que permanecem

- Resolver uma jogada completa ainda é a prioridade de produto: intenção, rolagem, ajuda, escolhas, consequências e histórico vinculados.
- `AlternarCondicao` continua sendo marcação/desmarcação; não representa por si só a consequência de precisar marcar quando todas estão marcadas.
- `MontadorDeBaralho` ainda possui busca por sufixo, potencialmente ambígua entre arquétipos.
- As categorias da ajuda continuam declaradas no código.
- A documentação `resumo.md` ainda afirma que faltam campos de versão, embora eles existam no código. Versionamento efetivamente validado/migrado e existência do campo são assuntos distintos.
- O editor já tem formulário rolável e modal ajustável, mas esta leitura não valida visualmente o layout em diferentes tamanhos de tela.

### Prioridade revista

1. Completar a confiabilidade: coerência entre arquivos, comportamento em falha de backup e testes da implementação real.
2. Definir e proteger a semântica de desfazer/refazer com alterações posteriores e compensações parciais.
3. Fechar uma jogada completa e depois uma rodada com saída BBCode.
4. Consolidar validação dos catálogos, documentação atual e conferência visual.

A direção das mudanças é adequada: houve correção concreta de dois riscos e inclusão de uma proteção importante. A avaliação passa de ausência dessas proteções para necessidade de completar e verificar seu comportamento em falhas.

Referências: [fila de gravação](../../Assets/Scripts/Infra/ArmazenamentoAdiado.cs), [backup local](../../Assets/Scripts/Infra/ArmazenamentoLocalJson.cs), [abertura da mesa](../../Assets/Scripts/UI/PainelMesa.cs), [correções](../../Assets/Scripts/Core/Mesa/Mesa.cs), [testes da mesa](../../Assets/Tests/EditMode/MesaTests.cs) e [testes de gravação adiada](../../Assets/Tests/EditMode/GravacaoAdiadaTests.cs).

