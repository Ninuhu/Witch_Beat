using UnityEngine;

public class TimeIndicator: MonoBehaviour
{
    public AudioSource audioSource;       // 曲
    public RectTransform playhead;        // 赤いライン
    public float pixelsPerSecond = 100f;  // 1秒あたりのpx
    void Update()
    {
        if (audioSource.isPlaying)
        {
            float time = audioSource.time; // 現在の再生秒数
            float yPos = -(time * pixelsPerSecond); //下に線が動く

            playhead.localPosition = new Vector3(
                playhead.localPosition.x,
                yPos,
                playhead.localPosition.z
            );
        }
    }
}
