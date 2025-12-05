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
    public Animator anim;
    public LaserBeam laserBeam;
    public GameObject energyWavePrefab;

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
    public float rushSpeed = 12f;
    public float rushDuration = 0.45f;  // 돌진 지속 시간
    public float rushCooldown = 0.4f;   // 돌진 사이 대기 시간

    [Header("분노 패턴 설정")]
    public int rageSlamCount = 5;               // 분노 모드 손바닥 찍기 횟수 증가
    public float rageSlamInterval = 0.5f;       // 더 빠른 간격
    public float rageSlamRadius = 6f;           // 범위 증가
    public int rageWaveCount = 15;              // 더 많은 웨이브
    public float rageWaveInterval = 0.15f;      // 더 빠른 웨이브
    public float rageWaveSpacing = 2f;          // 더 넓은 간격
    public float rageRushSpeed = 18f;           // 더 빠른 돌진
    public int rageRushTimes = 6;               // 더 많은 돌진 횟수
    public float rageRushDuration = 0.35f;      // 더 짧은 돌진 지속 시간
    public float rageRushCooldown = 0.3f;       // 더 짧은 대기 시간

    [Header("시각 효과 설정")]
    public float slamEffectDuration = 1.5f;
    public float shockwaveDuration = 0.8f;
    public float rageSlamEffectDuration = 2f;
    public Color normalSlamColor = Color.red;
    public Color rageSlamColor = Color.magenta;
    public float cameraShakeIntensity = 0.3f;
    public float cameraShakeDuration = 0.2f;
    public Material circleMaterial;             // 원형 이펙트에 사용할 머티리얼

    [Header("디버그 설정")]
    public bool showSlamRange = true;           // 손바닥 범위 시각화
    public bool showRushRange = true;           // 돌진 범위 시각화
    public Color debugSlamColor = Color.red;    // 디버그 색상
    public Color debugRushColor = Color.yellow; // 돌진 디버그 색상

    [Header("충돌 데미지 설정")]
    public float rushDamageRadius = 2f;         // 돌진 시 데미지 반경
    public bool canDealRushDamage = true;       // 돌진 데미지 활성화

    private Rigidbody2D rb;
    private bool isRage = false;
    private bool isInPattern = false;
    private Camera mainCamera;
    private Vector3 originalCameraPos;
    private Sprite circleSprite;                // 동적으로 생성할 원형 스프라이트
    private bool isRushing = false;             // 돌진 중인지 확인
    private float lastRushTime = 0f;            // 마지막 돌진 시간
    private Vector2 rushDirection;              // 현재 돌진 방향

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

        // 보스 스프라이트 렌더러 참조
        bossSpriteRenderer = GetComponent<SpriteRenderer>();
        if (bossSpriteRenderer != null)
        {
            originalBossColor = bossSpriteRenderer.color;
        }

        // 원형 스프라이트 생성
        circleSprite = CreateCircleSprite();

        currentHP = maxHP;

        // 코루틴을 리스트에 추가
        Coroutine routine = StartCoroutine(BossRoutine());
        activeCoroutines.Add(routine);
    }

    void Update()
    {
        if (!isInPattern)
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
    //              동적으로 원형 스프라이트 생성
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
        Coroutine shakeRoutine = StartCoroutine(CameraShake(0.5f, 0.5f));
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
            Vector2 dir = (player.position - transform.position).normalized;
            rushDirection = dir; // 돌진 방향 저장
            float timer = 0;

            // 돌진 시작
            isRushing = true;
            Debug.Log($"돌진 #{i + 1} 시작 - 방향: {dir}, 속도: {rushSpeed}");

            while (timer < rushDuration)
            {
                timer += Time.deltaTime;
                rb.velocity = dir * rushSpeed;
                yield return null;
            }

            // 돌진 종료
            isRushing = false;
            rb.velocity = Vector2.zero;
            yield return new WaitForSeconds(rushCooldown);
        }

        isInPattern = false;
        Debug.Log("연속 돌진 패턴 종료");
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
            Coroutine shakeRoutine = StartCoroutine(CameraShake(cameraShakeIntensity * 1.5f, cameraShakeDuration));
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
            Vector2 dir = (player.position - transform.position).normalized;
            rushDirection = dir; // 돌진 방향 저장
            float timer = 0;

            // 분노 돌진 이펙트 생성
            if (i % 2 == 0) // 짝수 번째 돌진마다 이펙트
            {
                CreateShockwave(transform.position, true);
            }

            // 돌진 시작
            isRushing = true;
            Debug.Log($"분노 돌진 #{i + 1} 시작 - 방향: {dir}, 속도: {rageRushSpeed}");

            while (timer < rageRushDuration)
            {
                timer += Time.deltaTime;
                rb.velocity = dir * rageRushSpeed;
                yield return null;
            }

            // 돌진 종료
            isRushing = false;
            rb.velocity = Vector2.zero;
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
        Coroutine shakeRoutine = StartCoroutine(CameraShake(
            isRageEffect ? cameraShakeIntensity * 2f : cameraShakeIntensity,
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

    IEnumerator CameraShake(float intensity, float duration)
    {
        if (mainCamera == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = originalCameraPos.x + Random.Range(-intensity, intensity);
            float y = originalCameraPos.y + Random.Range(-intensity, intensity);

            mainCamera.transform.position = new Vector3(x, y, originalCameraPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = originalCameraPos;
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

        // 데미지 받을 때마다 작은 카메라 흔들기
        Coroutine shakeRoutine = StartCoroutine(CameraShake(0.1f, 0.1f));
        activeCoroutines.Add(shakeRoutine);

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
    }

    void CreateDeathEffect()
    {
        Coroutine deathEffectRoutine = StartCoroutine(DeathEffectAnimation());
        activeCoroutines.Add(deathEffectRoutine);
    }

    IEnumerator DeathEffectAnimation()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * 3f;
            CreateSlamEffect(spawnPos, Random.Range(1f, 3f), Color.black, false);
            yield return new WaitForSeconds(0.1f);
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

    private void ReturnTextureToPool(Texture2D texture)
    {
        if (texture != null)
        {
            // 텍스처 초기화 (투명하게)
            Color[] clearColors = new Color[texture.width * texture.height];
            for (int i = 0; i < clearColors.Length; i++)
                clearColors[i] = Color.clear;
            texture.SetPixels(clearColors);
            texture.Apply();

            texturePool.Enqueue(texture);
        }
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
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // 돌진 범위 시각화 (돌진 중일 때만)
        if (showRushRange && isRushing)
        {
            Gizmos.color = debugRushColor;
            Gizmos.DrawWireSphere(transform.position, rushDamageRadius);

            // 돌진 방향 시각화
            Gizmos.color = Color.cyan;
            Vector3 start = transform.position;
            Vector3 end = start + (Vector3)rushDirection * 3f;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(end, 0.5f);
        }
    }

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