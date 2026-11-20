# 4장 보스 경기장 바닥 — 은빛 거대 문자판

## 개요
4장 보스(시침·분침 쌍둥이) 경기장 꼭대기 문자판 바닥.
기존 `Kit_Arena` (2장 거대 시계)처럼 FBX 하나로 만들어 `Resources/Kit/Ch4/`에 넣음.

## 크기·좌표
- 원판 반지름 **20m** (`Ch4Kit.ArenaR`), 중심 원점 (0, 0, 0)
- Y 위 → 바닥면, +Z → 북, +X → 동 (유니티 기준으로 내보냄)
- 바닥 윗면 y = 0 (Kit에서 `FlatFloor` 처리, 방 타일 위에 올림)

## 모양
1. **원형 대 테두리** (반지름 19.5~20m, 높이 0.25m): 은빛 금속(`dial_silver`) 위에 검은 외곽선(`dial_black`).
   - 테두리 위쪽 면에 얇은 뾰족한 눈금 60개 (분 단위, 높이 0.03m, 12·3·6·9시는 길게 0.08m)
2. **문자판 바닥** (반지름 19.5m, 평평한 원판 y=0): 재질 `dial_face` (흰 은빛)
   - 숫자 1~12: 아라비아 숫자 `digit_geometry` (Arial Black 메시, 높이 1.4m, 반경 16m 원 위에 배치, 12시 = +Z). 재질 `dial_number` (검은 금속)
   - 각 숫자 사이에 얇은 방사형 선 (길이 2.5m, 폭 0.04m, 반경 13~15.5m) 재질 `dial_line`
3. **시각 칸 경계** (12등분 방사형 선, 반지름 1~19m, 폭 0.06m, y=0.01): 재질 `dial_line`
4. **중심 축 장식** (반경 1m, 높이 0.5m 반구+원뿔): 재질 `dial_silver`
5. **장식 고리** 2겹 (반경 10m, 14m, 폭 0.08m): 재질 `dial_silver`

## 재질 (색만, 텍스처 없음)
| 이름 | 용도 | 색 (sRGB) |
|---|---|---|
| dial_face | 문자판 바닥 | (0.92, 0.91, 0.89) |
| dial_silver | 테두리·축·고리 | (0.78, 0.76, 0.73) |
| dial_number | 숫자 | (0.08, 0.08, 0.08) |
| dial_line | 선·눈금 | (0.15, 0.14, 0.13) |
| dial_black | 외곽 | (0.04, 0.04, 0.04) |

## 내보내기
- 파일 이름: `Kit_Ch4_BossFloor.fbx`
- 경로: `Game/Assets/Art/Characters/Ch4/Kit_Ch4_BossFloor.fbx`
- FBX Binary, Apply Transform, 삼각형, 위 Y, 앞 -Z
- 미리보기 PNG: `Art/tools/specs/blender_kit_ch4_bossfloor_preview.png` (위에서 직교)

## 주의
- 삼각형 총 수 8000 이하
- `digit_geometry`: Arial Black 글꼴 → 텍스트 오브젝트 → 메시 변환 → 닫힌 메시 (3장 `blender_kit_ch3_bosses.py`의 `digit_geometry` 함수 참고)
- 숫자 위치: 12시(+Z) · 1시(30°시계방향, 시계 기준) · ... · 11시. 각도 = `hour * 30°`, x = sin(θ) * R, z = cos(θ) * R
