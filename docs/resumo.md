# Resumo — onde a Torre está

> Handoff para a próxima conversa. Atualizado no fechamento da **v0.0.2** (2026-10-13).
> Se algo aqui contradiz o código, o código manda — e este arquivo precisa de poda.

## Leitura para retomar

Leia este resumo junto com [regras_masks.md](regras_masks.md): entendimento do sistema,
cuidados de interpretação e direção dos painéis de ajuda, em palavras próprias.
As regras executáveis continuam em `Assets/Resources/Regras/*.json`; consulte também
`CLAUDE.md`, `regras.md`, `fluxo_de_telas.md` e os achados dos relatórios antes de alterar regras.

**Duas IAs na mesma pasta:** esta (mecânica: regras, fichas, salas, mesa) e outra (imagens: retratos,
ícones, fotos). Antes de commitar, `git status` e ler o diff do que não é seu; não assumir que arquivo
novo é seu.

## Estado

- **Versão:** v0.0.2 — *Salas, instâncias e o ciclo da ficha*. Relatório: `docs/relatorio_v0.0.2.md`.
- **O que organiza o que vem:** a ficha tem um **caminho**, e cada ação depende de onde ela está:

  ```
  laboratório:  novo ⇄ em aprovação
  sala:         em aprovação ⇄ em revisão; em aprovação → em carreira → aposentado (sem volta)
  ```

  Pôr na vaga **copia** a ficha (a original fica no laboratório). Em carreira só muda por evento da
  **mesa** (com rodada e motivo), que existe no Core mas ainda não tem tela.
- **Princípio:** a Torre é só do mestre, mas se comporta como se um dia fosse pública (conteúdo do
  jogador passa por moderação: fotos por link com aprovar/recusar).

## Onde eu parei

- **Segunda etapa (equipe na campanha)** — `Core/Personagens/CadastroPersonagens.Equipe.cs`: resposta da
  equipe, relacionamentos com colegas e um **rascunho** da Influência inicial. Sem tela e **sem travar**
  `AprovarParaCampanha`. **Esperar o Alan explicar como quer a Influência** antes de seguir.
- **Área do mestre** — o botão está na sala (`PainelSala.AreaDoMestre`, só avisa "em construção"). O
  Core está pronto em `Core/Mesa/Mesa.cs` (rodada, eventos, potencial, condições, Rótulos com estouro,
  corrigir). Falta a tela: cartões das personagens em carreira, comandos com motivo, log da rodada.
- **Validação por arquétipo**, guiada pelo Alan: passaram Farol, Amaldiçoada, Janus, Nova, Forasteira;
  Bruta e Delinquente sem validação; Discípula, Legado e Transformado sem revisão. Esperar o Alan.
- Anotados para depois: aposentar no 15º avanço; ajuste do mestre em ficha em carreira
  (`implementacao_futura.md`).
- Testes: `Assets/Tests/EditMode/` — 84 passando no harness dotnet fora da Unity; rodar também no Test
  Runner da Unity.

## Armadilhas

| Sintoma | Causa | Saída |
|---|---|---|
| Campo de texto não edita | `TMP_InputField` cria o cursor no `OnEnable` | montar desligado, ligar depois |
| Lista espremida / cabeçalho gigante | `HorizontalLayoutGroup` com `childForceExpandHeight` | `LayoutElement.flexibleHeight = 0` |
| Coluna fixa esticando e espremendo as outras | herda `flexibleWidth` de um filho (campo de texto) | `LayoutElement` com `flexibleWidth = 0`, prioridade 2 |
| Contorno sai cheio | `Outline` sob fundo translúcido | borda com 4 filetes |
| Glifo vira quadrado | fonte sem ▾ | "›" girado |
| Colunas desalinhadas | empate de `layoutPriority` com TMP | `ColunaElastica` com prioridade 2 |
| Tab perde o foco / edição desfaz a anterior | redesenho após edição; callback com dado velho | não redesenhar em edição ok; ler `p.Identidade` na hora |
| Tab pula para o painel de trás | dois `NavegacaoTab` ligados | só o último ligado responde |
| Rolagem volta ao topo | aba refeita | `MontarConteudo(manterRolagem: true)` |
| Roda do mouse não rola | sem raycast nos vãos; TMP multilinha engole | Image transparente; `RepassaRolagem` |
| Ficha abre em branco | `AddComponent<Image>` num objeto que já tem TMP vem **nulo** | um gráfico por objeto; ver `Logs/Editor.log` |
| Compila "verde" mas a tela quebra | `tools/compilar.sh` só prova tipos | conferir o `Logs/Editor.log` da Unity |
| Compila "verde" sem o arquivo novo | `.csproj` velho até a Unity regenerar | `tools/compilar.sh` após a Unity abrir; ou cópias `_v_` dos csproj |
| Teste passa no harness e falha na Unity | NUnit da Unity é antigo: `Does.Contain` só aceita texto | `Has.Member` para coleções |
| `dotnet test` trava / dll presa | `testhost` de uma execução anterior | encerrar o `testhost` |
| Ficha concluída sem movimentos | foto posta antes de congelar os módulos | montar a foto inteira antes de atribuir |
| String C# quebrada | heredoc do Bash come `\` | patch em arquivo `.py` no scratchpad |
| Imagem não carrega por caminho | fora de uma pasta `Resources` | `Resources/...` ou lista no Inspector |
| Texto de regra copiado | repo é **público** | texto próprio sempre; livro/acervo só como consulta |
| Ícone de terceiros no repo | licença (ex.: Icons8) | fora do repositório (`.gitignore`) |

## MVP (do `CLAUDE.md`)

| Item | Estado |
|---|---|
| Tela de entrada → partida | de pé: Jogadores, Personagens e **Salas** (criar, vagas, NPCs) |
| Criar personagem | **de pé** (laboratório e revisão na sala, com pendências) |
| Mesa com N fichas clicáveis | **Core pronto**, tela falta (só o botão "Área do mestre") |
| Pilha de Equipe | falta |
| Mão de cartas por jogador | falta |
| Rolador 2d6 + log | **log pronto no Core**; rolador falta |
| Exportar log para o fórum | falta |

Fora do MVP, já de pé: jogadores; NPCs com ficha de vilão; fotos por link com aprovação; retratos.

## Schema da ficha

- Gravado em JSON por `ArmazenamentoLocalJson` (`persistentDataPath/dados/`): `jogadores.json`,
  `personagens.json` (laboratório, instâncias e NPCs juntos), `salas.json` e `mesa_<sala>.json`.
- **Ainda sem campo `versao` nem migração.** Na v0.0.2 o status `criado` deixou de existir; os dados
  locais do Alan foram convertidos à mão (backup em `dados/personagens.antes-dos-status.json`).
  Antes de uma campanha de verdade, criar `versao` + migração.
