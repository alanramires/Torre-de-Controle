# Fluxo de telas e modelo de contas, salas e personagens

> Escrito em 2026-10-13. Complementa `docs/proposta.md`.

## Entidades

| Entidade | O que é | Onde vive |
|---|---|---|
| **Conta** | nome, e-mail, senha | central de contas (simulada na fase 1) |
| **Modelo de personagem** | o "cartão" da personagem: arquétipo e escolhas de criação | repositório da conta |
| **Sala** | uma campanha: mestre, jogadores, instâncias, NPCs, pilha de Equipe, log | central de salas |
| **Instância** | a personagem sentada na mesa | dentro da sala |
| **NPC** | personagem do mestre, modelo leve | dentro da sala |

### Modelo × instância (como no Arkham Horror)

Escolher uma personagem do repositório e sentar na sala **cria uma instância**: uma cópia que começa
do zero (nível 1). Todo o progresso (potencial, Rótulos, condições, avanços, Influência) acontece **na
instância**, dentro da sala. O modelo no repositório **não muda**, e o mesmo modelo pode estar em
várias salas ao mesmo tempo, cada instância com a sua história.

Excluir ou alterar o modelo não afeta instâncias já criadas.

### NPC é outro modelo

Pelo livro, NPC nasce com **motivação simples e objetivo pequeno** ("quero namorar o personagem do
jogador X", "quero encontrar meu pai"). Não tem a ficha de um PJ. Modelo mínimo: nome, descrição,
motivação/objetivo, e as relações de Influência com os PJs. Vilões e condições de vilão ficam para depois.

**NPC que vira PJ é outra coisa**: a promoção cria uma **ficha de PJ nova** (instância) a partir do NPC,
atribuída a um jogador. Não é o mesmo objeto mudando de tipo.

## Central de contas simulada (fase 1)

Sem Firebase por enquanto. Uma central local que aceita **senha `1` para qualquer conta**.
Fica atrás de uma interface (`IAutenticacao`), e na fase 2 entra o serviço real (Supabase, Firebase ou
UGS Authentication). Ninguém fora dessa implementação sabe que as contas são de mentira.

A central simulada **não pode ir para o build público**: o build da fase 3 falha se ela estiver incluída.

## Telas

### Tela de entrada (deslogado)

- Criar conta (nome, e-mail, senha)
- Entrar na conta

### Logado (fora de sala)

- Criar sala (você é o mestre dela)
- Entrar em sala como mestre
- Entrar em sala como jogador
- Repositório: criar, alterar e excluir modelos de personagem
- Sair da conta

### Na sala, como mestre

- Criar, alterar e excluir NPCs
- Criar personagem e atribuir a um jogador
- Alterar qualquer instância da sala (o escudo do mestre)
- Reatribuir instância: passar para outro jogador ou assumir (jogador AFK)
- Promover NPC a PJ (cria instância nova)
- Ver quem está na sala, aprovar pedidos para jogar, remover jogadores
- Aprovar fichas (com a validação da Torre apontando o que fere as regras do arquétipo)

### Na sala, como jogador

- Pedir para jogar (o mestre aprova)
- Criar personagem para esta sala (nasce direto como instância; vai para aprovação do mestre)
- Escolher um modelo do repositório (cria instância)
- Ver a própria ficha, anotar, consultar o help das cartas
- Rolar dados: previsto, **desligado no MVP** (só o mestre rola)

## Decidido (2026-10-13)

1. **Aprovação**: o jogador monta a ficha da instância e **o mestre aprova**. A Torre ajuda com
   **validação de ficha** (cada arquétipo tem regras próprias para os Rótulos iniciais e as escolhas
   de criação); a validação aponta o problema, quem decide é o mestre. Depois de aprovada, só o mestre edita.
2. **Entrar na sala é livre; jogar precisa de aprovação do mestre.** Quem entra sem aprovação vê só o
   que é público da sala.
3. Co-mestre, mais de uma instância por jogador e trazer personagem de outra campanha: **depois**
   (ver `docs/implementacao_futura.md`).

## Em aberto

1. **O mestre joga na própria sala?** (sugestão: não; ele já controla NPCs e pode assumir instâncias)
2. Acabamento: recuperar senha, editar/excluir conta, renomear/arquivar/excluir sala.
