# Tokens: regras de uso

> Debatido com o Alan em 2026-10-15. **Decidido, ainda sem código.** A arte e os prompts ficam em
> `docs/tokens.md` (da outra IA); aqui fica como cada token funciona na Torre.

## Para que servem

Tokens servem para **anexar às cartas** (o que um movimento deu) e para **ficar na mesa**, de modo que o
mestre (e quem olha a ficha de outro) entenda o estado **batendo o olho**.

## Auto-explicativos: Equipe, Potencial, Chamas

- **Equipe:** contador da **pilha da mesa** (centro). O número é desenhado pela Torre.
- **Potencial:** contador da ficha (a cada 5, um avanço).
- **Chamas:** é o **controle do Nova** ("controle sobre as chamas"), com arte própria.

Esses não precisam de nota: o lugar onde estão e o número já dizem tudo.

## Precisam de nota: Adiante, Constante, Controle

Livro (Controle, +1 adiante e +1 constante): adiante vale na **próxima** jogada **aplicável**; constante em
**todas** as aplicáveis, pelo tempo que o movimento disser; controle são pontos gastos **um para um** nas
opções do movimento, que diz quando expiram. "+1" sem dizer para quê é o bônus que some ou é usado na
rolagem errada, então cada um carrega uma nota.

### Adiante e Constante: genéricos, com escopo (não presos a Rótulo)

Um tipo de token para cada. O que muda entre movimentos é o **escopo** (a que se aplica). Escopos do livro:

| Escopo | Exemplo |
|---|---|
| qualquer próxima jogada | ajudar um colega pela 1ª vez na sessão |
| contra uma pessoa | o NPC tropeça no Provocar: +1 adiante contra ele |
| um movimento | Bruta Irritada: +1 constante para liberar seus poderes |
| uma ação ou objetivo | impressionar o amor ou frustrar o rival |
| um Rótulo (raro) | Discípula: +1 adiante ao usar o Rótulo que a mentora incorpora |

### Controle: uma reserva por movimento

Não é genérico: cada movimento que dá controle tem a **sua** reserva, com o cardápio de gastos vindo das
regras (`arquetipos.json`) e a expiração do movimento. Dois movimentos com controle, duas reservas. As
Chamas são a "pele" do controle do Nova; um token genérico de Controle serve para os outros.

### A nota de cada um

| Token | A nota diz | Exemplo |
|---|---|---|
| Adiante | para quê (escopo) e de onde veio | "+1 contra a Rosa Rook · Provocar 8 (ela tropeçou)" |
| Constante | para quê, de onde veio e até quando | "+1 para liberar seus poderes · enquanto Irritada (Casca grossa)" |
| Controle | de qual movimento, quantos e no que se gasta | "Chamas · 3 · gastar nas faíscas · até o fim da cena" |

- **Na tela:** etiqueta curta embaixo da moeda ("vs. Rosa Rook", "enq. Irritada", "3"); a nota inteira ao
  passar o mouse. O número do controle é desenhado pela Torre por cima; **nada de texto na arte**.
- **Quem escreve:** quando um movimento dá o bônus, a Torre preenche pela regra e o mestre confirma; quando é
  narrativo ("a Rosa baixou a guarda"), o mestre escreve na hora.
- **Quando sai:** Adiante some ao ser usado numa rolagem que se encaixa no escopo (mestre confirma); Constante
  sai quando o mestre tira ou a condição acaba (ex.: Irritada removida); Controle desce um a um ao gastar.
- **Teto:** o modificador final continua entre −3 e +4, com tudo somado.
- **Rolador:** na hora de rolar, a Torre lista os Adiante e Constante cuja nota se encaixa naquela rolagem e
  pergunta quais valem (ação transacional, como o resto da mesa).

## Influência: moeda por personagem, só entre PJs

A Influência é diferente dos outros tokens: não é contador ("tenho 3"), é **binária e por par** ("eu e
você"), e pode ser **mútua** (eu ouço você e você me ouve).

- **Cada personagem tem a sua moeda**, com o próprio retrato. "Dar Influência a alguém" é **entregar a sua
  moeda**: na ficha de cada um aparecem as moedas dos colegas de quem ela **tem** Influência.
- **Cópias:** numa mesa física cada um precisaria de uma moeda por colega (5 jogadores, 4 moedas cada; a
  Forasteira feliz entrega as 4). Na Torre **ninguém conta moedas**: elas são **desenhadas a partir do estado**
  da ficha (que já é binário por par). Sem estoque, sem limite, sem moeda dada duas vezes.
- **Mútua:** cada um segura a moeda do outro; a moeda ganha um aro de "mão dupla".
- **Perdida:** moeda rachada e retrato em cinza (nada some, como na ficha).
- **Ganhar de novo** não empilha: a segunda Influência vira mudança de Rótulos (animação, não moeda a mais).
- **Retrato:** foto heroica aprovada; sem ela, o retrato da classe (os retratos **da Torre**, em
  `Resources/Retratos`; nunca a arte do livro).
- **Leitura (do token antigo do Alan):** as moedas que você **segura** mostram em quem você pode "dar
  piteco": dizer quem ela é (ela aceita ou rejeita) e ganhar +1 nos movimentos contra ela. A Torre escreve
  "Influência +1" por cima da moeda, como o "−2" das condições; a arte segue sem texto.
- **Só PJ ↔ PJ.** Com centenas de NPCs, moeda vira poluição: a Influência com NPCs fica **no papel**, como já
  está (chips na ficha do NPC, linhas com "(NPC)" na ficha da heroína, contagem dos adultos por padrão, aba
  Influência da sala). Se um dia precisar, uma marquinha "NPCs: 3" ao lado das moedas que abre a lista.
- **Visão do mestre:** um mapa de setas entre os retratos da equipe (seta dupla = mútua), ao lado da grade
  NPC × PJ já decidida em `docs/npc_lab.md`.

Pedido de arte feito à outra IA (via Alan): `influencia.png` (aro verde, **miolo transparente** para o
retrato), `influencia_mutua.png` e a camada `influencia_perdida.png`.

## Condições: uma moeda por condição

Ideia do Alan (ele já tinha feito tokens assim na mesa física): a moeda da condição mostra **onde dói**.
Conferido no livro (condições, p.~90):

| Condição (id) | −2 em | Rótulo desses movimentos |
|---|---|---|
| Assustado (`assustado`) | Enfrentar diretamente a ameaça | Perigoso |
| Culpado (`culpado`) | Provocar; Avaliar a situação | Superior |
| Desesperançoso (`desesperancoso`) | Liberar seus poderes | Aberração |
| Inseguro (`inseguro`) | Defender; Rejeitar Influência | Salvador (Rejeitar não usa Rótulo) |
| Irritado (`irritado`) | Confortar ou apoiar; Perfurar a máscara | Mundano |

- **Quase "uma por Rótulo", mas a regra é por movimento:** cada condição pega os movimentos de um Rótulo só
  (exceção: Inseguro leva junto Rejeitar Influência). Culpado **não** dá −2 em qualquer rolagem com
  Superior, só em Provocar e Avaliar; movimento de arquétipo com o mesmo Rótulo não leva o −2.
- **Nome:** é **Desesperançoso** (o livro e os dados da Torre), não "Desespero".
- **Binária por personagem:** marcada ou não. A moeda aparece na ficha quando marcada.
- **O −2 entra sozinho no rolador:** a regra já está em `nucleo.json` (condições). A moeda na ficha mostra o
  porquê.
- **Vilões também:** quando o vilão marca uma condição na luta, a mesma moeda aparece na ficha dele e lembra
  o **baralho daquela condição** (o movimento de condição que ele faz ao marcar).
- **Precisou marcar com todas marcadas:** a PJ sai de cena; o vilão é derrotado.

### Pedido de arte (para a outra IA, via Alan)

Cinco moedas na família dos tokens (aro metálico, miolo azul-marinho, bisel, 512 px, fundo transparente,
**sem texto nem números**: o nome e o "−2" a Torre escreve por cima). Em cada uma, o **emblema da condição**
grande no centro e, menores na borda de baixo, os **ícones dos movimentos afetados** (como no token antigo do
Alan). Um aro de cor própria por condição ajuda a distinguir de longe. Sugestões de emblema:

- `cond-assustado.png`: olhos arregalados / escudo trincado; ícone de Enfrentar (punho).
- `cond-culpado.png`: correntes / cabeça baixa; ícones de Provocar (boca/fala) e Avaliar (lupa).
- `cond-desesperancoso.png`: nuvem de chuva / chama apagando; ícone de Liberar (explosão de poder).
- `cond-inseguro.png`: silhueta tremida / reflexo rachado; ícones de Defender (escudo) e Rejeitar (mão espalmada).
- `cond-irritado.png`: punho cerrado / vapor; ícones de Confortar (mão no ombro) e Perfurar a máscara (máscara).

Os ícones dos movimentos valem também para as cartas e para o rolador: se forem gerados à parte (um por
movimento básico), dá para reusar.

### Símbolo de cada movimento básico (escolha do Alan)

Referência: os ícones que o Alan usava na mesa física (`Acervo/Cards/condicoes`). **Só referência**: ao
menos parte é do Icons8 (licença, fora do repositório); a outra IA gera os nossos, na família das moedas.

| Movimento | Símbolo | Condição que dá −2 |
|---|---|---|
| Enfrentar diretamente a ameaça | punho | Assustado |
| Liberar seus poderes | ponto no centro com setas para todos os lados (poder irradiando) | Desesperançoso |
| Defender | escudo | Inseguro |
| Avaliar a situação | lupa | Culpado |
| Provocar | homem xingando | Culpado |
| Confortar ou apoiar | aperto de mãos | Irritado |
| Perfurar a máscara | máscara (o "Batman") | Irritado |
| Rejeitar Influência | pessoa com sinal de negativo | Inseguro |
| Tomar um golpe poderoso | balão de onomatopeia "POW!" (estilo Batman anos 60) | — (nenhuma dá −2) |

Golpe poderoso: o "POW!" é a **única exceção** à regra de "sem texto na arte" (é desenho de quadrinho, não
legenda). Para não confundir com o Liberar: Liberar é um **ponto com setas saindo para todo lado** (poder
irradiando de dentro para fora); o golpe é o **balão** de onomatopeia, estrela pontuda chapada, bordas grossas, o POW dentro: um é poder saindo, o outro
é golpe chegando (físico ou emocional).

Os mesmos ícones servem nas moedas de condição (borda de baixo), nas cartas dos movimentos e no rolador.
Sugestão: gerar como um conjunto à parte (`Assets/img/icones-movimentos/`), um por movimento.
