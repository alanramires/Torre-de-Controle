# Handoff: Torre de Controle (ferramenta do MJ de Masks em Unity)

> Copie este arquivo como `CLAUDE.md` na raiz do projeto Unity novo (`D:/Unity Projects/TorreDeControle/`).
> Escrito em 2026-10-13 a partir da sessão de trabalho no acervo.

## O que é

Nome: **Torre de Controle** (referência à Watchtower de Smallville; faz par com a Sala de Mapas, outro projeto Unity do Alan).

Uma **mesa digital de controle** para o MJ de *Masks: A Nova Geração* (PbtA), **não** um jogo de aventura.
O Alan mestra no fórum novaerarpg.com e no WhatsApp, em formato assíncrono ("modo literário"). A dor principal
é o **controle**: ninguém registra progresso de ficha, o mestre perde horas formatando, poderes de outro arquétipo
quebram o layout, há discussões sobre PvP e o mestre não sabe quando usar os movimentos do MJ.
Só o mestre usa, local, na máquina dele. Responda em português.

## Onde estão os dados (fonte da verdade; NÃO copiar para cá)

Acervo: `D:/Acervo do RPG/2025/2025/Alan - Masks/`. Leia o `CLAUDE.md` de lá: regras, erros conhecidos, gerador de cartas.

- `Cards/catalogo.json`: 166 cartas (id, titulo, tipo, origem, arquivo PNG, modulo). Gerado por `Cards/gerador/catalogo.py`.
  Tipos: basico (12, PvE) · pvp (9) · arquetipo (48) · equipe (10) · extra (35, módulos de arquétipo) · adulto (5) · referencia · mj · personalizado.
- PNGs das cartas: 336×525, nos caminhos do catálogo (relativos ao acervo).
- `PARTIDA SIMULADA/01 FICHAS DE PERSONAGENS/*.json`: fichas (estado). O schema ainda está evoluindo (ver abaixo).
- `PARTIDA SIMULADA/`: emulação do fórum (ON/OFF game, rolagens, diário). Ler o README de lá.
- `botoes/`: ícones de controle do Alan (star0–15 = trilha de 15 avanços; estrelas bronze/prata/ouro 0–5 = potencial;
  pentagram0–5/filled = trilha da perdição; +/−, check etc.).
- `Cards/Tokens/`: tokens (Equipe, potencial, adiante, constante, controle, chamas, condição, Influência).
- `arte basica/Arquetipos 3/`: arte de cada arquétipo (296×433).
- `Livros/MASKS (Regras Oficiais).pdf`: livro em PT-BR. Texto original CC BY 4.0 (creditar Magpie Games / Brendan Conway);
  a tradução PT-BR e a arte do livro não são livres. Uso pessoal sem problema.

O caminho do acervo deve ser **configurável** (ex.: `config.json` ou ScriptableObject), nunca hardcoded espalhado.

## Modelo do jogo (decidido)

- **FSM**: estado = Rótulos (5, de −2 a +3), condições (5 booleanas), Influência (binária, por par de personagens),
  potencial (0–5), pilha de Equipe (da mesa), Rótulos trancados, Hora da Verdade (bloqueada/desbloqueada).
  Transições = movimentos e gatilhos (falha → +1 potencial; fim de cena; fim de sessão; quando o tempo passa).
- **Deckbuilding**: cada jogador tem o deck básico (12) e o PvP (9), mais as cartas do arquétipo, os movimentos de equipe e os extras.
  Os avanços são operações: comprar 1 carta de outro arquétipo · comprar um módulo inteiro de extra (cartas + tokens) ·
  trocar o deck de classe (mantém o núcleo: Rótulos, condições, potencial, Influência) · comprar do deck Adulto ·
  alterar estado (+1 Rótulo, trancar, desbloquear a Hora da Verdade) · aposentar.
- **Ficha = ids de cartas + tokens**, nunca texto de regra. Seções: Identidade · Crescimento · Núcleo · Módulos.
- **Limites**: modificador entre −3 e +4. Rótulo que estouraria o limite → marca condição e nada muda.
  Condição → −2 nos movimentos ligados a ela. Precisa marcar condição com todas já marcadas → sai de cena.
- **PvP é sequencial**: declaração simultânea, resolução em fila; Defender é a única interrupção (−2 antes de o resultado valer).

## Arquitetura proposta

1. **Motor de regras em C# puro** (sem MonoBehaviour), testável com testes de EditMode. Recebe **comandos**
   (`RolarMovimento`, `MarcarCondicao`, `MudarRotulos`, `GastarEquipe`, `ComprarCarta`…) e gera **eventos** num log.
   O log dá desfazer e alimenta a colheita.
2. **Persistência**: lê e grava os JSONs do acervo. Cada JSON tem `"versao"`; migrações explícitas quando o schema mudar.
3. **Tela = a mesa**: fichas em volta, pilha de Equipe com tokens no centro, mão de cartas por jogador (PNGs do catálogo).
4. **Exportar post**: gera em BBCode a "Mecânica da rodada" (colheita) para colar no fórum.

## MVP

- Tela de entrada → escolher a pasta da partida.
- Mesa com N fichas: Rótulos, condições, potencial e Influência, clicáveis.
- Pilha de Equipe no centro.
- Mão de cartas por jogador, a partir do catálogo.
- Rolador 2d6 + Rótulo que aplica condições e limites e registra no log.
- Exportar o log da rodada como texto para o fórum.

## Pendências e contexto

- Antes de codar, ler a arquitetura de `D:/Unity Projects/The Map Room` (projeto do Alan) e seguir o estilo dele.
- Em aberto: cartas PvP no verso das básicas; campos `verso` e `modo` (modificador|substituto|novo) + `modifica` no catálogo.
- O schema de ficha ainda vai mudar com a partida simulada: não congelar cedo.
- A partida simulada e o treino de MJ continuam no workspace do acervo, não aqui.
