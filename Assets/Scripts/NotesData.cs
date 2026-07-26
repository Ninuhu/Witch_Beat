using System;
using System.Collections.Generic;

[Serializable]
public class Note
{
    public int lane;   // レーン番号
    public float time; // 出現時間
    public NoteType type; //noteの種類
    public float endtime; // longnote 範囲
    public float duration; // ロングノーツ用の長さ
    public int width = 1; // 何レーン幅聞かせるか
    [NonSerialized]
    public bool spawned = false; //playscene nomi
}
public enum NoteType
{
    Single, Long, //普通のnote,ロングノーツ
    Slide, Wide //入り込むロングノーツ、幅聞かせのーつ（レーン何個分）
}
[Serializable]
public class Attack
{
    public float time; //出現時間
    public int lane; //0~8本no lane
    
    public  AtackType type; //0~2 の攻撃パターン

    [NonSerialized] public bool spawned = false; //生成済み確認
}
public enum AtackType
{
    ATsingle,Charge //普通の攻撃、チャージ
}

[Serializable]
public class NoteList
{
    public List<Note> notes = new List<Note>();
    public List<Attack> attacks = new List<Attack>();
}