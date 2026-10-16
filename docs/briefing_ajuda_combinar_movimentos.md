# Briefing: cartas "Combinar movimentos", "Ajuda em combate" e "Ceder terreno"

Para a IA que cuida do help (`Assets/Resources/Ajuda/catalogo.json`). Escrito em 2026-10-16, a partir de uma
descoberta do Alan ao validar a primeira luta (`docs/exemplo_primeira_luta.md`, lance da Aurora).

## O que fazer

Criar **uma carta nova** na categoria que você já fez, `movimentos-mj`, no mesmo formato das outras de lá
(`id`, `categoria`, `titulo`, `compacto: false`, `borda` igual à da categoria, `origem`, `secoes`
[{`titulo`, `texto`}]).

- **id:** `movimentos-mj/combinar-pesado-leve`
- **título:** COMBINAR MOVIMENTOS: PESADO E LEVE
- **origem:** "Movimentos do MJ · livro cap. 7, p.187 (a Professora)"
- **posição:** logo depois de `movimentos-mj/quando-usar` (que já tem a seção "LEVE OU PESADO"; esta carta é o
  passo seguinte: como **combinar** numa falha). Não repetir a definição de leve/pesado de lá.
- **Texto próprio** (repositório público): o livro é só consulta. Conferir no texto do livro no acervo
  (`Livros/MASKS (Regras Oficiais) - texto.txt`, linhas ~2884–2890, o exemplo da Professora) e ajustar se
  algo divergir. Seções curtas: se não couber na carta desenhada, cortar texto, não seção.

## As seções (rascunho aprovado pelo Alan)

- **NA FALHA, BATA FORTE:** numa falha o mestre tem controle total, e pode juntar mais de um movimento num
  lance só. O livro faz isso com a Professora: condição + golpe poderoso, as duas coisas por um movimento da
  vilã.
- **PESADO EM QUEM FALHOU:** em quem rolou e falhou, o efeito acontece: condição, golpe poderoso, o movimento
  do vilão. Não há chance de evitar.
- **LEVE EM QUEM ESTÁ PERTO:** para os outros, anuncie o perigo e pergunte "o que você faz?". Bater pesado em
  duas personagens pela mesma falha tira a escolha de quem estava perto.
- **USE O MOVIMENTO DELE:** se o vilão tem um movimento feito para aquela personagem (o Ocaso contra a Aurora),
  use-o no lugar de um "tome um golpe" genérico. É ali que o vilão mostra quem é.
- **NEM TUDO SE INTERCEPTA:** enquanto o movimento de quem rolou se resolve, um colega não pula para
  defender: o ataque foi rápido demais (o livro nega isso ao Rex no mesmo exemplo).

## Exemplo (pode virar a seção EXEMPLO, curta)

A Aurora falha ao socar o Ocaso. Pesado nela: ele absorve o golpe e devolve ("Patético!"); ela marca
Insegura e rola golpe poderoso. Leve em volta: a energia que sobra sai caótica e uma viga cede sobre a cabine
do vigia: "Mayane, você está na frente da porta. O que você faz?"

## Segunda carta: exemplo de ajuda em combate

Criar também, na categoria `equipe-sessao`, **logo depois de `equipe/ajudar`** (Pontos de Equipe), uma carta
de **exemplo** (as regras já estão certas em `equipe/ajudar`; não repetir, só mostrar em cena). Mesma borda da
categoria.

- **id:** `equipe/exemplo-ajuda-combate`
- **título:** EXEMPLO: AJUDA EM COMBATE
- **origem:** "Equipe · livro pp.84–85 (Toro contra Opala Sangrenta)"
- Conferir no texto do livro no acervo (linhas ~925–940): vários colegas podem ajudar a mesma rolagem, cada um
  com no máximo 1 ponto, e só quem de fato alcança (o livro nega a ajuda de quem está do outro lado da rua).

Seções (texto aprovado pelo Alan):

- **A CENA:** a Garça passa pelo Ocaso para cortar o cabo (Enfrentar) e tira 9.
- **QUEM PODE AJUDAR:** o Pulsar quer, mas está embaixo de uma viga do outro lado do galpão: não alcança. A
  Estrela está colada no Ocaso: alcança.
- **A AJUDA:** a Estrela segura o cabo esticado com telecinese e gasta 1 da pilha. O 9 vira 10.
- **MAIS GENTE:** se a Mayane gritasse onde está a fraqueza, gastaria outro 1: cada colega soma +1, um ponto
  por pessoa por rolagem.

## Terceira carta: exemplo de ceder terreno

Criar também, na categoria `movimentos-basicos`, **logo depois de `movimento/golpe-poderoso`** (que já lista
"Ceder terreno e dar abertura à oposição" no 7–9, numa linha), uma carta de **exemplo** de como o mestre
cobra essa escolha. Mesma borda da categoria.

- **id:** `movimento/exemplo-ceder-terreno`
- **título:** EXEMPLO: CEDER TERRENO
- **origem:** "Movimentos básicos · Tomar um golpe poderoso (livro p.59)"
- Conferir no texto do livro no acervo a definição de ceder terreno (recuar, se conter ou deixar uma abertura;
  o MJ decide o quê; em geral, a oposição faz algo que não conseguiria se você pudesse detê-la).

Seções (texto aprovado pelo Alan):

- **O QUE É:** você recua, se contém ou deixa uma abertura. O mestre decide o que a oposição faz com ela:
  algo que não conseguiria se você pudesse impedir.
- **NÃO ESQUEÇA:** anote na hora (na Torre, um Lembrete no vilão: "abertura: Aurora cedeu terreno") e cobre
  quando a ficção pedir, de preferência logo.
- **NA COBRANÇA:** a oposição age e a personagem **não reage naquele momento**; o mestre não pergunta "o que
  você faz?" a ela. Sem dano extra obrigatório: o preço é perder a vez.
- **DEPOIS:** ela volta a agir na próxima oportunidade que o mestre der, e aí rola normalmente.
- **EXEMPLO:** a Aurora cede terreno ao golpe do Ocaso. Irritado, ele derruba a parede: o teto cai em cima
  dela, que fica presa (invulnerável, mas fora da luta). Mais tarde, na vez dela, ela sai dos escombros
  (Liberar os poderes).

## Depois

Anotar no `docs/ajuda.md` e avisar o Alan. Se quiser ligar no jogo, o campo `ajuda` em `Regras/cartas.json`
pode apontar para esta carta a partir do movimento de mestre correspondente, como você fez com as outras.
