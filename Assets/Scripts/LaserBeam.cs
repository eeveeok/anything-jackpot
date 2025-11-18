using UnityEngine;

public class LaserBeam : MonoBehaviour
{
    [Header("레이저 속성")]
    public Vector2 direction = Vector2.right;
    public float damage = 10f;
    public float spawnDistance = 0.7f; // 캐릭터 중심에서 레이저가 생성될 거리

    [Header("이펙트")]
    public GameObject hitEffect;

    [HideInInspector]
    public Transform characterCenter; // 캐릭터 중심 Transform
    [HideInInspector]
    public Camera mainCamera;

    private SpriteRenderer spriteRenderer;
    private bool hasHit = false;
    private Vector2 hitPoint;
    private Vector2 actualLaserStartPoint;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        FireLaser();
    }

    void Update()
    {
        if (characterCenter != null)
        {
            FireLaser();
        }
    }

    void FireLaser()
    {
        // 마우스 위치를 기반으로 방향 계산
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
        Vector2 centerPos = new Vector2(characterCenter.position.x, characterCenter.position.y);

        // 방향 업데이트
        direction = (mousePos2D - centerPos).normalized;

        // 캐릭터 중심에서 spawnDistance만큼 떨어진 위치에서 레이저 시작
        actualLaserStartPoint = centerPos + direction * spawnDistance;

        // 화면 끝까지의 거리 계산
        float maxDistance = CalculateMaxDistanceToScreenEdge(actualLaserStartPoint, direction);

        // 레이캐스트로 충돌 검사
        RaycastHit2D hit = Physics2D.Raycast(actualLaserStartPoint, direction, maxDistance);

        if (hit.collider != null)
        {
            hasHit = true;
            hitPoint = hit.point;

            if (hitEffect != null && !hit.collider.CompareTag("Player"))
            {
                Instantiate(hitEffect, hitPoint, Quaternion.identity);
            }
        }
        else
        {
            hasHit = false;
            // 화면 끝까지 레이저 발사
            hitPoint = actualLaserStartPoint + direction * maxDistance;
        }

        UpdateLaserSprite();
    }

    float CalculateMaxDistanceToScreenEdge(Vector2 startPoint, Vector2 dir)
    {
        // 카메라의 화면 경계 계산
        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        Vector3 cameraPos = mainCamera.transform.position;
        Vector2 cameraMin = new Vector2(cameraPos.x - cameraWidth / 2f, cameraPos.y - cameraHeight / 2f);
        Vector2 cameraMax = new Vector2(cameraPos.x + cameraWidth / 2f, cameraPos.y + cameraHeight / 2f);

        // 레이저가 화면 경계와 교차하는 지점 계산
        float maxDistance = 100f; // 기본값

        // 각 경계와의 교차점 계산
        if (Mathf.Abs(dir.x) > 0.001f)
        {
            // 좌우 경계
            float distToLeft = (cameraMin.x - startPoint.x) / dir.x;
            float distToRight = (cameraMax.x - startPoint.x) / dir.x;

            // 양의 방향 거리만 고려
            if (distToLeft > 0) maxDistance = Mathf.Min(maxDistance, distToLeft);
            if (distToRight > 0) maxDistance = Mathf.Min(maxDistance, distToRight);
        }

        if (Mathf.Abs(dir.y) > 0.001f)
        {
            // 상하 경계
            float distToBottom = (cameraMin.y - startPoint.y) / dir.y;
            float distToTop = (cameraMax.y - startPoint.y) / dir.y;

            // 양의 방향 거리만 고려
            if (distToBottom > 0) maxDistance = Mathf.Min(maxDistance, distToBottom);
            if (distToTop > 0) maxDistance = Mathf.Min(maxDistance, distToTop);
        }

        // 약간의 여유를 두어 화면을 살짝 벗어나도록 설정
        return maxDistance + 0.5f;
    }

    void UpdateLaserSprite()
    {
        if (spriteRenderer != null)
        {
            // 레이저 길이 계산
            float distance = Vector2.Distance(actualLaserStartPoint, hitPoint);

            // 스프라이트의 기본 크기를 고려한 스케일 조정
            float baseWidth = spriteRenderer.sprite.bounds.size.x;
            float scaleX = distance / baseWidth;

            // 스프라이트 크기 조정
            transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);

            // 레이저 각도 설정
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // 레이저 위치 설정
            Vector2 middlePoint = (actualLaserStartPoint + hitPoint) / 2f;
            transform.position = new Vector3(middlePoint.x, middlePoint.y, characterCenter.position.z);

            // 디버그 시각화
            Debug.DrawRay(actualLaserStartPoint, direction * distance, Color.red);
            Debug.DrawLine(characterCenter.position, actualLaserStartPoint, Color.blue);
        }
    }

    // 화면 경계 시각화 (디버깅용)
    void OnDrawGizmos()
    {
        if (mainCamera != null && Application.isPlaying)
        {
            Gizmos.color = Color.green;

            // 카메라 화면 경계 그리기
            float cameraHeight = 2f * mainCamera.orthographicSize;
            float cameraWidth = cameraHeight * mainCamera.aspect;
            Vector3 cameraPos = mainCamera.transform.position;

            Vector3 bottomLeft = new Vector3(cameraPos.x - cameraWidth / 2f, cameraPos.y - cameraHeight / 2f, 0);
            Vector3 bottomRight = new Vector3(cameraPos.x + cameraWidth / 2f, cameraPos.y - cameraHeight / 2f, 0);
            Vector3 topLeft = new Vector3(cameraPos.x - cameraWidth / 2f, cameraPos.y + cameraHeight / 2f, 0);
            Vector3 topRight = new Vector3(cameraPos.x + cameraWidth / 2f, cameraPos.y + cameraHeight / 2f, 0);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
    }
}