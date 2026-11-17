// 런타임에 만든 메시(구운 트렁크 등)를 오브젝트와 함께 지움 (정적 캐시가 아닌 일회용 메시)
using UnityEngine;

namespace CatTang
{
    public class DestroyMesh : MonoBehaviour
    {
        public Mesh mesh;
        void OnDestroy() { if (mesh) Destroy(mesh); }
    }
}
