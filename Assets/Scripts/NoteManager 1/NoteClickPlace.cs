using UnityEngine;
using UnityEngine.UI;

public class NoteClickPlace : MonoBehaviour
{
    public RectTransform[] lanes;
    public RectTransform notesContainer;
    public Canvas canvas;
    public NoteEditorManager editorManager;
    public float pixelsPerSecond = 100f;

    private int selectedLaneIndex = 0;
    private bool isLongNoteStart = false;
    private Vector2 longStartPos;
    private float longStartTime;

    private void Awake()
    {
        if (editorManager == null)
            editorManager = GetComponent<NoteEditorManager>();
    }

    void Update()
    {
        // 数字キーでレーン選択
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedLaneIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedLaneIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedLaneIndex = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) selectedLaneIndex = 3;
        if (Input.GetKeyDown(KeyCode.Alpha5)) selectedLaneIndex = 4;
        if (Input.GetKeyDown(KeyCode.Alpha6)) selectedLaneIndex = 5;
        if (Input.GetKeyDown(KeyCode.Alpha7)) selectedLaneIndex = 6;
        if (Input.GetKeyDown(KeyCode.Alpha8)) selectedLaneIndex = 7;

        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceCamera) ? canvas.worldCamera : null;

        // === 左クリックでノーツ配置 ===
        if (Input.GetMouseButtonDown(0))
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(notesContainer, Input.mousePosition, cam))
                return;

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                lanes[selectedLaneIndex], Input.mousePosition, cam, out localPoint)) return;

            // 単純なY座標をそのまま使う（中央補正なし）
            float time = localPoint.y / pixelsPerSecond;

            if (editorManager.currentNoteType == NoteType.Long)
            {
                if (!isLongNoteStart)
                {
                    longStartPos = localPoint;
                    longStartTime = time;
                    editorManager.BeginLongNote(selectedLaneIndex, time, localPoint);
                    isLongNoteStart = true;
                }
                else
                {
                    editorManager.EndLongNote(selectedLaneIndex, time, localPoint);
                    isLongNoteStart = false;
                }
            }
            else
            {
                editorManager.PlaceNote(selectedLaneIndex, time, localPoint);
            }
        }

        // === 右クリックで削除 ===
        if (Input.GetMouseButtonDown(1))
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(notesContainer, Input.mousePosition, cam))
                return;

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                lanes[selectedLaneIndex], Input.mousePosition, cam, out localPoint)) return;

            editorManager.DeleteNoteAtPosition(selectedLaneIndex, localPoint, 50f);

            if (isLongNoteStart)
            {
                isLongNoteStart = false;
                Debug.Log("ロングノーツ設置キャンセル");
            }
        }
    }
}