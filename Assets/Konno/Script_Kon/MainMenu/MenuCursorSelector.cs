using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

namespace PersonaMenuUI
{
    public class MenuCursorSelector : MonoBehaviour
    {
        [System.Serializable]
        public class IndexEvent : UnityEvent<int> { }

        [Header("メニュー項目 (並び順)")]
        [Tooltip("各メニュー項目のRectTransform(縦並びなら上から順、横並びなら左から順)。カーソルはこれらの座標(X・Y)へ移動する。")]
        [SerializeField] private RectTransform[] menuItems;

        [Tooltip("各メニュー項目に対応するテキスト(menuItemsと同じ順番)。Manage Label ColorsがONなら色/太さの切替に、反転テキスト演出を使うなら文字列のコピー元として使う。両方使わないなら空でよい。")]
        [SerializeField] private TMP_Text[] menuLabels;

        [Header("カーソル本体")]
        [Tooltip("移動させるカーソルのRectTransform。SlantedRect(またはImage)を付けたオブジェクト。")]
        [SerializeField] private RectTransform cursorRect;

        [Tooltip("切替の瞬間だけ光らせるスラッシュ演出用のSlantedRect。未設定でも動作する(その場合は移動アニメのみ)。")]
        [SerializeField] private SlantedRect slashFlash;

        [Header("移動アニメーション")]
        [SerializeField] private float moveDuration = 0.18f;
        [SerializeField] private AnimationCurve moveEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("スラッシュ演出")]
        [SerializeField] private float slashDuration = 0.22f;
        [SerializeField] private Color slashColor = Color.white;
        [Tooltip("スラッシュが広がりきった時の幅(px)。slashFlashのRectTransformの初期WidthはInspector上で何を入れても、再生時はこの値が使われる。")]
        [SerializeField] private float slashFullWidth = 260f;

        [Header("テキストの見た目")]
        [Tooltip("ONにすると、このコンポーネントがMenu Labelsの色/太さを直接書き換える。既存のコード(MainMenuManagerなど)側で色/サイズを管理している場合はOFFのままにする。")]
        [SerializeField] private bool manageLabelColors = false;
        [SerializeField] private Color normalColor = new Color(0.55f, 0.85f, 1f);
        [SerializeField] private Color selectedColor = Color.black;
        [SerializeField] private bool boldWhenSelected = true;

        [Header("反転テキスト演出 (ペルソナ3風・任意)")]
        [Tooltip("Cursorの子にした、色反転表示専用のTMP_Text。CursorにMaskコンポーネントを付けて、この文字をカーソルの斜め形状で切り抜く。\n選択中の項目と同じ文字列・フォントサイズ・スタイルを自動でコピーし、Color欄で設定した色(白など)だけがカーソルの中に見える。未設定なら反転演出なしで動作する。")]
        [SerializeField] private TMP_Text invertedLabel;

        [Header("入力 (簡易デモ用。既存の入力処理がある場合はOFFにする)")]
        [SerializeField] private bool useInternalInput = true;
        [SerializeField] private KeyCode upKey = KeyCode.UpArrow;
        [SerializeField] private KeyCode downKey = KeyCode.DownArrow;
        [SerializeField] private KeyCode confirmKey = KeyCode.Z;
        [SerializeField] private float inputRepeatDelay = 0.15f;

        [Header("Unity標準UIナビゲーションとの連携 (任意)")]
        [Tooltip("ONにすると、EventSystemの現在選択オブジェクトがmenuItemsのいずれかと一致した時に自動でSelect()を呼ぶ。Selectable/Buttonで矢印キー移動を実装済みの場合に使う。")]
        [SerializeField] private bool followEventSystemSelection = false;

        public IndexEvent onSelectionChanged;
        public IndexEvent onConfirm;

        public int CurrentIndex { get; private set; }

        private Coroutine moveRoutine;
        private Coroutine slashRoutine;
        private float nextInputTime;

        private void Start()
        {
            if (menuItems == null || menuItems.Length == 0)
            {
                Debug.LogWarning($"{nameof(MenuCursorSelector)}: menuItems が設定されていません。", this);
                enabled = false;
                return;
            }

            CurrentIndex = Mathf.Clamp(CurrentIndex, 0, menuItems.Length - 1);
            SnapCursorTo(CurrentIndex);
            UpdateLabelStyles();
        }

        private void Update()
        {
            if (useInternalInput)
            {
                HandleInternalInput();
            }

            if (followEventSystemSelection)
            {
                SyncToEventSystemSelection();
            }
        }

        private void HandleInternalInput()
        {
            if (Time.unscaledTime < nextInputTime) return;

            bool up = Input.GetKeyDown(upKey);
            bool down = Input.GetKeyDown(downKey);

            if (up || down)
            {
                Select(CurrentIndex + (up ? -1 : 1));
                nextInputTime = Time.unscaledTime + inputRepeatDelay;
            }
            else if (Input.GetKeyDown(confirmKey))
            {
                onConfirm?.Invoke(CurrentIndex);
            }
        }

        public void SyncToEventSystemSelection()
        {
            if (EventSystem.current == null) return;
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null) return;

            for (int i = 0; i < menuItems.Length; i++)
            {
                if (menuItems[i] != null && menuItems[i].gameObject == selected)
                {
                    Select(i);
                    break;
                }
            }
        }

        public void Select(int index)
        {
            if (menuItems == null || menuItems.Length == 0) return;

            index = ((index % menuItems.Length) + menuItems.Length) % menuItems.Length;

            bool changed = index != CurrentIndex;
            CurrentIndex = index;
            UpdateLabelStyles();

            if (!changed)
            {
                SnapCursorTo(CurrentIndex);
                return;
            }

            onSelectionChanged?.Invoke(CurrentIndex);

            if (moveRoutine != null) StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(MoveCursorRoutine(menuItems[CurrentIndex]));

            if (slashFlash != null)
            {
                if (slashRoutine != null) StopCoroutine(slashRoutine);
                slashRoutine = StartCoroutine(SlashFlashRoutine(menuItems[CurrentIndex]));
            }
        }

        private void SnapCursorTo(int index)
        {
            if (cursorRect == null || menuItems[index] == null) return;
            cursorRect.anchoredPosition = menuItems[index].anchoredPosition;
        }

        private IEnumerator MoveCursorRoutine(RectTransform target)
        {
            if (cursorRect == null || target == null) yield break;

            Vector2 startPos = cursorRect.anchoredPosition;
            Vector2 endPos = target.anchoredPosition;
            float t = 0f;

            while (t < moveDuration)
            {
                t += Time.unscaledDeltaTime;
                float u = moveEase.Evaluate(Mathf.Clamp01(t / moveDuration));
                cursorRect.anchoredPosition = Vector2.Lerp(startPos, endPos, u);
                yield return null;
            }

            cursorRect.anchoredPosition = endPos;
        }

        private IEnumerator SlashFlashRoutine(RectTransform target)
        {
            RectTransform rt = slashFlash.rectTransform;
            float baseWidth = slashFullWidth;

            if (target != null)
            {
                rt.anchoredPosition = target.anchoredPosition;
            }

            Color startColor = slashColor;
            startColor.a = 1f;

            slashFlash.color = startColor;
            slashFlash.gameObject.SetActive(true);
            rt.sizeDelta = new Vector2(0f, rt.sizeDelta.y);

            float half = Mathf.Max(0.0001f, slashDuration * 0.5f);
            float t = 0f;

            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / half);
                rt.sizeDelta = new Vector2(Mathf.Lerp(0f, baseWidth, u), rt.sizeDelta.y);
                yield return null;
            }

            rt.sizeDelta = new Vector2(baseWidth, rt.sizeDelta.y);

            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / half);
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, u);
                slashFlash.color = c;
                yield return null;
            }

            slashFlash.gameObject.SetActive(false);
            rt.sizeDelta = new Vector2(baseWidth, rt.sizeDelta.y);
        }

        private void UpdateLabelStyles()
        {
            if (manageLabelColors && menuLabels != null)
            {
                for (int i = 0; i < menuLabels.Length; i++)
                {
                    if (menuLabels[i] == null) continue;

                    bool isSelected = i == CurrentIndex;
                    menuLabels[i].color = isSelected ? selectedColor : normalColor;

                    if (boldWhenSelected)
                    {
                        menuLabels[i].fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
                    }
                }
            }

            UpdateInvertedLabel();
        }

        private void UpdateInvertedLabel()
        {
            if (invertedLabel == null) return;

            if (menuLabels == null || CurrentIndex < 0 || CurrentIndex >= menuLabels.Length || menuLabels[CurrentIndex] == null)
            {
                invertedLabel.text = string.Empty;
                return;
            }

            TMP_Text source = menuLabels[CurrentIndex];
            invertedLabel.text = source.text;
            invertedLabel.font = source.font;
            invertedLabel.fontSize = source.fontSize;
            invertedLabel.alignment = source.alignment;
            invertedLabel.fontStyle = source.fontStyle;
        }
    }
}