# v0.3.0 — O primeiro movimento

> 2026-10-18. A Torre rola dados de verdade: o Enfrentar diretamente a ameaça virou um painel completo, do dado
> ao aceite, com a regra conferida no livro com o Alan. É o molde para os outros movimentos.

## O fio

O rolador estava segurado de propósito até a primeira luta ser validada à mão. Hoje ele saiu, mas não como
"2d6 + número": como uma **entidade**. Cada rolagem tem resultado binário (sucesso 7+ / falha 6−), tipo
(sucesso total 10+, drawback 7–9, falha), as opções escolhidas com subopção, a troca de golpes, as
intervenções e a hora do aceite. As regras que dizem "em um sucesso" leem o resultado, e as do 7–9 leem o
tipo. Nada se recalcula depois.

Escolhemos um movimento só, o Enfrentar, e fomos até o fim nele. O Alan foi lendo as ocorrências no livro
(47 do Enfrentar, 39 de "oportunidade"), eu conferi no texto, e cada dúvida virou decisão escrita em
`movimentos_basicos.json`.

## O que mudou e por quê

- **Dados físicos sem spoiler.** O lançador 3D é da outra IA. `Rolar2D6Async` passou a devolver os dois dados
  e a soma só quando os dois param. Antes, devolvia só a soma, e os valores vazavam no meio da rolagem.
  O botão temporário "Rolar 2d6" dela foi escondido (ainda dá para religar no Inspector).
- **Log editável e console do mestre.** Entradas se editam e se escondem (nada se exclui). Cliques de
  marcar e desmarcar seguidos somem sozinhos. O console aceita `/roll`, `/potencial`, `/pot` (debug: força
  o valor), `/pilha`, `/cond`, `/nota`, `/npc_create`, `/location_create`, `/rodada`, `/clear` e `/help`.
- **A rolagem pendente.** O dado cai e o resultado ainda não vale até o **Aceitar**. Antes disso, alguém
  pode intervir. As intervenções deslizam sem o teto de −3 a +4, que vale só no modificador da rolagem.
- **O painel "Rolar movimento"** (desenho do Alan), também aberto pelo "M" do assento, com a heroína já
  escolhida:
  1. rolamento simples: quem rola, contra quem, movimento, "2d6 + Rótulo";
  2. intervenção: pilha de Equipe, renúncia de Influência, Defender (rola), outro;
  3. efeito esperado, lido do JSON, com as opções como caixinhas e os cartões da troca de golpes.

  O Aceitar aplica na mesa as condições, o +1 Equipe e o potencial da falha, no mesmo evento da rolagem.
  Desfazer a rolagem desfaz tudo junto.

## Regras decididas hoje (todas em `movimentos_basicos.json`, com página)

- **A troca de golpes contra NPC** (p.57): o alvo marca 1 condição, escolhida pelo MJ, em todo sucesso.
  Quem rolou recebe o retorno que o MJ escolher: uma condição **ou** tomar um golpe poderoso, **nunca os
  dois**. Contra PJ, os dois tomam golpe poderoso.
- **Só "resistir ou evitar os golpes" protege quem rolou.** A condição do alvo continua. As outras opções
  são ganhos. O exemplo do Rex (11, impressionar + tirar) toma o golpe poderoso mesmo assim.
- **O impressionar, contra NPC, troca a condição; não soma outra** (Rex × Professora, p.189–190: "eu marcaria
  Irritada, mas... marco Insegura"). Contra PJ, o MJ pode dar uma condição.
- **Condição + golpe poderoso juntos só vêm de fora da troca:** movimento do MJ numa falha (Canção dos Céus,
  p.189), uma condição já marcada ou o resultado do próprio golpe poderoso.
- **Oportunidade:** o MJ diz se é chance a um aliado ou +1 Equipe. **Contra PJ, não há +1 Equipe**
  (decisão do Alan: brigar com colega não enche a pilha).
- **O Enfrentar pede "contra quem".** Contra o nada é liberar os poderes (p.57, p.60). Capanga não aciona o
  movimento.
- **Pilha de Equipe** (p.84): cada pessoa gasta 1 ponto por jogada, e vários podem ajudar. É um botão só:
  gasto por outra pessoa é **ajuda**; gasto por quem rola é **egoísta**.
- **PvP** (p.190–191): o exemplo Huma × Vértice confere com o que a Torre faz (resistir livra quem
  escolheu). Na falha, o MJ pode virar o movimento contra quem falhou.

## Achados que contradizem o que se acreditava

- **Influência dá +1 passivo** em todo movimento contra quem se tem Influência (p.81). Eu tinha dito que não.
  O Alan leu e corrigiu, e a decisão entrou no `nucleo.json`.
- **Eu errei a leitura da p.189 duas vezes.** Primeiro, disse que o exemplo não tinha relação com o
  impressionar: eu tinha olhado o trecho do Defender da Canção dos Céus, e não o do Rex, logo depois.
  Antes disso, eu tinha dito que o impressionar podia **somar** uma segunda condição. O exemplo do livro
  **troca** a condição. Na dúvida sobre uma página, ler a cena inteira antes de concluir.
- **A tradução chama o 7–9 de "sucesso parcial"** (p.12), justamente o nome que o Alan considera impreciso.
  A Torre usa "sucesso com drawback". Fica registrado, caso alguém procure pelo nome do livro.
- **"Herói genérico" não era um arquétipo.** Entendi o pedido como uma classe nova de personagem e criei um
  arquétipo; o Alan queria uma opção de "quem rola" com todos os Rótulos +0. O arquétipo foi desfeito no
  mesmo dia, antes de qualquer commit.

## Verificação

- Core: 184 testes no harness (eram 174 na v0.2.2). Os novos cobrem troca, aceite, intervenção, PvP, pilha
  e as travas.
- `tools/compilar.sh` passa.
- **Nada disso foi visto no Unity por mim.** O comportamento é provado no Play mode, pelo Alan. Ele já usou
  o painel: os ajustes de layout (resultado embaixo, o Rótulo ao lado do botão) e o log ambíguo ("retorno:
  ileso" parecia falar do alvo) vieram dos prints dele. As rodadas de teste, inclusive PvP, ficaram para
  ele.

## O que não terminou

- **Tomar um golpe poderoso:** é o mesmo painel (contra quem vazio, faixas invertidas, soma as condições
  marcadas). Hoje só fica no log: "toma golpe poderoso (role à parte)". A rolagem nova deve guardar
  `origem` apontando para o Enfrentar.
- **Movimentos do MJ na falha:** um cartão "pode combinar" (condição, golpe poderoso, movimento do vilão).
- **Os outros movimentos básicos:** o painel lê todos, mas só o Enfrentar foi conferido. O modelo de texto
  do efeito pode sair estranho nos outros.
- **O modificador ainda é só Rótulo:** condições (−2), adiante, constante e Influência passiva não entram
  na conta.
- **Sair de cena:** se quem rolou já tem as 5 condições marcadas, a lista do retorno fica vazia. Não há
  tratamento.
- **A altura do painel** (960 px) com 10+, duas escolhas e os dois cartões ainda não foi vista no Unity.
- **`revisao_luta.md`**: arquivo vazio, sem dono conhecido, deixado fora do repositório.
