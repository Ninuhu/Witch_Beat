using UnityEngine;
using System.Collections.Generic;
using System.IO;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class NoteEditorManager : MonoBehaviour
{
    // --- Prefab ---
    public GameObject singlePrefab;
    public GameObject longPrefab;
    public GameObject slidePrefab;
    public GameObject widePrefab;
    // --- Lanes ---
    public Transform[] lanes;

    // --- Note Data ---
    private NoteList noteList = new NoteList();
    public TMP_InputField fileNameInput;
    public NoteType currentNoteType = NoteType.Single;

    // === 幅変更関連 ===
    [Header("Note Width Settings")]
    [SerializeField] private int currentWidth = 1;
    public TextMeshProUGUI widthText;

    public void IncreaseWidth()
    {
        if (currentWidth < 4) currentWidth++;
        UpdateWidthText();
    }

    public void DecreaseWidth()
    {
        if (currentWidth > 1) currentWidth--;
        UpdateWidthText();
    }

    private void UpdateWidthText()
    {
        if (widthText != null)
            widthText.text = $"SIZE: {currentWidth}";
    }

    // === ロングノーツ配置 ===
    private bool isPlacingLong = false;
    private int pendingLane;
    private float pendingStartTime;
    private GameObject pendingLongObj;
    private LongNote11 pendingLongVisualizer;

    // === ノーツ設置 ===
    public void PlaceNote(int laneIndex, float time, Vector2 position)
    {
        if (laneIndex < 0 || laneIndex >= lanes.Length) return;
        if (laneIndex + currentWidth - 1 >= lanes.Length) return;

        GameObject prefab = GetPrefabForType(currentNoteType);
        if (prefab == null) return;

        GameObject note = Instantiate(prefab, lanes[laneIndex]);
        RectTransform rt = note.GetComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x * currentWidth, rt.sizeDelta.y);

        note.name = $"{currentNoteType}_L{laneIndex}_W{currentWidth}_Y{position.y:F2}";

        Note newNote = new Note
        {
            lane = laneIndex,
            time = position.y,
            width = currentWidth,
            type = currentNoteType,
            duration = (currentNoteType == NoteType.Long || currentNoteType == NoteType.Slide) ? 2.0f : 0f
        };

        noteList.notes.Add(newNote);
        Debug.Log($"[{currentNoteType}] 置いた: Lane={laneIndex},Time={time:F2},Width={currentWidth},Pos Y={position.y:F2}");
    }

    // === ロングノーツ開始 ===
    public void BeginLongNote(int laneIndex, float startTime, Vector2 startPos)
    {
        if (isPlacingLong) return;
        if (laneIndex < 0 || laneIndex >= lanes.Length) return;
        if (laneIndex + currentWidth - 1 >= lanes.Length) return;

        GameObject prefab = GetPrefabForType(NoteType.Long);
        if (prefab == null) return;

        pendingLongObj = Instantiate(prefab, lanes[laneIndex]);
        RectTransform rt = pendingLongObj.GetComponent<RectTransform>();
        rt.anchoredPosition = startPos;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x * currentWidth, rt.sizeDelta.y);

        pendingLane = laneIndex;
        pendingStartTime = startPos.y;
        isPlacingLong = true;

        pendingLongVisualizer = pendingLongObj.GetComponent<LongNote11>();
        Debug.Log($"[Long Start] Lane{laneIndex},Start Y={startPos.y:F2},Time{startTime:F2}");
    }
   public void EndLongNote(int laneIndex, float endTime, Vector2 endPos)
{
    if (!isPlacingLong || pendingLongObj == null) return;

    float startY = pendingStartTime;
    float endY = endPos.y;

    RectTransform rt = pendingLongObj.GetComponent<RectTransform>();
        // ここをしっかり正しい方向に
        float topY = Mathf.Min(startY, endY); // 上側
    float bottomY = Mathf.Max(startY, endY); // 下側
    float height = Mathf.Abs(endY - startY);

    // 上側に合わせて位置とサイズを再設定
    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, topY);
    rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);

    // データ登録
    Note newNote = new Note
    {
        lane = pendingLane,
        time = Mathf.Min(startY, endY),
        endtime = Mathf.Max(startY, endY),
        duration = Mathf.Abs(endTime - pendingStartTime),
        width = currentWidth,
        type = NoteType.Long
    };
    noteList.notes.Add(newNote);

    // 後片付け
    if (pendingLongVisualizer != null)
        pendingLongVisualizer.SetEnd(endPos);

    Debug.Log($"[Long End] Lane={laneIndex}, StartY={startY:F2}, EndY={endY:F2}, Height={height:F2}");

    pendingLongObj = null;
    pendingLongVisualizer = null;
    isPlacingLong = false;
}

    // === ノーツ削除 ===
    public void DeleteNoteAtPosition(int laneIndex, Vector2 localPoint, float radius = 50f)
    {
        if (laneIndex < 0 || laneIndex >= lanes.Length) return;
        Transform lane = lanes[laneIndex];
        RectTransform target = null;

        foreach (RectTransform child in lane)
        {
            float dist = Vector2.Distance(child.anchoredPosition, localPoint);
            if (dist < radius)
            {
                target = child;
                break;
            }
        }

        if (target != null) Destroy(target.gameObject);
        Debug.Log($"ノーツ削除:Lane={laneIndex},name={target.name}");
    }

    // === JSON保存 ===
    public void SaveNotes(string fileName)
    {
        string json = JsonUtility.ToJson(noteList, true);
        string path = Application.dataPath + "/Notes/" + fileName + ".json";
        File.WriteAllText(path, json);
        Debug.Log("Saved notes to " + path);
    }

    public void SaveButton()
    {
        string folderPath = Application.dataPath + "/Notes/";
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string fileName = fileNameInput.text.Trim();
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("ファイル名を入力してください!");
            return;
        }

        SaveNotes(fileName);
    }

    // === ノーツタイプ切替 ===
    public Button[] typeButtons;
    public void SetNoteType(int typeIndex)
    {
        currentNoteType = (NoteType)typeIndex;
        for (int i = 0; i < typeButtons.Length; i++)
        {
            var colors = typeButtons[i].colors;
            colors.normalColor = (i == typeIndex) ? Color.cyan : Color.white;
            typeButtons[i].colors = colors;
        }
    }

    private GameObject GetPrefabForType(NoteType type)
    {
        switch (type)
        {
            case NoteType.Single: return singlePrefab;
            case NoteType.Long: return longPrefab;
            case NoteType.Slide: return slidePrefab;
            case NoteType.Wide: return widePrefab;
        }
        return null;
    }

    private IEnumerator Start()
    {
        yield return null;
        UpdateWidthText();
    }
}