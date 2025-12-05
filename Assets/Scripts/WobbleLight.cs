using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class WobbleLight : MonoBehaviour
{
    [Header("스케일 흔들림 설정")]
    public bool wobbleX = false;          // X축 흔들림
    public bool wobbleY = true;           // Y축 흔들림

    public float baseScaleX = 1f;         // X 기본 스케일
    public float baseScaleY = 1f;         // Y 기본 스케일

    public float wobbleAmountX = 0.1f;    // X 축 흔들림 강도
    public float wobbleAmountY = 0.2f;    // Y 축 흔들림 강도

    public float wobbleSpeed = 5f;        // 흔들림 속도

    [Header("알파(투명도) 흔들림 설정")]
    public bool useAlpha = false;         // 알파 애니메이션 사용 여부
    [Range(0f, 1f)]
    public float minAlpha = 0.5f;         // 최소 알파값
    [Range(0f, 1f)]
    public float maxAlpha = 1f;           // 최대 알파값
    public float alphaSpeed = 2f;         // 알파 변화 속도

    private Transform tr;
    private SpriteRenderer sr;

    void Awake()
    {
        tr = transform;
        sr = GetComponent<SpriteRenderer>();

        // 현재 스케일을 기본값으로 자동 설정
        Vector3 s = tr.localScale;
        if (Mathf.Approximately(baseScaleX, 1f)) baseScaleX = s.x;
        if (Mathf.Approximately(baseScaleY, 1f)) baseScaleY = s.y;
    }

    void Update()
    {
        UpdateScaleWobble();
        UpdateAlphaWobble();
    }

    void UpdateScaleWobble()
    {
        Vector3 scale = tr.localScale;
        float t = Time.time * wobbleSpeed;

        // X축 흔들림 적용
        if (wobbleX)
        {
            float factorX = 1f + Mathf.Sin(t) * wobbleAmountX;
            scale.x = baseScaleX * factorX;
        }
        else
        {
            scale.x = baseScaleX;
        }

        // Y축 흔들림 적용
        if (wobbleY)
        {
            float factorY = 1f + Mathf.Sin(t) * wobbleAmountY;
            scale.y = baseScaleY * factorY;
        }
        else
        {
            scale.y = baseScaleY;
        }

        tr.localScale = scale;
    }

    void UpdateAlphaWobble()
    {
        if (!useAlpha || sr == null)
            return;

        float t = Time.time * alphaSpeed;

        // 0~1 사이 왕복
        float lerp = (Mathf.Sin(t) + 1f) * 0.5f;
        float finalAlpha = Mathf.Lerp(minAlpha, maxAlpha, lerp);

        Color c = sr.color;
        c.a = finalAlpha;
        sr.color = c;
    }
}
