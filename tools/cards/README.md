# Gerador de cards da Torre

Execute da raiz (ou passe o caminho completo ao script):

```powershell
python tools/cards/gerar_cartas.py
```

Dependência: `python -m pip install -r tools/cards/requirements.txt`.
Não precisa instalar se o Python já tem Pillow.

Gera os quatro panoramas `00-*` diretamente em `Assets/img/cards/regras`.
Na geração padrão, também atualiza as cópias em `Assets/Resources/Cards/Regras`,
usadas automaticamente pelo painel de ajuda. Não copie os `.meta` entre as pastas.
Para conferir sem substituir os PNGs, use `--out-dir tmp/cards-preview`.
Os arquivos de mesmo nome são sobrescritos; os `.meta` da Unity ficam intactos.

O desenho é próprio, feito por código: moldura azul e dourada, faixas claras,
quebra de linhas e fonte ajustada. Não depende do acervo, de PSDs ou de templates
de imagem. Aproveita a organização do gerador pessoal de Alan, sem trazer o
baralho antigo ou textos da tradução. Arial no Windows; DejaVu Sans no Linux.

Todos os títulos, seções, explicações e origens das cartas ficam em
`Assets/Resources/Ajuda/catalogo.json`. `cards()` somente carrega e valida esse
catálogo; o Python cuida do desenho. O schema está em `docs/ajuda.md`.
`regras_ref` aponta aos JSONs executáveis e às decisões do projeto. Essas
referências não interpolam nem sincronizam automaticamente o texto: ao mudar
uma regra, revisar os helps relacionados. O potencial acumulado reflete a
decisão de `docs/fluxo_de_telas.md`.

Formato: 672 × 1050 px (mesma proporção 336 × 525 dos pocket cards). O gerador
falha se o texto não couber com a fonte mínima; não corta silenciosamente.
Sempre conferir os quatro PNGs visualmente após alterações.

O painel de ajuda atual exibe os PNGs. Futuros painéis de texto podem carregar
`Resources.Load<TextAsset>("Ajuda/catalogo")`, compartilhando o conteúdo do gerador.
A exportação para impressão em tamanho físico ainda não está implementada.
