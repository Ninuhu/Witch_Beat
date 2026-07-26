using UnityEngine;

public class SelectManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // InspectorでSelectSceneのFadePanelにアタッチされたSceneFaderを紐づけます
    public SceneFader scenefader;
    void Start()
    {
        if (scenefader != null)
        {
            // StartCoroutineは、このManagerオブジェクトが実行します
            StartCoroutine(scenefader.FadeIn());
            Debug.Log("[SelectManager] 明転処理（フェードイン）を開始しました。");
        }
        else
        {
            Debug.LogError("[SelectManager] SelectSceneにSceneFaderが見つかりません。");
        }

        Debug.Log("SelectSceneの初期化を完了しました。");
    }
}