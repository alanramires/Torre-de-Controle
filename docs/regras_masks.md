# Masks — entendimento das regras para continuar a Torre

Registrado em 2026-10-13, após consulta ao texto do livro no acervo. Este documento
é um mapa de conceitos e cuidados, escrito com palavras próprias. Não é uma cópia
do livro, uma nova especificação executável nem um relatório do que já foi implementado.

## Como retomar o trabalho

Leia `resumo.md` para o contexto da implementação e este arquivo para o entendimento
do sistema. Antes de mudar uma regra, leia também `CLAUDE.md`, `regras.md`, o JSON
correspondente e `fluxo_de_telas.md`. Consulte os achados de `relatorio_v0.0.1.md`.

- As decisões validadas com Alan estão no repositório; não reabra escolhas por uma
  interpretação nova do livro. Se encontrar conflito, explicite a diferença.
- O motor lê `Assets/Resources/Regras/*.json`, não o acervo.
- Livro de consulta: `D:/Acervo do RPG/2025/2025/Alan - Masks/Livros/MASKS (Regras Oficiais) - texto.txt`
  (UTF-8). As páginas abaixo são as impressas no livro.
- Para campos e regras dos arquétipos, prefira as fichas oficiais em inglês:
  `D:/Acervo do RPG/2025/2025/Alan - Masks/Livros/Masks-ENG-Arquetipos.pdf`.
  Os valores iniciais dos Rótulos aparecem em azul: conferir visualmente, pois o
  texto extraído perde essa distinção. Não inferir valores pelas listas de números.
- `resumos/` e Tutorial 01 são material do Alan, sujeito a erros e adaptações;
  ajudam a localizar assuntos, mas não decidem regras.
- O repositório é público. Tradução PT-BR, artes e arquivos do acervo são apenas
  referência: não copiar para o projeto. Regras e helps entram com texto próprio
  e referência de origem.

## O que o jogo está modelando

Masks acompanha uma equipe de jovens heróis descobrindo sua identidade sob pressão
de amigos, rivais, adultos e responsabilidades. O arquétipo define conflitos e
temas narrativos, não apenas um conjunto de poderes (livro pp.13–17, 41–54).

A conversa estabelece a ficção; uma ação ou situação que satisfaz um gatilho aciona
o movimento. Não basta escolher um botão para justificar uma rolagem. Usos comuns
de poderes podem acontecer sem teste. Nem toda ação exige movimento, e nem todo
movimento exige dados (pp.29–36, 55–56).

Habilidades são possibilidades narrativas. Movimentos são procedimentos com
gatilhos e efeitos. Extras são sistemas particulares, como Perdição, Impulsos ou
Chamas. Comprar um movimento de outro arquétipo não concede automaticamente seus
extras ou habilidades (pp.43–49, 158–160).

## Núcleo e resolução

- Rótulos: Aberração, Perigoso, Salvador, Superior e Mundano. Representam autoimagem,
  podem mudar e normalmente ficam entre −2 e +3. Na criação há o +1 livre previsto
  pela ficha (pp.45–46).
- Mudar Rótulos normalmente significa subir um e descer outro. Se um deles
  ultrapassaria o limite, a troca inteira não ocorre e uma condição é marcada.
  Somar pontos por avanço é outra operação; não exige a descida de outro Rótulo
  (pp.46, 91, 158–160).
- Rolagens usam 2d6 e o modificador aplicável, limitado entre −3 e +4.
  As faixas usuais são 10+, 7–9 e 6−. Uma falha não precisa significar que a ação
  fracassou: ela abre consequências conduzidas pelo mestre (pp.32–35).
- O mestre não rola pelas ações dos PNJs no livro. Na Torre, o mestre opera também
  as rolagens das PJs; isso não transforma essas personagens em PNJs.
- Falhar em movimento rende potencial, inclusive quando falhar é favorável,
  como em Tomar um golpe poderoso (pp.76–78, 92, 157–158).
- Adiante vale para a próxima rolagem aplicável; bônus enquanto uma circunstância
  existir pode valer em várias rolagens. Controle é uma reserva gastável conforme
  as opções e o prazo do movimento, não um bônus genérico (p.56).

### Condições

| Condição | Penalidade de −2 nos movimentos associados |
|---|---|
| Assustado | Enfrentar diretamente a ameaça |
| Irritado | Confortar ou apoiar; Perfurar a máscara |
| Culpado | Provocar; Avaliar a situação |
| Desesperançoso | Liberar seus poderes |
| Inseguro | Defender; Rejeitar Influência |

As condições são estados emocionais, não pontos de vida. Marcar todas não remove
a personagem automaticamente: ela sai quando precisa marcar outra e não há
condição disponível (pp.89–91). Efeitos de movimentos podem remover condições
imediatamente; a remoção por comportamento narrativo acontece no fim da cena:
descontar raiva em algo importante, fugir de dificuldade, sacrificar-se para
reparar culpa, buscar alívio fácil ou agir imprudentemente sem consultar a equipe,
conforme a condição. Não remover antecipadamente a penalidade.

## Movimentos básicos: o que os helps devem distinguir

| Movimento | Gatilho e cuidado principal |
|---|---|
| Enfrentar diretamente a ameaça | Confronto direto com perigo real para a personagem; troca de golpes e escolhas. Contra PJ, a troca pode gerar Golpe poderoso para ambos (pp.57–59). |
| Liberar seus poderes | Forçar capacidades para superar obstáculo, alterar ambiente ou ampliar sentidos; não é todo uso de poder. Em 7–9, escolher condição ou efeito instável/temporário, preservando o sucesso central (pp.60–62). |
| Defender | Perigo iminente; diferenciar ameaça PNJ/ambiente de ameaça PJ. Contra PNJ há benefício em todo sucesso e custo adicional em 7–9; contra PJ há −2 na rolagem adversária (pp.63–65). |
| Avaliar a situação | Investigar circunstâncias para obter informações úteis. O bônus vale enquanto agir conforme as respostas; não é simplesmente +1 adiante. Respostas podem acumular bônus, respeitando o limite (pp.66–67). |
| Provocar | O alvo precisa ser suscetível; explicitar a ação desejada. Respostas e incentivos são diferentes para PJ e PNJ (pp.69–71). |
| Confortar ou apoiar | Apoio emocional significativo; os benefícios dependem da abertura do alvo. Pode desencadear depois um movimento de vulnerabilidade (pp.72–74). |
| Perfurar a máscara | Ler intenções e sentimentos; perguntas e respostas honestas, não apenas descobrir uma identidade secreta (pp.75–76). |
| Tomar um golpe poderoso | Golpe físico ou emocional determinado pela ficção; rola com condições marcadas. É invertido: resultado alto traz consequências mais graves, falha permite resistir e ainda marca potencial (pp.76–78). |

O help deve mostrar quem escolhe cada opção, o alvo, custos, efeitos imediatos e
eventuais movimentos seguintes. Não generalizar todas as listas como benefícios:
há listas de consequências negativas.

## Influência

Influência é direcional e binária: A ter Influência sobre B não implica o contrário.
Expressa que B se importa com a opinião de A. Adultos têm Influência sobre as jovens
PJs por padrão, mas podem perdê-la (pp.79–84, 178–179, 193).

- Ter Influência rende +1 nas ações pertinentes contra/em relação ao alvo.
- Receber novamente Influência sobre uma PJ permite alterar seus Rótulos; sobre
  PNJ rende +1 adiante. Não acumular níveis de Influência.
- Quando alguém com Influência impõe uma visão relevante da personagem ou do mundo,
  apresentar a troca de Rótulos antes da escolha entre aceitar e rejeitar.
- Rejeitar não usa Rótulo; outros modificadores podem existir, como Influência
  recíproca e a penalidade de Inseguro. Pode ser usado contra quem tem Influência,
  não só contra adultos.
- Aproveitar a Influência significa renunciar a ela para obter o efeito escolhido;
  não confundir esse gasto com o bônus passivo.
- Dar Influência é possível quando a ficção justificar. Retirá-la exige uma regra
  ou efeito aplicável; não é livremente desligar uma preferência.

**Achado já validado:** Influência inicial faz parte da criação no livro, depois
da formação da equipe e dos relacionamentos. Na Torre, isso requer a equipe da sala.
A ideia de esperar o fim de um prólogo era adaptação do Tutorial 01, não regra do livro.

## Equipe e relógios narrativos

Pontos de Equipe representam cooperação. Ajudar exige uma contribuição possível na
ficção; normalmente cada participante gasta até um ponto na mesma rolagem, salvo
exceções de movimentos. A ajuda pode ocorrer após os dados. Agir egoisticamente
gasta um ponto e muda Rótulos, com justificativa de como ignora ou prejudica os
colegas; não tratar isso como o mesmo fluxo de ajudar (pp.84–88).

Ao entrar em confronto perigoso como equipe, acrescentar a base e verificar
liderança/Influência, objetivo comum, confiança e preparação. Não disparar isso
em cada ataque individual. O líder pode aceitar uma condição para evitar uma
redução prevista (pp.86–88).

| Momento | Exemplos de efeito |
|---|---|
| Fim de cena | Remoções narrativas de condições, expiração das Chamas e de efeitos com esse prazo |
| Início de sessão | Acrescentar um ponto à pilha, preservando o saldo existente |
| Fim de sessão | Cada PJ escolhe aproximação, mudança de autoimagem ou afastamento; extras podem fazer verificações adicionais |
| Passagem de tempo | Reiniciar Equipe em um ponto e acionar extras pertinentes, como obrigações da Janus ou avaliação do Legado |

Passagem de tempo é um salto narrativo significativo, não cronômetro de tempo real,
troca de aba ou simples fim de rodada. Rodada do fórum, cena e sessão também não
são automaticamente equivalentes (pp.88–93, 185–186).

## Arquétipos: mapa para localizar os cuidados

Os detalhes operacionais e os campos são os do JSON validado e das fichas em inglês.
Este quadro não cria requisitos novos para a ficha.

| Arquétipo | Conflito e cuidado mecânico |
|---|---|
| Amaldiçoada | Destino terrível, nêmesis, trilha e sinais da Perdição, santuário. Começa com um sinal; não escolhe movimentos comuns na criação. Distinguir avanço da trilha, obtenção de sinal e chegada da perdição (pp.96–101). |
| Bruta | Arma viva com vínculos intensos; amor, rival e papéis do Coração. Amor não precisa ser romance, rival não precisa ser inimigo. Vínculos são definidos no contexto da sala na Torre (pp.102–107). |
| Delinquente | Rebeldia e necessidade de conexão. Interferência, truques e usos especiais de Equipe têm gatilhos próprios (pp.108–113). |
| Discípula | Treinamento e relação com mentoria. Habilidade compartilhada e exclusivas, Rótulo incorporado e negado, recursos; não presumir que todos os testes usam Superior (pp.114–119). |
| Farol | Pertencimento e entusiasmo com pouco poder. Impulsos são objetivos escolhidos, com benefício ao cumprir e renovação do conjunto. Equipamento e pet têm regras próprias (pp.120–125). |
| Forasteira | Lar versus pertencimento à Terra; contato com seu povo, controles, dispositivos e Nave Kirby. Pontos fortes/fracos e usos limitados não são só decoração (pp.126–131). |
| Janus | Vida heroica e civil, máscara e obrigações. A Máscara é movimento obrigatório; seu Rótulo pertence ao movimento. Trocar valores ao afirmar a identidade não é uma mudança comum de +1/−1 (pp.132–137). |
| Legado | Tradição, apoio e cobrança familiar. Conjunto de poderes com duas ausências iniciais; reação dos membros quando o tempo passa. Símbolo de autoridade tem −1 adiante na falha (pp.138–143). |
| Nova | Poder amplo com risco de dano colateral. Escolhe faíscas, não movimentos comuns; carregar gera reserva de Chamas, com custos e expiração na cena. Chamas e faíscas não são a mesma coisa (pp.144–149). |
| Transformado | Corpo visivelmente diferente e identidade humana. Eu sou o monstro oferece consequências negativas: em 10+ escolhe uma, em 7–9 duas. Eu não sou meu corpo reduz a contagem de condições do golpe físico, sem produzir contagem negativa (pp.150–155). |

Outros erros a evitar: Vamos, Sortudo! é bônus quando o pet ajuda, não adiante;
Poderes infinitos concede uso de habilidade, não compra de movimento. Não exigir
campos genéricos em todo arquétipo: seguir o que a ficha pede, mantendo opcionais
extras somente quando Alan tiver pedido.

## Crescimento e Hora da Verdade

No livro, cinco marcas de potencial dão um avanço e a trilha é limpa. **Na Torre,
a decisão é guardar potencial acumulado**, calculando ciclos de cinco e avanços
pendentes. Não confundir essa representação com uma regra de reiniciar o total
persistido (pp.92, 157–161; decisão em `fluxo_de_telas.md`).

Cada entrada de avanço é uma escolha individual, mesmo quando o texto se repete.
Depois de cinco escolhas da lista inicial, a lista tardia fica disponível; a inicial
continua acessível. A Torre registra 14 entradas (8 + 6), conforme validação do Alan.

A Hora da Verdade precisa ser desbloqueada. Seu uso dá controle narrativo conforme
o arquétipo, não controle sobre a vontade das outras PJs. Ao terminar, trava um
Rótulo e precisa de novo desbloqueio para outro uso (pp.49, 158–160).

Trocar arquétipo preserva Rótulos, condições, potencial e Influência. Movimentos,
habilidades e extras anteriores requerem avaliação do que permanece na ficção.
Na Torre, módulos retirados ficam inativos com seus dados; não apagar histórico.
Movimentos adultos não mudam automaticamente a maturidade. Aposentar ou virar
modelo encerra o papel de PJ; as consequências são tratadas pelo perfil e pelo
histórico, conforme as decisões de `fluxo_de_telas.md`.

## Mestre, PNJs, vilões e conflitos entre PJs

O mestre trabalha com objetivos, princípios e movimentos. Faz movimentos quando
a conversa pede uma resposta, há falha ou surge oportunidade na ficção. Um movimento
leve anuncia perigo e permite reação; um pesado concretiza consequências.
O sistema pode orientar essas escolhas, mas não decidir arbitrariamente a história
(pp.167–184).

PNJs precisam de identidade, um traço de comportamento e motivação. Vilões são
antagonistas capazes de representar ameaça real, não necessariamente pessoas
malignas. Podem ter de uma a cinco condições disponíveis e movimentos próprios.
Não aplicar automaticamente todas as regras de uma PJ a uma PNJ (pp.193–198).

Ao marcar condição num vilão, há uma reação de condição imediata, em geral
preparando novos perigos. Se precisar marcar outra sem espaço disponível, ele é
derrotado; pode fugir ou desistir antes disso. A ficção ainda decide imunidades,
vulnerabilidades e quando um inimigo é ameaça. Um grupo pode ser modelado como um
vilão coletivo, quando o conjunto realmente constitui perigo.

Conflitos entre PJs mantêm escolhas e holofotes para todos. Defender é a intervenção
direta prevista contra ameaça iminente; não vira um botão universal para impedir
qualquer interação. **A fila de declarações/resoluções da Torre é adaptação da mesa**,
especificada em `pvp.json`, apoiada nas distinções do livro (pp.191–192).

Ganchos conectam pessoas e pressões sobre a identidade de uma PJ; arcos organizam
ameaças, elenco e planos em fases. São preparação de possibilidades, não roteiro
obrigatório. Movimentos personalizados precisam de gatilhos específicos e efeitos
que continuem a história. As variantes experimentais do fim do livro não são
regras padrão a ativar sem decisão do Alan (pp.207–245).

## Como isso orienta a Torre e os helps

- A Torre é operada exclusivamente pelo mestre. Laboratório e instância da sala
  têm propósitos distintos; mudanças de jogo devem vir de comandos e eventos com
  origem e motivo. Modo teste é provisório, não a fonte definitiva da campanha.
- Ficha de criação só exige o que a ficha do arquétipo pede. O painel de controle
  administra os efeitos de jogo; os helps explicam as opções.
- Conteúdo de ajuda nasce como dados e texto próprio na Torre. As cartas antigas
  do acervo são referência de organização visual, não imagens a inserir no build.
- Primeiro fazemos painéis legíveis, com título, gatilho, rolagem quando existir,
  resultados, opções, custos, duração e orientações. As artes próprias ilustram.
- Depois o mesmo conteúdo pode alimentar cartas para download/impressão, com
  layout específico. Não usar o PNG de uma carta pequena como texto principal do
  help, nem manter duas versões independentes da regra.
- Estas são diretrizes de implementação; não afirmam que o motor, o painel de
  controle, os helps ou a exportação já estejam completos. Verificar o código.
