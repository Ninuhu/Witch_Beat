using UnityEngine;

public class SceneChanger : MonoBehaviour
{
    // TitleManagerへの参照を保持します。InspectorでTitleManagerのゲームオブジェクトを紐づけます。
    // ボタン自身がManagerを知っている必要があります。
    public TitleManager titleManager;

    void Start()
    {
        // 紐付け忘れを防ぐため、Startで確認できます
        if (titleManager == null)
        {
            Debug.LogError("[SceneChanger] TitleManagerが設定されていません。ボタンのInspectorを確認してください。");
        }
    }

    // ボタンの OnClick() イベントに直接紐づけるメソッド
    public void ChangeToSelectScene()
    {
        if (titleManager != null)
        {
            // TitleManagerに処理を依頼する
            titleManager.StartTransitionToSelectScene();
        }
        else
        {
            Debug.LogError("[SceneChanger] 遷移を開始できません。TitleManagerが設定されていません。");
        }
    }
}