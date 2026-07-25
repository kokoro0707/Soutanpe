using UnityEngine;
using System.Collections;

public class CharacterSelectInitializer : MonoBehaviour
{
    private IEnumerator Start()
    {
        // 1フレーム待つ（シーン生成完了待ち）
        yield return null;

        // タイトル→メインメニューと同じフェードイン
        FadeManager.Instance.StartFadeIn();
    }
}