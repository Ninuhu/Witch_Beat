// 曲の基本情報を保持するデータ構造
using UnityEngine;

[System.Serializable]
public class MusicData
{
    public string title;       // 曲名
    public int musicID;        // 曲を一意に識別するID
    public AudioClip audioClip; // 実際の楽曲データ

    // 難易度ごとの情報を保持するクラス
    [System.Serializable]
    public class DifficultyInfo
    {
        public string difficultyName; // "EASY", "NORMAL"など
        public int level;             // 難易度レベル
        public int maxScore;          // 記録されたハイスコア（セーブデータから読み込む）
        public TextAsset chartData;    // 譜面データ
    }

    public DifficultyInfo[] difficultyInfos; // 難易度の配列
}