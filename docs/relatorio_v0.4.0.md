# v0.4.0 — O MJ decide a ordem

> 2026-10-19. O segundo movimento (tomar um golpe poderoso) entrou no molde do Enfrentar, e com ele a regra que
> organiza o resto do rolador: o que uma rolagem pede vira **pendência no log**, nunca uma janela que abre sozinha.

## O fio

Eu fiz o golpe poderoso abrir sozinho logo depois do Aceitar do Enfrentar. O Alan recusou: quando o vilão marca uma
condição na troca, ele **interrompe a mesa** com o movimento daquela condição, e só o MJ sabe a ordem certa. A partir
daí, tudo o que uma rolagem pede (o golpe da troca, o movimento de condição do vilão, um golpe emocional anotado pelo
console) virou um botão no log. O botão some quando a coisa é feita e volta se ela for desfeita. A regra ficou na
memória do projeto para os próximos movimentos.

## O que mudou e por quê

### Tomar um golpe poderoso (`movimentos_basicos.json`, livro p.76–78)

- **No mesmo painel do Enfrentar.** Soma as condições marcadas (teto +4), sem "contra quem": quem rola é quem
  apanha. Com origem, a rolagem nova aponta para a que a pediu, e o log diz "pedido por: Enfrentar de Aurora →
  Doutor Ocaso".
- **Faixas invertidas só na tela.** O livro continua chamando o 6− de falha ("marque potencial, como em qualquer
  falha", p.77), e a Torre grava falha, porque é a falha que dá o potencial. A tela troca as cores: o "Sucesso" do 10+
  aparece em vermelho. O Alan gostou de ler assim e dispensou rótulos novos.
- **As opções.** "Aguentar a dor" pede as duas condições (à escolha de quem rolou) e as marca no Aceitar. "Duas da
  lista de 7–9" abre as de dentro, cada uma com o seu detalhe. "Ceder terreno" pergunta **quem ganha a abertura** e
  põe o lembrete no cartão dele, dentro do evento da rolagem. "Descontar num colega" pede provocar ou Influência,
  **só como marca narrativa** (a Influência na mesa ainda não existe).
- **Não aceita intervenção.** Decisão do Alan, registrada no JSON. O livro não diz com essas palavras, mas nada
  encaixa: a ajuda da pilha (+1) pioraria o golpe, o −2 da renúncia de Influência viraria ajuda, e o Defender é
  interrupção de PvP contra quem ataca.

### Pendências no log

- O golpe que a troca pediu: botão "Golpe poderoso: Nome" embaixo da rolagem (dois no PvP).
- O movimento de condição do vilão que a troca marcou: botão "Movimento de condição: Ocaso (Irritado)", com a carta
  da condição no help e um "Resolvido" com o que ele fez.
- Pedidos pelo console, com o atalho no JSON de cada movimento: `/eda Aurora vs Ocaso`, `/tgp "Estrela Negra"
  "motivo"`. Só os movimentos conferidos ganham atalho; o próximo traz o dele sem código novo.

### Condições na conta

A condição marcada pesa −2 nos movimentos dela (`nucleo.json`, p.89–90). Nada lia esse bloco: agora
`Core/Regras/RegrasDeCondicoes.cs` lê, a rolagem guarda as **partes** do modificador ("Perigoso +1, Assustado −2") e
o painel mostra a conta antes de rolar. Adiante, constante e Influência entram na mesma lista depois. A dica da
condição no assento também passou a dizer onde ela pesa e o que a tira no fim da cena.

### Ferramentas para validar

O Alan vai testar muitas vezes, e quer reproduzir os exemplos do livro: `/reset` (debug, com confirmação; cada parte
se desfaz), `/fixo` e o botão "Rolar fixo" (a soma dos dados, sem jogar), `/cena`, `/tempo`, `/salvar` (o log em
.txt), `/avancar`. A intervenção virou três listas (quem ajuda, de onde vem, mod), o resultado antes da intervenção
fica à vista ("10   8 + 2"), opção longa quebra em duas linhas e o help do console rola.

Em paralelo, a outra IA fez a **animação dirigida** dos dados (`6f2408d`): com dado fixo, os dados 3D caem no valor
pedido. O "Rolar fixo" do painel passa pelo caminho dela.

### Ficha

- Duas perguntas de histórico com "quem" não davam NPC: a da Amaldiçoada ("quem é essencial para derrotar seu
  nêmesis", aliado) e a da Discípula ("quem mais sabe do seu treinamento", neutro). Regra: "quem" é pessoa, e pessoa
  vira NPC. Os papéis foram escolha minha; o Alan ainda não confirmou.
- O movimento da Bruta "Lá quando importa" (nome nosso) virou **"Chegar na hora certa"**, perto do livro PT ("Chega
  na hora certa"). O id ficou `la-quando-importa` para não quebrar fichas gravadas.

## Achados que contradizem o que se acreditava

- **Cena nova não volta a pilha para 1.** O Alan pediu isso para o `/cena`. O livro diz que a pilha "só se esvazia de
  volta para 1 depois que o tempo passa" (p.88 e capítulo do MJ). Ficou separado: `/cena` marca, `/tempo` volta para
  1. Se for regra da casa, é uma linha.
- **"Chegar na hora certa" é preso à pessoa defendida.** O PT ("quando a pessoa estiver em perigo") é ambíguo; o
  inglês ("when **they** are in danger later") não. E o controle troca só a escolha do sucesso do Defender: o custo do
  7–9 continua.
- **A equipe dos exemplos do livro não tem ficha publicada** (procurei no acervo e na web). O texto dá os arquétipos de
  Huma, Toro, Crepúsculo, Vespa, Gafanhoto, Tiro-Certo e Rex. Vértice (Nova) e Canção dos Céus (Forasteira) são
  "segundo o Alan": o texto não diz.

## Meus erros do dia

- **O golpe abrindo sozinho** (acima). Virou regra na memória.
- **Uma busca-e-troca em massa (`tpg` → `tgp`) corrompeu dois PNGs** de cartas de regra: o `grep` sem `-I` pegou
  binário. Restaurei do git antes de qualquer commit.
- **Heredoc do Bash**: comeu o `\n` de uma string C#, e um `cat` sem entrada travou o comando por 2 minutos. Os patches
  passaram a ser arquivos `.py` no scratchpad.
- **A sigla do golpe** saiu `tpg`; o Alan corrigiu para `tgp`.

## Verificação

- Core: 197 testes no harness (eram 184). Os novos cobrem golpe, pendências, pedidos, partes do modificador,
  abertura, alternativas, reset, fixo, cena e as perguntas de histórico.
- `tools/compilar.sh` passa.
- **Visto no Play pelo Alan:** o painel do golpe (o "Sucesso" vermelho), a intervenção em listas, o texto que vazava do
  botão, o lembrete da abertura no Ocaso, o log com o pedido. **Não visto por ninguém ainda:** o reset, o /salvar, o
  Rolar fixo com a animação, a carta da condição abrindo no help, as duas perguntas de histórico novas.

## O que não terminou

- **Cena e Spectate** (desenho do Alan, combinado, não implementado): quem está em cena e quem assiste; entrar e sair
  à vontade do MJ, pela narração ou gastando o controle do "Chegar na hora certa"; saídas pela regra (golpe 10+
  "afastar-se", precisar marcar condição sem ter livre); o painel oferecendo só quem está em cena. Hoje "sai de cena"
  é só texto no log.
- **Próximo movimento: Liberar seus poderes**, depois o Defender (as duas caras: proteger e a interrupção de PvP) e o
  ganho de controle do "Chegar na hora certa".
- **O resto do modificador**: adiante, constante, Influência passiva +1.
- **Atalho para os movimentos do MJ no help** (o item 2 do resumo anterior).
- **Influência na mesa**: o "descontar num colega" e a renúncia continuam narrativos.
- **Validar com os exemplos do livro** na campanha Pacatópolis: as fichas ainda vão ser criadas pelo Alan.
- **O `Palco2D6.prefab`** ficou modificado com valores de dados ajustados no Inspector, fora deste lote (ver o
  resumo).
