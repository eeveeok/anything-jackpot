using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CannonEffect : MonoBehaviour
{
    [Header("이펙트 설정")]
    public Vector2 direction;
    public float range = 3f;
    public float width = 1f;
    public float duration = 0.6f;
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
        if (origin == null)
        {
            origin = transform;
        }

        // 타일 파괴 로직 실행
        BreakTilesAlongPath();
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 디버그 시각화
        if (debugVisualization && Application.isPlaying)
        {
            DrawDebugVisualization();
        }

        // 지속시간 종료 시 파괴
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }

    void BreakTilesAlongPath()
    {
        if (hasDamaged) return;

        // 1. 레이캐스트로 충돌점 찾기
        RaycastHit2D hit = Physics2D.Raycast(
            origin.position,
            direction,
            raycastDistance,
            damageLayers
        );

        if (debugVisualization)
        {
            Debug.DrawRay(origin.position, direction * raycastDistance, Color.yellow, 1f);
        }

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

    void BreakTilesInRadius(Tilemap tilemap, Vector3Int centerCell)
    {
        if (!tileHealthMap.ContainsKey(tilemap))
        {
            tileHealthMap[tilemap] = new Dictionary<Vector3Int, int>();
        }

        Dictionary<Vector3Int, int> healthDict = tileHealthMap[tilemap];

        // 반경 내의 모든 셀 확인
        int radius = Mathf.CeilToInt(tileBreakRadius);

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int cell = centerCell + new Vector3Int(x, y, 0);

                // 원형 반경 체크
                Vector3 cellWorldPos = tilemap.CellToWorld(cell) + tilemap.cellSize * 0.5f;
                Vector3 centerWorldPos = tilemap.CellToWorld(centerCell) + tilemap.cellSize * 0.5f;
                float distance = Vector2.Distance(cellWorldPos, centerWorldPos);

                if (distance <= tileBreakRadius)
                {
                    // 해당 셀에 타일이 있는지 확인
                    TileBase tile = tilemap.GetTile(cell);
                    if (tile != null)
                    {
                        // 타일 체력 관리
                        if (!healthDict.ContainsKey(cell))
                        {
                            healthDict[cell] = 1; // 기본 체력 1
                        }

                        // 데미지 적용
                        healthDict[cell]--;

                        // 체력이 0 이하이면 타일 제거
                        if (healthDict[cell] <= 0)
                        {
                            tilemap.SetTile(cell, null);
                            healthDict.Remove(cell);
                            Debug.Log($"CannonEffect: Tile destroyed at cell: {cell}");
                        }
                        else
                        {
                            Debug.Log($"CannonEffect: Tile damaged at cell: {cell}, health: {healthDict[cell]}");
                        }
                    }
                }
            }
        }
    }

    void DrawDebugVisualization()
    {
        // 레이캐스트 경로
        Debug.DrawLine(origin.position, impactPoint, Color.yellow);

        // 충돌점에 원 그리기
        DebugDrawCircle(impactPoint, tileBreakRadius, 32, Color.red);

        // 타일 파괴 범위 시각화
        if (tileBreakRadius > 0)
        {
            DebugDrawCircle(impactPoint, tileBreakRadius, 32, new Color(1f, 0.5f, 0f, 0.5f));
        }
    }

    void DebugDrawCircle(Vector2 center, float radius, int segments, Color color)
    {
        float angle = 0f;
        float angleIncrement = 360f / segments;
        Vector2 prevPoint = center + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;

        for (int i = 1; i <= segments; i++)
        {
            angle += angleIncrement;
            Vector2 nextPoint = center + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
            Debug.DrawLine(prevPoint, nextPoint, color);
            prevPoint = nextPoint;
        }
    }

    private void OnDrawGizmos()
    {
        if (!debugVisualization || !Application.isPlaying) return;

        // 타일 파괴 범위 시각화 (에디터에서도 보이게)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(impactPoint, tileBreakRadius);

        // 방향 시각화
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin.position, direction * 2f);
    }
}