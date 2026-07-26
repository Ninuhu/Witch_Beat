using UnityEngine;

public class NoteContainer : MonoBehaviour
{
    [Header("UI コンテナ / オーディオ")]
    public RectTransform notesContainer; // NotesContainer
    public AudioSource audioSource;      // 曲を再生
    public float pixelsPerSecond = 100f; // 1秒あたり何pxにするか
    [Header("レーン (8本)")]
    public RectTransform[] lanes = new RectTransform[8]; // レーンの参照

    void Start()
    {
        // lanes が Inspector で未設定の場合、自動取得
        if (lanes == null || lanes.Length == 0 || lanes[0] == null)
        {
            lanes = new RectTransform[8];
            for (int i = 0; i < 8; i++)
            {
                GameObject laneObj = GameObject.Find($"Lane{i + 1}");
                if (laneObj != null)
                {
                    lanes[i] = laneObj.GetComponent<RectTransform>();
                }
                else
                {
                    Debug.LogWarning($"Lane{i + 1} がシーン内に見つかりませんでした！");
                }
            }
        }

        // AudioSource に曲が入っている場合だけ処理
        if (audioSource != null && audioSource.clip != null)
        {
            float songLength = audioSource.clip.length; // 曲の長さ（秒）
            float newHeight = songLength * pixelsPerSecond;

            // NotesContainer の高さを更新
            if (notesContainer != null)
            {
                Vector2 size = notesContainer.sizeDelta;
                size.y = newHeight;
                notesContainer.sizeDelta = size;
            }

            // 各レーンの高さを更新
            foreach (RectTransform lane in lanes)
            {
                if (lane != null)
                {
                    Vector2 laneSize = lane.sizeDelta;
                    laneSize.y = newHeight;
                    lane.sizeDelta = laneSize;
                }
            }
        }
        else
        {
            Debug.LogWarning("AudioSource または AudioClip が設定されていません。");
        }
    }
}