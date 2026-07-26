using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class selectScene : MonoBehaviour
{
    // Inspectorで曲ボタン3つを順番にセット
    // index 0:mopemope(ID1) / index 1:shiningstar(ID2) / index 2:song3(ID3)
    [SerializeField] Button[] songButtons = new Button[3];
    [SerializeField] Image[] songButtonImages = new Image[3];

    // Inspectorで難易度ボタン2つをセット
    // index 0:normal(0) / index 1:hard(1)
    [SerializeField] Button[] difficultyButtons = new Button[2];
    [SerializeField] Image[] difficultyButtonImages = new Image[2];

    [SerializeField] Button startButton;

    [SerializeField] Color selectedColor = Color.yellow;
    [SerializeField] Color normalColor = Color.white;

    [SerializeField] string playSceneName = "playScene"; // 実際のPlayシーン名に合わせて変更

    int selectedSongIndex = -1;       // 0,1,2 (IDにするときは+1する)
    int selectedDifficultyIndex = -1; // 0,1

    void Start()
    {
        for (int i = 0; i < songButtons.Length; i++)
        {
            int index = i; // ラムダ式でのクロージャ対策（ループ変数をそのまま使うと全部最後の値になるため）
            songButtons[i].onClick.AddListener(() => SelectSong(index));
        }

        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            int index = i;
            difficultyButtons[i].onClick.AddListener(() => SelectDifficulty(index));
        }

        startButton.onClick.AddListener(StartGame);
        startButton.interactable = false; // 両方選ぶまで押せない
    }

    void SelectSong(int index)
    {
        selectedSongIndex = index;
        for (int i = 0; i < songButtonImages.Length; i++)
        {
            songButtonImages[i].color = (i == index) ? selectedColor : normalColor;
        }
        CheckReady();
    }

    void SelectDifficulty(int index)
    {
        selectedDifficultyIndex = index;
        for (int i = 0; i < difficultyButtonImages.Length; i++)
        {
            difficultyButtonImages[i].color = (i == index) ? selectedColor : normalColor;
        }
        CheckReady();
    }

    void CheckReady()
    {
        startButton.interactable = (selectedSongIndex != -1 && selectedDifficultyIndex != -1);
    }

    void StartGame()
    {
        GameSettings.selectedMusicId = selectedSongIndex + 1;       // 0,1,2 -> 1,2,3
        GameSettings.selectedDifficulty = selectedDifficultyIndex;  // 0,1 そのまま

        SceneManager.LoadScene(playSceneName);
    }
}