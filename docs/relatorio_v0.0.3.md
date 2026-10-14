# v0.0.3 — Ficha ágil, exportável e corrigível

> 2026-10-13/14, a madrugada depois da v0.0.2. O Alan chegou onde queria: uma sala com 5 personagens
> "em aprovação". O caminho até lá foi polir a ficha com ele usando de verdade.

## O fio

A v0.0.2 deu caminho à ficha; esta versão foi o **uso real** desse caminho. O Alan preencheu fichas
completas (Estrela Negra, Garça Escarlate, Titânia...), mandou para outra IA conferir, e cada atrito
virou uma correção: travava ao clicar, perdia um texto, o rodapé não cabia, o balão abria para fora da
tela. Quase nada aqui é parte nova; é a ficha ficando confiável e rápida.

## 1. Agilidade ("eu prezo pela agilidade do sistema")

O Alan sentiu engasgos ao marcar habilidades. Duas causas, duas correções:
- **Cada clique refazia a aba inteira** (chips, campos de texto, fotos, layout duas vezes). Agora o clique
  refaz só o **bloco** onde aconteceu (`SecaoRefazivel`: habilidades, movimentos, extras); fora de um
  bloco marcado, refaz tudo como antes, por segurança.
- **Cada clique gravava o arquivo inteiro.** Agora a gravação é **adiada** (`ArmazenamentoAdiado`, na
  Infra): cliques seguidos viram uma gravação, meio segundo depois do último, e o JSON só é gerado nessa
  hora. Grava na hora ao perder o foco, sair do Play ou fechar.

## 2. Exportar e importar (para outras máquinas analisarem)

Envelope com `formato` e `versao` em volta da ficha (`CadastroPersonagens.Troca`); arquivos em
`dados/exportacoes` e `dados/importar` (`ArquivosDeTroca`, Infra). Dois importar com papéis diferentes:
- **na lista**: traz como personagem **nova** no laboratório (sem a campanha de lá);
- **na ficha**: o caminho de volta, **por cima da mesma ficha** (fica o que é desta Torre: id, sala,
  jogador, status).

Prova de ida e volta: exportar → importar → exportar deu o mesmo JSON (o Alan conferiu).

## 3. O mestre corrige o que passou despercebido

O "voltar para novo" virou um botão explícito na aba **Mestre** (era um clique escondido no status). E a
regra mudou, a pedido do Alan: a instância **em carreira** também vai para **em revisão** (fica na vaga,
guarda o jogo, refaz a criação, volta a em aprovação). Só aposentado não volta. Do laboratório, a
instância continua só consulta ("hierarquia", nas palavras dele).

## 4. A ficha que a mesa vai mostrar

- Abas reordenadas e renomeadas: Identidade · Núcleo · História · Módulos · Evolução · Mestre.
- Descrições opcionais por item em mais listas (santuário, recursos da mentora, faíscas, obrigações da
  Janus) e o campo de conflitos da Discípula.
- 27 movimentos ganharam o balão (`regra`), e um teste agora exige `regra` em todo movimento.
- Faíscas do Nova reescritas no fio da ficha em inglês; Sobrecarga deixa explícito "sem rolar os dados".
- Rodapé numa linha com a lista de pendências no balão; balões abrem para cima quando não cabem;
  descrições longas e legendas de seção cortam com reticências e mostram o resto no balão.
- Equipamento do Farol virou campo **de jogo** (começa vazio: "whenever you pick up").

## ⚠ Achados que contradizem o que se acreditava

- **As descrições trocadas do santuário não eram bug de gravação.** Cada caixa gravava na chave certa;
  as caixas apareciam na **ordem da lista do JSON**, e o jogador preenche na **ordem em que marcou**.
  Agora seguem a ordem da marcação (a mesma do JSON exportado). Ordem visual é dado também.
- **Gravar só ao sair do campo perde texto.** A Absorção de vitalidade nunca foi gravada. Com a gravação
  adiada, gravar a cada tecla ficou barato e passou a ser o padrão nos campos de descrição.
- **O projeto entra no Play sem recarregar o domínio** (Enter Play Mode Options). Estáticos sobrevivem
  entre Plays; as texturas baixadas não. O cache do `FotoDaWeb` devolvia textura destruída e a ficha
  abria em branco. Regra: todo cache estático limpa com `[RuntimeInitializeOnLoadMethod]`.
- **"Sem poderes e quase sem treino" começa vazio.** Eu tinha modelado o equipamento como exigência da
  criação; a ficha diz "whenever you pick up a new piece of gear".

## Erros meus

- Tooltips telegráficos ("1 chama: ...") que, lado a lado com o PDF, pareciam outra regra.
- Uma troca por script inseriu um bloco duas vezes (o trecho buscado continuava dentro do novo); o
  compilador pegou.
- Um `testhost` preso de uma execução em segundo plano travou o `dotnet test` (a dll ficou bloqueada).

## O que não terminou

- **Aprovação na campanha** (em aprovação → em carreira): a sala vai cobrar história do time,
  Influências, relacionamentos e **NPCs que orbitam as personagens, criados pelos jogadores** (ideia do
  Alan). Registrado em `fluxo_de_telas.md`. **Esperar o Alan explicar a Influência** e desenhar os NPCs.
- **Área do mestre** (tela da mesa): só o botão; o Core está pronto desde a v0.0.2.
- **Ajuste pontual com motivo** em ficha em carreira (sem reabrir a criação inteira).
- Resultado sem rolagem (Sobrecarga 10+, Vou te salvar! 12+) para o futuro rolador.
- Validação dos arquétipos: Bruta, Delinquente, Discípula, Legado e Transformado ainda com o Alan.
