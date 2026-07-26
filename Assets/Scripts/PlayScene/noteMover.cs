using UnityEngine;

public class noteMover : MonoBehaviour
{
    float x, z;

    Transform trans;
    laneManager laneManager;

    [SerializeField] MeshRenderer thisMR, subMR1, subMR2;

    void Start()
    {
        GameObject lane = GameObject.FindGameObjectWithTag("lane");
        laneManager = lane.GetComponent<laneManager>();
        trans = gameObject.GetComponent<Transform>();

        x = trans.position.x;
        z = trans.position.z;

        //lane:0の時x:-7, lane:7の時x:7
        if (gameObject.tag == "single")
        {
            if (x < -6.5f) gameObject.tag = "single0";
            else if (x < -4.5f) gameObject.tag = "single1";
            else if (x < -2.5f) gameObject.tag = "single2";
            else if (x < -0.5f) gameObject.tag = "single3";
            else if (x < 1.5f) gameObject.tag = "single4";
            else if (x < 3.5f) gameObject.tag = "single5";
            else if (x < 5.5f) gameObject.tag = "single6";
            else gameObject.tag = "single7";
        }
        else
        {
            if (x < -6.5f) gameObject.tag = "double0";
            else if (x < -4.5f) gameObject.tag = "double1";
            else if (x < -2.5f) gameObject.tag = "double2";
            else if (x < -0.5f) gameObject.tag = "double3";
            else if (x < 1.5f) gameObject.tag = "double4";
            else if (x < 3.5f) gameObject.tag = "double5";
            else if (x < 5.5f) gameObject.tag = "double6";
            else gameObject.tag = "double7";
        }
    }

    void Update()
    {
        if (laneManager.playing == true)
        {
            trans.position = new Vector3(x, 0, z - laneManager.settingSpeed /2f *(float)laneManager.t);
        }
    }

    public void Destroy()
    {
        thisMR.enabled = false;
        subMR1.enabled = false;
        subMR2.enabled = false;
    }
}
