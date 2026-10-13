# Especificação de regras (`Assets/Regras/*.json`)

As regras que o motor usa, **escritas por nós** em formato de dados. O motor lê isto; não relê o livro.

## Por que existe

- O texto do livro (tradução PT-BR da Circuito Camaleão) **não pode entrar no repositório**, que é público.
- Reler 500 KB a cada sessão faz o modelo reinterpretar tudo e repetir erros já corrigidos.
- Aqui cada regra está resumida com palavras próprias, com `origem` apontando a página (`livro p.N`)
  ou `casa` quando é adaptação da mesa do Alan (jogo assíncrono no fórum).

## Arquivos

| Arquivo | Conteúdo |
|---|---|
| `nucleo.json` | Rótulos (−2 a +3, estouro → condição), modificador (−3 a +4), faixas 10+/7–9/6−, falha padrão, condições (penalidade, remoção), potencial, Hora da Verdade |
| `movimentos_basicos.json` | os 7 básicos + tomar um golpe poderoso: gatilho, Rótulo, faixas e efeitos |
| `influencia.json` | modelo binário, segunda Influência, +1 passivo, dizer quem você é, aproveitar, rejeitar, formas de perder |
| `equipe_e_sessao.json` | ajudar, egoísta, entrar em batalha, o tempo passa, início e fim de sessão |
| `avancos.json` | operações de avanço, tardios (depois de 5), personagem nova no meio da campanha |
| `pvp.json` | fluxo em fila, defender como única interrupção, variantes contra PJ |

Ainda não estão aqui: movimentos e extras de cada arquétipo, movimentos de equipe por arquétipo,
movimentos adultos, vilões, movimentos do MJ.

## Vocabulário de efeitos (`op`)

Um efeito é `{ "op": ..., ... }`. Os principais:

| op | significado |
|---|---|
| `marcar_condicao` / `remover_condicao` | `quem` (rolou, alvo…), `escolhe` (quem decide qual), `n` |
| `marcar_potencial` | `quem` |
| `mudar_rotulos` | um sobe e outro desce; `quem`, `escolhe`, `rejeitavel` |
| `somar_rotulo` | soma sem descer outro (avanços); `valor`, `n` |
| `equipe` / `equipe_definir` | soma `delta` na pilha / fixa em `valor` |
| `ganhar_influencia` / `perder_influencia` | `dono` tem Influência `sobre` alguém |
| `adiante` / `bonus_enquanto` | bônus na próxima rolagem / enquanto a condição valer |
| `modificar_rolagem` | muda o total de uma rolagem (pós-dados), `de`, `valor` |
| `escolher` | `n` opções de `opcoes` (ou `lista`/`opcoes_ref`), `quem_escolhe` |
| `perguntar` | `n` perguntas de `lista` |
| `sair_de_cena`, `golpe_poderoso`, `movimento_do_mj` | gatilhos de outros fluxos |
| `narrativo` | sem efeito mecânico: só texto para a tela/colheita |

Quem rola é `rolou`; o outro lado é `alvo`. Opções sem efeito mecânico existem para a tela e para o log.

## Regras para editar

1. **Nunca colar texto do livro.** Resumir com palavras próprias, curtas.
2. Toda regra nova leva `origem`.
3. Erro achado na regra → corrigir aqui e registrar em `_correcao` no item, para não voltar.
4. Dúvida → consultar o texto do livro no acervo
   (`D:/Acervo do RPG/2025/2025/Alan - Masks/Livros/MASKS (Regras Oficiais) - texto.txt`), não os resumos.
