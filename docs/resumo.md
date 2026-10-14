# Resumo — onde a Torre está

> Handoff para a próxima conversa. Atualizado no fechamento da **v0.0.4** (2026-10-14).
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

- **Versão:** v0.0.4 — *A equipe se forma na sala*. Relatório: `docs/relatorio_v0.0.4.md`.
- **Onde o Alan chegou:** uma sala com 5 personagens **em aprovação**, com a **formação da equipe** e a
  **Influência inicial** prontas. A trava de em carreira (`PendenciasParaCarreira`) cobra os dois; faltam
  relacionamentos e os NPCs dos jogadores.
- **Como o Alan trabalha esta fase:** ele **desenha cada tela** (rascunho colorido) e explica a regra.
  Não adiantar conteúdo nem regra antes do desenho (ele já parou isso duas vezes).
- **O caminho da ficha**, e cada ação depende de onde ela está (e de onde foi aberta: hierarquia):

  ```
  laboratório:  novo ⇄ em aprovação
  sala:         em aprovação → em carreira → aposentado (sem volta)
                em aprovação e em carreira → em revisão (botão na aba Mestre) → em aprovação
  ```

  Pôr na vaga **copia** a ficha (a original fica no laboratório). Do laboratório, a instância é só
  consulta. Em carreira, o jogo muda por evento da **mesa** (Core pronto, sem tela).
- **Troca com outras máquinas:** exportar/importar JSON (lista = personagem nova; ficha = por cima da
  mesma). Ida e volta conferida pelo Alan.
- **Princípio:** a Torre é só do mestre, mas se comporta como se um dia fosse pública (conteúdo do
  jogador passa por moderação: fotos por link com aprovar/recusar).

## Onde eu parei

- **Help básico (2026-10-14):** gerador próprio em `tools/cards/gerar_cartas.py`,
  instruções em `tools/cards/README.md`. Gera os quatro panoramas `00-*` em
  `Assets/img/cards/regras`, com texto próprio e moldura desenhada por código,
  sem depender do acervo. Ainda não ligado ao menu de ajuda.

- **Aprovação na campanha** — feitos: **formação da equipe** (aba da sala, fila da sala) e **Influência**
  (jogador marca na ficha, Núcleo; aba da sala = visão do mestre; `SeletorDeInfluencia`; por colega,
  dada/perdida). Faltam **relacionamentos** (a Bruta liga amor e rival a eles) e os **NPCs que orbitam**
  cada personagem, criados pelos jogadores. **Esperar o Alan desenhar.** Detalhes em `fluxo_de_telas.md`.
- **Influência no jogo** — `DefinirEstadoDaInfluencia` (dar nova, perder, voltar; só em carreira) está no
  Core, sem tela e sem evento no log: vai para o painel do mestre (a aba Influência da sala).
- **Área do mestre** — o botão está na sala (`PainelSala.AreaDoMestre`, só avisa "em construção"). O
  Core está pronto em `Core/Mesa/Mesa.cs` (rodada, eventos, potencial, condições, Rótulos com estouro,
  corrigir). Falta a tela: cartões das personagens em carreira, comandos com motivo, log da rodada.
- **Validação por arquétipo**, guiada pelo Alan: passaram Farol, Amaldiçoada, Janus, Nova, Forasteira;
  Bruta e Delinquente sem validação; Discípula, Legado e Transformado sem revisão. Esperar o Alan.
- Anotados para depois: aposentar no 15º avanço; ajuste do mestre em ficha em carreira
  (`implementacao_futura.md`).
- **Ajuste pontual com motivo** em ficha em carreira (sem reabrir a criação) e **resultado sem rolagem**
  (Sobrecarga 10+, Vou te salvar! 12+) para o rolador: anotados.
- Testes: `Assets/Tests/EditMode/` — 104 passando no harness dotnet fora da Unity; rodar também no Test
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
| Ficha em branco ao abrir (2º Play) | Play sem domain reload: cache estático com textura destruída | limpar estáticos com `[RuntimeInitializeOnLoadMethod]` |
| Texto digitado some | gravava só ao sair do campo | `onValueChanged` (barato com a gravação adiada) |
| "Textos trocados" nas descrições | caixas na ordem do JSON, jogador preenche na ordem que marcou | caixas na ordem da marcação |
| Engasgo a cada clique | aba inteira refeita + arquivo gravado por clique | `SecaoRefazivel` + `ArmazenamentoAdiado` |
| Recompilação de 6–16 s ao voltar à Unity | Auto Refresh com duas IAs editando | desligar Auto Refresh, Ctrl+R quando quiser |
| Direção da Influência trocada | "dar a X" = X tem Influência sobre quem deu | quem **recebe** tem o poder; reversa = "ela exerce Influência sobre" |
| Chips que deixam passar do limite | só o Core recusava, a tela deixava | travar os que sobram no limite (como as habilidades) |
| Botão/coluna gigante | coluna fixa herda "esticável" de dentro | `LayoutElement`: `flexibleWidth = 0`, prioridade 2 |
| `dotnet test` "sem resultado" | saída em inglês ("Passed!") | filtrar `Passed!|Failed!` também |
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

Fora do MVP, já de pé: jogadores; NPCs com ficha de vilão; fotos por link com aprovação; retratos;
exportar/importar a ficha em JSON (troca com outras máquinas).

## Schema da ficha

- Gravado em JSON por `ArmazenamentoLocalJson` (`persistentDataPath/dados/`): `jogadores.json`,
  `personagens.json` (laboratório, instâncias e NPCs juntos), `salas.json` e `mesa_<sala>.json`.
- **Ainda sem campo `versao` nem migração.** Na v0.0.2 o status `criado` deixou de existir; os dados
  locais do Alan foram convertidos à mão (backup em `dados/personagens.antes-dos-status.json`).
  Antes de uma campanha de verdade, criar `versao` + migração.
