# Implementação futura

Ideias aprovadas pelo Alan, mas **fora do MVP**. Não implementar sem ele pedir; o desenho atual não
deve impedi-las.

## Co-mestre

Uma sala pode ter mais de um mestre: o criador convida outra conta para mestrar junto.
*Para não bloquear:* papel na sala é uma lista de membros com papel, não um campo `mestre` único.

## Mais de uma instância por jogador na mesma sala

Um jogador pode controlar mais de uma personagem na sala, por exemplo jogando pelo amigo que sumiu.
*Para não bloquear:* a instância aponta para o jogador; a sala não supõe "um jogador = uma instância".

## Trazer personagem de outra campanha

O jogador pede para levar uma instância evoluída da campanha X para a sala Y ("o veterano"),
e o mestre da sala Y aprova. Se ficar complicado, alternativa mais simples: salvar a instância como
modelo novo no repositório da conta.
*Para não bloquear:* a instância guarda de qual modelo nasceu e é serializável sozinha, sem depender da sala.

(Registrado em 2026-10-13.)
