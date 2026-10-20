# v0.5.1 — Validação dos movimentos básicos

> 2026-10-20. A Torre começou a ser conferida contra os exemplos do próprio livro: cada exemplo vira uma cena na
> campanha Pacatópolis, os dados são fixados no valor do texto ("Rolar fixo") e o que a mesa mostra tem de bater com
> o que o livro conta. No caminho entraram o deck do MJ, as cenas paralelas e o Liberar seus poderes.

## O fio

A ideia do Alan: se o livro diz "Marissa rola + Perigosa e tira 8, pega as garras, a Ursa marca Irritada, a Toro toma
um golpe poderoso", a Torre tem de chegar exatamente aí. Com o "Rolar fixo" (a soma dos dados) e uma cena por
exemplo, cada validação fica separada no log, e a cena guarda quem estava nela.

## Cenários validados (Pacatópolis)

| Cenário | Livro | O que exercitou |
|---|---|---|
| Toro falha nos soldados de preto | p.33–34 | Enfrentar, falha, +1 potencial, movimento do MJ (Cobrar um preço pela vitória, pesado) |
| Toro × Ursa | p.58 | Enfrentar 8, "Arrancar algo", troca: Ursa marca Irritado, Toro toma golpe; as duas pendências |
| Rex × subordinados | p.58 | Validação negativa: capanga não aciona o movimento (não rola) |
| Fissão no reator (com o Vértice) | p.62 | Liberar seus poderes, 13, "você consegue" |
| Liberar no 7–9 (nosso) | — | Condição à escolha; instável/temporário; o instável pedindo golpe; Desesperançoso −2 |

Interpretações nossas, anotadas: os soldados são **ameaça** (p.33: "bem treinados, rajada de disparos"), então o
Enfrentar vale e eles são um vilão de **1 condição** ("nem senti", p.195), **jovens** (são irmãos de laboratório da
Toro; adulto teria Influência por padrão sobre as heroínas). O movimento do MJ do primeiro exemplo não é nomeado no
livro; "Cobrar um preço pela vitória" é leitura nossa.

## O que mudou e por quê

### O deck do MJ (`Regras/movimentos_mj.json`, `Core/Regras/MovimentosDoMj.cs`)

O Alan pediu o deck separado em leves e pesados. **O livro diz o contrário** (p.177): "você decide o peso de qualquer
movimento", e mostra a mesma cena leve e pesada. Decisão: o peso se escolhe ao jogar e fica **no log**, em cada
movimento; a tendência só vem pré-marcada onde o livro aponta (infligir condição, ganhar Influência). Infligir uma
condição marca junto; a falha deixa a pendência "Movimento do MJ" — menos no golpe poderoso, que descreve a própria
falha. O termômetro "leves · pesados" conta a cena.

### Cenas paralelas

Cada cena guarda quem está nela; o evento guarda a cena; o log da mesa mostra a cena aberta, e a Área do mestre ganha
o **Log da campanha** inteiro. Uma heroína fica numa cena só ("em outra cena: X"). O que pôs ou tirou alguém de cena
só se desfaz naquela cena.

### Desfazer e refazer

O Refazer fazia uma **cópia** sem a rolagem: a original seguia desfeita e as pendências dela sumiam (o Alan desfez três
e refez, e o log ficou bagunçado). Agora o refazer **reativa a original**, e o par desfazer/refazer sai da tela como as
idas e voltas. Desfazer e escolher outra coisa deixa o rastro (riscado + "corrigiu"), de propósito.

### Liberar seus poderes (p.60–62)

`/lsp`; no 7–9 o MJ marca temporário, instável ou "instável + golpe poderoso" (decisão do Alan, p.61: "O MJ pode até
pedir uma rolagem para tomar um golpe poderoso"). O painel passou a mostrar **o movimento inteiro** (as faixas que não
valem, em cinza) e a nota de cada opção — antes escondia as outras faixas e o Liberar parecia incompleto.

### Um arquétipo por equipe (p.16)

"Não pode ter dois jogadores com o mesmo arquétipo." A Torre não conferia. Agora recusa ao pôr na vaga ou trocar o
arquétipo; quem saiu da equipe (aposentada ou inativa) não conta — a Academia pode mandar outro Nova (decisão do
Alan). Os testes montavam equipes de Novas iguais: a ficha pronta dos testes passou a variar o arquétipo.

### Influência: "Clique errado? Desfazer"

O Alan clicou errado e deixou "perdida" a Influência da Toro sobre os soldados. Nova exceção ao "nada se exclui",
pedida por ele e registrada no `CLAUDE.md`: o clique errado no seletor PJ↔NPC apaga o registro.

### Dados fixos variando (outra IA, `be91399`)

O par do "fixo" passou a ser sorteado entre os que dão a soma (o "8" não é sempre 4 + 4).

## Meus erros do dia

- **NRE que sumiu com metade da mesa**: pedi o `LayoutElement` ao conteúdo do `ListaRolavel`, e ele mora na lista. O
  redesenho parou depois dos assentos (sem centro, log, Assistindo) e os botões ficaram sem o ajuste de texto. Achado
  no `Logs/Editor.log`. A armadilha que eu tinha escrito no resumo dizia o contrário; corrigida.
- **Patch com contexto zero**: ao separar meus trechos dos da outra IA no `Mesa.cs`, o `git apply --unidiff-zero` pôs
  duas inserções uma linha acima (o `return` antes do código novo). Vi antes de commitar; montei o índice desfazendo os
  trechos dela no texto.
- **Página do livro**: a "página 38" do Alan não era a impressa; perguntei em vez de adivinhar.

## Verificação

- Core: 220 testes no harness (eram 202 na v0.5.0; 11 são da outra IA). `tools/compilar.sh` passa.
- **Visto no Play pelo Alan:** os cinco cenários acima, o deck do MJ, a gestão da cena, o desfazer/refazer, a carta de
  condição, o Desesperançoso na conta. **Não visto ainda:** o "Clique errado? Desfazer" na ficha do NPC, a recusa do
  arquétipo repetido na tela, os botões do menu do help com a altura nova, o Log da campanha.

## O que não terminou

- **Defender**: o próximo movimento (proteger alguém de uma ameaça; a interrupção de PvP; o Controle do "Chegar na hora
  certa").
- **Saídas de cena pela regra** (golpe 10+ "afastar-se", sem condição livre): ainda só texto.
- **Resto do modificador**: adiante, constante, Influência passiva +1.
- **Influência na mesa**: "Ganhar Influência" (MJ) e "descontar num colega" são só registro.
- **Registro das validações** (`docs/validacao_livro.md`, roteiro de regressão): oferecido, não feito.
- **Exportações sem BOM**: o Bloco de Notas mostra os acentos errados; oferecido, não feito.
