// 使いやすいように入れただけで入れなくてもいい


using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NoDragScrollRect : ScrollRect
{
    // ドラッグ操作を無効化
    public override void OnBeginDrag(PointerEventData eventData) { }
    public override void OnDrag(PointerEventData eventData) { }
    public override void OnEndDrag(PointerEventData eventData) { }
    // ホイールスクロールは通常通り生かす
    public override void OnScroll(PointerEventData data)
    {
        base.OnScroll(data); 
    }
}