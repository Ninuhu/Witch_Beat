using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

[Serializable]
public class chartData
{
    public Chart[] notes;
}

[Serializable]
public class Chart
{
    public int lane; // どこのレーンか
    public float time; // 何小節目か
    public int note; // なんのノーツか
    public float length; // ロングノーツ、攻撃などの持続時間
}

public class notesGenerator : MonoBehaviour
{
    public List<double>[] singleNotesJudgeTimes; // 叩いた時の判定用
    public List<double>[] doubleNotesJudgeTimes;
    public List<double>[] singleLongNotesJudgeTimes;
    public List<double>[] doubleLongNotesJudgeTimes;

    public List<double>[] lineObjectsJudgeTimes;
    public List<double>[] placeObjectsJudgeTimes;

    public GameObject[][] notesObject;

    string loadChart; // なんの譜面読み込むか
    float barSpace; // １小節の長さ

    public int selectedMusicId; // 選曲シーンで選んだ曲のID (1:mopemope 2:shiningstar 3:song3)
    public string selectedMusic; // 曲名（IDから自動セットされる。表示用などにそのまま使える）
    public int selectedDifficulty; // 難易度 (0:normal 1:hard, -1:debug)
    public int totalNotesCount;

    // ID -> 曲名 の対応表（0番目はダミーで未使用）
    static readonly string[] musicNames = { "", "mopemope", "shiningstar", "song3" };

    [SerializeField] GameObject tapSingle, longSingle, tapDouble, longDouble, skillEnergyObject, attackProjectile;
    [SerializeField] laneManager laneManager;

    void Start()
    {
        // 選曲シーンで選ばれた内容を反映
        selectedMusicId = GameSettings.selectedMusicId;
        selectedDifficulty = GameSettings.selectedDifficulty;
        selectedMusic = musicNames[selectedMusicId];

        barSpace = laneManager.barSpace; // １小節の長さ取得

        // 判定の配列初期化
        singleNotesJudgeTimes = new List<double>[8];
        doubleNotesJudgeTimes = new List<double>[8];
        singleLongNotesJudgeTimes = new List<double>[8];
        doubleLongNotesJudgeTimes = new List<double>[8];
        lineObjectsJudgeTimes = new List<double>[9];
        placeObjectsJudgeTimes = new List<double>[9];
        notesObject = new GameObject[16][];
        for (int i = 0; i < 8; i++)
        {
            singleNotesJudgeTimes[i] = new List<double>();
            doubleNotesJudgeTimes[i] = new List<double>();
            singleLongNotesJudgeTimes[i] = new List<double>();
            doubleLongNotesJudgeTimes[i] = new List<double>();
            lineObjectsJudgeTimes[i] = new List<double>();
            placeObjectsJudgeTimes[i] = new List<double>();
        }
        lineObjectsJudgeTimes[8] = new List<double>();
        placeObjectsJudgeTimes[8] = new List<double>();

        // 譜面ファイル名を組み立てる
        // 通常時: "曲名"+"難易度番号"  例) shiningstar0(normal), shiningstar1(hard)
        // デバッグ時(difficulty == -1): debug.json
        if (selectedDifficulty == -1)
        {
            loadChart = "debug"; // debug.json用
        }
        else
        {
            loadChart = musicNames[selectedMusicId] + selectedDifficulty;
        }

        // 入力ファイルはAssets/Notes/{selectedChart}.json
        // .jsonをテキストファイルとして読み取り、string型で受け取る
        /*string filePath = Path.Combine(Application.dataPath, "Notes", loadChart+".json");
        string jsonString = File.ReadAllText(filePath);
        chartData chart = JsonUtility.FromJson<chartData>(jsonString); 
        // chart.notes[i].lane
        // chart.notes[i].time などでアクセス
        // chart.notes.GetLength(0)で全体のノーツ数*/

        //ビルドしたときに上のstringだと読み込まなかったから切り替え
        TextAsset chartAsset = Resources.Load<TextAsset>("Notes/" + loadChart);
        if(chartAsset == null)
        {
            Debug.LogError("譜面ファイルが見つかりません: Resources/Notes/" + loadChart + ".json");
            return;
        }
        chartData chart = JsonUtility.FromJson<chartData>(chartAsset.text);

        totalNotesCount = chart.notes.GetLength(0); // タップ、ロング関係なくノーツ数集計
        for (int i = 0; i < chart.notes.GetLength(0); i++) 
        {
            switch (chart.notes[i].note)
            {
                case 0: // シングルタップ
                    //lane:0の時x:-7, lane:7の時x:7
                    Instantiate(tapSingle, new Vector3(7-(7-chart.notes[i].lane)*2, 0, barSpace * chart.notes[i].time), tapSingle.transform.rotation);
                    singleNotesJudgeTimes[chart.notes[i].lane].Add(barSpace * chart.notes[i].time / (laneManager.settingSpeed /2f));

                    break;

                case 2: // ダブルタップ
                    Instantiate(tapDouble, new Vector3(7-(7-chart.notes[i].lane)*2, 0, barSpace * chart.notes[i].time), tapDouble.transform.rotation);
                    doubleNotesJudgeTimes[chart.notes[i].lane].Add(barSpace * chart.notes[i].time / (laneManager.settingSpeed /2f) *(-1)); // ダブルタップの左レーンは時間マイナスで記録
                    doubleNotesJudgeTimes[chart.notes[i].lane +1].Add(barSpace * chart.notes[i].time / (laneManager.settingSpeed /2f));

                    break;

                case 1: // シングルロング
                    totalNotesCount++; //ロングノーツの場合カウント+1
                    GameObject singleLongObj = Instantiate(longSingle, new Vector3(7-(7-chart.notes[i].lane)*2, 0, barSpace * chart.notes[i].time), longSingle.transform.rotation);
                    GameObject singleLine = singleLongObj.transform.GetChild(0).gameObject;
                    singleLine.transform.localPosition = new Vector3(0, 0, chart.notes[i].length*barSpace*0.5f *10 /6); // 親objectのscaleが0.6なので10/6を掛ける
                    singleLine.transform.localScale = new Vector3(1, 1, chart.notes[i].length*barSpace *10 /6);

                    singleNotesJudgeTimes[chart.notes[i].lane].Add(barSpace * chart.notes[i].time / (laneManager.settingSpeed /2f));

                    singleLongNotesJudgeTimes[chart.notes[i].lane].Add(barSpace * chart.notes[i].time / (laneManager.settingSpeed /2f) +0.25f); //開始時刻 0.25秒後から判定
                    singleLongNotesJudgeTimes[chart.notes[i].lane].Add(barSpace * (chart.notes[i].time + chart.notes[i].length) / (laneManager.settingSpeed /2f) -0.25f); //終了時刻 0.25秒前に離してもOK

                    break;

                case 3: // ダブルロング
                    totalNotesCount++;
                    GameObject doubleLongObj = Instantiate(longDouble, new Vector3(7-(7-chart.notes[i].lane)*2, 0, barSpace * chart.notes[i].time), longDouble.transform.rotation);
                    GameObject doubleLine = doubleLongObj.transform.GetChild(0).gameObject;
                    doubleLine.transform.localPosition = new Vector3(0, -1.25f, chart.notes[i].length*barSpace*0.5f *10 /6); // 親objectのscaleが0.6なので10/6を掛ける
                    doubleLine.transform.localScale = new Vector3(1, 2, chart.notes[i].length*barSpace *10 /6);

                    doubleNotesJudgeTimes[chart.notes[i].lane].Add(barSpace * chart.notes[i].time / (laneManager.settingSpeed /2f) *(-1));
                    doubleNotesJudgeTimes[chart.notes[i].lane +1].Add(barSpace * chart.notes[i].time / (laneManager.settingSpeed /2f));

                    doubleLongNotesJudgeTimes[chart.notes[i].lane].Add( (barSpace * chart.notes[i].time / (laneManager.settingSpeed /2f) +0.25f) *(-1)); //開始時刻 0.25秒後から判定
                    doubleLongNotesJudgeTimes[chart.notes[i].lane +1].Add(barSpace * chart.notes[i].time / (laneManager.settingSpeed /2f) +0.25f);

                    doubleLongNotesJudgeTimes[chart.notes[i].lane].Add( (barSpace * (chart.notes[i].time + chart.notes[i].length) / (laneManager.settingSpeed /2f) -0.25f) *(-1)); //終了時刻 0.25秒前に離してもOK
                    doubleLongNotesJudgeTimes[chart.notes[i].lane +1].Add(barSpace * (chart.notes[i].time + chart.notes[i].length) / (laneManager.settingSpeed /2f) -0.25f);

                    break;

                case 4: // スキルエネルギー
                    totalNotesCount--; // ノーツ数に含まない
                    Instantiate(skillEnergyObject, new Vector3(8-(8-chart.notes[i].lane)*2, 1, barSpace * chart.notes[i].time), skillEnergyObject.transform.rotation);
                    lineObjectsJudgeTimes[chart.notes[i].lane].Add(barSpace * chart.notes[i].time / (laneManager.settingSpeed /2f));
                    lineObjectsJudgeTimes[chart.notes[i].lane].Add(4);

                    break;

                case 5: // 発射体
                    totalNotesCount--; // ノーツ数に含まない
                    Instantiate(attackProjectile, new Vector3(8-(8-chart.notes[i].lane)*2, 1, barSpace * chart.notes[i].time), attackProjectile.transform.rotation);
                    lineObjectsJudgeTimes[chart.notes[i].lane].Add(barSpace * chart.notes[i].time / (laneManager.settingSpeed /2f));
                    lineObjectsJudgeTimes[chart.notes[i].lane].Add(5);

                    break;

                case 6: // 炎
                    totalNotesCount--; // ノーツ数に含まない
                    //Instantiate(attackFlame, new Vector3(8-(8-chart.notes[i].lane)*2, 1, barSpace * chart.notes[i].time), attackFlame.transform.rotation);
                    //警告線
                    placeObjectsJudgeTimes[chart.notes[i].lane].Add(barSpace * chart.notes[i].time / (laneManager.settingSpeed /2f) -2);
                    //本体
                    placeObjectsJudgeTimes[chart.notes[i].lane].Add(barSpace * chart.notes[i].time / (laneManager.settingSpeed /2f));
                    placeObjectsJudgeTimes[chart.notes[i].lane].Add(barSpace * chart.notes[i].time / (laneManager.settingSpeed /2f) +chart.notes[i].length);

                    break;

                case 7: // ソフラン レーン番号は同じ時間にかぶらなきゃ自由 length倍の速さになる
                    totalNotesCount--;
                    lineObjectsJudgeTimes[chart.notes[i].lane].Add(barSpace * chart.notes[i].time / (laneManager.settingSpeed /2f));
                    lineObjectsJudgeTimes[chart.notes[i].lane].Add(7);
                    lineObjectsJudgeTimes[chart.notes[i].lane].Add(chart.notes[i].length);

                    break;
            }
        }

        // list空になったときに空のindex指定するのを防ぐために最後に追加
        for (int i = 0; i < 8; i++) 
        {
            singleNotesJudgeTimes[i].Add(10000);
            doubleNotesJudgeTimes[i].Add(10000);
            singleLongNotesJudgeTimes[i].Add(10000);
            doubleLongNotesJudgeTimes[i].Add(10000);
            singleLongNotesJudgeTimes[i].Add(20000);
            doubleLongNotesJudgeTimes[i].Add(20000);
            lineObjectsJudgeTimes[i].Add(10000);
            lineObjectsJudgeTimes[i].Add(10000);
            placeObjectsJudgeTimes[i].Add(10000);
        }
        lineObjectsJudgeTimes[8].Add(10000);
        lineObjectsJudgeTimes[8].Add(10000);
        placeObjectsJudgeTimes[8].Add(10000);
        placeObjectsJudgeTimes[8].Add(10000);

        Invoke("tagFind", 1f); //startで同時にタグ取得するとバグるので1秒遅らす
    }

    void tagFind() //叩いたやつを非表示にする用にタグ取得
    {
        for (int i = 0; i < 8; i++)
        {
            GameObject[] foundObjects = GameObject.FindGameObjectsWithTag("single"+i);
            notesObject[i] = foundObjects;
        }
        for (int i = 0; i < 8; i++)
        {
            GameObject[] foundObjects = GameObject.FindGameObjectsWithTag("double"+i);
            notesObject[i+8] = foundObjects;
        }
    }
}