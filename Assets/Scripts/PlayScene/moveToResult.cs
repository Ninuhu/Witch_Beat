using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

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

    void Awake()
    {
        // 変数引継ぎ
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 初期化
        averageGap = 0;
        alpha = 0;
        gapSaveList = new List<double>();
        gapGraphPercentage = new double[10];
        judgeCounts = new int[4];
    }

    async void ShowBlackScreen()
    {
        overlayObj = GameObject.FindWithTag("overlay");
        overlayObj.transform.SetAsLastSibling();
        overlayImage = overlayObj.GetComponent<Image>();
        alpha = 0;
        while (alpha != 255)
        {
            alpha += 5;
            overlayImage.color = new Color32 (0, 0, 0, alpha);
            await Task.Delay(5);
        }
    }

    async void SceneLoad(string name, int time)
    {
        await Task.Delay(time);
        SceneManager.LoadScene(name);
    }

    public void MoveToResultScene()
    {
        characterController = FindObjectOfType<characterController>();
        notesGenerator = FindObjectOfType<notesGenerator>();

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
        techScore = (int)characterController.techScore - damage * 10000;
        maxCombo = characterController.maxComboCount;
        accuracy = characterController.bossHp;
        for (int i = 0; i < gapSaveList.Count; i++)
        {
            averageGap += gapSaveList[i];
            for (int k = 0; k < 10; k++)
            {
                if (gapSaveList[i] < -0.08 + 0.02*k) 
                {
                    gapGraphPercentage[k]++;
                    break;
                }
            }
        }
        for (int k = 0; k < 10; k++)
        {
            gapGraphPercentage[k] /= gapSaveList.Count;
        }
        averageGap = averageGap / gapSaveList.Count * 1000;

        ShowBlackScreen();
        SceneLoad("resultScene", 2000);
    }
}
