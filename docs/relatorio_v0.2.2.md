# v0.2.2 — Confiabilidade

> 2026-10-16, fim do dia. A outra IA fez uma revisão geral da Torre (`docs/revisao/analise.md`) e uma
> segunda leitura depois dos primeiros consertos. Esta versão responde aos riscos de dados que ela apontou,
> enquanto o rolador espera a validação do resto da primeira luta.

## O fio

O Alan está segurando o rolador de propósito: primeiro valida as regras lance a lance, depois elas viram
código. Enquanto isso, o que não depende de regra de Masks podia andar. A revisão da outra IA deu a lista, e
eu conferi cada ponto no código antes de mexer (todos procediam).

## O que mudou e por quê

- **Gravação numa operação só** (`IGravacaoEmLote`, `ArmazenamentoLocalJson.GravarLote`): ficha, mesa e salas
  gravadas juntas como um lote, com diário. Ao abrir, a Torre termina um lote interrompido
  (`RecuperarLoteInterrompido`) ou descarta versões novas de um lote que nem começou a trocar. Antes, uma queda
  entre gravar `personagens.json` e `mesa_<sala>.json` deixava ficha e histórico de "fotos" diferentes.
- **A fila da gravação adiada não perde pendência**: só sai da fila o que gravou; se o disco falhar, o resto
  continua pendente e o erro aparece.
- **Cópia de segurança ao abrir a mesa** (`ICopiaDeSeguranca`): `dados/backups/<data> mesa <campanha>/`, uma
  por campanha por sessão (marcada só depois do sucesso; se falhar, avisa na mesa e tenta de novo), nunca
  sobrescreve outra, marca `COPIA_COMPLETA.txt` no fim; ficam as 15 últimas.
- **Desfazer respeita o limite dos Rótulos** (−2 a +3) e **Refazer devolve só o que o desfazer tirou de
  fato** (desfazer parcial por limite não vira subida fantasma ao refazer).

## Verificação

- Core: 165 testes no harness (dois novos: limite no desfazer, refazer exato).
- Armazenamento real: 5 testes num harness à parte (o assembly de testes do Unity não enxerga o Infra):
  lote completo, queda depois do diário, queda antes do diário, fila sobrevivendo a falha do disco, cópia sem
  sobrescrever. **Esses testes não estão no repositório** (ficaram no scratchpad da sessão).
- `tools/compilar.sh` acusa os dois arquivos novos de interface até o Unity abrir e regenerar os `.csproj`;
  compilado pelas cópias que incluem todos os arquivos. Os `.meta` dos arquivos novos o Unity cria ao abrir.

## O que não terminou

- **Desfazer com dependências** (avisar antes de confirmar quando uma mudança posterior interfere): entra com o
  "lance" do rolador, que vai agrupar as mudanças.
- **Testes do armazenamento dentro do projeto**: pede que o assembly de testes referencie o Infra.
- **Migração de versão** ao carregar, busca do baralho por id completo, categorias do help vindas do catálogo,
  verificador do catálogo de cartas.
- O rolador, quando o Alan terminar de validar o exemplo (falta: Estrela provoca, fuga, depois da luta).
