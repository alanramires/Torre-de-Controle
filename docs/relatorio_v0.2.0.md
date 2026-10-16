# v0.2.0 — A mesa

> 2026-10-16. O primeiro item do MVP que faltava: a **mesa de jogo**. A Torre deixa de ser só preparação
> (fichas, NPCs, cenário) e passa a ter onde o mestre aperta botão narrando. O Alan: "cara isso tá muito
> maneiro".

## O fio

A conversa que organizou o dia foi "a ideia nem é jogar: é criar os mecanismos para que, quando eu for jogar,
eu consiga apertar os botões". O método foi escrever **antes** um exemplo comentado de primeira luta, no
estilo dos exemplos do livro (`docs/exemplo_primeira_luta.md`), com um bloco **[TORRE]** em cada lance: o
texto mostrou quais botões a sessão pede. Daí saíram a Área do mestre e a mesa, nessa ordem.

## 1. Área do mestre

- **Primeira sessão** (livro cap. 8): o preparo que a Torre confere nos dados (fichas **em carreira**,
  vilão ligado a uma heroína, ficha do vilão com 3+ movimentos e condições, gente e lugares das fichas) e o
  roteiro de 15 itens que o mestre marca (abertura, os 8 movimentos básicos, Rótulos/Influência/condições,
  depois da luta). Regras em `Regras/primeira_sessao.json`; o "?" abre as cartas da Primeira sessão (feitas
  pela outra IA a partir de `docs/briefing_ajuda_primeira_sessao.md`).
- **Anotações** do mestre (equipe, NPCs, trama) num lugar só, com exportar/importar: começaram espalhadas
  pela sala e o Alan as juntou ("são anotações do mestre").
- **Vilões em cena** e **Potencial** (abertas pela mesa): pôr e tirar vilões da mesa; dar potencial **com o
  que aconteceu**, obrigatório, porque vai para o log.

## 2. A mesa

- **Entrar na sala**: entra direto na única campanha pronta (todas em carreira); senão lista as ativas,
  "0/5 prontas · entrar mesmo assim" (jogadora nova preenchendo ficha não trava a mesa).
- **Assentos**: heroínas em carreira em cima e embaixo (até 3 por aresta), retrato e nome abrem a ficha;
  Rótulos, condições clicáveis, potencial só leitura, tokens de bônus. **Log** do lado direito, com motivo e
  desfazer. **Vilões em cena** à esquerda, com as condições que têm e os movimentos de vilão no log.
- **Pilha de Equipe**: à vista no centro, mexida pelo **Gerir pilha** (ajuste ±1 com motivo, usar pilha com
  quantidade e quem, questionário, passagem do tempo).
- **Bônus** (adiante, constante, controle) com valor e contexto, como tokens no assento.
- Tudo é **evento** no log de `Core/Mesa` (com desfazer): a pilha, as condições do vilão e os bônus são da
  mesa; Rótulos, condições e potencial, da ficha.

## ⚠ Achados de regra (conferidos no livro)

- **A pilha sempre soma.** Ela só volta para 1 quando o tempo passa (p.88); o início da sessão soma +1 ao que
  sobrou (cap. 7); o questionário "adiciona" (p.86). O Alan achava que o questionário substituía; o exemplo
  certo do livro é o da Professora (p.187: 2 +1 +1 −1 = 3).
- **O questionário é "Entrar em batalha"**, e vale toda vez que a equipe entra junta contra um inimigo
  perigoso, não só na primeira luta. **"Companheiros de equipe" são os que estão na luta**: com 3 de 5, a líder
  precisa de Influência só sobre as outras 2. A Torre pergunta "quem está entrando na luta?" antes da líder.
- **A direção da Influência** (quem recebe tem o poder) é o que a conta do +1 da líder lê das fichas. O
  sistema de Influência arrumado nas versões anteriores pagou aqui.
- **Movimento de vilão é livre em quantidade**: o livro pede "três ou mais" e manda acrescentar depois. O que
  importa é ser ativo e dizer o que muda na ficção ("puxar pela fenda", não "criar uma fenda").

## 3. NPCs e trocas

- **"Ligado a"** (só vilões): o mestre liga o vilão que criou a heroínas, com o "como". Sem isso, um grupo sem
  vilão nas fichas nunca teria o "vilão ligado" pronto, embora o livro aceite "crie um vinculado".
- **Renomear o NPC da ficha muda a ficha**: antes a sincronização desfazia ("q raiva kkk"). O "Codinome
  (Nome)" virou só exibição (`NomeCompleto`).
- **Importar lista por id atualiza por cima**: o importar antigo criava cópias, e a Halcyon City chegou a 57
  NPCs (38 cópias inativas nos dados do Alan, que ficaram lá).
- Exportar/importar **Influência e relacionamentos** juntos; abas da equipe seguem visíveis em carreira e em
  revisão, só leitura.

## Erros meus que valem registrar

- **Caixa de texto que crescia com o texto**: o `TMP_InputField` também informa altura preferida, com a mesma
  prioridade do `LayoutElement`; no empate vence a maior. `AreaTexto` agora tem prioridade 2.
- **Rótulos vazando dos botões**: ajustei botão a botão e alguns escaparam; a mesa passou a ajustar todos ao
  se redesenhar (exceto os de texto longo, marcados).
- **Heredoc do Bash** comeu um `\n` dentro de string C# de novo (armadilha conhecida): sempre `.py` pelo
  Write.
- **Mesa com 4 arestas**: o Alan reservou a direita para o log e a esquerda ficou para os vilões; eu teria
  espremido tudo.

## O que não terminou

- **Rolador** (o próximo corte): modificador montado pela Torre, dados ou resultado digitado, lista do
  movimento, falha marca potencial, adiante usado sai, "agir de forma egoísta" no 6−.
- Influência mudando Rótulos pela mesa (com Rejeitar), o roteiro da primeira sessão marcando sozinho pelo log,
  o post BBCode do fórum, a colheita.
- Mudar Rótulos ao agir de forma egoísta é manual por enquanto.
- Nada da mesa foi visto por mim no Play mode além do que o Alan mostrou em prints.
