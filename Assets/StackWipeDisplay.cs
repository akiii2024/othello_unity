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
    public float verticalOffset = 0f;       // 上下オフセット（中央基準）
    public bool useCustomPositions = false; // 個別座標を使用するか
    public Vector3 layer1Position = new Vector3(20f, 0f, 0f);   // 1段目パネルのx,y,z（Screen Space Overlayでもzを保持）
    public Vector3 layer2Position = new Vector3(-20f, 0f, 0f);  // 2段目パネルのx,y,z
    
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
    Image[,] layer1Cells;
    Image[,] layer2Cells;
    Text[] layerLabels = new Text[2];
    Text[] countLabels = new Text[2]; // 各段の駒数表示
    int boardSize = 8;

    void Start()
    {
        // BoardManagerからボードサイズを取得
        if (boardManager != null)
        {
            boardSize = boardManager.boardSize;
        }
        
        // RebuildUIを使用して初期構築（BoardManagerからも呼ばれるが、重複は RebuildUI 内で処理される）
        RebuildUI();
    }

    /// <summary>
    /// ボードサイズ変更時にワイプUIを再構築
    /// </summary>
    public void RebuildUI()
    {
        // BoardManagerからボードサイズを取得
        if (boardManager != null)
        {
            boardSize = boardManager.boardSize;
        }
        
        // 既存のパネルを削除
        for (int i = 0; i < wipePanels.Length; i++)
        {
            if (wipePanels[i] != null)
            {
                Destroy(wipePanels[i].gameObject);
                wipePanels[i] = null;
            }
        }
        
        // 配列を再割り当て
        layer1Cells = new Image[boardSize, boardSize];
        layer2Cells = new Image[boardSize, boardSize];
        layerLabels = new Text[2];
        countLabels = new Text[2];
        
        // フォントが未設定の場合は初期化
        if (uiFont == null)
        {
            if (customFont != null)
            {
                uiFont = customFont;
            }
            else
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (uiFont == null)
                {
                    uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }
        }
        
        // Canvasが存在しない場合は再作成
        if (canvas == null)
        {
            GameObject canvasGO = GameObject.Find("StackWipeCanvas");
            if (canvasGO != null)
            {
                canvas = canvasGO.GetComponent<Canvas>();
            }
            
            if (canvas == null)
            {
                canvasGO = new GameObject("StackWipeCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                canvasGO.SetActive(true);
            }
        }
        
        // 2つのワイプパネルを作成（左中央／右中央）
        CreateWipePanel(0, true, "1段目", layer1Cells);
        CreateWipePanel(1, false, "2段目", layer2Cells);
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
            // Resourcesフォルダから日本語フォントを読み込む（優先順位順）
            uiFont = Resources.Load<Font>("NotoSansJP") ?? 
                    Resources.Load<Font>("Meiryo") ?? 
                    Resources.Load<Font>("YuGothic") ??
                    Resources.Load<Font>("MS Gothic") ??
                    Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            if (uiFont == null)
            {
                Debug.LogWarning("[StackWipeDisplay] 日本語フォントが見つかりません。Resourcesフォルダに日本語フォント（NotoSansJP、Meiryo等）を追加してください。");
                uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        // 専用Canvasを作成（既存のStackWipeCanvasを探して再利用、なければ作成）
        GameObject canvasGO = GameObject.Find("StackWipeCanvas");
        if (canvasGO != null)
        {
            canvas = canvasGO.GetComponent<Canvas>();
        }
        
        if (canvas == null)
        {
            // 既存のStackWipeCanvasがない場合、新規作成
            canvasGO = new GameObject("StackWipeCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            // Canvasは常にアクティブに保つ
            canvasGO.SetActive(true);
        }
        else
        {
            // 既存のCanvasがある場合、アクティブに保つ
            canvasGO.SetActive(true);
        }

        // 2つのワイプパネルを作成（左中央／右中央）
        CreateWipePanel(0, true, "1段目", layer1Cells);
        CreateWipePanel(1, false, "2段目", layer2Cells);
    }

    void CreateWipePanel(int index, bool isLeft, string labelText, Image[,] cells)
    {
        // パネル作成
        var panelGO = new GameObject($"WipePanel_{index}");
        panelGO.transform.SetParent(canvas.transform, false);
        
        var panelRect = panelGO.AddComponent<RectTransform>();
        // 画面左右中央に固定
        Vector2 anchoredPos;
        float posZ = 0f;

        if (useCustomPositions)
        {
            // 個別指定座標を利用
            var p = isLeft ? layer1Position : layer2Position;
            anchoredPos = new Vector2(p.x, p.y);
            posZ = p.z;
        }
        else
        {
            // デフォルト: 左右中央＋マージン／垂直オフセット
            if (isLeft)
            {
                anchoredPos = new Vector2(margin, verticalOffset);
                panelRect.anchorMin = new Vector2(0f, 0.5f);
                panelRect.anchorMax = new Vector2(0f, 0.5f);
                panelRect.pivot = new Vector2(0f, 0.5f);
            }
            else
            {
                anchoredPos = new Vector2(-margin, verticalOffset);
                panelRect.anchorMin = new Vector2(1f, 0.5f);
                panelRect.anchorMax = new Vector2(1f, 0.5f);
                panelRect.pivot = new Vector2(1f, 0.5f);
            }
        }

        // 位置適用（Overlayでもzは保持されますが見た目にはほぼ影響なし）
        panelRect.anchoredPosition = anchoredPos;
        panelRect.localPosition = new Vector3(anchoredPos.x, anchoredPos.y, posZ);
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

        // ボードサイズに応じたセルを作成
        float gridSize = wipeSize - cellPadding * 2;
        float cellSize = (gridSize - cellPadding * (boardSize - 1)) / (float)boardSize;
        
        for (int y = 0; y < boardSize; y++)
        for (int x = 0; x < boardSize; x++)
        {
            var cellGO = new GameObject($"Cell_{x}_{y}");
            cellGO.transform.SetParent(gridGO.transform, false);
            
            var cellRect = cellGO.AddComponent<RectTransform>();
            cellRect.anchorMin = Vector2.zero;
            cellRect.anchorMax = Vector2.zero;
            cellRect.pivot = Vector2.zero;
            
            // x軸方向も反転させて左右を入れ替える
            float posX = (boardSize - 1 - x) * (cellSize + cellPadding);
            float posY = (boardSize - 1 - y) * (cellSize + cellPadding); // Y軸反転
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

        for (int y = 0; y < boardSize; y++)
        for (int x = 0; x < boardSize; x++)
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

        // デバッグ: 各セルの状態をログ出力（毎フレームは多すぎるのでキー押下時のみ）
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("=== Wipe Display State ===");
            for (int dy = 0; dy < boardSize; dy++)
            for (int dx = 0; dx < boardSize; dx++)
            {
                int c = boardManager.GetStackCount(dx, dy);
                if (c > 0)
                {
                    DiscColor col = boardManager.GetTopColor(dx, dy);
                    Debug.Log($"  ({dx},{dy}): count={c}, topColor={col}");
                }
            }
            Debug.Log($"Layer1: Black={black1}, White={white1}");
            Debug.Log($"Layer2: Black={black2}, White={white2}");
        }
    }

    // Inspector から BoardManager をアタッチできない場合の自動検索
    void Awake()
    {
        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
        }
    }
}

