using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class NoteSpawner : MonoBehaviour
{
    [Header("ノーツPrefab")]
    public GameObject singlePrefab;
    public GameObject longPrefab;
    public GameObject slidePrefab;
    public GameObject widePrefab;
    [Header("各レーンの親オブジェクト(0〜7)")]
    public Transform[] lanes;

    [Header("読み込むJSONファイル名（拡張子なし）")]
    public string fileName;

    [Header("曲再生用")]
    public AudioSource musicSource;

    private NoteList noteList;
    private bool isPlaying = false;
    private float startTime;

    // ノーツが出現するZ軸の範囲設定
    [SerializeField] private float spawnZ = 25f;  // 出現位置
    [SerializeField] private float hitZ = 0f;     // 判定位置
    [SerializeField] private float noteSpeed = 10f; // 1秒あたり進むZ距離（あとで調整OK）

    void Start()
    {
        LoadNotes(fileName);

        if (musicSource != null)
        {
            musicSource.Play();
            startTime = Time.time;
            isPlaying = true;
        }
        else
        {
            Debug.LogWarning("AudioSourceが設定されていません。");
            startTime = Time.time;
            isPlaying = true;
        }
    }

    void Update()
    {
        if (!isPlaying || noteList == null || noteList.notes.Count == 0) return;

        float songTime = Time.time - startTime;

        foreach (Note n in noteList.notes)
        {
            if (!n.spawned && n.time <= songTime + (spawnZ / noteSpeed))
            {
                SpawnNote(n);
                n.spawned = true;
            }
        }
    }

    // JSONから譜面データ読込
    void LoadNotes(string file)
    {
        string path = Application.dataPath + "/Notes/" + file + ".json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            noteList = JsonUtility.FromJson<NoteList>(json);
            Debug.Log($"Loaded {noteList.notes.Count} notes from {file}.json");
        }
        else
        {
            Debug.LogError("File not found: " + path);
        }
    }

    // ノーツを指定レーンに生成
    void SpawnNote(Note n)
    {
        if (n.lane < 0 || n.lane >= lanes.Length)
        {
            Debug.LogError($"Invalid lane index: {n.lane}");
            return;
        }

        GameObject prefab = null;
        switch (n.type)
        {
            case NoteType.Single: prefab = singlePrefab; break;
            case NoteType.Long: prefab = longPrefab; break;
            case NoteType.Slide: prefab = slidePrefab; break;
            case NoteType.Wide: prefab = widePrefab; break;
        }

        if (prefab == null)
        {
            Debug.LogError("Prefab not assigned for type: " + n.type);
            return;
        }

        GameObject note = Instantiate(prefab, lanes[n.lane]);
        note.transform.localPosition = new Vector3(0, 0, spawnZ);

        // 落下スクリプトがあるなら初期化
        NoteMover mover = note.GetComponent<NoteMover>();
        if (mover != null)
        {
            mover.Initialize(n.time, spawnZ, hitZ, noteSpeed);
        }
    }
}