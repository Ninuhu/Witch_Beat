using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MusicListItem : MonoBehaviour
{
    public MusicData musicData; // このアイテムが持つ曲データ
    private Button button;

    // この変数をManagerのInspectorから設定する
    private MusicSelectManager manager;

    public void Initialize(MusicData data, MusicSelectManager selectManager)
    {
        musicData = data;
        manager = selectManager;

        // UI表示の更新（例：ボタンのTextに曲名を設定）
        GetComponentInChildren<TMP_Text>().text = data.title;

        button = GetComponent<Button>();
        // クリックイベントにManagerのメソッドを設定
        button.onClick.AddListener(OnItemClicked);
    }

    private void OnItemClicked()
    {
        // Managerにこの曲データを選択したことを通知
        manager.SelectMusic(musicData);
    }
}