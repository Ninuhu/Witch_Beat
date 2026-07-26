using UnityEngine;
using UnityEngine.UI;

public class LongNote11 : MonoBehaviour
{
    [SerializeField] private Image image;
    private Vector2 startPos;
    private Vector2 endPos;
    public void SetStart(Vector2 pos)
    {
        startPos = pos;
        endPos = pos;
        UpdateVisual();
    }

    public void SetEnd(Vector2 pos)
    {
        endPos = pos;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (image == null)
            image = GetComponent<Image>();

        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) return;

        Vector2 center = (startPos + endPos) / 2f;
        rt.anchoredPosition = center;

        float distance = Vector2.Distance(startPos, endPos);
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, distance);

        Vector2 direction = endPos - startPos;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        rt.localRotation = Quaternion.Euler(0, 0, angle);
    }
}