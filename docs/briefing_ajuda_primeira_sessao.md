# Briefing: cartas de ajuda da Primeira sessão

Para a IA que cuida do help (`Assets/Resources/Ajuda/catalogo.json`). Escrito em 2026-10-15.

## O que fazer

Criar uma **categoria principal nova**, `primeira-sessao` (nome na tela: "Primeira sessão"), com as
**seis cartas** abaixo, no mesmo formato das cartas de Vilões (`viloes/*`): `id`, `categoria`, `titulo`,
`compacto: false`, `borda`, `origem`, `secoes` [{`titulo`, `texto`}].

- **Borda:** verde-água, `#3fb8a8` (diferente de Vilões, vermelho; Equipe, azul).
- **Ids:** `primeira-sessao/antes`, `primeira-sessao/cidade`, `primeira-sessao/vilao`,
  `primeira-sessao/entrada`, `primeira-sessao/luta`, `primeira-sessao/depois`.
- **Origem:** "Primeira sessão · livro cap. 8, pp.207–213".
- **Sem PNG** (decisão do resumo: as cartas são desenhadas a partir do JSON). Sem campo `arquivo`.
- Registrar a categoria onde as outras são registradas (`PainelAjuda`, lista de categorias), como foi feito
  com `perdicao`.
- **Texto próprio.** O livro é só consulta (repositório público). Conferir no texto do livro no acervo
  (`Livros/MASKS (Regras Oficiais) - texto.txt`, cap. 8, linhas ~3102–3215) e ajustar se algo divergir.
- A carta 5 **aponta** para a carta já existente `equipe/batalha` (Entrando em batalha); não duplicar o
  questionário da pilha.
- Manter as seções curtas (cabem na carta desenhada); se passar, cortar texto, não seção.

## As seis cartas (rascunho aprovado pelo Alan)

**1 · ANTES DA LUTA** (`primeira-sessao/antes`)
- FICHAS: criação em conversa; pergunte e aprofunde; resposta vaga vira pergunta.
- CACE NOMES: anote toda pessoa e lugar que aparecer; são o elenco da cidade.
- VILÃO: prepare um ligado a pelo menos uma heroína; abertura boa é com vilão que importa.

**2 · A CIDADE EM QUADROS** (`primeira-sessao/cidade`)
- ABRA LONGE: dois ou três quadros de partes diferentes da cidade antes do local da luta.
- CIDADE DE SUPERS: estátuas de heróis antigos, abrigos, protocolos de emergência; o povo já viu isso.
- ZOOM: só então o lugar da luta.

**3 · O VILÃO EM AÇÃO** (`primeira-sessao/vilao`)
- ELE JÁ COMEÇOU: destruindo algo, causando confusão, perseguindo o objetivo dele.
- UM MOVIMENTO DELE: abra com um dos movimentos de vilão dele.
- AINDA SEM DADOS: ninguém rola antes de todas entrarem.

**4 · COMO VOCÊ ENTRA?** (`primeira-sessao/entrada`)
- UMA POR VEZ: pergunte a cada jogadora como a heroína chega.
- DEIXE SE EXIBIR: é a hora de mostrar como ela é incrível; o drama fica para depois.
- POUCA AÇÃO: no máximo um movimento curto; todas em cena antes de engrenar.

**5 · A PRIMEIRA LUTA** (`primeira-sessao/luta`)
- ENTRAR EM BATALHA: com todas em cena, dispare o movimento de equipe e a pilha nasce (carta Entrando em batalha).
- ANUNCIE OS MOVIMENTOS: "Parece que você está tentando defender, é isso?"
- SUGIRA QUANDO HESITAREM: sempre em pergunta, pela ficção.
- MOSTRE TUDO UMA VEZ: cada movimento básico (inclusive golpe poderoso), mudar Rótulos, rejeitar Influência, ganhar e tirar condição.
- NÃO GASTE A SESSÃO NISSO: o vilão pode fugir quando der errado.

**6 · DEPOIS DA LUTA** (`primeira-sessao/depois`)
- CENAS EM DUPLA OU MAIS: nunca uma heroína sozinha; a bronca da mentora é na frente de outra.
- QUEM APARECE: heróis adultos criticando, entes queridos comentando, comemoração, segredos, confortar.
- AGORA SIM O DRAMA: "sua mãe sabe que você saiu às 3h?" é daqui.

## Depois

Anotar no `docs/ajuda.md` (como nas outras categorias) e avisar o Alan. A outra IA (mecânica) liga um "?"
em cada grupo do roteiro da Área do mestre (`PainelMestre`) para abrir estas cartas pelos ids acima: não
mudar os ids sem avisar.
