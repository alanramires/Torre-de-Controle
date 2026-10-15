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

Constante: ampulheta prateada (substitui o infinito). Controle: engrenagem dourada com aro verde.
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


### Prompt aplicado: Constante com ampulheta

Arte substituída em `Assets/img/tokens/constante.png` pela ferramenta integrada;
aro prateado, miolo azul-marinho e transparência preservados.

Edit the attached Constante circular RPG token. Replace ONLY the central silver infinity symbol with one large upright silver HOURGLASS emblem. Bold simple polished silver frame, symmetric upper and lower bulbs with visible silver sand and a narrow waist, unmistakable silhouette readable at 48px. Symbol represents a sustained but finite duration. Preserve the exact midnight navy enamel disk, silver metallic circular rim, front orthographic view, lighting, finish, centered composition, proportions, and transparent background outside the disk. No text, letters, numbers or extra symbols. Match the existing family. The infinity symbol must be completely gone.


## Influência PJ ↔ PJ — aros e sobreposição (2026-10-15)

Arquivos RGBA de 512 × 512 em `Assets/img/tokens/`:

- `influencia.png`: aro verde metálico; centro e exterior transparentes.
- `influencia_mutua.png`: mesmo aro com elos opostos no topo e na base.
- `influencia_perdida.png`: apenas fissura diagonal, sem moeda, retrato ou fundo.

Gerados com a ferramenta integrada; exportação final em 512 px. Aros centralizados
com extensão de 450 px (31 px de margem por lado, aproximadamente 6%). Transparência
residual quase invisível (alfa < 3/255) removida na exportação; miolo conferido.
A fissura usa a mesma área circular. Quantidade e estado vêm da Torre, não da arte.

Composição prevista: retrato circular embaixo → aro → fissura se perdida.
Retrato: foto heroica aprovada; sem ela, retrato da classe em Retratos.
A Torre aplica tons de cinza ao retrato perdido. Uma moeda por par de PJs, sem
arte individual para cada personagem. NPCs permanecem em texto/lista.
Estes assets não implementam a lógica ou a composição da ficha nesta etapa.

### Prompts aplicados

influencia.png:

Create a single portrait coin FRAME asset, not a complete coin. Match the polished metallic beveled rim style of the reference token, but replace lime with muted emerald green matching #68bc88 Influence cards. Output square PNG, intended final 512x512. Perfect circular annulus centered at (50%,50%), outer radius 44% of canvas, inner radius 37%, exactly 6% transparent margin. Only the green metal RING is visible. The entire large inner circular opening is FULLY TRANSPARENT alpha zero: NO navy disk, NO portrait, NO symbol, NO glass, NO fill, NO checkerboard painted into image. Exterior also truly transparent. Front orthographic view, subtle polished bevel, consistent upper-left lighting, no perspective or cast shadow. No text, letters or numbers. This is an overlay frame for a portrait placed behind it by Unity.

influencia_mutua.png:

Edit attached hollow green portrait frame to make the MUTUAL INFLUENCE variant. Keep exactly the same circle, outer and inner diameters, polished emerald green metal, subtle bevel, lighting and center position. Add two very small paired interlocking link accents, one at the top and one at the bottom, integrated entirely into the width of the ring, denoting a reciprocal two-way bond. Keep the entire central circular opening fully transparent, no portrait, disk, glass or symbols inside. Outside transparent too. No text or numbers. Front orthographic view. Single isolated circular frame, same silhouette as original. Intended output 512x512.

influencia_perdida.png:

Create ONLY a transparent OVERLAY layer representing a lost bond: a single thin jagged crack with two short branches, running diagonally from upper left rim to lower right rim across an invisible coin. The attached ring shows the alignment/scale only: do NOT draw the ring, coin, portrait, disk, background or any fragments. Show just the fracture itself, a dark charcoal fissure with a slim pale silver bevel edge, clear enough over either dark or light portraits, predominantly transparent everywhere else. Fit within a centered circular area of diameter 88% of square canvas, with endpoints around 10 o'clock and 4 o'clock. Delicate broken-glass incision, not a lightning bolt, no glow or dramatic effects, no text or numbers. Overlay must leave most of the portrait visible. Front orthographic UI icon, intended final 512x512 RGBA.


## Símbolos dos movimentos — lote separado (2026-10-15)

Pedido: apenas os nove símbolos em `Assets/img/icones-movimentos/`, sem moedas,
sem aplicar às cartas ou ao rolador nesta etapa. PNGs individuais transparentes,
512 × 512. Reutilizar a mesma arte em cartas, condições e rolador.
Geração pela ferramenta integrada de imagens; não copiar ícones do acervo.

### Prompt comum

Create ONE standalone UI SYMBOL for a superhero tabletop RPG, part of a coherent nine-icon family. Transparent background, square canvas intended final 512x512. ONLY the symbol: NO coin, NO disk, NO circular frame, NO card, NO background panel or scenery. Pale warm gold polished metal emblem with restrained bevel and crisp dark navy edge, bold simple solid shapes and generous negative space, front orthographic view, centered in a square with 8% breathing room. Designed to remain recognizable at 32-48px. No fine ornament, no realistic skin or faces, no text, letters or numbers unless explicitly required below. Original generic artwork, not a copied commercial icon. 

### Complementos por arquivo

- `enfrentar.png`: A single clenched FIST, knuckles forward and thumb wrapped over fingers, compact clear silhouette. Represents confronting a threat.

- `liberar-poderes.png`: A solid central DOT with eight thick straight arrows radiating OUTWARD in all directions, evenly spaced like compass rays. Leave clear gaps between dot and arrow shafts. Represents power flowing out from within. NOT a jagged comic explosion, NOT an impact bubble, NO lettering.

- `defender.png`: A single classic heraldic SHIELD, broad flat upper edge, tapering pointed bottom, smooth strong outline and simple gently raised center. NO crest, emblem or decorations inside.

- `avaliar.png`: A single MAGNIFYING GLASS with a thick circular rim and short diagonal handle to lower right. The inside of the lens is fully transparent, no glass fill or reflection. Clear compact silhouette.

- `provocar.png`: A stylized MALE HEAD IN SIDE PROFILE facing right, mouth open as if angrily shouting, with two short angular speech rays leaving the mouth. A simple bold pictogram silhouette, not a detailed portrait, no words or profanity marks.

- `confortar.png`: A single HANDSHAKE: two hands clasped firmly from opposite sides, one unified compact emblem. Simple cuffs and bold fingers, no extra arms or hands.

- `perfurar-mascara.png`: A standalone generic SUPERHERO COWL MASK with short pointed temples, two clear transparent eye holes, angular brow and lower face cutout. Original design suggesting a secret heroic identity, not a bat logo or a licensed character portrait. Face-on, no head behind it.

- `rejeitar-influencia.png`: A simple PERSON BUST silhouette with a small circular NEGATIVE SIGN at lower right, visibly overlapping shoulder: a bold MINUS horizontal bar in the small circle. No diagonal prohibition slash, no text. Make both the person and the negative sign unmistakable at small size.

- `golpe-poderoso.png`: A retro 1960s comic IMPACT BURST emblem: bold flat jagged pointed star-shaped speech explosion, thick dark navy outline, warm gold fill, with exactly the text 'POW!' inside in chunky navy comic lettering. This text is REQUIRED and is the sole exception to no text. NOT outward arrows or a radiating central dot. No other text. Simple flat inked comic emblem with very restrained metallic finish to match family.


## Ensaio de condição: Assustado (2026-10-15)

Arquivo `Assets/img/tokens/cond-assustado.png`, 512 × 512 RGBA, margem de
aproximadamente 6%. Aro vermelho comum às condições, miolo azul-marinho,
emblema emocional grande e punho de Enfrentar menor na base. Sem nome ou −2.
Gerado pela ferramenta integrada com equipe.png e enfrentar.png como referências.
É um ensaio completo de composição; os outros quatro ainda não foram criados.

### Prompt usado

Create ONE prototype round condition token for ASSUSTADO (afraid) in the same family as image 1. Image 1 is STYLE REFERENCE: perfectly circular front-facing midnight navy enamel disk, polished metallic rim, subtle bevel, no perspective. Change rim to rich RUBY RED metal. Replace the people symbol with a large simple warm-gold frightened FACE emblem centered in the upper-middle: wide round eyes with tiny pupils, raised tense eyebrows, small open mouth, clear worried expression, tasteful pictogram rather than realistic portrait. At the bottom inside the disk place ONE smaller gold FIST icon matching image 2 exactly in shape and orientation; image 2 is the existing Enfrentar action symbol. Fist must be a distinct secondary emblem with breathing room below the face. Preserve strong legibility and uncluttered hierarchy: emotion large, affected action smaller. NO WORDS, letters, numbers, -2, labels or extra symbols. Transparent outside the disk. Intended 512x512 final PNG, centered circular token diameter 88% of canvas with 6% transparent margin on each side. Navy interior remains solid. Red rim and gold emblems match premium metallic token family.


## Conjunto de condições completo (2026-10-15)

Adicionados `cond-culpado.png`, `cond-desesperancoso.png`, `cond-inseguro.png`
e `cond-irritado.png` em `Assets/img/tokens/`. Junto com Assustado, são cinco.
512 × 512 RGBA, aro vermelho e interior azul-marinho, sem nomes ou números.
Quando há dois movimentos afetados, os símbolos ficam lado a lado na base.
A composição segue o ensaio aprovado, sem reservar uma nova posição para −2.
A versão impressa com nome/penalidade poderá ser composta posteriormente.

Gerados pela ferramenta integrada, tendo cond-assustado.png como referência
visual e os ícones dos movimentos como referências adicionais.

### Prompt comum

Create ONE round condition token matching image 1 exactly in family, geometry and finish: polished ruby RED metallic circular rim, midnight navy enamel interior, warm gold emblems, subtle bevel, front orthographic view. Square transparent PNG intended final 512x512, disk diameter 88%, 6% transparent outside margin. No text, letters, numbers or -2. Main emotional FACE is large in upper-middle, matching the simple round gold pictogram face style of image 1. At the bottom INSIDE the disk put the specified smaller existing action icons from the other reference images. Secondary icons must be clearly separated side by side if there are two, no overlap with face or rim, recognizable at small size. Preserve their shapes. No extra symbols. 

### Complementos

- `cond-culpado.png` (referências: provocar, avaliar): GUILTY: replace frightened face with a remorseful round face, downcast eyes, inner brows raised in regret and small sorrowful mouth. Bottom row: image 2 shouting man profile (Provocar) LEFT, image 3 magnifying glass (Avaliar) RIGHT.

- `cond-desesperancoso.png` (referências: liberar-poderes): HOPELESS: replace frightened face with a deeply dispirited round face, drooping eyelids and downturned mouth, distinct from frightened, angry or guilty. Bottom centered: image 2 central dot with eight outward arrows, the Liberar power symbol.

- `cond-inseguro.png` (referências: defender, rejeitar-influencia): INSECURE: replace frightened face with a hesitant uncertain round face, one raised eyebrow, worried side glance, small uneven mouth. Distinct from wide-eyed fear or guilt. Bottom row: image 2 shield (Defender) LEFT, image 3 person bust with negative minus badge (Rejeitar Influência) RIGHT. Keep minus sign visible.

- `cond-irritado.png` (referências: confortar, perfurar-mascara): ANGRY: replace frightened face with a clearly angry round face, strongly inward-slanting eyebrows, narrowed eyes, clenched tense mouth and two small gold steam wisps near temples. Bottom row: image 2 handshake (Confortar) LEFT, image 3 superhero mask (Perfurar) RIGHT.


### Revisão de Desesperançoso (2026-10-15)

Expressão anterior parecia apenas desanimada. Substituída por aflição intensa,
olhos apertados e boca aberta, conforme referência emocional do Alan.
Arquivo mantido: `Assets/img/tokens/cond-desesperancoso.png`, 512 × 512 RGBA.
Ferramenta integrada, edição da arte anterior.

Prompt:

Edit ONLY the facial expression in this condition token. The current face looks mildly sad; it must look truly DESPERATE, DISTRAUGHT, anguished, overwhelmed and at the end of its rope, like the classic weary anguished emoji: eyes tightly squeezed shut into strong scrunched angular curves, eyebrows pinched and sharply contorted upward at the center, a large OPEN wailing mouth showing dark navy inside. Intense unmistakable despair, not anger, not sleepy sadness, not wide-eyed fear. Keep the circular warm-gold face as a simple bold metallic pictogram in the same upper-middle position and size. Preserve EVERYTHING else exactly: red metallic rim, navy enamel disk, lower gold central dot with eight outward arrows, proportions, spacing, 6% margin and transparent exterior. No tears, hands, sweat, new symbols, words or numbers. Final token square, intended 512px.
