using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class SceneFader : MonoBehaviour
{
    public Image fadePanel;
    public float fadeDuration = 1.0f;

    // 外部から呼び出されるメソッド
    public void FadeToScene(string sceneName)
    {
        // FadePanelが画面に描画されることを確実にする
        if (fadePanel != null)
        {
            // 処理中に下のUIを操作できないようにRaycastTargetをONにする
            fadePanel.raycastTarget = true;

            // パネルを描画するために、ゲームオブジェクトをアクティブにする
            fadePanel.gameObject.SetActive(true);

            // コルーチンを開始
            StartCoroutine(FadeOutAndLoad(sceneName));
        }
        else
        {
            // エラー処理
            Debug.LogError("FadePanelがSceneFaderに設定されていません。");
        }
    }

    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        // 1. 【非同期ロードの開始】
        // ロードは始めるが、自動でシーンを切り替えないようにする
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        float timer = 0f;

        // 【重要】フェードアウト開始時、FadePanelのアルファ値が0から始まっているか確認
        // パネルの色を初期化
        Color color = fadePanel.color;
        color.a = 0f; // ★ 念のため、透明から開始する設定を確認
        fadePanel.color = color;

        // 2. 【フェードアウト処理 (透明度 0 -> 1)】
        while (timer < fadeDuration)
        {
            // 時間の経過に合わせてアルファ値を計算
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / fadeDuration);

            color.a = alpha;
            fadePanel.color = color;

            // ロードが90%以上完了していても、フェードアウトが完了するまでは待つ
            // (ロード処理はバックグラウンドで継続している)
            yield return null; // 1フレーム待機
        }

        // フェードアウト完了時点でロードがまだ90%未満だったら、ロード完了を待つ
        // (通常はフェードアウト時間内に完了するため省略可だが、安全のため)
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // 3. 【シーンのアクティベート（切り替え）】
        // 画面が完全に暗転（フェードアウト完了）したら、シーン切り替えを許可する
        asyncLoad.allowSceneActivation = true;

        // 新しいシーンが完全にアクティブになるのを待つ
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // ここで SelectScene が完全にロードされ、アクティブになります。
    }

    // 【SelectScene で実行するための処理】
    // シーンロード時に FadePanel を非アクティブにして、入力を遮断しないようにします
    void Awake()
    {
        // FadePanel が存在し、すでに透明（A=0）になっている場合は非アクティブにします。
        // ※ 画面が暗転していないシーン（SelectScene）にアタッチされている場合を想定
        if (fadePanel != null && fadePanel.color.a == 0)
        {
            fadePanel.gameObject.SetActive(false);
        }
    }


    // 画面を明るくする（フェードイン）処理
    public IEnumerator FadeIn()
    {
        // フェードイン開始前に、パネルをアクティブに戻して最前面に表示
        fadePanel.gameObject.SetActive(true);

        // パネルの色（アルファ値）を完全に不透明（A=1）からスタートさせます
        Color color = fadePanel.color;
        color.a = 1f;
        fadePanel.color = color;

        float timer = fadeDuration; // タイマーを最大値から開始

        // 画面が黒い状態（A=1）から完全に透明（A=0）になるまで
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / fadeDuration); // アルファ値を 1 から 0 へ

            color = fadePanel.color;
            color.a = alpha;
            fadePanel.color = color;

            yield return null;
        }

        // 完全に透明になったら、パネルを非アクティブ化して入力遮断を防ぎます
        fadePanel.gameObject.SetActive(false);
    }
}