using UnityEngine;

public class lineObjectMover : MonoBehaviour
{
    float x, y, z;
    bool destroy;

    Transform trans;
    laneManager laneManager;

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
        if (laneManager.playing == true && destroy == false)
        {
            trans.position = new Vector3(x, y, z - laneManager.settingSpeed /2f *(float)laneManager.t -2);
        }
        if (z - laneManager.settingSpeed /2f *(float)laneManager.t -2 < -2) 
        {
            destroy = true;
            trans.position = new Vector3(x, -10, -2);
        }
    }
}
