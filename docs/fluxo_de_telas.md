# Fluxo de telas

> Redesenhado em 2026-10-13. **A Torre é do mestre.** Não há contas, login nem tela de jogador:
> jogador é um registro que o mestre mantém. A parte online da `docs/proposta.md` é futura
> (ver `docs/implementacao_futura.md`).

## Entidades

| Entidade | O que é |
|---|---|
| **Banco de jogadores** | todos os jogadores que o mestre já cadastrou (podem ser 20). Nome e status **ativo · inativo** |
| **Banco de personagens** | fichas criadas **sem sala**, com ou sem jogador — é por aqui que tudo começa. Um jogador pode ter várias |
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

## Personagem

```text
Personagem
  id
  nome
  tipo        Herói | NPC
  status      Herói: novo → criado → em carreira    NPC: não tem (é "npc")
  jogador     nenhum | jogador X
  sala        nenhuma | sala X
  origem      nenhuma | id da personagem de onde foi copiada
  foto da criação   (congelada ao passar de novo para criado)
  eventos           (potencial, Influência, avanços… por cima da foto)
```

- **novo**: em criação, seguindo as regras do livro (nível 1). Submete para aprovação.
- **novo → criado**: a validação da Torre ajuda o mestre; **fora da sala a validação é o mestre temporário**.
  Mesmo validador nos dois lugares, só muda quem dá a palavra final.
- **criado**: foto congelada; daí em diante, eventos por cima (no laboratório, à vontade).
- **NPC** não tem status nem validação de criação. Pelo livro (Resumo 7): nome, aparência marcante,
  **uma palavra** para o modo de agir e **motivação com verbo no infinitivo** ("vingar minha irmã").
  Subtipos: **adulto** (tem Influência sobre os PJs por padrão), **jovem** (começa sem) e **vilão**
  (+ geração ouro/prata/bronze/moderna, 1–5 movimentos de vilão, 1–5 condições; sem condição para marcar = derrotado).
  PNJ comum só marca condição se enfrentado diretamente. NPCs também vivem
  no laboratório (tem jogador que cria NPCs para completar o background) e também podem ir para a sala.
- **Influência** liga qualquer par: NPC → herói, herói → NPC, herói ↔ herói e NPC ↔ NPC.
- A `sala` de uma personagem não muda: ir para a sala cria uma cópia com `sala: X` e `origem` apontando
  para a original.

## Fora da sala é laboratório; dentro, o mestre é suserano

(Decidido em 2026-10-13. Sim, Crusader Kings.)

- **Fora da sala** as personagens são **experimentos**: cria quantas quiser, dá potencial, faz multiclasse,
  testa avanços. Nada disso tem consequência.
- **Toda personagem do banco tem a mesma estrutura em camadas**: id e **foto da criação** congelada,
  mais a camada de **upgrades e avanços** do laboratório (eventos).
- **Pôr no slot cria uma cópia** (a instância do Arkham). A personagem do banco não muda e pode virar
  cópia em outras salas. Ao pôr no slot, o mestre escolhe:
  - **só a criação** (um reset): a instância nasce da foto da criação, sem os upgrades;
  - **criação + upgrades**: a instância nasce da foto e os eventos do laboratório entram no log dela
    marcados como vindos do laboratório. Como continuam eventos, o mestre pode desfazer um a um.
- **Dentro da sala** a cópia é **vassala**: só o mestre mexe nela, pelo motor de regras.

### Como a instância é guardada: camada sobre camada

```text
personagem (banco ou instância) = origem (id da criação)
          + foto da criação   (estado aprovado, imutável)
          + log de eventos    (cada mudança desde a foto, com autor e quando)
estado atual = foto da criação + eventos aplicados em ordem
```

A **foto da criação** é a mesma do banco (congelada lá); pôr no slot é a aprovação, e o que o mestre
aprovou (só criação ou criação + upgrades) define com que eventos o log da instância começa.
Desfazer = tirar o último evento. Histórico da ficha e colheita do post = ler os eventos.
Se reaplicar tudo ficar lento um dia, guarda-se um cache do estado atual, mas a verdade é foto + log.

## Em aberto

1. **Validação de ficha** (regras de Rótulos iniciais por arquétipo): avisa no laboratório e na hora de
   pôr no slot; quem decide é o mestre.
