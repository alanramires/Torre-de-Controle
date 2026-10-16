# Resumo — onde a Torre está

> Handoff para a próxima conversa. Atualizado no fechamento da **v0.2.1** (2026-10-16).
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

- **Versão:** v0.2.1 — *Ponto a ponto*. Relatórios: `docs/relatorio_v0.2.0.md` (a mesa) e
  `docs/relatorio_v0.2.1.md` (a mesa testada lance a lance: trilhas, Refazer, Lembretes, moedas de Influência,
  atalhos, e as correções de regra de ajuda, egoísta, ceder terreno e combinar movimentos).
- **Novo nesta versão:** a **mesa de jogo** ("Entrar na sala" na entrada → `PainelMesa`): assentos, log,
  pilha de Equipe (Gerir pilha: ±1, usar, questionário de entrar em batalha, passagem do tempo), vilões em
  cena e bônus (adiante/constante/controle). E a **Área do mestre** (`PainelMestre`): primeira sessão,
  anotações, vilões em cena, potencial.
- **O que organiza o que vem:** escrever o **exemplo comentado** antes do código (`docs/exemplo_primeira_luta.md`,
  blocos [TORRE]) mostrou quais botões a sessão pede. O próximo corte é o **rolador**, o lance seguinte do
  exemplo. Caso de teste do produto: `docs/Case de Teste/` (Halcyon City).
- **Onde o Alan chegou:** salas com personagens **em aprovação** (formação, Influência e relacionamentos
  prontos; NPCs nascem da ficha). Agora há o **laboratório de NPCs** (botão Npcs na entrada): originais,
  cópia entre campanhas, exportar/importar, foto, papel "Vilão / Oponente" e a ficha de vilão conferida no
  livro. Exemplos nos dados locais do Alan: Professora, Rosa Rook, Sentinel, Eclipse.
- **Pendente de decisão do Alan:** a outra IA trocou a regra da casa da Forasteira ("Entre dois mundos")
  pela leitura do livro em `arquetipos.json`; confirmar se a regra da casa sai.
- **O que organiza o que vem:** regra que gera NPC ou cenário é **dado** no `arquetipos.json` (`npcs`,
  `npcs_historico`, `por_escolha` com `opcoes`, `equipe.npc`; `cenario` no lugar de papel; `cenario` nos
  extras com nome automático e `dica_nome`; `npc_se` + `senao_cenario`), não código.
- **Regra de mapear:** conferir a pergunta no livro antes de dizer que ela dá NPC ou lugar (o Farol pede
  *sinais*, não lugar: o mapeamento errado saiu na v0.1.0).
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

- **Cartas:** `Regras/cartas.json` (jogo) + `Ajuda/catalogo.json` (texto do help), desenhadas por
  `DesenhoDeCarta`; help em `PainelAjuda` (cortina, dois níveis nos arquétipos, filtro); Gestão das cartas
  = help em modo edição (`PainelEditorDeCarta`, grava via `Infra/ArquivosDoProjeto`, só no Editor).
  **Não se fabricam mais PNGs de cartas** (decisão no fim deste arquivo). Falta: categorias descobertas pelo
  catálogo, botão "nova carta" (Adulto), "?" por chip de movimento.
- **Cenário:** `Cenario.cs`, `CadastroPersonagens.Cenario.cs` (`FontesDeCenario`, sincronização junto com a
  dos NPCs), `CadastroPersonagens.TrocaCenario.cs`, aba em `PainelSala.SecaoCenario`. Anotados: copiar um
  elemento para outra campanha; laboratório de cenário (botão "NPCs e cenário", não um sexto botão);
  cidade acima das campanhas; Legado (linhagem → Organização) e Janus (emprego/escola → Local) em "talvez".
  "Evento" não é cenário: vai para o log/Diário.

- **Aprovação na campanha** — feitos: formação da equipe, Influência e **relacionamentos** (ficha, aba
  História; sala só leitura; Bruta sincroniza amor/rival com a Influência). **NPCs da ficha** feitos em
  `CadastroPersonagens.Pnj.cs` (`FontesDeNpc`, `SincronizarNpcsDaFicha`, `OrigemDoNpc`): nêmesis, mentora,
  Legado, histórico de 7 arquétipos, obrigações do Janus, 3 impulsos do Farol. Em aprovação, nome e
  descrição de NPC seguem editáveis (decisão do Alan: ajudar o mestre). **Ainda não** entram na trava.
- **Laboratório de NPCs** — feito: `CadastroPersonagens.Pnj.cs` (`NpcsDoLaboratorio`, `CopiarNpc`,
  `NomeRepetidoNoDestino`), `CadastroPersonagens.TrocaNpc.cs` (formato `torre-de-controle/npcs`),
  `PainelNpcs.cs` (tela), `PainelNpc.cs` (ficha: foto, papel/maturidade, exportar/importar por cima).
- **Influência PJ↔NPC** feita (ficha do NPC, ficha da heroína e árvore com "(NPC)"). Falta, de
  `docs/npc_lab.md`: Influência oculta e a de nascença pela regra. E o vilão na mesa: marcar condição na
  luta, derrotado, lembrete dos movimentos de condição (depende da Área do mestre).
- **Help contextual:** `PainelAjuda.TemMovimentos(id)` / `AbrirMovimentos(id, movimento)`; o "?" por chip
  de movimento tem o parâmetro pronto, sem ligação.
- **Sons:** código versionado (`SonsUI`, `SomBotao`); `Assets/audio` no `.gitignore` até confirmar a origem.
- **NPCs de fichas antigas** nasceram com o texto inteiro no nome (Estrela Negra, Aurora, a Discípula):
  o Alan separa nome e descrição à mão.
- **Influência no jogo** — `DefinirEstadoDaInfluencia` (dar nova, perder, voltar; só em carreira) está no
  Core, sem tela e sem evento no log: vai para o painel do mestre (a aba Influência da sala).
- **Mesa** — `UI/PainelMesa.cs` + `Core/Mesa/Mesa.cs` (eventos com desfazer e refazer; pilha, condições de vilão,
  bônus e lembretes são da mesa, `mesa_<sala>.json`; trilhas e potencial vão para a ficha). O exemplo
  `docs/exemplo_primeira_luta.md` é o roteiro de teste: revisado até "Aurora sai dos escombros"; dali em diante
  (Estrela provoca, depois da luta) falta revisar com o Alan. Falta: **rolador**, com as regras certas de ajuda
  (1 por colega que alcança), egoísta (a mesa confirma + quais Rótulos mudam) e golpe poderoso,
  Influência mudando Rótulos pela mesa (com Rejeitar), roteiro marcando sozinho pelo log, post BBCode,
  mudar Rótulos ao agir de forma egoísta (hoje manual).
- **Área do mestre** — `UI/PainelMestre.cs`: Primeira sessão (`Regras/primeira_sessao.json`,
  `Core/Salas/PrimeiraSessao.cs`), Anotações (na sala: `AnotacoesFormacao`, `AnotacoesNpcs`, `Trama`),
  e, abertas pela mesa, Vilões em cena e Potencial. Trama → ganchos e arcos (livro cap. 9) depois.
- **Validação por arquétipo**, guiada pelo Alan: passaram Farol, Amaldiçoada, Janus, Nova, Forasteira;
  Bruta e Delinquente sem validação; Discípula, Legado e Transformado sem revisão. Esperar o Alan.
- Anotados para depois: aposentar no 15º avanço; ajuste do mestre em ficha em carreira
  (`implementacao_futura.md`).
- **Ajuste pontual com motivo** em ficha em carreira (sem reabrir a criação) e **resultado sem rolagem**
  (Sobrecarga 10+, Vou te salvar! 12+) para o rolador: anotados.
- Testes: `Assets/Tests/EditMode/` — 163 passando no harness dotnet fora da Unity; rodar também no Test
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
| Caixa de texto "expande" às vezes | campo de uma linha com texto colado com quebras vaza | `AreaTexto` (várias linhas, rola dentro) |
| NPC duplicado ao mudar a chave do vínculo | busca nova não acha o gravado no formato antigo | adotar o antigo no mesmo passo (`AcertarVinculosSemCampo`) |
| Mudança de formato "não pegou" | o Unity só recompilou depois | conferir no Play mode depois do reload, antes de mexer à mão |
| Em aprovação, chip parece clicável e não funciona | tela destravou o extra inteiro, o Core só o campo | destravar por **campo** (`ExtraEditavelNaSala(p, extra, campo)`) |
| Desmarcar opção não mexe no NPC | sincronização só no texto | sincronizar também em `AlternarOpcaoDeExtra` |
| Heredoc do Bash falha com aspas | "unexpected EOF" no Git Bash do Windows | script `.py` escrito com a ferramenta de arquivo |
| NPC original aparece entre as heroínas | "laboratório" era "sem sala" | filtrar também por `Controle` |
| Conta de vida do vilão | "3 condições = cai no 3º" é errado | derrotado quando **precisa** marcar e não tem: com 3, cai no 4º |
| Gravar nos dados com a Unity aberta | a gravação adiada sobrescreve | Unity fechada, ou arquivo na pasta `importar` |
| Link de foto não aparece | a Unity só desenha jpg/png (outro formato vira 8x8 "?") | aviso + abrir no navegador; aprovar mesmo assim |
| PDF "baixado" sem texto | impresso por site de download, perdeu a camada de texto | não serve de fonte; conferir no livro |
| NRE em `AddComponent` depois de `GetComponent ?? ...` | "fake null" da Unity: `??` não vê o objeto destruído/ausente | `TryGetComponent` |
| NRE em `GetPreferredValues` | TMP em objeto inativo não mede | estimar a largura (`LarguraEstimada`) |
| Carta nova "não aparece" | Auto Refresh desligado, JSON velho em memória | Ctrl+R antes de procurar bug |
| Commit levou arquivo de outra frente | `git commit` grava o índice inteiro (havia `D` já staged) | `git status` antes; refazer se ainda não houve push |
| Caixa de texto cresce com o texto | `TMP_InputField` também é `ILayoutElement`: empate de prioridade, vence a maior | `LayoutElement.layoutPriority = 2` (`AreaTexto`) |
| Rótulo vaza do botão | ajuste feito botão a botão, alguns escapam | ajustar todos ao redesenhar (`PainelMesa.LateUpdate`), exceto texto longo marcado |
| Enum e constante com o mesmo nome | `const string Controle` dentro de `Mesa` esconde o enum `Controle` | prefixar (`BonusControle`) |
| Narrar o que a ficha não tem | inventei habilidade ("enxerga no escuro") | ler a ficha antes de narrar; usar o que ela tem (Visões sombrias) |
| Explicar regra de cabeça | "egoísta = +1", "ajuda de quem está longe" | conferir no livro (p.84–85) antes de escrever o exemplo |
| Pilha "zerando" no questionário | leitura errada da regra | a pilha **soma** sempre; só "o tempo passa" volta para 1 (pp.86–88) |
| Mapeamento de NPC/lugar errado | lista pronta sem conferir a pergunta (Farol) | conferir no livro PT-BR e inglês |

## MVP (do `CLAUDE.md`)

| Item | Estado |
|---|---|
| Tela de entrada → partida | **de pé**: "Entrar na sala" abre a mesa da campanha pronta (ou lista) |
| Criar personagem | **de pé** (laboratório e revisão na sala, com pendências) |
| Mesa com N fichas clicáveis | **de pé**: assentos com Rótulos, condições, potencial, bônus; ficha no clique |
| Pilha de Equipe | **de pé**: Gerir pilha (±1, usar, questionário, passagem do tempo) |
| Mão de cartas por jogador | falta |
| Rolador 2d6 + log | **log de pé** na mesa; rolador é o próximo corte |
| Exportar log para o fórum | falta |

Fora do MVP, já de pé: jogadores; NPCs com ficha de vilão (Neutro/Aliado/Vilão / Oponente), **NPCs que
nascem da ficha** e o **laboratório de NPCs** (cópia entre campanhas, troca em JSON); Influência PJ↔NPC;
**Cenário** da campanha; relacionamentos; **help com as cartas desenhadas** e editor de cartas; fotos por
link com aprovação; retratos; exportar/importar a ficha em JSON (troca com outras máquinas).
A "mão de cartas" tem base: `CatalogoDeCartas` + `MontadorDeBaralho` no Core, sem tela de mesa.

## Schema da ficha

- Gravado em JSON por `ArmazenamentoLocalJson` (`persistentDataPath/dados/`): `jogadores.json`,
  `personagens.json` (laboratório, instâncias e NPCs juntos), `salas.json` e `mesa_<sala>.json`.
- Campos novos desta versão (sem migração, com conversão ao carregar quando preciso): `relacoes`
  (era `relacionamentos`), `nomes_historico`, `pnj.vinculo` (personagem, extra, campo, vaga),
  `pnj.maturidade_a_definir`; NPC com papel `heroi` vira `aliado` ao carregar.
- v0.0.6: NPC sem sala = original do laboratório de NPCs; troca de NPCs com formato próprio
  (`torre-de-controle/npcs`, versão 1). Foto do NPC usa `foto_heroica`.
- v0.1.0: `personagens.json` ganha a lista `cenario` (elementos com `sala`, `tipo`, `vinculo`,
  `descricao_copiada`); `pnj.descricao_copiada`; `equipe_npc_nome`; textos do nome do lugar nos extras com
  campo `cenario`. Troca de cenário: formato `torre-de-controle/cenario`, versão 1.
- v0.2.0: `mesa_<sala>.json` ganha `equipe`, `em_cena`, `condicoes_de_vilao`, `bonus`; `salas.json` ganha
  `primeira_sessao`, `anotacoes_formacao`, `anotacoes_npcs`, `trama`, `ultima_mesa`; `pnj.ligacoes`. Trocas
  novas: `torre-de-controle/anotacoes` e `torre-de-controle/vinculos` (versão 1).
- **Os arquivos têm o campo `versao`, mas ainda não há migração** (o número é gravado; ao carregar, não se
  converte nem se recusa versão diferente). Na v0.0.2 o status `criado` deixou de existir; os dados
  locais do Alan foram convertidos à mão (backup em `dados/personagens.antes-dos-status.json`).
  Antes de uma campanha de verdade, criar a migração (converter ou recusar versão diferente ao carregar).


Movimentos de arquétipo: Delinquente adicionado com seis cartas de borda vermelha,
conferidas com Fichas Exemplo/Delinquente e o livro (pp.110 e 113).
Textos em Ajuda/catalogo.json; referências para as seis opções de
arquetipos.json. A galeria e o help contextual da ficha usam os IDs existentes.
Amaldiçoada não tem movimentos comuns na criação; seus extras ficam pendentes.


Discípula: seis movimentos adicionados a Movimentos de arquétipo, com borda
vermelha e IDs do arquetipos.json. Conferidos com as seis cartas de
Fichas Exemplo/Discipula e o livro (pp.116 e 118–119). Recursos da mentora
ficam para os extras. O help contextual usa os parâmetros existentes.


Farol: seis movimentos adicionados a Movimentos de arquétipo, com borda
vermelha e IDs do arquetipos.json. Conferidos com Fichas Exemplo/Farol
e o livro (pp.122 e 124–125). Vamos, Sortudo! dá +1 quando o bicho ajuda
nos três movimentos escolhidos, sem ser adiante de uso único. Impulsos
ficam para os extras. O help contextual usa os parâmetros existentes.


Forasteira: seis movimentos em Movimentos de arquétipo, borda vermelha,
IDs de arquetipos.json. Conferidos com Fichas Exemplo/Forasteira e livro
(pp.128 e 130–131). Entre dois mundos explicita que a exigência do povo ocorre na falha;
não exige pagamento em todo contato nem bloqueia novo contato no texto do livro. Nave Kirby inclui as opções da nave.
O help contextual usa os parâmetros existentes.


Janus: seis movimentos em Movimentos de arquétipo, borda vermelha e IDs
de arquetipos.json. Conferidos com Fichas Exemplo/Janus e livro (pp.134
e 136–137). A Máscara é inicial obrigatório, mais dois escolhidos; troca
os valores dos Rótulos uma vez por sessão. Obrigações civis ficam para os extras.


Legado: seis movimentos em Movimentos de arquétipo, borda vermelha e IDs
de arquetipos.json. Conferidos com Fichas Exemplo/Legado e livro (pp.140
e 142–143). Símbolo de autoridade dá −1 adiante na falha (o PNG antigo
indicava +1 incorretamente). Nunca desista usa +Salvador e substitui o golpe
poderoso básico. O módulo do legado fica para os extras.


Nova: onze cartas (Chamas + dez faíscas) em Movimentos de arquétipo,
borda vermelha. Chamas explica a analogia com pontos de magia: carregar,
formar reserva, pagar custos e perder o restante no fim da cena. Conferidas
com Fichas Exemplo/Nova e livro (pp.146 e 148–149). Referências apontam
para extras de Nova, pois ele não escolhe movimentos comuns. IDs de help:
arquetipo/nova/chamas e arquetipo/nova/<id-da-faisca>.


Transformado: seis movimentos em Movimentos de arquétipo, borda vermelha
e IDs de arquetipos.json. Conferidos com Fichas Exemplo/Transformado e o
livro (pp.152 e 155). Eu não sou meu corpo reduz a contagem de condições
em duas, com mínimo zero; Eu sou o monstro exige complicações mesmo no
sucesso. Galeria e help contextual usam os parâmetros existentes.


## Decisão sobre cartas (2026-10-15)

Não fabricar nem regenerar PNGs de cartas a partir de agora. O novo sistema
desenha as cartas usando Assets/Resources/Ajuda/catalogo.json; textos novos
e correções entram nele. Tokens e ícones podem continuar sendo gerados como
artes separadas. Esta decisão substitui o fluxo antigo de geração de cartas.


## Perdição da Amaldiçoada (2026-10-15)

Onze cartas novas, somente em Ajuda/catalogo.json, categoria principal
perdicao: Nêmesis, Perdição, Sinais (visão geral), os cinco poderes,
Sua perdição chega, Santuário e Usar o santuário. Todas ligadas pelo
campo ajuda em Regras/cartas.json, origem amaldicoada (selo da classe),
tipo extra. Entradas existentes preservadas; criadas as faltantes para
os sinais e para o uso do santuário. Referências por IDs dos extras,
sem posições numéricas. Texto próprio conferido com livro pp.96,98–101
e Cards/Novas/Arquetipos/Amaldicoada (só consulta).

Começa com um Sinal; trilha de cinco casas rende outro. Poderes infinitos
concede habilidade, não movimento. O Sinal final e o avanço de enfrentar
a perdição são caminhos distintos. Requisitos do santuário não são
condições emocionais. Não foram gerados PNGs de cartas nem novos tokens.
Validado: IDs únicos, um vínculo de jogo por help, marcação e build UI.
Conferência visual no Play ainda depende do Alan.
