using UnityEngine;

public class AttackNoteMover : MonoBehaviour
{
    private float spawnZ;
    private float hitZ;
    private float speed;
    private float targetTime;
    private bool initialized = false;

private const float deleteZ = -20f; // このZ座標Sで消す

    // NoteSpawner から呼ぶ
    public void Initialize(float time, float spawnZ, float hitZ, float speed)
    {
        targetTime = time;
        this.spawnZ = spawnZ;
        this.hitZ = hitZ;
        this.speed = speed;
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;

        // ノーツと同じく deleteZ 方向に移動
        float step = speed * Time.deltaTime;
        Vector3 cur = transform.localPosition;
        Vector3 target = new Vector3(cur.x, cur.y, deleteZ);
        transform.localPosition = Vector3.MoveTowards(cur, target, step);

        // 指定座標まで来たら削除
        if (transform.localPosition.z <= deleteZ)
        {
            Destroy(gameObject);
        }
    }

}