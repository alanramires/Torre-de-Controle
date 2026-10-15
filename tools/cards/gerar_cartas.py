#!/usr/bin/env python3
"""Cartas próprias da Torre. Execute de qualquer pasta: python tools/cards/gerar_cartas.py.

Adaptação da ideia do gerador pessoal de Alan: especificação CARDS, quebra de linhas,
faixas e ajuste de fonte. Não usa PNGs, textos, templates ou caminhos do acervo.
As regras vêm dos JSONs da Torre; descrições editoriais abaixo são texto próprio.
"""
import argparse
import json
import os
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
RULES = ROOT / 'Assets/Resources/Regras'
OUTPUT = ROOT / 'Assets/img/cards/regras'
SIZE = (672, 1050)  # mesma proporção pocket card, com texto de maior resolução
CATALOG = ROOT / 'Assets/Resources/Ajuda/catalogo.json'


def font(size, bold=False):
    filename = 'arialbd.ttf' if bold else 'arial.ttf'
    candidates = [Path(os.environ.get('WINDIR', 'C:/Windows')) / 'Fonts' / filename,
                  Path('/usr/share/fonts/truetype/dejavu') /
                  ('DejaVuSans-Bold.ttf' if bold else 'DejaVuSans.ttf')]
    for path in candidates:
        if path.exists():
            return ImageFont.truetype(str(path), size)
    raise RuntimeError('Fonte não encontrada. Instale Arial ou DejaVu Sans.')


def wrap(text, face, width):
    lines = []
    for paragraph in text.split('\n'):
        line = ''
        for word in paragraph.split():
            if face.getlength(word) > width:
                raise ValueError('Palavra maior que a área útil: ' + word)
            candidate = (line + ' ' + word).strip()
            if line and face.getlength(candidate) > width:
                lines.append(line)
                line = word
            else:
                line = candidate
        lines.append(line)
    return lines


def cards():
    catalog = json.loads(CATALOG.read_text(encoding='utf-8'))
    if catalog['versao'] != 1:
        raise ValueError('Vers?o de cat?logo n?o suportada.')
    result, ids, outputs = [], set(), set()
    for item in catalog['cartas']:
        if item['id'] in ids or item['arquivo'] in outputs:
            raise ValueError('ID ou arquivo repetido no cat?logo: ' + item['id'])
        ids.add(item['id'])
        outputs.add(item['arquivo'])
        if Path(item['arquivo']).name != item['arquivo'] or not item['arquivo'].endswith('.png'):
            raise ValueError('Arquivo deve ser um nome PNG simples.')
        for ref in item['regras_ref']:
            name, _, pointer = ref.partition('#')
            path = ROOT/name if name.startswith('docs/') else RULES/name
            if not path.is_file():
                raise ValueError('Refer?ncia ausente: ' + ref)
            if pointer:
                value = json.loads(path.read_text(encoding='utf-8'))
                for key in pointer.lstrip('/').split('/'):
                    value = value[int(key)] if isinstance(value, list) else value[key]
        if not item['secoes']:
            raise ValueError('Carta sem se??es: ' + item['id'])
        result.append(dict(out=item['arquivo'], title=item['titulo'],
                           compact=item.get('compacto', False), source=item['origem'],
                           sections=[(section['titulo'], section['texto']) for section in item['secoes']]))
    return result


def render(spec, destination):
    width, height = SIZE
    image = Image.new('RGB', SIZE, '#0d172a')
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle((12, 12, width-13, height-13), radius=28,
                           fill='#152238', outline='#dbb65d', width=4)
    draw.text((40, 32), 'TORRE DE CONTROLE / REFERÊNCIA', font=font(19, True), fill='#dbb65d')
    title_font = font(34, True)
    title_lines = wrap(spec['title'], title_font, width-80)
    y = 69
    for line in title_lines:
        draw.text((40, y), line, font=title_font, fill='white')
        y += 43
    start = y + 18
    bottom = height-95
    available = bottom-start
    sections = spec['sections']
    gap = 3 if spec.get('compact') else 6
    padding = 18 if spec.get('compact') else 27
    for size in range(29, 21, -1):
        body = font(size)
        heading = font(size, True)
        laid = [(wrap(t, heading, width-100), wrap(b, body, width-100)) for t, b in sections]
        heights = [(len(a)+len(b))*(size+gap)+padding for a,b in laid]
        if sum(heights) <= available:
            break
    else:
        raise ValueError(spec['out'] + ': conteúdo não cabe com fonte legível; enxugue o texto.')
    y = start
    extra = (available-sum(heights))//len(sections)
    for index, ((head, lines), section_height) in enumerate(zip(laid, heights)):
        section_height += extra
        draw.rectangle((32, y, width-33, y+section_height),
                       fill='#efe5d4' if index % 2 == 0 else '#dae3ed')
        text_y = y+10
        for line in head:
            draw.text((50, text_y), line, font=heading, fill='#234b73')
            text_y += size+gap
        for line in lines:
            draw.text((50, text_y), line, font=body, fill='#142037')
            text_y += size+gap
        assert text_y <= y+section_height
        y += section_height
    footer = wrap(spec['source'], font(17), width-80)
    for line in footer:
        draw.text((40, bottom+15), line, font=font(17), fill='#c0cada')
        bottom += 22
    destination.mkdir(parents=True, exist_ok=True)
    image.save(destination/spec['out'], optimize=True)
    print(f"{spec['out']}: {width}x{height}, fonte {size}px")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--out-dir', type=Path, default=OUTPUT)
    args = parser.parse_args()
    for spec in cards():
        render(spec, args.out_dir)
    if args.out_dir.resolve() == OUTPUT.resolve():
        import shutil
        runtime = ROOT / 'Assets/Resources/Cards/Regras'
        runtime.mkdir(parents=True, exist_ok=True)
        for spec in cards():
            shutil.copyfile(OUTPUT/spec['out'], runtime/spec['out'])


if __name__ == '__main__':
    main()
