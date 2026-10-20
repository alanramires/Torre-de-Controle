# v0.5.0 — A cena

> 2026-10-20. A Pacatópolis tem 9 heroínas, e a mesa quebrou. A saída não foi só de layout: "estar em cena" virou
> estado da mesa, e é ele que decide quem senta, quem rola, quem ajuda e quem entra na batalha.

## O fio

O Alan criou a campanha Pacatópolis para validar os exemplos do livro e pôs nela as 9 heroínas dos exemplos. O jogo
foi pensado para até 6. A mesa empilhou assentos uns sobre os outros, e as abas da sala vazaram da tela. Ele desenhou
a saída: a mesa começa vazia, a cena diz quem está nela, e quem não está fica numa caixa "Assistindo", podendo entrar
quando a narração trouxer (ou quando um movimento como o "Chegar na hora certa" da Bruta trouxer).

No caminho apareceu que isso é regra, não só tela: a ajuda da pilha exige "ser capaz de ajudar de verdade na ficção"
(p.84), e quem está em outra cena não é.

## O que mudou e por quê

### A cena (`Core/Mesa/Mesa.cs`, `PainelMesa.cs`)

- **A mesa começa vazia**, com "Criar cena": nome, local opcional (os Locais do Cenário) e quem está nela. Na
  primeira, todas vêm marcadas até caber; nas seguintes, quem já estava. **Cabem 6** (`Mesa.MaximoEmCena`, pergunta
  do Alan): 3 em cima, 3 embaixo, o lado esquerdo para vilões e NPCs.
- **Assistindo**: o canto de cima à esquerda, com o motivo de cada uma ("não entrou nesta cena", "fugiu"…) e o
  "Entrar", pela narração ou gastando um Controle do assento (o "Chegar na hora certa" já funciona por aqui, com o
  Controle dado à mão). "Sair" no assento.
- **Gestão da cena** (clicar no nome dela): nome e local, e pôr ou tirar heroínas, vilões e outros NPCs, com um
  motivo que vai para o log.
- **Tudo é evento** e se desfaz: cena nova (volta quem estava), entrar (devolve o Controle), sair. Vilões e NPCs
  entrando e saindo passaram a ir para o log também; antes era silencioso, "preparo do mestre".
- **NPCs aliados e neutros** sentam abaixo dos vilões, num cartão próprio, e podem ajudar com a pilha (p.84: um ponto
  por NPC). Antes, qualquer NPC em cena sentava como vilão. A coluna da esquerda rola.
- **A trava no Core**: com cena aberta, quem rola, o alvo, quem intervém, quem usa a pilha e a líder e as colegas do
  "entrar em batalha" precisam estar em cena. O "+1 se quem lidera tem Influência sobre cada colega" (p.86) passa a
  contar só as colegas da cena. Os pedidos (`/eda`, `/tgp`) ficam livres: são para depois.
- `/cena "nome"` abre a cena de verdade; `/reset` volta a mesa sem cena.

### A sala com 9 heroínas (`PainelSala.cs`)

As abas Formação, Relacionamentos e Influência empilhavam um botão por heroína sem rolagem. Na Formação os 9 couberam
por pouco ("não sei se foi sorte" — foi); na Influência, o Exportar/Importar embaixo empurrava a coluna, e a fila
inteira esticava, levando as caixas do meio e da direita para fora. Os nomes agora rolam, e o Exportar não quebra mais
em "EXPORTA/R".

### O deck do MJ: decidido, ainda não feito

O Alan quis o deck do MJ separado em leves e pesados. **O livro diz o contrário** (p.177): "Como MJ, você decide o
peso de qualquer movimento", e mostra a mesma cena leve e pesada. Decisão: o peso se escolhe ao jogar a carta, com a
tendência do livro sugerida. Entrou a carta tutorial **"Leve ou pesado?"** no help (texto próprio, exemplo nosso com o
Ocaso), sem falar do deck, que ainda não existe.

## Achados

- **"Estar em cena" é regra** (p.84), não layout. Por isso a trava está no Core, e não só na tela.
- **O exemplo da Toro (p.33–34) não é cena para validar**: é a ilustração de "falha não é fracasso", sem nomear o
  movimento (arremessar capangas nem aciona o Enfrentar, p.59). Serviu para dois dados: Toro tem Perigosa +2, e o MJ ali
  fez um movimento leve ("Cobrar um preço pela vitória"), sem condição. Não saiu barato: a falha dá potencial de
  qualquer jeito.
- **O MJ nunca rola em Masks** (p.34). Quando o Alan falou em "rolagem dos vilões e NPCs", perguntei antes de fazer:
  era a barra de rolagem da coluna da esquerda.

## Meus erros do dia

- **Fim de linha**: os arquivos C# estão em CRLF e meus patches comparavam com LF; o primeiro patch da tela não aplicou
  nada (sem estrago). Os scripts passaram a respeitar o fim de linha do arquivo.
- **Heredoc** estragou `\r\n` dentro de um script de novo; corrigi o script com a ferramenta de arquivo.
- **CS0136/CS0128**: o `DesenharRolagem` e o `Centro` são grandes, e nomes repetidos em escopos aninhados quebraram a
  compilação duas vezes (`gerir`, `opcao`).

## Verificação

- Core: 202 testes no harness (eram 197). Os novos cobrem cena, Assistindo, Controle gasto, desfazer, limite de 6,
  a trava de quem age, gestão da cena, aliado ajudando, NPC no log.
- `tools/compilar.sh` passa.
- **Visto no Play pelo Alan:** a mesa com a cena, o Assistindo, os vilões e aliados à esquerda (foi assim que apareceu
  a falta de rolagem), a faixa da cena. **Não visto ainda:** a rolagem da coluna da esquerda, o NPC no log, a gestão da
  cena por inteiro, as abas da sala rolando, a carta "Leve ou pesado?".

## O que não terminou

- **Deck do MJ** (decidido, na memória do projeto): botão "Movimento do MJ" ao lado do "Rolar movimento", peso na hora,
  "Infligir uma condição" marcando no mesmo evento, pendência "Movimento do MJ" quando uma rolagem falha, a conta de
  leves e pesados da sessão.
- **Saídas pela regra**: o "afastar-se" do golpe 10+ e precisar marcar condição sem ter livre ainda são só texto no log;
  tirar de cena é pelo botão.
- **A cena não guarda quem estava nela no fim** (só o log conta). Um histórico por heroína fica para depois.
- **Liberar seus poderes, Defender e o ganho de Controle do "Chegar na hora certa"** continuam na fila.
- **Fichas da Pacatópolis** para validar os exemplos: criadas pelo Alan, ainda sem os Rótulos do livro conferidos (a
  Toro tem Perigosa +2).
