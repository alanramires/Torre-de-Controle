# Escudo do Mestre — o plano da Torre de Controle

> Resumo do que planejamos até 2026-10-13, revisado com as críticas de `revisor.md`.
> Detalhes: `fluxo_de_telas.md` (telas e entidades), `regras.md` (regras do motor),
> `implementacao_futura.md` (o que ficou para depois).

> **Estrela-guia:** a Torre deve permitir que o mestre administre toda a evolução mecânica de uma
> campanha **sem interromper o fluxo da narrativa**. Abrir, escolher a personagem, mudar, dizer por quê,
> voltar a mestrar.

## 1. O que é

A Torre é o **escudo do mestre** de Masks: uma ferramenta **só do mestre**, para controlar as fichas
e a mesa de uma campanha assíncrona (fórum + WhatsApp). Não é mesa virtual, não tem login, não tem
tela de jogador.

A dor que resolve: ninguém registra o progresso das fichas, o mestre perde horas formatando,
poderes de outro arquétipo bagunçam o layout, há briga de regra no PvP, e é difícil saber qual
movimento do MJ usar.

## 2. O que existe na Torre

| Coisa | O que é |
|---|---|
| **Jogador** | só um registro: nome + **ativo/inativo** (inativo = sumiu, AFK, placeholder) |
| **Personagem** | a ficha. Tipo **Herói** ou **NPC** |
| **Sala** | uma campanha, com **N slots** (ex.: 6), NPCs, pilha de Equipe e o log |
| **Slot** | lugar à mesa: **um jogador + uma personagem da sala** |

Dá para ter 20 jogadores cadastrados; na sala só entram os que cabem nos slots.

## 3. Telas

**Entrada**
- Jogadores: criar, editar, ativar/inativar
- Personagens (laboratório): criar, editar, excluir — com ou sem jogador
- Criar sala (com número de slots)
- Entrar na sala

**Na sala**
- Slots: pôr jogador, pôr personagem (do banco ou nova), trocar jogador
- Criar NPCs
- Gestão das personagens: Rótulos, condições, potencial, Influência, Equipe, avanços
- Rolar dados (o mestre rola por todos)
- Exportar a colheita da rodada em BBCode para o fórum

## 4. Personagem: laboratório e sala

**Fora da sala é laboratório.** Cria-se à vontade, dá potencial, faz multiclasse, testa avanços.
Nada tem consequência. Um jogador pode ter várias personagens; o mestre pode criar sem jogador.

**Dentro da sala o mestre é suserano.** Pôr uma personagem no slot cria uma **cópia** (como no
Arkham Horror). A original fica no banco, intacta, e pode virar cópia em outras salas.
Na sala, **o mestre controla tudo**. A Torre aplica as regras automaticamente, mas aceita
**intervenção manual**: quando algo foge da regra (ex.: 2 potenciais de recompensa narrativa), a Torre
avisa "fora da regra" e registra o motivo; não proíbe. O mestre é o mestre.

### Ciclo do Herói

```text
novo  ──(aprovação)──▶  criado  ──▶  em carreira
```

- **novo**: montando a ficha pelas regras do arquétipo (Rótulos iniciais, +1 livre, habilidades, movimentos, extras).
- **aprovação**: a Torre valida e aponta o que fere a regra; quem decide é o mestre.
  Fora da sala, a validação faz o papel de mestre temporário.
- **criado**: a **foto da criação** fica congelada.
- **em carreira**: a personagem entrou numa campanha (foi copiada para um slot de sala).
  Formar a equipe e distribuir a Influência inicial são **preparação da campanha**, não o que define
  a carreira.

NPC não tem ciclo: é sempre "npc".

### Como a ficha é guardada: camada sobre camada

```text
personagem    = origem + foto da criação (congelada) + eventos por cima
estado atual  = foto + eventos aplicados em ordem
```

Ao pôr no slot, o mestre escolhe:
- **só a criação** (reset): a cópia nasce da foto, sem os experimentos do laboratório;
- **criação + upgrades**: os eventos do laboratório vêm junto, marcados como vindos do laboratório.

**O histórico nunca é apagado.** Corrigir é acrescentar um evento de correção que anula ou ajusta um
evento antigo (o 3º de 12, por exemplo), e o estado atual é recalculado. Se a correção mudar o efeito de
eventos posteriores (ex.: um Rótulo que agora estouraria o limite), a Torre mostra o que mudou antes de
confirmar. O histórico da ficha e a colheita do post saem do mesmo log.

### Rodadas

Todo evento pertence a uma **rodada** (e, se quiser, a uma cena) da sala. A rodada fica **aberta**
enquanto o mestre registra; ao **fechar**, a Torre junta os acontecimentos e monta o BBCode da
"Mecânica da rodada" para o fórum. Eventos já exportados ficam marcados, para não publicar duas vezes.

```text
Sala: Nova Aeternia · Rodada 07 (aberta)
- Max: Superior +1, Salvador −1 (Influência da mentora)
- Jinx marcou Insegura
- Raposa gastou 1 ponto de Equipe
- Flâmula marcou 1 potencial (falhou em Enfrentar)
4 acontecimentos
```

### NPC (pelo livro)

Nome, aparência marcante, **uma palavra** para o modo de agir, **motivação com verbo no infinitivo**
("vingar minha irmã"). Três tipos:
- **adulto**: já tem Influência sobre os heróis;
- **jovem**: começa sem;
- **vilão**: + geração, 1–5 movimentos, 1–5 condições; sem condição para marcar = derrotado.

NPCs também podem ser criados no laboratório (para completar background) e levados para a sala.
**Influência** liga qualquer par: NPC↔herói, herói↔herói, NPC↔NPC.

## 5. Regras: o motor lê a nossa especificação, não o livro

`Assets/Regras/*.json` tem as regras **escritas por nós**, em dados, com a página de origem:

- Rótulos de −2 a +3 (estourou → nada muda e marca condição); modificador de −3 a +4
- Cada movimento básico: gatilho, Rótulo, 10+ / 7–9 / 6− e efeitos
- Condições: −2 em movimentos específicos, como remover, todas marcadas → sai de cena
- Influência, pilha de Equipe, início e fim de sessão
- Avanços como operações (tardios depois de 5)
- PvP **em fila**: declara no mesmo turno, resolve um por vez; **defender** é a única interrupção

O texto do livro fica no acervo, só para tirar dúvidas. **Nunca entra no projeto** (o repositório é público).

## 6. Como é construída (por baixo)

- **Motor de regras em C# puro**, testável sem abrir a Unity: recebe comandos
  (`RolarMovimento`, `MarcarCondicao`, `MudarRotulos`, `GastarEquipe`…) e grava eventos.
- **Nada é definitivo até o mestre confirmar** (herança do Map Room), mas **confirmar é um Enter**:
  "Max, Superior +1 porque enfrentou a mentora, Enter, pronto". Prévia só quando o comando tem
  escolhas (rolagem, opções 10+/7–9). Sem janelas em cascata. **Rapidez é prioridade.**
- **Armazenamento** atrás de uma interface: hoje JSON local da própria Torre. Tudo nasce aqui;
  o acervo é só consulta.
- **Cartas digitais** desenhadas pela Unity a partir de dados, com **help** em cada carta.
  Arte e fontes nossas (ou trazidas por demanda, com licença livre).
- Compilar pela linha de comando antes de entregar; versões `vX.Y.Z` com relatório e fechamento do dia.

## 7. MVP

1. Tela de entrada: jogadores, personagens, salas.
2. Criar personagem com validação das regras do arquétipo.
3. Sala com slots; pôr jogador e personagem.
4. Mesa: fichas clicáveis (Rótulos, condições, potencial, Influência), pilha de Equipe no centro.
5. Mão de cartas por personagem.
6. Rolador 2d6 + Rótulo aplicando condições e limites, registrando no log (**só o mestre rola**).
7. Ajuste manual com motivo; correção de evento antigo.
8. Rodada aberta/fechada; ao fechar, exportar a colheita em BBCode (marcando o que já foi exportado).

**Pronto quando:** o mestre controla uma rodada inteira da partida simulada pela Torre.

## 8. Em aberto

1. Validação de ficha: confirmar que só avisa e o mestre decide.
2. Faltam na especificação: movimentos e extras de cada arquétipo, movimentos adultos, vilões,
   movimentos do MJ.

## 9. Fora do escopo (por enquanto)

Contas e login, jogador entrando para ver a ficha, rolagem pelo jogador, co-mestre, mais de uma
personagem por jogador na mesma sala, trazer personagem de outra campanha, publicação no Unity Play.
Tudo anotado em `implementacao_futura.md`.
