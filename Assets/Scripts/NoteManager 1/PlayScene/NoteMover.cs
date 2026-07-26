using UnityEngine;

public class NoteMover : MonoBehaviour
{
    private float spawnZ;
    private float hitZ;
    private float noteSpeed;
    private float targetTime;
    private bool initialized = false;
    private const float deleteZ = -20f; // このZ座標で消す

    public void Initialize(float time, float spawnZ, float hitZ, float speed)
    {
        targetTime = time;
        this.spawnZ = spawnZ;
        this.hitZ = hitZ;
        this.noteSpeed = speed;
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;

        // hitZじゃなくdeleteZ方向に動かす！
        float step = noteSpeed * Time.deltaTime;
        Vector3 cur = transform.localPosition;
        Vector3 target = new Vector3(cur.x, cur.y, deleteZ);
        transform.localPosition = Vector3.MoveTowards(cur, target, step);

        // deleteZに到達したら削除
        if (transform.localPosition.z <= deleteZ)
        {
            Destroy(gameObject);
        }
    }
}