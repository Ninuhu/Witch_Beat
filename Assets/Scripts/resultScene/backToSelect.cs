using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class backToSelect : MonoBehaviour
{
    [SerializeField] Button button;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        button.onClick.AddListener(Back);
    }

    void Back()
    {
        SceneManager.LoadScene("SelectScene");
    }
}
