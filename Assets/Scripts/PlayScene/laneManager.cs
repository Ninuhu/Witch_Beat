using UnityEngine;
using System.Threading.Tasks;

public class laneManager : MonoBehaviour
{
    public int bpm, settingSpeed;
    public float barSpace;
    public double t;
    public double soflan;
    public bool playing;

    [SerializeField] GameObject[] barLines = new GameObject[8];
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip drum; // カウント用ドラム音

    // ID(1:mopemope 2:shiningstar 3:song3)でBGMとBPMを管理
    // index 0はダミー（未使用）。InspectorでSize=4にして1,2,3番目に値を入れる
    [SerializeField] AudioClip[] musicClips = new AudioClip[4];
    [SerializeField] int[] musicBpms = new int[4];

    [SerializeField] notesGenerator notesGenerator;

    async void StartGame()
    {
        audioSource.PlayScheduled(AudioSettings.dspTime +480 /bpm -1.1393f); // 8拍後、+なんとなくで誤差修正
        Debug.Log("曲再生時間:"+(AudioSettings.dspTime +480 /bpm -1.1393f));
        for (int i = 0; i < 4; i++) 
        {
            audioSource.PlayOneShot(drum);
            await Task.Delay(60000/bpm); //4回1拍ずつ
        }
        TimeCountStart();
    }

    void TimeCountStart()
    {
        playing = true;
        Debug.Log("譜面再生時間:"+AudioSettings.dspTime);
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
        for (int i = 0; i < barLines.Length; i++) 
            barLines[i].transform.localPosition = new Vector3(0, 0, barSpace * i); //初期配置
    }

    void Update()
    {
        if (playing == true) 
        {
            t += Time.deltaTime * soflan;
            for (int i = 0; i < barLines.Length; i++) 
                barLines[i].transform.localPosition = new Vector3(0, 0, Mathf.Repeat(barSpace * (i+1) - settingSpeed /2f *(float)t, barSpace * (barLines.Length-1))-barSpace); // Mathf.Repeat使って -barSpace ~ 7barSpaceを繰り返す、負の数はRepeat使えないので全体にbarSpace足して関数外で引く
        }
        else if (Input.GetKeyDown(KeyCode.P)) StartGame(); // Pキー押したらスタート　後で変える
    }
}