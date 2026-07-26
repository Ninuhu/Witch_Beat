using UnityEngine;

public class NOTEMOVER1 : MonoBehaviour
{
    private float spawnZ;
    private float hitZ;
    private float noteSpeed;
    private float targetTime;
    private bool initialized = false;
    // Initializeの引数順をSpawnerと一致させた
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

        // Z方向に移動
        transform.localPosition -= new Vector3(0, 0, noteSpeed * Time.deltaTime);

        // 判定線を超えたら削除
        if (transform.localPosition.z <= hitZ - 1f)
        {
            Destroy(gameObject);
        }
    }
}
