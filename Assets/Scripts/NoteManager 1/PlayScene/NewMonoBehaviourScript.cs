using UnityEngine;
using System.Threading.Tasks;

public class LaneMane11 : MonoBehaviour
{
    public int bpm = 120;               // 曲のBPM
    public int settingSpeed = 10;       // 流れる速さ
    public float barSpace;              // 小節間の距離
    public double t;                    // 経過時間用
    public double soflan;               // 速度倍率
    public bool playing;                // 再生中かどうか

    [SerializeField] GameObject[] barLines = new GameObject[8]; // 小節線
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip drum;
    [SerializeField] AudioClip mopemope;

    // ゲーム開始処理
    async void StartGame()
    {
        // 8拍後に曲再生（+誤差補正あり）
        double startTime = AudioSettings.dspTime + 480.0 / bpm - 1.3393;
        audioSource.clip = mopemope;
        audioSource.PlayScheduled(startTime);
        Debug.Log("曲再生予定時刻: " + startTime);

        // カウント音4回
        for (int i = 0; i < 4; i++)
        {
            audioSource.PlayOneShot(drum);
            await Task.Delay(60000 / bpm); // 1拍間隔
        }

        TimeCountStart();
    }

    // 再生開始
    void TimeCountStart()
    {
        playing = true;
        Debug.Log("譜面再生スタート: " + AudioSettings.dspTime);
    }

    void Awake()
    {
        // 小節間隔を計算
        barSpace = 1200.0f / bpm * settingSpeed / 10f;
        t = 0f;
        soflan = 1;
        playing = false;
    }

    void Start()
    {
        // 小節線の初期配置
        for (int i = 0; i < barLines.Length; i++)
        {
            barLines[i].transform.localPosition = new Vector3(0, 0, barSpace * i);
        }
    }

    void Update()
    {
        if (playing)
        {
            // 経過時間カウント
            t += Time.deltaTime * soflan;

            // 小節線をループ移動させる
            for (int i = 0; i < barLines.Length; i++)
            {
                float z = Mathf.Repeat(barSpace * (i + 1) - settingSpeed / 2f * (float)t, barSpace * (barLines.Length - 1)) - barSpace;
                barLines[i].transform.localPosition = new Vector3(0, 0, z);
            }
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            StartGame();
        }
    }
}