using System.Collections.Generic;
using UnityEngine;

public class BoardHighlighter : MonoBehaviour
{
    public BoardGrid grid;
    public Material highlightMat;        // 通常の合法手用
    public Material stackHighlightMat;   // スタック可能マス用（別色）
    public Material cursorMat;           // カーソル用マテリアル（nullの場合はhighlightMatを使用）
    public float yOffset = 0.01f;        // 盤面より少し上（Z-fight防止）

    GameObject[,] tiles = new GameObject[8,8];
    MeshRenderer[,] tileRenderers = new MeshRenderer[8,8]; // マテリアル切り替え用
    GameObject cursorTile; // カーソル表示用
    int currentCursorX = -1;
    int currentCursorY = -1;

    void Awake()
    {
        // スタック用マテリアルがない場合は動的に作成（オレンジ色）
        if (stackHighlightMat == null && highlightMat != null)
        {
            stackHighlightMat = new Material(highlightMat);
            stackHighlightMat.color = new Color(1f, 0.6f, 0.2f, 0.5f); // オレンジ
        }

        // 8x8のQuadを生成
        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++)
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
    }

    public void Clear()
    {
        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++)
            tiles[x,y].SetActive(false);
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
        if (x < 0 || x >= 8 || y < 0 || y >= 8) return;

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
