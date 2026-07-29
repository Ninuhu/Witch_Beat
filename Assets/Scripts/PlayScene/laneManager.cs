using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// 曲の再生と、小節線（バーライン）の位置管理を担当するクラス
/// 「今何秒目か」を表す基準時間 t を持ち、他のスクリプト（notesGenerator, noteMover, lineObjectMover, characterController など）
/// はこの t を参照してノーツやレーンの位置を計算
/// 曲のBGM・BPMは選曲シーンで選ばれた曲ID（GameSettings.selectedMusicId）をもとに配列から取得する
/// </summary>

public class laneManager : MonoBehaviour
{
    // ===== 外部から参照される基準値 =====
    public int bpm; // 曲のBPM（GameSettingsの曲IDから自動セットされる）
    public int settingSpeed; // ノーツの流れる速さ（Inspectorで設定）
    public float barSpace; // 小節線同士の間隔
    public double t; // 曲開始からの経過時間（秒）。判定・移動の基準になる
    public double soflan; // 現在の再生速度倍率（ソフラン用。通常は1）
    public bool playing; // 判定・ノーツ移動を進行させてよいか

    [SerializeField] GameObject[] barLines = new GameObject[8];
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip drum; // カウント用ドラム音

    // ID(1:mopemope 2:shiningstar 3:song3)でBGMとBPMを管理
    // index 0はダミー（未使用）。InspectorでSize=4にして1,2,3番目に値を入れる
    [SerializeField] AudioClip[] musicClips = new AudioClip[4];
    [SerializeField] int[] musicBpms = new int[4];

    [SerializeField] notesGenerator notesGenerator;




    // ===== 曲開始のタイミング調整に使う定数 =====
    // 何拍分のカウントを待ってから曲を鳴らし始めるか（8拍）
    const float START_OFFSET_BEATS = 480f;
    // 実測で発生する再生タイミングのズレを吸収するための微調整値
    const float SCHEDULE_ADJUST_SECONDS = -1.1393f;
    // スタート前に鳴らすカウント用ドラムの回数（4拍分）
    const int COUNT_IN_BEATS = 4;

    


    // Pキーが押されたときに呼ばれる。カウント用ドラムを4回鳴らしたあと、判定・ノーツ移動を開始
    async void StartGame()
    {
        double scheduledDspTime = AudioSettings.dspTime + START_OFFSET_BEATS / bpm + SCHEDULE_ADJUST_SECONDS;
        audioSource.PlayScheduled(scheduledDspTime); // 8拍後、+なんとなくで誤差修正
        Debug.Log("曲再生時間:" + scheduledDspTime);

        for (int i = 0; i < COUNT_IN_BEATS; i++)
        {
            audioSource.PlayOneShot(drum);
            await Task.Delay(60000 / bpm); //4回1拍ずつ
        }
        TimeCountStart();
    }

    //||||||||||||||||||||||||||||
    // 判定・ノーツ移動を開始状態に
    void TimeCountStart()
    {
        playing = true;
        Debug.Log("譜面再生時間:" + AudioSettings.dspTime);
    }

    void Awake()
    {
        // 選曲シーンで選ばれた曲IDから直接取得（notesGeneratorのStartを待たずに済むように）
        int musicId = GameSettings.selectedMusicId;
        bpm = musicBpms[musicId];
        audioSource.clip = musicClips[musicId];

        barSpace = 1200.0f / bpm * settingSpeed / 10f; //小節線の間隔計算
        t = 0f;
        soflan = 1;
    }

    void Start()
    {
        // 8本の小節線を等間隔で初期配置
        for (int i = 0; i < barLines.Length; i++)
        {
            barLines[i].transform.localPosition = new Vector3(0, 0, barSpace * i);
        }
    }

    void Update()
    {
        if (playing)
        {
            t += Time.deltaTime * soflan;
            for (int i = 0; i < barLines.Length; i++)
            {
                barLines[i].transform.localPosition = new Vector3(0, 0, CalculateBarLineZ(i));
            }
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            StartGame(); // Pキー押したらスタート　(後で変える)
        }
    }

    

    // index番目の小節線の、今この瞬間のZ座標を計算
    // Mathf.Repeatは負の値を扱えないため、いったん全体にbarSpaceを足してから最後にbarSpace分を引くことで -barSpace ～ 7*barSpace の範囲を繰り返させてる
    float CalculateBarLineZ(int index)
    {
        float rawZ = barSpace * (index + 1) - settingSpeed / 2f * (float)t;
        return Mathf.Repeat(rawZ, barSpace * (barLines.Length - 1)) - barSpace;
    }
}