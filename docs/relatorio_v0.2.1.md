# v0.2.1 — Ponto a ponto

> 2026-10-16, segunda metade do dia. A mesa pronta foi posta à prova lance a lance com o exemplo comentado da
> primeira luta: o Alan jogava cada lance na Torre, eu revia o texto, e cada desencontro virou botão ou
> correção de regra. O Alan: "tá divertido pra caramba testar ponto a ponto".

## O fio

O exemplo (`docs/exemplo_primeira_luta.md`) deixou de ser só um desenho: virou o **roteiro de teste** da mesa.
Cada vez que o texto pedia algo que a mesa não fazia, nasceu um recurso; cada vez que eu narrava algo que não
batia com a ficha ou com o livro, o Alan corrigiu (e quase sempre com razão).

## 1. A mesa ganhou o que o roteiro pediu

- **Trilhas do arquétipo** na Área do mestre → Potencial (genérico: todo campo `trilha` do `arquetipos.json`;
  hoje a perdição da Amaldiçoada), com "o que aconteceu?" obrigatório e aviso ao fechar 5 (sinal novo).
- **Refazer** no log: um desfazer por engano se refaz (outro evento, que também se desfaz).
- **Lembretes**: tag temporária sem valor, em heroína **ou vilão em cena** (ex.: "abertura: Aurora cedeu
  terreno"), resolvida com um clique. Nasceu de "com certeza eu vou esquecer disso".
- **Moedas de Influência** ao lado de cada assento: uma por colega sobre quem ela tem Influência, com o retrato
  dele no miolo transparente da moeda; mútua usa a moeda de mão dupla. Só PJ↔PJ.
- **Atalhos**: Sala ⇄ Mesa (nos dois sentidos, sem empilhar) e Ajuda na mesa; retrato e nome abrem a ficha.
- Mesa reorganizada para caber: log mais estreito, vilão à esquerda, assentos com coluna de moedas.

## ⚠ Achados de regra (o Alan corrigindo, conferido no livro)

- **Falha permite combinar movimentos** (p.187, a Professora): condição + golpe poderoso por um movimento da
  vilã. Daí a orientação **pesado em quem falhou, leve (perigo anunciado) em quem está perto**, e **use o
  movimento do vilão feito para aquela heroína**.
- **Ajudar** (p.84–85): +1 a um **colega**, depois do dado, **1 ponto por pessoa por rolagem**, vários podem
  somar, e **só quem alcança na ficção** (o livro nega a ajuda de quem está ocupado do outro lado da rua).
  Ninguém gasta da pilha para si mesmo ajudando.
- **Agir de forma egoísta** (p.85): **não é "+1 na rolagem"**. É desprezar/ignorar a equipe, **a mesa
  confirmar**, gastar 1 e mudar os próprios Rótulos (o resultado só sobe se subir o Rótulo da rolagem).
  Quem acabou de ajudar no movimento de outro não age de forma egoísta naquele lance.
- **Ceder terreno** (golpe poderoso 7–9): a oposição faz o que não conseguiria contra ela de pé, e **ela não
  reage naquele momento**; volta a agir na próxima oportunidade.
- **Reação de condição do vilão** é "leve", mas não fraca: prepara perigo e passa a bola, sem desfazer o que
  as PJs conquistaram (carta Vilão · Irritado tem o cardápio).
- **Amaldiçoada**: a trilha começa em 0 pontos e 1 sinal. **Visões sombrias** = marcar perdição, ter a visão
  e **fazer uma pergunta que o mestre responde com honestidade** (e responder honestamente decide a trama:
  a voz do Helix não é o Eclipse).

## Erros meus que valem registrar

- Inventei habilidade ("no escuro eu enxergo") que não estava na ficha da Estrela Negra.
- Expliquei o egoísta como "+1" e, depois, forcei um exemplo de egoísta onde a ficção não pedia. O Alan:
  "tenha paciência, a gente vai chegar lá".
- Puse ajuda do Pulsar quando ele estava preso noutra parte da cena.
- Levei os textos da tela para o feminino ("heroínas", "a líder", "ela sobre ela"); o Pulsar é ele. Virou
  regra guardada: neutro na Torre, gênero do personagem quando específico.
- `const string Controle` dentro de `Mesa` escondeu o enum `Controle` (renomeado para `BonusControle`).

## 2. Help (outra IA)

As três cartas do `docs/briefing_ajuda_combinar_movimentos.md`: Combinar movimentos (pesado e leve), Exemplo:
ajuda em combate, Exemplo: ceder terreno. E o caso de teste da Halcyon City entrou no repositório como
validador do produto.

## O que não terminou

- **Rolador** (o próximo corte): agora com as regras certas de ajuda (1 por colega que alcança), egoísta
  (confirmação da mesa + quais Rótulos mudam), golpe poderoso + condições, falha → potencial e lembrete do
  movimento de mestre.
- O exemplo da primeira luta segue da Estrela provocando em diante sem revisão lance a lance; o "agir de
  forma egoísta" fica para quando a ficção pedir (a Aurora já anunciou "o Ocaso é meu").
- Influência mudando Rótulos pela mesa, roteiro marcando sozinho, post BBCode.
- Nada visto por mim no Play mode além dos prints do Alan.
