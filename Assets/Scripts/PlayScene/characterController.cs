using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;

public class characterController : MonoBehaviour
{
    public int now; // キャラ現在位置

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

    List<double>[] activeLaneSingleJudges;
    List<double>[] activeLaneDoubleJudges;
    List<double>[] activeLaneSingleLongJudges;
    List<double>[] activeLaneDoubleLongJudges;

    List<double> emptyArray;

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

    void Start()
    {
        if (DontDestroySingleObject.Instance != null)  moveToResult = DontDestroySingleObject.Instance.GetComponent<moveToResult>();
    
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

        // 判定用の配列初期化
        activeLaneSingleJudges = new List<double>[4];
        activeLaneDoubleJudges = new List<double>[4];
        activeLaneSingleLongJudges = new List<double>[4];
        activeLaneDoubleLongJudges = new List<double>[4];
        for(int i = 0; i < 4; i++)
        {
            activeLaneSingleJudges[i] = new List<double>();
            activeLaneDoubleJudges[i] = new List<double>();
            activeLaneSingleLongJudges[i] = new List<double>();
            activeLaneDoubleLongJudges[i] = new List<double>();

            activeLaneSingleJudges[i] = notesGenerator.singleNotesJudgeTimes[now /2 +2+i];
            activeLaneDoubleJudges[i] = notesGenerator.doubleNotesJudgeTimes[now /2 +2+i];
            activeLaneSingleLongJudges[i] = notesGenerator.singleLongNotesJudgeTimes[now /2 +2+i];
            activeLaneDoubleLongJudges[i] = notesGenerator.doubleLongNotesJudgeTimes[now /2 +2+i];
        }

        if (difficulty == 0)
        {
            /* 下位レーンでも0, 3レーン使う
            // 下位難易度だと0, 3レーン使わないので
            for (int i = 0; i < 2; i++)
            {
                activeLaneSingleJudges[i*3] = emptyArray;
                activeLaneDoubleJudges[i*3] = emptyArray;
                activeLaneSingleLongJudges[i*3] = emptyArray;
                activeLaneDoubleLongJudges[i*3] = emptyArray;
            }
            */
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

    void BossHpChange()
    {
        bossHp = (double)((criticalBreakCount + breakCount*0.9 + weakCount*0.4) /totalNotesCount *100);
        bossHpGaugeImage.fillAmount = 1 - (float)bossHp/100f;
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
        comboTextTrans.localScale = new Vector3 (0.5f, 0.5f, 1);
    }

    void CriticalBreak(double gap)
    {
        //Debug.Log("CriticalBreak");
        judgeImage.sprite = judgeImageSprites[0];
        judgeImageTrans.localScale = new Vector3 (0.5f, 0.5f, 1);
        comboTextTrans.localScale = new Vector3 (0.5f, 0.5f, 1);
        criticalBreakCount++;
        comboCount++;
        techScore += 1000000 / totalNotesCount;
        if (maxComboCount <= comboCount ) maxComboCount = comboCount;
        BossHpChange();
        SkillEnergyChange(0.4);
        // gap 0の場合はロングノーツなのでリザルトのグラフにカウントしない
        if (gap != 0 && Math.Abs(gap) <= criticalBreakJudgeTime * nowPlayingSpeed) gapSaveList.Add(gap);
    }

    void Break(double gap)
    {
        //Debug.Log("Break "+gap);
        judgeImage.sprite = judgeImageSprites[1];
        judgeImageTrans.localScale = new Vector3 (0.5f, 0.5f, 1);
        comboTextTrans.localScale = new Vector3 (0.5f, 0.5f, 1);
        breakCount++;
        comboCount++;
        techScore += 1000000 / totalNotesCount * 0.9;
        if (maxComboCount <= comboCount ) maxComboCount = comboCount;
        BossHpChange();
        SkillEnergyChange(0.36);
        // gap 0の場合はリカバースキルなのでカウントせず
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
        judgeImageTrans.localScale = new Vector3 (0.5f, 0.5f, 1);
        comboTextTrans.localScale = new Vector3 (0.5f, 0.5f, 1);
        weakCount++;
        comboCount++;
        techScore += 1000000 / totalNotesCount * 0.4;
        if (maxComboCount <= comboCount ) maxComboCount = comboCount;
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
        judgeImageTrans.localScale = new Vector3 (0.5f, 0.5f, 1);
        lostCount++;
        if (skillActive == true && skillType == 1)
        {
            comboCount++;
            if (maxComboCount <= comboCount ) maxComboCount = comboCount;
            skillEnergy -= 25;
        }
        else 
        {
            comboCount = 0;
        }
    }

    void OnDamage()
    {
        Debug.Log("damage");
        damageTaken++;
    }

    void ActiveLaneChange()
    {
        if (now /2 +3 < 0) // fレーン
        {
            activeLaneSingleJudges[1] = emptyArray;
            activeLaneDoubleJudges[1] = emptyArray;
            activeLaneSingleLongJudges[1] = emptyArray;
            activeLaneDoubleLongJudges[1] = emptyArray;
        }
        else
        {
            activeLaneSingleJudges[1] = notesGenerator.singleNotesJudgeTimes[now /2 +3]; // 参照を渡すことでもともとのlistを変更する
            activeLaneDoubleJudges[1] = notesGenerator.doubleNotesJudgeTimes[now /2 +3];
            activeLaneSingleLongJudges[1] = notesGenerator.singleLongNotesJudgeTimes[now /2 +3];
            activeLaneDoubleLongJudges[1] = notesGenerator.doubleLongNotesJudgeTimes[now /2 +3];
        }

        if (now /2 +4 > 7) // jレーン
        {
            activeLaneSingleJudges[2] = emptyArray;
            activeLaneDoubleJudges[2] = emptyArray;
            activeLaneSingleLongJudges[2] = emptyArray;
            activeLaneDoubleLongJudges[2] = emptyArray;
        }
        else
        {
            activeLaneSingleJudges[2] = notesGenerator.singleNotesJudgeTimes[now /2 +4];
            activeLaneDoubleJudges[2] = notesGenerator.doubleNotesJudgeTimes[now /2 +4];
            activeLaneSingleLongJudges[2] = notesGenerator.singleLongNotesJudgeTimes[now /2 +4];
            activeLaneDoubleLongJudges[2] = notesGenerator.doubleLongNotesJudgeTimes[now /2 +4];
        }

        /*
        if (difficulty == 1)
        {
        */
            if (now /2 +2 < 0) // dレーン
            {
                activeLaneSingleJudges[0] = emptyArray;
                activeLaneDoubleJudges[0] = emptyArray;
                activeLaneSingleLongJudges[0] = emptyArray;
                activeLaneDoubleLongJudges[0] = emptyArray;
            }
            else
            {
                activeLaneSingleJudges[0] = notesGenerator.singleNotesJudgeTimes[now /2 +2];
                activeLaneDoubleJudges[0] = notesGenerator.doubleNotesJudgeTimes[now /2 +2];
                activeLaneSingleLongJudges[0] = notesGenerator.singleLongNotesJudgeTimes[now /2 +2];
                activeLaneDoubleLongJudges[0] = notesGenerator.doubleLongNotesJudgeTimes[now /2 +2];
            }

            if (now /2 +5 > 7) // kレーン
            {
                activeLaneSingleJudges[3] = emptyArray;
                activeLaneDoubleJudges[3] = emptyArray;
                activeLaneSingleLongJudges[3] = emptyArray;
                activeLaneDoubleLongJudges[3] = emptyArray;
            }
            else
            {
                activeLaneSingleJudges[3] = notesGenerator.singleNotesJudgeTimes[now /2 +5];
                activeLaneDoubleJudges[3] = notesGenerator.doubleNotesJudgeTimes[now /2 +5];
                activeLaneSingleLongJudges[3] = notesGenerator.singleLongNotesJudgeTimes[now /2 +5];
                activeLaneDoubleLongJudges[3] = notesGenerator.doubleLongNotesJudgeTimes[now /2 +5];
            }
        /*
        }
        */
    }

    // nowの偏移 lane0(左端、右二レーン有効) = -8
    // lane4 = 0
    // lane8(右端、左二レーン有効) = 8

    // fレーンジャッジの指定index
    // nowが-6のとき、0
    // nowが<-6のとき、emptyArray
    // nowが8のとき、7
    // 3 +now /2 (ifでマイナスのときはemptyArrayにする)

    // dレーンジャッジの指定index 2 +now /2 (ifでマイナスのときはemptyArrayにする)

    // jレーンジャッジの指定index
    // now 6 -> 7
    // now >6 -> emptyArray
    // now -8 -> 0
    // 4 +now /2 (ifで7<のときはemptyArrayにする)
    void KeyPressJudge(int laneNumber)
    {
        if (activeLaneSingleJudges[laneNumber][0] <= Math.Abs(activeLaneDoubleJudges[laneNumber][0]))
        {
            if (Math.Abs(activeLaneSingleJudges[laneNumber][0]-t) <= criticalBreakJudgeTime * nowPlayingSpeed)
            {
                CriticalBreak(activeLaneSingleJudges[laneNumber][0]-t);
                notesGenerator.notesObject[now /2 +2 +laneNumber][notesDestroyIndex[now /2 +2 +laneNumber]].GetComponent<noteMover>().Destroy();
                activeLaneSingleJudges[laneNumber].RemoveAt(0);
                notesDestroyIndex[now /2 +2 +laneNumber]++;
            }
            else if (Math.Abs(activeLaneSingleJudges[laneNumber][0]-t) <= breakJudgeTime * nowPlayingSpeed)
            {
                Break(activeLaneSingleJudges[laneNumber][0]-t); // どれくらいずれているかを引数に入れる
                notesGenerator.notesObject[now /2 +2 +laneNumber][notesDestroyIndex[now /2 +2 +laneNumber]].GetComponent<noteMover>().Destroy();
                activeLaneSingleJudges[laneNumber].RemoveAt(0);
                notesDestroyIndex[now /2 +2 +laneNumber]++;
            }
            else if (Math.Abs(activeLaneSingleJudges[laneNumber][0]-t) <= weakJudgeTime * nowPlayingSpeed)
            {
                Weak(activeLaneSingleJudges[laneNumber][0]-t);
                notesGenerator.notesObject[now /2 +2 +laneNumber][notesDestroyIndex[now /2 +2 +laneNumber]].GetComponent<noteMover>().Destroy();
                activeLaneSingleJudges[laneNumber].RemoveAt(0);
                notesDestroyIndex[now /2 +2 +laneNumber]++;
            }
        }
        else // ダブルタップは時間がマイナスかプラスかで左右どっちか判定
        {
            if (Math.Abs(Math.Abs(activeLaneDoubleJudges[laneNumber][0])-t) <= criticalBreakJudgeTime * nowPlayingSpeed)
            {
                CriticalBreak(activeLaneSingleJudges[laneNumber][0]-t);
                
                if (activeLaneDoubleJudges[laneNumber][0] < 0) // 叩いたのが左側
                {
                    notesGenerator.doubleNotesJudgeTimes[now /2 +3 +laneNumber].RemoveAt(0);
                    notesGenerator.notesObject[now /2 +2 +laneNumber +8][notesDestroyIndex[now /2 +2 +laneNumber +8]].GetComponent<noteMover>().Destroy();
                    notesDestroyIndex[now /2 +2 +laneNumber +8]++;
                }
                else // 叩いたのが右側
                {
                    notesGenerator.doubleNotesJudgeTimes[now /2 +1 +laneNumber].RemoveAt(0);
                    notesGenerator.notesObject[now /2 +1 +laneNumber +8][notesDestroyIndex[now /2 +1 +laneNumber +8]].GetComponent<noteMover>().Destroy();
                    notesDestroyIndex[now /2 +1 +laneNumber +8]++;
                }

                activeLaneDoubleJudges[laneNumber].RemoveAt(0);
            }
            else if (Math.Abs(Math.Abs(activeLaneDoubleJudges[laneNumber][0])-t) <= breakJudgeTime * nowPlayingSpeed)
            {
                Break(Math.Abs(activeLaneDoubleJudges[laneNumber][0])-t);

                if (activeLaneDoubleJudges[laneNumber][0] < 0) 
                {
                    notesGenerator.doubleNotesJudgeTimes[now /2 +3 +laneNumber].RemoveAt(0);
                    notesGenerator.notesObject[now /2 +2 +laneNumber +8][notesDestroyIndex[now /2 +2 +laneNumber +8]].GetComponent<noteMover>().Destroy();
                    notesDestroyIndex[now /2 +2 +laneNumber +8]++;
                }
                else 
                {
                    notesGenerator.doubleNotesJudgeTimes[now /2 +1 +laneNumber].RemoveAt(0);
                    notesGenerator.notesObject[now /2 +1 +laneNumber +8][notesDestroyIndex[now /2 +1 +laneNumber +8]].GetComponent<noteMover>().Destroy();
                    notesDestroyIndex[now /2 +1 +laneNumber +8]++;
                }

                activeLaneDoubleJudges[laneNumber].RemoveAt(0);
            }
            else if (Math.Abs(Math.Abs(activeLaneDoubleJudges[laneNumber][0])-t) <= weakJudgeTime * nowPlayingSpeed)
            {
                Weak(Math.Abs(activeLaneDoubleJudges[laneNumber][0])-t);

                if (activeLaneDoubleJudges[laneNumber][0] < 0) 
                {
                    notesGenerator.doubleNotesJudgeTimes[now /2 +3 +laneNumber].RemoveAt(0);
                    notesGenerator.notesObject[now /2 +2 +laneNumber +8][notesDestroyIndex[now /2 +2 +laneNumber +8]].GetComponent<noteMover>().Destroy();
                    notesDestroyIndex[now /2 +2 +laneNumber +8]++;
                }
                else 
                {
                    notesGenerator.doubleNotesJudgeTimes[now /2 +1 +laneNumber].RemoveAt(0);
                    notesGenerator.notesObject[now /2 +1 +laneNumber +8][notesDestroyIndex[now /2 +1 +laneNumber +8]].GetComponent<noteMover>().Destroy();
                    notesDestroyIndex[now /2 +1 +laneNumber +8]++;
                }

                activeLaneDoubleJudges[laneNumber].RemoveAt(0);
            }
        }
    }

    void Update()
    {
        t = laneManager.t;

        /*
        switch (difficulty)
        {
            case 0:
                if (Input.GetKeyDown(KeyCode.D) && now > -8 && Mathf.Abs(trans.localPosition.x - now) < 1f)
                {
                    now -= 2;
                    trans.Rotate (0f, 0f, 15f);

                    ActiveLaneChange();
                }
                if (Input.GetKeyDown(KeyCode.K) && now < 8 && Mathf.Abs(trans.localPosition.x - now) < 1f) 
                {
                    now += 2;
                    trans.Rotate (0f, 0f, -15f);

                    ActiveLaneChange();
                }

                break;
            case 1:
            */
                if ( (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.R) )&& now > -8 && Mathf.Abs(trans.localPosition.x - now) < 1f) 
                {
                    now -= 2;
                    trans.Rotate (0f, 0f, 15f);

                    ActiveLaneChange();
                }
                if ( (Input.GetKeyDown(KeyCode.U) || Input.GetKeyDown(KeyCode.I) )&& now < 8 && Mathf.Abs(trans.localPosition.x - now) < 1f) 
                {
                    now += 2;
                    trans.Rotate (0f, 0f, -15f);

                    ActiveLaneChange();
                }

                //break;
        //}

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
                    notesGenerator.notesObject[i+8][notesDestroyIndex[i+8]].GetComponent<noteMover>().Destroy();
                    notesDestroyIndex[i+8]++;
                }
                else
                {
                    notesGenerator.doubleNotesJudgeTimes[i].RemoveAt(0);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            KeyPressJudge(1);
            keyBeamTransforms[1].localPosition = new Vector3 (-1 +now, 0.002f, 2.5f);
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            KeyPressJudge(2);
            keyBeamTransforms[2].localPosition = new Vector3 (1 +now, 0.002f, 2.5f);
        }
        /*
        if (difficulty == 1)
        {
        */
            if (Input.GetKeyDown(KeyCode.D))
            {
                KeyPressJudge(0);
                keyBeamTransforms[0].localPosition = new Vector3 (-3 +now, 0.002f, 2.5f);
            }
            if (Input.GetKeyDown(KeyCode.K))
            {
                KeyPressJudge(3);
                keyBeamTransforms[3].localPosition = new Vector3 (3 +now, 0.002f, 2.5f);
            }
        //}

        // シングルロング
        for (int i = 0; i < 4; i++)
        {
            if (activeLaneSingleLongJudges[i][0] <= t)
            { //[0]で開始、[1]まで
                if (t - activeLaneSingleLongJudges[i][0] > (30.0f / laneManager.bpm)) 
                { //bpmの二倍でカウント 時間経過したら
                    switch (i)
                    {
                        case 0:
                            if (Input.GetKey(KeyCode.D)) 
                            {
                                LongNotesPressed();
                                //経過時間分をlistに追加してから先頭削除
                                activeLaneSingleLongJudges[i].Insert(1, activeLaneSingleLongJudges[i][0] +(30.0f / laneManager.bpm));
                                activeLaneSingleLongJudges[i].RemoveAt(0);
                            }
                            else 
                            {
                                Lost();
                                activeLaneSingleLongJudges[i].RemoveAt(0);
                                activeLaneSingleLongJudges[i].RemoveAt(0);
                            }
                            break;

                        case 1:
                            if (Input.GetKey(KeyCode.F)) 
                            {
                                LongNotesPressed();
                                //経過時間分をlistに追加してから先頭削除
                                activeLaneSingleLongJudges[i].Insert(1, activeLaneSingleLongJudges[i][0] +(30.0f / laneManager.bpm));
                                activeLaneSingleLongJudges[i].RemoveAt(0);
                            }
                            else 
                            {
                                Lost();
                                activeLaneSingleLongJudges[i].RemoveAt(0);
                                activeLaneSingleLongJudges[i].RemoveAt(0);
                            }
                            break;

                        case 2:
                            if (Input.GetKey(KeyCode.J)) 
                            {
                                LongNotesPressed();
                                //経過時間分をlistに追加してから先頭削除
                                activeLaneSingleLongJudges[i].Insert(1, activeLaneSingleLongJudges[i][0] +(30.0f / laneManager.bpm));
                                activeLaneSingleLongJudges[i].RemoveAt(0);
                            }
                            else 
                            {
                                Lost();
                                activeLaneSingleLongJudges[i].RemoveAt(0);
                                activeLaneSingleLongJudges[i].RemoveAt(0);
                            }
                            break;

                        case 3:
                            if (Input.GetKey(KeyCode.K)) 
                            {
                                LongNotesPressed();
                                //経過時間分をlistに追加してから先頭削除
                                activeLaneSingleLongJudges[i].Insert(1, activeLaneSingleLongJudges[i][0] +(30.0f / laneManager.bpm));
                                activeLaneSingleLongJudges[i].RemoveAt(0);
                            }
                            else 
                            {
                                Lost();
                                activeLaneSingleLongJudges[i].RemoveAt(0);
                                activeLaneSingleLongJudges[i].RemoveAt(0);
                            }
                            break;
                    }
                    if (activeLaneSingleLongJudges[i][1] < activeLaneSingleLongJudges[i][0]) 
                    { //上記の処理で[0] >= [1]になったら終了
                        activeLaneSingleLongJudges[i].RemoveAt(0);
                        activeLaneSingleLongJudges[i].RemoveAt(0);
                        CriticalBreak(0);
                    }
                }
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

        // ダブルロング
        for (int i = 0; i< 4; i++)
        {
            if (Math.Abs(activeLaneDoubleLongJudges[i][0]) <= t)
            { //[0]で開始、[1]まで
                if (t - Math.Abs(activeLaneDoubleLongJudges[i][0]) > (30.0f / laneManager.bpm)) 
                {
                    // ノーツ左側
                    if (activeLaneDoubleLongJudges[i][0] < 0)
                    {
                        switch (i)
                        {
                            case 0:
                                if (Input.GetKey(KeyCode.D)) 
                                {
                                    LongNotesPressed();
                                    //経過時間分をlistに追加してから先頭削除
                                    activeLaneDoubleLongJudges[i].Insert(1, activeLaneDoubleLongJudges[i][0] -(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i+1].Insert(1, activeLaneDoubleLongJudges[i+1][0] +(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i+1].RemoveAt(0);
                                }
                                else if (Input.GetKey(KeyCode.F))
                                {
                                    LongNotesPressed();
                                    activeLaneDoubleLongJudges[i].Insert(1, activeLaneDoubleLongJudges[i][0] -(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i+1].Insert(1, activeLaneDoubleLongJudges[i+1][0] +(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i+1].RemoveAt(0);
                                }
                                else 
                                {
                                    Lost();
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i+1].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i+1].RemoveAt(0);
                                }
                                break;

                            case 1:
                                if (Input.GetKey(KeyCode.F)) 
                                {
                                    LongNotesPressed();
                                    //経過時間分をlistに追加してから先頭削除
                                    activeLaneDoubleLongJudges[i].Insert(1, activeLaneDoubleLongJudges[i][0] -(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i+1].Insert(1, activeLaneDoubleLongJudges[i+1][0] +(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i+1].RemoveAt(0);
                                }
                                else if (Input.GetKey(KeyCode.J))
                                {
                                    LongNotesPressed();
                                    activeLaneDoubleLongJudges[i].Insert(1, activeLaneDoubleLongJudges[i][0] -(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i+1].Insert(1, activeLaneDoubleLongJudges[i+1][0] +(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i+1].RemoveAt(0);
                                }
                                else 
                                {
                                    Lost();
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i+1].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i+1].RemoveAt(0);
                                }
                                break;

                            case 2:
                                if (Input.GetKey(KeyCode.J)) 
                                {
                                    LongNotesPressed();
                                    //経過時間分をlistに追加してから先頭削除
                                    activeLaneDoubleLongJudges[i].Insert(1, activeLaneDoubleLongJudges[i][0] -(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i+1].Insert(1, activeLaneDoubleLongJudges[i+1][0] +(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i+1].RemoveAt(0);
                                }
                                else if (Input.GetKey(KeyCode.K))
                                {
                                    LongNotesPressed();
                                    activeLaneDoubleLongJudges[i].Insert(1, activeLaneDoubleLongJudges[i][0] -(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i+1].Insert(1, activeLaneDoubleLongJudges[i+1][0] +(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i+1].RemoveAt(0);
                                }
                                else 
                                {
                                    Lost();
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i+1].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i+1].RemoveAt(0);
                                }
                                break;

                            case 3:
                                if (Input.GetKey(KeyCode.K)) 
                                {
                                    LongNotesPressed();
                                    //経過時間分をlistに追加してから先頭削除
                                    activeLaneDoubleLongJudges[i].Insert(1, activeLaneDoubleLongJudges[i][0] -(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i+1].Insert(1, activeLaneDoubleLongJudges[i+1][0] +(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i+1].RemoveAt(0);
                                }
                                else 
                                {
                                    Lost();
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i+1].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i+1].RemoveAt(0);
                                }
                                break;
                        }
                    }
                    // ノーツ右側
                    else 
                    {
                        switch (i)
                        {
                            case 0:
                                if (Input.GetKey(KeyCode.D)) 
                                {
                                    LongNotesPressed();
                                    //経過時間分をlistに追加してから先頭削除
                                    activeLaneDoubleLongJudges[i].Insert(1, activeLaneDoubleLongJudges[i][0] +(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i-1].Insert(1, activeLaneDoubleLongJudges[i-1][0] -(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i-1].RemoveAt(0);
                                }
                                else 
                                {
                                    Lost();
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i-1].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i-1].RemoveAt(0);
                                }
                                break;

                            case 1:
                                if (Input.GetKey(KeyCode.F)) 
                                {
                                    LongNotesPressed();
                                    //経過時間分をlistに追加してから先頭削除
                                    activeLaneDoubleLongJudges[i].Insert(1, activeLaneDoubleLongJudges[i][0] +(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i-1].Insert(1, activeLaneDoubleLongJudges[i-1][0] -(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i-1].RemoveAt(0);
                                }
                                else if (Input.GetKey(KeyCode.D)) 
                                {
                                    LongNotesPressed();
                                    //経過時間分をlistに追加してから先頭削除
                                    activeLaneDoubleLongJudges[i].Insert(1, activeLaneDoubleLongJudges[i][0] +(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i-1].Insert(1, activeLaneDoubleLongJudges[i-1][0] -(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i-1].RemoveAt(0);
                                }
                                else 
                                {
                                    Lost();
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i-1].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i-1].RemoveAt(0);
                                }
                                break;

                            case 2:
                                if (Input.GetKey(KeyCode.J)) 
                                {
                                    LongNotesPressed();
                                    //経過時間分をlistに追加してから先頭削除
                                    activeLaneDoubleLongJudges[i].Insert(1, activeLaneDoubleLongJudges[i][0] +(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i-1].Insert(1, activeLaneDoubleLongJudges[i-1][0] -(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i-1].RemoveAt(0);
                                }
                                else if (Input.GetKey(KeyCode.F)) 
                                {
                                    LongNotesPressed();
                                    //経過時間分をlistに追加してから先頭削除
                                    activeLaneDoubleLongJudges[i].Insert(1, activeLaneDoubleLongJudges[i][0] +(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i-1].Insert(1, activeLaneDoubleLongJudges[i-1][0] -(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i-1].RemoveAt(0);
                                }
                                else 
                                {
                                    Lost();
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i-1].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i-1].RemoveAt(0);
                                }
                                break;

                            case 3:
                                if (Input.GetKey(KeyCode.K)) 
                                {
                                    LongNotesPressed();
                                    //経過時間分をlistに追加してから先頭削除
                                    activeLaneDoubleLongJudges[i].Insert(1, activeLaneDoubleLongJudges[i][0] +(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i-1].Insert(1, activeLaneDoubleLongJudges[i-1][0] -(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i-1].RemoveAt(0);
                                }
                                else if (Input.GetKey(KeyCode.J)) 
                                {
                                    LongNotesPressed();
                                    //経過時間分をlistに追加してから先頭削除
                                    activeLaneDoubleLongJudges[i].Insert(1, activeLaneDoubleLongJudges[i][0] +(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i-1].Insert(1, activeLaneDoubleLongJudges[i-1][0] -(30.0f / laneManager.bpm));
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i-1].RemoveAt(0);
                                }
                                else 
                                {
                                    Lost();
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i-1].RemoveAt(0);
                                    activeLaneDoubleLongJudges[i-1].RemoveAt(0);
                                }
                                break;
                        }
                    }
                }
                if (Math.Abs(activeLaneDoubleLongJudges[i][1]) < Math.Abs(activeLaneDoubleLongJudges[i][0])) 
                { //上記の処理で[0] >= [1]になったら終了
                    if (activeLaneDoubleLongJudges[i][0] < 0)
                    {
                        activeLaneDoubleLongJudges[i].RemoveAt(0);
                        activeLaneDoubleLongJudges[i].RemoveAt(0);
                        activeLaneDoubleLongJudges[i+1].RemoveAt(0);
                        activeLaneDoubleLongJudges[i+1].RemoveAt(0);
                    }
                    else 
                    {
                        activeLaneDoubleLongJudges[i].RemoveAt(0);
                        activeLaneDoubleLongJudges[i].RemoveAt(0);
                        activeLaneDoubleLongJudges[i-1].RemoveAt(0);
                        activeLaneDoubleLongJudges[i-1].RemoveAt(0);
                    }
                    CriticalBreak(0);
                }
            }
        }
        // 上記の処理終了後に全レーンで押されてないノーツあるか確認
        for (int i = 0; i < 8; i++)
        {
            if (Math.Abs(notesGenerator.doubleLongNotesJudgeTimes[i][0]) <= t - (30.0f / laneManager.bpm) - weakJudgeTime)
            {
                notesGenerator.doubleLongNotesJudgeTimes[i].RemoveAt(0);
                notesGenerator.doubleLongNotesJudgeTimes[i].RemoveAt(0);
                notesGenerator.doubleLongNotesJudgeTimes[i+1].RemoveAt(0);
                notesGenerator.doubleLongNotesJudgeTimes[i+1].RemoveAt(0);
                Lost();
            }
        }

        //線上のオブジェクトの判定
        for (int i = 0; i < 9; i++)
        {
            if (notesGenerator.lineObjectsJudgeTimes[i][0] <= t)
            {
                switch (notesGenerator.lineObjectsJudgeTimes[i][1])
                {
                    case 4:
                        if (8-(8-i)*2 == now)
                        {
                            SkillEnergyChange(4);
                        }
                        notesGenerator.lineObjectsJudgeTimes[i].RemoveAt(0);
                        notesGenerator.lineObjectsJudgeTimes[i].RemoveAt(0);

                        break;

                    case 5:
                        if (8-(8-i)*2 == now)
                        {
                            OnDamage();
                        }
                        notesGenerator.lineObjectsJudgeTimes[i].RemoveAt(0);
                        notesGenerator.lineObjectsJudgeTimes[i].RemoveAt(0);

                        break;

                    case 7:
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
                    if (8-(8-i)*2 == now)
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

        if (Input.GetKeyUp(KeyCode.F))
        {
            keyBeamTransforms[1].localPosition = new Vector3 (-1 +now, -0.002f, 2.5f);
        }
        if (Input.GetKeyUp(KeyCode.J))
        {
            keyBeamTransforms[2].localPosition = new Vector3 (1 +now, -0.002f, 2.5f);
        }
        /*
        if (difficulty == 1)
        {
        */
            if (Input.GetKeyUp(KeyCode.D))
            {
                keyBeamTransforms[0].localPosition = new Vector3 (-3 +now, -0.002f, 2.5f);
            }
            if (Input.GetKeyUp(KeyCode.K))
            {
                keyBeamTransforms[3].localPosition = new Vector3 (3 +now, -0.002f, 2.5f);
            }
        //}


        y = Mathf.PingPong(Time.time * 0.2f, 0.4f); //上下にゆらゆら動かす
        float x = trans.localPosition.x;
        trans.localPosition = new Vector3 (x, y+1f, -2.5f); // 移動

        if (trans.localPosition.x < now) 
        { // 想定位置とずれてたら
            x += 24f * Time.deltaTime; // 1秒で12レーン動く速さで
            trans.localPosition = new Vector3 (x, y+1f, -2.5f); // 移動
            leftLane.localPosition = new Vector3 (x-2, 0.001f, 7.5f);
            rightLane.localPosition = new Vector3 (x+2, 0.001f, 7.5f);
        }
        if (trans.localPosition.x > now) 
        {
            x -= 24f * Time.deltaTime;
            trans.localPosition = new Vector3 (x, y+1f, -2.5f);
            leftLane.localPosition = new Vector3 (x-2, 0.001f, 7.5f);
            rightLane.localPosition = new Vector3 (x+2, 0.001f, 7.5f);
        }

        float zAngle = trans.eulerAngles.z;
        if (zAngle > 180f) zAngle -= 360f; // -180 ~ 180に正規化
        else if (zAngle < -180f) zAngle += 360f;
        if (zAngle < -0.05f) 
        { // 角度がほぼ真上向いてなかったら
            trans.Rotate (0f, 0f, 60f * Time.deltaTime); // 戻す
        }
        if (zAngle > 0.05f) 
        {
            trans.Rotate (0f, 0f, -60f * Time.deltaTime);
        }

        totalText.text = $"{notesGenerator.selectedMusic}\n\nCritical Break : {criticalBreakCount}\nBreak : {breakCount}\nWeak : {weakCount}\nLost : {lostCount}";
        comboText.text = $"Combo\n{comboCount}";
        bossHpText.text = $"Accuracy\n{bossHp:F2}%";

        // combotext 変化する瞬間大きさ変わるように
        if (comboTextTrans.localScale.x < 1) comboTextTrans.localScale = new Vector3 (comboTextTrans.localScale.x + 0.025f, comboTextTrans.localScale.y + 0.025f, 1);
        if (comboTextTrans.localScale.x > 1) comboTextTrans.localScale = new Vector3 (1, 1, 1);

        // judgetext 変化する瞬間大きさ変わるように
        if (judgeImageTrans.localScale.x < 1) judgeImageTrans.localScale = new Vector3 (judgeImageTrans.localScale.x + 0.025f, judgeImageTrans.localScale.y + 0.025f, 1);
        if (judgeImageTrans.localScale.x > 1) judgeImageTrans.localScale = new Vector3 (1, 1, 1);

        // skillActive 中
        // スキル実装当初と仕様変更 回数制に
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

        if (criticalBreakCount + breakCount + weakCount + lostCount == totalNotesCount && end == false)
        {
            end = true;
            moveToResult.Invoke("MoveToResultScene", 2f);
        }
    }
}
