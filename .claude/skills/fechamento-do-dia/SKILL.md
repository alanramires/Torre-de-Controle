---
name: fechamento-do-dia
description: Fecha o dia de trabalho na Torre de Controle — escreve o relatório da versão, faz os commits separados por frente, cria e publica a tag, e atualiza o resumo de retomada. Use quando o autor disser "vamos fazer o fechamento do dia", "fechar o dia", "encerrar o dia" ou pedir relatório + tag + push.
---

# Fechamento do dia

Ritual do autor, herdado do The Map Room. Ordem fixa. **Não pule passos e não
inverta a ordem** — ela existe porque cada inversão já custou algo.

## 0. Antes de tudo: levantar o que aconteceu

```bash
git log --oneline <última-tag>..HEAD   # sem tag ainda: git log --oneline
git status --porcelain
git diff --stat
bash tools/compilar.sh                 # se já existir
```

Se houver trabalho do autor na árvore que você não acompanhou (é comum — ele
mexe no Unity em paralelo), **leia o diff antes de escrever qualquer coisa sobre
ele**. Nunca descreva no relatório ou no commit algo que você não verificou.
"Verificar antes de documentar" é regra do projeto.

Pergunte ao autor o que você não conseguir atribuir. Arquivo órfão costuma
pertencer a uma frente por um motivo não óbvio.

Se não compila, o dia não fecha: avise o autor antes de seguir.

### O repositório é público

`github.com/alanramires/Torre-de-Controle` é **público**. Antes de qualquer
commit, confira que nada do acervo entrou na árvore: PNGs das cartas, PSDs, arte
dos arquétipos, o PDF do livro, fichas dos jogadores. Esse material fica em
`D:/Acervo do RPG/...` e é lido pelo caminho configurado, nunca copiado para cá.
Na dúvida sobre um arquivo, pergunte.

## 1. Escolher o número da versão

`vX.Y.Z`, definido pelo autor:

| dígito | significa |
|---|---|
| **X** | grande mudança de arquitetura |
| **Y** | mudança localizada importante — uma parte e seus filhos (o motor de regras, a mesa, o rolador, a exportação) |
| **Z** | salvamento de fim de trabalho |

Na dúvida entre Y e Z, **pergunte**. Não escolha sozinho.

## 2. Escrever `docs/relatorio_vX.Y.Z.md`

O relatório explica **o porquê**, não o quê. O `git log` já diz o quê.

- Título curto que capture o fio do dia, não uma lista.
- Uma seção por frente de trabalho.
- **O que não terminou** tem seção própria. Relatório que só conta vitória é
  inútil para retomar.
- Achado que contradiz o que se acreditava vale mais que feature entregue —
  escreva com destaque. Vale também para regra de Masks: se o livro desmentiu
  uma suposição, registre e avise que o `CLAUDE.md` do acervo talvez precise
  da correção.
- Se você errou uma hipótese e a medição desmentiu, **registre isso**. É o tipo
  de coisa que a próxima sessão repete se não estiver escrita.

Adicione a linha correspondente na tabela de versões do `CHANGELOG.md` (crie o
arquivo no primeiro fechamento).

## 3. Commits separados por frente de trabalho

**Um commit por frente, não um commit do lote inteiro.** Stage arquivo por
arquivo, nunca `git add .` neste passo.

O ganho: reverter uma frente sem tocar nas outras. O ganho oculto: obriga a
olhar cada arquivo uma vez, e é assim que se descobre que um arquivo pertence a
uma frente inesperada.

Mensagem de commit no padrão do repo: explica o porquê, cita o arquivo e a linha
quando ajuda, e diz o que **não** mudou de propósito.

Termine as mensagens com a linha de coautoria que o harness indicar na sessão
(hoje: `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`).

## 4. `git add .` — só agora, e só para o churn

O que sobrou é reserialização do Unity Editor: cena, atlas de fonte, `.asset`
tocados por reimport, `ProjectSettings`.

Commite com mensagem dizendo **que é churn** e que não há mudança de design
intencional. Se você não tiver certeza de que é churn, pergunte ao autor em vez
de afirmar.

Este é o único passo em que `git add .` é permitido — e a regra do repositório
público continua valendo: olhe o que entrou.

## 5. Tag — depois de tudo commitado

```bash
git tag -a vX.Y.Z -m "<o título do relatório>"
```

**A tag é a última coisa antes do push.** Criar antes do commit final obriga a
mover uma referência já publicada com `--force`, que sobrescreve o que outros
possam ter puxado. Já aconteceu no Map Room; não repita.

## 6. Push do commit e da tag

```bash
git push origin main
git push origin vX.Y.Z
```

## 7. Atualizar `docs/resumo.md`

O handoff. **Resumo desatualizado é pior que resumo nenhum** — uma conversa nova
acredita nele.

Vai depois da tag, e num commit próprio: ele descreve o estado *pós-versão*, não
faz parte dela.

Atualize:

- **Estado** — a versão nova e a descoberta que organiza o que vem
- **Onde eu parei** — o que ficou pela metade, com nomes de arquivo
- **Armadilhas** — toda que custou tempo hoje entra na tabela
- **MVP** — quais itens do MVP do `CLAUDE.md` já estão de pé e quais faltam
- **Schema da ficha** — se a `versao` mudou, qual migração existe

Corte o que virou obsoleto. O resumo cresce se ninguém podar.

## Critério de pronto

- [ ] compila (`tools/compilar.sh`, quando existir)
- [ ] nenhum material do acervo na árvore
- [ ] `git status` limpo
- [ ] relatório existe e tem seção do que não terminou
- [ ] `CHANGELOG.md` tem a linha da versão
- [ ] um commit por frente, mais um de churn
- [ ] tag criada **depois** do último commit, e pushada
- [ ] `docs/resumo.md` descreve o estado de agora

Relate ao autor o que ficou pendente e o que você não conseguiu verificar.
