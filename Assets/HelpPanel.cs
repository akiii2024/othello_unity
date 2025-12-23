using UnityEngine;
using UnityEngine.UI;

public class HelpPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject helpPanel; // ヘルプパネル（Canvas内のPanel）
    public Text helpText; // ヘルプテキスト

    bool isVisible = false;

    // 外部からアクセス可能にする
    public bool IsVisible => isVisible;

    void Start()
    {
        // 初期状態では非表示
        if (helpPanel != null)
        {
            helpPanel.SetActive(false);
        }
        
        // ヘルプテキストを設定
        if (helpText != null)
        {
            helpText.text = GetHelpText();
        }
    }

    // 入力処理はBoardClickPlaceで行うため、Updateは不要

    public void ToggleHelp()
    {
        isVisible = !isVisible;
        if (helpPanel != null)
        {
            helpPanel.SetActive(isVisible);
        }
    }

    public void ShowHelp()
    {
        isVisible = true;
        if (helpPanel != null)
        {
            helpPanel.SetActive(true);
        }
    }

    public void HideHelp()
    {
        isVisible = false;
        if (helpPanel != null)
        {
            helpPanel.SetActive(false);
        }
    }

    string GetHelpText()
    {
        return @"オセロ - 操作方法

【マウス操作】
・左クリック: 駒を置く

【キーボード操作】
・矢印キー / WASD: カーソル移動
・Enter / Space: 駒を置く
・H: ヘルプ表示/非表示

【ゲームルール】
・黒と白が交互に駒を置きます
・相手の駒を挟むと自分の色に変わります
・置ける場所は緑色でハイライト表示されます
・置ける場所がない場合はパスになります
・両方とも置けなくなったらゲーム終了です

Hキーでこのヘルプを閉じます";
    }
}

