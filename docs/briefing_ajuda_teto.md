# Briefing: o teto da rolagem, uma carta nova e a revisão das cartas que já existem

Para a IA que cuida do help (`Assets/Resources/Ajuda/catalogo.json`). Escrito em 2026-10-17. Três tarefas, nesta
ordem. As decisões de regra abaixo são do Alan e estão registradas em `Assets/Resources/Regras/nucleo.json`
(`modificador`) e `Assets/Resources/Regras/influencia.json`.

## A regra (decidida)

| Entra **na rolagem** (teto de −3 a +4) | Entra **depois do dado** (o resultado desliza livre) |
|---|---|
| Rótulo do movimento | Ajuda: +1 por colega que alcança, 1 ponto da pilha cada (nunca em si mesmo) |
| Adiante e constante | Renunciar à Influência: +1 numa jogada sua contra o alvo (soma com o passivo) |
| Avaliação (+1 por resposta usada) | Renunciar à Influência: −2 num movimento que quem rolou acabou de fazer (quem tem Influência sobre ele) |
| **Influência passiva: +1 em todo movimento em relação a quem você tem Influência** (livro p.81) | Defender contra PJ: com 7+, −2 na jogada do atacante (o alvo ou um terceiro que alcança) |
| −2 por condição ligada ao movimento | Agir de forma egoísta (só quem rolou: a mesa confirma, gasta 1, muda os próprios Rótulos) |

Fontes no livro (texto no acervo, `Livros/MASKS (Regras Oficiais) - texto.txt`): o teto na p.47 ("nunca pode
rolar com mais de +4 e menos de −3"), a Influência na p.78–81 (o +1 passivo e a renúncia), a ajuda na p.84–85, o
egoísta na p.85, Defender contra PJ na p.189–191. O livro não limita o que vem depois do dado: por isso desliza.

## Tarefa 1: corrigir "Pontos de Equipe"

Na carta `equipe/ajudar`, seção **LIMITES**, o texto termina com "respeite o teto do modificador". Está errado.
Trocar por algo como: "Vários colegas podem ajudar no mesmo teste; a ajuda vem depois do dado e não está presa ao
teto de −3 a +4 (o teto vale só na rolagem)."

## Tarefa 2: carta nova "A rolagem: antes e depois do dado"

- **id:** `panorama/antes-e-depois-do-dado` (categoria `referencia-rapida`, a Visão geral, ao lado de
  "Sucessos e falhas")
- **título:** A ROLAGEM: ANTES E DEPOIS DO DADO
- **origem:** "Visão geral · livro pp.47, 81, 84–85, 189"
- Seções curtas (texto aprovado pelo Alan):
  - **NA ROLAGEM:** Rótulo, adiante, constante, avaliação, +1 de Influência sobre o alvo, −2 por condição. O
    total fica entre −3 e +4.
  - **DEPOIS DO DADO:** ajuda (+1 de cada colega, da pilha), renunciar à Influência (+1 contra o alvo, ou −2 no
    movimento de quem rolou), Defender contra PJ (−2), agir de forma egoísta. Aqui o resultado desliza livre.
  - **QUEM PODE:** ninguém se ajuda; egoísta e o +1 da renúncia são de quem rolou; o −2 da renúncia é de quem tem
    Influência sobre quem rolou.
  - **EXEMPLO:** a Vespa provoca o Tiro-Certo com +1 de Influência e tira 6. Renuncia à Influência e chega a 7,
    mas o Tiro-Certo deixa de dar peso ao que ela pensa.

## Tarefa 3: revisar o que já existe antes de acrescentar (vale daqui para frente)

O catálogo cresceu e você só vinha acrescentando. A partir de agora, **antes de criar ou mudar qualquer carta**:

1. **Leia a categoria inteira** onde a carta vai entrar (e as vizinhas que tratam do mesmo assunto).
2. **Procure contradições e repetições.** Para esta tarefa, revise pelo menos todas as cartas que falam de:
   teto/modificador, Influência (passiva, renúncia, rejeitar), ajuda e pilha de Equipe, agir de forma egoísta,
   Defender (PNJ e PJ), golpe poderoso. Cada uma tem de bater com a tabela acima.
3. **Confira contra as regras da Torre** (`Regras/nucleo.json`, `influencia.json`, `equipe_e_sessao.json`,
   `movimentos_basicos.json`) e, na dúvida, contra o texto do livro no acervo.
4. **Não apague nem funda cartas sozinha.** Corrija texto errado; o que for repetido ou duvidoso, **liste**.
5. **Registre no `docs/ajuda.md`** uma seção "Revisão 2026-10-17" com: o que corrigiu (carta e seção), o que achou
   repetido (pares de ids) e o que ficou em dúvida para o Alan decidir.
6. Mantenha **texto próprio** (o repositório é público; o livro é só consulta) e as regras de forma do briefing
   anterior (seções curtas, mesma borda da categoria, sem PNG).

Ao terminar, avise o Alan com o resumo do que mudou e do que ficou para ele decidir.
