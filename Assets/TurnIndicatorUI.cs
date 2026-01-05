using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 現在どちらのターンかを画面上部に表示するUI
/// </summary>
public class TurnIndicatorUI : MonoBehaviour
{
    [Header("参照")]
    public BoardManager boardManager;
    public Font customFont;

    [Header("表示設定")]
    public float panelWidth = 200f;
    public float panelHeight = 50f;
    public float topMargin = 20f;
    public float iconSize = 30f;

    [Header("色設定")]
    public Color blackColor = Color.black;
    public Color whiteColor = Color.white;
    public Color panelBgColor = new Color(0f, 0f, 0f, 0.7f);
    public Color labelColor = Color.white;

    Font uiFont;
    Canvas canvas;
    RectTransform panelRect;
    Text turnLabel;
    Image turnIcon;

    void Awake()
    {
        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
        }
    }

    void Start()
    {
        // フォントの設定
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

        CreateUI();
    }

    void CreateUI()
    {
        // 専用Canvasを作成（既存のTurnIndicatorCanvasを探して再利用、なければ作成）
        GameObject canvasGO = GameObject.Find("TurnIndicatorCanvas");
        if (canvasGO != null)
        {
            canvas = canvasGO.GetComponent<Canvas>();
        }

        if (canvas == null)
        {
            canvasGO = new GameObject("TurnIndicatorCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110; // StackWipeDisplayより前面
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.SetActive(true);
        }
        else
        {
            canvasGO.SetActive(true);
        }

        // パネル作成
        var panelGO = new GameObject("TurnPanel");
        panelGO.transform.SetParent(canvas.transform, false);

        panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0, -topMargin);
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = panelBgColor;

        // レイアウト用のHorizontalLayoutGroupを追加
        var layout = panelGO.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.padding = new RectOffset(15, 15, 5, 5);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // ターンアイコン（駒の色を示す丸）
        var iconGO = new GameObject("TurnIcon");
        iconGO.transform.SetParent(panelGO.transform, false);

        var iconRect = iconGO.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);

        turnIcon = iconGO.AddComponent<Image>();
        turnIcon.color = blackColor;

        // アイコンを丸くする（Spriteがない場合はそのまま四角）
        // 丸いスプライトがないため、円形マスクの代わりに見た目を工夫

        // ターンラベル
        var labelGO = new GameObject("TurnLabel");
        labelGO.transform.SetParent(panelGO.transform, false);

        var labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(120f, panelHeight - 10f);

        turnLabel = labelGO.AddComponent<Text>();
        turnLabel.text = "黒の番";
        turnLabel.font = uiFont;
        turnLabel.fontSize = 20;
        turnLabel.alignment = TextAnchor.MiddleLeft;
        turnLabel.color = labelColor;
    }

    void Update()
    {
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (boardManager == null) return;

        DiscColor currentTurn = boardManager.turn;

        if (currentTurn == DiscColor.Black)
        {
            turnIcon.color = blackColor;
            turnLabel.text = "黒の番";
        }
        else
        {
            turnIcon.color = whiteColor;
            turnLabel.text = "白の番";
        }

        // CPU対戦時は「CPU思考中」を表示
        if (boardManager.gameMode == GameMode.HumanVsCPU && boardManager.IsCPUTurn())
        {
            turnLabel.text = currentTurn == DiscColor.Black ? "黒(CPU)思考中..." : "白(CPU)思考中...";
        }
    }
}
