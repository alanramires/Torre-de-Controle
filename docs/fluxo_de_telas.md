# Fluxo de telas

> Redesenhado em 2026-10-13. **A Torre é do mestre.** Não há contas, login nem tela de jogador:
> jogador é um registro que o mestre mantém. A parte online da `docs/proposta.md` é futura
> (ver `docs/implementacao_futura.md`).

## Entidades

| Entidade | O que é |
|---|---|
| **Banco de jogadores** | todos os jogadores que o mestre já cadastrou (podem ser 20). Nome e status **ativo · inativo** |
| **Banco de personagens** | fichas criadas **sem sala e sem jogador** — é por aqui que tudo começa |
| **Sala** | uma campanha: número de **slots** (ex.: 6), NPCs, pilha de Equipe, log |
| **Slot** | lugar à mesa (ideia dos slots do `MatchController` do Map Room): **um jogador + uma personagem da sala** |
| **NPC** | personagem do mestre, modelo leve: nome, descrição, motivação/objetivo simples ("quero encontrar meu pai") e Influência com os PJs |

### Slot

A sala aceita N slots. Dos 20 jogadores do banco, só entram os que cabem nos slots.
Cada slot recebe uma personagem por um de dois caminhos:

1. **Do banco**: escolhe uma personagem do banco de personagens.
2. **Nova na sala**: cria a personagem já pertencendo à sala.

Nos dois casos a personagem fica **associada ao jogador do slot**. Jogador sumiu → troca o jogador do slot
(ou marca inativo); a personagem continua no slot.

## Telas

### Tela de entrada

- Banco de jogadores: criar, editar, ativar/inativar
- Banco de personagens: criar, editar, excluir (sem jogador)
- Criar sala (com o número de slots)
- Entrar na sala

### Na sala

- Slots: pôr jogador, pôr personagem (do banco ou nova), trocar jogador
- Criar NPCs
- Gestão das personagens: Rótulos, condições, potencial, Influência, Equipe, avanços
- Rolar dados (o mestre rola por todos)
- Exportar a colheita da rodada para o fórum

## Em aberto

1. **Personagem do banco no slot: cópia ou a própria?** Sugestão: **cópia** (a instância do Arkham).
   O banco vira um conjunto de modelos que nunca mudam, a cópia passa a pertencer à sala, e os dois
   caminhos acabam iguais: todo slot tem uma personagem da sala. Também deixa a mesma ficha do banco
   entrar em duas campanhas.
2. **Validação de ficha** na criação (regras de Rótulos iniciais por arquétipo): avisa, quem decide é o mestre.
