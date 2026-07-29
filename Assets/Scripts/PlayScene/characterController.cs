using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;


// プレイヤーキャラクターの移動・キー入力受付・ノーツ判定・スコア更新・HUD表示を
// 担当するクラス。Playシーンの中核ロジック。
//
// 役割の整理:
//   ・時間の基準は laneManager.t を毎フレーム取得して使う
//   ・譜面データ（判定用タイムテーブル）は notesGenerator が保持しているものを直接参照する
//   ・曲終了時、集計済みの結果を moveToResult 経由でResultシーンへ渡す

public class characterController : MonoBehaviour
{
    //||||||||||||||||||||||||||||||||||||||||
    // フィールド
    public int now; // キャラ現在位置（-8～8の偶数。0がレーン中央）

    /* スキル
    0:アジャスト (weak->break 5回) 
    1:コンティニュー (コンボカット無し 4回) 
    2:リカバー (miss -> break 2回) 
    */
    public int skillType;

    int difficulty;
    public int criticalBreakCount, breakCount, weakCount, lostCount;
    int comboCount;
    public int maxComboCount;
    public int totalNotesCount;
    float y;
    public double bossHp, skillEnergy;
    public double techScore;
    double t;
    double criticalBreakJudgeTime;
    double breakJudgeTime;
    double weakJudgeTime;
    double nowPlayingSpeed;
    public int damageTaken;

    bool skillActive;
    bool end;

    int[] notesDestroyIndex;

    // 「今、自機がいる4レーン分」の判定リストへの参照。ActiveLaneChange()で自機移動のたびに差し替える
    List<double>[] activeLaneSingleJudges;
    List<double>[] activeLaneDoubleJudges;
    List<double>[] activeLaneSingleLongJudges;
    List<double>[] activeLaneDoubleLongJudges;

    List<double> emptyArray; // 判定対象レーンが存在しない場合に割り当てるダミーリスト

    public List<double> gapSaveList;

    [SerializeField] Transform trans, leftLane, rightLane;
    [SerializeField] laneManager laneManager;
    [SerializeField] notesGenerator notesGenerator;
    [SerializeField] moveToResult moveToResult;
    [SerializeField] Transform[] keyBeamTransforms = new Transform[4];
    [SerializeField] Transform judgeImageTrans;
    [SerializeField] Sprite[] judgeImageSprites = new Sprite[4];
    [SerializeField] Image judgeImage, bossHpGaugeImage, skillEnergyGaugeImage;
    [SerializeField] TextMeshProUGUI totalText, comboText, bossHpText;
    [SerializeField] Transform totalTextTrans, comboTextTrans, bossHpTextTrans;
    [SerializeField] GameObject skillActiveEffect;
    [SerializeField] GameObject[] stripeCautionObj = new GameObject[9];
    [SerializeField] GameObject[] flameObj = new GameObject[9];

    // 4レーン分の入力キー（D, F, J, K の順）。ロングノーツ判定などで配列参照に使う
    static readonly KeyCode[] laneKeys = { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K };

    // 判定の結果種別
    enum JudgeResult { None, Critical, Break, Weak }




    //||||||||||||||||||||||||||||||||||||||||
    // 初期化
    void Start()
    {
        // Inspectorでの参照はシーン構成（DontDestroySingleObjectの重複破棄等）により
        // 別インスタンスを指している場合があるため、生き残っている本物を取り直す
        if (DontDestroySingleObject.Instance != null)
        {
            moveToResult = DontDestroySingleObject.Instance.GetComponent<moveToResult>();
        }

        now = 0;
        //difficulty = 1;
        y = 0f;
        bossHp = 0;
        techScore = 0;
        skillEnergy = 0;
        totalNotesCount = 1; // 1以上に初期化
        criticalBreakCount = 0;
        breakCount = 0;
        weakCount = 0;
        lostCount = 0;
        damageTaken = 0;
        skillActive = false;
        end = false;
        skillEnergyGaugeImage.fillAmount = (float)skillEnergy / 100f;
        nowPlayingSpeed = 1;

        difficulty = notesGenerator.selectedDifficulty;
        notesDestroyIndex = new int[16];
        emptyArray = new List<double>();
        gapSaveList = new List<double>();
        emptyArray.Add(10000); // 空のindex指定するエラー回避のため
        emptyArray.Add(20000);

        // 判定用の配列初期化（自機の初期位置 now=0 に対応する4レーン分）
        activeLaneSingleJudges = new List<double>[4];
        activeLaneDoubleJudges = new List<double>[4];
        activeLaneSingleLongJudges = new List<double>[4];
        activeLaneDoubleLongJudges = new List<double>[4];
        for (int i = 0; i < 4; i++)
        {
            activeLaneSingleJudges[i] = notesGenerator.singleNotesJudgeTimes[now / 2 + 2 + i];
            activeLaneDoubleJudges[i] = notesGenerator.doubleNotesJudgeTimes[now / 2 + 2 + i];
            activeLaneSingleLongJudges[i] = notesGenerator.singleLongNotesJudgeTimes[now / 2 + 2 + i];
            activeLaneDoubleLongJudges[i] = notesGenerator.doubleLongNotesJudgeTimes[now / 2 + 2 + i];
        }

        if (difficulty == 0)
        {
            // CriticalBreak -- Break -- Weak
            // 50ms -- 75ms -- 100ms
            criticalBreakJudgeTime = 0.05;
            breakJudgeTime = 0.075;
            weakJudgeTime = 0.1;
        }
        else
        {
            // CriticalBreak -- Break -- Weak
            // 30ms -- 60ms -- 100ms
            criticalBreakJudgeTime = 0.03;
            breakJudgeTime = 0.06;
            weakJudgeTime = 0.1;
        }

        skillType = 2;

        Invoke("TotalNotesCheck", 1); //総ノーツ数取得
    }

    void TotalNotesCheck()
    {
        totalNotesCount = notesGenerator.totalNotesCount;
    }




    //||||||||||||||||||||||||||||||||||||||||
    // スコア・状態更新（判定結果を受けて呼ばれる一連の処理）

    void BossHpChange()
    {
        bossHp = (double)((criticalBreakCount + breakCount * 0.9 + weakCount * 0.4) / totalNotesCount * 100);
        bossHpGaugeImage.fillAmount = 1 - (float)bossHp / 100f;
    }

    void SkillEnergyChange(double amount)
    {
        if (skillActive == false)
        {
            skillEnergy += amount;
            skillEnergyGaugeImage.fillAmount = (float)skillEnergy / 100f;
        }
        if (skillEnergy >= 100)
        {
            skillEnergy = 100;
            skillActive = true;
            skillActiveEffect.SetActive(true);
        }
    }

    void LongNotesPressed()
    {
        //Debug.Log("LongNote OK");
        comboCount++;
        comboTextTrans.localScale = new Vector3(0.5f, 0.5f, 1);
    }

    void CriticalBreak(double gap)
    {
        //Debug.Log("CriticalBreak");
        judgeImage.sprite = judgeImageSprites[0];
        judgeImageTrans.localScale = new Vector3(0.5f, 0.5f, 1);
        comboTextTrans.localScale = new Vector3(0.5f, 0.5f, 1);
        criticalBreakCount++;
        comboCount++;
        techScore += 1000000 / totalNotesCount;
        if (maxComboCount <= comboCount) maxComboCount = comboCount;
        BossHpChange();
        SkillEnergyChange(0.4);
        // gap 0の場合はロングノーツなのでリザルトのグラフにカウントしない
        if (gap != 0 && Math.Abs(gap) <= criticalBreakJudgeTime * nowPlayingSpeed) gapSaveList.Add(gap);
    }

    void Break(double gap)
    {
        //Debug.Log("Break "+gap);
        judgeImage.sprite = judgeImageSprites[1];
        judgeImageTrans.localScale = new Vector3(0.5f, 0.5f, 1);
        comboTextTrans.localScale = new Vector3(0.5f, 0.5f, 1);
        breakCount++;
        comboCount++;
        techScore += 1000000 / totalNotesCount * 0.9;
        if (maxComboCount <= comboCount) maxComboCount = comboCount;
        BossHpChange();
        SkillEnergyChange(0.36);
        // gap 0の場合はリカバースキルなのでカウントなし
        if (gap != 0) gapSaveList.Add(gap);
    }

    void Weak(double gap)
    {
        //Debug.Log("Weak "+gap);
        if (skillActive == true && skillType == 0)
        {
            Break(gap);
            skillEnergy -= 20;
            return;
        }
        judgeImage.sprite = judgeImageSprites[2];
        judgeImageTrans.localScale = new Vector3(0.5f, 0.5f, 1);
        comboTextTrans.localScale = new Vector3(0.5f, 0.5f, 1);
        weakCount++;
        comboCount++;
        techScore += 1000000 / totalNotesCount * 0.4;
        if (maxComboCount <= comboCount) maxComboCount = comboCount;
        BossHpChange();
        SkillEnergyChange(0.16);
        gapSaveList.Add(gap);
    }

    void Lost()
    {
        //Debug.Log("Lost");
        if (skillActive == true && skillType == 2)
        {
            Break(0);
            skillEnergy -= 50;
            return;
        }
        judgeImage.sprite = judgeImageSprites[3];
        judgeImageTrans.localScale = new Vector3(0.5f, 0.5f, 1);
        lostCount++;
        if (skillActive == true && skillType == 1)
        {
            comboCount++;
            if (maxComboCount <= comboCount) maxComboCount = comboCount;
            skillEnergy -= 25;
        }
        else comboCount = 0;
        
    }

    void OnDamage()
    {
        Debug.Log("damage");
        damageTaken++;
    }




    //||||||||||||||||||||||||||||||||||||||||
    // レーン管理（自機移動で「今判定すべき4レーン」を切り替える）

    
    // 自機が移動したときに、判定対象となる4レーン分の参照を更新
    // スロット0=D, 1=F, 2=J, 3=K に対応
    
    void ActiveLaneChange()
    {
        UpdateActiveLaneSlot(1, now / 2 + 3); // fレーン
        UpdateActiveLaneSlot(2, now / 2 + 4); // jレーン
        UpdateActiveLaneSlot(0, now / 2 + 2); // dレーン
        UpdateActiveLaneSlot(3, now / 2 + 5); // kレーン
    }

    
    // nowの範囲は±8に制限されているため、sourceIndexは実際には-1～8の範囲にしかならない（0～7の範囲外になるのはどちらか片側のみ）
    // そのため上下の範囲チェックを両方行っても元のロジックと結果は変化なし    
    void UpdateActiveLaneSlot(int slotIndex, int sourceIndex)
    {
        bool outOfRange = sourceIndex < 0 || sourceIndex > 7;
        if (outOfRange)
        {
            activeLaneSingleJudges[slotIndex] = emptyArray;
            activeLaneDoubleJudges[slotIndex] = emptyArray;
            activeLaneSingleLongJudges[slotIndex] = emptyArray;
            activeLaneDoubleLongJudges[slotIndex] = emptyArray;
        }
        else
        {
            // 参照を渡すことでもともとのlistを直接変更する
            activeLaneSingleJudges[slotIndex] = notesGenerator.singleNotesJudgeTimes[sourceIndex];
            activeLaneDoubleJudges[slotIndex] = notesGenerator.doubleNotesJudgeTimes[sourceIndex];
            activeLaneSingleLongJudges[slotIndex] = notesGenerator.singleLongNotesJudgeTimes[sourceIndex];
            activeLaneDoubleLongJudges[slotIndex] = notesGenerator.doubleLongNotesJudgeTimes[sourceIndex];
        }
    }



    //||||||||||||||||||||||||||||||||||||||||
    // 通常ノーツの入力判定

    // gapの絶対値から判定ランクを決める（Critical/Break/Weak/範囲外）    
    JudgeResult DetermineJudge(double gap)
    {
        double absGap = Math.Abs(gap);
        if (absGap <= criticalBreakJudgeTime * nowPlayingSpeed) return JudgeResult.Critical;
        if (absGap <= breakJudgeTime * nowPlayingSpeed) return JudgeResult.Break;
        if (absGap <= weakJudgeTime * nowPlayingSpeed) return JudgeResult.Weak;
        return JudgeResult.None;
    }


    // fレーンジャッジの指定index: 3 +now/2 （マイナスならemptyArray）
    // dレーンジャッジの指定index: 2 +now/2 （マイナスならemptyArray）
    // jレーンジャッジの指定index: 4 +now/2 （7より大きいならemptyArray）
    
    // laneNumber（0=D,1=F,2=J,3=K）のキーが押されたときの判定処理
    // シングルノーツとダブルノーツ、時間的に近い方を優先して判定
    void KeyPressJudge(int laneNumber)
    {
        bool singleIsCloser = activeLaneSingleJudges[laneNumber][0] <= Math.Abs(activeLaneDoubleJudges[laneNumber][0]);
        if (singleIsCloser)
        {
            ProcessSingleNoteJudge(laneNumber);
        }
        else // ダブルタップは時間がマイナスかプラスかで左右どっちか判定
        {
            ProcessDoubleNoteJudge(laneNumber);
        }
    }

    void ProcessSingleNoteJudge(int laneNumber)
    {
        double gap = activeLaneSingleJudges[laneNumber][0] - t;
        JudgeResult result = DetermineJudge(gap);
        if (result == JudgeResult.None) return; // 判定範囲外なら何もしない

        switch (result)
        {
            case JudgeResult.Critical: CriticalBreak(gap); break;
            case JudgeResult.Break: Break(gap); break; // どれくらいずれているかを引数に入れる
            case JudgeResult.Weak: Weak(gap); break;
        }

        int noteIndex = now / 2 + 2 + laneNumber;
        notesGenerator.notesObject[noteIndex][notesDestroyIndex[noteIndex]].GetComponent<noteMover>().Destroy();
        activeLaneSingleJudges[laneNumber].RemoveAt(0);
        notesDestroyIndex[noteIndex]++;
    }

    void ProcessDoubleNoteJudge(int laneNumber)
    {
        double doubleTime = activeLaneDoubleJudges[laneNumber][0];
        double gapForCompare = Math.Abs(doubleTime) - t;
        JudgeResult result = DetermineJudge(gapForCompare);
        if (result == JudgeResult.None) return;

        /*
        注意: 元のコードでは Critical 判定のときだけ gap 引数にシングルノーツ側の時刻（activeLaneSingleJudges）を使ってる
        Break/Weakでは Math.Abs(doubleTime)-t を使っており、Criticalだけ非対称になっている
        */
        switch (result)
        {
            case JudgeResult.Critical: CriticalBreak(activeLaneSingleJudges[laneNumber][0] - t); break;
            case JudgeResult.Break: Break(gapForCompare); break;
            case JudgeResult.Weak: Weak(gapForCompare); break;
        }

        bool isLeftSide = doubleTime < 0; // 叩いたのが左側か右側か
        int judgeTimesIndex = isLeftSide ? (now / 2 + 3 + laneNumber) : (now / 2 + 1 + laneNumber);
        int destroyIndex = isLeftSide ? (now / 2 + 2 + laneNumber + 8) : (now / 2 + 1 + laneNumber + 8);

        notesGenerator.doubleNotesJudgeTimes[judgeTimesIndex].RemoveAt(0);
        notesGenerator.notesObject[destroyIndex][notesDestroyIndex[destroyIndex]].GetComponent<noteMover>().Destroy();
        notesDestroyIndex[destroyIndex]++;

        activeLaneDoubleJudges[laneNumber].RemoveAt(0);
    }

    //||||||||||||||||||||||||||||||||||||||||
    //各処理を役割ごとのメソッドへ委譲するだけ
    void Update()
    {
        t = laneManager.t;

        UpdateNowFromInput();
        CheckMissedNotes();
        HandleFJInput();
        HandleDKInput();
        UpdateSingleLongNotes();
        UpdateDoubleLongNotes();
        UpdateFieldObjects();
        HandleKeyUpBeams();
        UpdateCharacterTransform();
        UpdateHud();
        UpdateSkillState();
        CheckSongEnd();
    }

    

    // E/R（左移動）、U/I（右移動）の入力を受けて自機位置(now)を更新
    void UpdateNowFromInput()
    {
        if ((Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.R)) && now > -8 && Mathf.Abs(trans.localPosition.x - now) < 1f)
        {
            now -= 2;
            trans.Rotate(0f, 0f, 15f);
            ActiveLaneChange();
        }
        if ((Input.GetKeyDown(KeyCode.U) || Input.GetKeyDown(KeyCode.I)) && now < 8 && Mathf.Abs(trans.localPosition.x - now) < 1f)
        {
            now += 2;
            trans.Rotate(0f, 0f, -15f);
            ActiveLaneChange();
        }
    }


    // 全8レーンを対象に、判定タイミングを過ぎても叩かれなかったノーツをLost扱いにする
    void CheckMissedNotes()
    {
        for (int i = 0; i < 8; i++)
        {
            // 逃したノーツ用
            if ((notesGenerator.singleNotesJudgeTimes[i][0] + 0.1) * nowPlayingSpeed < t)
            {
                Lost();
                notesGenerator.notesObject[i][notesDestroyIndex[i]].GetComponent<noteMover>().Destroy();
                notesGenerator.singleNotesJudgeTimes[i].RemoveAt(0);
                notesDestroyIndex[i]++;
            }
            if ((Math.Abs(notesGenerator.doubleNotesJudgeTimes[i][0]) + 0.1) * nowPlayingSpeed < t)
            {
                if (notesGenerator.doubleNotesJudgeTimes[i][0] < 0)
                {
                    notesGenerator.doubleNotesJudgeTimes[i].RemoveAt(0);
                    Lost();
                    notesGenerator.notesObject[i + 8][notesDestroyIndex[i + 8]].GetComponent<noteMover>().Destroy();
                    notesDestroyIndex[i + 8]++;
                }
                else
                {
                    notesGenerator.doubleNotesJudgeTimes[i].RemoveAt(0);
                }
            }
        }
    }



    // F（レーン1）・J（レーン2）キーの押下判定とキービームの表示
    void HandleFJInput()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            KeyPressJudge(1);
            keyBeamTransforms[1].localPosition = new Vector3(-1 + now, 0.002f, 2.5f);
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            KeyPressJudge(2);
            keyBeamTransforms[2].localPosition = new Vector3(1 + now, 0.002f, 2.5f);
        }
    }



    // D（レーン0）・K（レーン3）キーの押下判定とキービームの表示
    void HandleDKInput()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            KeyPressJudge(0);
            keyBeamTransforms[0].localPosition = new Vector3(-3 + now, 0.002f, 2.5f);
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            KeyPressJudge(3);
            keyBeamTransforms[3].localPosition = new Vector3(3 + now, 0.002f, 2.5f);
        }
    }



    //||||||||||||||||||||||||||||||||||||||||
    // シングルロングノーツ
    /*
    押しっぱなしで判定するシングルロングノーツの処理。
    30/bpm 秒ごとに1回「そのタイミングでキーが押されているか」をチェックし、
    押されていればコンボ継続、離されていればLostにする。
    */
    void UpdateSingleLongNotes()
    {
        for (int i = 0; i < 4; i++)
        {
            if (activeLaneSingleLongJudges[i][0] > t) continue; //[0]で開始、[1]まで
            if (t - activeLaneSingleLongJudges[i][0] <= (30.0f / laneManager.bpm)) continue; //bpmの二倍でカウント 時間経過したら

            ProcessSingleLongNoteTick(i);

            if (activeLaneSingleLongJudges[i][1] < activeLaneSingleLongJudges[i][0])
            { //上記の処理で[0] >= [1]になったら終了
                activeLaneSingleLongJudges[i].RemoveAt(0);
                activeLaneSingleLongJudges[i].RemoveAt(0);
                CriticalBreak(0);
            }
        }

        // 上記の処理終了後に全レーンで押されてないノーツあるか確認
        for (int i = 0; i < 8; i++)
        {
            if (notesGenerator.singleLongNotesJudgeTimes[i][0] <= t - (30.0f / laneManager.bpm) - weakJudgeTime)
            {
                notesGenerator.singleLongNotesJudgeTimes[i].RemoveAt(0);
                notesGenerator.singleLongNotesJudgeTimes[i].RemoveAt(0);
                Lost();
            }
        }
    }

    void ProcessSingleLongNoteTick(int laneIndex)
    {
        if (Input.GetKey(laneKeys[laneIndex]))
        {
            LongNotesPressed();
            //経過時間分をlistに追加してから先頭削除
            activeLaneSingleLongJudges[laneIndex].Insert(1, activeLaneSingleLongJudges[laneIndex][0] + (30.0f / laneManager.bpm));
            activeLaneSingleLongJudges[laneIndex].RemoveAt(0);
        }
        else
        {
            Lost();
            activeLaneSingleLongJudges[laneIndex].RemoveAt(0);
            activeLaneSingleLongJudges[laneIndex].RemoveAt(0);
        }
    }




    //||||||||||||||||||||||||||||||||||||||||
    // ダブルロングノーツ（隣接2レーン判定のロングノーツ）

    // ダブルロングノーツの処理。判定時刻は正負の符号で左右どちら側のノーツかを表してて
    // 左側ノーツは i と i+1、右側ノーツは i と i-1 のペアで開始・終了時刻を同時に更新
    void UpdateDoubleLongNotes()
    {
        for (int i = 0; i < 4; i++)
        {
            if (Math.Abs(activeLaneDoubleLongJudges[i][0]) > t) continue; //[0]で開始、[1]まで

            if (t - Math.Abs(activeLaneDoubleLongJudges[i][0]) > (30.0f / laneManager.bpm))
            {
                ProcessDoubleLongNoteTick(i);
            }



            CheckDoubleLongNoteCompletion(i);
        }

        // 上記の処理終了後に全レーンで押されてないノーツあるか確認
        for (int i = 0; i < 8; i++)
        {
            if (Math.Abs(notesGenerator.doubleLongNotesJudgeTimes[i][0]) <= t - (30.0f / laneManager.bpm) - weakJudgeTime)
            {
                notesGenerator.doubleLongNotesJudgeTimes[i].RemoveAt(0);
                notesGenerator.doubleLongNotesJudgeTimes[i].RemoveAt(0);
                notesGenerator.doubleLongNotesJudgeTimes[i + 1].RemoveAt(0);
                notesGenerator.doubleLongNotesJudgeTimes[i + 1].RemoveAt(0);
                Lost();
            }
        }
    }



    /*
    ダブルロングノーツの30/bpm秒ごとのチェック
    自分のキー（laneKeys[i]）に加え、対になっている隣のレーンのキーでも受け付ける
    （左側ノーツはi+1側のキーも許容、右側ノーツはi-1側のキーも許容）
    */
    void ProcessDoubleLongNoteTick(int i)
    {
        bool isLeftSide = activeLaneDoubleLongJudges[i][0] < 0;
        int pairIndex = isLeftSide ? i + 1 : i - 1;
        bool pairIndexInRange = isLeftSide ? (i + 1 <= 3) : (i - 1 >= 0);

        bool primaryPressed = Input.GetKey(laneKeys[i]);
        bool altPressed = pairIndexInRange && Input.GetKey(laneKeys[pairIndex]);

        if (primaryPressed || altPressed)
        {
            LongNotesPressed();
            //経過時間分をlistに追加してから先頭削除
            double step = (30.0f / laneManager.bpm) * (isLeftSide ? -1 : 1);
            activeLaneDoubleLongJudges[i].Insert(1, activeLaneDoubleLongJudges[i][0] + step);
            activeLaneDoubleLongJudges[pairIndex].Insert(1, activeLaneDoubleLongJudges[pairIndex][0] - step);
            activeLaneDoubleLongJudges[i].RemoveAt(0);
            activeLaneDoubleLongJudges[pairIndex].RemoveAt(0);
        }
        else
        {
            Lost();
            activeLaneDoubleLongJudges[i].RemoveAt(0);
            activeLaneDoubleLongJudges[i].RemoveAt(0);
            activeLaneDoubleLongJudges[pairIndex].RemoveAt(0);
            activeLaneDoubleLongJudges[pairIndex].RemoveAt(0);
        }
    }



    // ダブルロングノーツが最後まで押しきられたか（[0]が[1]を追い越したか）を確認して完了していればリストを片付けてCriticalBreak(0)を発生
    void CheckDoubleLongNoteCompletion(int i)
    {
        if (Math.Abs(activeLaneDoubleLongJudges[i][1]) >= Math.Abs(activeLaneDoubleLongJudges[i][0])) return; //上記の処理で[0] >= [1]になったら終了

        bool isLeftSide = activeLaneDoubleLongJudges[i][0] < 0;
        int pairIndex = isLeftSide ? i + 1 : i - 1;

        activeLaneDoubleLongJudges[i].RemoveAt(0);
        activeLaneDoubleLongJudges[i].RemoveAt(0);
        activeLaneDoubleLongJudges[pairIndex].RemoveAt(0);
        activeLaneDoubleLongJudges[pairIndex].RemoveAt(0);
        CriticalBreak(0);
    }

    //||||||||||||||||||||||||||||||||||||||||
    // フィールド上のギミック（スキル玉・攻撃玉・ソフラン・炎）

    // 線上を流れるオブジェクト（スキル玉/攻撃/ソフラン）と、設置される炎の判定
    void UpdateFieldObjects()
    {
        for (int i = 0; i < 9; i++)
        {
            if (notesGenerator.lineObjectsJudgeTimes[i][0] <= t)
            {
                switch (notesGenerator.lineObjectsJudgeTimes[i][1])
                {
                    case 4: // スキルエネルギー
                        if (8 - (8 - i) * 2 == now)
                        {
                            SkillEnergyChange(4);
                        }
                        notesGenerator.lineObjectsJudgeTimes[i].RemoveAt(0);
                        notesGenerator.lineObjectsJudgeTimes[i].RemoveAt(0);
                        break;

                    case 5: // 攻撃（発射体）
                        if (8 - (8 - i) * 2 == now)
                        {
                            OnDamage();
                        }
                        notesGenerator.lineObjectsJudgeTimes[i].RemoveAt(0);
                        notesGenerator.lineObjectsJudgeTimes[i].RemoveAt(0);
                        break;

                    case 7: // ソフラン（曲速度変化）
                        nowPlayingSpeed = notesGenerator.lineObjectsJudgeTimes[i][2];
                        laneManager.soflan = nowPlayingSpeed;
                        notesGenerator.lineObjectsJudgeTimes[i].RemoveAt(0);
                        notesGenerator.lineObjectsJudgeTimes[i].RemoveAt(0);
                        notesGenerator.lineObjectsJudgeTimes[i].RemoveAt(0);
                        break;
                }
            }

            if (notesGenerator.placeObjectsJudgeTimes[i][0] <= t)
            {
                stripeCautionObj[i].SetActive(true);
                if (notesGenerator.placeObjectsJudgeTimes[i][1] <= t)
                {
                    flameObj[i].SetActive(true);
                    if (8 - (8 - i) * 2 == now)
                    {
                        OnDamage();
                    }
                    notesGenerator.placeObjectsJudgeTimes[i][1] += 0.5;
                    if (notesGenerator.placeObjectsJudgeTimes[i][1] > notesGenerator.placeObjectsJudgeTimes[i][2])
                    {
                        notesGenerator.placeObjectsJudgeTimes[i].RemoveAt(0);
                        notesGenerator.placeObjectsJudgeTimes[i].RemoveAt(0);
                        notesGenerator.placeObjectsJudgeTimes[i].RemoveAt(0);
                        stripeCautionObj[i].SetActive(false);
                        flameObj[i].SetActive(false);
                    }
                }
            }
        }
    }




    //||||||||||||||||||||||||||||||||||||||||
    // 見た目の更新（キービーム／自機移動アニメーション／HUD／スキル状態）

    // キーを離したときにキービームの見た目を消灯位置へ戻す
    void HandleKeyUpBeams()
    {
        if (Input.GetKeyUp(KeyCode.F))
        {
            keyBeamTransforms[1].localPosition = new Vector3(-1 + now, -0.002f, 2.5f);
        }
        if (Input.GetKeyUp(KeyCode.J))
        {
            keyBeamTransforms[2].localPosition = new Vector3(1 + now, -0.002f, 2.5f);
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            keyBeamTransforms[0].localPosition = new Vector3(-3 + now, -0.002f, 2.5f);
        }
        if (Input.GetKeyUp(KeyCode.K))
        {
            keyBeamTransforms[3].localPosition = new Vector3(3 + now, -0.002f, 2.5f);
        }
    }

    // 自機の上下ゆらぎ、目標位置(now)へのスライド移動、左右レーン表示の追従、傾いた姿勢を正面へ戻す回転補正
    void UpdateCharacterTransform()
    {
        y = Mathf.PingPong(Time.time * 0.2f, 0.4f); //上下にゆらゆら動かす
        float x = trans.localPosition.x;
        trans.localPosition = new Vector3(x, y + 1f, -2.5f); // 移動

        if (trans.localPosition.x < now)
        { // 想定位置とずれてたら
            x += 24f * Time.deltaTime; // 1秒で12レーン動く速さで
            trans.localPosition = new Vector3(x, y + 1f, -2.5f); // 移動
            leftLane.localPosition = new Vector3(x - 2, 0.001f, 7.5f);
            rightLane.localPosition = new Vector3(x + 2, 0.001f, 7.5f);
        }
        if (trans.localPosition.x > now)
        {
            x -= 24f * Time.deltaTime;
            trans.localPosition = new Vector3(x, y + 1f, -2.5f);
            leftLane.localPosition = new Vector3(x - 2, 0.001f, 7.5f);
            rightLane.localPosition = new Vector3(x + 2, 0.001f, 7.5f);
        }

        float zAngle = trans.eulerAngles.z;
        if (zAngle > 180f) zAngle -= 360f; // -180 ~ 180に正規化
        else if (zAngle < -180f) zAngle += 360f;
        if (zAngle < -0.05f)
        { // 角度がほぼ真上向いてなかったら
            trans.Rotate(0f, 0f, 60f * Time.deltaTime); // 戻す
        }
        if (zAngle > 0.05f)
        {
            trans.Rotate(0f, 0f, -60f * Time.deltaTime);
        }
    }

    /* スコア表示・コンボ表示・正答率表示のテキスト更新と、
     判定が入った瞬間の拡大→縮小アニメーションを行う
    */
    void UpdateHud()
    {
        totalText.text = $"{notesGenerator.selectedMusic}\n\nCritical Break : {criticalBreakCount}\nBreak : {breakCount}\nWeak : {weakCount}\nLost : {lostCount}";
        comboText.text = $"Combo\n{comboCount}";
        bossHpText.text = $"Accuracy\n{bossHp:F2}%";

        // combotext 変化する瞬間大きさ変わるように
        if (comboTextTrans.localScale.x < 1) comboTextTrans.localScale = new Vector3(comboTextTrans.localScale.x + 0.025f, comboTextTrans.localScale.y + 0.025f, 1);
        if (comboTextTrans.localScale.x > 1) comboTextTrans.localScale = new Vector3(1, 1, 1);

        // judgetext 変化する瞬間大きさ変わるように
        if (judgeImageTrans.localScale.x < 1) judgeImageTrans.localScale = new Vector3(judgeImageTrans.localScale.x + 0.025f, judgeImageTrans.localScale.y + 0.025f, 1);
        if (judgeImageTrans.localScale.x > 1) judgeImageTrans.localScale = new Vector3(1, 1, 1);
    }



    // スキル発動中のゲージ表示更新と、エネルギー切れ時の終了処理
    void UpdateSkillState()
    {
        // スキル実装当初と仕様変更 回数制へ
        if (skillActive == true)
        {
            //skillEnergy -= 0.02;
            skillEnergyGaugeImage.fillAmount = (float)skillEnergy / 100f;
            if (skillEnergy <= 0)
            {
                skillActive = false;
                skillEnergy = 0;
                skillActiveEffect.SetActive(false);
            }
        }
    }

    //全ノーツの判定が終わったら、一度だけリザルトシーンへの遷移を予約
    void CheckSongEnd()
    {
        if (criticalBreakCount + breakCount + weakCount + lostCount == totalNotesCount && end == false)
        {
            end = true;
            moveToResult.Invoke("MoveToResultScene", 2f);
        }
    }

    
}