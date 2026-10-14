# v0.0.2 — Salas, instâncias e o ciclo da ficha

> 2026-10-13, segunda metade do dia. Da ficha solta no laboratório à personagem jogando numa sala.

## O fio

A v0.0.1 terminou com uma ficha completa, mas sem lugar para jogar. Esta versão deu à ficha um
**caminho**: nasce no laboratório, é copiada para a vaga de uma sala, passa pela aprovação do mestre
e entra em carreira. Quase todas as decisões do dia são sobre **onde a ficha está e o que se pode
fazer com ela ali**: o mesmo clique (voltar para novo, editar, aprovar) vale numa situação e não em
outra.

## 1. Salas, vagas e instâncias

- **Sala** com 1 a 12 vagas ("slot" por dentro, "vaga" na tela, pedido do Alan). Um jogador por sala.
- **Adicionar personagem copia** a ficha do laboratório (a instância do Arkham): "só a criação" ou
  "criação + jogo". A original fica intacta e pode ir para outras salas. A lista da vaga filtra pelas
  fichas do jogador da vaga.
- **Tirar da vaga não apaga**: a cópia vai para "vagas liberadas", o arquivo da sala, só consulta
  ("acervo para a eternidade da ficha errada ou dos jogadores que saíram").
- No laboratório, "Ficha" de uma personagem com instâncias pergunta qual abrir; o título da ficha da
  instância mostra a campanha.

**Por quê:** o laboratório é experimento; a sala é onde o mestre é suserano. Copiar (em vez de mover)
deixa errar no laboratório sem quebrar a mesa, e a mesa ganha histórico próprio.

## 2. O ciclo da ficha (decidido pelo Alan)

```
laboratório:  novo ⇄ em aprovação
sala:         em aprovação ⇄ em revisão
              em aprovação → em carreira → aposentado   (sem volta)
```

- **Em aprovação**: concluída no laboratório (foto congelada), mesmo sem a parte da equipe, que só
  existe com colegas. A instância chega assim na vaga e completa a equipe na campanha.
- **Em revisão**: a instância reaberta para corrigir a criação. Fica na vaga e conclui de volta para
  em aprovação. Só existe na sala, por isso é status próprio (não "novo").
- **Em carreira**: o mestre aprova na campanha. **Não volta.** Erro descoberto tarde vira ajuste do
  mestre com motivo (anotado em `implementacao_futura.md`).
- **Aposentado**: saiu da vaga (e, no futuro, o 15º avanço).

**Por quê:** o "voltar para novo" solto na lista do laboratório deixava corrigir a original achando que
mudava a mesa. Agora a correção está **atrelada à instância que se olha**, e o que já joga só muda por
evento com motivo.

## 3. NPCs

Dentro da sala, numa aba própria (para não poluir as vagas). Ficha simples do livro: aparência marcante,
modo de agir, motivação; papel Vilão abre geração, movimentos e 1–5 condições. "Algo bobo, sem
protagonismo" (Alan), e é isso.

## 4. Fotos por link, com moderação

Foto de herói e civil por **link** (.jpg/.jpeg/.png). O mestre **aprova ou recusa vendo a imagem**, na
instância da sala; trocar o link volta a "aguardando". Sem link, vale o retrato do arquétipo.

**Princípio que saiu daqui:** a Torre é só do mestre hoje, mas deve **se comportar como se um dia fosse
pública**. Por isso conteúdo que vem do jogador já passa por moderação, e duas fotos (não galeria).

## 5. A mesa, só no Core

`Core/Mesa`: rodada corrente e log de eventos por sala; comandos com rodada e motivo (potencial,
condição, mudar Rótulos com o estouro do livro, corrigir lançando o inverso). Testado, **sem tela**: o
Alan pediu só o botão "Área do mestre" por enquanto.

## ⚠ Achados que contradizem o que se acreditava

- **Toda ficha concluída perdia os movimentos escolhidos** desde a v0.0.1. `Aprovar` punha a foto
  vazia na personagem antes de copiar os movimentos, e `MovimentosNaFicha` lê da foto quando ela
  existe. O teste de concluir usava a **Nova**, que não escolhe movimentos, e passava. Lição: teste de
  regra comum precisa de um arquétipo que exercite a regra.
- **O repositório usa Git LFS.** Eu disse ao Alan que a arte (30 MB) engordava o histórico; o push
  mostrou "Uploading LFS objects". O histórico guarda ponteiros; o peso vai para a cota do LFS.
- **`tools/compilar.sh` verde não prova a tela.** Um `Image` posto no mesmo objeto de um texto TMP
  compila, mas vem nulo em tempo de execução (um objeto aceita um gráfico só) e a ficha abria em
  branco. Só o log da Unity (`Logs/Editor.log`) mostrou.
- **Dois painéis empilhados disputavam o Tab**: a lista atrás da ficha roubava o foco para o "novo
  codinome". Só o último painel ligado responde agora.

## Erros meus

- **Adiantei a regra da Influência** a partir do JSON, sem o Alan explicar como imagina; ele parou
  ("calma, eu não disse como influência vai funcionar"). O código ficou como rascunho, sem travar nada.
- `Does.Contain(objeto)` funciona no NUnit novo do harness e não no da Unity (só aceita texto). Usar
  `Has.Member`.
- Imagem fora de `Resources` não carrega por caminho: quase movi os arquivos da outra IA; ficou uma
  lista no Inspector, e depois ela mesma passou para `Resources/Retratos`.

## Trabalho em paralelo com outra IA

O Alan pôs outra IA nas imagens (retratos, ícones, fotos) enquanto eu fico na mecânica, **na mesma
pasta**. Funcionou, mas: arquivos dela aparecem sem aviso (ler o diff antes de commitar), commits
precisam incluir o código dela quando o meu depende dele, e os `icons8-*` (licença que não deixa
redistribuir) ficaram fora do repositório público.

## O que não terminou

- **Segunda etapa (equipe na campanha)**: `CadastroPersonagens.Equipe.cs` tem a resposta da equipe, os
  relacionamentos com colegas e um **rascunho** da Influência inicial. Sem tela e **sem travar** a
  aprovação. Esperando o Alan explicar a Influência.
- **Área do mestre**: o botão existe; a tela (rodada, cartões das personagens, log, corrigir) não.
- **Aposentar no 15º avanço** e **ajuste do mestre em ficha em carreira**: anotados, não feitos.
- **Validação por arquétipo** continua: Bruta e Delinquente sem validação; Discípula, Legado e
  Transformado sem revisão.
- **Rolador, colheita em BBCode, Equipe (pilha), Influência no jogo**: nada ainda.
- **Schema**: os status mudaram (`criado` deixou de existir) e os dados locais foram convertidos à mão
  (backup em `dados/personagens.antes-dos-status.json`). Ainda sem campo `versao` nem migração.
