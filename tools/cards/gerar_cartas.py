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
NAMES = {'aberracao': 'Aberração', 'perigoso': 'Perigoso', 'mundano': 'Mundano',
         'salvador': 'Salvador', 'superior': 'Superior'}


def read_rule(name):
    return json.loads((RULES / (name + '.json')).read_text(encoding='utf-8'))


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
    core = read_rule('nucleo')
    basics = read_rule('movimentos_basicos')['movimentos']
    influence = read_rule('influencia')
    if not any(m['id'] == 'influencia/rejeitar' for m in influence['movimentos']):
        raise ValueError('Regra de rejeitar Influência ausente; revisar o panorama.')
    overview = {
        'basico/enfrentar': 'Trocar golpes com uma ameaça real.',
        'basico/liberar-poderes': 'Ir além dos limites conhecidos dos poderes.',
        'basico/defender': 'Proteger alguém ou algo de perigo iminente.',
        'basico/avaliar': 'Investigar a situação para orientar sua ação.',
        'basico/provocar': 'Levar alguém suscetível a agir como você deseja.',
        'basico/confortar': 'Oferecer apoio emocional significativo.',
        'basico/perfurar-mascara': 'Entender intenções e sentimentos de alguém.',
        'basico/golpe-poderoso': 'Resolver o impacto de um golpe físico ou emocional.',
    }
    limit = core['modificador']
    ranges = {r['id']: r for r in core['rolagem']['faixas']}
    descriptions = {
        'aberracao': 'Como você percebe sua diferença, seus poderes e o que o torna incomum.',
        'perigoso': 'Como você percebe sua capacidade de ameaçar, ferir ou vencer pela força.',
        'mundano': 'Como você percebe sua humanidade e sua conexão com as outras pessoas.',
        'salvador': 'Como você percebe seu papel de proteger, resgatar e assumir responsabilidades.',
        'superior': 'Como você percebe sua inteligência e sua capacidade de entender e conduzir situações.',
    }
    return [
        dict(out='00-rotulos.png', title='RÓTULOS', sections=[
            (NAMES[k], descriptions[k]) for k in core['rotulos']['ids']
        ] + [('AUTOIMAGEM', 'Não são uma medida fixa de poder. As relações e os acontecimentos podem mudar seus valores.'),
             ('LIMITES', f"De {core['rotulos']['minimo']} a +{core['rotulos']['maximo']}. Na troca comum, um sobe e outro desce. Se houver estouro, nenhum muda: marque uma condição.")],
             source='Núcleo · livro pp.45–46 e 91'),
        dict(out='00-movimentos.png', title='SUCESSOS E FALHAS', sections=[
            ('ANTES DOS DADOS', 'Descreva a ação. Role quando ela acionar um movimento que pede dados; usar um poder nem sempre exige teste.'),
            ('ROLAGEM', f"{core['rolagem']['dados']} + modificador. Some Rótulo, bônus e penalidades aplicáveis. O modificador final fica entre {limit['minimo']} e +{limit['maximo']}."),
            (f"{ranges['forte']['de']}+", 'Use o resultado forte do movimento e qualquer efeito indicado para todo sucesso.'),
            (f"{ranges['parcial']['de']}–{ranges['parcial']['ate']}", 'Use o resultado parcial e os efeitos comuns ao sucesso. Respeite escolhas e custos.'),
            (f"{ranges['falha']['ate']} OU MENOS", 'Marque potencial. Siga a falha específica; quando não houver, o mestre faz um movimento conforme a ficção.'),
            ('ATENÇÃO', 'Tomar um golpe poderoso é invertido: uma falha permite resistir. Equipe e outros efeitos podem mudar o resultado depois dos dados, antes da resolução.')],
             source='Núcleo e movimentos básicos · livro pp.32–35, 76–78 e 84'),
        dict(out='00-lista-movimentos.png', title='MOVIMENTOS', compact=True, sections=[
            (m['nome'], overview[m['id']]) for m in basics
        ] + [('Rejeitar Influência', 'Resistir a quem tem Influência sobre você, seja PJ ou PNJ.')],
             source='Movimentos básicos e Influência · livro pp.57–83'),
        dict(out='00-controles.png', title='RECURSOS E BÔNUS', sections=[
            ('CONTROLE', core['bonus']['controle'].capitalize() + '. Confira no efeito o que pode comprar e quando a reserva termina.'),
            ('ADIANTE', core['bonus']['adiante'] + '. Não se aplica a qualquer teste: respeite o alvo e a situação.'),
            ('CONSTANTE', core['bonus']['constante'] + '. Registre a circunstância que mantém o bônus ativo.'),
            ('POTENCIAL', f"Falhas e alguns efeitos rendem potencial. Cada ciclo de {core['potencial']['trilha']} pontos concede um avanço, não um nível. Na Torre, o total é acumulado; a ficha mostra o progresso do ciclo."),
            ('CHAMAS DO NOVA', 'Reserva própria do arquétipo: carregar gera Chamas, gastas nas faíscas escolhidas. Elas expiram no fim da cena; não são um bônus genérico.')],
             source='Núcleo, Nova e decisão da Torre · livro pp.56, 92 e 146–148'),
    ]


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


if __name__ == '__main__':
    main()
