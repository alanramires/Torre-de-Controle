# Implementação futura

Ideias registradas, mas **fora do escopo atual**: a Torre é a ferramenta do mestre. Não implementar
sem o Alan pedir.

## Parte online (fases 2 e 3 da `docs/proposta.md`)

Contas, login, sala com jogadores reais vendo a própria ficha, anotações do jogador, pedido para
jogar e aprovação pelo mestre, rolagem pelo próprio jogador, publicação no Unity Play.
Se um dia vier: autenticação atrás de uma interface (`IAutenticacao`), central simulada com senha
`1` para testar, nada de guardar senha por conta própria.

## Co-mestre

Uma sala com mais de um mestre.

## Mais de uma personagem por jogador na mesma sala

Ex.: jogar pelo amigo que sumiu. (Com jogador inativo isso já se resolve em parte.)

## Transformações de personagem (telas)

O modelo já suporta (personagem única com `controle`, `maturidade`, `papel` e fichas por perfil; ver
`fluxo_de_telas.md`), mas as telas do MVP só criam heróis, PNJs e vilões e promovem PNJ a vilão.
Depois:

- **Regulamentadas pelo livro**: aposentar-se / virar modelo para a cidade (PJ vira PNJ adulto,
  ficha de herói fica guardada); mudar de arquétipo (troca o deck de classe, mantém o núcleo).
- **Do mestre, com motivo**: herói → antagonista → vilão; PNJ → PJ de um jogador; mudar maturidade;
  qualquer outra mudança de controle ou papel.
- Linha do tempo da personagem mostrando as fases (jovem heroína → modelo → mentora).

## Galerias de fotos da personagem

Na aba Identidade, duas galerias opcionais: **civil** e **mascarada**, cada uma com 1..n imagens
(navegação 1/n). Arte do jogador ou do mestre; o arquivo fica junto dos dados da personagem.

## Trazer personagem de outra campanha

Levar uma personagem evoluída da campanha X para a sala Y ("o veterano").

(Registrado em 2026-10-13.)
