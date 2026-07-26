using UnityEngine;

public class NoteVisual : MonoBehaviour
{
    public int lane;       // 左端レーン
    public int width = 1;  // 何レーン分占有してるか
    public NoteType type;
    public float time;
    public float duration;
}