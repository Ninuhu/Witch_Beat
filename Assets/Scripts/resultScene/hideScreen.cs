using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class hideScreen : MonoBehaviour
{
    Image img;
    byte alpha;

    [SerializeField] showScores showScores;

    async void Start()
    {
        gameObject.transform.SetAsLastSibling();
        img = gameObject.GetComponent<Image>();
        alpha = 255;
        while (alpha != 0)
        {
            alpha -= 5;
            img.color = new Color32 (0, 0, 0, alpha);
            await Task.Delay(5);
        }
        gameObject.transform.SetAsFirstSibling();
        showScores.display();
    }
}
