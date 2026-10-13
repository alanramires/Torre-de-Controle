# Fluxo de telas e modelo de personagem

> Redesenhado em 2026-10-13; modelo de personagem revisto no mesmo dia (personagem única com perfis).
> **A Torre é do mestre.** Não há contas, login nem tela de jogador: jogador é um registro que o
> mestre mantém. A parte online da `docs/proposta.md` é futura (ver `docs/implementacao_futura.md`).

## Os quatro pilares

| Módulo | Cuida de |
|---|---|
| **Gestão de Jogadores** | cadastro, edição, ativo/inativo |
| **Gestão de Salas** | campanhas, slots, participantes, personagens da sala, rodadas, histórico |
| **Gestão de NPCs** | personagens do mestre: comuns, mentores e aliados, antagonistas e vilões |
| **Gestão de Personagens** | criação, arquétipo, Rótulos, condições, potencial, movimentos, avanços |

O **motor de regras** atravessa os quatro: ele lê `Assets/Resources/Regras/*.json` e valida/aplica comandos
em qualquer módulo.

Gestão de NPCs e Gestão de Personagens são **duas portas para a mesma entidade**: a lista de NPCs
mostra as personagens de controle do mestre, a de Personagens mostra as de jogador.

## Jogador, sala e slot

| Entidade | O que é |
|---|---|
| **Jogador** | uma pessoa cadastrada. Nome e status **ativo · inativo** (inativo = sumiu, AFK, placeholder). Pode haver 20 |
| **Sala** | uma campanha: número de **slots** (ex.: 6), personagens da sala, pilha de Equipe, rodadas, log |
| **Slot** | lugar à mesa (ideia dos slots do `MatchController` do Map Room): **um jogador + uma personagem da sala** |

- Jogador e personagem são independentes. **A associação acontece no slot.**
- Um jogador pode ter várias personagens no laboratório; **na sala, uma por jogador** (MVP).
- Uma personagem pode existir sem jogador.
- Jogador sumiu → troca o jogador do slot (ou marca inativo); a personagem continua no slot e o mestre
  opera por ela. Isso **não** a transforma em NPC (ver "Controle" abaixo).

## Personagem: uma entidade, três dimensões, fichas por perfil

Herói, NPC, vilão, jovem e adulto **não são entidades diferentes**. Tudo é `Personagem`, com uma
identidade que persiste enquanto o resto muda.

```text
Personagem
  id, nome, aparência, descrição            ← identidade (nunca muda de entidade)
  controle      jogador | mestre            ← PJ ou PNJ (quais regras de personagem valem)
  maturidade    jovem | adulto
  papel         heroi | vilao | neutro
  origem        nenhuma | id de onde foi copiada (laboratório → sala)
  sala          nenhuma | sala X
  fichas        componentes por perfil (abaixo); os inativos ficam guardados
  eventos       tudo o que mudou, com motivo
```

As três dimensões são **independentes**:

| Exemplo | controle | maturidade | papel |
|---|---|---|---|
| Herói da equipe | jogador | jovem | heroi |
| Mentor | mestre | adulto | neutro |
| Vilã adolescente | mestre | jovem | vilao |
| Vilão clássico | mestre | adulto | vilao |
| Ex-herói que virou modelo para a cidade | mestre | adulto | heroi |

**NPC** é só o nome para `controle: mestre`. **Vilão** é papel narrativo, não idade nem controle.

### Controle é regra, não quem está clicando

`controle` diz **quais regras de personagem valem**: PJ (ficha de arquétipo, potencial, avanços,
Influência dos adultos por padrão…) ou PNJ (só marca condição se enfrentado diretamente, age pela
motivação…). Quem opera a personagem na Torre é sempre o mestre. Assumir o PJ de um jogador sumido
**não** muda o controle.

### Fichas por perfil (componentes)

A personagem carrega um ou mais componentes. A tela mostra os campos dos componentes ativos.

| Componente | Quando se aplica | Campos |
|---|---|---|
| **Perfil PNJ** | controle mestre | uma palavra para o modo de agir, motivação (verbo no infinitivo), traços marcantes |
| **Ficha de herói** | controle jogador | arquétipo, Rótulos, condições, potencial, movimentos (cartas), módulos/extras, avanços, Hora da Verdade, status de criação |
| **Ficha de vilão** | papel vilão | geração (ouro/prata/bronze/moderna), 1–5 movimentos de vilão, 1–5 condições |
| **Influência** | sempre (núcleo) | quem tem Influência sobre quem; qualquer par de personagens |

Mudar de perfil **não apaga** o componente antigo: ele fica inativo, com seus dados e histórico.
O ex-herói que vira mentor ainda tem a ficha de herói guardada.

### As quatro seções da ficha de herói

```text
IDENTIDADE   nome · codinome · jogador · arquétipo · aparência · habilidades · histórico
NÚCLEO       Rótulos (e trancados) · condições · potencial · Influência · Hora da Verdade
CRESCIMENTO  avanços marcados · cartas compradas (outros arquétipos, adultos)
MÓDULOS      extras do arquétipo (Impulsos, Perdição, Chamas, Máscara…) e os comprados por avanço
```

- **Núcleo sobrevive à troca de arquétipo** (como multiclasse): Rótulos, condições, **potencial**
  (decidido em 2026-10-13: fica no Núcleo) e Influência continuam iguais.
- **Módulos nunca somem**: na troca de arquétipo, o mestre decide quais continuam; os outros são
  **inativados** com tudo o que foi marcado, e podem voltar.

### Ciclo de criação (ficha de herói)

```text
novo  ──(aprovação)──▶  criado  ──▶  em carreira (= entrou numa sala)
```

- **novo**: montando a ficha pelas regras do arquétipo. Pode trocar de arquétipo à vontade, mas
  **trocar reseta a ficha de criação** (Rótulos, movimentos e extras recomeçam do novo arquétipo;
  a Identidade fica). Se já houver escolhas feitas, a Torre pede confirmação antes.
  Depois de aprovada, erro de criação se conserta com ajuste manual ou evento de correção, com motivo.
- **aprovação**: a validação da Torre aponta o que fere a regra; o mestre decide.
  Fora da sala, a validação faz o papel de mestre temporário.
- **criado**: foto da criação congelada.
- **em carreira**: copiada para um slot de sala. Formar a equipe e dar a Influência inicial são
  preparação da campanha, não o que define a carreira.

PNJ não tem esse ciclo.

### Transições: regulamentadas × do mestre

Personagens mudam durante a campanha **sem recriar a ficha**:
herói jovem → herói adulto → aposentado → mentor; herói → antagonista → vilão; PNJ → vilão.

O motor separa dois tipos:

| Tipo | O que é | Exemplos |
|---|---|---|
| **Regulamentada** | o livro prevê; o motor sabe o que fazer | avanço "aposentar-se ou virar modelo" (PJ vira PNJ adulto); avanço "mudar de arquétipo"; PNJ comum vira vilão (ganha ficha de vilão) |
| **Do mestre** | decisão narrativa excepcional | herói vira vilão; PNJ vira PJ de um jogador; mudar maturidade sem avanço |

Transição do mestre exige **motivo** e é registrada como "fora da regra" (mesmo mecanismo do ajuste manual).

**MVP:** o modelo suporta tudo isso desde já, mas a tela só oferece **criar** heróis, PNJs e vilões e
**promover PNJ a vilão**. As outras transições ficam para depois (ver `implementacao_futura.md`).

## Fora da sala é laboratório; dentro, o mestre é suserano

- **Fora da sala** as personagens (heróis e PNJs) são **experimentos**: cria quantas quiser, dá potencial,
  faz multiclasse, testa avanços. Nada tem consequência.
- **Pôr na sala cria uma cópia** (a instância do Arkham). A original fica no laboratório, intacta, e pode
  virar cópia em outras salas. Ao copiar um herói, o mestre escolhe:
  - **só a criação** (reset): a cópia nasce da foto da criação;
  - **criação + upgrades**: os eventos do laboratório vêm junto, marcados como vindos do laboratório.
- **Dentro da sala** o mestre controla tudo. O motor aplica as regras, mas aceita ajuste manual com
  motivo (avisa "fora da regra", não proíbe).

A identidade que persiste é a **da personagem na campanha**: a cópia tem `origem` apontando para a do
laboratório, e daí em diante tem histórico próprio.

### Camada sobre camada

```text
estado atual = foto da criação + eventos aplicados em ordem
```

- O log **nunca é apagado**. Corrigir = acrescentar um evento de correção que anula ou ajusta um evento
  antigo; o estado é recalculado (e a Torre mostra o que mudou nos eventos seguintes).
- Todo evento tem **motivo**, **autor**, **quando** e a **rodada** da sala.
- Eventos de personagem: Rótulo, potencial, condição, Influência, avanço, **maturidade**, **papel**,
  **controle**, ajuste manual, correção.
- Histórico da ficha e colheita do post = ler os eventos.
- PNJ criado direto (sem ciclo) tem como "foto" o estado em que foi criado.

## Telas

### Entrada

- Jogadores: criar, editar, ativar/inativar
- Personagens (laboratório): criar, editar, inativar heróis
- NPCs (laboratório): criar, editar, inativar; promover a vilão
- Salas: criar (com número de slots), entrar, inativar (campanha encerrada)

### Na sala

- Slots: pôr jogador, pôr personagem (do laboratório ou nova), trocar jogador
- NPCs da sala: criar, trazer do laboratório, promover a vilão
- Gestão das personagens: Rótulos, condições, potencial, Influência, Equipe, avanços
- Rolar dados (o mestre rola por todos)
- Rodada: abrir, registrar, fechar e exportar a colheita em BBCode

## Em aberto

1. **Validação de ficha** (Rótulos iniciais por arquétipo): avisa no laboratório e ao pôr na sala;
   o mestre decide.
2. **Herói adulto controlado por jogador**: o livro não prevê um PJ "virar adulto" (movimentos adultos
   não mudam a maturidade). Fica como transição do mestre até haver regra da casa.
