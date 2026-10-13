# Proposta: Torre de Controle online (adendo ao HANDOFF - Unity)

> Complementa o `CLAUDE.md` / `HANDOFF - Unity.md` já lido. Copie para a raiz do projeto
> (`proposta.md`) e acrescente ao `CLAUDE.md` do projeto a linha:
> "Ler também `proposta.md`: a visão online muda decisões do handoff."
> Escrito em 2026-10-13.

## O que mudou

A Torre de Controle **não é mais só local**. Vai ser publicada no **Unity Play** (build WebGL) com dois lados:

- **Mestre: o escudo do mestre.** Controla tudo e mexe na ficha de qualquer personagem, a qualquer hora,
  **sem precisar que o jogador esteja online**. O jogo continua assíncrono, no fórum e no WhatsApp.
- **Jogador: um lugar para consultar.** Entra quando quiser, vê a própria ficha atualizada, faz anotações e
  aciona o **help** das cartas (as cartas são digitais: cada uma explica a si mesma). **Não é uma mesa virtual
  ao vivo** nem concorrente do D&D Beyond: não há sessão em tempo real, presença ou turno sincronizado.

Isto **substitui** do handoff 1: "só o mestre usa" e "local na máquina dele". O resto continua valendo
(FSM, deckbuilding, ficha = ids de cartas + tokens, motor de regras em C# puro, log de comandos e eventos).

Arte: vem **por demanda** (trazida quando houver) **ou é criada por nós**. O acervo é só referência.

## Fases (não pular)

| Fase | Escopo | Pronto quando |
|---|---|---|
| **1. Local** | Só o mestre, dados em JSON local da Torre (nascem aqui), o MVP do handoff 1 | O mestre controla uma rodada inteira da partida simulada pela Torre |
| **2. Online** | Backend na nuvem, sala com código, papéis mestre/jogador; jogador vê a ficha, anota e consulta as cartas | O mestre fecha uma rodada offline dos jogadores, e cada um entra depois e vê a própria ficha atualizada |
| **3. Publicação** | Arte e textos próprios, créditos, build no Unity Play | Nenhum asset de terceiro no build |

A fase 1 **não pode** ser escrita de um jeito que precise ser refeito na fase 2. Por isso:

## Decisões de arquitetura (valem desde a fase 1)

1. **`IArmazenamento`**: interface para carregar e salvar partida, fichas, catálogo e log.
   - Fase 1: `ArmazenamentoLocalJson` (lê e grava os JSONs da própria Torre; o acervo nunca é lido).
   - Fase 2: `ArmazenamentoNuvem` (Firebase, Supabase ou Unity Gaming Services; decidir na fase 2).
   - Nenhuma outra parte do código sabe onde os dados estão.
2. **Event sourcing**: o estado é o resultado de aplicar o log de eventos. O log dá desfazer, histórico da ficha
   e a colheita do post. Na fase 2 o mestre grava eventos e o jogador lê; não há sincronização ao vivo.
   Todo evento tem `autor` (mestre ou jogador X) e `quando`; rolagens também têm `rolou: mestre|jogador`.
3. **Papéis e permissões** no motor, não na tela: o mestre emite qualquer comando sobre qualquer ficha;
   o jogador **lê** a própria ficha e **escreve só as próprias anotações**.
   **Rolagem: as duas opções ficam previstas** (mestre rola por qualquer um; jogador rola a própria, ligável
   por partida). **No MVP só o mestre rola**; a permissão do jogador fica desligada, mas o motor já a modela.
   O jogador vê só o que é público (nada de `00 CONTROLE DO MESTRE`: bastidores, validação, personas).
4. **WebGL não grava arquivo local.** Não use `System.IO` fora do `ArmazenamentoLocalJson`.

## Direitos (a fase 3 depende disto; projetar desde já)

- **Texto das regras**: o original em inglês de *Masks* é **CC BY 4.0** (creditar Brendan Conway / Magpie Games).
  A **tradução PT-BR é da Circuito Camaleão (todos os direitos reservados)**. Na versão pública, todo texto de carta
  e de regra deve ser **próprio** (tradução nossa do inglês ou reescrita). As cartas novas do gerador já seguem isso;
  as originais do Alan em `Cards/` muitas vezes copiam a tradução e precisam ser reescritas antes da fase 3.
- **Marcas**: não usar "Masks", "Pacatópolis", "Exemplares" no nome nem na UI. O cenário é **Nova Aeternia** (do Alan).
  Crédito: "compatível com *Masks: A New Generation*, de Brendan Conway / Magpie Games, CC BY 4.0".
- **Arte**: a de `arte basica/Arquetipos 3/` e os recortes `Cards/MJ/` vêm do livro e **não** podem ir para o build
  público. Arte é um **pacote trocável**: carregar por id (`arquetipo/bruta`) de um manifesto de arte; trocar o pacote
  não muda código. Tokens e botões do Alan (`Cards/Tokens/`, `botoes/`) são dele e podem ser usados.
- O catálogo deve ganhar o campo `licenca` por carta e arte (`propria` | `cc-by` | `terceiros`),
  e o build público falha se houver `terceiros`.
