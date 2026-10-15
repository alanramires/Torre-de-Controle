# v0.0.6 — Vilões

> 2026-10-15. O dia em que os NPCs ganharam casa própria (o laboratório de NPCs) e os vilões ganharam
> regra, exemplos e help. O Alan resumiu: "eu finalmente não vou ficar perdido narrando".

## O fio

A dor que abriu o projeto era o mestre sem controle e sem saber quando usar os movimentos do MJ. Os NPCs que
nasceram da ficha na v0.0.5 precisavam de um lugar fora das campanhas, para guardar vilões bons e reusá-los;
e os vilões precisavam de uma ficha que o mestre entenda na hora da luta. O livro foi conferido parte a parte
(pp.184, 186–198, 214) e virou ficha na Torre, help para a mesa e quatro NPCs de exemplo nos dados do Alan.

## 1. Laboratório de NPCs (núcleo e tela)

- **Originais e cópias, como as heroínas.** NPC sem sala é original do laboratório de NPCs; "Copiar…" leva
  ao laboratório ou a qualquer campanha, como cópia independente que lembra de onde veio. A cópia leva o que
  o NPC **é** (ficha, notas, foto) e deixa o que **aconteceu** com ele (Influências, vínculo com a heroína).
- **Por quê assim:** o NPC é da campanha. A mesma Estrela Negra em duas salas dá duas Eclipses que seguem
  caminhos diferentes; o laboratório é a biblioteca do mestre e só recebe NPC de propósito, nunca por
  efeito colateral da ficha de um jogador (que em "novo" ainda muda o nome do nêmesis três vezes).
- **Troca em JSON** com formato próprio (lista): laboratório, campanha ou um NPC; importar como novos ou por
  cima de um, guardando o que é da campanha daqui. Cada tela de importar lista só o tipo que entende.
- NPCs **não** entram na trava de "em carreira": as perguntas de histórico já são obrigatórias.

## 2. Vilões: o que o livro diz, e a ficha

- **Movimento de vilão** é movimento do mestre, do vilão em cena; piora a situação; três ou mais; pode ser
  usado pela metade. **Movimentos de condição** são cinco baralhos comuns a todos os vilões (Assustado foge,
  Irritado exagera, Culpado humaniza, Desesperançoso encerra ou explode, Inseguro é social). Comparação do
  Alan que ficou: cada vilão tem o seu deck, todos dividem os cinco de condição.
- **Papel "Vilão / Oponente"** (o id gravado continua `vilao`): a babá que apanhou da equipe usa ficha de vilão
  sem ser vilão. Papel e maturidade se trocam na própria ficha do NPC; a ficha de vilão fica guardada.
- **Geração é sabor** (época e tom do nome, p.194), sem efeito mecânico: saiu dos avisos e ganhou um selo
  explicando.
- **Exemplos nos dados do Alan** (não no repositório): a Professora e a Rosa Rook (do livro, completadas por
  nós, com as Notas dizendo o que é de quem), o Sentinel (aliado com ficha de vilão guardada) e o Eclipse
  (nêmesis da Estrela Negra, com movimentos que miram ela, a equipe e a cidade).

## 3. Help (outra IA)

Cartas de movimentos de arquétipo (Bruta, Delinquente, Discípula, Farol, Forasteira) e doze de Vilões, a
partir do `docs/briefing_ajuda_viloes.md` escrito aqui. O "?" da ficha deixou de ter "bruta" fixo: aparece
quando o catálogo tem cartas do arquétipo, filtra pelo prefixo do id e aceita o id do movimento.

## 4. Fotos

Qualquer link http(s) vale (o Alan: "com tanta ferramenta gerando imagens temos que ser mais flexíveis"); o
aprovar/recusar continua sendo a garantia. A Unity só desenha jpg/png: o resto mostra o aviso e abre no
navegador. O NPC ganhou foto (sem aprovação: quem põe é o mestre), à esquerda dos dados da Identidade.

## Achados e erros

- **Eu errei a conta das condições.** Disse que um vilão com 3 condições "cai no terceiro golpe". O livro diz
  que é derrotado quando **precisa** marcar e não tem livre: com 3, aguenta três e cai no quarto. A outra IA
  tinha acertado nas cartas; o briefing foi corrigido.
- **Página errada:** citei a cena da Rosa Rook na p.221; é a p.214.
- **Regra da casa trocada pela outra IA:** em `arquetipos.json`, a nota da Forasteira ("Entre dois mundos")
  dizia "regra da casa: contate de novo só depois de pagar a exigência pendente"; agora diz o que o livro diz
  (a exigência vem na falha; não bloqueia novo contato). **O Alan precisa confirmar** se a regra da casa sai.
- **`deck_of_villainy.pdf` não serve:** foi "impresso" de um site de download e perdeu o texto (108 páginas
  só de molduras e arte). Nem o original entraria aqui (material pago, repo público); a regra do livro basta.
- O Alan lembrava da cena do giz com uma agente da A.E.G.I.S.; é a Rosa Rook, CEO das Indústrias Rook.

## O que não terminou

- **Influência com NPCs** (adulto por padrão calculado, grade NPC × PJ, oculta, de nascença): decidida em
  `docs/npc_lab.md`, sem código.
- **Vilão na mesa:** marcar condição na luta, aviso de derrotado, lembrete dos movimentos de condição, "fazer
  movimento de vilão" no log. Depende da Área do mestre.
- **Sons dos botões:** código no repositório, `Assets/audio` fora (origem dos `.wav` não confirmada; o Alan
  deixou para depois).
- O "?" por movimento (abrir a carta do chip) tem o parâmetro pronto, sem ligação nos chips.
- Nada de hoje visto por mim no Play mode; 126 testes no harness.
