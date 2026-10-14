# v0.0.4 — A equipe se forma na sala

> 2026-10-14. Os dois primeiros requisitos para uma personagem sair de "em aprovação" e entrar em
> carreira: a formação da equipe e a Influência inicial. Os dois desenhados pelo Alan, à mão.

## O fio

Com 5 personagens em aprovação numa sala, o que falta é a parte da criação que só existe **com
equipe**. O Alan desenhou cada tela (um rascunho por mensagem) e explicou o porquê de cada regra; o
trabalho foi transformar o desenho em tela e a regra em Core testado. O que organiza o que vem: a
**trava** de em carreira agora existe e cobra requisito por requisito (`PendenciasParaCarreira`).

## 1. Formação da equipe (aba da sala)

À esquerda, quem está em aprovação; em cima, a pergunta do arquétipo da selecionada e o enviar; embaixo,
**só leitura**, as respostas na ordem de chegada.

**Por quê:** "quem responde primeiro estabelece a verdade; os demais constroem sobre ela" — a ordem de
chegada é narrativa, então a fila é da **sala** (`Sala.FormacaoEquipe`), não da ficha. Reenviar edita e
**mantém a posição**. O mestre pode errar o clique: **Desfazer** tira o primeiro envio da fila (quem vinha
depois sobe) ou volta o texto anterior, e avisa quando alguém já construiu em cima. Copiar para o fórum
(BBCode) e para o WhatsApp sai do leitor.

## 2. Influência inicial

- **O jogador marca na ficha** (Núcleo); a aba da sala é a visão do mestre — e vira, no jogo, o painel de
  dar e tirar Influência. Um componente só (`SeletorDeInfluencia`) nos dois lugares.
- Cada regra do arquétipo: fixo, todos (o jogador ainda confirma), comportamento (Nova, Forasteira,
  Discípula) e amor e rival da Bruta (por ora, escolher 2).
- **Influência dada não sai da ficha** (Alan): por colega, **dada** ou **perdida**; no jogo perde e volta,
  e se dá Influência nova. A leitura reversa lê esse estado nas fichas. O porquê de cada uma (roleplay) e
  o de não dar a ninguém ficam guardados; a Torre nunca apaga.

## 3. A trava de "em carreira"

`AprovarParaCampanha` cobra a formação enviada e a Influência confirmada; o botão fica apagado com as
pendências no balão. Os testes cumprem os requisitos por `FichaPronta.ParaCarreira`.

## 4. Fotos

Aprovar diz APROVADO; recusar diz RECUSADO e a imagem some (o link fica para o jogador trocar). A foto é
opcional: não trava a carreira.

## 5. Ajuda (outra IA)

A outra IA fez um gerador próprio de cartas de ajuda (`tools/cards/gerar_cartas.py`): texto próprio com
página do livro, moldura desenhada por código, nada do acervo. Gera 4 panoramas em
`Assets/img/cards/regras`; ainda não ligados ao menu. Também botões novos em `img/logo`.

## ⚠ Achados e erros meus

- **Eu inverti a direção da Influência na tela.** "Dar Influência a X" quer dizer que X tem Influência
  sobre quem deu. Escrevi "têm Influência sobre ela" para quem deu a ela — o certo é "**ela exerce**
  Influência sobre" essas pessoas. O cálculo estava certo; o texto e os comentários, invertidos.
  Regra para a próxima: na Influência, **quem recebe tem o poder**.
- **Adiantei a regra da Influência e o conteúdo da aba da equipe** antes de o Alan explicar (de novo:
  "eu não disse como a aba vai funcionar"). Ele quer desenhar primeiro. Tirei o que tinha feito.
- **Deixei marcar 4 de 2.** O Core recusava na confirmação, mas a tela deixava; os chips agora travam no
  limite, como nas habilidades.
- **Um "colega falso" no dicionário** (a chave `*ninguem` para o porquê de não dar a ninguém) — trocado por
  campo próprio a pedido.
- Coluna de largura fixa que herda "esticável" de dentro rouba o espaço do lado (o "Enviar como" gigante,
  de novo): `LayoutElement` com `flexibleWidth = 0` e prioridade 2.

## O que não terminou

- **Relacionamentos** e os **NPCs que orbitam** cada personagem (criados pelos jogadores): os outros
  requisitos de em carreira. Esperando o Alan desenhar.
- A Bruta liga a Influência aos relacionamentos (amor e rival) quando eles existirem.
- **Painel do mestre** (área do mestre): só o botão; o Core da mesa e o comando de dar/perder Influência
  no jogo estão prontos, sem tela e sem evento no log.
- As cartas de ajuda não estão ligadas a nenhuma tela.
