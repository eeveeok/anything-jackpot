using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LaserBeam : MonoBehaviour
{
    [Header("레이저 속성")]
    public float damage = 10f;
    public float damageInterval = 0.2f;
    public float spawnDistance = 0.7f;
    public LayerMask layerMask;

    [Header("이펙트 설정")]
    public GameObject hitEffect;
    public GameObject[] fireEffect;
    public int maxEffects = 20;
    public float effectLifetime = 0.3f;

    [Header("이펙트 생성 설정")]
    [Tooltip("한 번에 생성되는 이펙트 개수")]
    public int effectsPerSpawn = 3;
    [Tooltip("이펙트가 생성되는 반경")]
    public float effectSpawnRadius = 0.5f;
    [Tooltip("이펙트 생성 간격 (0이면 데미지 간격과 동일)")]
    public float effectSpawnInterval = 0f;
    [Tooltip("이펙트 생성 확률 (0~1)")]
    [Range(0f, 1f)]
    public float effectSpawnChance = 1f;
    [Tooltip("이펙트의 랜덤 크기 범위")]
    public Vector2 effectSizeRange = new Vector2(0.8f, 1.2f);
    [Tooltip("이펙트의 랜덤 회전")]
    public bool randomRotation = true;

    [Header("스프라이트 설정")]
    public float minLaserLength = 0.1f;
    public float maxLaserLength = 200f;

    [HideInInspector]
    public Transform characterCenter;
    [HideInInspector]
    public Camera mainCamera;
    [HideInInspector]
    public Vector2 direction;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Vector2 actualLaserStartPoint;
    private Vector2 hitPoint;

    // 데미지 관련
    private HashSet<GameObject> damagedObjects = new HashSet<GameObject>();
    private bool isActive = true;

    // 이펙트 풀링 시스템
    private Queue<GameObject> effectPool;
    private HashSet<GameObject> activeEffects; // 활성 이펙트 추적
    private List<Coroutine> activeCoroutines; // 활성 코루틴 추적
    private bool isCleaningUp = false;

    // 레이저 충돌 정보
    private RaycastHit2D[] hits = new RaycastHit2D[10];

    Dictionary<Vector3Int, int> tileHealth = new Dictionary<Vector3Int, int>();

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        fireEffect = new GameObject[2];

        // 이펙트 시스템 초기화
        InitializeEffectSystem();

        StartCoroutine(DamageRoutine());

        // 이펙트 생성 코루틴 시작 (데미지와 별도로 실행)
        if (effectSpawnInterval > 0)
        {
            StartCoroutine(EffectSpawnRoutine());
        }
    }

    void InitializeEffectSystem()
    {
        effectPool = new Queue<GameObject>();
        activeEffects = new HashSet<GameObject>();
        activeCoroutines = new List<Coroutine>();

        // 초기 풀 생성
        for (int i = 0; i < Mathf.Min(maxEffects / 2, maxEffects); i++)
        {
            CreatePooledEffect();
        }

        // 정리 코루틴 시작
        StartCoroutine(CleanupRoutine());
    }

    void Update()
    {
        if (characterCenter != null && isActive)
        {
            UpdateLaser();
        }
    }

    void UpdateLaser()
    {
        // 마우스 위치를 기반으로 방향 계산
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
        Vector2 centerPos = new Vector2(characterCenter.position.x, characterCenter.position.y - 0.1f);

        // 방향 업데이트
        direction = (mousePos2D - centerPos).normalized;

        // 캐릭터 중심에서 spawnDistance만큼 떨어진 위치에서 레이저 시작
        actualLaserStartPoint = centerPos + direction * spawnDistance;

        // 화면 끝까지의 거리 계산
        float maxDistance = CalculateScreenEdgeDistance(actualLaserStartPoint, direction);

        // 레이캐스트로 충돌 검사
        RaycastHit2D hit = PerformLaserCast(actualLaserStartPoint, direction, maxDistance);

        if (hit.collider != null)
        {
            hitPoint = hit.point;
        }
        else
        {
            hitPoint = actualLaserStartPoint + direction * maxDistance;
        }

        //fireEffect[0].transform.position = new Vector2(actualLaserStartPoint.x, actualLaserStartPoint.y);
        //fireEffect[1].transform.position = new Vector2(hitPoint.x, hitPoint.y);

        UpdateLaserSprite();
    }

    float CalculateScreenEdgeDistance(Vector2 startPoint, Vector2 dir)
    {
        // 카메라의 orthographicSize와 aspect ratio를 사용하여 화면 경계 계산
        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        Vector3 cameraPos = mainCamera.transform.position;
        Vector2 cameraMin = new Vector2(cameraPos.x - cameraWidth / 2f, cameraPos.y - cameraHeight / 2f);
        Vector2 cameraMax = new Vector2(cameraPos.x + cameraWidth / 2f, cameraPos.y + cameraHeight / 2f);

        float maxDistance = maxLaserLength;

        // 각 방향별로 화면 경계까지의 거리 계산
        if (Mathf.Abs(dir.x) > 0.001f)
        {
            float distToLeft = (cameraMin.x - startPoint.x) / dir.x;
            float distToRight = (cameraMax.x - startPoint.x) / dir.x;

            // 양의 방향 거리만 고려
            if (distToLeft > 0) maxDistance = Mathf.Min(maxDistance, distToLeft);
            if (distToRight > 0) maxDistance = Mathf.Min(maxDistance, distToRight);
        }

        if (Mathf.Abs(dir.y) > 0.001f)
        {
            float distToBottom = (cameraMin.y - startPoint.y) / dir.y;
            float distToTop = (cameraMax.y - startPoint.y) / dir.y;

            // 양의 방향 거리만 고려
            if (distToBottom > 0) maxDistance = Mathf.Min(maxDistance, distToBottom);
            if (distToTop > 0) maxDistance = Mathf.Min(maxDistance, distToTop);
        }

        // 최소 길이 보장
        return Mathf.Max(maxDistance, minLaserLength);
    }

    RaycastHit2D PerformLaserCast(Vector2 origin, Vector2 dir, float maxDistance)
    {
        // 레이어 마스크 설정
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, maxDistance, layerMask);

        // 디버그 시각화
        Debug.DrawRay(origin, dir * maxDistance, Color.red);
        Debug.DrawLine(characterCenter.position, actualLaserStartPoint, Color.blue);

        return hit;
    }

    IEnumerator DamageRoutine()
    {
        while (isActive)
        {
            yield return new WaitForSeconds(damageInterval);
            ApplyContinuousDamage();

            // 이펙트 생성 간격이 0이면 데미지와 함께 이펙트 생성
            if (effectSpawnInterval <= 0)
            {
                SpawnEffectsAlongLaser();
            }
        }
    }

    IEnumerator EffectSpawnRoutine()
    {
        while (isActive)
        {
            yield return new WaitForSeconds(effectSpawnInterval);
            SpawnEffectsAlongLaser();
        }
    }

    void ApplyContinuousDamage()
    {
        if (!isActive) return;

        // 레이저 경로 상의 모든 충돌체 검출
        float maxDistance = Vector2.Distance(actualLaserStartPoint, hitPoint);
        int hitCount = Physics2D.RaycastNonAlloc(actualLaserStartPoint, direction, hits, maxDistance, layerMask);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = hits[i];
            GameObject hitObject = hit.collider.gameObject;

            // 이미 이번 데미지 주기에서 처리한 객체는 건너뛰기
            if (damagedObjects.Contains(hitObject)) continue;

            // 보스 데미지 처리
            Stage1Boss stage1Boss = hitObject.GetComponent<Stage1Boss>();
            if (stage1Boss != null)
            {
                stage1Boss.ApplyDamage(damage);
            }

            Stage3Boss stage3Boss = hitObject.GetComponent<Stage3Boss>();
            if (stage3Boss != null)
            {
                stage3Boss.ApplyDamage(damage);
            }

            if (!hitObject.CompareTag("Breakable")) continue;

            // --- 타일맵인지 체크 ---
            Tilemap tilemap = hitObject.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                Debug.Log("타일");

                Vector3 hitPos = hit.point - hit.normal * 0.01f; // 경계면 보정
                Vector3Int cell = tilemap.WorldToCell(hitPos);

                TileBase tile = tilemap.GetTile(cell);

                if (tile != null)
                {
                    // 타일 데미지 처리
                    DamageTile(tilemap, cell);
                }

                continue;
            }

            damagedObjects.Add(hitObject);
        }

        // 다음 데미지 주기를 위해 초기화
        damagedObjects.Clear();
    }

    void DamageTile(Tilemap tilemap, Vector3Int cell)
    {
        if (!tileHealth.ContainsKey(cell))
            tileHealth[cell] = 1; // 초기 HP 지정

        tileHealth[cell]--;

        if (tileHealth[cell] <= 0)
        {
            tilemap.SetTile(cell, null);
            tileHealth.Remove(cell);
        }
    }

    void SpawnEffectsAlongLaser()
    {
        if (hitEffect == null || !isActive) return;

        // 확률 체크
        if (Random.value > effectSpawnChance) return;

        // 레이저를 따라 여러 지점에 이펙트 생성
        float laserLength = Vector2.Distance(actualLaserStartPoint, hitPoint);
        int numSpawnPoints = effectsPerSpawn;

        for (int i = 0; i < numSpawnPoints; i++)
        {
            // 레이저를 따라 랜덤한 위치 계산
            float t = Random.Range(0f, 1f);
            Vector2 spawnPosition = hitPoint;

            // 랜덤 오프셋 추가
            Vector2 randomOffset = Random.insideUnitCircle * effectSpawnRadius;
            spawnPosition += randomOffset;

            SpawnHitEffect(spawnPosition);
        }
    }

    void UpdateLaserSprite()
    {
        if (spriteRenderer != null)
        {
            // 레이저 길이 계산
            float distance = Vector2.Distance(actualLaserStartPoint, hitPoint);

            // 스프라이트 크기 조정 (길이만 변경)
            spriteRenderer.size = new Vector2(distance * 2, spriteRenderer.size.y);

            // 레이저 각도 설정
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // 레이저 위치 설정 (시작점과 끝점의 중간)
            Vector2 middlePoint = (actualLaserStartPoint + hitPoint) / 2f;
            transform.position = new Vector3(middlePoint.x, middlePoint.y, characterCenter.position.z);
        }
    }

    void CreatePooledEffect()
    {
        if (hitEffect == null) return;

        GameObject effect = Instantiate(hitEffect);
        effect.SetActive(false);

        // 풀 전용 컴포넌트 추가
        PooledEffect pooledComponent = effect.GetComponent<PooledEffect>();
        if (pooledComponent == null)
        {
            pooledComponent = effect.AddComponent<PooledEffect>();
        }
        pooledComponent.laserBeam = this;

        effectPool.Enqueue(effect);
    }

    void SpawnHitEffect(Vector2 position)
    {
        if (hitEffect == null || isCleaningUp) return;

        GameObject effect;

        if (effectPool.Count > 0)
        {
            effect = effectPool.Dequeue();
            effect.transform.position = position;

            // 랜덤 크기 적용
            float randomScale = Random.Range(effectSizeRange.x, effectSizeRange.y);
            effect.transform.localScale = Vector3.one * randomScale;

            // 랜덤 회전 적용
            if (randomRotation)
            {
                effect.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            }

            effect.SetActive(true);

            // 활성 이펙트로 등록
            activeEffects.Add(effect);

            // 애니메이션 재시작
            RestartEffectAnimation(effect);
        }
        else
        {
            // 풀이 비었으면 새로 생성
            effect = Instantiate(hitEffect, position, Quaternion.identity);

            // 랜덤 크기 적용
            float randomScale = Random.Range(effectSizeRange.x, effectSizeRange.y);
            effect.transform.localScale = Vector3.one * randomScale;

            // 랜덤 회전 적용
            if (randomRotation)
            {
                effect.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            }

            // 풀 전용 컴포넌트 추가
            PooledEffect pooledComponent = effect.GetComponent<PooledEffect>();
            if (pooledComponent == null)
            {
                pooledComponent = effect.AddComponent<PooledEffect>();
            }
            pooledComponent.laserBeam = this;

            activeEffects.Add(effect);
        }

        // 자동 반환 코루틴 시작
        Coroutine returnCoroutine = StartCoroutine(ReturnToPoolAfterTime(effect, effectLifetime));
        activeCoroutines.Add(returnCoroutine);
    }

    void RestartEffectAnimation(GameObject effect)
    {
        // Animator만 처리
        Animator animator = effect.GetComponent<Animator>();
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    IEnumerator ReturnToPoolAfterTime(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);

        ReturnEffectToPool(effect);
    }

    // 외부에서 호출할 수 있는 풀 반환 메서드
    public void ReturnEffectToPool(GameObject effect)
    {
        if (effect == null || isCleaningUp) return;

        // 이미 풀에 있거나 비활성화된 이펙트는 무시
        if (!effect.activeInHierarchy || !activeEffects.Contains(effect)) return;

        effect.SetActive(false);

        // 활성 목록에서 제거
        activeEffects.Remove(effect);

        // 풀에 다시 추가 (최대 개수 제한)
        if (effectPool.Count < maxEffects * 2) // 풀 크기 제한
        {
            effectPool.Enqueue(effect);
        }
        else
        {
            // 풀이 너무 크면 파괴
            Destroy(effect);
        }
    }

    // 정리 루틴 - 주기적으로 풀 정리
    IEnumerator CleanupRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f); // 2초마다 정리

            CleanupStrayEffects();
        }
    }

    void CleanupStrayEffects()
    {
        if (isCleaningUp) return;

        // 활성 이펙트 중에서 사라진 것들 정리
        List<GameObject> effectsToRemove = new List<GameObject>();

        foreach (GameObject effect in activeEffects)
        {
            if (effect == null)
            {
                effectsToRemove.Add(effect);
            }
            else if (!effect.activeInHierarchy)
            {
                // 활성 목록에 있지만 비활성화된 이펙트
                effectsToRemove.Add(effect);
                if (effectPool.Count < maxEffects * 2)
                {
                    effectPool.Enqueue(effect);
                }
            }
        }

        foreach (GameObject effect in effectsToRemove)
        {
            activeEffects.Remove(effect);
        }

        // 비정상적인 코루틴 정리
        for (int i = activeCoroutines.Count - 1; i >= 0; i--)
        {
            if (activeCoroutines[i] == null)
            {
                activeCoroutines.RemoveAt(i);
            }
        }
    }

    public void SetActive(bool active)
    {
        isActive = active;
        if (spriteRenderer != null)
            spriteRenderer.enabled = active;

        if (!active)
        {
            // 모든 이펙트 즉시 정리
            ForceCleanupAllEffects();
        }
    }

    void ForceCleanupAllEffects()
    {
        if (isCleaningUp) return;

        isCleaningUp = true;

        // 모든 코루틴 정지
        foreach (Coroutine coroutine in activeCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        activeCoroutines.Clear();

        // 모든 활성 이펙트 풀에 반환
        foreach (GameObject effect in activeEffects)
        {
            if (effect != null && effect.activeInHierarchy)
            {
                effect.SetActive(false);
                if (effectPool.Count < maxEffects * 2)
                {
                    effectPool.Enqueue(effect);
                }
            }
        }
        activeEffects.Clear();

        isCleaningUp = false;
    }

    void OnDestroy()
    {
        // 모든 코루틴 정지
        StopAllCoroutines();

        // 모든 이펙트 파괴
        ForceCleanupAllEffects();

        if (effectPool != null)
        {
            foreach (GameObject effect in effectPool)
            {
                if (effect != null)
                {
                    Destroy(effect);
                }
            }
            effectPool.Clear();
        }
    }
}

// 풀링된 이펙트를 위한 컴포넌트
public class PooledEffect : MonoBehaviour
{
    [HideInInspector]
    public LaserBeam laserBeam;

    void OnDisable()
    {
        // 비활성화 시 LaserBeam에 알림
        if (laserBeam != null)
        {
            laserBeam.ReturnEffectToPool(gameObject);
        }
    }

    void OnDestroy()
    {
        // 파괴 시 LaserBeam에서 제거
        if (laserBeam != null)
        {
            laserBeam.ReturnEffectToPool(gameObject);
        }
    }
}