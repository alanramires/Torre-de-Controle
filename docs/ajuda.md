# Catálogo de help

Fonte única dos textos das quatro cartas de panorama, dos oito movimentos básicos,
das nove cartas PvP e das quatro cartas de Influência:
`Assets/Resources/Ajuda/catalogo.json` (UTF-8, texto próprio).
As regras executáveis continuam em `Assets/Resources/Regras/`; o catálogo
editorial não substitui essas regras nem os dados de jogadores, personagens e salas.

## Formato, versão 1

- `versao`: versão do formato do catálogo.
- `cartas`: lista ordenada das entradas de ajuda.
- `id`: chave estável, como `panorama/rotulos`; não usar o título como identidade.
- `categoria`: agrupamento da navegação: `referencia-rapida`, `movimentos-basicos`, `pvp` ou `influencia`.
- `arquivo`: nome do PNG gerado, preservando os nomes `00-*` já usados pela UI.
- `titulo`: título exibido na carta.
- `compacto`: opção de composição para conteúdos com muitas seções.
- `origem`: rodapé com as fontes/decisões consultadas.
- `regras_ref`: caminhos de referência relativos a `Regras/` ou `docs/` desde a
  raiz. Para JSON, aceita fragmento como `nucleo.json#/bonus`.
- `secoes`: pares `titulo` e `texto`, na ordem de leitura.

`regras_ref` fornece rastreabilidade, não valores substituídos automaticamente.
Ao corrigir uma regra, revisar o texto dos helps relacionados e regenerar.
Não manter explicações concorrentes dentro do Python. Para adicionar uma carta,
adicionar uma entrada ao catálogo com ID, fontes e texto próprios; ampliar a
navegação da Unity quando necessário. O painel lê títulos e arquivos do catálogo
nas categorias `referencia-rapida`, `movimentos-basicos`, `pvp` e `influencia`; novas categorias ainda
precisam ser incluídas na lista de categorias do painel.

As variantes contra PJ ficam nas nove cartas PvP, conferidas com as cartas do
acervo `Cards/Novas/PvP`, `pvp.json`, `influencia.json` e o livro (consulta).
Os básicos não repetem as variantes de Enfrentar, Defender e Provocar.
Confortar e Perfurar mantêm seus efeitos gerais; a reação contra um colega em
Tomar um golpe poderoso permanece porque é parte do próprio movimento.
As correções de Liberar (condição OU efeito instável em 7–9) e Avaliar
(bônus enquanto usa as respostas) prevalecem sobre as cartas antigas.

Influência tem quatro cartas: conceito, uso, renúncia e rejeição. Conferidas
com `Cards/Influencia` do acervo, `influencia.json`, `nucleo.json` e o livro
(pp.79–83). Rejeitar usa 2d6 sem Rótulo, +1 por Influência recíproca e −2 por
Inseguro; a falha também rende potencial. Os textos reduzidos ficam no catálogo.

## Geração e uso

`python tools/cards/gerar_cartas.py` valida IDs/arquivos únicos, referências e
espaço de texto, gera os PNGs e atualiza as cópias para o painel atual.
O painel atual ainda usa imagens, não renderiza o catálogo como texto.
Um futuro consumidor Unity pode ler `Resources.Load<TextAsset>("Ajuda/catalogo")`.
Assim a apresentação textual e a geração de cartas usarão a mesma fonte editorial.

Assets fora de Resources também podem entrar no build quando referenciados por
cenas/assets incluídos. Resources permite carregar estes arquivos por caminho
em tempo de execução; não é a única forma de incluir conteúdo no build.
