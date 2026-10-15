# v0.1.0 — Cartas e Cenário

> 2026-10-15. Duas partes novas e inteiras: as **cartas desenhadas pela Unity** (catálogo em JSON, desenhador,
> help e editor) e o **Cenário** das campanhas (locais, organizações, outros). Por isso o Y sobe.

## O fio

A ficha do jogador já criava NPCs (v0.0.5) e os NPCs já tinham casa (v0.0.6). Faltavam duas coisas para a
mesa: **o que o jogador segura na mão** (as cartas, que até aqui eram PNGs fabricados fora) e **onde as
coisas acontecem** (lugares que estavam nascendo como NPC por falta de outra gaveta). As duas frentes seguem
a mesma regra da Torre: dado é identidade, a tela interpreta; e a ficha alimenta a campanha sem o mestre
redigitar nada.

## 1. Influência entre heroína e NPC (a38d749)

- Fecha o quadro de Influência: **PJ↔PJ** (já existia), **PJ↔NPC** (agora), **NPC↔NPC** (não existe, decisão).
- Na ficha do NPC: "{NPC} DEU INFLUÊNCIA A" e "{NPC} EXERCE INFLUÊNCIA SOBRE". A ficha da heroína e a árvore
  da sala mostram o NPC com "(NPC)". Adulto continua com o padrão calculado do livro.
- "?" na seção de Influência da ficha, no mesmo estilo dos movimentos de arquétipo.

## 2. Cartas: de PNG fabricado a JSON desenhado

- **Por quê:** cada carta nova era uma arte fechada, cara de refazer e impossível de corrigir uma vírgula.
  O Alan: "as cartas primeiramente teriam que ser templates JSON". Agora a carta é dado (`Regras/cartas.json`
  + `Ajuda/catalogo.json`) e a Unity desenha no estilo oficial: ícone, faixas, rodapé de moedas, selo da classe.
- **O help virou as cartas desenhadas**: os PNGs saíram de `Resources`. Navegação em cortina com dois níveis
  nos arquétipos; filtro por categoria que volta ao todo.
- **Gestão das cartas** é o próprio help em modo edição: rota na árvore, ícone, "exclusiva de", borda (inclusive
  roxa), nome, texto com `[barra]`, `[cor=…]`, `[b]`, benefícios. Salva nos dois JSON do projeto
  (`ArquivosDoProjeto`, só no Editor, preservando CRLF). `PainelCartas` saiu.
- **Decisão registrada pela outra IA no resumo:** não se fabricam mais PNGs de cartas; tokens e ícones seguem
  como arte separada.

## 3. NPCs que nascem da ficha: o resto dos buracos

- **Perguntas de formação da equipe** que dão NPC (Bruta, Janus, Legado, Transformado): nome opcional ao lado
  da pergunta, na ficha e no •••; a resposta é a descrição.
- **Nome repetido não cria outro**: se o nome já é de um NPC da sala, a Torre usa o que existe e avisa.
- **Descrição copiada, não espelhada.** O Alan: "prefiro uma cópia, porque lá é história narrada e aqui tem
  que ser editada". O texto do jogador entra nas Notas uma vez; daí em diante cada lado é de quem escreve.
- Links de foto `blob:` são recusados com explicação (o ChatGPT passou a gerar assim).

## 4. Cenário

- **Por quê:** o local destruído da Nova estava virando NPC neutro jovem. Lugar não é gente: não tem
  Influência, maturidade nem ficha de vilão. Tipos básicos, por decisão do Alan: **Local, Organização, Outros**,
  com nome, tipo e descrição livres. Por enquanto cada campanha é uma cidade.
- **Aba "Cenário" na sala** (criar, editar, inativar; nada se exclui) e **exportar/importar** a cidade em JSON
  próprio (`torre-de-controle/cenario`); importar não duplica nome que já existe.
- **O que nasce da ficha:**
  - Nova: local destruído na formação → Local;
  - Forasteira: "De onde você vem?" → Local (antes era NPC neutro: um remendo meu de antes do Cenário);
  - Amaldiçoada: santuário → Local, com nome opcional no título do extra e, sem nome, "Santuário de {heroína}";
    a descrição junta características e desvantagens;
  - Discípula: base secreta → Local, **só se marcada** nos recursos da mentora;
  - Nave Kirby: **NPC só se for Telepática**; senão, Cenário · Outros (regra `npc_se` / `senao_cenario`).
- O nome escrito pelo jogador manda (na sala, só leitura); o nome automático é do mestre, que renomeia.
- NPCs que já tinham nascido dessas fontes ficam **inativos** ao carregar.

## ⚠ Achado: o Farol não pede lugar

Eu tinha mapeado a formação do Farol como "local do incidente". **O livro desmente**, no PT-BR e no inglês:
a pergunta pede **sinais** ("Encontramos indícios de que o incidente era só o começo de algo maior. Quais?";
"What were the signs?"). Quem pede lugar é a Nova. O mapeamento saiu; um local que tenha nascido dele fica
inativo. Quem pegou foi a outra IA, ao revisar a mensagem para os jogadores. Lição: conferir a pergunta no
livro antes de mapear, mesmo quando a lista vem pronta.

Na mesma revisão, "evento" foi proposto como tipo de cenário e **ficou de fora**: cenário é o que existe;
o que aconteceu é do log e do Diário da Campanha, que ainda não existem.

## 5. Outra IA

Help e tokens: movimentos de Janus, Legado, Nova e Transformado (666376b); as 11 cartas da Perdição da
Amaldiçoada; tokens de condição e de Influência; ícones de movimentos; `docs/ajuda.md` e `docs/tokens.md`.

## O que não terminou

- **Nada disso foi visto no Play mode por mim.** Cenário (aba, exportar/importar, caixa do santuário no título),
  editor de cartas com borda e o help desenhado dependem da conferência do Alan.
- Cenário: copiar um elemento para outra campanha, laboratório de cenário (botão "NPCs e cenário"), cidade
  como nível acima das campanhas — anotados, sem pressa.
- Legado (nome da linhagem → Organização) e Janus (emprego/escola → Local) ficaram em "talvez".
- Cartas: descobrir as categorias do help pelo catálogo; botão "nova carta" (Adulto); "?" por chip de movimento.
- Seguem de antes: mesa/rolador/log, moedas de Influência PJ↔PJ na mesa, origem dos sons.
