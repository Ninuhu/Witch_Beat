using UnityEngine;

/// <summary>
/// 警告表示（炎の設置場所などに出る縞模様オブジェクト）の色を、
/// 赤～オレンジの間で往復させて点滅しているように見せるクラス。
/// </summary>
public class stripeCautionColor : MonoBehaviour
{
    byte x = 0;
    SpriteRenderer spr;

    const float FPS_REFERENCE = 120f; // 基準フレームレート（Time.timeを120fps換算にするための倍率）
    const float COLOR_CHANGE_SPEED = 5f; // 色が往復する速さ
    const float COLOR_RANGE = 160f; // G成分（0～160）の往復幅

    void Start()
    {
        spr = gameObject.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float t = Time.time * FPS_REFERENCE; // 120fpsなので120倍
        x = (byte)Mathf.PingPong(COLOR_CHANGE_SPEED * t, COLOR_RANGE); // 0 ~ 160往復
        spr.color = new Color32(255, x, 0, 255); // 赤(255,0,0)～黄寄りオレンジの間で点滅
    }
}