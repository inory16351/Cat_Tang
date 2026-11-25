// Interactable.cs에서 분리 (씬에 저장하려면 파일 이름 = 클래스 이름)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    /// <summary>카메라를 향해 돌기. 부모가 좌우 반전(3장 거울 마을: 마을 뿌리 x 배율 -1)이면 글 · 그림이 뒤집혀 보이지 않게 자기 x 배율을 맞춤</summary>
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam) transform.rotation = cam.transform.rotation;
            var par = transform.parent;
            float sign = par && par.lossyScale.x < 0 ? -1f : 1f;
            var s = transform.localScale;
            if (Mathf.Sign(s.x) != sign) transform.localScale = new Vector3(Mathf.Abs(s.x) * sign, s.y, s.z);
        }
    }
}
