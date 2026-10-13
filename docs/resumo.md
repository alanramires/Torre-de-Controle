# Resumo — onde a Torre está

> Handoff para a próxima conversa. Atualizado no fechamento da **v0.0.1** (2026-10-13).
> Se algo aqui contradiz o código, o código manda — e este arquivo precisa de poda.

## Estado

- **Versão:** v0.0.1 — *Escopo, Cadastro de Players e construção de Personagens (em andamento)*.
  Relatório: `docs/relatorio_v0.0.1.md`.
- **A descoberta que organiza o que vem:** a Torre é **só do mestre** (sem contas, sem tela de
  jogador). A ficha tem a parte de **criação** (definitiva, no laboratório) e **placeholders de jogo**
  que hoje se mexem pela aba **Variáveis** em **modo teste**; no jogo, quem mexe é o **painel de
  controle do mestre**, como evento com origem e motivo. Esse painel ainda não existe.

## Onde eu parei

- **Validação por arquétipo, guiada pelo Alan** (ele abre a ficha, aponta o que falta, eu ajusto
  `Assets/Resources/Regras/arquetipos.json` e o render por tipo em `Assets/Scripts/UI/PainelFicha.cs`):
  - passaram: Farol, Amaldiçoada, Janus, Nova, Forasteira;
  - só regras escritas, **sem validação**: Bruta, Delinquente;
  - **não revisados**: Discípula, Legado, Transformado.
  - Regra: **esperar o Alan chegar no arquétipo**; não adiantar.
- Último pedido atendido: observações por movimento (`Personagem.ObsMovimentos`), botão ••• de texto
  longo (`Estilo.ComExpandir`), descrições opcionais na Nave Kirby.
- Testes: `Assets/Tests/EditMode/` (51 passando no harness dotnet fora da Unity; rodar também no Test
  Runner da Unity quando possível).

## Armadilhas

| Sintoma | Causa | Saída |
|---|---|---|
| Campo de texto não edita | `TMP_InputField` cria o cursor no `OnEnable` | montar desligado, ligar depois |
| Lista espremida / cabeçalho gigante | `HorizontalLayoutGroup` com `childForceExpandHeight` | `LayoutElement.flexibleHeight = 0` |
| Contorno sai cheio | `Outline` sob fundo translúcido | borda com 4 filetes |
| Glifo vira quadrado | fonte sem ▾ | "›" girado |
| Colunas desalinhadas | empate de `layoutPriority` com TMP | `ColunaElastica` com prioridade 2 |
| Tab perde o foco / edição desfaz a anterior | redesenho após edição; callback com dado velho | não redesenhar em edição ok; ler `p.Identidade` na hora |
| Rolagem volta ao topo | aba refeita | `MontarConteudo(manterRolagem: true)` |
| Roda do mouse não rola | sem raycast nos vãos; TMP multilinha engole | Image transparente; `RepassaRolagem` |
| Compila "verde" sem o arquivo novo | `.csproj` velho até a Unity regenerar | `tools/compilar.sh` após a Unity abrir; ou cópias `_v_` dos csproj |
| String C# quebrada | heredoc do Bash come `\` e aspas | editar com a ferramenta de arquivo |
| Texto de regra copiado | repo é **público** | texto próprio sempre; livro/acervo só como consulta |

## MVP (do `CLAUDE.md`)

| Item | Estado |
|---|---|
| Tela de entrada | de pé, mas com Jogadores e Personagens (sem partida/sala) |
| Criar personagem | **de pé** (laboratório, criação completa com pendências) |
| Mesa com N fichas clicáveis | falta (salas e slots não existem) |
| Pilha de Equipe | falta |
| Mão de cartas por jogador | falta |
| Rolador 2d6 + log | falta |
| Exportar log para o fórum | falta |

Fora do MVP, já de pé: cadastro de jogadores (ativo/inativo, ordem manual).

## Schema da ficha

- A personagem é gravada em JSON por `ArmazenamentoLocalJson` (`persistentDataPath/dados/`).
- **Ainda não há campo `versao` nem migração.** Campos mudaram de nome durante a v0.0.1
  (`historico` → `resumo`; Rótulo da máscara do Janus foi de extra para movimento): fichas de teste
  antigas podem precisar ser refeitas. Antes da primeira sala de verdade, criar `versao` + migração.
