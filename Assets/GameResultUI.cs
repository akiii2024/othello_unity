using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲーム終了時に勝敗を表示するUI
/// </summary>
public class GameResultUI : MonoBehaviour
{
    [Header("参照")]
    public BoardManager boardManager;
    public Font customFont;

    [Header("表示設定")]
    public float panelPadding = 40f;
    public float buttonWidth = 180f;
    public float buttonHeight = 50f;
    public float buttonSpacing = 20f;

    [Header("色設定")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.8f);
    public Color panelColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    public Color resultTextColor = Color.white;
    public Color blackScoreColor = new Color(0.2f, 0.2f, 0.2f);
    public Color whiteScoreColor = new Color(0.95f, 0.95f, 0.95f);
    public Color buttonColor = new Color(0.2f, 0.5f, 0.8f, 1f);
    public Color buttonTextColor = Color.white;

    Font uiFont;
    Canvas canvas;
    GameObject overlayPanel;
    Text resultText;
    Text scoreText;
    Button retryButton;
    Button titleButton;

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
        HideResult();
    }

    void CreateUI()
    {
        // 専用Canvasを作成
        GameObject canvasGO = GameObject.Find("GameResultCanvas");
        if (canvasGO != null)
        {
            canvas = canvasGO.GetComponent<Canvas>();
        }

        if (canvas == null)
        {
            canvasGO = new GameObject("GameResultCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // 最前面
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // オーバーレイパネル（フルスクリーン半透明背景）
        overlayPanel = new GameObject("OverlayPanel");
        overlayPanel.transform.SetParent(canvas.transform, false);

        var overlayRect = overlayPanel.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;

        var overlayImg = overlayPanel.AddComponent<Image>();
        overlayImg.color = overlayColor;

        // 中央パネル
        var centerPanel = new GameObject("CenterPanel");
        centerPanel.transform.SetParent(overlayPanel.transform, false);

        var centerRect = centerPanel.AddComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerRect.pivot = new Vector2(0.5f, 0.5f);
        centerRect.sizeDelta = new Vector2(400f, 300f);

        var centerImg = centerPanel.AddComponent<Image>();
        centerImg.color = panelColor;

        // 縦方向レイアウト
        var layout = centerPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 15f;
        layout.padding = new RectOffset((int)panelPadding, (int)panelPadding, (int)panelPadding, (int)panelPadding);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // 結果テキスト（勝者表示）
        var resultGO = new GameObject("ResultText");
        resultGO.transform.SetParent(centerPanel.transform, false);

        var resultLE = resultGO.AddComponent<LayoutElement>();
        resultLE.preferredHeight = 60f;

        resultText = resultGO.AddComponent<Text>();
        resultText.text = "ゲーム終了";
        resultText.font = uiFont;
        resultText.fontSize = 36;
        resultText.fontStyle = FontStyle.Bold;
        resultText.alignment = TextAnchor.MiddleCenter;
        resultText.color = resultTextColor;

        // スコアテキスト
        var scoreGO = new GameObject("ScoreText");
        scoreGO.transform.SetParent(centerPanel.transform, false);

        var scoreLE = scoreGO.AddComponent<LayoutElement>();
        scoreLE.preferredHeight = 40f;

        scoreText = scoreGO.AddComponent<Text>();
        scoreText.text = "黒: 0  白: 0";
        scoreText.font = uiFont;
        scoreText.fontSize = 24;
        scoreText.alignment = TextAnchor.MiddleCenter;
        scoreText.color = resultTextColor;

        // ボタンコンテナ
        var buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(centerPanel.transform, false);

        var buttonContainerRect = buttonContainer.AddComponent<RectTransform>();
        var buttonContainerLE = buttonContainer.AddComponent<LayoutElement>();
        buttonContainerLE.preferredHeight = buttonHeight + 20f;

        var buttonLayout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = buttonSpacing;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childControlWidth = false;
        buttonLayout.childControlHeight = false;
        buttonLayout.childForceExpandWidth = false;
        buttonLayout.childForceExpandHeight = false;

        // もう一度プレイボタン
        retryButton = CreateButton(buttonContainer.transform, "RetryButton", "もう一度プレイ");
        retryButton.onClick.AddListener(OnRetryClicked);

        // タイトルに戻るボタン
        titleButton = CreateButton(buttonContainer.transform, "TitleButton", "タイトルに戻る");
        titleButton.onClick.AddListener(OnTitleClicked);
    }

    Button CreateButton(Transform parent, string name, string label)
    {
        var buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);

        var buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(buttonWidth, buttonHeight);

        var buttonImg = buttonGO.AddComponent<Image>();
        buttonImg.color = buttonColor;

        var button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonImg;

        // ボタンラベル
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(buttonGO.transform, false);

        var labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;

        var labelText = labelGO.AddComponent<Text>();
        labelText.text = label;
        labelText.font = uiFont;
        labelText.fontSize = 18;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = buttonTextColor;

        return button;
    }

    /// <summary>
    /// ゲーム結果を表示する
    /// </summary>
    public void ShowResult(int blackCount, int whiteCount)
    {
        if (overlayPanel == null) return;

        overlayPanel.SetActive(true);

        // 結果判定
        string resultMessage;
        if (blackCount > whiteCount)
        {
            resultMessage = "黒の勝ち!";
            resultText.color = new Color(0.3f, 0.3f, 0.3f);
        }
        else if (whiteCount > blackCount)
        {
            resultMessage = "白の勝ち!";
            resultText.color = resultTextColor;
        }
        else
        {
            resultMessage = "引き分け";
            resultText.color = new Color(0.8f, 0.8f, 0.5f);
        }

        resultText.text = resultMessage;
        scoreText.text = $"黒: {blackCount}  白: {whiteCount}";
    }

    /// <summary>
    /// 結果画面を非表示にする
    /// </summary>
    public void HideResult()
    {
        if (overlayPanel != null)
        {
            overlayPanel.SetActive(false);
        }
    }

    void OnRetryClicked()
    {
        HideResult();
        if (boardManager != null)
        {
            boardManager.isGameOver = false;
            boardManager.InitBoard();
        }
    }

    void OnTitleClicked()
    {
        Debug.Log("[GameResultUI] OnTitleClicked called");
        HideResult();
        
        if (boardManager != null)
        {
            boardManager.isGameOver = false;
            boardManager.InitBoard(); // ボードをリセット
        }
        
        // TitleScreenUIを探して表示
        var titleUI = FindFirstObjectByType<TitleScreenUI>();
        if (titleUI != null)
        {
            Debug.Log("[GameResultUI] Found TitleScreenUI, calling ShowMenu");
            titleUI.ShowMenu();
        }
        else
        {
            Debug.Log("[GameResultUI] TitleScreenUI not found, creating new instance");
            // TitleScreenUIを手動で作成
            TitleScreenUI.created = false;
            var go = new GameObject("TitleScreenUI");
            go.AddComponent<TitleScreenUI>();
            // Awake()でメニューが表示され、ゲームが一時停止される
        }
    }
}
