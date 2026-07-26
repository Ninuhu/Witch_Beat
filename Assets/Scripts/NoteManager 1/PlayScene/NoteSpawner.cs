using UnityEngine;

using System.Collections.Generic;

using System.IO;

namespace RhytmGabe.playscene

{

    public class NoteSpawner : MonoBehaviour

    {

        [Header("ノーツPrefab")]

        public GameObject singlePrefab;

        public GameObject longPrefab;

        public GameObject slidePrefab;

        public GameObject widePrefab;

        [Header("攻撃Prefab（type別）")]
        public GameObject attackPrefabs;
        public GameObject Chargeprefab;

        [Header("各レーンの親オブジェクト(0〜7)")]
        public Transform[] lanes;

        [Header("攻撃ライン(0〜8)")]
        public Transform[] attackLines;

        [Header("読み込むJSONファイル名（拡張子なし）")]
        public string fileName;

        private NoteList noteList;
        private float startTime;

        [SerializeField] private float zScale = 0.1f; // ノーツ奥行スケール
        [SerializeField] public float noteSpeed = 8f; // 通常ノーツの落下速度
        [SerializeField] public float attackNoteSpeed = 5f; // 攻撃ノーツの落下速度 ←★追加

        private float hitZ = 0f; // 判定線Z=0

        void Start()
        {
            LoadNotes(fileName);
            startTime = Time.time;
        }

        void Update()
        {
            if (noteList == null) return;

            float songTime = Time.time - startTime;

            // ノーツ出現チェック
            foreach (Note n in noteList.notes)
            {
                if (!n.spawned && n.time <= songTime)
                {
                    SpawnNote(n);
                    n.spawned = true;
                }
            }

            // 攻撃出現チェック
            foreach (Attack a in noteList.attacks)
            {
                if (!a.spawned && a.time <= songTime)
                {
                    SpawnAttack(a);
                    a.spawned = true;
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
                Debug.Log($"Loaded {noteList.notes.Count} notes and {noteList.attacks.Count} attacks from {file}.json");
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

            float spawnZ = hitZ + (n.time * zScale + 25f);

            // 各レーンのX座標（中央位置）
            float[] laneX = { -7f, -5f, -3f, -1f, 1f, 3f, 5f, 7f };

            // ワールド座標で生成
            Vector3 spawnPos = new Vector3(laneX[n.lane], 0f, spawnZ);
            GameObject note = Instantiate(prefab, spawnPos, Quaternion.identity);

            // 親を設定したい場合（整理用）—座標ずれない
            note.transform.SetParent(lanes[n.lane], worldPositionStays: true);

            NoteMover mover = note.GetComponent<NoteMover>();
            if (mover != null)
            {
                mover.Initialize(n.time, spawnZ, hitZ, noteSpeed); // ←★通常ノーツ用スピード
            }

            Debug.Log($"Spawned NOTE lane={n.lane}, x={laneX[n.lane]}, z={spawnZ}");
        }

        void SpawnAttack(Attack a)
        {
            Debug.Log($"ATTACK DATA → lane={a.lane}, time={a.time}, type={a.type}");
            if (a.lane < 0 || a.lane >= attackLines.Length)
            {
                Debug.LogError($"Invalid attack line index: {a.lane}");
                return;
            }

            GameObject prefab = null;
            switch (a.type)
            {
                case AtackType.ATsingle: prefab = attackPrefabs; break;
                case AtackType.Charge: prefab = Chargeprefab; break;
                default: prefab = attackPrefabs; break;
            }

            if (prefab == null)
            {
                Debug.LogError("Prefab not assigned for attack type: " + a.type);
                return;
            }

            // 👇 出現位置を固定でZ=12に設定
            float spawnZ = 24f;

            // attackLines からXとYを取得（lineごとに位置をずらす）
            Vector3 basePos = attackLines[a.lane].position;
            Vector3 spawnPos = new Vector3(basePos.x, basePos.y, spawnZ);

            // 生成（親なし・ワールド座標で）
            GameObject atk = Instantiate(prefab, spawnPos, Quaternion.identity);

            // 移動初期化
            AttackNoteMover atkMover = atk.GetComponent<AttackNoteMover>();
            if (atkMover != null)
            {
                atkMover.Initialize(a.time, spawnZ, hitZ, attackNoteSpeed); // ←★攻撃ノーツ用スピード
            }

            Debug.Log($"[ATTACK SPAWN] lane={a.lane}, X={spawnPos.x}, Y={spawnPos.y}, Z={spawnPos.z}");
        }
    }
}