# Gerador de cards da Torre

Execute da raiz (ou passe o caminho completo ao script):

```powershell
python tools/cards/gerar_cartas.py
```

Dependência: `python -m pip install -r tools/cards/requirements.txt`.
Não precisa instalar se o Python já tem Pillow.

Gera os quatro panoramas `00-*` diretamente em `Assets/img/cards/regras`.
Para conferir sem substituir os PNGs, use `--out-dir tmp/cards-preview`.
Os arquivos de mesmo nome são sobrescritos; os `.meta` da Unity ficam intactos.

O desenho é próprio, feito por código: moldura azul e dourada, faixas claras,
quebra de linhas e fonte ajustada. Não depende do acervo, de PSDs ou de templates
de imagem. Aproveita a organização do gerador pessoal de Alan, sem trazer o
baralho antigo ou textos da tradução. Arial no Windows; DejaVu Sans no Linux.

As especificações editoriais ficam em `cards()`. Os panoramas consultam os JSONs
de regras da Torre para nomes, gatilhos, limites e bônus; explicações próprias
complementam os dados. A descrição de Chamas e as autoimagens dos Rótulos ainda
são editoriais: conferir quando as regras correspondentes mudarem. O potencial
acumulado reflete a decisão de `docs/fluxo_de_telas.md`.

Formato: 672 × 1050 px (mesma proporção 336 × 525 dos pocket cards). O gerador
falha se o texto não couber com a fonte mínima; não corta silenciosamente.
Sempre conferir os quatro PNGs visualmente após alterações.

Isso gera assets de referência, ainda sem menu de ajuda ou exportação para
impressão em tamanho físico. Os futuros painéis podem consumir os mesmos JSONs.
