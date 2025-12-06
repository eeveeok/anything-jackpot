using UnityEngine;

public class CannonEffect : MonoBehaviour
{
    [Header("이펙트 설정")]
    public Vector2 direction;
    public float damage = 10f;
    public float range = 3f;
    public float width = 1f;
    public float duration = 0.3f;
    public LayerMask damageLayers;
    public Transform origin;

    [Header("타일 파괴 설정")]
    public float tileBreakRadius = 2.5f; // 타일 파괴 반경
    public float raycastDistance = 5f;   // 레이캐스트 거리
    public bool debugVisualization = true; // 디버그 시각화

    private float timer = 0f;
    private bool hasDamaged = false;
    private Dictionary<Tilemap, Dictionary<Vector3Int, int>> tileHealthMap = new Dictionary<Tilemap, Dictionary<Vector3Int, int>>();
    private Vector2 impactPoint; // 충돌 지점 저장

    void Start()
    {
        // LineRenderer 설정
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.colorGradient = colorGradient;

            // 초기 위치 설정
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.position + (Vector3)direction * range);
        }

        // 충격 입자 효과
        if (impactParticles != null)
        {
            impactParticles.transform.position = transform.position + (Vector3)direction * range;
            impactParticles.Play();
        }

        // 데미지 적용
        ApplyDamage();
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 이펙트 애니메이션
        if (lineRenderer != null)
        {
            float progress = timer / duration;

            // 너비 변화
            float currentWidth = width * widthCurve.Evaluate(progress);
            lineRenderer.startWidth = currentWidth;
            lineRenderer.endWidth = currentWidth * 0.5f;

            // 알파값 감소
            Gradient gradient = lineRenderer.colorGradient;
            GradientAlphaKey[] alphaKeys = gradient.alphaKeys;
            for (int i = 0; i < alphaKeys.Length; i++)
            {
                alphaKeys[i].alpha = Mathf.Lerp(1f, 0f, progress);
            }
            gradient.SetKeys(gradient.colorKeys, alphaKeys);
            lineRenderer.colorGradient = gradient;
        }

        // 지속시간 종료 시 파괴
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }

    void ApplyDamage()
    {
        if (hasDamaged) return;
        hasDamaged = true;

        // 원뿔 형태의 충돌체 탐지
        //float angle = 30f; // 30도 각도

        // 주변 적 탐지
        //Collider2D[] colliders = Physics2D.OverlapCircleAll(origin.position, range * 1.5f, damageLayers);

        //foreach (Collider2D collider in colliders)
        //{
        //    // 충돌체의 방향 계산
        //    Vector2 targetDir = (collider.transform.position - origin.position).normalized;

        //    // 발사 방향과의 각도 차이 계산
        //    float angleDifference = Vector2.Angle(direction, targetDir);

        //    // 각도 내에 있고 거리 내에 있는지 확인
        //    float distance = Vector2.Distance(origin.position, collider.transform.position);

        //    if (angleDifference <= angle && distance <= range)
        //    {
        //        // 보스 데미지 처리
        //        Stage1Boss boss = collider.GetComponent<Stage1Boss>();
        //        if (boss != null)
        //        {
        //            boss.ApplyDamage(damage);
        //        }
        //    }
        //}
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, direction * range);

        if (hit.collider != null)
        {
            impactPoint = hit.point;
            Debug.Log($"CannonEffect hit: {hit.collider.name} at {impactPoint}");

            // 2. 충돌한 객체가 Breakable 태그를 가지고 있는지 확인
            if (hit.collider.CompareTag("Breakable"))
            {
                // 3. 타일맵인지 확인
                Tilemap tilemap = hit.collider.GetComponent<Tilemap>();
                if (tilemap != null)
                {
                    // 4. 충돌점의 타일 위치 계산
                    Vector3 hitPosition = hit.point - (Vector2)hit.normal * 0.01f; // 약간 안쪽으로
                    Vector3Int cellPosition = tilemap.WorldToCell(hitPosition);

                    // 5. 타일 파괴
                    BreakTilesInRadius(tilemap, cellPosition);
                }
                else
                {
                    // 타일맵이 아닌 일반 Breakable 오브젝트 파괴
                    Debug.Log($"Destroying breakable object: {hit.collider.name}");
                    Destroy(hit.collider.gameObject);
                }
            }
        }
        else
        {
            // 충돌이 없으면 최대 거리 지점을 impactPoint로 설정
            impactPoint = (Vector2)origin.position + direction * raycastDistance;
        }
    }

    void DrawConeGizmo(Vector2 origin, Vector2 direction, float range, float angle)
    {
        Vector2 leftBound = Quaternion.Euler(0, 0, angle) * direction * range;
        Vector2 rightBound = Quaternion.Euler(0, 0, -angle) * direction * range;

        Gizmos.DrawRay(origin, leftBound);
        Gizmos.DrawRay(origin, rightBound);

        // 호 그리기
        int segments = 20;
        float angleStep = angle * 2 / segments;
        Vector2 prevPoint = origin + leftBound;

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = angle - i * angleStep;
            Vector2 currentDir = Quaternion.Euler(0, 0, currentAngle) * direction;
            Vector2 currentPoint = origin + currentDir * range;

            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
    }
}