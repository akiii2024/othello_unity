using UnityEngine;
using UnityEngine.UI;

public class HelpPanel : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] Color panelColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
    [SerializeField] Color headerColor = new Color(0.2f, 0.5f, 0.3f, 1f);
    [SerializeField] Color buttonColor = new Color(0.3f, 0.3f, 0.35f, 1f);
    [SerializeField] Color buttonHoverColor = new Color(0.4f, 0.4f, 0.45f, 1f);

    [Header("Legacy UI (自動非表示)")]
    public GameObject helpPanel; // シーン内の古いパネル（自動で非表示になります）
    public Text helpText; // シーン内の古いテキスト（自動で非表示になります）

    Canvas canvas;
    GameObject panelRoot;
    Text contentText;
    Text pageIndicatorText;
    Text headerText;
    ScrollRect scrollRect;
    RectTransform contentRect;

    int currentPage = 0;
    const int totalPages = 2;
    bool isVisible = false;

    // 外部からアクセス可能にする
    public bool IsVisible => isVisible;

    void Start()
    {
        // シーン内の古いUIを非表示にする
        if (helpPanel != null)
        {
            helpPanel.SetActive(false);
        }

        BuildHelpUI();
        HideHelp();
    }

    void Update()
    {
        if (!isVisible) return;

        // ページ切り替え（左右キー）
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            PreviousPage();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            NextPage();
        }

        // 閉じる（Escape のみ - HキーはBoardClickPlaceで処理）
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HideHelp();
        }
    }

    void BuildHelpUI()
    {
        // Canvas作成
        var canvasGO = new GameObject("HelpCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000; // タイトル画面より前面

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // 背景（半透明オーバーレイ）
        var backdrop = new GameObject("Backdrop", typeof(Image));
        var backdropRect = backdrop.GetComponent<RectTransform>();
        backdropRect.SetParent(canvas.transform, false);
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        // メインパネル
        panelRoot = new GameObject("HelpPanel", typeof(Image));
        var panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.SetParent(canvas.transform, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(700f, 550f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRoot.GetComponent<Image>().color = panelColor;

        // ヘッダー
        var header = new GameObject("Header", typeof(Image));
        var headerRect = header.GetComponent<RectTransform>();
        headerRect.SetParent(panelRoot.transform, false);
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, 60f);
        headerRect.anchoredPosition = Vector2.zero;
        header.GetComponent<Image>().color = headerColor;

        // ヘッダーテキスト
        headerText = CreateText(header.transform, "操作方法", 28, FontStyle.Bold);
        var headerTextRect = headerText.GetComponent<RectTransform>();
        headerTextRect.anchorMin = Vector2.zero;
        headerTextRect.anchorMax = Vector2.one;
        headerTextRect.offsetMin = Vector2.zero;
        headerTextRect.offsetMax = Vector2.zero;

        // コンテンツエリア（スクロールビュー）
        var scrollViewArea = new GameObject("ScrollViewArea", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        var scrollViewRect = scrollViewArea.GetComponent<RectTransform>();
        scrollViewRect.SetParent(panelRoot.transform, false);
        scrollViewRect.anchorMin = new Vector2(0f, 0f);
        scrollViewRect.anchorMax = new Vector2(1f, 1f);
        scrollViewRect.offsetMin = new Vector2(30f, 80f);
        scrollViewRect.offsetMax = new Vector2(-30f, -70f);

        // Maskの設定（Imageが必要）
        var maskImage = scrollViewArea.GetComponent<Image>();
        maskImage.color = new Color(0f, 0f, 0f, 0.01f); // ほぼ透明（完全に0だとMaskが機能しないことがある）
        var mask = scrollViewArea.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        // スクロールコンテンツ（実際のテキストを含む）
        var contentArea = new GameObject("Content", typeof(RectTransform));
        contentRect = contentArea.GetComponent<RectTransform>();
        contentRect.SetParent(scrollViewArea.transform, false);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        // 幅は親に合わせ、高さは後で動的に設定
        contentRect.sizeDelta = new Vector2(0f, 600f);

        // ScrollRectの設定
        scrollRect = scrollViewArea.GetComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20f;
        scrollRect.viewport = scrollViewRect;

        // コンテンツテキスト
        contentText = CreateText(contentArea.transform, "", 20, FontStyle.Normal);
        var contentTextRect = contentText.GetComponent<RectTransform>();
        contentTextRect.anchorMin = new Vector2(0f, 0f);
        contentTextRect.anchorMax = new Vector2(1f, 1f);
        contentTextRect.pivot = new Vector2(0.5f, 0.5f);
        contentTextRect.offsetMin = Vector2.zero;
        contentTextRect.offsetMax = Vector2.zero;
        contentText.alignment = TextAnchor.UpperLeft;
        contentText.lineSpacing = 1.3f;

        // フッターエリア（ナビゲーション）
        var footer = new GameObject("Footer", typeof(RectTransform));
        var footerRect = footer.GetComponent<RectTransform>();
        footerRect.SetParent(panelRoot.transform, false);
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(0.5f, 0f);
        footerRect.sizeDelta = new Vector2(0f, 70f);
        footerRect.anchoredPosition = Vector2.zero;

        // 次へボタン
        CreateNavButton(footer.transform, "次へ ▶", new Vector2(120f, 20f), () => NextPage());

        // ページインジケーター
        pageIndicatorText = CreateText(footer.transform, "1 / 2", 18, FontStyle.Normal);
        var indicatorRect = pageIndicatorText.GetComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
        indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
        indicatorRect.sizeDelta = new Vector2(100f, 30f);
        indicatorRect.anchoredPosition = new Vector2(0f, 20f);

        // 前へボタン
        CreateNavButton(footer.transform, "◀ 前へ", new Vector2(-120f, 20f), () => PreviousPage());

        // 閉じるヒント
        var closeHint = CreateText(footer.transform, "H / Esc で閉じる　｜　← → でページ切替", 14, FontStyle.Italic);
        var closeHintRect = closeHint.GetComponent<RectTransform>();
        closeHintRect.anchorMin = new Vector2(0.5f, 0f);
        closeHintRect.anchorMax = new Vector2(0.5f, 0f);
        closeHintRect.sizeDelta = new Vector2(500f, 30f);
        closeHintRect.anchoredPosition = new Vector2(0f, 5f);
        closeHint.color = new Color(0.7f, 0.7f, 0.7f, 1f);

        UpdatePageContent();
    }

    void CreateNavButton(Transform parent, string text, Vector2 position, System.Action onClick)
    {
        var btnGO = new GameObject("NavButton", typeof(Image), typeof(Button));
        var btnRect = btnGO.GetComponent<RectTransform>();
        btnRect.SetParent(parent, false);
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(100f, 36f);
        btnRect.anchoredPosition = position;

        var btnImage = btnGO.GetComponent<Image>();
        btnImage.color = buttonColor;

        var btn = btnGO.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = new Color(0.5f, 0.5f, 0.55f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick());

        var btnText = CreateText(btnGO.transform, text, 16, FontStyle.Bold);
        var textRect = btnText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    Text CreateText(Transform parent, string text, int size, FontStyle style)
    {
        var go = new GameObject("Text", typeof(Text));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        var uiText = go.GetComponent<Text>();
        uiText.text = text;
        uiText.fontSize = size;
        uiText.fontStyle = style;
        uiText.color = Color.white;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;

        return uiText;
    }

    void UpdatePageContent()
    {
        if (contentText == null || pageIndicatorText == null || headerText == null) return;

        pageIndicatorText.text = $"{currentPage + 1} / {totalPages}";

        switch (currentPage)
        {
            case 0:
                headerText.text = "操作方法";
                contentText.text = GetControlsText();
                break;
            case 1:
                headerText.text = "ゲームルール";
                contentText.text = GetRulesText();
                break;
        }

        // コンテンツの高さをテキストのpreferredHeightに合わせる
        if (contentRect != null)
        {
            // テキストの必要な高さを取得
            float preferredHeight = contentText.preferredHeight;
            contentRect.sizeDelta = new Vector2(0f, preferredHeight + 20f); // 余白を追加
        }

        // スクロール位置を先頭にリセット
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    string GetControlsText()
    {
        return @"<b>【マウス操作】</b>

  • <color=#90EE90>左クリック</color>
      置ける場所（緑色ハイライト）をクリックして駒を置く

  • <color=#90EE90>クリック位置</color>
      盤面上の好きな場所にカーソルを移動


<b>【キーボード操作】</b>

  • <color=#FFD700>矢印キー / WASD</color>
      カーソルを上下左右に移動

  • <color=#FFD700>Enter / Space</color>
      カーソル位置に駒を置く


<b>【その他のキー】</b>

  • <color=#87CEEB>H キー</color>
      このヘルプを表示/非表示

  • <color=#87CEEB>M キー</color>
      ゲーム中にタイトル画面を再表示";
    }

    string GetRulesText()
    {
        return @"<b>【基本ルール】</b>

  • 黒と白が<color=#FFD700>交互に</color>駒を置きます（黒が先手）

  • 駒を置いて<color=#90EE90>相手の駒を挟む</color>と、
    挟まれた駒が自分の色に<color=#90EE90>ひっくり返ります</color>

  • 縦・横・斜めの<color=#87CEEB>8方向</color>すべてで挟むことができます

  • 1つでも相手の駒を挟める場所にのみ置けます
    （置ける場所は緑色でハイライト表示）


<b>【パスと終了】</b>

  • 置ける場所がない場合は<color=#FFA500>自動的にパス</color>となり、
    相手の番になります

  • 両方のプレイヤーが置けなくなったら<color=#FF6B6B>ゲーム終了</color>


<b>【勝敗】</b>

  • ゲーム終了時、<color=#FFD700>駒の数が多い方が勝ち</color>です

  • 同数の場合は引き分けとなります";
    }

    public void NextPage()
    {
        currentPage = (currentPage + 1) % totalPages;
        UpdatePageContent();
    }

    public void PreviousPage()
    {
        currentPage = (currentPage - 1 + totalPages) % totalPages;
        UpdatePageContent();
    }

    public void ToggleHelp()
    {
        if (isVisible)
        {
            HideHelp();
        }
        else
        {
            ShowHelp();
        }
    }

    public void ShowHelp()
    {
        isVisible = true;
        currentPage = 0;
        UpdatePageContent();
        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
        }
    }

    public void HideHelp()
    {
        isVisible = false;
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
        }
    }
}
