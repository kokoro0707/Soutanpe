using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

namespace PersonaMenuUI
{
    /// <summary>
    /// 縦・横どちらに並んだメニュー項目にも使える「選択カーソル(斜めハイライト)」を制御する。
    /// カーソルは各項目のRectTransformのanchoredPosition(X・Y両方)へ移動するので、
    /// 縦リストにも横並びメニューにもそのまま使える。
    ///
    /// メニュー項目そのもの(テキストの配置など)は既に用意されている前提で、
    /// このコンポーネントは
    ///   ・選択位置へカーソル(SlantedRectを付けたImage的オブジェクト)を移動させる
    ///   ・移動中に斜めスラッシュが走るワイプ演出を再生する
    ///   ・選択中/非選択の項目テキストの色・太さを切り替える
    /// という3つだけを担当する。
    ///
    /// 既存のメニュー入力ロジックがある場合は useInternalInput を false にして、
    /// 自前のコードから Select(index) を呼び出すだけで組み込める。
    /// </summary>
    public class MenuCursorSelector : MonoBehaviour
    {
        [System.Serializable]
        public class IndexEvent : UnityEvent<int> { }

        [Header("メニュー項目 (並び順)")]
        [Tooltip("各メニュー項目のRectTransform(縦並びなら上から順、横並びなら左から順)。カーソルはこれらの座標(X・Y)へ移動する。")]
        [SerializeField] private RectTransform[] menuItems;

        [Tooltip("各メニュー項目に対応するテキスト。省略可(色/太さの切替をしない場合は空でよい)。")]
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
        [SerializeField] private Color normalColor = new Color(0.55f, 0.85f, 1f);
        [SerializeField] private Color selectedColor = Color.black;
        [SerializeField] private bool boldWhenSelected = true;

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

        /// <summary>
        /// EventSystemの現在の選択オブジェクトを見て、menuItemsに含まれていれば
        /// そのインデックスをSelect()する。Selectable(Button等)で上下ナビゲーションを
        /// 組んでいる既存メニューに、見た目のカーソルだけ後付けしたい場合に使う。
        /// </summary>
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

        /// <summary>
        /// 指定インデックスへ選択を切り替える。外部の入力/選択ロジックから
        /// 直接呼び出してよい公開メソッド。
        /// </summary>
        public void Select(int index)
        {
            if (menuItems == null || menuItems.Length == 0) return;

            // 端でループさせる(最上段でさらに上へ行くと最下段へ)
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
                slashRoutine = StartCoroutine(SlashFlashRoutine());
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

            // 縦並びメニュー(Y移動のみ)・横並びメニュー(X移動のみ)の
            // どちらでも使えるよう、XY両方を対象の座標へ補間する。
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

        /// <summary>
        /// 選択切替の瞬間に、斜めの白いバー(slashFlash)を
        /// 幅0→最大まで一気に走らせてからフェードアウトさせる演出。
        /// 「斜めスラッシュが切り込むような」ペルソナ風の切替感を出す。
        /// </summary>
        private IEnumerator SlashFlashRoutine()
        {
            RectTransform rt = slashFlash.rectTransform;
            float baseWidth = slashFullWidth;

            Color startColor = slashColor;
            startColor.a = 1f;

            slashFlash.color = startColor;
            slashFlash.gameObject.SetActive(true);
            rt.sizeDelta = new Vector2(0f, rt.sizeDelta.y);

            float half = Mathf.Max(0.0001f, slashDuration * 0.5f);
            float t = 0f;

            // 幅0→最大まで一気に広がる(スラッシュが走り抜ける)
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / half);
                rt.sizeDelta = new Vector2(Mathf.Lerp(0f, baseWidth, u), rt.sizeDelta.y);
                yield return null;
            }

            rt.sizeDelta = new Vector2(baseWidth, rt.sizeDelta.y);

            // 広がりきったら、フェードアウトしながら消える
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
            if (menuLabels == null) return;

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
    }
}
