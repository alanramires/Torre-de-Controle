# Torre de Controle (ferramenta do MJ de Masks em Unity)

> Nasceu do `handoff.md` escrito em 2026-10-13 na sessão de trabalho no acervo.
> Repositório: `github.com/alanramires/Torre-de-Controle` (**público**: nada de material não livre aqui).
>
> **Ler também `docs/proposta.md`: a visão online muda decisões deste arquivo** (deixa de ser só do mestre;
> `IArmazenamento`, event sourcing com autor, permissões no motor, nada de `System.IO` fora do armazenamento local,
> direitos e campo `licenca`). Contas, salas, modelo × instância de personagem e telas: `docs/fluxo_de_telas.md`.

## O que é

Nome: **Torre de Controle** (referência à Watchtower de Smallville; faz par com a Sala de Mapas, outro projeto Unity do Alan).

Uma **mesa digital de controle** para o MJ de *Masks: A Nova Geração* (PbtA), **não** um jogo de aventura.
O Alan mestra no fórum novaerarpg.com e no WhatsApp, em formato assíncrono ("modo literário"). A dor principal
é o **controle**: ninguém registra progresso de ficha, o mestre perde horas formatando, poderes de outro arquétipo
quebram o layout, há discussões sobre PvP e o mestre não sabe quando usar os movimentos do MJ.
Na fase 1 só o mestre usa, local; nas fases 2 e 3 vira sala online (ver `docs/proposta.md`). Responda em português.

## O acervo é referência; tudo é criado aqui

Acervo: `D:/Acervo do RPG/2025/2025/Alan - Masks/`. Leia o `CLAUDE.md` de lá: regras, erros conhecidos, gerador de cartas.

**O acervo é só referência** (decisão do Alan, 2026-10-13). Cartas, catálogo, arte, ícones e fontes são
**criados no Unity**, com texto próprio. Nada do acervo é copiado para cá nem lido pelo build: ele serve para
consultar regras, ver como as cartas ficaram no papel e conferir o modelo de dados.
Isso também resolve os direitos por construção (ver `docs/proposta.md`).

O que está lá, para consulta:

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

**As fichas também nascem aqui**, criadas pela própria Torre. A Torre nunca lê o acervo em tempo de execução:
o caminho acima é só para consulta quando houver dúvida. As fichas JSON da partida simulada servem de modelo do schema.

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
2. **Persistência**: pelo `IArmazenamento` (fase 1: JSON local da própria Torre; fase 2: nuvem). Cada JSON tem `"versao"`.
3. **Tela = a mesa**: fichas em volta, pilha de Equipe com tokens no centro, mão de cartas por jogador (cartas digitais desenhadas pela Unity, com help).
4. **Exportar post**: gera em BBCode a "Mecânica da rodada" (colheita) para colar no fórum.

## MVP

- Tela de entrada → criar ou abrir uma partida.
- Mesa com N fichas: Rótulos, condições, potencial e Influência, clicáveis.
- Pilha de Equipe no centro.
- Criar personagem (ficha nasce na Torre).
- Mão de cartas por jogador, a partir do catálogo da Torre.
- Rolador 2d6 + Rótulo que aplica condições e limites e registra no log. **Só o mestre rola no MVP** (jogador rolar fica previsto no motor, desligado).
- Exportar o log da rodada como texto para o fórum.

## Estilo herdado do The Map Room

(`D:/Unity Projects/The Map Room`, lido em 2026-10-13.)

- **Ações transacionais**: nada é definitivo até o MJ confirmar. Seleções, menus e prévias de rolagem
  são provisórios e canceláveis; só o comando confirmado entra no log de eventos e muda o estado.
- **Dado é identidade, o alvo dá o sentido**: carta/Rótulo/condição são chaves (id + nome + descrição),
  sem booleano que conceda poder. Quem interpreta é a regra que as consulta. Renomear não pode quebrar nada.
- **Três camadas**: serviço burro (responde fatos sobre o estado) · consumidor (agrega, ranqueia) ·
  organizador (decide com política). Ranquear/desempatar nunca é trabalho do serviço.
- **Catálogo diz o que a coisa É; a partida diz onde ela ESTÁ**: catálogo de cartas é compartilhado;
  fichas e mesa são da partida. Nada de estado de uma partida em asset compartilhado.
- **Compilar pela linha de comando antes de entregar** (`tools/compilar.sh`, a portar do Map Room:
  `dotnet build` nos `.csproj`, e falha se algum `.cs` estiver fora do `.csproj`). Compilar prova tipos,
  não comportamento: comportamento é provado no Play mode, pelo Alan.
- **Versionamento `vX.Y.Z`** (X arquitetura · Y mudança localizada · Z ponto de salvamento), com
  `docs/relatorio_vX.Y.Z.md` explicando o porquê, linha no `CHANGELOG.md`, um commit por frente de trabalho,
  tag anotada e push do commit e da tag.
- Igual ao Map Room: nada publicado ainda, então **sem shims de migração** — se o formato estiver errado,
  muda o formato. O campo `"versao"` existe para quando houver dados de jogadores na nuvem (fase 2);
  a partir daí, migração explícita.

## Pendências e contexto

- Em aberto: cartas PvP no verso das básicas; campos `verso` e `modo` (modificador|substituto|novo) + `modifica` no catálogo.
- O schema de ficha ainda vai mudar com a partida simulada: não congelar cedo.
- A partida simulada e o treino de MJ continuam no workspace do acervo, não aqui. Para testar a Torre com eles,
  recria-se as personagens dentro da Torre.
