using UnityEngine;

public class TitleManager : MonoBehaviour
{
    // 遷移先のシーン名をInspectorから設定できるようにする
    [Header("次のシーン名")]
    [SerializeField]
    private string nextSceneName = "SelectScene";

    // このメソッドをボタンのOnClickイベントなどに設定します
    void Start()
    {
        // 開発中にフェードイン処理を確認したい場合はここに記述します
        // 例: StartCoroutine(sceneFader.FadeIn()); 

        // 既存のタイトル画面開始時の初期化処理などがあればここに記述
        Debug.Log("TitleSceneの初期化が完了しました。");
    }

    public void StartTransitionToSelectScene()
    {
        // 自身にアタッチされているSceneFaderコンポーネントを取得
        SceneFader fader = GetComponent<SceneFader>();

        if (fader != null)
        {
            // SceneFaderを通じて、次のシーンへの非同期ロードと暗転を開始します
            fader.FadeToScene(nextSceneName);
            Debug.Log($"[TitleManager] 暗転開始。{nextSceneName}へ遷移します。");
        }
        else
        {
            Debug.LogError("[TitleManager] SceneFaderがこのオブジェクトにアタッチされていません！");
        }
    }
}