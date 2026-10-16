# Catálogo de help

> Decisão do Alan — 2026-10-15: parar de fabricar cartas como PNGs.
> A apresentação das cartas passa a ser desenhada pelo novo sistema a partir
> de `Assets/Resources/Ajuda/catalogo.json`. Novos textos e correções entram
> nesse catálogo, sem regenerar imagens de cartas. Tokens e ícones continuam
> podendo ser criados como artes separadas. As instruções do gerador abaixo
> descrevem o fluxo anterior; não executá-lo para novas cartas ou revisões.

Fonte única dos textos das quatro cartas de panorama, dos oito movimentos básicos,
das nove cartas PvP, das seis cartas de Influência, das duas de Condições
das seis de Equipe e sessão, das dez de Movimentos de equipe e dos seis
movimentos da Bruta e das doze cartas de Vilões (116 ao todo):
`Assets/Resources/Ajuda/catalogo.json` (UTF-8, texto próprio).
As regras executáveis continuam em `Assets/Resources/Regras/`; o catálogo
editorial não substitui essas regras nem os dados de jogadores, personagens e salas.

## Formato, versão 1

- `versao`: versão do formato do catálogo.
- `cartas`: lista ordenada das entradas de ajuda.
- `id`: chave estável, como `panorama/rotulos`; não usar o título como identidade.
- `categoria`: agrupamento da navegação: `referencia-rapida`, `movimentos-basicos`, `pvp`, `influencia`, `condicoes` ou `equipe-sessao`.
- `arquivo`: nome do PNG gerado, preservando os nomes `00-*` já usados pela UI.
- `titulo`: título exibido na carta.
- `compacto`: opção de composição para conteúdos com muitas seções.
- `borda`: cor hexadecimal opcional; altera somente o contorno da carta.
  Condições: amarelo; Influência: verde; Equipe e sessão: azul. Miolo preservado.
- `arquetipo`: agrupamento opcional dentro de Movimentos de arquétipo.
- `origem`: rodapé com as fontes/decisões consultadas.
- `regras_ref`: caminhos de referência relativos a `Regras/` ou `docs/` desde a
  raiz. Para JSON, aceita fragmento como `nucleo.json#/bonus`.
- `secoes`: pares `titulo` e `texto`, na ordem de leitura.

`regras_ref` fornece rastreabilidade, não valores substituídos automaticamente.
Ao corrigir uma regra, revisar o texto dos helps relacionados e regenerar.
Não manter explicações concorrentes dentro do Python. Para adicionar uma carta,
adicionar uma entrada ao catálogo com ID, fontes e texto próprios; ampliar a
navegação da Unity quando necessário. O painel lê títulos e arquivos do catálogo
nas categorias `referencia-rapida`, `movimentos-basicos`, `pvp`, `influencia`, `condicoes` e `equipe-sessao`; novas categorias ainda
precisam ser incluídas na lista de categorias do painel.

As variantes contra PJ ficam nas nove cartas PvP, conferidas com as cartas do
acervo `Cards/Novas/PvP`, `pvp.json`, `influencia.json` e o livro (consulta).
Os básicos não repetem as variantes de Enfrentar, Defender e Provocar.
Confortar e Perfurar mantêm seus efeitos gerais; a reação contra um colega em
Tomar um golpe poderoso permanece porque é parte do próprio movimento.
As correções de Liberar (condição OU efeito instável em 7–9) e Avaliar
(bônus enquanto usa as respostas) prevalecem sobre as cartas antigas.

Influência tem seis cartas: conceito, uso, renúncia, rejeição, NPCs e ganhar/perder. Conferidas
com `Cards/Influencia` do acervo, `influencia.json`, `nucleo.json` e o livro
(pp.79–83). Rejeitar usa 2d6 sem Rótulo, +1 por Influência recíproca e −2 por
Inseguro; a falha também rende potencial. Os textos reduzidos ficam no catálogo.

Condições tem duas cartas: efeitos/marcação e remoção. Conferidas com
`Cards/condicoes` e o livro (pp.89–91). Remoção por ação acontece no fim da
cena; a penalidade permanece até lá. Remoção por movimento segue seu efeito.
`condicoes-sessao.png` do acervo é conteúdo de Sessão, não de Condições.

Equipe e sessão reúne ajuda, uso egoísta, entrada em batalha, passagem de tempo,
gatilhos dos arquétipos e início/fim de sessão. Movimentos de equipe é outra
categoria principal (`movimentos-equipe`), com dez cartas, uma por arquétipo.
Conferidos com o livro e as cartas de `Cards/Equipe`, usando as
versões corrigidas de Forasteira, Legado e Transformado em `Cards/Novas/Equipe`.
A pilha volta a 1 quando há um salto ficcional significativo, não a cada cena
ou fim de sessão. Os movimentos individuais foram conferidos diretamente no
livro: as referências a `arquetipos.json` identificam o arquétipo, mas esse
arquivo ainda não contém seus efeitos de celebração/vulnerabilidade no motor.

Movimentos de arquétipo (`movimentos-arquetipo`) é categoria principal separada
de Movimentos de equipe, com subseções pelo campo `arquetipo`. Começa com os
seis movimentos da Bruta, borda vermelha e miolo padrão. Os nomes e IDs seguem
`arquetipos.json`, mesmo quando diferem dos títulos das cartas antigas.
Conferidos com `Fichas Exemplo/Bruta/bruta1.png` a `bruta6.png`, o livro
(pp.104 e 107) e as regras da Torre. Coração da Bruta fica para os extras.

Primeiro help contextual: na ficha da Bruta, aba Módulos, um botão `?` ao lado
de Movimentos do arquétipo abre as seis cartas filtradas. A ficha fica aberta
por baixo, sem reconstrução ou mudança dos movimentos escolhidos. No leitor,
Voltar retorna à galeria filtrada; na galeria, Voltar fecha a ajuda. O menu
principal de Ajuda continua abrindo o catálogo completo.

Vilões (`viloes`) é categoria principal para o mestre, com borda vermelha:
movimentos, ficha, condições, luta, cinco cartas de reações por condição,
capangas e dois exemplos (Professora e Rosa Rook). Textos próprios, com base
no briefing `docs/briefing_ajuda_viloes.md` e consulta ao livro. As referências
apontam para `Regras/viloes.json`, material de consulta que não implementa
combate ou registro de movimentos de vilão no motor. Derrota: precisa marcar
uma condição e não tem nenhuma livre; marcar a última livre não basta.

Na ficha de NPC com papel de vilão, `?` ao lado de Condições abre apenas as
seis cartas: `viloes/luta` primeiro e as cinco `viloes/condicao-*`, mantendo a
ficha por baixo. Mostra todas, independentemente das condições disponíveis naquela ficha; não marca
condições nem altera dados. A Ajuda geral também oferece a subgaleria
Movimentos de condição em Vilões, com retorno a Todos os vilões. Voltar do
leitor retorna à galeria filtrada; Voltar da galeria fecha a ajuda contextual.

## Geração e uso

`python tools/cards/gerar_cartas.py` valida IDs/arquivos únicos, referências e
espaço de texto, gera os PNGs e atualiza as cópias para o painel atual.
O painel atual ainda usa imagens, não renderiza o catálogo como texto.
Um futuro consumidor Unity pode ler `Resources.Load<TextAsset>("Ajuda/catalogo")`.
Assim a apresentação textual e a geração de cartas usarão a mesma fonte editorial.

Assets fora de Resources também podem entrar no build quando referenciados por
cenas/assets incluídos. Resources permite carregar estes arquivos por caminho
em tempo de execução; não é a única forma de incluir conteúdo no build.


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

## Primeira sessão (2026-10-15)

Categoria principal `primeira-sessao`, borda verde-água `#3fb8a8`:
Antes da luta, A cidade em quadros, O vilão em ação, Como você entra?,
A primeira luta e Depois da luta. IDs estáveis, respectivamente:
`primeira-sessao/antes`, `/cidade`, `/vilao`, `/entrada`, `/luta`, `/depois`.

Texto próprio conferido com o capítulo 8 do livro (pp.207–213), seguindo
`briefing_ajuda_primeira_sessao.md`. A entrada pode ter um movimento breve;
a luta engrena com todas presentes. A carta da luta remete a
`equipe/batalha` pelo título, sem repetir o questionário da pilha.
As seis cartas têm vínculo de jogo do tipo `mj` em `Regras/cartas.json`
para a Gestão das cartas. Sem PNGs. Os botões contextuais da Área do mestre
ficam para a frente de mecânica, usando os IDs acima.


## Movimentos do MJ (2026-10-16)

Categoria `movimentos-mj`, borda cinza: uma referência de quando usar e
16 movimentos básicos, com movimentos de arquétipo e de vilão separados.
Texto próprio conferido no livro pp.177–184 e nas cartas antigas do acervo.
Cada carta tem vínculo `tipo: mj`, origem `mestre`, em Regras/cartas.json.
IDs de ajuda usam `movimentos-mj/`; IDs de jogo usam `mj/movimentos-mj/`.
São orientações editoriais: não implementam resolução automática no motor.
Uma falha contra PJ não exige por si só inverter o movimento; o peso segue
a ficção. Nenhum PNG foi gerado. Visualização no Play ainda deve ser conferida.
