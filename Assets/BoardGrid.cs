using UnityEngine;

public class BoardGrid : MonoBehaviour
{
    public int size = 8;
    public float cellSize = 1f; // 8x8で行くなら基本1でOK

    // セル中心(ローカル) → ワールド
    public Vector3 CellToWorld(int x, int y)
    {
        float half = size * cellSize * 0.5f;
        float wx = -half + (x + 0.5f) * cellSize;
        float wz = -half + (y + 0.5f) * cellSize;

        // 盤上面がY=0なら y=0 固定でOK（あなたの設定だとこれが気持ちいい）
        return transform.TransformPoint(new Vector3(wx, 0f, wz));
    }

    // ワールド座標 → セル（盤外なら false）
    public bool TryWorldToCell(Vector3 world, out int x, out int y)
    {
        Vector3 local = transform.InverseTransformPoint(world);

        float half = size * cellSize * 0.5f;
        float fx = (local.x + half) / cellSize; // 0..8
        float fy = (local.z + half) / cellSize;

        x = Mathf.FloorToInt(fx);
        y = Mathf.FloorToInt(fy);

        return (0 <= x && x < size && 0 <= y && y < size);
    }

    // デバッグ用：セル中心をSceneに描く
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Gizmos.DrawSphere(CellToWorld(x, y), 0.05f);
        }
    }
}
