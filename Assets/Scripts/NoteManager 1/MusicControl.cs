using UnityEngine;

public class MusicControl : MonoBehaviour
{
    public AudioSource audioSource;
    // 再生
    public void PlayBGM()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
    // 一時停止
    public void PauseBGM()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }
    // 停止（最初に戻す）
    public void StopBGM()
    {
        audioSource.Stop();
    }

}
