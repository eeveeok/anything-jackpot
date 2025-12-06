using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stage3Boss : MonoBehaviour
{
    public enum BossPattern
    {
        Idle,
        Follow,         // 기본 추적 AI
        Slam,           // 손바닥 내려찍기
        EnergyWave,     // 바닥 파동
        Rush,           // 연속 돌진
        Laser,          // 전면 레이저
        RageSlam,       // 분노 모드 손바닥 찍기 (강화)
        RageWave        // 분노 모드 에너지 웨이브 (강화)
    }

    [Header("레퍼런스")]
    public Transform player;
    public GameObject laserPrefab;
    public GameObject energyWavePrefab;
    public GameObject portal;              // 보스 사망 시 활성화 될 포탈

    [Header("보스 스탯")]
    public float maxHP = 1000f;
    public float currentHP;

    [Header("추적 AI")]
    public float followSpeed = 10f;
    public float followStopDistance = 0.5f;

    [Header("기본 패턴 설정")]
    public float idleDelay = 3f;
    public int slamCount = 3;
    public float slamInterval = 1.2f;
    public float slamRadius = 3f;
    public float waveInterval = 0.3f;
    public float waveSpacing = 1.5f;
    public int rushTimes = 3;
    public float rushSpeed = 50f;         // 증가된 돌진 속도
    public float rushCooldown = 0.2f;     // 돌진 사이 대기 시간
    public float rushWarningTime = 0.5f;  // 돌진 준비 시간
    public float rushPathWidth = 5f;    // 돌진 경로 표시 너비
    public float groundLevel = 2.3f;       // 지면 높이

    [Header("레이저 패턴 설정")]
    public float laserSpeed = 30f;             // 레이저 속도
    public int laserCount = 5;                  // 일반 모드 레이저 발사 수
    public float laserInterval = 0.3f;         // 레이저 발사 간격
    public float laserWarmingTime = 0.8f;      // 레이저 발사 준비 시간
    public float laserDuration = 2f;           // 레이저 지속 시간 (발사 후 사라지는 시간)

    [Header("분노 패턴 설정")]
    public int rageWaveCount = 15;              // 더 많은 웨이브
    public float rageWaveInterval = 0.15f;      // 더 빠른 웨이브
    public float rageWaveSpacing = 2f;          // 더 넓은 간격
    public float rageRushCooldown = 0.1f;       // 더 짧은 대기 시간

    [Header("시각 효과 설정")]
    public float slamEffectDuration = 1.5f;
    public float shockwaveDuration = 0.8f;
    public float rageSlamEffectDuration = 2f;

    private Color normalSlamColor = Color.red;
    private Color rageSlamColor = Color.magenta;
    private Color rushPathColor = new Color(1f, 0.2f, 0.2f, 0.6f); // 돌진 경로 색상
    private Color rageRushPathColor = new Color(1f, 0f, 1f, 0.8f); // 분노 돌진 경로 색상

    [Header("충돌 데미지 설정")]
    public float rushDamageRadius = 4f;         // 증가된 돌진 데미지 반경
    public bool canDealRushDamage = true;       // 돌진 데미지 활성화

    [Header("카메라 설정")]
    public CinemachineVirtualCamera virtualCamera;
    public float cameraShakeIntensity = 20f;
    public float cameraShakeDuration = 1.5f;

    [Header("죽음 이펙트 설정")]
    [SerializeField] private GameObject deathExplosionPrefab; // 죽음 폭발 프리팹
    [SerializeField] private int explosionCount = 10; // 생성할 폭발 수
    [SerializeField] private float explosionRadius = 3f; // 생성 반경
    [SerializeField] private float explosionInterval = 0.1f; // 생성 간격

    private Rigidbody2D rb;
    private bool isRage = false;
    private bool isInPattern = false;
    private Sprite circleSprite;                // 동적으로 생성할 원형 스프라이트
    private Sprite lineSprite;                  // 돌진 경로 표시용 선형 스프라이트
    private bool isRushing = false;             // 돌진 중인지 확인
    private GameObject rushPathIndicator;       // 돌진 경로 표시 오브젝트
    private List<GameObject> rushLineIndicators = new List<GameObject>(); // 라인 마커들

    // Stage1Boss에서 가져온 메모리 관리 시스템
    private List<GameObject> activeEffects = new List<GameObject>();
    private Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
    private Queue<Texture2D> texturePool = new Queue<Texture2D>();
    private List<Coroutine> activeCoroutines = new List<Coroutine>();

    // 보스 색상 관련
    private SpriteRenderer bossSpriteRenderer;
    private Color originalBossColor;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // 보스 스프라이트 렌더러 참조
        bossSpriteRenderer = GetComponent<SpriteRenderer>();
        if (bossSpriteRenderer != null)
        {
            originalBossColor = bossSpriteRenderer.color;
        }

        // 스프라이트 생성
        circleSprite = CreateCircleSprite();
        lineSprite = CreateLineSprite();

        currentHP = maxHP;

        // 코루틴을 리스트에 추가
        Coroutine routine = StartCoroutine(BossRoutine());
        activeCoroutines.Add(routine);
    }

    void Update()
    {
        if (!isInPattern && !isRushing)
        {
            FollowPlayer();
        }
    }

    void FixedUpdate()
    {
        // 돌진 중일 때 플레이어와의 충돌 체크
        if (isRushing && canDealRushDamage)
        {
            CheckRushCollision();
        }
    }

    // ----------------------------------------------------------
    //              동적으로 스프라이트 생성
    // ----------------------------------------------------------
    Sprite CreateCircleSprite()
    {
        Texture2D texture = GetTextureFromPool(128, 128);
        Vector2 center = new Vector2(64, 64);
        float radius = 64;

        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    float alpha = Mathf.Clamp01(1 - (distance / radius));
                    alpha = Mathf.Pow(alpha, 0.5f);
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 128);
    }

    Sprite CreateLineSprite()
    {
        Texture2D texture = GetTextureFromPool(128, 128);

        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                if (Mathf.Abs(y - 64) <= 5)
                {
                    float alpha = Mathf.Clamp01(1 - Mathf.Abs(y - 64) / 5f);
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 128);
    }

    // ----------------------------------------------------------
    //              기본 추적 AI
    // ----------------------------------------------------------
    void FollowPlayer()
    {
        if (player == null || (player.GetComponent<LaserShooter>() != null && player.GetComponent<LaserShooter>().isDead)) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= followStopDistance)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        dir.Normalize();

        rb.velocity = dir * followSpeed;
    }

    // ----------------------------------------------------------
    //              보스 패턴 메인 루프
    // ----------------------------------------------------------
    private int lastNormalPattern = -1;
    private int lastRagePattern = -1;

    IEnumerator BossRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(idleDelay);

            if (!isRage && currentHP <= maxHP * 0.5f)
            {
                isRage = true;
                EnterRageMode();
                yield return new WaitForSeconds(1.5f);
                lastNormalPattern = -1;
                lastRagePattern = -1;
            }

            if (!isRage)
            {
                int p = GetNextPattern(false);
                switch (p)
                {
                    case 0: lastNormalPattern = 0; yield return StartCoroutine(Pattern_Slam()); break;
                    case 1: lastNormalPattern = 1; yield return StartCoroutine(Pattern_EnergyWave()); break;
                    case 2: lastNormalPattern = 2; yield return StartCoroutine(Pattern_Rush()); break;
                }
            }
            else
            {
                int p = GetNextPattern(true);
                switch (p)
                {
                    case 0: lastRagePattern = 0; yield return StartCoroutine(Pattern_Slam_Rage()); break;
                    case 1: lastRagePattern = 1; yield return StartCoroutine(Pattern_EnergyWave_Rage()); break;
                    case 2: lastRagePattern = 2; yield return StartCoroutine(Pattern_Rush_Rage()); break;
                    case 3: lastRagePattern = 3; yield return StartCoroutine(Pattern_Laser()); break;
                    case 4: lastRagePattern = 4; yield return StartCoroutine(Pattern_ComboAttack()); break;
                }
            }
        }
    }

    private int GetNextPattern(bool isRageMode)
    {
        int availablePatternsCount = isRageMode ? 5 : 3;
        List<int> availablePatterns = new List<int>();
        for (int i = 0; i < availablePatternsCount; i++) availablePatterns.Add(i);

        int lastPattern = isRageMode ? lastRagePattern : lastNormalPattern;
        if (lastPattern != -1 && availablePatterns.Count > 1) availablePatterns.Remove(lastPattern);

        return availablePatterns[Random.Range(0, availablePatterns.Count)];
    }

    // ----------------------------------------------------------
    //              분노 모드 진입
    // ----------------------------------------------------------
    void EnterRageMode()
    {
        Debug.Log("보스 분노 모드 돌입!");
        CreateRageAuraEffect();

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.3f, 0.3f, 1f);

        Coroutine shakeRoutine = StartCoroutine(ShakeCinemachineCamera(10f, 1.5f));
        activeCoroutines.Add(shakeRoutine);
    }

    void CreateRageAuraEffect()
    {
        GameObject aura = new GameObject("RageAura");
        aura.transform.position = transform.position;
        aura.transform.SetParent(transform);
        RegisterEffect(aura);

        SpriteRenderer auraRenderer = aura.AddComponent<SpriteRenderer>();
        auraRenderer.sprite = circleSprite;
        auraRenderer.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        auraRenderer.sortingOrder = -1;

        Coroutine auraRoutine = StartCoroutine(RageAuraAnimation(aura));
        activeCoroutines.Add(auraRoutine);
    }

    IEnumerator RageAuraAnimation(GameObject aura)
    {
        float time = 0f;
        float pulseSpeed = 3f;

        while (isRage && aura != null)
        {
            time += Time.deltaTime;
            float scale = 6f + Mathf.Sin(time * pulseSpeed);
            aura.transform.localScale = new Vector3(scale, scale, 1);

            SpriteRenderer renderer = aura.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                float alpha = 0.3f + Mathf.Sin(time * pulseSpeed * 2f) * 0.1f;
                renderer.color = new Color(1f, 0.2f, 0.2f, alpha);
            }

            yield return null;
        }

        if (aura != null)
        {
            UnregisterEffect(aura);
            Destroy(aura);
        }
    }

    // ----------------------------------------------------------
    //              기본 패턴들
    // ----------------------------------------------------------

    IEnumerator Pattern_Slam()
    {
        rb.velocity = Vector2.zero;

        for (int i = 0; i < slamCount; i++)
        {
            CreateSlamEffect(transform.position, slamRadius * 1.8f, normalSlamColor, false);
            yield return new WaitForSeconds(slamInterval * 0.5f);

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, slamRadius * 1.5f, LayerMask.GetMask("Player"));
            foreach (Collider2D hit in hits)
            {
                LaserShooter playerScript = hit.GetComponent<LaserShooter>();
                if (playerScript != null) playerScript.PlayerDie();
            }

            yield return new WaitForSeconds(slamInterval * 0.5f);
        }
    }

    IEnumerator Pattern_EnergyWave()
    {
        isInPattern = true;
        rb.velocity = Vector2.zero;

        float groundY = transform.position.y - 1.5f;
        int direction = (player.position.x > transform.position.x) ? 1 : -1;

        for (int i = 1; i <= 10; i++)
        {
            Vector2 spawnPos = new Vector2(transform.position.x + i * waveSpacing * direction, groundY);
            GameObject wave = Instantiate(energyWavePrefab, spawnPos, Quaternion.identity);
            Destroy(wave, 1f);
            yield return new WaitForSeconds(waveInterval);
        }

        yield return new WaitForSeconds(0.5f);
        isInPattern = false;
    }

    IEnumerator Pattern_Rush()
    {
        isInPattern = true;
        rb.velocity = Vector2.zero;
        rb.gravityScale = 0f;

        for (int i = 0; i < rushTimes; i++)
        {
            // 플레이어를 넘어서까지 가기 위해 목표 위치 계산
            Vector2 playerPos = player.position;
            Vector2 direction = (playerPos - (Vector2)transform.position).normalized;

            // 최대 돌진 거리 제한
            float maxRushDistance = 18f;

            // 최대 거리로 제한된 위치로 계산
            Vector2 targetPosition = (Vector2)transform.position + (direction * maxRushDistance);
            if(targetPosition.y < groundLevel)
            {
                targetPosition = new Vector2(targetPosition.x, groundLevel);
            }

            // 돌진 경로 표시
            yield return StartCoroutine(ShowRushPath(targetPosition, false));

            // 돌진 실행
            yield return StartCoroutine(ExecuteRushToTarget(targetPosition, false));

            yield return new WaitForSeconds(rushCooldown);
        }

        // 모든 돌진이 끝난 후, 자연스럽게 하강
        yield return StartCoroutine(SmoothDescentToGround());

        rb.gravityScale = 1f;

        isInPattern = false;
    }

    // 부드러운 하강 코루틴
    IEnumerator SmoothDescentToGround()
    {
        float descentDuration = 1.0f; // 하강에 걸리는 시간
        float elapsed = 0f;

        // 시작 위치 저장
        Vector2 startPosition = transform.position;

        // 목표 위치 계산 (같은 x값, 지면 높이)
        Vector2 targetPosition = new Vector2(transform.position.x, groundLevel);

        Debug.Log($"부드러운 하강 시작: {startPosition.y:F2} → {groundLevel:F2}");

        while (elapsed < descentDuration && transform.position.y > groundLevel + 0.1f)
        {
            elapsed += Time.deltaTime;

            // 서서히 아래로 이동 (선형 보간)
            float t = elapsed / descentDuration;
            Vector2 newPosition = Vector2.Lerp(startPosition, targetPosition, t);

            // y축만 부드럽게 이동 (x축은 고정)
            transform.position = new Vector2(transform.position.x, newPosition.y);

            yield return null;
        }

        // 최종 위치 보정
        Vector3 finalPos = transform.position;
        transform.position = finalPos;

        Debug.Log($"하강 완료: 최종 위치 {transform.position}");
    }

    // ----------------------------------------------------------
    //              돌진 경로 표시 (라인 마커 생성)
    // ----------------------------------------------------------
    IEnumerator ShowRushPath(Vector2 targetPosition, bool isRageMode)
    {
        Color pathColor = isRageMode ? rageRushPathColor : rushPathColor;

        // 경로 라인 생
        rushPathIndicator = new GameObject("RushPathLine");
        rushPathIndicator.transform.position = transform.position;
        RegisterEffect(rushPathIndicator);

        SpriteRenderer pathRenderer = rushPathIndicator.AddComponent<SpriteRenderer>();
        pathRenderer.sprite = lineSprite;
        pathRenderer.color = pathColor;
        pathRenderer.sortingOrder = 10;

        // 경로 방향과 길이 계산
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(transform.position, targetPosition);

        // 경로 설정 (방향과 길이 조정)
        rushPathIndicator.transform.right = direction;
        rushPathIndicator.transform.localScale = new Vector3(distance, rushPathWidth, 1f);

        // 경로 위치를 시작점과 끝점 사이의 중간으로 설정
        Vector2 midpoint = ((Vector2)transform.position + targetPosition) / 2f;
        rushPathIndicator.transform.position = midpoint;

        // 점멸 애니메이션
        float elapsed = 0f;
        float pulseSpeed = 4f;

        while (elapsed < rushWarningTime && rushPathIndicator != null)
        {
            elapsed += Time.deltaTime;

            // 점멸 효과
            float alpha = 0.3f + (Mathf.Sin(elapsed * pulseSpeed * 2f) * 0.3f + 0.4f);
            pathRenderer.color = new Color(pathColor.r, pathColor.g, pathColor.b, alpha);

            yield return null;
        }

        // 경로 표시 제거
        ClearRushPathIndicators();
    }

    void ClearRushPathIndicators()
    {
        if (rushPathIndicator != null)
        {
            UnregisterEffect(rushPathIndicator);
            Destroy(rushPathIndicator);
            rushPathIndicator = null;
        }
    }

    // ----------------------------------------------------------
    //              목표 지점까지 직선 돌진
    // ----------------------------------------------------------
    IEnumerator ExecuteRushToTarget(Vector2 targetPosition, bool isRageMode)
    {
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;

        float currentRushSpeed = rushSpeed;
        float arrivalThreshold = 0.5f; // 도착 판정 임계값
        float maxRushTime = 1.0f; // 최대 돌진 시간 (1초)

        // 돌진 시작
        isRushing = true;
        Debug.Log($"돌진 시작 - 목표: {targetPosition}, 속도: {currentRushSpeed}");

        // 돌진 이펙트
        GameObject rushEffect = CreateRushTrailEffect(isRageMode);

        float rushTimer = 0f;

        // 목표 지점에 도달할 때까지 직선 이동 (최대 1초)
        while (Vector2.Distance(transform.position, targetPosition) > arrivalThreshold && rushTimer < maxRushTime)
        {
            rushTimer += Time.deltaTime;
            rb.velocity = direction * currentRushSpeed;

            // 이펙트 위치 업데이트
            if (rushEffect != null)
            {
                rushEffect.transform.position = transform.position;
            }

            yield return null;
        }

        // 목표 지점 도착 또는 시간 초과
        rb.velocity = Vector2.zero;
        isRushing = false;

        // 이펙트 정리
        if (rushEffect != null)
        {
            UnregisterEffect(rushEffect);
            Destroy(rushEffect);
        }
    }

    GameObject CreateRushTrailEffect(bool isRageMode)
    {
        GameObject trail = new GameObject("RushTrail");
        trail.transform.position = transform.position;
        RegisterEffect(trail);

        SpriteRenderer trailRenderer = trail.AddComponent<SpriteRenderer>();
        trailRenderer.sprite = circleSprite;
        trailRenderer.color = isRageMode ?
            new Color(1f, 0f, 1f, 0.4f) :
            new Color(1f, 0.2f, 0.2f, 0.4f);
        trailRenderer.sortingOrder = 9;

        Coroutine trailRoutine = StartCoroutine(RushTrailAnimation(trail, isRageMode));
        activeCoroutines.Add(trailRoutine);

        return trail;
    }

    IEnumerator RushTrailAnimation(GameObject trail, bool isRageMode)
    {
        float duration = 1.0f;
        float elapsed = 0f;

        while (elapsed < duration && trail != null)
        {
            elapsed += Time.deltaTime;
            float scale = 2f + elapsed * 3f;
            trail.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer renderer = trail.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                float alpha = Mathf.Lerp(0.4f, 0f, elapsed / duration);
                renderer.color = isRageMode ?
                    new Color(1f, 0f, 1f, alpha) :
                    new Color(1f, 0.2f, 0.2f, alpha);
            }

            yield return null;
        }

        if (trail != null)
        {
            UnregisterEffect(trail);
            Destroy(trail);
        }
    }

    // ----------------------------------------------------------
    //              분노 패턴들
    // ----------------------------------------------------------

    IEnumerator Pattern_Slam_Rage()
    {
        rb.velocity = Vector2.zero;

        for (int i = 0; i < slamCount; i++)
        {
            CreateSlamEffect(transform.position, slamRadius * (1f + 0.3f * (i + 1)), rageSlamColor, true);
            yield return new WaitForSeconds(slamInterval * 0.3f);

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, slamRadius * (1f + 0.3f * (i + 1)), LayerMask.GetMask("Player"));
            foreach (Collider2D hit in hits)
            {
                LaserShooter playerScript = hit.GetComponent<LaserShooter>();
                if (playerScript != null) playerScript.PlayerDie();
            }

            Coroutine shakeRoutine = StartCoroutine(ShakeCinemachineCamera(cameraShakeIntensity * 1.5f, cameraShakeDuration));
            activeCoroutines.Add(shakeRoutine);
            yield return new WaitForSeconds(slamInterval * 0.7f);
        }
    }

    IEnumerator Pattern_EnergyWave_Rage()
    {
        isInPattern = true;
        rb.velocity = Vector2.zero;

        float groundY = transform.position.y - 1f;

        for (int i = 1; i <= rageWaveCount; i++)
        {
            Vector2 rightPos = new Vector2(transform.position.x + i * rageWaveSpacing, groundY);
            GameObject rightWave = Instantiate(energyWavePrefab, rightPos, Quaternion.identity);
            rightWave.transform.localScale *= 1.5f;

            Vector2 leftPos = new Vector2(transform.position.x - i * rageWaveSpacing, groundY);
            GameObject leftWave = Instantiate(energyWavePrefab, leftPos, Quaternion.identity);
            leftWave.transform.localScale *= 1.5f;

            SpriteRenderer rightRenderer = rightWave.GetComponent<SpriteRenderer>();
            SpriteRenderer leftRenderer = leftWave.GetComponent<SpriteRenderer>();
            if (rightRenderer != null) rightRenderer.color = rageSlamColor;
            if (leftRenderer != null) leftRenderer.color = rageSlamColor;

            Destroy(rightWave, 1f);
            Destroy(leftWave, 1f);
            yield return new WaitForSeconds(rageWaveInterval);
        }

        yield return new WaitForSeconds(0.3f);
        isInPattern = false;
    }

    IEnumerator Pattern_Rush_Rage()
    {
        isInPattern = true;
        rb.velocity = Vector2.zero;
        rb.gravityScale = 0f; // 돌진 중에는 중력 비활성화

        for (int i = 0; i < rushTimes * 2; i++)
        {
            // 플레이어를 넘어서까지 가기 위해 목표 위치 계산
            Vector2 playerPos = player.position;
            Vector2 direction = (playerPos - (Vector2)transform.position).normalized;

            // 최대 돌진 거리 제한
            float maxRageRushDistance = 20f; // 분노 모드는 더 길게

            // 최대 거리로 제한된 위치로 재계산
            Vector2 targetPosition = (Vector2)transform.position + (direction * maxRageRushDistance);
            if (targetPosition.y < groundLevel)
            {
                targetPosition = new Vector2(targetPosition.x, groundLevel);
            }

            // 돌진 경로 표시
            yield return StartCoroutine(ShowRushPath(targetPosition, true));

            // 돌진 실행
            yield return StartCoroutine(ExecuteRushToTarget(targetPosition, true));

            yield return new WaitForSeconds(rageRushCooldown);
        }

        // 모든 돌진이 끝난 후, 자연스럽게 하강
        yield return StartCoroutine(SmoothDescentToGround());

        rb.gravityScale = 1f; // 중력 복원

        isInPattern = false;
    }

    IEnumerator Pattern_Laser()
    {
        isInPattern = true;
        rb.velocity = Vector2.zero;

        Debug.Log(isRage ? "패턴: 분노 레이저 발사!" : "패턴: 레이저 발사!");

        // 레이저 패턴 준비 시간
        yield return new WaitForSeconds(laserWarmingTime);

        for (int i = 0; i < laserCount; i++)
        {
            // 플레이어 방향 계산
            Vector2 playerPos = player.position;
            Vector2 direction = (playerPos - (Vector2)transform.position).normalized;

            // 레이저 생성
            GameObject laser = Instantiate(laserPrefab, transform.position, Quaternion.identity);

            // 레이저 각도 설정 (플레이어 방향으로)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            laser.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // 레이저 속성 설정
            LaserProjectile laserScript = laser.GetComponent<LaserProjectile>();
            if (laserScript != null)
            {
                laserScript.SetDirection(direction);
                laserScript.SetSpeed(laserSpeed);
                laserScript.SetColor(Color.red);
                laserScript.SetDuration(laserDuration);
            }
            else
            {
                // LaserProjectile 컴포넌트가 없다면 Rigidbody2D로 직접 이동
                Rigidbody2D laserRb = laser.GetComponent<Rigidbody2D>();
                if (laserRb != null)
                {
                    laserRb.velocity = direction * laserSpeed;
                }

                // 일정 시간 후 파괴
                Destroy(laser, laserDuration);
            }

            // 레이저 생성 효과
            CreateLaserSpawnEffect(transform.position, direction, isRage);

            // 다음 레이저 발사까지 대기
            yield return new WaitForSeconds(laserInterval);
        }

        yield return new WaitForSeconds(0.5f); // 마지막 레이저 발사 후 추가 대기
        isInPattern = false;
    }

    void CreateLaserSpawnEffect(Vector2 position, Vector2 direction, bool isRageMode)
    {
        GameObject effect = new GameObject("LaserSpawnEffect");
        effect.transform.position = position;
        RegisterEffect(effect);

        SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
        renderer.sprite = circleSprite;
        renderer.color = Color.red;
        renderer.sortingOrder = 12;

        // 레이저 방향에 맞게 회전
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        effect.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 원형에서 타원형으로 변환 (레이저 방향으로 길게)
        effect.transform.localScale = new Vector3(0.2f, 1f, 1f);

        Coroutine effectRoutine = StartCoroutine(LaserSpawnEffectAnimation(effect, isRageMode));
        activeCoroutines.Add(effectRoutine);
    }

    IEnumerator LaserSpawnEffectAnimation(GameObject effect, bool isRageMode)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        SpriteRenderer renderer = effect.GetComponent<SpriteRenderer>();

        Vector3 originalScale = effect.transform.localScale;
        Color originalColor = renderer.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 점점 커지면서 사라짐
            float scaleX = Mathf.Lerp(originalScale.x, originalScale.x * 3f, t);
            float scaleY = Mathf.Lerp(originalScale.y, originalScale.y * 0.5f, t);
            effect.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            // 레이저 방향으로 이동
            effect.transform.position += (Vector3)effect.transform.right * Time.deltaTime * 5f;

            // 페이드 아웃
            float alpha = Mathf.Lerp(originalColor.a, 0f, t);
            renderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            yield return null;
        }

        if (effect != null)
        {
            UnregisterEffect(effect);
            Destroy(effect);
        }
    }

    IEnumerator Pattern_ComboAttack()
    {
        isInPattern = true;
        rb.velocity = Vector2.zero;

        yield return StartCoroutine(Pattern_Slam_Rage());
        yield return StartCoroutine(Pattern_Rush_Rage());
        yield return StartCoroutine(Pattern_EnergyWave_Rage());
        isInPattern = false;
    }

    // ----------------------------------------------------------
    //              돌진 충돌 체크
    // ----------------------------------------------------------
    void CheckRushCollision()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= rushDamageRadius)
        {
            LaserShooter playerScript = player.GetComponent<LaserShooter>();
            if (playerScript != null && !playerScript.isDead)
            {
                Debug.Log($"돌진 충돌! 플레이어에게 데미지 - 거리: {distance}");
                playerScript.PlayerDie();
            }
        }
    }

    // ----------------------------------------------------------
    //              시각 효과
    // ----------------------------------------------------------

    void CreateSlamEffect(Vector2 position, float radius, Color effectColor, bool isRageEffect)
    {
        GameObject effect = new GameObject("SlamEffect");
        effect.transform.position = position;
        RegisterEffect(effect);

        SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
        renderer.sprite = circleSprite;
        renderer.color = effectColor;
        renderer.sortingOrder = 10;

        float baseSize = radius * 2f;
        effect.transform.localScale = new Vector3(baseSize, baseSize, 1f);
        if (isRageEffect) effect.transform.localScale *= 1.5f;

        Coroutine animationRoutine = StartCoroutine(SlamEffectAnimation(effect, isRageEffect ? rageSlamEffectDuration : slamEffectDuration));
        activeCoroutines.Add(animationRoutine);

        CreateShockwave(position, isRageEffect);
        Coroutine shakeRoutine = StartCoroutine(ShakeCinemachineCamera(
            isRageEffect ? cameraShakeIntensity * 1.5f : cameraShakeIntensity,
            cameraShakeDuration
        ));
        activeCoroutines.Add(shakeRoutine);
    }

    IEnumerator SlamEffectAnimation(GameObject effect, float duration)
    {
        SpriteRenderer renderer = effect.GetComponent<SpriteRenderer>();
        Transform effectTransform = effect.transform;
        Vector3 originalScale = effectTransform.localScale;
        Color originalColor = renderer.color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(1f, 0f, t);
            renderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            float pulse = Mathf.Sin(t * Mathf.PI * 2f) * 0.1f + 1f;
            effectTransform.localScale = originalScale * pulse;
            yield return null;
        }

        if (effect != null)
        {
            UnregisterEffect(effect);
            Destroy(effect);
        }
    }

    void CreateShockwave(Vector2 position, bool isRageEffect)
    {
        GameObject shockwave = new GameObject("Shockwave");
        shockwave.transform.position = position;
        RegisterEffect(shockwave);

        SpriteRenderer renderer = shockwave.AddComponent<SpriteRenderer>();
        renderer.sprite = circleSprite;
        renderer.color = new Color(1f, 1f, 1f, 0.5f);
        renderer.sortingOrder = 9;

        Coroutine animationRoutine = StartCoroutine(ShockwaveAnimation(shockwave, isRageEffect));
        activeCoroutines.Add(animationRoutine);
    }

    IEnumerator ShockwaveAnimation(GameObject shockwave, bool isRageEffect)
    {
        SpriteRenderer renderer = shockwave.GetComponent<SpriteRenderer>();
        Transform shockwaveTransform = shockwave.transform;

        float elapsed = 0f;
        float endSize = isRageEffect ? 4f : 3f;

        while (elapsed < shockwaveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shockwaveDuration;
            float currentSize = Mathf.Lerp(0.5f, endSize, t);
            shockwaveTransform.localScale = new Vector3(currentSize, currentSize, 1f);
            float alpha = Mathf.Lerp(0.5f, 0f, t);
            renderer.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        if (shockwave != null)
        {
            UnregisterEffect(shockwave);
            Destroy(shockwave);
        }
    }

    // ----------------------------------------------------------
    //              카메라 흔들기
    // ----------------------------------------------------------
    IEnumerator ShakeCinemachineCamera(float duration, float intensity)
    {
        if (virtualCamera == null) yield break;

        CinemachineBasicMultiChannelPerlin noise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (noise == null) yield break;

        float originalAmplitude = noise.m_AmplitudeGain;
        float originalFrequency = noise.m_FrequencyGain;

        noise.m_AmplitudeGain = intensity;
        noise.m_FrequencyGain = intensity * 2f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentIntensity = Mathf.Lerp(intensity, 0f, t);
            noise.m_AmplitudeGain = currentIntensity;
            noise.m_FrequencyGain = currentIntensity * 2f;
            yield return null;
        }

        noise.m_AmplitudeGain = originalAmplitude;
        noise.m_FrequencyGain = originalFrequency;
    }

    // ----------------------------------------------------------
    //                 보스 데미지 처리
    // ----------------------------------------------------------
    public void ApplyDamage(float damage)
    {
        currentHP -= damage;

        Coroutine flashRoutine = StartCoroutine(FlashRed());
        activeCoroutines.Add(flashRoutine);
        CreateHitEffect();

        if (currentHP <= 0) Die();
    }

    IEnumerator FlashRed()
    {
        if (bossSpriteRenderer != null)
        {
            bossSpriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            bossSpriteRenderer.color = originalBossColor;
        }
    }

    void CreateHitEffect()
    {
        GameObject hitEffect = new GameObject("HitEffect");
        hitEffect.transform.position = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
        RegisterEffect(hitEffect);

        SpriteRenderer renderer = hitEffect.AddComponent<SpriteRenderer>();
        renderer.sprite = circleSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 11;
        hitEffect.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        Coroutine animationRoutine = StartCoroutine(HitEffectAnimation(hitEffect));
        activeCoroutines.Add(animationRoutine);
    }

    IEnumerator HitEffectAnimation(GameObject effect)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        SpriteRenderer renderer = effect.GetComponent<SpriteRenderer>();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float size = Mathf.Lerp(0.5f, 2f, t);
            effect.transform.localScale = new Vector3(size, size, 1f);
            float alpha = Mathf.Lerp(1f, 0f, t);
            renderer.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        if (effect != null)
        {
            UnregisterEffect(effect);
            Destroy(effect);
        }
    }

    void Die()
    {
        StopAllCoroutines();
        foreach (var coroutine in activeCoroutines) if (coroutine != null) StopCoroutine(coroutine);
        activeCoroutines.Clear();

        rb.velocity = Vector2.zero;
        isInPattern = true;

        Debug.Log("스테이지 3 보스 사망!");
        CreateDeathEffect();
        CleanupAllEffects();
        ClearMaterialCache();
        ClearTexturePool();

        Destroy(gameObject, 3f);
        portal.SetActive(true);
    }

    void CreateDeathEffect()
    {
        if (deathExplosionPrefab == null)
        {
            Debug.LogWarning("Death Explosion Prefab이 할당되지 않았습니다!");
            return;
        }

        Coroutine deathEffectRoutine = StartCoroutine(DeathEffectAnimation());
        activeCoroutines.Add(deathEffectRoutine);
    }

    IEnumerator DeathEffectAnimation()
    {
        for (int i = 0; i < explosionCount; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * explosionRadius;
            Vector2 spawnPos = (Vector2)transform.position + randomOffset;

            GameObject explosion = Instantiate(deathExplosionPrefab, spawnPos, Quaternion.identity);
            if (Random.value > 0.5f) explosion.transform.Rotate(0f, 0f, Random.Range(0f, 360f));

            float randomScale = Random.Range(0.8f, 1.2f);
            explosion.transform.localScale = Vector3.one * randomScale;
            explosion.transform.parent = transform;

            yield return new WaitForSeconds(explosionInterval);
        }
    }

    // ----------------------------------------------------------
    //                 메모리 관리 메서드
    // ----------------------------------------------------------
    private Material GetOrCreateMaterial(string shaderName, Color color)
    {
        string key = $"{shaderName}_{color.GetHashCode()}";
        if (!materialCache.ContainsKey(key))
        {
            Shader shader = Shader.Find(shaderName);
            if (shader != null)
            {
                Material mat = new Material(shader);
                mat.color = color;
                materialCache[key] = mat;
            }
        }
        return materialCache.ContainsKey(key) ? materialCache[key] : null;
    }

    private void ClearMaterialCache()
    {
        foreach (var mat in materialCache.Values) if (mat != null) Destroy(mat);
        materialCache.Clear();
    }

    private Texture2D GetTextureFromPool(int width, int height)
    {
        foreach (var texture in texturePool)
        {
            if (texture != null && texture.width == width && texture.height == height)
            {
                var list = new List<Texture2D>(texturePool);
                list.Remove(texture);
                texturePool = new Queue<Texture2D>(list);
                return texture;
            }
        }
        return new Texture2D(width, height);
    }

    private void ClearTexturePool()
    {
        foreach (var texture in texturePool) if (texture != null) Destroy(texture);
        texturePool.Clear();
    }

    private void RegisterEffect(GameObject effect)
    {
        if (effect != null && !activeEffects.Contains(effect)) activeEffects.Add(effect);
    }

    private void UnregisterEffect(GameObject effect)
    {
        if (effect != null) activeEffects.Remove(effect);
    }

    private void CleanupAllEffects()
    {
        foreach (var effect in activeEffects) if (effect != null) Destroy(effect);
        activeEffects.Clear();
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        CleanupAllEffects();
        ClearMaterialCache();
        ClearTexturePool();
    }

    // ----------------------------------------------------------
    //              시각화용 Gizmos
    // ----------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 4f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rushDamageRadius);
    }
}