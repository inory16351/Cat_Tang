// 인벤토리 칸 하나: 끌기 · 놓기 · 클릭(탄 강화 창) · 마우스 올리기(설명). 옮기기 규칙은 InventoryScreen.Move
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatTang
{
    public enum SlotPlace { Cylinder, Storage, GunRight, GunLeft, Sell, EnhanceOp, EnhanceStore }

    public class SlotWidget : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public SlotPlace place;
        public int index;
        public Image icon;
        public InventoryScreen owner;

        static GameObject ghost;
        static SlotWidget dragging;

        public void OnBeginDrag(PointerEventData e)
        {
            if (!owner.CanEdit || owner.ItemAt(this) == null) return;
            dragging = this;
            ghost = new GameObject("Ghost", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            ghost.transform.SetParent(owner.transform.root, false);
            ghost.transform.SetAsLastSibling();
            var im = ghost.GetComponent<Image>();
            im.sprite = icon ? icon.sprite : null;
            im.preserveAspect = true;
            im.raycastTarget = false;
            ghost.GetComponent<CanvasGroup>().blocksRaycasts = false;
            ((RectTransform)ghost.transform).sizeDelta = new Vector2(96, 96);
            ghost.transform.position = e.position;
            if (icon) icon.color = new Color(1, 1, 1, 0.35f);
        }

        public void OnDrag(PointerEventData e) { if (ghost) ghost.transform.position = e.position; }

        public void OnEndDrag(PointerEventData e)
        {
            if (ghost) Destroy(ghost);
            if (icon) icon.color = Color.white;
            dragging = null;
            owner.Refresh();
        }

        public void OnDrop(PointerEventData e) { if (dragging && dragging != this) owner.Move(dragging, this); }
        public void OnPointerEnter(PointerEventData e) => owner.ShowTip(this);
        public void OnPointerExit(PointerEventData e) => owner.ShowTip(null);

        public void OnPointerClick(PointerEventData e)
        {
            if (e.dragging) return;
            owner.Click(this);
        }
    }
}
