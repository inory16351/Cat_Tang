# 4장 기획서 Ver01(사용자가 손으로 고친 판) → Ver02 (2026-11-20)
# 사용자가 고친 글은 그대로 두고 아래만 바꿈:
#  - 슬라이드 11 진자 운동: 그림 속 '틱 / 톡' → '왼쪽 / 오른쪽' (사용자가 규칙 글을 왼쪽 / 오른쪽으로 바꿈, 방 탑뷰에서 틱 = 왼쪽 반, 톡 = 오른쪽 반)
#  - 슬라이드 10 · 14: 남은 '틱-톡 박자' / '틱-톡' → '진자 운동' (사용자가 바꾼 기믹 이름에 맞춤)
#  - 슬라이드 10 이동: 글리치 순간이동 (사용자: 지직거리는 글리치와 함께 전광석화처럼 사라졌다 나타나며 이동, 갑자기 고양이 앞에 나타나 공격)
#   python docs/tools/edit_ch4_ver02.py
import os
from pptx import Presentation
from pptx.util import Inches

D = os.path.normpath(os.path.join(os.path.dirname(__file__), '..'))
SRC = os.path.join(D, '1159_4장_기획서_20261119_정승운_Ver01.pptx')
DST = os.path.join(D, '1159_4장_기획서_20261120_정승운_Ver02.pptx')

p = Presentation(SRC)


def set_text(shape, old, new):
    """문단 · 런 서식은 그대로, 첫 런 글만 바꾸고 나머지 런은 비움"""
    tf = shape.text_frame
    assert tf.text == old, (tf.text, old)
    runs = [r for para in tf.paragraphs for r in para.runs]
    runs[0].text = new
    for r in runs[1:]:
        r.text = ''


def replace_in(shape, old, new):
    for para in shape.text_frame.paragraphs:
        for r in para.runs:
            if old in r.text:
                r.text = r.text.replace(old, new)
                return True
    return False


# 슬라이드 11
s = p.slides[10]
for sh in s.shapes:
    if not sh.has_text_frame:
        continue
    t = sh.text_frame.text
    if t == '틱':
        set_text(sh, '틱', '왼쪽')
    elif t == '톡':
        set_text(sh, '톡', '오른쪽')
# 방 탑뷰 큰 글자(틱 · 톡) 상자는 한 글자 폭이라 넓힘 (왼쪽 글자는 그 자리, 오른쪽 글자는 오른쪽 끝을 맞춤)
big_l = next(sh for sh in s.shapes if sh.name == 'text_255')
big_r = next(sh for sh in s.shapes if sh.name == 'text_256')
right_edge = big_r.left + big_r.width
big_l.width = Inches(1.8)
big_r.width = Inches(1.8)
big_r.left = right_edge - big_r.width
from pptx.enum.text import PP_ALIGN
for para in big_r.text_frame.paragraphs:
    para.alignment = PP_ALIGN.RIGHT

# 슬라이드 10
s = p.slides[9]
for sh in s.shapes:
    if not sh.has_text_frame:
        continue
    t = sh.text_frame.text
    if t == '진자처럼 좌우로 흔들리듯 걸으며 다가옴 (이속 3.5m/초)':
        set_text(sh, t, '지직거리는 글리치와 함께 사라졌다 나타나며 이동 / 수시로 고양이 바로 앞에 나타나 기습 (이속 3.5m/초)')
    elif '틱-톡 박자' in t:
        assert replace_in(sh, '틱-톡 박자', '진자 운동')

# 슬라이드 14
s = p.slides[13]
for sh in s.shapes:
    if sh.has_text_frame and '틱-톡' in sh.text_frame.text:
        assert replace_in(sh, '틱-톡', '진자 운동')

p.save(DST)
print(DST)
# (덧붙임) 표지 'Ver 01' · 날짜와 꼬리말 'Ver01'도 Ver02로 — 이 파일 실행 뒤 따로 바꿈 (세션 기록)
