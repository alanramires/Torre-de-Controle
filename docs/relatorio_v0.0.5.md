# v0.0.5 — Personagens criando NPCs automaticamente

> 2026-10-14. O terceiro requisito para "em carreira" (relacionamentos) fechou, e o quarto (os NPCs que
> orbitam cada personagem) começou pelo lado da ficha: o que a jogadora escreve vira NPC da sala.

## O fio

Masks pede que cada ficha nomeie gente: o nêmesis da Amaldiçoada, a família do Legado, a mentora da
Discípula, quem a Nova feriu, quem sabe da identidade do Janus. Até hoje isso era texto solto, e o mestre
recriava cada pessoa à mão na aba NPCs. Agora **o nome escrito na ficha cria o NPC na sala**, ligado à dona
("Estrela Negra · nêmesis"), e o mestre só completa a parte dele (motivação, ficha de vilão). O Alan foi
arquétipo por arquétipo dizendo o que gera NPC e com que papel; a regra ficou no `arquetipos.json`, não no
código, para o próximo arquétipo ser só configuração.

## 1. Relacionamentos (terceiro requisito)

Mesma ideia da Influência, porque é só entre membros da equipe: o jogador escolhe na ficha (aba História),
a sala mostra só leitura com o nome do colega em dourado no lugar do "___" e a leitura reversa em rosa.
Em carreira, trocar ou perder guarda o histórico (`Relacao.Anteriores`). Repetir colega pode, mas vira
observação. A **Bruta** sincroniza nos dois sentidos com a Influência (amor e rival viram Influência; a
Influência confirmada filtra as opções). Junto: os porquês viraram caixas de várias linhas (texto colado
com quebras vazava da caixa de uma linha) e o atalho do laboratório para uma instância **leva à sala**.

## 2. NPCs que nascem da ficha

Três formas, todas no `arquetipos.json`:

- **`npcs` num extra:** nêmesis (vilão), mentora (vilã, para ter ficha de vilão, "embora não seja vilã"),
  e os quatro membros do Legado (aliados; o próximo da linhagem nasce jovem; o maior inimigo, vilão).
  Cada um ganhou **nome e descrição separados**: o texto inteiro na caixa de nome virava nome do NPC.
- **`npcs_historico`:** perguntas de histórico com caixa de nome; a resposta vira a descrição (Nova,
  Janus, Bruta, Farol, Delinquente, Transformado, Forasteira).
- **`por_escolha`:** cada obrigação marcada do Janus e três impulsos do Farol ganham "pessoa ligada
  (opcional)". Vazio, o impulso vale para qualquer um ("vazio: qualquer herói adulto"); os outros 17
  impulsos são de jogo e não criam NPC. Papel, maturidade e dica por opção.

Regras do ciclo: nome preenchido cria ou reativa e renomeia; nome vazio, opção desmarcada ou fonte que
sumiu **inativa** (nada se exclui). No laboratório não há sala: o NPC nasce quando a ficha é copiada.
Maturidade "mestre": nasce adulto com aviso até o mestre decidir.

**Por quê "relaxado":** em aprovação a ficha trava a criação, mas o NPC nasce justamente na sala. O Alan
preferiu deixar nome e descrição de NPC editáveis em aprovação a obrigar a revisão ("a ideia é ajudar o
mestre e não atrapalhar ele"). O resto do extra (ex.: os Rótulos da mentora) continua da criação.

Papéis de NPC passaram a **Neutro / Aliado / Vilão**; `Heroi` ficou só para as PJs (o herói adulto que
enfrenta os jovens entra como Vilão: o papel diz o que ele é *nesta* história).

## 3. Help (a outra IA)

O catálogo de ajuda foi de 25 para 45 cartas: Influência ganhou NPCs e ganhar/perder; entraram Condições,
Equipe e sessão, e Movimentos de equipe (uma carta por arquétipo), com bordas por tema. A revisão pedida
aqui pegou um erro de regra: "sai da cena e volta na próxima com as condições" não está no livro
(Tomar um golpe poderoso 10+ é "afastar-se da situação: fugir, desmaiar"); a outra IA corrigiu também em
`movimentos_basicos.json`.

## Achados e erros

- **Eu dupliquei o primeiro nêmesis.** O Eclipse nasceu com o vínculo sem campo; quando o Legado exigiu
  vários NPCs por extra, a busca passou a exigir campo, não o achou e criou outro. Correção: ao carregar,
  o antigo é adotado e o repetido fica inativo. Lição: mudar a chave de um vínculo já gravado pede adoção
  no mesmo passo.
- **O Unity só recompilou depois:** o Alan inativou o repetido à mão antes da correção entrar. Testar no
  Play mode logo depois de mudar formato evita confusão.
- **Afirmei sem conferir** que os impulsos do Farol não tinham descrição; tinham (`descrever_opcional`).
  Conferir o JSON antes de descrever comportamento.
- **Desmarcar uma opção não sincronizava** os NPCs: o teste do Janus pegou.
- O Alan lembrava de Provocar 10+ dando Influência; no livro é o **7–9** contra NPC ("exagera").

## O que não terminou

- **Influência com NPCs:** decidida, não programada (`docs/npc_lab.md`: adulto por padrão calculado,
  só o mestre muda pela mesa, Influência oculta, grade NPC × PJ, Influência de nascença por regra).
- **Laboratório e tela de NPCs** (originais, instâncias, reboot entre campanhas): decididos, não programados.
- O quarto requisito ainda **não entra** em `PendenciasParaCarreira`.
- NPCs de fichas que já estavam na sala nascem com o texto inteiro no nome: o Alan separa à mão.
- Nada do dia foi visto no Play mode por mim; compila e os testes passam (119).
