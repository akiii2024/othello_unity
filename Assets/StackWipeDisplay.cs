using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 各段（1段目・2段目）の盤面状況を画面角にワイプ表示するUI
/// </summary>
public class StackWipeDisplay : MonoBehaviour
{
    [Header("参照")]
    public BoardManager boardManager;
    public Font customFont; // カスタムフォント（オプション）

    [Header("表示設定")]
    public float wipeSize = 150f;           // ワイプのサイズ（ピクセル）
    public float margin = 20f;              // 画面端からのマージン
    public float cellPadding = 2f;          // セル間のパディング
    
    [Header("色設定")]
    public Color blackColor = Color.black;
    public Color whiteColor = Color.white;
    public Color emptyColor = new Color(0.2f, 0.5f, 0.2f, 1f); // 緑（盤面色）
    public Color borderColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    public Color labelColor = Color.white;
    public Color panelBgColor = new Color(0f, 0f, 0f, 0.7f);
    
    Font uiFont;

    // 内部変数
    Canvas canvas;
    RectTransform[] wipePanels = new RectTransform[2]; // 0:1段目, 1:2段目
    Image[,] layer1Cells = new Image[8, 8];
    Image[,] layer2Cells = new Image[8, 8];
    Text[] layerLabels = new Text[2];
    Text[] countLabels = new Text[2]; // 各段の駒数表示

    void Start()
    {
        CreateUI();
    }

    void Update()
    {
        UpdateDisplay();
    }

    void CreateUI()
    {
        // フォントの設定
        if (customFont != null)
        {
            uiFont = customFont;
        }
        else
        {
            // Unityの組み込みフォントを取得
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        // Canvas作成（既存があれば使用）
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasGO = new GameObject("StackWipeCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // 2つのワイプパネルを作成（左下と右下）
        CreateWipePanel(0, new Vector2(margin, margin), "1段目", layer1Cells);
        CreateWipePanel(1, new Vector2(Screen.width - wipeSize - margin, margin), "2段目", layer2Cells);
    }

    void CreateWipePanel(int index, Vector2 position, string labelText, Image[,] cells)
    {
        // パネル作成
        var panelGO = new GameObject($"WipePanel_{index}");
        panelGO.transform.SetParent(canvas.transform, false);
        
        var panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.zero;
        panelRect.pivot = Vector2.zero;
        panelRect.anchoredPosition = position;
        panelRect.sizeDelta = new Vector2(wipeSize, wipeSize + 40f); // ラベル分の高さを追加
        
        // 背景
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = panelBgColor;
        
        wipePanels[index] = panelRect;

        // ラベル作成
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(panelGO.transform, false);
        
        var labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 1);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.pivot = new Vector2(0.5f, 1);
        labelRect.anchoredPosition = new Vector2(0, 0);
        labelRect.sizeDelta = new Vector2(0, 20f);
        
        var labelTextComp = labelGO.AddComponent<Text>();
        labelTextComp.text = labelText;
        labelTextComp.font = uiFont;
        labelTextComp.fontSize = 14;
        labelTextComp.alignment = TextAnchor.MiddleCenter;
        labelTextComp.color = labelColor;
        layerLabels[index] = labelTextComp;

        // カウントラベル作成
        var countGO = new GameObject("Count");
        countGO.transform.SetParent(panelGO.transform, false);
        
        var countRect = countGO.AddComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0, 1);
        countRect.anchorMax = new Vector2(1, 1);
        countRect.pivot = new Vector2(0.5f, 1);
        countRect.anchoredPosition = new Vector2(0, -20);
        countRect.sizeDelta = new Vector2(0, 18f);
        
        var countTextComp = countGO.AddComponent<Text>();
        countTextComp.text = "B:0 W:0";
        countTextComp.font = uiFont;
        countTextComp.fontSize = 12;
        countTextComp.alignment = TextAnchor.MiddleCenter;
        countTextComp.color = labelColor;
        countTextComp.supportRichText = true; // リッチテキスト有効化
        countLabels[index] = countTextComp;

        // グリッドコンテナ作成
        var gridGO = new GameObject("Grid");
        gridGO.transform.SetParent(panelGO.transform, false);
        
        var gridRect = gridGO.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0, 0);
        gridRect.anchorMax = new Vector2(1, 1);
        gridRect.offsetMin = new Vector2(cellPadding, cellPadding);
        gridRect.offsetMax = new Vector2(-cellPadding, -40f); // ラベル分のオフセット

        // 8x8のセルを作成
        float gridSize = wipeSize - cellPadding * 2;
        float cellSize = (gridSize - cellPadding * 7) / 8f;
        
        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++)
        {
            var cellGO = new GameObject($"Cell_{x}_{y}");
            cellGO.transform.SetParent(gridGO.transform, false);
            
            var cellRect = cellGO.AddComponent<RectTransform>();
            cellRect.anchorMin = Vector2.zero;
            cellRect.anchorMax = Vector2.zero;
            cellRect.pivot = Vector2.zero;
            
            float posX = x * (cellSize + cellPadding);
            float posY = (7 - y) * (cellSize + cellPadding); // Y軸反転
            cellRect.anchoredPosition = new Vector2(posX, posY);
            cellRect.sizeDelta = new Vector2(cellSize, cellSize);
            
            var cellImg = cellGO.AddComponent<Image>();
            cellImg.color = emptyColor;
            
            cells[x, y] = cellImg;
        }
    }

    void UpdateDisplay()
    {
        if (boardManager == null) return;

        int black1 = 0, white1 = 0; // 1段目のカウント
        int black2 = 0, white2 = 0; // 2段目のカウント

        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++)
        {
            int count = boardManager.GetStackCount(x, y);
            DiscColor color = boardManager.GetTopColor(x, y);

            // 1段目の表示
            if (count >= 1)
            {
                layer1Cells[x, y].color = (color == DiscColor.Black) ? blackColor : whiteColor;
                if (color == DiscColor.Black) black1++;
                else if (color == DiscColor.White) white1++;
            }
            else
            {
                layer1Cells[x, y].color = emptyColor;
            }

            // 2段目の表示
            if (count >= 2)
            {
                layer2Cells[x, y].color = (color == DiscColor.Black) ? blackColor : whiteColor;
                if (color == DiscColor.Black) black2++;
                else if (color == DiscColor.White) white2++;
            }
            else
            {
                layer2Cells[x, y].color = emptyColor;
            }
        }

        // カウント表示更新
        if (countLabels[0] != null)
        {
            countLabels[0].text = $"<color=#333333>●</color>{black1}  <color=#ffffff>○</color>{white1}";
        }
        if (countLabels[1] != null)
        {
            countLabels[1].text = $"<color=#333333>●</color>{black2}  <color=#ffffff>○</color>{white2}";
        }
    }

    // Inspector から BoardManager をアタッチできない場合の自動検索
    void Awake()
    {
        if (boardManager == null)
        {
            boardManager = FindObjectOfType<BoardManager>();
        }
    }
}

