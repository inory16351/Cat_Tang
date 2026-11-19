// 마을 섬 아래 구름층 (뒤집힌 밤하늘 사이로 천천히 흐르는 구름). 재질 텍스처 오프셋만 옮김
using UnityEngine;

namespace CatTang
{
    public class CloudScroll : MonoBehaviour
    {
        public Vector2 speed = new(0.004f, 0.0015f);
        Material mat;

        void Start() { var r = GetComponent<Renderer>(); if (r) mat = r.material; }

        void Update()
        {
            if (!mat) return;
            mat.SetTextureOffset("_BaseMap", speed * Time.time);
        }
    }
}
