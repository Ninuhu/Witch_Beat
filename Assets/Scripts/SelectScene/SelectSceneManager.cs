using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;

public class SelectSceneManager : MonoBehaviour
{
    // SceneFaderへの参照
    public SceneFader sceneFader;

    // 右側のパネルグループのAnimatorをアタッチするための変数
    public Animator rightPanelAnimator;

    // 選択された曲のIDや名前を保持
    private string selectedSongName;

    // 選択された難易度を保持
    private string selectedDifficulty;

    // 遷移先のシーン名をInspectorから設定できるようにする
    [Header("次のシーン名")]
    [SerializeField]
    private string nextSceneName = "PlayScene";

    // Start is called before the first frame update
    private void Start()
    {
        if (sceneFader != null)
        {
            // TitleSceneからの遷移後、画面を明るく戻す(フェードイン)
            StartCoroutine(sceneFader.FadeIn());
        }
    }


    // 曲選択ボタンが押されたときに呼ばれるメソッド
    public void SelectSong(string songName)
    {
        selectedSongName = songName;
        Debug.Log("Selected Song: " + selectedSongName);
        // 選択された曲の視覚的なフィードバックを更新する処理を追加

        // アニメーションを再生する
        if (rightPanelAnimator != null)
        {
            rightPanelAnimator.Play("PanelSlideIn");
        }
    }

    // 難易度ボタンが押されたときに呼ばれるメソッド
    public void SelectDifficulty(string difficulty)
    {
        selectedDifficulty = difficulty;
        Debug.Log("Selected Difficulty: " + selectedDifficulty);
        // 選択された難易度の視覚的なフィードバックを更新する処理を追加
    }

    // Startボタンが押されたときに呼ばれるメソッド
    public void StartGame()
    {
        if (string.IsNullOrEmpty(selectedSongName) || string.IsNullOrEmpty(selectedDifficulty))
        {
            // ユーザーに選択を促すメッセージなどを表示
            Debug.LogWarning("曲または難易度が選択されていません。");
            return;
        }

        // 選択された曲と難易度をPlaySceneへ引き継ぐ処理をここに追加(PlayerPrefsなど)
        PlayerPrefs.SetString("SelectedSong", selectedSongName);
        PlayerPrefs.SetString("SelectedDifficulty", selectedDifficulty);

        SceneManager.LoadSceneAsync("PlayScene");
    }
}