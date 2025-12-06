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
    public LaserBeam laserBeam;
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
    public float slamInterval = 0.8f;
    public float waveInterval = 0.3f;
    public float waveSpacing = 1.5f;
    public int rushTimes = 4;
    public float rushSpeed = 25f;         // 증가된 돌진 속도
    public float rushDuration = 1.2f;     // 증가된 돌진 지속 시간
    public float rushCooldown = 0.4f;     // 돌진 사이 대기 시간
    public float rushOvershootDistance = 8f; // 증가된 오버슈트 거리
    public float descentSpeed = 20f;      // 하강 속도
    public float descentHeight = 3f;      // 하강 높이
    public float rushPathWidth = 2f;      // 돌진 경로 표시 너비
    public float rushWarningTime = 0.8f;  // 돌진 준비 시간

    [Header("분노 패턴 설정")]
    public int rageSlamCount = 5;               // 분노 모드 손바닥 찍기 횟수 증가
    public float rageSlamInterval = 0.5f;       // 더 빠른 간격
    public float rageSlamRadius = 6f;           // 범위 증가
    public int rageWaveCount = 15;              // 더 많은 웨이브
    public float rageWaveInterval = 0.15f;      // 더 빠른 웨이브
    public float rageWaveSpacing = 2f;          // 더 넓은 간격
    public float rageRushSpeed = 35f;           // 더 빠른 돌진
    public int rageRushTimes = 6;               // 더 많은 돌진 횟수
    public float rageRushDuration = 1.0f;       // 더 긴 돌진 지속 시간
    public float rageRushCooldown = 0.3f;       // 더 짧은 대기 시간
    public float rageRushOvershootDistance = 12f; // 더 많이 넘어서 감
    public float rageDescentSpeed = 25f;        // 더 빠른 하강 속도
    public float rageRushWarningTime = 0.5f;    // 더 짧은 경고 시간

    [Header("시각 효과 설정")]
    public float slamEffectDuration = 1.5f;
    public float shockwaveDuration = 0.8f;
    public float rageSlamEffectDuration = 2f;
    public Color normalSlamColor = Color.red;
    public Color rageSlamColor = Color.magenta;
    public Material circleMaterial;             // 원형 이펙트에 사용할 머티리얼
    public Color rushPathColor = new Color(1f, 0.2f, 0.2f, 0.4f); // 돌진 경로 색상
    public Color rageRushPathColor = new Color(1f, 0f, 1f, 0.6f); // 분노 돌진 경로 색상

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
    private Camera mainCamera;
    private Vector3 originalCameraPos;
    private Sprite circleSprite;                // 동적으로 생성할 원형 스프라이트
    private Sprite lineSprite;                  // 돌진 경로 표시용 선형 스프라이트
    private bool isRushing = false;             // 돌진 중인지 확인
    private float lastRushTime = 0f;            // 마지막 돌진 시간
    private Vector2 rushDirection;              // 현재 돌진 방향
    private bool isDescending = false;          // 하강 중인지 확인
    private float originalGravityScale;         // 원래 중력 스케일 저장
    private GameObject rushPathIndicator;       // 돌진 경로 표시 오브젝트

    // Stage1Boss에서 가져온 메모리 관리 시스템
    private List<GameObject> activeEffects = new List<GameObject>();
    private Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
    private Queue<Texture2D> texturePool = new Queue<Texture2D>();
    private List<Coroutine> activeCoroutines = new List<Coroutine>();

    // 보스 색상 관련
    private SpriteRenderer bossSpriteRenderer;
    private Color originalBossColor;
    private Collider2D bossCollider;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        bossCollider = GetComponent<Collider2D>();

        if (mainCamera != null)
        {
            originalCameraPos = mainCamera.transform.position;
        }

        // 원래 중력 스케일 저장
        originalGravityScale = rb.gravityScale;

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
        if (!isInPattern && !isRushing && !isDescending)
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
        // 텍스처 풀에서 가져오기
        Texture2D texture = GetTextureFromPool(128, 128);

        // 원의 중심
        Vector2 center = new Vector2(64, 64);
        float radius = 64;

        // 텍스처에 원 그리기
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                // 원 안쪽인지 확인
                float distance = Vector2.Distance(new Vector2(x, y), center);

                if (distance <= radius)
                {
                    // 부드러운 가장자리를 위한 알파값
                    float alpha = Mathf.Clamp01(1 - (distance / radius));
                    alpha = Mathf.Pow(alpha, 0.5f); // 더 부드러운 가장자리
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();

        // 스프라이트 생성
        return Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 128);
    }

    Sprite CreateLineSprite()
    {
        // 선형 스프라이트 생성 (돌진 경로 표시용)
        Texture2D texture = GetTextureFromPool(128, 128);

        // 수평선 그리기
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                // 선의 두께 (중앙에서 ±10픽셀)
                if (Mathf.Abs(y - 64) <= 10)
                {
                    // 선형 그라데이션 (중앙에서 가장 진하고 가장자리로 갈수록 희미해짐)
                    float alpha = Mathf.Clamp01(1 - Mathf.Abs(y - 64) / 10f);
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();

        // 스프라이트 생성
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
    //              보스 패턴 메인 루프 (분노 패턴 추가)
    // ----------------------------------------------------------
    private int lastNormalPattern = -1;  // 일반 모드에서 마지막으로 사용한 패턴
    private int lastRagePattern = -1;    // 분노 모드에서 마지막으로 사용한 패턴

    IEnumerator BossRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(idleDelay);

            // HP 50% 이하 → 분노 돌입
            if (!isRage && currentHP <= maxHP * 0.5f)
            {
                isRage = true;
                EnterRageMode();
                yield return new WaitForSeconds(1.5f);

                // 분노 모드 진입 시 패턴 기록 초기화
                lastNormalPattern = -1;
                lastRagePattern = -1;
            }

            // 패턴 선택
            if (!isRage)
            {
                // 일반 패턴
                int p = GetNextPattern(false);  // 일반 모드 패턴 선택

                switch (p)
                {
                    case 0:
                        lastNormalPattern = 0;
                        yield return StartCoroutine(Pattern_Slam());
                        break;
                    case 1:
                        lastNormalPattern = 1;
                        yield return StartCoroutine(Pattern_EnergyWave());
                        break;
                    case 2:
                        lastNormalPattern = 2;
                        yield return StartCoroutine(Pattern_Rush());
                        break;
                }
            }
            else
            {
                // 분노 패턴 (더 강력하고 다양한 패턴)
                int p = GetNextPattern(true);  // 분노 모드 패턴 선택

                switch (p)
                {
                    case 0:
                        lastRagePattern = 0;
                        yield return StartCoroutine(Pattern_Slam_Rage());
                        break;
                    case 1:
                        lastRagePattern = 1;
                        yield return StartCoroutine(Pattern_EnergyWave_Rage());
                        break;
                    case 2:
                        lastRagePattern = 2;
                        yield return StartCoroutine(Pattern_Rush_Rage());
                        break;
                    case 3:
                        lastRagePattern = 3;
                        yield return StartCoroutine(Pattern_Laser());
                        break;
                    case 4:
                        lastRagePattern = 4;
                        yield return StartCoroutine(Pattern_ComboAttack());
                        break;
                }
            }
        }
    }

    // 중복되지 않는 패턴 선택 메서드
    private int GetNextPattern(bool isRageMode)
    {
        int availablePatternsCount = isRageMode ? 5 : 3;  // 패턴 총 개수

        // 선택 가능한 패턴 목록 생성
        List<int> availablePatterns = new List<int>();
        for (int i = 0; i < availablePatternsCount; i++)
        {
            availablePatterns.Add(i);
        }

        // 이전에 사용한 패턴 제외 (사용 가능한 패턴이 2개 이상일 경우)
        int lastPattern = isRageMode ? lastRagePattern : lastNormalPattern;
        if (lastPattern != -1 && availablePatterns.Count > 1)
        {
            availablePatterns.Remove(lastPattern);
        }

        // 랜덤 선택
        int randomIndex = Random.Range(0, availablePatterns.Count);
        return availablePatterns[randomIndex];
    }

    // ----------------------------------------------------------
    //              분노 모드 진입
    // ----------------------------------------------------------
    void EnterRageMode()
    {
        Debug.Log("보스 분노 모드 돌입!");

        // 분노 오라 이펙트 생성 (코드로)
        CreateRageAuraEffect();

        // 색상 변경 (옵션)
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.3f, 0.3f, 1f); // 붉은색 톤
        }

        // 카메라 흔들기
        Coroutine shakeRoutine = StartCoroutine(ShakeCinemachineCamera(10f, 1.5f));
        activeCoroutines.Add(shakeRoutine);
    }

    // ----------------------------------------------------------
    //              분노 오라 이펙트 생성
    // ----------------------------------------------------------
    void CreateRageAuraEffect()
    {
        GameObject aura = new GameObject("RageAura");
        aura.transform.position = transform.position;
        aura.transform.SetParent(transform);
        RegisterEffect(aura);

        SpriteRenderer auraRenderer = aura.AddComponent<SpriteRenderer>();
        auraRenderer.sprite = circleSprite;
        auraRenderer.color = new Color(1f, 0.2f, 0.2f, 0.3f); // 붉은색 반투명
        auraRenderer.sortingOrder = -1;

        // 오라 애니메이션
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

            // 펄스 효과
            float scale = 6f + Mathf.Sin(time * pulseSpeed);
            aura.transform.localScale = new Vector3(scale, scale, 1);

            // 알파값 변화
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
        isInPattern = true;
        rb.velocity = Vector2.zero;

        Debug.Log("패턴: 손바닥 내려찍기");

        for (int i = 0; i < slamCount; i++)
        {
            // 손바닥 찍기 이펙트 생성
            CreateSlamEffect(transform.position, 4f, normalSlamColor, false);

            yield return new WaitForSeconds(slamInterval * 0.5f);

            // 360도 범위 공격
            float slamRadius = 4f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, slamRadius, LayerMask.GetMask("Player"));
            foreach (Collider2D hit in hits)
            {
                LaserShooter playerScript = hit.GetComponent<LaserShooter>();
                if (playerScript != null)
                {
                    playerScript.PlayerDie();
                }
            }

            yield return new WaitForSeconds(slamInterval * 0.5f);
        }

        isInPattern = false;
    }

    IEnumerator Pattern_EnergyWave()
    {
        isInPattern = true;
        rb.velocity = Vector2.zero;

        Debug.Log("패턴: 에너지 웨이브");

        float groundY = transform.position.y - 1.5f;
        int maxWaves = 10;
        int direction = (player.position.x > transform.position.x) ? 1 : -1;

        for (int i = 1; i <= maxWaves; i++)
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

        Debug.Log("패턴: 연속 돌진");

        for (int i = 0; i < rushTimes; i++)
        {
            // 플레이어를 넘어서까지 가기 위해 목표 위치 계산
            Vector2 playerPos = player.position;
            Vector2 direction = (playerPos - (Vector2)transform.position).normalized;

            // 플레이어를 넘어서 가는 추가 거리 포함
            Vector2 targetPosition = playerPos + (direction * rushOvershootDistance);

            // 돌진 경로 표시
            yield return StartCoroutine(ShowRushPath(targetPosition, false));

            // 돌진 실행
            yield return StartCoroutine(ExecuteRush(targetPosition, false));

            // 공중에 있다면 하강
            if (IsInAir())
            {
                yield return StartCoroutine(DescendAfterRush(false));
            }

            yield return new WaitForSeconds(rushCooldown);
        }

        isInPattern = false;
        Debug.Log("연속 돌진 패턴 종료");
    }

    // ----------------------------------------------------------
    //              돌진 경로 표시
    // ----------------------------------------------------------
    IEnumerator ShowRushPath(Vector2 targetPosition, bool isRageMode)
    {
        float warningTime = isRageMode ? rageRushWarningTime : rushWarningTime;
        Color pathColor = isRageMode ? rageRushPathColor : rushPathColor;

        // 돌진 경로 오브젝트 생성
        rushPathIndicator = new GameObject("RushPathIndicator");
        rushPathIndicator.transform.position = transform.position;
        RegisterEffect(rushPathIndicator);

        SpriteRenderer pathRenderer = rushPathIndicator.AddComponent<SpriteRenderer>();
        pathRenderer.sprite = lineSprite;
        pathRenderer.color = pathColor;
        pathRenderer.sortingOrder = -3;

        // 경로 방향 계산
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(transform.position, targetPosition);

        // 경로 설정
        rushPathIndicator.transform.right = direction;
        rushPathIndicator.transform.localScale = new Vector3(distance / 128f, rushPathWidth, 1f);

        // 경로 위치를 시작점과 끝점 사이의 중간으로 설정
        Vector2 midpoint = ((Vector2)transform.position + targetPosition) / 2f;
        rushPathIndicator.transform.position = midpoint;

        // 펄스 애니메이션
        float elapsed = 0f;
        float pulseSpeed = 5f;

        while (elapsed < warningTime && rushPathIndicator != null)
        {
            elapsed += Time.deltaTime;

            // 펄스 효과 (점멸)
            float alpha = 0.4f + Mathf.Sin(elapsed * pulseSpeed) * 0.3f;
            pathRenderer.color = new Color(pathColor.r, pathColor.g, pathColor.b, alpha);

            // 살짝 확대/축소 효과
            float pulseScale = 1f + Mathf.Sin(elapsed * pulseSpeed * 2f) * 0.1f;
            rushPathIndicator.transform.localScale = new Vector3(
                distance / 128f * pulseScale,
                rushPathWidth * pulseScale,
                1f
            );

            yield return null;
        }

        // 경로 표시 제거
        if (rushPathIndicator != null)
        {
            UnregisterEffect(rushPathIndicator);
            Destroy(rushPathIndicator);
            rushPathIndicator = null;
        }
    }

    // ----------------------------------------------------------
    //              돌진 실행
    // ----------------------------------------------------------
    IEnumerator ExecuteRush(Vector2 targetPosition, bool isRageMode)
    {
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        rushDirection = direction; // 돌진 방향 저장
        float timer = 0;

        float currentRushSpeed = isRageMode ? rageRushSpeed : rushSpeed;
        float currentRushDuration = isRageMode ? rageRushDuration : rushDuration;

        // 돌진 시작
        isRushing = true;
        Debug.Log($"돌진 시작 - 목표: {targetPosition}, 방향: {direction}, 속도: {currentRushSpeed}, 지속시간: {currentRushDuration}");

        // 돌진 중에 보스 주변에 이펙트 생성
        GameObject rushTrailEffect = CreateRushTrailEffect(isRageMode);

        while (timer < currentRushDuration)
        {
            timer += Time.deltaTime;

            // 목표 위치를 향해 직선으로 이동
            Vector2 currentDirection = (targetPosition - (Vector2)transform.position).normalized;
            rb.velocity = currentDirection * currentRushSpeed;

            // 돌진 이펙트 업데이트
            if (rushTrailEffect != null)
            {
                rushTrailEffect.transform.position = transform.position;
            }

            yield return null;
        }

        // 돌진 종료
        isRushing = false;
        rb.velocity = Vector2.zero;

        // 돌진 이펙트 정리
        if (rushTrailEffect != null)
        {
            UnregisterEffect(rushTrailEffect);
            Destroy(rushTrailEffect);
        }

        Debug.Log($"돌진 종료 - 현재 위치: {transform.position}");
    }

    GameObject CreateRushTrailEffect(bool isRageMode)
    {
        GameObject trail = new GameObject("RushTrail");
        trail.transform.position = transform.position;
        RegisterEffect(trail);

        SpriteRenderer trailRenderer = trail.AddComponent<SpriteRenderer>();
        trailRenderer.sprite = circleSprite;
        trailRenderer.color = isRageMode ?
            new Color(1f, 0f, 1f, 0.3f) :
            new Color(1f, 0.2f, 0.2f, 0.3f);
        trailRenderer.sortingOrder = -2;

        Coroutine trailRoutine = StartCoroutine(RushTrailAnimation(trail, isRageMode));
        activeCoroutines.Add(trailRoutine);

        return trail;
    }

    IEnumerator RushTrailAnimation(GameObject trail, bool isRageMode)
    {
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration && trail != null)
        {
            elapsed += Time.deltaTime;

            // 점점 커지면서 사라짐
            float scale = 2f + elapsed * 4f;
            trail.transform.localScale = new Vector3(scale, scale, 1f);

            // 페이드 아웃
            SpriteRenderer renderer = trail.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                float alpha = Mathf.Lerp(0.3f, 0f, elapsed / duration);
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
    //              공중 체크
    // ----------------------------------------------------------
    bool IsInAir()
    {
        // 간단한 방법: 일정 높이 이상인지 체크
        // 실제 게임에서는 레이캐스트로 지면과의 거리를 체크하는 것이 좋음
        float groundLevel = -3f; // 기본 지면 높이 (게임에 맞게 조정 필요)
        return transform.position.y > groundLevel + 0.5f;
    }

    // ----------------------------------------------------------
    //              돌진 후 하강 코루틴
    // ----------------------------------------------------------
    IEnumerator DescendAfterRush(bool isRageMode)
    {
        isDescending = true;

        // 하강 속도와 높이 설정
        float currentDescentSpeed = isRageMode ? rageDescentSpeed : descentSpeed;
        float groundLevel = -3f; // 게임의 지면 높이

        Debug.Log($"하강 시작 - 현재 높이: {transform.position.y}, 목표 높이: {groundLevel}, 속도: {currentDescentSpeed}");

        // 하강 애니메이션 (이펙트)
        CreateDescentEffect(isRageMode);

        // 하강
        while (transform.position.y > groundLevel + 0.1f)
        {
            // 아래로 하강 (중력 적용)
            Vector2 velocity = rb.velocity;
            velocity.y = -currentDescentSpeed;
            velocity.x *= 0.8f; // 수평 이동 감속
            rb.velocity = velocity;
            yield return null;
        }

        // 하강 종료
        rb.velocity = Vector2.zero;

        // 착지 이펙트
        CreateLandingEffect(isRageMode);

        yield return new WaitForSeconds(0.2f);
        isDescending = false;

        Debug.Log($"하강 완료 - 최종 위치: {transform.position}");
    }

    void CreateDescentEffect(bool isRageMode)
    {
        GameObject descentEffect = new GameObject("DescentEffect");
        descentEffect.transform.position = transform.position;
        RegisterEffect(descentEffect);

        SpriteRenderer renderer = descentEffect.AddComponent<SpriteRenderer>();
        renderer.sprite = circleSprite;
        renderer.color = isRageMode ? new Color(1f, 0.2f, 0.8f, 0.4f) : new Color(1f, 0.5f, 0.2f, 0.4f);
        renderer.sortingOrder = -2;

        Coroutine effectRoutine = StartCoroutine(DescentEffectAnimation(descentEffect, isRageMode));
        activeCoroutines.Add(effectRoutine);
    }

    IEnumerator DescentEffectAnimation(GameObject effect, bool isRageMode)
    {
        float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration && effect != null)
        {
            elapsed += Time.deltaTime;

            // 보스를 따라다니며 점점 커지기
            effect.transform.position = transform.position;
            float scale = 3f + elapsed * 3f;
            effect.transform.localScale = new Vector3(scale, scale * 0.5f, 1f);

            // 페이드 아웃
            SpriteRenderer renderer = effect.GetComponent<SpriteRenderer>();
            float alpha = Mathf.Lerp(0.4f, 0f, elapsed / duration);
            Color color = isRageMode ? new Color(1f, 0.2f, 0.8f, alpha) : new Color(1f, 0.5f, 0.2f, alpha);
            renderer.color = color;

            yield return null;
        }

        if (effect != null)
        {
            UnregisterEffect(effect);
            Destroy(effect);
        }
    }

    void CreateLandingEffect(bool isRageMode)
    {
        // 착지 충격파 이펙트
        CreateShockwave(transform.position, isRageMode);

        // 카메라 흔들기
        float shakeIntensity = isRageMode ? cameraShakeIntensity * 0.7f : cameraShakeIntensity * 0.5f;
        Coroutine shakeRoutine = StartCoroutine(ShakeCinemachineCamera(shakeIntensity, 0.5f));
        activeCoroutines.Add(shakeRoutine);
    }

    // ----------------------------------------------------------
    //              분노 패턴들 (강화된 패턴)
    // ----------------------------------------------------------

    IEnumerator Pattern_Slam_Rage()
    {
        isInPattern = true;
        rb.velocity = Vector2.zero;

        Debug.Log("패턴: 분노 손바닥 찍기!");

        for (int i = 0; i < rageSlamCount; i++)
        {
            // 분노 손바닥 이펙트 (더 크고 강렬한 효과)
            CreateSlamEffect(transform.position, rageSlamRadius, rageSlamColor, true);

            yield return new WaitForSeconds(rageSlamInterval * 0.3f);

            // 더 넓은 범위 공격
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, rageSlamRadius, LayerMask.GetMask("Player"));
            foreach (Collider2D hit in hits)
            {
                LaserShooter playerScript = hit.GetComponent<LaserShooter>();
                if (playerScript != null)
                {
                    playerScript.PlayerDie();
                }
            }

            // 카메라 흔들기
            Coroutine shakeRoutine = StartCoroutine(ShakeCinemachineCamera(cameraShakeIntensity * 1.5f, cameraShakeDuration));
            activeCoroutines.Add(shakeRoutine);

            yield return new WaitForSeconds(rageSlamInterval * 0.7f);
        }

        isInPattern = false;
    }

    IEnumerator Pattern_EnergyWave_Rage()
    {
        isInPattern = true;
        rb.velocity = Vector2.zero;

        Debug.Log("패턴: 분노 에너지 웨이브!");

        float groundY = transform.position.y - 1.5f;

        // 양쪽으로 동시에 웨이브 발사
        for (int i = 1; i <= rageWaveCount; i++)
        {
            // 오른쪽 웨이브
            Vector2 rightPos = new Vector2(transform.position.x + i * rageWaveSpacing, groundY);
            GameObject rightWave = Instantiate(energyWavePrefab, rightPos, Quaternion.identity);

            // 왼쪽 웨이브
            Vector2 leftPos = new Vector2(transform.position.x - i * rageWaveSpacing, groundY);
            GameObject leftWave = Instantiate(energyWavePrefab, leftPos, Quaternion.identity);

            // 분노 웨이브는 색상 변경
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

        Debug.Log("패턴: 분노 돌진!");

        for (int i = 0; i < rageRushTimes; i++)
        {
            // 플레이어를 더 많이 넘어서 가기 위해 목표 위치 계산
            Vector2 playerPos = player.position;
            Vector2 direction = (playerPos - (Vector2)transform.position).normalized;

            // 플레이어를 더 많이 넘어서 가는 추가 거리 포함
            Vector2 targetPosition = playerPos + (direction * rageRushOvershootDistance);

            // 돌진 경로 표시 (더 짧은 경고 시간)
            yield return StartCoroutine(ShowRushPath(targetPosition, true));

            // 돌진 실행
            yield return StartCoroutine(ExecuteRush(targetPosition, true));

            // 공중에 있다면 하강
            if (IsInAir())
            {
                yield return StartCoroutine(DescendAfterRush(true));
            }

            yield return new WaitForSeconds(rageRushCooldown);
        }

        isInPattern = false;
        Debug.Log("분노 돌진 패턴 종료");
    }

    IEnumerator Pattern_Laser()
    {
        isInPattern = true;
        rb.velocity = Vector2.zero;

        Debug.Log(isRage ? "패턴: 분노 레이저!" : "패턴: 전면 레이저");

        yield return new WaitForSeconds(0.8f);

        laserBeam.SetActive(true);

        if (isRage)
        {
            // 분노 레이저는 더 오래 지속
            yield return new WaitForSeconds(4f);
        }
        else
        {
            yield return new WaitForSeconds(3f);
        }

        laserBeam.SetActive(false);

        isInPattern = false;
    }

    IEnumerator Pattern_ComboAttack()
    {
        isInPattern = true;
        rb.velocity = Vector2.zero;

        Debug.Log("패턴: 연속 콤보 공격!");

        // 1. 손바닥 찍기
        yield return StartCoroutine(Pattern_Slam_Rage());

        // 2. 빠른 돌진
        yield return StartCoroutine(Pattern_Rush_Rage());

        // 3. 에너지 웨이브
        yield return StartCoroutine(Pattern_EnergyWave_Rage());

        isInPattern = false;
    }

    // ----------------------------------------------------------
    //              돌진 충돌 체크
    // ----------------------------------------------------------
    void CheckRushCollision()
    {
        if (player == null) return;

        // 플레이어와의 거리 계산
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
    //              시각 효과 생성 메서드 (코드로 생성)
    // ----------------------------------------------------------

    void CreateSlamEffect(Vector2 position, float radius, Color effectColor, bool isRageEffect)
    {
        // 기본 손바닥 이펙트
        GameObject effect = new GameObject("SlamEffect");
        effect.transform.position = position;
        RegisterEffect(effect);

        SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
        renderer.sprite = circleSprite;
        renderer.color = effectColor;
        renderer.sortingOrder = 10;

        // 머티리얼 설정 (캐시 사용)
        Material material = GetOrCreateMaterial("Sprites/Default", effectColor);
        if (material != null)
        {
            renderer.material = material;
        }

        // 크기 설정 (반지름에 맞춤)
        float baseSize = radius * 2f; // 128은 스프라이트 픽셀 단위
        effect.transform.localScale = new Vector3(baseSize, baseSize, 1f);

        // 분노 모드면 더 크게
        if (isRageEffect)
        {
            effect.transform.localScale *= 1.5f;
        }

        // 페이드 아웃 애니메이션
        Coroutine animationRoutine = StartCoroutine(SlamEffectAnimation(effect, isRageEffect ? rageSlamEffectDuration : slamEffectDuration));
        activeCoroutines.Add(animationRoutine);

        // 충격파 이펙트
        CreateShockwave(position, isRageEffect);

        // 카메라 흔들기
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

        float elapsed = 0f;
        Vector3 originalScale = effectTransform.localScale;
        Color originalColor = renderer.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 페이드 아웃
            float alpha = Mathf.Lerp(1f, 0f, t);
            renderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            // 살짝 커졌다 작아지는 효과
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
        renderer.color = new Color(1f, 1f, 1f, 0.5f); // 흰색 반투명
        renderer.sortingOrder = 9;

        // 충격파 애니메이션
        Coroutine animationRoutine = StartCoroutine(ShockwaveAnimation(shockwave, isRageEffect));
        activeCoroutines.Add(animationRoutine);
    }

    IEnumerator ShockwaveAnimation(GameObject shockwave, bool isRageEffect)
    {
        SpriteRenderer renderer = shockwave.GetComponent<SpriteRenderer>();
        Transform shockwaveTransform = shockwave.transform;

        float elapsed = 0f;
        float duration = shockwaveDuration;
        float startSize = 0.5f;
        float endSize = isRageEffect ? 4f : 3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 크기 증가
            float currentSize = Mathf.Lerp(startSize, endSize, t);
            shockwaveTransform.localScale = new Vector3(currentSize, currentSize, 1f);

            // 페이드 아웃
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
    //              카메라 흔들기 효과
    // ----------------------------------------------------------

    // Cinemachine 카메라 흔들림
    IEnumerator ShakeCinemachineCamera(float duration, float intensity)
    {
        if (virtualCamera == null) yield break;

        // Cinemachine Noise 컴포넌트 가져오기
        CinemachineBasicMultiChannelPerlin noise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        if (noise == null) yield break;

        // 기존 설정 저장
        float originalAmplitude = noise.m_AmplitudeGain;
        float originalFrequency = noise.m_FrequencyGain;

        // 흔들림 시작
        noise.m_AmplitudeGain = intensity;
        noise.m_FrequencyGain = intensity * 2f; // 진동 빈도

        float elapsed = 0f;

        // 점점 약해지는 흔들림
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // 시간에 따라 흔들림 강도 감소
            float t = elapsed / duration;
            float currentIntensity = Mathf.Lerp(intensity, 0f, t);

            noise.m_AmplitudeGain = currentIntensity;
            noise.m_FrequencyGain = currentIntensity * 2f;

            yield return null;
        }

        // 원래 설정으로 복원
        noise.m_AmplitudeGain = originalAmplitude;
        noise.m_FrequencyGain = originalFrequency;
    }

    // ----------------------------------------------------------
    //                 보스 데미지 처리
    // ----------------------------------------------------------
    public void ApplyDamage(float damage)
    {
        currentHP -= damage;

        // 피격 효과 (빨간색으로 변했다가 복원)
        Coroutine flashRoutine = StartCoroutine(FlashRed());
        activeCoroutines.Add(flashRoutine);

        // 피격 이펙트
        CreateHitEffect();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // Stage1Boss의 FlashRed 효과 적용
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

            // 빠르게 커지면서 사라짐
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

        // 모든 활성 코루틴 정리
        foreach (var coroutine in activeCoroutines)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        activeCoroutines.Clear();

        rb.velocity = Vector2.zero;
        isInPattern = true;

        Debug.Log("스테이지 3 보스 사망!");

        // 죽음 이펙트
        CreateDeathEffect();

        // 생성된 모든 이펙트 정리
        CleanupAllEffects();

        // Material 캐시 정리
        ClearMaterialCache();

        // Texture 풀 정리
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
            // 랜덤 위치 계산
            Vector2 randomOffset = Random.insideUnitCircle * explosionRadius;
            Vector2 spawnPos = (Vector2)transform.position + randomOffset;

            // 프리팹 인스턴스 생성
            GameObject explosion = Instantiate(
                deathExplosionPrefab,
                spawnPos,
                Quaternion.identity
            );

            // 랜덤 회전 적용 (선택사항)
            if (Random.value > 0.5f)
            {
                explosion.transform.Rotate(0f, 0f, Random.Range(0f, 360f));
            }

            // 랜덤 크기 적용 (선택사항)
            float randomScale = Random.Range(0.8f, 1.2f);
            explosion.transform.localScale = Vector3.one * randomScale;

            // 생성된 이펙트를 자식으로 설정하여 관리
            explosion.transform.parent = transform;

            yield return new WaitForSeconds(explosionInterval);
        }
    }

    // ----------------------------------------------------------
    //                 메모리 관리 메서드 (Stage1Boss에서 가져옴)
    // ----------------------------------------------------------

    // Material 캐싱 및 재사용
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
        foreach (var mat in materialCache.Values)
        {
            if (mat != null)
                Destroy(mat);
        }
        materialCache.Clear();
    }

    // Texture2D 풀 관리
    private Texture2D GetTextureFromPool(int width, int height)
    {
        // 풀에서 적절한 텍스처 찾기
        foreach (var texture in texturePool)
        {
            if (texture != null && texture.width == width && texture.height == height)
            {
                // 풀에서 제거하고 반환
                var list = new List<Texture2D>(texturePool);
                list.Remove(texture);
                texturePool = new Queue<Texture2D>(list);
                return texture;
            }
        }

        // 풀에 없으면 새로 생성
        Texture2D newTexture = new Texture2D(width, height);
        return newTexture;
    }

    private void ClearTexturePool()
    {
        foreach (var texture in texturePool)
        {
            if (texture != null)
                Destroy(texture);
        }
        texturePool.Clear();
    }

    // 이펙트 관리
    private void RegisterEffect(GameObject effect)
    {
        if (effect != null && !activeEffects.Contains(effect))
            activeEffects.Add(effect);
    }

    private void UnregisterEffect(GameObject effect)
    {
        if (effect != null)
            activeEffects.Remove(effect);
    }

    private void CleanupAllEffects()
    {
        foreach (var effect in activeEffects)
        {
            if (effect != null)
                Destroy(effect);
        }
        activeEffects.Clear();
    }

    void OnDestroy()
    {
        // 모든 리소스 정리
        StopAllCoroutines();
        CleanupAllEffects();
        ClearMaterialCache();
        ClearTexturePool();
    }

    // ----------------------------------------------------------
    //              시각화용 Gizmos (디버그 범위 표시)
    // ----------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying) return;

        // 에디터에서 선택 시 기본 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 4f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rushDamageRadius);
    }
}