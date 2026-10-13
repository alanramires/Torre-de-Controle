# v0.0.1 — Escopo, Cadastro de Players e construção de Personagens (em andamento)

> 2026-10-13. Primeiro dia do projeto: do `handoff.md` até a ficha de personagem quase completa.

## O fio do dia

O dia começou com um handoff que pedia "uma mesa digital de controle para o mestre" e terminou com a
**ficha de personagem** como peça central, montada a partir das fichas oficiais em inglês e com um
**modo teste** para validar tudo antes que as fontes de verdade (painel do mestre, eventos com motivo)
existam. No meio, o escopo foi e voltou duas vezes — e isso é a lição mais importante do dia.

## 1. Escopo: foi, voltou, e assentou

**O que aconteceu:** o handoff dizia "só o mestre, local". Uma proposta posterior (`proposta.md`)
levou a Torre para "sala online no Unity Play". Daí eu desenhei contas, login, aprovação de jogador e
assentos placeholder — e o Alan cortou: *"é pra ser uma torre de controle, pra mim, o mestre!"*.

**Onde assentou** (ver `docs/fluxo_de_telas.md` e `docs/escudo do mestre.md`):
- A Torre é **do mestre**. Jogador é só um registro (ativo/inativo). A parte online é futura.
- **O acervo é só referência**: cartas, regras, fichas, arte — tudo nasce aqui, com texto próprio.
  Isso resolve os direitos por construção (o repositório é público).
- **Laboratório × sala** (Crusader Kings): fora da sala tudo é experimento; pôr na sala cria uma
  **cópia** (a instância do Arkham) que o mestre controla.
- **Personagem única** com três dimensões independentes (controle PJ/PNJ, maturidade, papel) e fichas
  por componente. **Controle é regra, não quem clica**: assumir o PJ de um jogador sumido não o vira NPC.
- **Nada se exclui, se inativa** (o "Protheus/TOTVS" do Alan): o histórico aponta para tudo por id.
- **Ficha × painel do mestre**: a ficha tem a parte de **criação** (definitiva) e **placeholders de
  jogo**, que no jogo virão do painel como comando → evento com motivo.

**Por que importa:** cada expansão de escopo minha custou uma volta. Ficou na memória do projeto
(`escopo-torre-e-do-mestre`): partir do mínimo para o mestre; ideia maior vai para
`implementacao_futura.md` em uma linha.

## 2. Regras em dados (o motor não lê o livro)

- `Assets/Resources/Regras/*.json`: núcleo, movimentos básicos, Influência, Equipe e sessão, avanços,
  PvP — texto próprio, cada regra com `origem` (página ou "casa").
- `arquetipos.json`: criação dos 10 arquétipos tirada das **fichas em inglês**. Os **Rótulos iniciais**
  não saem no texto extraído (são círculos); foram lidos **pela cor** do número no PDF (o inicial vem em
  azul). Todos somam +2 — Farol bate com a ficha do Max, Legado com o exemplo do Huma.
- Foram a `Resources` porque a Unity só leva para o build o que está lá.

### ⚠ Achados que contradizem o que se acreditava

- **"Influência fica para o fim do prólogo, pelo livro" — errado.** Isso é adaptação do Tutorial 01
  do acervo para o fórum. No livro, a Influência é parte da criação, depois de "Quando a equipe se
  reuniu" — o que, de quebra, explica por que ela só se completa **com equipe** (na sala).
- **Os resumos do acervo (`resumos/`) não são fonte.** São do Alan e estão incompletos ou errados em
  pontos. A fonte é o texto do livro e, melhor ainda, as **fichas em inglês** (a tradução complica).
- **Amaldiçoada começa com 1 sinal da perdição** ("Choose one doomsign you already hold at character
  creation"). O Alan tinha lido 0 — tradução dele; corrigido na conversa. Vale conferir se o material
  do acervo repete isso.
- **Amaldiçoada e Nova não escolhem movimentos na criação**: os extras fazem esse papel.
- **14 avanços** em todo arquétipo (8 + 6); o último possível é aposentar/virar modelo — a carreira
  termina aí (achado do Alan).

## 3. Cadastro de Jogadores

Core em C# puro (`noEngineReferences`), armazenamento JSON isolado (único com `System.IO`), UI montada
por código no visual do menu do Alan (`UI/Estilo.cs`). Inativar em vez de excluir; blocos
ATIVOS/INATIVOS com ordem manual por arrasto (`Core/Ordenacao.cs`).

## 4. Personagens e a ficha

- Laboratório: criar sem sala e sem jogador; arquétipo travado depois de concluir; trocar de arquétipo
  em "novo" zera a criação (com confirmação dizendo o que fica).
- Fichário: **Identidade · Histórico · Núcleo · Crescimento · Módulos · Variáveis**.
  - Habilidades por tipo de regra vindo do JSON (lista, fixas travadas, linha com negação, mentora).
  - Aparência e resumo livres; perguntas de histórico cobradas; equipe e Influência desativadas fora de sala.
  - Núcleo: Rótulos com +1 livre (azul = do arquétipo, dourado = bônus); potencial em pontos corridos.
  - Módulos: deck do arquétipo + extras, com subseções por movimento e campos de jogo.
  - **Concluir criação só sem pendências** (no laboratório não há fluxo de aprovação; a aprovação do
    mestre é pôr no slot da sala).
  - **Variáveis** (só do mestre) e **modo teste**: o disco guarda a personagem real; desligar desfaz.
- Exceções por arquétipo, validadas com o Alan: **Farol** (equipamento, bicho, impulsos cumpridos),
  **Amaldiçoada** (trilha → sinais, perdição final, uso do santuário), **Janus** (Rótulo da máscara no
  próprio movimento), **Nova** (chamas como reserva da cena), **Forasteira** (Nave Kirby como ficha
  aberta, controle e exigência do povo, dispositivos com cargas).
- Tipos de campo genéricos que saíram disso: trilha que rende escolha, lista que cresce no jogo com
  opção final, condições com lacuna, reserva, registro, itens, texto opcional / várias linhas.

## Erros meus que a próxima sessão não deve repetir

- **Generalizar campo da ficha.** Pus "como se manifesta" em todo arquétipo; só Bruta e Transformado
  pedem. Regra: ler o trecho nas 10 fichas em inglês antes de modelar (memória `regra-so-o-que-a-ficha-pede`).
  (Depois virou opcional para todos, a pedido — mas como opcional, não como regra.)
- **Ler as estrelas do Alan como potencial.** Elas contam **avanços** (bronze 1–5, prata 6–10, ouro 11–15).
- **Me adiantar** para Bruta e Delinquente enquanto ele validava o Farol.

## Armadilhas técnicas que custaram tempo

| Sintoma | Causa | Saída |
|---|---|---|
| Campo de texto não edita | `TMP_InputField` cria o cursor no `OnEnable` | montar o objeto desligado e ligar depois |
| Cabeçalho gigante, lista espremida | `HorizontalLayoutGroup` com `childForceExpandHeight` se declara esticável | `LayoutElement.flexibleHeight = 0` na fila |
| Botão de contorno saiu cheio | `Outline` desenha cópias inteiras sob fundo translúcido | borda com 4 filetes |
| Seta vira quadradinho | a fonte do TMP não tem ▾ | "›" girado |
| Colunas desalinhadas | empate de `layoutPriority` com o TMP fica com o **maior** valor | prioridade 2 |
| Tab perde o campo | redesenhar destrói o campo de destino | edição bem-sucedida não redesenha |
| Edição desfaz a anterior | callback capturava a identidade antiga | ler `p.Identidade` na hora |
| Roda do mouse não rola | vãos sem raycast; TMP multilinha engole a roda | fundo invisível na lista; `RepassaRolagem` |
| Rolagem volta ao topo | aba refeita do zero | guardar e restaurar `anchoredPosition` |
| Build verde sem compilar arquivo novo | `.csproj` desatualizado (e o Infra referencia o Core antigo) | `tools/compilar.sh`; ou cópia temporária de Core+Infra+UI |
| C# com string quebrada | o Bash da ferramenta come `\n` e aspas em heredoc | edições assim pela ferramenta de arquivo |

## O que não terminou

- **Validação do Alan, arquétipo por arquétipo**: Farol, Amaldiçoada, Janus, Nova e Forasteira passaram
  pela conversa; **Bruta e Delinquente** têm só as regras escritas, sem validação; **Discípula,
  Legado e Transformado** não foram revisados (movimentos e extras ainda genéricos).
- **Salas, NPCs, painel de controle do mestre** — nada construído. Os placeholders de jogo na ficha
  (potencial, trilhas, cumpridos, santuário, chamas, controle, dispositivos) esperam o painel.
- **Motor de regras, rolador 2d6, log de eventos, correção, exportar BBCode** — o núcleo do MVP ainda
  não existe; a ficha grava estado direto (no jogo, isso deve virar evento com rodada e motivo).
- **Efeitos dos avanços** (comprar carta, mexer em Rótulo, trocar arquétipo) — só a marca.
- **Marcas visuais** (estrelas do Alan, pentagramas) — anotadas em `implementacao_futura.md`.
- **Ajuda** das cartas/arquétipos — deixada para o fim, como pedido.
- **Personagens antigas**: campos foram renomeados no meio do dia (`historico` → `resumo`, Rótulo da
  máscara mudou de extra para movimento); dados de teste anteriores podem precisar ser refeitos.
