using UnityEngine;

public static class GameSettings
{
    public static GameMode GameMode = GameMode.HumanVsHuman;
    public static DiscColor CpuColor = DiscColor.White;
    public static CPUDifficulty CpuDifficulty = CPUDifficulty.Medium;
    public static int BoardSize = 8; // 6 or 8
    
    // タイトル画面が表示されているかどうか
    public static bool IsTitleScreenActive = false;

    public static void ResetToDefaults()
    {
        GameMode = GameMode.HumanVsHuman;
        CpuColor = DiscColor.White;
        CpuDifficulty = CPUDifficulty.Medium;
        BoardSize = 8;
    }

    public static void ApplyTo(BoardManager manager)
    {
        if (manager == null) return;
        manager.gameMode = GameMode;
        manager.cpuColor = CpuColor;
        manager.cpuDifficulty = CpuDifficulty;
        manager.boardSize = BoardSize;
    }
}

