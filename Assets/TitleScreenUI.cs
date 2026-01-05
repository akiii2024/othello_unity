using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class TitleScreenUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] Color backdropColor = new Color(0f, 0f, 0f, 0.65f);
    [SerializeField] Vector2 referenceResolution = new Vector2(1920, 1080);
    [SerializeField] string titleText = "Stack Othello";
    [SerializeField] string subtitleText = "対戦方式を選んでください";
    [SerializeField] Font overrideFont; // 未設定ならビルトインArialを使用

    Canvas canvas;
    float originalTimeScale = 1f;
    bool menuActive = false;
    Font defaultFont;

    // 対戦モード選択用
    GameMode selectedMode = GameMode.HumanVsHuman;
    Text modeDisplayText;

    static bool created = false; // シーンロードをまたいで一度だけ生成する

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateMenuOnLoad()
    {
        if (created) return;
        var go = new GameObject("TitleScreenUI");
        go.AddComponent<TitleScreenUI>();
    }

    void Awake()
    {
        created = true;
        DontDestroyOnLoad(gameObject);

        originalTimeScale = Time.timeScale;
        BuildUI();
        PauseGame(true);
        menuActive = true;
        GameSettings.IsTitleScreenActive = true;
        Debug.Log("[TitleScreenUI] Menu created");
    }

    void OnDestroy()
    {
        // 念のためゲームを再開
        PauseGame(false);
        GameSettings.IsTitleScreenActive = false;
    }

    void BuildUI()
    {
        EnsureEventSystem();

        var canvasGO = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 他UIより前面に出す
        canvas.pixelPerfect = true;

        // フォントを確実にセット（未指定ならLegacyRuntime）
        defaultFont = overrideFont != null ? overrideFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var backdrop = new GameObject("Backdrop", typeof(Image));
        var backdropRect = backdrop.GetComponent<RectTransform>();
        backdropRect.SetParent(canvas.transform, false);
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = backdropColor;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.SetParent(canvas.transform, false);
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(520f, 0f);

        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 14f;
        layout.padding = new RectOffset(24, 24, 24, 24);

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateText(content.transform, titleText, 48, FontStyle.Bold, new Vector2(0f, 20f));
        CreateText(content.transform, subtitleText, 24, FontStyle.Normal);

        // 対戦モード選択（表示のみ、クリックはUpdateで検出）
        modeDisplayText = CreateModeButton(content.transform, GetModeDisplayString(), 28, FontStyle.Bold);

        // 使い方テキスト
        CreateText(content.transform, "画面クリック or 矢印キー/Tabでモード切り替え", 16, FontStyle.Italic);
        CreateText(content.transform, "Enter/Spaceでゲーム開始 / Hキーでヘルプ", 16, FontStyle.Italic);
    }

    void Update()
    {
        if (!menuActive) 
        {
            // Mキーでメニューを再表示（何も見えない場合のリカバリ）
            if (Input.GetKeyDown(KeyCode.M))
            {
                ShowMenu();
            }
            return;
        }

        // キーボードでモード切り替え（矢印キー、Tab、左右）
        if (Input.GetKeyDown(KeyCode.Tab) || 
            Input.GetKeyDown(KeyCode.LeftArrow) || 
            Input.GetKeyDown(KeyCode.RightArrow) ||
            Input.GetKeyDown(KeyCode.UpArrow) || 
            Input.GetKeyDown(KeyCode.DownArrow))
        {
            ToggleMode();
        }

        // マウスクリックでモード切り替え（画面中央付近のクリック）
        if (Input.GetMouseButtonDown(0))
        {
            // 画面中央付近（上下30%〜70%の範囲）のクリックでモード切り替え
            float normalizedY = Input.mousePosition.y / Screen.height;
            if (normalizedY > 0.3f && normalizedY < 0.7f)
            {
                ToggleMode();
            }
        }

        // Enter/Spaceでゲーム開始
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"[TitleScreenUI] Enter/Space pressed. selectedMode = {selectedMode}");
            StartGame(selectedMode);
        }
    }

    void ToggleMode()
    {
        Debug.Log($"[TitleScreenUI] ToggleMode called. Current mode: {selectedMode}");
        // 2人対戦 ↔ AI対戦 の切り替え
        if (selectedMode == GameMode.HumanVsHuman)
        {
            selectedMode = GameMode.HumanVsCPU;
        }
        else
        {
            selectedMode = GameMode.HumanVsHuman;
        }
        Debug.Log($"[TitleScreenUI] Mode changed to: {selectedMode}");
        UpdateModeDisplay();
    }

    string GetModeDisplayString()
    {
        if (selectedMode == GameMode.HumanVsHuman)
        {
            return "【 2人対戦 】";
        }
        else
        {
            return "【 AI対戦 】";
        }
    }

    void UpdateModeDisplay()
    {
        if (modeDisplayText != null)
        {
            modeDisplayText.text = GetModeDisplayString();
        }
    }

    void PauseGame(bool pause)
    {
        Time.timeScale = pause ? 0f : originalTimeScale;
    }

    void StartGame(GameMode mode)
    {
        GameSettings.GameMode = mode;
        // AI対戦の場合はデフォルト設定を使用
        GameSettings.CpuColor = DiscColor.White;
        GameSettings.CpuDifficulty = CPUDifficulty.Medium;

        var manager = FindFirstObjectByType<BoardManager>();
        if (manager != null)
        {
            // 直接プロパティを設定（確実に反映されるように）
            manager.gameMode = mode;
            manager.cpuColor = DiscColor.White;
            manager.cpuDifficulty = CPUDifficulty.Medium;
            manager.InitBoard();
            Debug.Log($"[TitleScreenUI] Game started with mode: {mode}");
        }
        else
        {
            Debug.LogWarning("[TitleScreenUI] BoardManager not found!");
        }

        PauseGame(false);
        GameSettings.IsTitleScreenActive = false;

        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }
        Destroy(gameObject);
        menuActive = false;
    }

    void ShowMenu()
    {
        // 既にキャンバスがなければ再生成
        if (canvas == null)
        {
            BuildUI();
        }

        PauseGame(true);
        menuActive = true;
        GameSettings.IsTitleScreenActive = true;
        Debug.Log("[TitleScreenUI] Menu re-opened (M key)");
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        es.transform.SetParent(transform, false);
    }

    Text CreateText(Transform parent, string text, int size, FontStyle style, Vector2? extraPadding = null)
    {
        var go = new GameObject("Text", typeof(Text));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(520f, 0f);

        if (extraPadding.HasValue)
        {
            rect.anchoredPosition = extraPadding.Value;
        }

        var uiText = go.GetComponent<Text>();
        uiText.text = text;
        uiText.fontSize = size;
        uiText.fontStyle = style;
        uiText.color = Color.white;
        uiText.font = defaultFont;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;

        return uiText;
    }

    Text CreateModeButton(Transform parent, string text, int size, FontStyle style)
    {
        // 背景用のImageを持つ親オブジェクトを作成
        var go = new GameObject("ModeButton", typeof(RectTransform), typeof(Image));
        var buttonRect = go.GetComponent<RectTransform>();
        buttonRect.SetParent(parent, false);
        buttonRect.sizeDelta = new Vector2(300f, 60f);

        // 背景
        var bgImage = go.GetComponent<Image>();
        bgImage.color = new Color(0.2f, 0.4f, 0.6f, 0.8f);

        // テキストを子オブジェクトとして作成
        var textGO = new GameObject("Text", typeof(Text));
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.SetParent(go.transform, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var uiText = textGO.GetComponent<Text>();
        uiText.text = text;
        uiText.fontSize = size;
        uiText.fontStyle = style;
        uiText.color = Color.white;
        uiText.font = defaultFont;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;

        return uiText;
    }
}
