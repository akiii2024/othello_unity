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

    // ボタン用スプライト
    Sprite buttonNormalSprite;
    Sprite buttonHoverSprite;

    // 対戦モード選択用
    GameMode selectedMode = GameMode.HumanVsHuman;
    Text modeDisplayText;
    RectTransform modeButtonRect;

    // AI難易度選択用
    CPUDifficulty selectedDifficulty = CPUDifficulty.Medium;
    Text difficultyDisplayText;
    RectTransform difficultyButtonRect;
    GameObject difficultyContainer;

    // ボードサイズ選択用
    int selectedBoardSize = 8;
    Text boardSizeDisplayText;
    RectTransform boardSizeButtonRect;

    // スタートボタン用
    RectTransform startButtonRect;

    // オーディオ
    AudioSource audioSource;
    AudioClip selectSound;
    AudioClip okSound;

    public static bool created = false; // シーンロードをまたいで一度だけ生成する（外部からリセット可能）

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateMenuOnLoad()
    {
        Debug.Log($"[TitleScreenUI] CreateMenuOnLoad called. created={created}");
        if (created) return;
        var go = new GameObject("TitleScreenUI");
        go.AddComponent<TitleScreenUI>();
        Debug.Log("[TitleScreenUI] New TitleScreenUI instance created");
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
        LoadButtonSprites();
        LoadAudioResources();

        var canvasGO = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 他UIより前面に出す
        canvas.pixelPerfect = true;

        // フォントを確実にセット（未指定ならResourcesから日本語フォントを読み込む）
        if (overrideFont != null)
        {
            defaultFont = overrideFont;
        }
        else
        {
            // Resourcesフォルダから日本語フォントを読み込む（優先順位順）
            defaultFont = Resources.Load<Font>("NotoSansJP") ?? 
                         Resources.Load<Font>("Meiryo") ?? 
                         Resources.Load<Font>("YuGothic") ??
                         Resources.Load<Font>("MS Gothic") ??
                         Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            if (defaultFont == null)
            {
                Debug.LogWarning("[TitleScreenUI] 日本語フォントが見つかりません。Resourcesフォルダに日本語フォント（NotoSansJP、Meiryo等）を追加してください。");
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

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
        layout.spacing = 20f;
        layout.padding = new RectOffset(32, 32, 40, 40);

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // タイトル
        CreateText(content.transform, titleText, 52, FontStyle.Bold);
        
        // スペーサー
        CreateSpacer(content.transform, 10f);
        
        CreateText(content.transform, subtitleText, 22, FontStyle.Normal);

        // スペーサー
        CreateSpacer(content.transform, 15f);

        // 対戦モード選択
        var modeResult = CreateOptionButton(content.transform, GetModeDisplayString(), 26, FontStyle.Bold, new Color(0.2f, 0.4f, 0.6f, 0.9f), () => ToggleMode());
        modeDisplayText = modeResult.text;
        modeButtonRect = modeResult.rect;

        // AI難易度選択（AI対戦時のみ表示）
        difficultyContainer = new GameObject("DifficultyContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        var diffRect = difficultyContainer.GetComponent<RectTransform>();
        diffRect.SetParent(content.transform, false);
        diffRect.sizeDelta = new Vector2(520f, 0f);
        var diffLayout = difficultyContainer.GetComponent<VerticalLayoutGroup>();
        diffLayout.childAlignment = TextAnchor.MiddleCenter;
        diffLayout.spacing = 10f;

        CreateText(difficultyContainer.transform, "AI難易度", 18, FontStyle.Normal);
        var diffResult = CreateOptionButton(difficultyContainer.transform, GetDifficultyDisplayString(), 22, FontStyle.Bold, new Color(0.4f, 0.3f, 0.5f, 0.9f), () => CycleDifficulty(1));
        difficultyDisplayText = diffResult.text;
        difficultyButtonRect = diffResult.rect;

        // 難易度表示の初期状態を設定
        UpdateDifficultyVisibility();

        // スペーサー
        CreateSpacer(content.transform, 10f);

        // ボードサイズ選択
        var boardSizeResult = CreateOptionButton(content.transform, GetBoardSizeDisplayString(), 22, FontStyle.Bold, new Color(0.5f, 0.4f, 0.3f, 0.9f), () => ToggleBoardSize());
        boardSizeDisplayText = boardSizeResult.text;
        boardSizeButtonRect = boardSizeResult.rect;

        // スペーサー
        CreateSpacer(content.transform, 20f);

        // スタートボタン
        startButtonRect = CreateStartButton(content.transform, "ゲームスタート", 26, FontStyle.Bold, () => StartGame(selectedMode));

        // スペーサー
        CreateSpacer(content.transform, 15f);

        // 使い方テキスト
        CreateText(content.transform, "マウスでクリックしづらい場合はキーで操作してください。", 14, FontStyle.Italic);
        CreateText(content.transform, "左右キー: モード切替 / 上下キー: 難易度切替 / Bキー: ボードサイズ", 14, FontStyle.Italic);
        CreateText(content.transform, "Enterキー / Spaceキー: ゲームスタート", 14, FontStyle.Italic);
        CreateText(content.transform, "Hキーでヘルプ", 14, FontStyle.Italic);
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

        // 左右キー/Tabでモード切り替え
        if (Input.GetKeyDown(KeyCode.Tab) || 
            Input.GetKeyDown(KeyCode.LeftArrow) || 
            Input.GetKeyDown(KeyCode.RightArrow))
        {
            ToggleMode();
        }

        // 上下キーで難易度切り替え（AI対戦時のみ）
        if (selectedMode == GameMode.HumanVsCPU)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                CycleDifficulty(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                CycleDifficulty(1);
            }
        }

        // Bキーでボードサイズ切り替え
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBoardSize();
        }

        // Enter/Spaceでゲーム開始
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"[TitleScreenUI] Starting game: mode={selectedMode}, difficulty={selectedDifficulty}, boardSize={selectedBoardSize}");
            StartGame(selectedMode);
        }
    }

    void ToggleMode()
    {
        // 2人対戦 ↔ AI対戦 の切り替え
        if (selectedMode == GameMode.HumanVsHuman)
        {
            selectedMode = GameMode.HumanVsCPU;
        }
        else
        {
            selectedMode = GameMode.HumanVsHuman;
        }
        UpdateModeDisplay();
        UpdateDifficultyVisibility();
        PlaySelectSound();
    }

    void CycleDifficulty(int direction)
    {
        int current = (int)selectedDifficulty;
        int count = System.Enum.GetValues(typeof(CPUDifficulty)).Length;
        current = (current + direction + count) % count;
        selectedDifficulty = (CPUDifficulty)current;
        UpdateDifficultyDisplay();
        PlaySelectSound();
    }

    string GetModeDisplayString()
    {
        if (selectedMode == GameMode.HumanVsHuman)
        {
            return "◀  2人対戦  ▶";
        }
        else
        {
            return "◀  AI対戦  ▶";
        }
    }

    string GetDifficultyDisplayString()
    {
        switch (selectedDifficulty)
        {
            case CPUDifficulty.Easy:
                return "▲ 簡単 ▼";
            case CPUDifficulty.Medium:
                return "▲ 普通 ▼";
            case CPUDifficulty.Hard:
                return "▲ 難しい ▼";
            default:
                return "▲ 普通 ▼";
        }
    }

    void UpdateModeDisplay()
    {
        if (modeDisplayText != null)
        {
            modeDisplayText.text = GetModeDisplayString();
        }
    }

    void UpdateDifficultyDisplay()
    {
        if (difficultyDisplayText != null)
        {
            difficultyDisplayText.text = GetDifficultyDisplayString();
        }
    }

    void UpdateDifficultyVisibility()
    {
        if (difficultyContainer != null)
        {
            difficultyContainer.SetActive(selectedMode == GameMode.HumanVsCPU);
        }
    }

    void ToggleBoardSize()
    {
        // 8 -> 6 -> 4 -> 8 の循環切り替え
        if (selectedBoardSize == 8)
            selectedBoardSize = 6;
        else if (selectedBoardSize == 6)
            selectedBoardSize = 4;
        else
            selectedBoardSize = 8;
        UpdateBoardSizeDisplay();
        PlaySelectSound();
    }

    string GetBoardSizeDisplayString()
    {
        return $"◀  {selectedBoardSize}×{selectedBoardSize}  ▶";
    }

    void UpdateBoardSizeDisplay()
    {
        if (boardSizeDisplayText != null)
        {
            boardSizeDisplayText.text = GetBoardSizeDisplayString();
        }
    }

    void PauseGame(bool pause)
    {
        Time.timeScale = pause ? 0f : originalTimeScale;
    }

    void StartGame(GameMode mode)
    {
        // 直ちにサウンド再生（PlayClipAtPointではなくAudioSourceを使う）
        if (okSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(okSound);
        }

        GameSettings.GameMode = mode;
        GameSettings.CpuColor = DiscColor.White;
        GameSettings.CpuDifficulty = selectedDifficulty;
        GameSettings.BoardSize = selectedBoardSize;

        var manager = FindFirstObjectByType<BoardManager>();
        if (manager != null)
        {
            // 直接プロパティを設定（確実に反映されるように）
            manager.gameMode = mode;
            manager.cpuColor = DiscColor.White;
            manager.cpuDifficulty = selectedDifficulty;
            manager.boardSize = selectedBoardSize;
            
            // グリッドのサイズも同期
            if (manager.grid != null)
            {
                manager.grid.size = selectedBoardSize;
            }
            
            manager.InitBoard();
            Debug.Log($"[TitleScreenUI] Game started with mode: {mode}, difficulty: {selectedDifficulty}, boardSize: {selectedBoardSize}");
        }
        else
        {
            Debug.LogWarning("[TitleScreenUI] BoardManager not found!");
        }

        PauseGame(false);
        GameSettings.IsTitleScreenActive = false;

        // UIを非表示にしてから、音が鳴り終わるまで待ってDestroyする
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false); // キャンバスごと非表示
        }
        
        // メニュー操作を受け付けないようにフラグを下げる
        menuActive = false;

        // 音の長さ分待ってから破壊
        float delay = (okSound != null) ? okSound.length : 0f;
        StartCoroutine(DestroyAfterDelay(delay));
    }

    System.Collections.IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); // Time.timeScaleの影響を受けないようにRealtime推奨だが、PauseGame(false)してるのでどちらでも可
        Destroy(gameObject);
    }

    public void ShowMenu()
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

    void LoadButtonSprites()
    {
        // Resourcesフォルダからテクスチャを読み込んでスプライトを作成
        var normalTex = Resources.Load<Texture2D>("button_normal");
        var hoverTex = Resources.Load<Texture2D>("button_hover");

        if (normalTex != null)
        {
            buttonNormalSprite = Sprite.Create(normalTex, new Rect(0, 0, normalTex.width, normalTex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(30, 30, 30, 30));
        }
        if (hoverTex != null)
        {
            buttonHoverSprite = Sprite.Create(hoverTex, new Rect(0, 0, hoverTex.width, hoverTex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(30, 30, 30, 30));
        }
    }

    void LoadAudioResources()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        selectSound = Resources.Load<AudioClip>("select");
        okSound = Resources.Load<AudioClip>("ok");
    }

    void PlaySelectSound()
    {
        if (audioSource != null && selectSound != null)
        {
            audioSource.PlayOneShot(selectSound);
        }
    }

    Text CreateText(Transform parent, string text, int size, FontStyle style)
    {
        var go = new GameObject("Text", typeof(Text));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(520f, 0f);

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

    void CreateSpacer(Transform parent, float height)
    {
        var go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        
        var layoutElement = go.GetComponent<LayoutElement>();
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;
    }

    (Text text, RectTransform rect) CreateOptionButton(Transform parent, string text, int size, FontStyle style, Color tintColor, System.Action onClick)
    {
        // 背景用のImageを持つ親オブジェクトを作成（Buttonコンポーネント追加）
        var go = new GameObject("OptionButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var buttonRect = go.GetComponent<RectTransform>();
        buttonRect.SetParent(parent, false);
        buttonRect.sizeDelta = new Vector2(320f, 55f);

        // 背景（スプライト使用）
        var bgImage = go.GetComponent<Image>();
        if (buttonNormalSprite != null)
        {
            bgImage.sprite = buttonNormalSprite;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = tintColor;
        }
        else
        {
            bgImage.color = tintColor;
        }

        // Buttonコンポーネントの設定
        var btn = go.GetComponent<Button>();
        if (buttonNormalSprite != null && buttonHoverSprite != null)
        {
            // スプライトスワップモード
            btn.transition = Selectable.Transition.SpriteSwap;
            var spriteState = new SpriteState();
            spriteState.highlightedSprite = buttonHoverSprite;
            spriteState.pressedSprite = buttonHoverSprite;
            spriteState.selectedSprite = buttonNormalSprite;
            btn.spriteState = spriteState;
        }
        else
        {
            // フォールバック：色変更モード
            var colors = btn.colors;
            colors.normalColor = tintColor;
            colors.highlightedColor = new Color(tintColor.r + 0.15f, tintColor.g + 0.15f, tintColor.b + 0.15f, tintColor.a);
            colors.pressedColor = new Color(tintColor.r + 0.25f, tintColor.g + 0.25f, tintColor.b + 0.25f, tintColor.a);
            btn.colors = colors;
        }
        btn.onClick.AddListener(() => onClick());

        // テキストを子オブジェクトとして作成（影付き）
        var textGO = new GameObject("Text", typeof(Text), typeof(Shadow));
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

        // テキストに影を追加
        var shadow = textGO.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        shadow.effectDistance = new Vector2(2f, -2f);

        return (uiText, buttonRect);
    }

    RectTransform CreateStartButton(Transform parent, string text, int size, FontStyle style, System.Action onClick)
    {
        // スタートボタン用のオブジェクトを作成（Buttonコンポーネント追加）
        var go = new GameObject("StartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var buttonRect = go.GetComponent<RectTransform>();
        buttonRect.SetParent(parent, false);
        buttonRect.sizeDelta = new Vector2(320f, 65f);

        // 背景（緑系の目立つ色 + スプライト）
        Color tintColor = new Color(0.3f, 0.75f, 0.4f, 1f);
        var bgImage = go.GetComponent<Image>();
        if (buttonNormalSprite != null)
        {
            bgImage.sprite = buttonNormalSprite;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = tintColor;
        }
        else
        {
            bgImage.color = tintColor;
        }

        // Buttonコンポーネントの設定
        var btn = go.GetComponent<Button>();
        if (buttonNormalSprite != null && buttonHoverSprite != null)
        {
            // スプライトスワップモード
            btn.transition = Selectable.Transition.SpriteSwap;
            var spriteState = new SpriteState();
            spriteState.highlightedSprite = buttonHoverSprite;
            spriteState.pressedSprite = buttonHoverSprite;
            spriteState.selectedSprite = buttonNormalSprite;
            btn.spriteState = spriteState;
        }
        else
        {
            // フォールバック：色変更モード
            var colors = btn.colors;
            colors.normalColor = tintColor;
            colors.highlightedColor = new Color(0.35f, 0.85f, 0.45f, 1f);
            colors.pressedColor = new Color(0.4f, 0.95f, 0.5f, 1f);
            btn.colors = colors;
        }
        btn.onClick.AddListener(() => onClick());

        // テキストを子オブジェクトとして作成（影付き）
        var textGO = new GameObject("Text", typeof(Text), typeof(Shadow), typeof(Outline));
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

        // テキストに影を追加
        var shadow = textGO.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(2f, -2f);

        // テキストにアウトラインを追加（より目立つように）
        var outline = textGO.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0.3f, 0.1f, 0.8f);
        outline.effectDistance = new Vector2(1f, -1f);

        return buttonRect;
    }
}
