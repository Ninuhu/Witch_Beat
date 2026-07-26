using UnityEngine;

public class skyboxChanger : MonoBehaviour
{
    public int skyboxIndex;

    [SerializeField] Material[] skys = new Material[4];

    float rotationRepeatValue = 0f;

    void Start()
    {
        RenderSettings.skybox = skys[skyboxIndex]; // listからindexでskybox指定
    }

    void Update()
    {
        rotationRepeatValue = Mathf.Repeat(skys[skyboxIndex].GetFloat("_Rotation") + 0.002f , 360f); // 1fに0.002度ずつ回転
        skys[skyboxIndex].SetFloat("_Rotation",rotationRepeatValue);
    }
}
