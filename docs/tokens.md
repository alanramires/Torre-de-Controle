# Tokens da mesa

> Decidido com o Alan em 2026-10-15. Tokens servem para **anexar às cartas** (o que um movimento deu) e para
> **ficar na pilha em cima da mesa**. Arte em `Assets/img/tokens/` (feita pelo Alan). Ainda sem código.

## Os tokens e o que cada um é

| Token | Arte | Natureza | De quem |
|---|---|---|---|
| Equipe | `equipe.png` | contador da **pilha da mesa** | da equipe (centro da mesa) |
| Potencial | `potencial.png` | contador da ficha (a cada 5, um avanço) | da personagem |
| Chamas | `chamas.png` | **controle** de um movimento (Nova: "controle sobre as chamas") | da personagem, ligado ao movimento |
| Adiante | a fazer | bônus **genérico com escopo**, some depois de usado | da personagem |
| Constante | a fazer | bônus **genérico com escopo**, dura enquanto a condição durar | da personagem |
| Controle | a fazer | **reserva por movimento**, gasta um para um | da personagem, ligado ao movimento |

## Adiante e Constante: genéricos, não presos a Rótulo

O livro (Controle, +1 adiante e +1 constante) define os dois por "jogadas **aplicáveis**": adiante vale na
próxima, constante em todas, pelo tempo que o movimento disser. O que muda de um movimento para outro é
**a que se aplica** (o escopo). Escopos encontrados no livro:

| Escopo | Exemplo |
|---|---|
| qualquer próxima jogada | ajudar um colega pela 1ª vez na sessão |
| contra uma pessoa | o NPC tropeça no Provocar: +1 adiante contra ele |
| um movimento | Bruta Irritada: +1 constante para liberar seus poderes |
| uma ação ou objetivo | impressionar o amor ou frustrar o rival; a busca de um objetivo |
| um Rótulo (raro) | Discípula: +1 adiante ao usar o Rótulo que a mentora incorpora |

Um token só de cada, guardando: **valor** (+1 quase sempre), **escopo** (tipo + alvo: pessoa, movimento,
Rótulo ou texto livre da ação), **origem** (o movimento/carta que deu) e, no Constante, **até quando** (texto
do movimento: enquanto Irritada, até o fim da cena, enquanto seguir o conselho).

Na mesa: o mestre confirma se a rolagem é aplicável (ação transacional, como o resto). Teto do modificador
continua **+4** com tudo somado. Adiante usado sai da pilha da personagem.

## Controle: uma reserva por movimento

O livro: pontos gastos "de acordo com as especificações do movimento", **um para um**, e o movimento diz
quando expiram. Então não é genérico: cada movimento que dá controle tem a **sua** reserva, com o cardápio
de gastos vindo das regras (`arquetipos.json`) e a expiração do movimento (ex.: as chamas acabam no fim da cena).
Dois movimentos com controle = duas reservas.

A arte segue isso: **Chamas** é a "pele" do controle do Nova. Um token genérico de Controle serve para os
movimentos que não tiverem arte própria; quem tiver, usa a sua.

## Notas de arte (para quem gera)

- Os PNGs atuais têm ~1254 px e 1,4–1,7 MB cada; na mesa aparecem pequenos. Vale exportar em 256–512 px
  (ou limitar o Max Size no import da Unity) para não pesar o repositório (vão por LFS).
- Mesmo padrão visual: moeda com borda e miolo azul-marinho. Sugestão para diferenciar à primeira vista:
  Adiante com uma seta para a frente, Constante com um símbolo de duração (ampulheta ou laço), Controle com
  marcas de reserva (pips) — e o número da reserva desenhado pela Torre por cima, não na arte.


## Segundo lote: Constante, Controle e Adiante

Artes em `Assets/img/tokens/constante.png`, `controle.png` e `adiante.png`.
Geradas pela ferramenta integrada com equipe.png como referência visual.
Transparência conferida. Sem aplicação às cartas nesta etapa.

Constante: infinito prateado. Controle: engrenagem dourada com aro verde.
Adiante: seta dourada com aro ciano.

Constante vale enquanto durar a situação indicada pelo movimento; adiante,
na próxima rolagem aplicável; controle é gasto nos efeitos previstos na regra.

Prompts usados:

- Create ONE new circular RPG UI resource token matching the attached reference token family exactly: front-facing perfectly circular midnight navy enamel disk, polished metallic rim and bold metallic center symbol, subtle bevels. Reference is style only; replace its people symbol and blue rim as described. Transparent background outside disk, no perspective, no text, no letters, no numbers, no cast shadow outside disk. Strong simple emblem readable at 48px, centered with 6% transparent margin, same scale and finish as reference. CONSTANTE / ongoing bonus: cool silver metallic rim and a single large smooth horizontal infinity-loop emblem in pale polished silver. The emblem means sustained effect, not a clock.
- Create ONE new circular RPG UI resource token matching the attached reference token family exactly: front-facing perfectly circular midnight navy enamel disk, polished metallic rim and bold metallic center symbol, subtle bevels. Reference is style only; replace its people symbol and blue rim as described. Transparent background outside disk, no perspective, no text, no letters, no numbers, no cast shadow outside disk. Strong simple emblem readable at 48px, centered with 6% transparent margin, same scale and finish as reference. CONTROLE / spendable hold resource: olive-lime green metallic rim and a single large eight-toothed gear emblem in warm pale gold with a dark round central hole. Clean simple teeth and bold silhouette.
- Create ONE new circular RPG UI resource token matching the attached reference token family exactly: front-facing perfectly circular midnight navy enamel disk, polished metallic rim and bold metallic center symbol, subtle bevels. Reference is style only; replace its people symbol and blue rim as described. Transparent background outside disk, no perspective, no text, no letters, no numbers, no cast shadow outside disk. Strong simple emblem readable at 48px, centered with 6% transparent margin, same scale and finish as reference. ADIANTE / next applicable action bonus: cyan turquoise metallic rim and a single thick right-pointing arrow emblem in warm pale gold. Straight horizontal shaft and large arrowhead, clean and unmistakable.
### Troca pedida pelo Alan: Constante com ampulheta (2026-10-15)

O infinito sugere "para sempre", e o Constante do Masks dura **enquanto a situação do movimento durar**
(Irritada, o fim da cena, seguir o conselho): na mesa, o ∞ leva a esquecer de tirar o bônus. Refazer o
Constante com **ampulheta**, mantendo o aro prateado e o resto do padrão. Prompt sugerido (mesma família):

- Create ONE new circular RPG UI resource token matching the attached reference token family exactly: front-facing perfectly circular midnight navy enamel disk, polished metallic rim and bold metallic center symbol, subtle bevels. Reference is style only; replace its people symbol and blue rim as described. Transparent background outside disk, no perspective, no text, no letters, no numbers, no cast shadow outside disk. Strong simple emblem readable at 48px, centered with 6% transparent margin, same scale and finish as reference. CONSTANTE / bonus that lasts while a situation lasts: cool silver metallic rim and a single large upright hourglass emblem in pale polished silver, simple bold frame, sand visible in both bulbs. The emblem means "lasts for a while, then ends", not forever.

Guardar aqui os prompts de cada arte nova, para regenerar no mesmo estilo.
