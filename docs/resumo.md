# Resumo — onde a Torre está

> Handoff para a próxima conversa. Atualizado no fechamento da **v0.4.0** (2026-10-19).
> Se algo aqui contradiz o código, o código manda — e este arquivo precisa de poda.

## Leitura para retomar

Leia este resumo junto com [regras_masks.md](regras_masks.md): entendimento do sistema,
cuidados de interpretação e direção dos painéis de ajuda, em palavras próprias.
As regras executáveis continuam em `Assets/Resources/Regras/*.json`; consulte também
`CLAUDE.md`, `regras.md`, `fluxo_de_telas.md` e os achados dos relatórios antes de alterar regras.

**Duas IAs na mesma pasta:** esta (mecânica: regras, fichas, salas, mesa, rolador) e outra (cartas de ajuda e os
dados 3D). Cada uma commita o seu. Antes de commitar, `git status` e ler o diff do que não é seu; não assumir que
arquivo novo é seu. Arquivo misto (as duas mexeram): pôr no índice só os seus trechos (script
`so_meus_trechos.py`, recriar no scratchpad: `git diff -U0`, tirar os trechos dela, `git apply --cached --unidiff-zero`).

## Estado

- **Versão:** v0.4.0 — *O MJ decide a ordem* (`docs/relatorio_v0.4.0.md`). Antes: v0.3.0 (*O primeiro movimento*:
  o Enfrentar), v0.2.2, v0.2.1, v0.2.0 (*A mesa*).
- **O que organiza o que vem:** o que uma rolagem pede vira **pendência no log** (botão), nunca janela que abre
  sozinha: a condição do vilão interrompe a mesa com o movimento dela, e só o MJ sabe a ordem. Hoje há três:
  golpe pedido pela troca (`Mesa.GolpesPedidos`), movimento de condição do vilão (`CondicaoPedida` /
  `ResolverMovimentoDeCondicao`), pedido pelo console (`PedirRolagem` / `PedidoAberto`, atalho no JSON).
  A rolagem nova guarda `origem`; desfazer devolve a pendência.
- **Movimentos conferidos:** Enfrentar (v0.3.0) e Tomar um golpe poderoso (v0.4.0). O molde: regra no JSON do
  movimento (`movimentos_basicos.json`, com `_decisao` e página), lida por `Core/Regras/MovimentosBasicos.cs`;
  opções com `efeitos` (`marcar_condicao` com `n`, `escolher` dentro de opção, `mj_decide`/`quem_rolou_decide` para
  alternativas, `lembrete_na_oposicao`), `atalho` do console, `invertido`, `sem_intervencao`.
- **Como o Alan trabalha esta fase:** ele **desenha cada tela** e explica a regra; não adiantar conteúdo nem regra
  antes do desenho. Quando ele diz "não implemente, só leia", é só leitura.
- **Validação pelo livro:** a ideia do Alan é reproduzir os exemplos do livro na Torre com "Rolar fixo" e ver se bate.
  Ele criou a campanha **Pacatópolis** para isso (fichas ainda por criar). Arquétipos da equipe dos exemplos: Huma
  (Legado), Toro (Bruta), Crepúsculo (Amaldiçoada), Vespa (Discípula), Gafanhoto (Janus), Tiro-Certo (Farol), Rex
  (Transformado); Vértice (Nova) e Canção dos Céus (Forasteira) **segundo o Alan**, o texto não diz. Não há fichas
  publicadas; o "Rolar fixo" escolhe a soma dos dados, então basta o arquétipo e o estado da cena.
- **O caminho da ficha** (inalterado):

  ```
  laboratório:  novo ⇄ em aprovação
  sala:         em aprovação → em carreira → aposentado (sem volta)
                em aprovação e em carreira → em revisão (botão na aba Mestre) → em aprovação
  ```

- **Princípio:** a Torre é só do mestre, mas se comporta como se um dia fosse pública.

## Onde eu parei

- **Próximo (combinado com o Alan): Cena e Spectate.** Desenho dele: quem está em cena (assentos) e quem assiste
  (caixa "Spectate" no canto superior esquerdo, com o motivo: em outra cena, afastou-se, nocauteada, fugiu). Criar
  cena registra quem está e quem assiste (sugere: a cena anterior + quem saiu nocauteada, que volta na seguinte).
  **O MJ move qualquer uma para dentro e para fora à vontade** (exigência do Alan); entrar também pode gastar o
  controle do "Chegar na hora certa" (Bruta). Saídas pela regra no Aceitar: golpe 10+ "afastar-se", precisar marcar
  condição sem ter livre. O painel de rolagem e a intervenção passam a oferecer só quem está em cena (p.84). Hoje "sai
  de cena" é só texto no log; NPCs já têm `em_cena` na mesa. Perguntas em aberto: a falha só tira de cena pelo botão
  manual (movimento do MJ)? O Alan vai desenhar a janela de criar cena?
- **Depois:** Liberar seus poderes (o par do Enfrentar; o 7–9 já pede a condição), Defender (proteger alguém + a
  interrupção de PvP, hoje só intervenção −2) e o ganho de controle do "Chegar na hora certa" ("hold 1 instead of
  choosing", preso à pessoa defendida; o custo do 7–9 continua). Depois, patches de arquétipo um movimento por vez
  (ex.: Legado rolar Superior no Enfrentar).
- **Modificador** (`Mesa.PartesDoModificador`): Rótulo + condições −2 já entram. Faltam adiante, constante e
  Influência passiva +1 (p.81).
- **Atalho para os movimentos do MJ no help** (pedido do Alan, "parece rápido"): ainda não feito. `PainelAjuda` tem
  `AbrirCategoria("movimentos-mj")`.
- **Influência na mesa**: "descontar num colega" e a renúncia são só marca narrativa. `DefinirEstadoDaInfluencia`
  está no Core, sem tela e sem evento.
- **Console** (`Core/Mesa/ConsoleDaMesa.cs`): `/eda`, `/tgp`, `/cena`, `/tempo`, `/salvar` (a tela grava: o Core só
  monta o texto), `/avancar`, `/fixo`, `/reset` (a tela pede confirmação). Nome com espaço vai entre aspas; nome de
  uma palavra ou primeiro nome único dispensam; se nenhum nome começa com o texto, vale o começo de qualquer palavra.
- **Dados fixos:** `Mesa.FixarDados` / `TirarDadoFixo` (fila em memória, não gravada). `PainelMesa.JogarDados` (da
  outra IA) anima os dados 3D até o valor pedido. Botão "Rolar fixo" no painel (`AbrirRolarFixo`).
- **Cartas e help**: `Regras/cartas.json` + `Ajuda/catalogo.json`, desenhadas por `DesenhoDeCarta`; não se fabricam
  PNGs de cartas (decisão de 2026-10-15). Faltam categorias descobertas pelo catálogo, "nova carta" (Adulto), "?" por
  chip de movimento (`PainelAjuda.AbrirMovimentos(id, movimento)` pronto, sem ligação). Movimentos de arquétipo de
  todos os arquétipos já estão no catálogo (trabalho da outra IA, conferido com Fichas Exemplo e livro).
- **Pendente de decisão do Alan:** os papéis das NPCs novas de histórico (Amaldiçoada: aliado; Discípula: neutro);
  a regra da casa da Forasteira ("Entre dois mundos"), trocada pela leitura do livro.
- **Validação por arquétipo:** passaram Farol, Amaldiçoada, Janus, Nova, Forasteira; Bruta e Delinquente sem
  validação; Discípula, Legado e Transformado sem revisão.
- **Anotados para depois:** aposentar no 15º avanço; ajuste pontual com motivo em ficha em carreira; resultado sem
  rolagem (Sobrecarga 10+, Vou te salvar! 12+); Influência oculta e de nascença; o vilão derrotado na mesa;
  post BBCode; o exemplo `docs/exemplo_primeira_luta.md` revisado só até "Aurora sai dos escombros".
- **Sons:** `Assets/audio` no `.gitignore` até confirmar a origem.
- **Testes:** `Assets/Tests/EditMode/` — 197 passando no harness dotnet fora da Unity (scratchpad `testcore/t.csproj`:
  Core + testes + Newtonsoft + NUnit, com os JSON de Regras ao lado). Rodar também no Test Runner da Unity. A outra
  IA tem testes próprios em `Assets/Tests/Dice/`.

## Armadilhas

| Sintoma | Causa | Saída |
|---|---|---|
| Campo de texto não edita | `TMP_InputField` cria o cursor no `OnEnable` | montar desligado, ligar depois |
| Lista espremida / cabeçalho gigante | `HorizontalLayoutGroup` com `childForceExpandHeight` | `LayoutElement.flexibleHeight = 0` |
| Coluna fixa esticando e espremendo as outras | herda `flexibleWidth` de um filho | `LayoutElement` com `flexibleWidth = 0`, prioridade 2 |
| Contorno sai cheio | `Outline` sob fundo translúcido | borda com 4 filetes |
| Glifo vira quadrado | fonte sem ▾ | "›" girado |
| Colunas desalinhadas | empate de `layoutPriority` com TMP | `ColunaElastica` com prioridade 2 |
| Tab perde o foco / edição desfaz a anterior | redesenho após edição; callback com dado velho | não redesenhar em edição ok; ler na hora |
| Rolagem volta ao topo | aba refeita | `MontarConteudo(manterRolagem: true)` |
| Roda do mouse não rola | sem raycast nos vãos; TMP multilinha engole | Image transparente; `RepassaRolagem` |
| Ficha abre em branco | `AddComponent<Image>` num objeto que já tem TMP vem **nulo** | um gráfico por objeto; `Logs/Editor.log` |
| Compila "verde" mas a tela quebra | `tools/compilar.sh` só prova tipos | conferir o `Logs/Editor.log` da Unity |
| `compilar.sh` acusa arquivo fora (meu ou da outra IA) | `.csproj` só se regenera com a Unity aberta (Ctrl+R) | compilar por cópias `_v_` com glob da pasta (`vcsproj.py` no scratchpad: Core, Infra, UI e Dados=`Dice`), apagar depois |
| Teste passa no harness e falha na Unity | NUnit da Unity é antigo: `Does.Contain` só aceita texto | `Has.Member` para coleções |
| `dotnet test` trava / dll presa | `testhost` de uma execução anterior | encerrar o `testhost` |
| String C# quebrada / comando travado | heredoc do Bash come `\`; `cat >` sem entrada espera para sempre | patch em arquivo `.py` escrito com a ferramenta de arquivo |
| **Busca-e-troca corrompeu PNG** | `grep -rl` sem `-I` lista binários e o `sed` reescreve | `grep -rlI` (só texto); conferir `git status` depois |
| Busca com acento não acha | `grep -i` com `[aá]` falha no locale do Git Bash | sem `-i`, as duas grafias, ou Python |
| Python não imprime acento | `-I` ignora `PYTHONIOENCODING`; console em cp1252 | `sys.stdout.reconfigure(encoding='utf-8')` |
| Imagem não carrega por caminho | fora de uma pasta `Resources` | `Resources/...` ou lista no Inspector |
| Ficha em branco ao abrir (2º Play) | Play sem domain reload: cache estático com textura destruída | limpar estáticos com `[RuntimeInitializeOnLoadMethod]` |
| Engasgo a cada clique | aba refeita + arquivo gravado por clique | `SecaoRefazivel` + `ArmazenamentoAdiado` |
| Recompilação lenta / "não pegou" / carta nova não aparece | Auto Refresh desligado (duas IAs editando) | **Ctrl+R** na Unity antes de procurar bug |
| Direção da Influência trocada | "dar a X" = X tem Influência sobre quem deu | quem **recebe** tem o poder |
| Texto de regra copiado / ícone de terceiros | repo é **público** | texto próprio sempre; ícones fora do repo |
| Caixa de texto cresce com o texto | `TMP_InputField` também é `ILayoutElement` | `LayoutElement.layoutPriority = 2` (`AreaTexto`) |
| Rótulo vaza do botão | `Encolher` põe numa linha só, mínimo 12; frase longa não cabe | opção do resultado: `TextoDeOpcao` (duas linhas, mín. 11); botão `TextoLivre` fica fora do `LateUpdate` |
| Texto fixo espremido numa seção que cresce | seção de altura fixa com itens empilhados | explicação em `Dica` (flutuante); lista longa em `ListaRolavel` |
| CS0136 no painel | `DesenharRolagem` é enorme: nome de variável repetido em escopo aninhado | nomes específicos (`somaDasPartes`, `deDentro`) |
| Enum e constante com o mesmo nome | `const string Controle` dentro de `Mesa` esconde o enum | prefixar (`BonusControle`) |
| Gravar nos dados com a Unity aberta | a gravação adiada sobrescreve | Unity fechada, ou arquivo na pasta `importar` |
| Testar o armazenamento real | o assembly de testes não referencia o Infra | harness à parte (scratchpad `testinfra`) |
| **Janela que abre sozinha** | auto-trigger do que a rolagem pede | pendência no log; o MJ decide a ordem (memória do projeto) |
| Narrar o que a ficha não tem / regra de cabeça | inventei habilidade; "egoísta = +1" | ler a ficha e conferir no livro antes de escrever |
| Pilha "zerando" | leitura errada | a pilha **soma** sempre; só "o tempo passa" volta para 1 (p.88); cena nova não mexe |
| Mapeamento de NPC/lugar errado | lista pronta sem conferir a pergunta | conferir no livro; pergunta com "quem" = pessoa = NPC |
| Exemplo do livro lido pela metade | a p.189 tem duas cenas | ler a cena inteira antes de concluir |
| PT ambíguo | "quando a pessoa estiver em perigo" | conferir no PDF inglês do acervo (`Livros/Masks-ENG-Arquetipos.pdf`, `pypdf`) |
| Nome do movimento no livro não acha | o nome é nosso (texto próprio) | procurar pela regra, não pelo nome |
| Pedido entendido como entidade nova | "Herói genérico" virou arquétipo | perguntar o lugar na tela antes de criar entidade |
| Log ambíguo | "retorno: ileso" sem dizer de quem | escrever sempre o nome de quem recebe |
| Teste antigo quebra com regra nova | rolagem sem alvo, +1 Equipe contra PJ | atualizar o teste para a regra |
| Dados físicos escondidos | a caixa do painel por cima | fechar a caixa antes de jogar e reabrir com o resultado |
| Commit levou arquivo de outra frente | `git commit` grava o índice inteiro | `git status` antes; arquivo misto por trecho |
| Valor no prefab parece churn | ajuste no Inspector junto com a reordenação da Unity | comparar com o padrão do script antes de chamar de churn |

## MVP (do `CLAUDE.md`)

| Item | Estado |
|---|---|
| Tela de entrada → partida | **de pé** |
| Criar personagem | **de pé** (laboratório e revisão na sala) |
| Mesa com N fichas clicáveis | **de pé**; com 9 heroínas falta o modo cena/Spectate |
| Pilha de Equipe | **de pé** |
| Mão de cartas por jogador | falta (base no Core: `CatalogoDeCartas` + `MontadorDeBaralho`) |
| Rolador 2d6 + log | **de pé para Enfrentar e golpe poderoso**, com condições na conta, pendências e dados fixos; outros movimentos faltam |
| Exportar log para o fórum | **parcial**: `/salvar` grava o log em .txt e copia; falta o post BBCode da colheita |

Fora do MVP, já de pé: jogadores; NPCs (laboratório, ficha de vilão, os que nascem da ficha); Influência PJ↔NPC;
Cenário; relacionamentos; help com cartas desenhadas e editor de cartas; fotos por link com aprovação;
exportar/importar ficha; console do mestre; dados 3D com animação dirigida (outra IA).

## Schema da ficha

- Gravado em JSON por `ArmazenamentoLocalJson` (`persistentDataPath/dados/`): `jogadores.json`, `personagens.json`,
  `salas.json`, `mesa_<sala>.json`. Exportações em `dados/exportacoes` (inclui `log_<sala>_<data>.txt`).
- v0.4.0 (todos opcionais, sem migração; evento antigo continua valendo): na rolagem, `partes` (do modificador),
  `origem`; nas escolhas, `condicoes`, `faltaram_condicoes`, `dentro`, `para`; no evento, `pedido` (movimento, alvo)
  e `resolve` (a pendência de condição do vilão). Lembretes da abertura são `bonus` do tipo lembrete.
- v0.3.0: eventos com `oculto`, `editado` e `rolagem`. v0.2.0: `mesa_<sala>.json` com `equipe`, `em_cena`,
  `condicoes_de_vilao`, `bonus`. Formatos de troca próprios: `torre-de-controle/npcs`, `cenario`, `anotacoes`,
  `vinculos` (versão 1).
- **Os arquivos têm o campo `versao`, mas ainda não há migração** (grava o número; ao carregar, não converte nem
  recusa). Antes de uma campanha de verdade, criar a migração.
