# Laboratório de NPCs, tela de NPCs e Influência com NPCs

Debatido com o Alan em 2026-10-14. **Decidido, ainda não programado.**
O que já existe está em "Ponto de partida"; o resto é o plano.

## Ponto de partida (já feito)

- NPC é a mesma entidade da personagem, com `Controle.Mestre`, perfil PNJ (modo de agir, motivação) e,
  se o papel for Vilão, a ficha de vilão (geração, movimentos, condições).
- Papéis de NPC: **Neutro / Aliado / Vilão**. `Heroi` é só das PJs. Trocar o papel guarda a ficha de vilão.
- Hoje todo NPC nasce numa sala: criado pelo mestre na aba NPCs, ou **nascido da ficha** de uma heroína
  (nêmesis, Legado, mentora, perguntas de histórico, obrigações do Janus, impulsos do Farol), com o
  vínculo `dona · laço`. A regra fica em `arquetipos.json` (`npcs`, `npcs_historico`, `por_escolha`).

## Laboratório de NPCs

Igual ao laboratório das heroínas: **originais** no laboratório, **instâncias** nas salas.

- Dá para criar um NPC no laboratório antes de ter campanha para ele (a vilã que ainda não tem onde morar).
- Pôr na sala cria uma instância independente, que guarda de onde veio (`Origem`, como nas heroínas).
- **Reboot entre campanhas:** copiar um NPC de uma sala para outra também cria uma instância independente,
  com origem "reboot de Lady Faust (Halcyon City)". Mexer numa não mexe na outra.
- Pode-se copiar o mesmo NPC várias vezes para a mesma sala, com aviso de nome igual.

### O que a cópia leva (o que o NPC é)

Nome e nome de vilão, foto, aparência, modo de agir, motivação, papel, maturidade, a ficha de vilão inteira
e as **Notas do mestre** (vão junto; o mestre apaga o que for só da campanha antiga).

### O que fica para trás (o que aconteceu com ele)

- **Influências:** a cópia nasce no estado original, sem nenhuma.
- **Vínculo com a heroína:** a Eclipse copiada não é nêmesis de ninguém na campanha nova; vira NPC livre do mestre.
- **Eventos da mesa:** o log fica na campanha de origem.

### Depois: ligar a um NPC que já existe

Se na campanha nova uma Amaldiçoada quiser *a mesma* Eclipse como nêmesis, a caixa de nome deve permitir
"ligar a um NPC que já existe" em vez de criar outro. Muda o jeito da caixa de nome; fica para depois.

## Tela de NPCs (gestão de todas as campanhas)

- Lista de todos os NPCs, do laboratório e de todas as salas, agrupada por campanha.
- Filtros: campanha, papel, ativo/inativo, origem ("da ficha de alguém" ou "criado pelo mestre"), busca por nome.
- Ações por linha: abrir a ficha, **copiar para…** (sala de destino), inativar.
- A aba NPCs da sala continua: é a visão do dia a dia daquela campanha.

## Influência com NPCs

O que o livro diz (conferido no texto):

- Adultos têm Influência sobre as PJs por definição; com o tempo a PJ pode se livrar dela.
  NPC jovem começa sem (o livro manda perguntar aos jogadores).
- PJ ganha Influência sobre NPC: **Defender** (de quem protegeu), **Provocar** contra NPC no **7–9**
  (o NPC "exagera"), **Perfurar a máscara** (perguntar como ganhar e cumprir).
- Segunda Influência sobre NPC: +1 adiante contra ele. Sobre PJ: muda os Rótulos dela.
- Perder: Rejeitar Influência no 10+ ("cancele a Influência e ganhe +1 adiante"); Renunciar gasta a que se tem.

Decisões:

1. **Uma regra só:** cada personagem (PJ ou NPC) guarda quem tem Influência sobre ela, no mesmo modelo
   dada/perdida que as PJs já usam entre colegas. A leitura reversa ("exerce Influência sobre") vale igual.
2. **Adulto por padrão, calculado:** o adulto tem Influência sobre as PJs da sala a menos que esteja marcado
   que perdeu. Só as exceções são gravadas. PJ nova na sala já fica sob os adultos; trocar a maturidade
   do NPC muda tudo junto.
3. **Só o mestre muda, sempre como evento da mesa**, com motivo ("Provocar 8, Doutor Ocaso exagerou").
   A jogadora só lê.
4. **Ficha da PJ (Núcleo), só leitura:** "NPCs com Influência sobre você" e "Você tem Influência sobre",
   listando os NPCs ligados a ela e as exceções explícitas, mais a linha
   "e todo adulto da sala, por padrão (menos: …)". Não lista os 15 adultos um por um.
5. **Ficha do NPC:** as duas listas, com as PJs, e as ações do mestre (pela mesa).
6. **Sem Influência entre NPCs.**
7. **Influência oculta:** marca "só o mestre vê" (o vilão disfarçado de professor).
8. **Grade NPC × PJ** por campanha, com os dois sentidos em cada célula: o painel do mestre.

9. **Influência de nascença, pela regra:** o NPC nascido da ficha pode já nascer com Influência num dos
   dois sentidos, marcado no `arquetipos.json` junto do resto da regra dele (ex.: `"influencia": "da_dona"`).
   A irmã novinha do Legado ("próximo da linhagem") nasce com a **dona tendo Influência sobre ela**: ela olha
   para a irmã mais velha, por mais pentelha que seja. O padrão é não nascer com nada além do que a maturidade
   já dá (adulto sobre as PJs). Quais outros NPCs nascem assim se decide caso a caso, quando chegar a hora.
