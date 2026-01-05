using System.Collections.Generic;
using UnityEngine;

public class BoardHighlighter : MonoBehaviour
{
    public BoardGrid grid;
    public Material highlightMat;        // 通常の合法手用
    public Material stackHighlightMat;   // スタック可能マス用（別色）
    public Material cursorMat;           // カーソル用マテリアル（nullの場合はhighlightMatを使用）
    public float yOffset = 0.01f;        // 盤面より少し上（Z-fight防止）

    GameObject[,] tiles;
    MeshRenderer[,] tileRenderers; // マテリアル切り替え用
    GameObject cursorTile; // カーソル表示用
    GameObject[] boundaryLines; // 境界線表示用
    int currentCursorX = -1;
    int currentCursorY = -1;
    int boardSize = 8;

    void Awake()
    {
        // スタック用マテリアルがない場合は動的に作成（オレンジ色）
        if (stackHighlightMat == null && highlightMat != null)
        {
            stackHighlightMat = new Material(highlightMat);
            stackHighlightMat.color = new Color(1f, 0.6f, 0.2f, 0.5f); // オレンジ
        }

        CreateTiles();
    }

    /// <summary>
    /// ボードサイズ変更時にタイルを再構築
    /// </summary>
    public void RebuildTiles()
    {
        // 既存タイルを削除
        if (tiles != null)
        {
            for (int y = 0; y < tiles.GetLength(1); y++)
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                if (tiles[x, y] != null)
                {
                    Destroy(tiles[x, y]);
                }
            }
        }
        if (cursorTile != null)
        {
            Destroy(cursorTile);
        }
        
        // 境界線を削除
        if (boundaryLines != null)
        {
            foreach (var line in boundaryLines)
            {
                if (line != null) Destroy(line);
            }
        }

        CreateTiles();
    }

    void CreateTiles()
    {
        // グリッドからサイズを取得
        boardSize = grid != null ? grid.size : 8;
        
        // 配列を動的に割り当て
        tiles = new GameObject[boardSize, boardSize];
        tileRenderers = new MeshRenderer[boardSize, boardSize];

        // ボードサイズに応じたQuadを生成
        for (int y = 0; y < boardSize; y++)
        for (int x = 0; x < boardSize; x++)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = $"HL_{x}_{y}";
            quad.transform.SetParent(transform, worldPositionStays:false);

            // Collider不要
            Destroy(quad.GetComponent<Collider>());

            // セル中心に配置（少し浮かす）
            Vector3 p = grid.CellToWorld(x, y);
            quad.transform.position = new Vector3(p.x, p.y + yOffset, p.z);

            // 盤に寝かせる（Quadは初期でカメラ向きなので）
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // 1マス=1なのでサイズ1でOK（気持ち小さくして隙間作る）
            quad.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

            var r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = highlightMat;
            tileRenderers[x, y] = r; // レンダラー参照を保存

            quad.SetActive(false);
            tiles[x,y] = quad;
        }

        // カーソル用のQuadを生成（ハイライトより少し上に表示）
        cursorTile = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cursorTile.name = "Cursor";
        cursorTile.transform.SetParent(transform, worldPositionStays:false);
        Destroy(cursorTile.GetComponent<Collider>());
        cursorTile.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        cursorTile.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        
        var cursorRend = cursorTile.GetComponent<MeshRenderer>();
        cursorRend.sharedMaterial = cursorMat != null ? cursorMat : highlightMat;
        
        cursorTile.SetActive(false);
        
        // 境界線を作成（盤面のプレイ可能エリアを示す）
        CreateBoundaryLines();
    }
    
    void CreateBoundaryLines()
    {
        boundaryLines = new GameObject[4]; // 上、下、左、右
        
        // 境界位置を計算
        float half = boardSize * grid.cellSize * 0.5f;
        float y = yOffset + 0.005f;
        float lineWidth = 0.08f;
        
        // 四辺の線を作成
        Color boundaryColor = new Color(1f, 0.8f, 0.2f, 0.9f); // 黄色
        
        // 上辺
        boundaryLines[0] = CreateBoundaryQuad("Boundary_Top", 
            new Vector3(0, y, half), 
            new Vector3(boardSize * grid.cellSize + lineWidth, lineWidth, 1f),
            boundaryColor);
        
        // 下辺
        boundaryLines[1] = CreateBoundaryQuad("Boundary_Bottom", 
            new Vector3(0, y, -half), 
            new Vector3(boardSize * grid.cellSize + lineWidth, lineWidth, 1f),
            boundaryColor);
        
        // 左辺
        boundaryLines[2] = CreateBoundaryQuad("Boundary_Left", 
            new Vector3(-half, y, 0), 
            new Vector3(lineWidth, boardSize * grid.cellSize + lineWidth, 1f),
            boundaryColor);
        
        // 右辺
        boundaryLines[3] = CreateBoundaryQuad("Boundary_Right", 
            new Vector3(half, y, 0), 
            new Vector3(lineWidth, boardSize * grid.cellSize + lineWidth, 1f),
            boundaryColor);
    }
    
    GameObject CreateBoundaryQuad(string name, Vector3 localPos, Vector3 scale, Color color)
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        quad.transform.SetParent(grid.transform, worldPositionStays:false);
        Destroy(quad.GetComponent<Collider>());
        
        quad.transform.localPosition = localPos;
        quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localScale = scale;
        
        var rend = quad.GetComponent<MeshRenderer>();
        rend.material = new Material(Shader.Find("Sprites/Default"));
        rend.material.color = color;
        
        return quad;
    }

    public void Clear()
    {
        if (tiles == null) return;
        for (int y = 0; y < boardSize; y++)
        for (int x = 0; x < boardSize; x++)
            if (tiles[x,y] != null) tiles[x,y].SetActive(false);
    }

    /// <summary>
    /// 合法手を表示（通常手とスタック手を区別して表示）
    /// </summary>
    public void ShowMoves(List<(int x, int y)> normalMoves, List<(int x, int y)> stackMoves)
    {
        Clear();
        
        // 通常の合法手（空きマスへの配置）
        foreach (var m in normalMoves)
        {
            tileRenderers[m.x, m.y].sharedMaterial = highlightMat;
            tiles[m.x, m.y].SetActive(true);
        }
        
        // スタック可能手（自分の駒の上への配置）
        Material stackMat = stackHighlightMat != null ? stackHighlightMat : highlightMat;
        foreach (var m in stackMoves)
        {
            tileRenderers[m.x, m.y].sharedMaterial = stackMat;
            tiles[m.x, m.y].SetActive(true);
        }
    }

    /// <summary>
    /// 後方互換性のための旧メソッド
    /// </summary>
    public void ShowMoves(List<(int x,int y)> moves)
    {
        Clear();
        foreach (var m in moves)
        {
            tileRenderers[m.x, m.y].sharedMaterial = highlightMat;
            tiles[m.x, m.y].SetActive(true);
        }
    }

    public void ShowCursor(int x, int y)
    {
        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize) return;

        currentCursorX = x;
        currentCursorY = y;

        Vector3 p = grid.CellToWorld(x, y);
        cursorTile.transform.position = new Vector3(p.x, p.y + yOffset + 0.001f, p.z); // ハイライトより少し上
        cursorTile.SetActive(true);
    }

    public void HideCursor()
    {
        if (cursorTile != null)
        {
            cursorTile.SetActive(false);
        }
        currentCursorX = -1;
        currentCursorY = -1;
    }
}
