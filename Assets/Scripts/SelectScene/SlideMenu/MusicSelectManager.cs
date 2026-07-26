using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshProを使う場合

public class MusicSelectManager : MonoBehaviour
{
    // === Inspectorから設定するUI要素 ===
    public RectTransform detailPanel; // 1. スライドインさせるパネル本体

    [Header("スライドイン設定")]
    public Vector3 hiddenPosition;    // 2. パネル非表示時の位置 (Inspectorで設定)
    public Vector3 visiblePosition;   // 3. パネル表示時の位置 (Inspectorで設定)
    public float moveSpeed = 8f;      // 4. パネルの移動速度

    // ... その他のUI要素（タイトルテキスト、難易度ボタンなど） ...

    // === 内部変数 ===
    private MusicData currentSelectedMusic;
    private bool isPanelVisible = false; // パネルが表示されるべきかどうかのフラグ

    void Start()
    {
        // 初期位置を非表示位置に設定（念のため）
        detailPanel.localPosition = hiddenPosition;
    }

    void Update()
    {
        // 毎フレーム、ターゲット位置へ向かってパネルを移動させる
        Vector3 targetPos = isPanelVisible ? visiblePosition : hiddenPosition;

        // Lerpを使って滑らかに位置を補間
        detailPanel.localPosition = Vector3.Lerp(
            detailPanel.localPosition,
            targetPos,
            Time.deltaTime * moveSpeed
        );
    }

    // 曲リストのアイテムから呼ばれるメソッド
    public void SelectMusic(MusicData musicData)
    {
        currentSelectedMusic = musicData;

        // 1. 詳細パネルに情報を設定 (前回の回答を参照)
        // ... (省略) ...

        // 2. パネルを「表示状態」に切り替える
        isPanelVisible = true;
    }

    // ... (その他のメソッド：難易度ボタンの設定など) ...
}