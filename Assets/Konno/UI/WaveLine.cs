using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WaveLine : MonoBehaviour
{
    [Header("”gü‚Ì”ÍˆÍ")]
    public float lineWidth = 8f;      // lŠpŒ`‚Ì‰¡•‚É‡‚í‚¹‚é
    public int segmentCount = 50;     // ”g‚ÌŠŠ‚ç‚©‚³(‘½‚¢‚Ù‚ÇŠŠ‚ç‚©)

    [Header("”g‚ÌŒ`ó")]
    public float amplitude = 0.2f;    // ”g‚Ì‚‚³
    public float wavelength = 2f;     // ”g‚ÌŠÔŠu(¬‚³‚¢‚Ù‚Ç”g‚ª×‚©‚¢)
    public float speed = 1.5f;        // ”g‚ª—¬‚ê‚é‘¬‚³

    [Header("•¡”‚Ì”g‚ğd‚Ë‚Äƒ‰ƒ“ƒ_ƒ€Š´‚ğo‚·")]
    public float amplitude2 = 0.1f;
    public float wavelength2 = 0.7f;
    public float speed2 = -2.5f;      // ‹t•ûŒü‚É—¬‚·‚Æ©‘R‚ÉŒ©‚¦‚é

    private LineRenderer lr;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = segmentCount;
        lr.useWorldSpace = false;
    }

    void Update()
    {
        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / (segmentCount - 1);
            float x = Mathf.Lerp(-lineWidth / 2f, lineWidth / 2f, t);

            // 2‚Â‚Ì”g‚ğd‚Ë‚Ä©‘R‚È—h‚ê‚É‚·‚é
            float y = Mathf.Sin((x / wavelength) + Time.time * speed) * amplitude
                     + Mathf.Sin((x / wavelength2) + Time.time * speed2) * amplitude2;

            lr.SetPosition(i, new Vector3(x, y, 0));
        }
    }
}