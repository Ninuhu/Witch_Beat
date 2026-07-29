using UnityEngine;

/// <summary>
/// スキル玉・攻撃玉など、レーンの線上を流れてくるオブジェクトの移動を担当するクラス。
/// 画面奥から手前に流れ、判定ライン付近を通り過ぎたら非表示位置へ退避させる。
/// </summary>
public class lineObjectMover : MonoBehaviour
{
    float x, y, z;
    bool destroy;

    Transform trans;
    laneManager laneManager;



    // このZ座標より手前に来たら「通過済み」として退避
    const float DESPAWN_Z = -2f;

    void Start()
    {
        GameObject lane = GameObject.FindGameObjectWithTag("lane");
        laneManager = lane.GetComponent<laneManager>();
        trans = gameObject.GetComponent<Transform>();

        x = trans.position.x;
        y = trans.position.y;
        z = trans.position.z;

        destroy = false;
    }



    void Update()
    {
        // 現在フレームでのZ座標を1回だけ計算し、移動・退避判定の両方に使い回す
        float currentZ = z - laneManager.settingSpeed / 2f * (float)laneManager.t - 2;

        if (laneManager.playing && !destroy) trans.position = new Vector3(x, y, currentZ);
        

        if (currentZ < DESPAWN_Z)
        {
            destroy = true;
            trans.position = new Vector3(x, -10, DESPAWN_Z);
        }
    }
}