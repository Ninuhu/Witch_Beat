using UnityEngine;

/// <summary>
/// 個々のノーツオブジェクトの移動と、レーンごとのタグ付けを担当するクラス。
/// 生成時点では "single" または "double" という汎用タグしか付いていないため、
/// 自分のX座標からレーン番号を判定し、"single0"～"single7" のような
/// レーン別タグに付け替える（characterController側がこのタグでノーツを検索するため）。
/// </summary>
public class noteMover : MonoBehaviour
{
    float x, z;

    Transform trans;
    laneManager laneManager;

    [SerializeField] MeshRenderer thisMR, subMR1, subMR2;

    // X座標からレーン番号を判定するための閾値（lane:0の時x:-7, lane:7の時x:7、2刻み）
    static readonly float[] laneXThresholds = { -6.5f, -4.5f, -2.5f, -0.5f, 1.5f, 3.5f, 5.5f };

    void Start()
    {
        GameObject lane = GameObject.FindGameObjectWithTag("lane");
        laneManager = lane.GetComponent<laneManager>();
        trans = gameObject.GetComponent<Transform>();

        x = trans.position.x;
        z = trans.position.z;

        bool isSingleNote = gameObject.tag == "single";
        string tagPrefix = isSingleNote ? "single" : "double";
        gameObject.tag = tagPrefix + DetermineLaneIndex(x);
    }



    // X座標から、0～7のレーン番号を求める
    int DetermineLaneIndex(float xPosition)
    {
        for (int lane = 0; lane < laneXThresholds.Length; lane++)
        {
            if (xPosition < laneXThresholds[lane]) return lane;
        }
        return 7; // どの閾値にも該当しなければ一番右のレーン
    }

    void Update()
    {
        if (laneManager.playing)
        {
            trans.position = new Vector3(x, 0, z - laneManager.settingSpeed / 2f * (float)laneManager.t);
        }
    }


    // 叩かれた（判定済みになった）ノーツの見た目消す
    // Destroyせず非表示にしているのは、判定処理側がインデックス経由で同じ配列を参照し続ける実装になっているため
    public void Destroy()
    {
        thisMR.enabled = false;
        subMR1.enabled = false;
        subMR2.enabled = false;
    }
}
