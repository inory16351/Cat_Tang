// 마을 섬 둘레 모양 (섬 로컬 x, z) — 마을 지도(VillageMap)가 섬을 그릴 때 씀 (2026-11-19)
// 섬 윗면(Top)은 정적 오브젝트라 플레이 중엔 static batching으로 메시가 합쳐져 원래 모양을 못 읽음 → 빌드할 때 둘레를 여기 저장
// VillageKitBuilder.BuildIsle가 붙임. 예전 씬은 메뉴 Cat_Tang/Village Map Outlines로 채움
using UnityEngine;

namespace CatTang
{
    public class IsleOutline : MonoBehaviour
    {
        public Vector2[] points;      // 섬 가운데 기준, 한 바퀴 (가운데에서 부채꼴로 채워도 되는 별 모양)
    }
}
