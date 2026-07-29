using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

/// <summary>
/// Playシーンでの結果を保持したままResultシーンへ受け渡すためのクラス
/// DontDestroyOnLoadされるオブジェクトにアタッチされ、シーンをまたいで生き残る
/// 曲が終わったタイミングで characterController から MoveToResultScene() が呼ばれ、
/// スコア等を集計したうえで暗転演出をしつつResultシーンへ遷移
/// </summary>
public class moveToResult : MonoBehaviour
{
    [SerializeField] characterController characterController;
    [SerializeField] notesGenerator notesGenerator;

    GameObject overlayObj;
    Image overlayImage;
    byte alpha;

    public int maxCombo;
    public int totalNotesCount;
    public int difficulty;
    public int techScore;
    public int damage;
    public string music;
    public double averageGap;
    public double accuracy;
    public List<double> gapSaveList;
    public double[] gapGraphPercentage;
    public int[] judgeCounts;
    public int musicId;

    const int GRAPH_BUCKET_COUNT = 10; // ズレ分布グラフの区分数
    const double GRAPH_BUCKET_WIDTH = 0.02; // 1区分あたりのズレ幅（秒）
    const double GRAPH_MIN_GAP = -0.08; // 最初の区分の下限（秒）
    const int DAMAGE_SCORE_PENALTY = 10000; // 被ダメージ1回あたりのスコア減点

    void Awake()
    {
        // 変数引継ぎ：シーンをまたいでもこのオブジェクトが破棄されないようにする
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 初期化
        averageGap = 0;
        alpha = 0;
        gapSaveList = new List<double>();
        gapGraphPercentage = new double[GRAPH_BUCKET_COUNT];
        judgeCounts = new int[4];
    }



    // 黒いオーバーレイを少しずつ不透明にしていき、画面を暗転させる
    async void ShowBlackScreen()
    {
        overlayObj = GameObject.FindWithTag("overlay");
        overlayObj.transform.SetAsLastSibling();
        overlayImage = overlayObj.GetComponent<Image>();
        alpha = 0;
        while (alpha != 255)
        {
            alpha += 5;
            overlayImage.color = new Color32(0, 0, 0, alpha);
            await Task.Delay(5);
        }
    }


    //|||||||||||||||||||||||||||||||||||||||||||||
    // 指定時間待ってからシーンを読み込む
    async void SceneLoad(string name, int time)
    {
        await Task.Delay(time);
        SceneManager.LoadScene(name);
    }


    //|||||||||||||||||||||||||||||||||||||||||||||
    // 曲終了時に characterController から呼ばれるエントリーポイント
    // プレイ結果を集計し、暗転しながらResultシーンへ遷移する
    public void MoveToResultScene()
    {
        // Inspectorでの参照はシーン構成（DontDestroySingleObjectの重複破棄等）により
        // 別インスタンスを指している場合があるため、呼び出し時点で実際にシーンに存在する本物を取り直す
        characterController = FindObjectOfType<characterController>();
        notesGenerator = FindObjectOfType<notesGenerator>();

        CollectResultData();
        CalculateGapStatistics();

        ShowBlackScreen();
        SceneLoad("resultScene", 2000);
    }


    //|||||||||||||||||||||||||||||||||||||||||||||
    // characterController / notesGenerator から結果データを集めてフィールドへ反映
    void CollectResultData()
    {
        musicId = notesGenerator.selectedMusicId;
        difficulty = notesGenerator.selectedDifficulty;
        music = notesGenerator.selectedMusic;

        judgeCounts[0] = characterController.criticalBreakCount;
        judgeCounts[1] = characterController.breakCount;
        judgeCounts[2] = characterController.weakCount;
        judgeCounts[3] = characterController.lostCount;

        totalNotesCount = characterController.totalNotesCount;
        gapSaveList = characterController.gapSaveList;
        damage = characterController.damageTaken;
        techScore = (int)characterController.techScore - damage * DAMAGE_SCORE_PENALTY;
        maxCombo = characterController.maxComboCount;
        accuracy = characterController.bossHp;
    }



    //|||||||||||||||||||||||||||||||||||||||||||||
    /// <summary>
    /// 判定タイミングのズレ（gapSaveList）から、平均ズレと分布グラフ用の割合を計算する
    /// ノーツを1つも記録できていない（gapSaveListが空の）場合は0除算になるため、
    /// その場合は集計をスキップして0のままにする（表示がNaNになるのを防ぐ安全対策）
    /// </summary>
    void CalculateGapStatistics()
    {
        if (gapSaveList.Count == 0) return;

        for (int i = 0; i < gapSaveList.Count; i++)
        {
            averageGap += gapSaveList[i];

            for (int bucket = 0; bucket < GRAPH_BUCKET_COUNT; bucket++)
            {
                if (gapSaveList[i] < GRAPH_MIN_GAP + GRAPH_BUCKET_WIDTH * bucket)
                {
                    gapGraphPercentage[bucket]++;
                    break;
                }
            }
        }

        for (int bucket = 0; bucket < GRAPH_BUCKET_COUNT; bucket++)
        {
            gapGraphPercentage[bucket] /= gapSaveList.Count;
        }

        averageGap = averageGap / gapSaveList.Count * 1000;
    }
}