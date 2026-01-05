using UnityEngine;

public class BoardClickPlace : MonoBehaviour
{
    public BoardGrid board;
    public BoardManager manager;
    public BoardHighlighter highlighter;
    public HelpPanel helpPanel; // ヘルプパネル（オプション）

    // カーソル位置（キー入力用）
    int cursorX = 0;
    int cursorY = 0;

    void Start()
    {
        // カーソルを中央付近に初期化
        cursorX = 4;
        cursorY = 4;
        UpdateCursorDisplay();
    }

    void Update()
    {
        // Hキーでヘルプ表示/非表示（ヘルプ表示中でも処理する）
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (helpPanel != null)
            {
                helpPanel.ToggleHelp();
                // ヘルプ表示状態に応じてハイライトとカーソルを制御
                UpdateHighlightVisibility();
            }
        }

        // ヘルプ表示中は他の操作を無視
        bool isHelpVisible = helpPanel != null && helpPanel.helpPanel != null && helpPanel.helpPanel.activeSelf;
        if (isHelpVisible)
        {
            return;
        }

        // CPUのターン時は入力を無視
        if (manager != null && manager.IsCPUTurn())
        {
            return;
        }

        // マウスクリック処理
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                if (board.TryWorldToCell(hit.point, out int x, out int y))
                {
                    cursorX = x;
                    cursorY = y;
                    UpdateCursorDisplay();
                    manager.TryPlace(x, y);
                }
            }
        }

        // キー入力処理
        HandleKeyboardInput();
    }

    void HandleKeyboardInput()
    {
        // Hキーの処理はUpdate()で行うため、ここでは処理しない
        // ヘルプが表示されている場合は他の操作を無視（念のため）
        bool isHelpVisible = helpPanel != null && helpPanel.helpPanel != null && helpPanel.helpPanel.activeSelf;
        if (isHelpVisible)
        {
            return;
        }

        // CPUのターン時は入力を無視
        if (manager != null && manager.IsCPUTurn())
        {
            return;
        }

        bool moved = false;

        // 矢印キーまたはWASDでカーソル移動（左右・上下を反転）
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            cursorX = Mathf.Min(7, cursorX + 1);
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            cursorX = Mathf.Max(0, cursorX - 1);
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            cursorY = Mathf.Max(0, cursorY - 1);
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            cursorY = Mathf.Min(7, cursorY + 1);
            moved = true;
        }

        if (moved)
        {
            UpdateCursorDisplay();
        }

        // EnterまたはSpaceで駒を置く
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            manager.TryPlace(cursorX, cursorY);
        }
    }

    void UpdateCursorDisplay()
    {
        // ヘルプ表示中はカーソルを表示しない
        if (helpPanel != null && helpPanel.helpPanel != null && helpPanel.helpPanel.activeSelf)
        {
            if (highlighter != null)
            {
                highlighter.HideCursor();
            }
            return;
        }

        // CPUのターン時はカーソルを非表示
        if (manager != null && manager.IsCPUTurn())
        {
            if (highlighter != null)
            {
                highlighter.HideCursor();
            }
            return;
        }

        if (highlighter != null)
        {
            highlighter.ShowCursor(cursorX, cursorY);
        }
    }

    void UpdateHighlightVisibility()
    {
        bool isHelpVisible = helpPanel != null && helpPanel.helpPanel != null && helpPanel.helpPanel.activeSelf;
        
        if (isHelpVisible)
        {
            // ヘルプ表示時はハイライトとカーソルを非表示
            if (highlighter != null)
            {
                highlighter.Clear();
                highlighter.HideCursor();
            }
        }
        else
        {
            // ヘルプ非表示時はカーソルとハイライトを再表示
            UpdateCursorDisplay();
            if (manager != null)
            {
                manager.UpdateHighlighter();
            }
        }
    }
}
