using System.Collections;
using UnityEngine;

public enum DiscColor { Empty = 0, Black = 1, White = 2 }

public class PieceView : MonoBehaviour
{
    public DiscColor Color { get; private set; } = DiscColor.Empty;

    [Header("Materials")]
    public Material blackMat;
    public Material whiteMat;

    [Header("Flip Animation")]
    public float flipHeight = 0.3f;  // ひっくり返すときの上昇高さ
    public float flipDuration = 0.6f;  // アニメーションの総時間
    
    [Header("位置維持")]
    public float snapBackThreshold = 0.02f; // 正規位置からの許容距離
    Vector3 homePosition;
    bool hasHomePosition;

    Renderer rend;

    void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        if (rend == null) rend = GetComponent<Renderer>();
    }

    public void SetHomePosition(Vector3 pos)
    {
        homePosition = pos;
        hasHomePosition = true;
    }

    public void SetColorImmediate(DiscColor c)
    {
        Color = c;
        ApplyMaterial();
    }

    public void FlipTo(DiscColor toColor, float duration = -1f)
    {
        StopAllCoroutines();
        // durationが-1の場合はデフォルト値を使用
        if (duration < 0f) duration = flipDuration;
        StartCoroutine(FlipRoutine(toColor, duration));
    }

    IEnumerator FlipRoutine(DiscColor toColor, float duration)
    {
        // Rigidbodyを一時的に無効化して位置を直接制御
        Rigidbody rb = GetComponent<Rigidbody>();
        bool wasKinematic = false;
        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
        }

        // 初期状態を保存
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Vector3 targetPos = hasHomePosition ? homePosition : startPos;
        Vector3 topPos = startPos + Vector3.up * flipHeight;
        Quaternion midRot = startRot * Quaternion.Euler(0f, 90f, 0f);
        Quaternion endRot = startRot * Quaternion.Euler(0f, 180f, 0f);

        // アニメーションの時間配分
        float riseTime = duration * 0.3f;      // 上昇時間（30%）
        float flipTime = duration * 0.4f;       // 回転時間（40%）
        float fallTime = duration * 0.3f;       // 下降時間（30%）

        // フェーズ1: 上昇
        float t = 0f;
        while (t < riseTime)
        {
            t += Time.deltaTime;
            float progress = t / riseTime;
            transform.position = Vector3.Lerp(startPos, topPos, progress);
            yield return null;
        }
        transform.position = topPos;

        // フェーズ2: 回転しながら色変更
        t = 0f;
        while (t < flipTime)
        {
            t += Time.deltaTime;
            float progress = t / flipTime;
            
            // 回転
            if (progress < 0.5f)
            {
                // 前半：0度から90度
                transform.rotation = Quaternion.Slerp(startRot, midRot, progress * 2f);
            }
            else
            {
                // 後半：90度から180度（色変更は中間点で）
                if (progress >= 0.5f && Color != toColor)
                {
                    Color = toColor;
                    ApplyMaterial();
                }
                transform.rotation = Quaternion.Slerp(midRot, endRot, (progress - 0.5f) * 2f);
            }
            yield return null;
        }

        // 色がまだ変更されていない場合は変更
        if (Color != toColor)
        {
            Color = toColor;
            ApplyMaterial();
        }
        transform.rotation = endRot;

        // フェーズ3: 下降
        t = 0f;
        while (t < fallTime)
        {
            t += Time.deltaTime;
            float progress = t / fallTime;
            transform.position = Vector3.Lerp(topPos, startPos, progress);
            yield return null;
        }
        transform.position = startPos;

        // Rigidbodyの状態を復元
        if (rb != null)
        {
            // 正規位置からずれていればスナップ
            if (hasHomePosition && Vector3.Distance(transform.position, targetPos) > snapBackThreshold)
            {
                transform.position = targetPos;
            }
            FreezeRigidbody(rb);
        }
    }

    void ApplyMaterial()
    {
        if (rend == null) return;
        rend.sharedMaterial = (Color == DiscColor.Black) ? blackMat : whiteMat;
    }

    void FreezeRigidbody(Rigidbody rb)
    {
        if (rb == null) return;
        bool wasKinematic = rb.isKinematic;
        if (wasKinematic) rb.isKinematic = false; // 速度リセット時の警告回避
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        rb.isKinematic = true;
    }
}
