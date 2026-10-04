using UnityEngine;

/// <summary>
/// ラウンドが切り替わるタイミングで、P1・P2のキャラクターを開始位置に戻す汎用スクリプト。
/// MatchScoreManagerの On Round Continue イベントにこのスクリプトの ResetPositions() を
/// 登録しておくことで、「ラウンド2が始まる直前に位置をリセットする」が実現できる。
///
/// P1・P2どちらも1つのスクリプトで管理できるように、Inspectorで配列指定する方式にしている。
///
/// 開始位置の決め方は2通りから選べる:
///   ・Capture On Start (ON、デフォルト): 戦闘が始まる瞬間(後述のCaptureInitialPositions()が
///     呼ばれた時点)のTransformの位置・回転・スケールを記録し、それをラウンド切り替え時に
///     復元する。今シーンに配置した位置がそのまま開始位置になるので、特別な座標入力は不要
///   ・Capture On Start (OFF): Custom Positionで指定した座標に固定でリセットする。
///     「P1は必ずX=-3、P2は必ずX=3」のように毎ラウンド完全に同じ位置へ戻したい場合に使う
///
/// Rigidbody2Dを使っている場合は、残っている移動速度(ノックバックの慣性など)も
/// 一緒にリセットされるよう、Rigidbody2Dの参照も指定できる(任意)。
///
/// 注意(重要): 位置の記録はこのスクリプトのStart()では行わない。
///   キャラクター選択後にスポーン/配置される場合、このスクリプトのStart()時点では
///   まだキャラクターが正しい位置に置かれていない可能性があるため。
///   代わりに CaptureInitialPositions() を、キャラクターの配置が確実に終わった後の
///   タイミング(例: RoundAnnouncementControllerのOn Fight Start、つまり
///   ラウンド1のROUND演出が終わって戦闘が始まる瞬間)に呼び出すこと。
///   一度記録した後は(2ラウンド目以降のOn Fight Startで再度呼ばれても)上書きされない。
///
/// 使い方:
///   1. シーン内の常にアクティブなオブジェクトにこのスクリプトをアタッチ
///      (MatchScoreManagerと同じ場所でよい)
///   2. Playersの要素0=P1、要素1=P2として、Targetに各キャラクターのTransformを割り当てる
///   3. 必要であれば、各キャラクターのRigidbody2Dも割り当てる(残存速度リセット用)
///   4. RoundAnnouncementControllerのOn Fight Startイベントに、このスクリプトの
///      CaptureInitialPositions()を登録する(初回のみ記録され、以降は何もしない)
///   5. MatchScoreManagerのOn Round Continueイベントに、このスクリプトの
///      ResetPositions()を登録する
/// </summary>
public class PlayerPositionResetter : MonoBehaviour
{
    [System.Serializable]
    public class PlayerPositionEntry
    {
        [Tooltip("Inspector上で分かりやすくするためのラベル(P1 / P2など)。処理には使用しない")]
        public string label;

        [Tooltip("位置をリセットしたいキャラクターのTransform")]
        public Transform target;

        [Tooltip("キャラクターにRigidbody2Dがついている場合はここに割り当てる(任意)。" +
                 "割り当てると、リセット時に残っている移動速度(ノックバックの慣性など)も" +
                 "一緒に0にする")]
        public Rigidbody2D rigidbody2D;

        [Tooltip("ONの場合、シーン再生開始時のTransformの位置・回転・スケールを自動で記録し、" +
                 "ラウンド切り替え時にはその位置へ戻す。" +
                 "OFFの場合はCustom Positionで指定した座標に固定でリセットする")]
        public bool captureOnStart = true;

        [Tooltip("Capture On StartがOFFの場合に使う、固定の開始位置(ワールド座標)")]
        public Vector3 customPosition;

        [Tooltip("Capture On StartがOFFの場合に使う、固定の開始回転(度、Z軸)")]
        public float customRotationZ;

        [Tooltip("Capture On StartがOFFの場合に使う、固定の開始スケール。" +
                 "キャラクターを左右反転させる仕組み(localScale.xの符号)を使っている場合、" +
                 "ここで最初の向きを指定できる")]
        public Vector3 customScale = Vector3.one;

        // captureOnStartがONの場合に、Start()で記録される実際の値
        [HideInInspector] public Vector3 capturedPosition;
        [HideInInspector] public float capturedRotationZ;
        [HideInInspector] public Vector3 capturedScale;
        [HideInInspector] public bool captured;
    }

    [Tooltip("要素0=P1、要素1=P2、という想定。3人以上に増やしても動作する")]
    [SerializeField] private PlayerPositionEntry[] players = new PlayerPositionEntry[2];

    /// <summary>
    /// 現在のP1・P2の位置・回転・スケールを「開始位置」として記録する。
    /// キャラクターの配置(スポーン/キャラ選択後の初期配置など)が確実に終わった後の
    /// タイミングで、一度だけ呼び出すこと(例: RoundAnnouncementControllerの
    /// On Fight Startイベントに登録しておけば、ラウンド1の演出が終わって戦闘が
    /// 始まる瞬間に自動で記録される)。
    /// すでに記録済みのエントリは上書きしないため、2ラウンド目以降のOn Fight Startで
    /// 重ねて呼ばれても問題ない。
    /// </summary>
    public void CaptureInitialPositions()
    {
        foreach (PlayerPositionEntry entry in players)
        {
            if (entry == null || entry.target == null) continue;
            if (!entry.captureOnStart) continue;
            if (entry.captured) continue; // 既に記録済みなら上書きしない

            entry.capturedPosition = entry.target.position;
            entry.capturedRotationZ = entry.target.eulerAngles.z;
            entry.capturedScale = entry.target.localScale;
            entry.captured = true;
        }
    }

    /// <summary>
    /// 全プレイヤーの位置・回転・スケール・移動速度を開始状態に戻す。
    /// MatchScoreManagerのOn Round ContinueイベントからInspectorで呼び出す想定。
    /// </summary>
    public void ResetPositions()
    {
        foreach (PlayerPositionEntry entry in players)
        {
            ResetOne(entry);
        }
    }

    /// <summary>
    /// 指定した1人分(0=P1, 1=P2)だけを開始位置に戻す。
    /// </summary>
    public void ResetPosition(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= players.Length) return;
        ResetOne(players[playerIndex]);
    }

    private void ResetOne(PlayerPositionEntry entry)
    {
        if (entry == null || entry.target == null) return;

        Vector3 position;
        float rotationZ;
        Vector3 scale;

        if (entry.captureOnStart && entry.captured)
        {
            position = entry.capturedPosition;
            rotationZ = entry.capturedRotationZ;
            scale = entry.capturedScale;
        }
        else
        {
            position = entry.customPosition;
            rotationZ = entry.customRotationZ;
            scale = entry.customScale;
        }

        entry.target.position = position;
        entry.target.rotation = Quaternion.Euler(0f, 0f, rotationZ);
        entry.target.localScale = scale;

        if (entry.rigidbody2D != null)
        {
            entry.rigidbody2D.linearVelocity = Vector2.zero;
            entry.rigidbody2D.angularVelocity = 0f;
        }
    }
}