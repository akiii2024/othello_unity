using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameMode
{
    HumanVsHuman,  // 対人戦
    HumanVsCPU     // CPU対戦
}

public enum CPUDifficulty
{
    Easy,    // 簡単：ランダムに手を選ぶ
    Medium,  // 中級：評価関数を使うが、時々ランダムに選ぶ
    Hard     // 難しい：最良の手を選ぶ
}

public class BoardManager : MonoBehaviour
{
    public BoardGrid grid;
    public GameObject piecePrefab;
    public float dropHeight = 2.0f;
    public BoardHighlighter highlighter;

    [Header("CPU対戦設定")]
    public GameMode gameMode = GameMode.HumanVsHuman;
    public DiscColor cpuColor = DiscColor.White;  // CPUの色
    public float cpuThinkDelay = 0.5f;  // CPUの思考時間（秒）
    public CPUDifficulty cpuDifficulty = CPUDifficulty.Medium;  // CPUの難易度

    [Header("スタック設定")]
    public int maxStackHeight = 2;  // 最大スタック段数
    public float stackHeightOffset = 0.15f;  // 駒の厚み（2段目のY座標オフセット）

    // スタック対応データ構造
    int[,] stackCount = new int[8, 8];           // 各マスの駒の段数（0, 1, 2）
    DiscColor[,] topColor = new DiscColor[8, 8]; // 一番上の駒の色
    List<PieceView>[,] pieceStacks = new List<PieceView>[8, 8]; // 駒のビュー（複数）

    public DiscColor turn = DiscColor.Black;
    bool isProcessingMove = false;  // 手を処理中かどうか

    static readonly (int dx, int dy)[] dirs = new (int, int)[]
    {
        (-1,-1),(0,-1),(1,-1),
        (-1, 0),       (1, 0),
        (-1, 1),(0, 1),(1, 1),
    };

    void Start()
    {
        InitBoard();
    }

    void Update()
    {
        // CPU対戦モードで、CPUのターンかつ処理中でない場合
        if (gameMode == GameMode.HumanVsCPU && 
            turn == cpuColor && 
            !isProcessingMove &&
            HasAnyLegalMove(turn))
        {
            StartCoroutine(CPUTurnCoroutine());
        }
    }

    public void InitBoard()
    {
        // 既存駒削除
        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++)
        {
            stackCount[x, y] = 0;
            topColor[x, y] = DiscColor.Empty;
            if (pieceStacks[x, y] != null)
            {
                foreach (var pv in pieceStacks[x, y])
                {
                    if (pv != null) Destroy(pv.gameObject);
                }
            }
            pieceStacks[x, y] = new List<PieceView>();
        }

        // 初期配置（標準の並び）
        // (3,3) White / (4,4) White
        // (3,4) Black / (4,3) Black
        SpawnDiscImmediate(3, 3, DiscColor.White);
        SpawnDiscImmediate(4, 4, DiscColor.White);
        SpawnDiscImmediate(3, 4, DiscColor.Black);
        SpawnDiscImmediate(4, 3, DiscColor.Black);

        turn = DiscColor.Black;
        Debug.Log("Game start. Turn = Black (Stack Othello)");

        UpdateHighlighter();
    }

    // クリックから呼ぶ
    public void TryPlace(int x, int y)
    {
        // CPU対戦モードでCPUのターンの場合は無視
        if (gameMode == GameMode.HumanVsCPU && turn == cpuColor)
        {
            return;
        }

        if (isProcessingMove)
        {
            return;
        }

        if (!IsLegalMove(x, y, turn, out var flips))
        {
            Debug.Log($"Illegal move: ({x},{y})");
            return;
        }

        ApplyMove(x, y, turn, flips);
        ProcessTurnEnd();
    }

    void ProcessTurnEnd()
    {
        // ターン切替
        turn = Opponent(turn);

        // パス処理
        if (!HasAnyLegalMove(turn))
        {
            if (HasAnyLegalMove(Opponent(turn)))
            {
                Debug.Log($"{turn} has no moves -> PASS");
                turn = Opponent(turn);
            }
            else
            {
                // 両者置けない -> 終了
                int b = Count(DiscColor.Black);
                int w = Count(DiscColor.White);
                Debug.Log($"Game Over! Black={b} White={w} Winner={(b>w?"Black":(w>b?"White":"Draw"))}");
            }
        }
        else
        {
            Debug.Log($"Turn = {turn}");
        }
        UpdateHighlighter();
    }

    void ApplyMove(int x, int y, DiscColor color, List<(int x, int y)> flips)
    {
        SpawnDiscDrop(x, y, color);

        foreach (var f in flips)
        {
            // スタック全体をひっくり返す
            topColor[f.x, f.y] = color;
            var stack = pieceStacks[f.x, f.y];
            if (stack != null)
            {
                foreach (var pv in stack)
                {
                    if (pv != null) pv.FlipTo(color);
                }
            }
        }

        int b = Count(DiscColor.Black);
        int w = Count(DiscColor.White);
        Debug.Log($"Placed {color} at ({x},{y}) [Stack:{stackCount[x,y]}]  Black={b} White={w}");
    }

    bool IsLegalMove(int x, int y, DiscColor color, out List<(int x, int y)> flips)
    {
        flips = new List<(int, int)>();
        if (!InBounds(x, y)) return false;

        // 空きマス、または自分の駒で最大スタック未満の場合のみ置ける
        bool isEmpty = stackCount[x, y] == 0;
        bool canStack = topColor[x, y] == color && stackCount[x, y] < maxStackHeight;
        if (!isEmpty && !canStack) return false;

        DiscColor opp = Opponent(color);

        foreach (var d in dirs)
        {
            var line = new List<(int, int)>();
            int cx = x + d.dx;
            int cy = y + d.dy;

            // 最初が相手色でないとダメ（トップの色をチェック）
            if (!InBounds(cx, cy) || topColor[cx, cy] != opp) continue;

            // 相手が続く限り進む
            while (InBounds(cx, cy) && topColor[cx, cy] == opp)
            {
                line.Add((cx, cy));
                cx += d.dx;
                cy += d.dy;
            }

            // その先が自分色なら挟める
            if (InBounds(cx, cy) && topColor[cx, cy] == color)
            {
                flips.AddRange(line);
            }
        }

        return flips.Count > 0;
    }

    bool HasAnyLegalMove(DiscColor color)
    {
        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++)
        {
            if (IsLegalMove(x, y, color, out _)) return true;
        }
        return false;
    }

    List<(int x, int y)> GetLegalMoves(DiscColor color)
    {
        var moves = new List<(int x, int y)>();
        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++)
            if (IsLegalMove(x, y, color, out _))
                moves.Add((x, y));
        return moves;
    }

    /// <summary>
    /// 通常手とスタック手を区別して取得
    /// </summary>
    void GetLegalMovesSeparated(DiscColor color, out List<(int x, int y)> normalMoves, out List<(int x, int y)> stackMoves)
    {
        normalMoves = new List<(int x, int y)>();
        stackMoves = new List<(int x, int y)>();
        
        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++)
        {
            if (IsLegalMove(x, y, color, out _))
            {
                // 空きマスなら通常手、自分の駒があればスタック手
                if (stackCount[x, y] == 0)
                {
                    normalMoves.Add((x, y));
                }
                else
                {
                    stackMoves.Add((x, y));
                }
            }
        }
    }

    int Count(DiscColor c)
    {
        int n = 0;
        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++)
            if (topColor[x, y] == c) n += stackCount[x, y];
        return n;
    }

    static bool InBounds(int x, int y) => (0 <= x && x < 8 && 0 <= y && y < 8);

    static DiscColor Opponent(DiscColor c) => (c == DiscColor.Black) ? DiscColor.White : DiscColor.Black;

    void SpawnDiscImmediate(int x, int y, DiscColor color)
    {
        // スタック段数に応じてY座標をオフセット
        int currentStack = stackCount[x, y];
        Vector3 center = grid.CellToWorld(x, y);
        center.y += currentStack * stackHeightOffset;

        var go = Instantiate(piecePrefab, center, Quaternion.identity);

        // 初期配置は物理を止めて固定
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        var pv = go.GetComponent<PieceView>();
        pv.SetColorImmediate(color);

        // スタックに追加
        if (pieceStacks[x, y] == null)
        {
            pieceStacks[x, y] = new List<PieceView>();
        }
        pieceStacks[x, y].Add(pv);
        stackCount[x, y]++;
        topColor[x, y] = color;
    }

    void SpawnDiscDrop(int x, int y, DiscColor color)
    {
        // スタック段数に応じてY座標をオフセット
        int currentStack = stackCount[x, y];
        Vector3 center = grid.CellToWorld(x, y);
        center.y += currentStack * stackHeightOffset;
        Vector3 spawn = center + Vector3.up * dropHeight;

        var go = Instantiate(piecePrefab, spawn, Quaternion.identity);

        // 色設定
        var pv = go.GetComponent<PieceView>();
        pv.SetColorImmediate(color);

        // スタックに追加
        if (pieceStacks[x, y] == null)
        {
            pieceStacks[x, y] = new List<PieceView>();
        }
        pieceStacks[x, y].Add(pv);
        stackCount[x, y]++;
        topColor[x, y] = color;

        // 落下→吸着
        var magnet = go.GetComponent<PieceMagnet>();
        magnet.SetTarget(center);

        // もし前回の固定が残ってたら戻す（Prefab設定によって）
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;
    }
    public void UpdateHighlighter()
    {
        if (highlighter != null)
        {
            // CPU対戦モードでCPUのターンの場合はハイライトを非表示
            if (gameMode == GameMode.HumanVsCPU && turn == cpuColor)
            {
                highlighter.Clear();
            }
            else
            {
                // 通常手とスタック手を区別して取得・表示
                GetLegalMovesSeparated(turn, out var normalMoves, out var stackMoves);
                highlighter.ShowMoves(normalMoves, stackMoves);
            }
        }
    }

    // CPUのターン処理（コルーチン）
    IEnumerator CPUTurnCoroutine()
    {
        isProcessingMove = true;
        
        // 思考時間の待機
        yield return new WaitForSeconds(cpuThinkDelay);

        // CPUの手を選択
        var move = SelectCPUMove();
        if (move.HasValue)
        {
            var (x, y) = move.Value;
            if (IsLegalMove(x, y, cpuColor, out var flips))
            {
                ApplyMove(x, y, cpuColor, flips);
                ProcessTurnEnd();
            }
        }
        else
        {
            // 合法手がない場合（パス）
            ProcessTurnEnd();
        }

        isProcessingMove = false;
    }

    // CPUの手を選択するAI
    (int x, int y)? SelectCPUMove()
    {
        var legalMoves = GetLegalMoves(cpuColor);
        if (legalMoves.Count == 0)
        {
            return null;
        }

        // 難易度に応じて手を選択
        switch (cpuDifficulty)
        {
            case CPUDifficulty.Easy:
                // 簡単：完全にランダム
                return legalMoves[Random.Range(0, legalMoves.Count)];

            case CPUDifficulty.Medium:
                // 中級：70%の確率で最良の手、30%でランダム
                if (Random.Range(0f, 1f) < 0.7f)
                {
                    return SelectBestMove(legalMoves);
                }
                else
                {
                    return legalMoves[Random.Range(0, legalMoves.Count)];
                }

            case CPUDifficulty.Hard:
                // 難しい：常に最良の手を選ぶ
                return SelectBestMove(legalMoves);

            default:
                return SelectBestMove(legalMoves);
        }
    }

    // 最良の手を選択する（評価関数を使用）
    (int x, int y)? SelectBestMove(List<(int x, int y)> legalMoves)
    {
        (int x, int y)? bestMove = null;
        int bestScore = int.MinValue;

        foreach (var move in legalMoves)
        {
            int score = EvaluateMove(move.x, move.y, cpuColor);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    // 手の評価関数（位置の重要度 + 取れる駒の数 + スタック考慮）
    int EvaluateMove(int x, int y, DiscColor color)
    {
        int score = 0;

        // 位置の重要度（角や端が高得点）
        int[,] positionValue = new int[8, 8]
        {
            { 100, -20,  10,   5,   5,  10, -20, 100 },
            { -20, -30,  -5,  -5,  -5,  -5, -30, -20 },
            {  10,  -5,   1,   1,   1,   1,  -5,  10 },
            {   5,  -5,   1,   1,   1,   1,  -5,   5 },
            {   5,  -5,   1,   1,   1,   1,  -5,   5 },
            {  10,  -5,   1,   1,   1,   1,  -5,  10 },
            { -20, -30,  -5,  -5,  -5,  -5, -30, -20 },
            { 100, -20,  10,   5,   5,  10, -20, 100 }
        };

        score += positionValue[x, y];

        // 取れる駒の数を加算（スタックも考慮）
        if (IsLegalMove(x, y, color, out var flips))
        {
            // ひっくり返す駒の総数（スタックを含む）
            int flipCount = 0;
            foreach (var f in flips)
            {
                flipCount += stackCount[f.x, f.y];
            }
            score += flipCount * 2;
        }

        // スタック手のボーナス/ペナルティ
        if (stackCount[x, y] > 0)
        {
            // 自分の駒の上に置く（スタック手）
            // 角にスタックするとより安全なので高ボーナス
            bool isCorner = (x == 0 || x == 7) && (y == 0 || y == 7);
            if (isCorner)
            {
                score += 50; // 角にスタック = 非常に強固
            }
            else
            {
                // 通常のスタックは少しボーナス（防御的価値）
                score += 10;
            }
        }

        return score;
    }

    // CPU対戦モードかどうかを確認
    public bool IsCPUTurn()
    {
        return gameMode == GameMode.HumanVsCPU && turn == cpuColor;
    }

    /// <summary>
    /// 指定マスのスタック段数を取得
    /// </summary>
    public int GetStackCount(int x, int y)
    {
        if (!InBounds(x, y)) return 0;
        return stackCount[x, y];
    }

    /// <summary>
    /// 指定マスの最上位の駒の色を取得
    /// </summary>
    public DiscColor GetTopColor(int x, int y)
    {
        if (!InBounds(x, y)) return DiscColor.Empty;
        return topColor[x, y];
    }
}
