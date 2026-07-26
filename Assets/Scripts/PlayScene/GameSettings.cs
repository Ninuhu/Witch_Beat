// 選曲シーンで選んだ内容をPlayシーンに渡すための静的クラス
// シーンをまたいでも値が保持される（アプリ実行中は常に保持、DontDestroyOnLoad等は不要）
public static class GameSettings
{
    // 曲ID: 1=mopemope, 2=shiningstar, 3=song3
    public static int selectedMusicId = 1;

    // 難易度: 0=normal, 1=hard （デバッグ用に-1も使う場合はnotesGenerator側でdebug.jsonを読む）
    public static int selectedDifficulty = 0;
}
