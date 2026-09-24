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
    ///   ・(任意)選択中/非選択の項目テキストの色・太さを切り替える
    ///   ・(任意)Cursorの中だけ文字色が反転して見える、ペルソナ3風の演出を行う
    ///   ・(任意)選択項目の左右の矢印を、Cursorに追従させつつ小刻みにバウンドさせる
    ///   ・(任意)Cursorを1つ動かす代わりに、項目ごとに個別のカーソル画像を用意しておいて
    ///     選択中のものだけを表示する「個別カーソル」モードで動作する
    ///   ・(任意)スラッシュ演出も、項目ごとに個別のオブジェクトを用意しておいて
    ///     選択中の項目に対応するものだけを再生する「個別スラッシュ」モードで動作する
    /// を担当する。
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

        [Tooltip("各メニュー項目に対応するテキスト(menuItemsと同じ順番)。Manage Label ColorsがONなら色/太さの切替に、反転テキスト演出を使うなら文字列のコピー元として使う。両方使わないなら空でよい。")]
        [SerializeField] private TMP_Text[] menuLabels;

        [Header("カーソル本体")]
        [Tooltip("移動させるカーソルのRectTransform。SlantedRect(またはImage)を付けたオブジェクト。Item Cursorsを使う場合は未設定でもよい。")]
        [SerializeField] private RectTransform cursorRect;

        [Tooltip("切替の瞬間だけ光らせるスラッシュ演出用のSlantedRect。未設定でも動作する(その場合は移動アニメのみ)。Item Slash Flashesを使う場合は未設定でもよい。")]
        [SerializeField] private SlantedRect slashFlash;

        [Header("項目ごとに別々のカーソルを使う場合 (任意)")]
        [Tooltip("Cursor Rectを1つ移動させる代わりに、項目ごとに個別に用意したカーソル(SlantedRectでも、丸い赤枠のようなImageでも、形も大きさも項目ごとに自由に変えてよい)を切り替えて表示したい場合に使う。\n" +
            "menuItemsと同じ順番・同じ要素数で設定すると、CursorRectの移動アニメーションの代わりに、選択中の項目のオブジェクトだけを表示(それ以外は非表示)する「個別カーソル」モードに自動で切り替わる。\n" +
            "各要素は、あらかじめその項目にぴったり合う位置・大きさ・傾き・色で個別に作っておく(実行中に座標を動かしたりはしない)。未設定(要素数0)なら今まで通りCursor Rectが移動する。")]
        [SerializeField] private RectTransform[] itemCursors;

        [Tooltip("個別カーソルモードで選択が切り替わった瞬間、一瞬拡大してから等倍に戻る「ポン」というポップアニメーションの時間(秒)。0にすると即座に切り替わる。")]
        [SerializeField] private float itemCursorPopDuration = 0.12f;

        [Tooltip("個別カーソルモードのポップアニメーションで、切り替わった瞬間に一瞬拡大する倍率。")]
        [SerializeField] private float itemCursorPopScale = 1.15f;

        [Tooltip("Slash Flashも、Item Cursorsと同じように項目ごとに個別のオブジェクトを用意して切り替えたい場合に使う。\n" +
            "menuItems / itemCursorsと同じ順番・同じ要素数で設定すると、共通のSlash Flashを毎回移動させる代わりに、" +
            "選択された項目に対応するスラッシュだけを、そのオブジェクト自身の位置・角度のまま再生する「個別スラッシュ」モードに自動で切り替わる。\n" +
            "各要素は、あらかじめ対応するItem Cursorと同じ位置・角度になるよう個別に配置しておく(このスクリプトが座標を動かすことはない)。未設定(要素数0)なら今まで通り共通のSlash Flashが使われる。")]
        [SerializeField] private SlantedRect[] itemSlashFlashes;

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

        [Header("選択時テキスト拡大 (ペルソナ3風・任意)")]
        [Tooltip("ONにすると、選択中の項目のMenu Labelだけを少し拡大表示する(ペルソナ3のメニューで選択中の文字が一回り大きくなる、あの見た目)。Manage Label Colorsとは独立してON/OFFできる。\n" +
            "対象のTMP_TextのRectTransformのPivotは(0.5, 0.5)にしておくこと(そうしないと中心以外を基準に拡大されて位置がズレて見える)。")]
        [SerializeField] private bool scaleSelectedLabel = false;

        [Tooltip("選択中の項目のテキストの拡大倍率。1.1〜1.2あたりがちょうど良い見た目になりやすい。")]
        [SerializeField] private float selectedLabelScale = 1.15f;

        [Tooltip("拡大・縮小の切り替えにかかる時間(秒)。0にすると即座に切り替わる。")]
        [SerializeField] private float labelScaleDuration = 0.12f;

        [Header("反転テキスト演出 (ペルソナ3風・任意)")]
        [Tooltip("Cursorの子にした、色反転表示専用のTMP_Text。CursorにMaskコンポーネントを付けて、この文字をカーソルの斜め形状で切り抜く。\n選択中の項目と同じ文字列・フォントサイズ・スタイルを自動でコピーし、Color欄で設定した色(白など)だけがカーソルの中に見える。未設定なら反転演出なしで動作する。")]
        [SerializeField] private TMP_Text invertedLabel;

        [Header("矢印アニメーション (ペルソナ3風・任意)")]
        [Tooltip("選択中の項目の左右に表示する矢印(▸ ◂ のようなImage/TMP_Text)のRectTransform。\n" +
            "毎フレーム Cursor Rect の位置 + Arrow Side Offset の座標へ自動で追従させるので、Cursorの子にする必要はない" +
            "(むしろCursorにMaskを付けている場合、子にすると矢印までCursorの斜め形状で切り抜かれて見えなくなるので、" +
            "CursorではなくCursorと同じ親の下に置くこと)。未設定なら矢印演出なしで動作する。")]
        [SerializeField] private RectTransform arrowLeft;
        [SerializeField] private RectTransform arrowRight;

        [Tooltip("カーソル中心から左右の矢印までの距離(px)。")]
        [SerializeField] private float arrowSideOffset = 140f;

        [Tooltip("矢印を、カーソルの上下中央からどれだけずらすか(px)。0でカーソルの中心の高さに揃う。")]
        [SerializeField] private float arrowVerticalOffset = 0f;

        [Tooltip("矢印が左右に小刻みに動く幅(px)。")]
        [SerializeField] private float arrowBounceDistance = 6f;

        [Tooltip("矢印が1秒間に往復する回数の目安(大きいほど素早くピコピコ動く)。")]
        [SerializeField] private float arrowBounceSpeed = 3f;

        [Tooltip("ONだと左右の矢印が逆方向に(選択項目に近づいたり離れたりするように)動く「呼吸」のような見た目になる。OFFだと両方とも同じ向きに揃って動く。")]
        [SerializeField] private bool mirrorArrows = true;

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
        private Coroutine itemCursorRoutine;
        private Coroutine labelScaleRoutine;
        private int previousLabelIndex = -1;
        private float nextInputTime;

        /// <summary>
        /// 項目ごとに個別のカーソルオブジェクトが(menuItemsと同じ数だけ)設定されているかどうか。
        /// ONの場合、Cursor Rectの移動アニメーションの代わりに、選択中の項目のオブジェクトだけを
        /// 表示する「個別カーソル」モードで動作する。
        /// </summary>
        private bool UseItemCursors => itemCursors != null && itemCursors.Length > 0 && menuItems != null && itemCursors.Length == menuItems.Length;

        /// <summary>
        /// 項目ごとに個別のスラッシュ演出オブジェクトが(menuItemsと同じ数だけ)設定されているかどうか。
        /// ONの場合、共通のSlash Flashを毎回移動させる代わりに、選択された項目に対応するスラッシュだけを
        /// そのオブジェクト自身の位置・角度のまま再生する「個別スラッシュ」モードで動作する。
        /// </summary>
        private bool UseItemSlashFlashes => itemSlashFlashes != null && itemSlashFlashes.Length > 0 && menuItems != null && itemSlashFlashes.Length == menuItems.Length;

        private void Start()
        {
            if (menuItems == null || menuItems.Length == 0)
            {
                Debug.LogWarning($"{nameof(MenuCursorSelector)}: menuItems が設定されていません。", this);
                enabled = false;
                return;
            }

            CurrentIndex = Mathf.Clamp(CurrentIndex, 0, menuItems.Length - 1);

            if (UseItemCursors) ShowItemCursorImmediate(CurrentIndex);
            else SnapCursorTo(CurrentIndex);

            HideAllSlashFlashesImmediate();

            if (scaleSelectedLabel) ShowLabelScaleImmediate(CurrentIndex);

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

            UpdateArrowBounce();
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
                if (UseItemCursors) ShowItemCursorImmediate(CurrentIndex);
                else SnapCursorTo(CurrentIndex);
                return;
            }

            onSelectionChanged?.Invoke(CurrentIndex);

            if (UseItemCursors)
            {
                if (itemCursorRoutine != null) StopCoroutine(itemCursorRoutine);
                itemCursorRoutine = StartCoroutine(ItemCursorPopRoutine(CurrentIndex));
            }
            else
            {
                if (moveRoutine != null) StopCoroutine(moveRoutine);
                moveRoutine = StartCoroutine(MoveCursorRoutine(menuItems[CurrentIndex]));
            }

            PlaySlashFlash(CurrentIndex);

            if (scaleSelectedLabel)
            {
                if (labelScaleRoutine != null) StopCoroutine(labelScaleRoutine);
                labelScaleRoutine = StartCoroutine(LabelScaleRoutine(CurrentIndex, previousLabelIndex));
                previousLabelIndex = CurrentIndex;
            }
        }

        /// <summary>
        /// 選択切替の瞬間に再生するスラッシュ演出を選び、再生する。
        /// 個別スラッシュモード(UseItemSlashFlashes)なら、選択中の項目に対応する
        /// オブジェクトをそのままの位置・角度で光らせる(座標移動はしない)。
        /// そうでなければ、従来通り共通のslashFlashを選択先の座標へ移動させてから光らせる。
        /// </summary>
        private void PlaySlashFlash(int index)
        {
            SlantedRect flash;
            RectTransform repositionTarget;

            if (UseItemSlashFlashes)
            {
                flash = itemSlashFlashes[index];
                repositionTarget = null; // 個別スラッシュはあらかじめ正しい位置に置いてある前提なので動かさない
            }
            else
            {
                flash = slashFlash;
                repositionTarget = menuItems[index];
            }

            if (flash == null) return;

            if (slashRoutine != null) StopCoroutine(slashRoutine);
            slashRoutine = StartCoroutine(SlashFlashRoutine(flash, repositionTarget));
        }

        /// <summary>
        /// 起動直後、スラッシュ演出をすべて非表示にしておく。
        /// slashFlash / itemSlashFlashes は「選択が切り替わった瞬間だけ」光る演出なので、
        /// Select()が一度も呼ばれていない起動直後の状態でシーン上にアクティブなまま
        /// 置かれていると、そのまま(アニメーションなしの完全表示で)見えてしまう。
        /// それを防ぐため、Start()時点で一旦すべて非表示にしておく。
        /// </summary>
        private void HideAllSlashFlashesImmediate()
        {
            if (slashFlash != null)
            {
                slashFlash.gameObject.SetActive(false);
            }

            if (itemSlashFlashes != null)
            {
                for (int i = 0; i < itemSlashFlashes.Length; i++)
                {
                    if (itemSlashFlashes[i] == null) continue;
                    itemSlashFlashes[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// アニメーションなしに、選択中の項目のMenu Labelだけを即座に拡大倍率にし、
        /// それ以外は等倍に戻す。Start()時の初期表示に使う。
        /// </summary>
        private void ShowLabelScaleImmediate(int index)
        {
            if (menuLabels == null) return;

            for (int i = 0; i < menuLabels.Length; i++)
            {
                if (menuLabels[i] == null) continue;
                float s = i == index ? selectedLabelScale : 1f;
                menuLabels[i].rectTransform.localScale = new Vector3(s, s, 1f);
            }

            previousLabelIndex = index;
        }

        /// <summary>
        /// 選択が切り替わった時、新しく選ばれた項目のテキストをselectedLabelScaleまで拡大し、
        /// 直前まで選ばれていた項目のテキストは等倍(1.0)まで縮小する。
        /// ペルソナ3のメニューで、選択中の文字だけ一回り大きく見える演出を狙ったもの。
        /// </summary>
        private IEnumerator LabelScaleRoutine(int newIndex, int oldIndex)
        {
            if (menuLabels == null) yield break;

            RectTransform newRt = (newIndex >= 0 && newIndex < menuLabels.Length && menuLabels[newIndex] != null) ? menuLabels[newIndex].rectTransform : null;
            RectTransform oldRt = (oldIndex >= 0 && oldIndex < menuLabels.Length && menuLabels[oldIndex] != null) ? menuLabels[oldIndex].rectTransform : null;

            if (labelScaleDuration <= 0f)
            {
                if (newRt != null) newRt.localScale = new Vector3(selectedLabelScale, selectedLabelScale, 1f);
                if (oldRt != null) oldRt.localScale = Vector3.one;
                yield break;
            }

            float newStart = newRt != null ? newRt.localScale.x : 1f;
            float oldStart = oldRt != null ? oldRt.localScale.x : 1f;
            float t = 0f;

            while (t < labelScaleDuration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / labelScaleDuration);

                if (newRt != null)
                {
                    float s = Mathf.Lerp(newStart, selectedLabelScale, u);
                    newRt.localScale = new Vector3(s, s, 1f);
                }

                if (oldRt != null)
                {
                    float s = Mathf.Lerp(oldStart, 1f, u);
                    oldRt.localScale = new Vector3(s, s, 1f);
                }

                yield return null;
            }

            if (newRt != null) newRt.localScale = new Vector3(selectedLabelScale, selectedLabelScale, 1f);
            if (oldRt != null) oldRt.localScale = Vector3.one;
        }

        private void SnapCursorTo(int index)
        {
            if (cursorRect == null || menuItems[index] == null) return;
            cursorRect.anchoredPosition = menuItems[index].anchoredPosition;
        }

        /// <summary>
        /// 個別カーソルモードで、アニメーションなしに選択中の項目のオブジェクトだけを即座に表示する
        /// (それ以外は非表示にする)。Start()時の初期表示や、同じ項目を選び直した時に使う。
        /// </summary>
        private void ShowItemCursorImmediate(int index)
        {
            for (int i = 0; i < itemCursors.Length; i++)
            {
                if (itemCursors[i] == null) continue;
                itemCursors[i].gameObject.SetActive(i == index);
                itemCursors[i].localScale = Vector3.one;
            }
        }

        /// <summary>
        /// 個別カーソルモードで選択が切り替わった時に、選択中の項目のオブジェクトだけを表示し、
        /// 「一瞬拡大してから等倍に戻る」ポップアニメーションを再生する。
        /// 項目ごとに位置・大きさ・形・色が違っていても、表示/非表示を切り替えるだけなので
        /// そのまま使える。
        /// </summary>
        private IEnumerator ItemCursorPopRoutine(int index)
        {
            for (int i = 0; i < itemCursors.Length; i++)
            {
                if (itemCursors[i] == null) continue;
                itemCursors[i].gameObject.SetActive(i == index);
            }

            RectTransform target = itemCursors[index];
            if (target == null) yield break;

            if (itemCursorPopDuration <= 0f)
            {
                target.localScale = Vector3.one;
                yield break;
            }

            float t = 0f;
            while (t < itemCursorPopDuration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / itemCursorPopDuration);
                float scale = Mathf.Lerp(itemCursorPopScale, 1f, u);
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        /// <summary>
        /// 選択中の項目を挟む左右の矢印を、Cursorの現在位置(移動アニメ中も含む)を
        /// 基準に毎フレーム追従させつつ、左右に小刻みにバウンドさせる。
        /// ペルソナ3のメニューで矢印がピコピコ動いているような見た目を狙ったもの。
        /// arrowLeft/arrowRightはCursorの子にしない前提(Maskで切り抜かれてしまうため)なので、
        /// ここで座標を直接計算して追従させている。
        /// </summary>
        private void UpdateArrowBounce()
        {
            if (cursorRect == null) return;
            if (arrowLeft == null && arrowRight == null) return;

            float wave = Mathf.Sin(Time.unscaledTime * arrowBounceSpeed * Mathf.PI * 2f) * arrowBounceDistance;
            Vector2 center = cursorRect.anchoredPosition;

            if (arrowLeft != null)
            {
                float bounce = mirrorArrows ? -wave : wave;
                arrowLeft.anchoredPosition = new Vector2(center.x - arrowSideOffset + bounce, center.y + arrowVerticalOffset);
            }

            if (arrowRight != null)
            {
                arrowRight.anchoredPosition = new Vector2(center.x + arrowSideOffset + wave, center.y + arrowVerticalOffset);
            }
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
        /// 選択切替の瞬間に、斜めの白いバー(flash)を
        /// 幅0→最大まで一気に走らせてからフェードアウトさせる演出。
        /// 「斜めスラッシュが切り込むような」ペルソナ風の切替感を出す。
        ///
        /// repositionTargetがnullでなければ、再生の直前にflashをその座標へ瞬間移動させる
        /// (共通のslashFlashを使い回す場合に必要な処理)。
        /// repositionTargetがnullの場合は座標を一切変更しない
        /// (個別スラッシュモードで、あらかじめ項目ごとに正しい位置へ置いてある場合)。
        /// </summary>
        private IEnumerator SlashFlashRoutine(SlantedRect flash, RectTransform repositionTarget)
        {
            RectTransform rt = flash.rectTransform;
            float baseWidth = slashFullWidth;

            // 共通のslashFlashは自分では選択位置を追いかけないので、
            // 再生の直前に選択先の項目の座標へ瞬間移動させておく。
            // (これをしないと、エディタで最初に置いた位置に固定されたままになる)
            // 個別スラッシュモードの場合はrepositionTargetがnullなので、ここはスキップされる
            // (あらかじめ対応する項目にぴったり合う位置・角度で個別に配置してある前提のため)。
            if (repositionTarget != null)
            {
                rt.anchoredPosition = repositionTarget.anchoredPosition;
            }

            Color startColor = slashColor;
            startColor.a = 1f;

            flash.color = startColor;
            flash.gameObject.SetActive(true);
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
                flash.color = c;
                yield return null;
            }

            flash.gameObject.SetActive(false);
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

        /// <summary>
        /// 選択中の項目と同じ文字列・フォントサイズ・スタイルをinvertedLabelにコピーする。
        /// invertedLabelはCursorの子にして、Cursor側のMaskで斜め形状に切り抜いておくことで、
        /// 「カーソルの中だけ文字色が反転して見える」というペルソナ3風の見た目になる。
        /// (invertedLabel自体の色はInspectorで設定した固定色のまま変更しない)
        /// </summary>
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