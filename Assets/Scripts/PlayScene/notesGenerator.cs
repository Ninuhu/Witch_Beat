using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class chartData
{
    public Chart[] notes;
}

[Serializable]
public class Chart
{
    public int lane;   // どこのレーンか
    public float time; // 何小節目か
    public int note;   // なんのノーツか
    public float length; // ロングノーツ、攻撃などの持続時間
}



/// <summary>
/// 譜面JSON（Resources/Notes/xxx.json）を読み込み、ノーツオブジェクトの生成と
/// 判定用タイムテーブル（レーンごとのList&lt;double&gt;）の構築を担当するクラス
/// characterControllerはここで作られたリストを直接参照して判定を行う
/// </summary>


public class notesGenerator : MonoBehaviour
{
    // ===== 判定用タイムテーブル（characterControllerが参照する） =====
    public List<double>[] singleNotesJudgeTimes; // 叩いた時の判定用（シングルタップ）
    public List<double>[] doubleNotesJudgeTimes; // ダブルタップ（符号で左右のレーンを区別）
    public List<double>[] singleLongNotesJudgeTimes; // シングルロング（開始/終了の2値を交互に格納）
    public List<double>[] doubleLongNotesJudgeTimes; // ダブルロング（同上、符号で左右を区別）

    public List<double>[] lineObjectsJudgeTimes; // スキル玉・攻撃玉・ソフランなどのタイミング
    public List<double>[] placeObjectsJudgeTimes; // 炎など、場所に設置されるギミックのタイミング

    public GameObject[][] notesObject; // 叩いた後に非表示にするためのノーツ本体一覧（タグ検索で取得）

    string loadChart; // 読み込む譜面ファイル名（拡張子なし）
    float barSpace; // 1小節の長さ（laneManagerから取得）

    public int selectedMusicId; // 選曲シーンで選んだ曲のID (1:mopemope 2:shiningstar 3:song3)
    public string selectedMusic; // 曲名（IDから自動セットされる。表示用などにそのまま使える）
    public int selectedDifficulty; // 難易度 (0:normal 1:hard, -1:debug)
    public int totalNotesCount; // 判定対象になるノーツの総数（スキル玉等は含まない）

    // ID -> 曲名 の対応表（0番目はダミーで未使用）
    static readonly string[] musicNames = { "", "mopemope", "shiningstar", "song3" }; // song3は未実装（後で入れる）

    // list末尾に入れておく番兵値。判定処理側で「もうノーツが無い」ときにlist[0]へ安全にアクセスできるようにするためのダミー時刻（実際には絶対来ない未来の値）
    const double SENTINEL_TIME_PRIMARY = 10000;
    const double SENTINEL_TIME_SECONDARY = 20000;

    [SerializeField] GameObject tapSingle, longSingle, tapDouble, longDouble, skillEnergyObject, attackProjectile;
    [SerializeField] laneManager laneManager;


    void Start()
    {
        LoadSelectionFromGameSettings();
        InitializeJudgeArrays();

        chartData chart = LoadChartData();
        if (chart == null) return; // 譜面ファイルが見つからない場合はここで中断（LoadChartData内でログ出力済み）

        totalNotesCount = chart.notes.GetLength(0); // タップ、ロング関係なくノーツ数集計
        GenerateNotesFromChart(chart);

        AppendSentinelValues();

        Invoke("tagFind", 1f); // startで同時にタグ取得するとバグるので1秒遅らす
    }

    
    // 選曲シーンで選ばれた曲ID・難易度をGameSettingsから取り込み、曲名も導出
    
    void LoadSelectionFromGameSettings()
    {
        selectedMusicId = GameSettings.selectedMusicId;
        selectedDifficulty = GameSettings.selectedDifficulty;
        selectedMusic = musicNames[selectedMusicId];

        barSpace = laneManager.barSpace; // 1小節の長さ取得
    }

    
    // 判定用タイムテーブルの配列を、レーン数分（8レーン、線上オブジェクトは9）だけ確保
    
    void InitializeJudgeArrays()
    {
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
    }

    
    // Resources/Notes/ 以下から譜面JSONを読み込む
    // ファイル名は「曲名+難易度番号」（例: shiningstar0, shiningstar1）で、難易度-1のときだけ特別に debug.json を読む。
    
    chartData LoadChartData()
    {
        // 通常時: "曲名"+"難易度番号"  例) shiningstar0(normal), shiningstar1(hard)
        // デバッグ時(difficulty == -1): debug.json
        loadChart = (selectedDifficulty == -1)
            ? "debug": musicNames[selectedMusicId] + selectedDifficulty;

        // 入力ファイルは Resources/Notes/{loadChart}.json
        // ※ビルド後に読み込めなくなるから、Application.dataPath直読みではなくResources.Loadを使用
        TextAsset chartAsset = Resources.Load<TextAsset>("Notes/" + loadChart);
        if (chartAsset == null)
        {
            Debug.LogError("譜面ファイルが見つかりません: Resources/Notes/" + loadChart + ".json");
            return null;
        }

        return JsonUtility.FromJson<chartData>(chartAsset.text);
    }



    //|||||||||||||||||||||||||||||||||||||||||||
    // 読み込んだ譜面データを1件ずつノーツの種類別に処理
    void GenerateNotesFromChart(chartData chart)
    {
        for (int i = 0; i < chart.notes.GetLength(0); i++)
        {
            Chart note = chart.notes[i];
            switch (note.note)
            {
                case 0: GenerateSingleTapNote(note); break;
                case 2: GenerateDoubleTapNote(note); break;
                case 1: GenerateSingleLongNote(note); break;
                case 3: GenerateDoubleLongNote(note); break;
                case 4: GenerateSkillEnergyObject(note); break;
                case 5: GenerateAttackProjectile(note); break;
                case 6: GenerateFlameHazard(note); break;
                case 7: GenerateSoflanEvent(note); break;
            }
        }
    }

    // レーン番号からX座標を求める（lane:0の時x:-7, lane:7の時x:7）
    static float LaneToX_Note(int lane) => 7 - (7 - lane) * 2;
    // スキル玉・攻撃玉など「線上オブジェクト」用のX座標（ノーツとは原点が異なる）
    static float LaneToX_LineObject(int lane) => 8 - (8 - lane) * 2;

    
    // note.time（小節位置）を、判定に使う「秒」に変換
    float ToJudgeSeconds(float noteTime) => barSpace * noteTime / (laneManager.settingSpeed / 2f);

    void GenerateSingleTapNote(Chart note)
    {
        Instantiate(tapSingle, new Vector3(LaneToX_Note(note.lane), 0, barSpace * note.time), tapSingle.transform.rotation);
        singleNotesJudgeTimes[note.lane].Add(ToJudgeSeconds(note.time));
    }

    void GenerateDoubleTapNote(Chart note)
    {
        Instantiate(tapDouble, new Vector3(LaneToX_Note(note.lane), 0, barSpace * note.time), tapDouble.transform.rotation);
        // ダブルタップの左レーンは時間マイナスで記録（押した瞬間に符号で左右を判定するため）
        doubleNotesJudgeTimes[note.lane].Add(ToJudgeSeconds(note.time) * -1);
        doubleNotesJudgeTimes[note.lane + 1].Add(ToJudgeSeconds(note.time));
    }

    void GenerateSingleLongNote(Chart note)
    {
        totalNotesCount++; // ロングノーツの場合カウント+1

        GameObject singleLongObj = Instantiate(longSingle, new Vector3(LaneToX_Note(note.lane), 0, barSpace * note.time), longSingle.transform.rotation);
        GameObject singleLine = singleLongObj.transform.GetChild(0).gameObject;
        // 親objectのscaleが0.6なので10/6を掛けて見た目の長さを合わせる
        singleLine.transform.localPosition = new Vector3(0, 0, note.length * barSpace * 0.5f * 10 / 6);
        singleLine.transform.localScale = new Vector3(1, 1, note.length * barSpace * 10 / 6);

        singleNotesJudgeTimes[note.lane].Add(ToJudgeSeconds(note.time));

        singleLongNotesJudgeTimes[note.lane].Add(ToJudgeSeconds(note.time) + 0.25f); // 開始時刻 0.25秒後から判定
        singleLongNotesJudgeTimes[note.lane].Add(ToJudgeSeconds(note.time + note.length) - 0.25f); // 終了時刻 0.25秒前に離してもOK
    }

    //||||||||||||||||||||||||||||||||||||
    // ダブルロングノーツ
    void GenerateDoubleLongNote(Chart note)
    {
        totalNotesCount++;

        GameObject doubleLongObj = Instantiate(longDouble, new Vector3(LaneToX_Note(note.lane), 0, barSpace * note.time), longDouble.transform.rotation);
        GameObject doubleLine = doubleLongObj.transform.GetChild(0).gameObject;
        doubleLine.transform.localPosition = new Vector3(0, -1.25f, note.length * barSpace * 0.5f * 10 / 6);
        doubleLine.transform.localScale = new Vector3(1, 2, note.length * barSpace * 10 / 6);

        doubleNotesJudgeTimes[note.lane].Add(ToJudgeSeconds(note.time) * -1);
        doubleNotesJudgeTimes[note.lane + 1].Add(ToJudgeSeconds(note.time));

        doubleLongNotesJudgeTimes[note.lane].Add((ToJudgeSeconds(note.time) + 0.25f) * -1); // 開始時刻 0.25秒後から判定
        doubleLongNotesJudgeTimes[note.lane + 1].Add(ToJudgeSeconds(note.time) + 0.25f);

        doubleLongNotesJudgeTimes[note.lane].Add((ToJudgeSeconds(note.time + note.length) - 0.25f) * -1); // 終了時刻 0.25秒前に離してもOK
        doubleLongNotesJudgeTimes[note.lane + 1].Add(ToJudgeSeconds(note.time + note.length) - 0.25f);
    }

    //|||||||||||||||||||||||||||||
    // スキルエネルギー
    void GenerateSkillEnergyObject(Chart note)
    {
        totalNotesCount--; // ノーツ数に含まない
        Instantiate(skillEnergyObject, new Vector3(LaneToX_LineObject(note.lane), 1, barSpace * note.time), skillEnergyObject.transform.rotation);
        lineObjectsJudgeTimes[note.lane].Add(ToJudgeSeconds(note.time));
        lineObjectsJudgeTimes[note.lane].Add(4); // 種別4=スキルエネルギー（characterController側の switch と対応）
    }


    //|||||||||||||||||||||||||||||
    // アタックエネルギー
    void GenerateAttackProjectile(Chart note)
    {
        totalNotesCount--; // ノーツ数に含まない
        Instantiate(attackProjectile, new Vector3(LaneToX_LineObject(note.lane), 1, barSpace * note.time), attackProjectile.transform.rotation);
        lineObjectsJudgeTimes[note.lane].Add(ToJudgeSeconds(note.time));
        lineObjectsJudgeTimes[note.lane].Add(5); // 種別5=攻撃（発射体）
    }


    //||||||||||||||||||||||||||||||
    // GenerateFlameHazard
    void GenerateFlameHazard(Chart note)
    {
        totalNotesCount--; // ノーツ数に含まない
        // 警告線 → 本体出現 → 消滅、の3段階の時刻を保持する
        placeObjectsJudgeTimes[note.lane].Add(ToJudgeSeconds(note.time) - 2); // 警告線
        placeObjectsJudgeTimes[note.lane].Add(ToJudgeSeconds(note.time));     // 本体
        placeObjectsJudgeTimes[note.lane].Add(ToJudgeSeconds(note.time) + note.length); // 消滅
    }


    //||||||||||||||||||||||||||||
    // ソフラン
    void GenerateSoflanEvent(Chart note)
    {
        // ソフラン：レーン番号は同じ時間にかぶらなければ自由。length倍の速さになる
        totalNotesCount--;
        lineObjectsJudgeTimes[note.lane].Add(ToJudgeSeconds(note.time));
        lineObjectsJudgeTimes[note.lane].Add(7); // 種別7=ソフラン
        lineObjectsJudgeTimes[note.lane].Add(note.length);
    }

    
    // 各リストの末尾に番兵値を追加
    // list.RemoveAt(0)を繰り返して空になった後でも list[0] へ安全にアクセスできるようにするため
    void AppendSentinelValues()
    {
        for (int i = 0; i < 8; i++)
        {
            singleNotesJudgeTimes[i].Add(SENTINEL_TIME_PRIMARY);
            doubleNotesJudgeTimes[i].Add(SENTINEL_TIME_PRIMARY);
            singleLongNotesJudgeTimes[i].Add(SENTINEL_TIME_PRIMARY);
            doubleLongNotesJudgeTimes[i].Add(SENTINEL_TIME_PRIMARY);
            singleLongNotesJudgeTimes[i].Add(SENTINEL_TIME_SECONDARY);
            doubleLongNotesJudgeTimes[i].Add(SENTINEL_TIME_SECONDARY);
            lineObjectsJudgeTimes[i].Add(SENTINEL_TIME_PRIMARY);
            lineObjectsJudgeTimes[i].Add(SENTINEL_TIME_PRIMARY);
            placeObjectsJudgeTimes[i].Add(SENTINEL_TIME_PRIMARY);
        }

        lineObjectsJudgeTimes[8].Add(SENTINEL_TIME_PRIMARY);
        lineObjectsJudgeTimes[8].Add(SENTINEL_TIME_PRIMARY);
        placeObjectsJudgeTimes[8].Add(SENTINEL_TIME_PRIMARY);
        placeObjectsJudgeTimes[8].Add(SENTINEL_TIME_PRIMARY);
    }

    
    // タグ検索で、叩いた後に非表示にする対象のノーツオブジェクト一覧を取得する
    // Start()と同フレームで行うとタグが引けないことがあるため、1秒遅らせて実行してる
    void tagFind()
    {
        for (int i = 0; i < 8; i++)
        {
            notesObject[i] = GameObject.FindGameObjectsWithTag("single" + i);
        }
        for (int i = 0; i < 8; i++)
        {
            notesObject[i + 8] = GameObject.FindGameObjectsWithTag("double" + i);
        }
    }
}