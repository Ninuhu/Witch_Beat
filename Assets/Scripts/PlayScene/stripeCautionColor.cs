using UnityEngine;

public class stripeCautionColor : MonoBehaviour
{
    byte x = 0;
    SpriteRenderer spr;

    void Start()
    {
        spr = gameObject.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float t = Time.time * 120; // 120fpsなので120倍
        x = (byte)Mathf.PingPong(5*t, 160); // 0 ~ 160往復
        spr.color = new Color32 (255, x, 0, 255);
    }
}
