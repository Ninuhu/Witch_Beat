using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using TMPro;

public class showScores : MonoBehaviour
{
    //[SerializeField] GameObject score, total, count, gap, back;
    moveToResult moveToResult;
    [SerializeField] TextMeshProUGUI score, rank, highScore, combo, damage, accuracy, cBreakCount, breakCount, weakCount, lostCount, average;
    [SerializeField] GameObject[] blocks = new GameObject[4]; 
    RectTransform[] graphBars = new RectTransform[10];

    [SerializeField] Image jacket;
    [SerializeField] Sprite[] jacketSprites = new Sprite[4]; // index 0はダミー、1:mopemope 2:shiningstar 3:song3
    [SerializeField] TextMeshProUGUI nameDifficultyText; // "name-diffculty" オブジェクト用
    void Start()
    {
        for (int i = 0; i < 10; i++)
        {
            graphBars[i] = GameObject.Find("bar"+i).GetComponent<RectTransform>();
        }
        moveToResult = GameObject.Find("settings").GetComponent<moveToResult>();
        for (int i = 0; i < 4; i++)
        {
            blocks[i].SetActive(false);
        }
        /* 時間あったら画面外から出てくる演出
        score.transform.localPosition = new Vector3 (1400f, 245f, 0f);
        total.transform.localPosition = new Vector3 (1400f, 0f, 0f);
        count.transform.localPosition = new Vector3 (1400f, -185f, 0f);
        gap.transform.localPosition = new Vector3 (1250f, -370f, 0f);
        back.transform.localPosition = new Vector3 (1750f, -400f, 0f);
        */

        // ID -> 表示名 の対応表（0番目はダミー、未使用）
        string[] musicDisplayNames = { "", "もぺもぺ", "シャイニングスター", "そんぐ３" };
        string[] difficultyDisplayNames = { "Normal", "Hard" };
        
        nameDifficultyText.text = $"{musicDisplayNames[moveToResult.musicId]} - {difficultyDisplayNames[moveToResult.difficulty]}";
        
        jacket.sprite = jacketSprites[moveToResult.musicId];
        score.text = moveToResult.techScore.ToString();
        /*
        rank 
        1,000,000 MAX
        ~ 990,000 SSS
        ~ 980,000 SS
        ~ 970,000 S
        ~ 950,000 AAA
        ~ 925,000 AA
        ~ 900,000 A
        ~ 800,000 B
        ~ 700,000 C
        700,000 ~ F
        */
        if (moveToResult.techScore >= 1000000) rank.text = "MAX";
        else if (moveToResult.techScore >= 990000) rank.text = "SSS";
        else if (moveToResult.techScore >= 980000) rank.text = "SS";
        else if (moveToResult.techScore >= 970000) rank.text = "S";
        else if (moveToResult.techScore >= 950000) rank.text = "AAA";
        else if (moveToResult.techScore >= 925000) rank.text = "AA";
        else if (moveToResult.techScore >= 900000) rank.text = "A";
        else if (moveToResult.techScore >= 800000) rank.text = "B";
        else if (moveToResult.techScore >= 700000) rank.text = "C";
        else if (moveToResult.techScore >= 600000) rank.text = "D";
        else if (moveToResult.techScore >= 500000) rank.text = "E";
        else rank.text = "F";
        // highscore いったん保留
        combo.text = moveToResult.maxCombo.ToString();
        damage.text = moveToResult.damage.ToString();
        accuracy.text = $"{moveToResult.accuracy:F2}%";
        cBreakCount.text = moveToResult.judgeCounts[0].ToString();
        breakCount.text = moveToResult.judgeCounts[1].ToString();
        weakCount.text = moveToResult.judgeCounts[2].ToString();
        lostCount.text = moveToResult.judgeCounts[3].ToString();
        average.text = $"Average: {moveToResult.averageGap:F2}ms";

        for (int i = 0; i < 10; i++)
        {
            Vector2 offset = graphBars[i].offsetMax;
            offset.y = (float)(175 - 110 * moveToResult.gapGraphPercentage[i]) * (-1);
            graphBars[i].offsetMax = offset;
            /*
            100% : 65
            0%   : 175
            175 - 110 * percent
            */
        }
    }

    async void ShowBlock(int n, int time)
    {
        await Task.Delay(time);
        blocks[n].SetActive(true);
    }

    public void display()
    {
        ShowBlock(0, 750);
        ShowBlock(1, 1000);
        ShowBlock(2, 1250);
        ShowBlock(3, 1500);
    }
}
