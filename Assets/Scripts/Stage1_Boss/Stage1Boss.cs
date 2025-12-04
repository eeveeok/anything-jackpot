using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stage1Boss : MonoBehaviour
{
    public enum BossPattern
    {
        Idle,
        Follow,          // 기본 추적 AI
        GroundSlam,      // 땅울림 포효
        FeatherStorm,    // 깃털 폭풍
        MeteorShower,    // 메테오 샤워
        Rage             // 분노 모드
    }

    [Header("레퍼런스")]
    public Transform player;
    public Animator anim;
    public GameObject meteorPrefab;        // 메테오 프리팹
    public GameObject featherPrefab;       // 깃털 프리팹

    [Header("보스 스탯")]
    public float maxHP = 800f;
    public float currentHP;

    [Header("추적 AI")]
    public float followSpeed = 8f;
    public float followStopDistance = 2f;   // 플레이어와 거리 유지

    [Header("패턴 설정")]
    public float idleDelay = 2f;
    public float groundSlamHeight = 10f;    // 점프 높이
    public float groundSlamRadius = 5f;     // 충격파 반경
    public int featherCount = 30;           // 깃털 발사 개수
    public float featherSpeed = 8f;         // 깃털 속도
    public int meteorCount = 20;            // 메테오 개수
    public float meteorWarningTime = 1f;    // 그림자 경고 시간
    public int rageMeteorCount = 30;        // 분노 모드 메테오 개수

    [Header("패턴 위치")]
    public Vector2 featherStormPosition; // 우측 위치
    public float meteorShowerHeight = 15f;  // 상승 높이

    [Header("카메라 설정")]
    public CinemachineVirtualCamera virtualCamera;
    public float cameraShakeIntensity = 20f;
    public float cameraShakeDuration = 1.5f;

    private Rigidbody2D rb;
    private bool isRage = false;
    private bool isInPattern = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D bossCollider;

    // 보스 색상 관련
    private SpriteRenderer bossSpriteRenderer;
    private Color originalBossColor;

    // 메모리 관리
    private List<GameObject> activeEffects = new List<GameObject>();
    private Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
    private Queue<Texture2D> texturePool = new Queue<Texture2D>();
    private List<Coroutine> activeCoroutines = new List<Coroutine>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bossCollider = GetComponent<Collider2D>();

        // 보스 스프라이트 렌더러 참조
        bossSpriteRenderer = GetComponent<SpriteRenderer>();
        if (bossSpriteRenderer != null)
        {
            originalBossColor = bossSpriteRenderer.color;
        }

        currentHP = maxHP;
        
        // 코루틴을 리스트에 추가
        Coroutine routine = StartCoroutine(BossRoutine());
        activeCoroutines.Add(routine);
    }

    // Update 메서드에 추가
    void Update()
    {
        if (!isInPattern)
        {
            FollowPlayer();
        }

        // 보스가 플레이어를 바라보도록 업데이트
        UpdateBossFacing();
    }

    // ----------------------------------------------------------
    //              기본 추적 AI
    // ----------------------------------------------------------
    void UpdateBossFacing()
    {
        if (player == null) return;

        // 플레이어가 보스보다 왼쪽에 있으면 스케일을 -1 (왼쪽 보기)
        // 플레이어가 보스보다 오른쪽에 있으면 스케일을 1 (오른쪽 보기)
        if (player.position.x < transform.position.x)
        {
            // 왼쪽을 보도록 (스케일 x를 -1)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x),
                                              transform.localScale.y,
                                              transform.localScale.z);
        }
        else
        {
            // 오른쪽을 보도록 (스케일 x를 1)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x),
                                              transform.localScale.y,
                                              transform.localScale.z);
        }
    }

    void FollowPlayer()
    {
        if (player == null || player.GetComponent<LaserShooter>().isDead) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // 너무 가까우면 멈춤
        if (distance <= followStopDistance)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // 플레이어 방향
        Vector2 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        dir.Normalize(); // y를 0으로 만들었으니 다시 정규화

        rb.velocity = dir * followSpeed;
    }

    // ----------------------------------------------------------
    //              보스 패턴 메인 루프
    // ----------------------------------------------------------
    private int lastNormalPattern = -1;  // 일반 모드에서 마지막으로 사용한 패턴
    private int lastRagePattern = -1;    // 분노 모드에서 마지막으로 사용한 패턴

    IEnumerator BossRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(idleDelay);

            // HP 40% 이하 → 분노 돌입
            if (!isRage && currentHP <= maxHP * 0.4f)
            {
                isRage = true;
                //anim.SetTrigger("rage");
                Debug.Log("보스 분노 모드 돌입!");
                yield return new WaitForSeconds(1.5f);

                // 분노 모드 진입 시 패턴 기록 초기화
                lastNormalPattern = -1;
                lastRagePattern = -1;
            }

            // 패턴 선택
            if (!isRage)
            {
                int p = GetNextPattern(false);  // 일반 모드 패턴 선택

                switch (p)
                {
                    case 0:
                        lastNormalPattern = 0;
                        yield return StartCoroutine(Pattern_GroundSlam());
                        break;
                    case 1:
                        lastNormalPattern = 1;
                        yield return StartCoroutine(Pattern_FeatherStorm());
                        break;
                    case 2:
                        lastNormalPattern = 2;
                        yield return StartCoroutine(Pattern_MeteorShower());
                        break;
                }
            }
            else
            {
                // 분노 모드: 더 강력한 패턴
                int p = GetNextPattern(true);  // 분노 모드 패턴 선택

                switch (p)
                {
                    case 0:
                        lastRagePattern = 0;
                        yield return StartCoroutine(Pattern_GroundSlam_Rage());
                        break;
                    case 1:
                        lastRagePattern = 1;
                        yield return StartCoroutine(Pattern_FeatherStorm_Rage());
                        break;
                    case 2:
                        lastRagePattern = 2;
                        yield return StartCoroutine(Pattern_MeteorShower_Rage());
                        break;
                }
            }
        }
    }

    // 중복되지 않는 패턴 선택 메서드
    private int GetNextPattern(bool isRageMode)
    {
        int availablePatternsCount = 3;  // 패턴 총 개수

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
    //              개별 패턴 정의 (일반 모드)
    // ----------------------------------------------------------

    IEnumerator Pattern_GroundSlam()
    {
        isInPattern = true;
        rb.velocity = Vector2.zero;

        Debug.Log("패턴: 땅울림 포효");

        float elapsedTime = 0f;
        Vector2 startPos = transform.position;
        Vector2 slamTarget = new Vector2(startPos.x, startPos.y - 0.5f); // 착지 지점

        // --------------------------------------------------
        // 1. 상승 전 색상 변화 (까맣게 변했다가 복원)
        // --------------------------------------------------
        if (bossSpriteRenderer != null)
        {
            // 서서히 까맣게 변하기
            float darkenTime = 0.3f;
            elapsedTime = 0f;

            while (elapsedTime < darkenTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / darkenTime;

                // 검은색으로 서서히 변화
                bossSpriteRenderer.color = Color.Lerp(Color.white, Color.black, t);
                yield return null;
            }

            // 완전히 검은색으로
            bossSpriteRenderer.color = Color.black;

            // 잠시 유지
            yield return new WaitForSeconds(0.2f);

            // 다시 원래 색으로 복원
            elapsedTime = 0f;
            while (elapsedTime < darkenTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / darkenTime;

                // 원래 색으로 복원
                bossSpriteRenderer.color = Color.Lerp(Color.black, Color.white, t);
                yield return null;
            }

            // 정확히 원래 색으로
            bossSpriteRenderer.color = Color.white;
        }

        // --------------------------------------------------
        // 2. 높이 점프
        // --------------------------------------------------
        Vector2 jumpTarget = startPos + Vector2.up * groundSlamHeight;

        float jumpTime = 0.5f;
        elapsedTime = 0f;

        // 점프 애니메이션
        while (elapsedTime < jumpTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / jumpTime;
            transform.position = Vector2.Lerp(startPos, jumpTarget, t);
            yield return null;
        }

        // --------------------------------------------------
        // 3. 잠시 체공
        // --------------------------------------------------
        yield return new WaitForSeconds(0.5f);

        // --------------------------------------------------
        // 4. 바닥 충격 (내려찍기)
        // --------------------------------------------------
        //anim.SetTrigger("slam");

        elapsedTime = 0f;
        float slamTime = 0.2f;

        while (elapsedTime < slamTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / slamTime;
            float easedT = Mathf.Pow(t, 3f);
            transform.position = Vector2.Lerp(jumpTarget, slamTarget, easedT);
            yield return null;
        }

        // 정확히 착지 지점에 위치
        transform.position = slamTarget;
        slamTarget = new Vector2(slamTarget.x, slamTarget.y - 0.7f);

        // --------------------------------------------------
        // 5. Cinemachine 카메라 흔들림
        // --------------------------------------------------
        Coroutine shakeRoutine = StartCoroutine(ShakeCinemachineCamera(cameraShakeDuration, cameraShakeIntensity));
        activeCoroutines.Add(shakeRoutine);

        // --------------------------------------------------
        // 6. 즉시 데미지 처리
        // --------------------------------------------------
        ApplyGroundSlamDamage(slamTarget);

        // --------------------------------------------------
        // 7. 충격파 생성
        // --------------------------------------------------
        Coroutine shockwaveRoutine = StartCoroutine(CreateShockwave(slamTarget));
        activeCoroutines.Add(shockwaveRoutine);
        
        yield return shockwaveRoutine;

        yield return new WaitForSeconds(0.5f);
        isInPattern = false;
    }

    // 착지 시 데미지 처리 (땅에 닿아있으면 무조건 데미지)
    void ApplyGroundSlamDamage(Vector2 center)
    {
        LaserShooter laserShooter = player.GetComponent<LaserShooter>();

        if (laserShooter == null || laserShooter.isDead || laserShooter.IsInvincible) return;

        // 플레이어가 땅에 닿아있는지 확인
        if (laserShooter.IsGrounded)
        {
            laserShooter.PlayerDie();
        }
    }

    // 충격파 이펙트
    IEnumerator CreateShockwave(Vector2 center)
    {
        center = new Vector2(center.x, center.y - 1.5f);

        // --------------------------------------------------
        // 1. 주 충격파 (지면을 따라가는 두꺼운 원)
        // --------------------------------------------------
        GameObject mainShockwave = CreateGroundShockwave(center);
        RegisterEffect(mainShockwave);

        // --------------------------------------------------
        // 2. 중앙 폭발 이펙트
        // --------------------------------------------------
        GameObject explosion = CreateCentralExplosion(center);
        if (explosion != null)
        {
            RegisterEffect(explosion);
            Destroy(explosion, 1f);
        }

        // --------------------------------------------------
        // 3. 주 충격파 애니메이션
        // --------------------------------------------------
        float duration = 1.2f; // 더 긴 지속 시간
        float elapsed = 0f;

        LineRenderer lr = mainShockwave.GetComponent<LineRenderer>();
        Transform shockwaveTransform = mainShockwave.transform;

        // 초기 크기 설정
        float startRadius = 0.5f;
        float endRadius = groundSlamRadius * 2.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 현재 반경 (처음엔 빠르게, 나중엔 느리게)
            float radius = Mathf.Lerp(startRadius, endRadius, EaseOutQuart(t));

            // 원형으로 포인트 배치
            for (int i = 0; i < lr.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2 / lr.positionCount;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                lr.SetPosition(i, new Vector3(point.y > center.y ? point.x : point.x,
                                              Mathf.Max(point.y, center.y - 0.3f), 0)); // 지면 아래로 안내려가게
            }

            // 색상과 알파값 변화
            UpdateShockwaveColor(lr, t);

            // 크기 변화로 파동 효과
            float waveEffect = Mathf.Sin(t * Mathf.PI * 8f) * 0.3f + 1f;
            lr.startWidth = 0.3f * waveEffect;
            lr.endWidth = 0.1f * waveEffect;

            yield return null;
        }

        // 서서히 사라짐
        float fadeDuration = 0.3f;
        elapsed = 0f;

        Material material = lr.material;
        if (material != null)
        {
            Color originalColor = material.color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;

                Color fadedColor = originalColor;
                fadedColor.a = Mathf.Lerp(originalColor.a, 0f, t);
                material.color = fadedColor;

                yield return null;
            }
        }

        UnregisterEffect(mainShockwave);
        Destroy(mainShockwave);
    }

    // Easing 함수
    float EaseOutQuart(float t)
    {
        return 1f - Mathf.Pow(1f - t, 4f);
    }

    // 주 충격파 생성
    GameObject CreateGroundShockwave(Vector2 center)
    {
        GameObject shockwave = new GameObject("GroundShockwave");
        shockwave.transform.position = center;

        LineRenderer lr = shockwave.AddComponent<LineRenderer>();
        lr.startWidth = 0.3f;
        lr.endWidth = 0.1f;
        lr.positionCount = 128; // 더 많은 포인트로 부드러운 원

        // 재질 설정 (캐시 사용)
        Material material = GetOrCreateMaterial("Sprites/Default", new Color(1f, 0.5f, 0f, 0.8f));
        lr.material = material;

        // 그림자 효과를 위한 두 번째 라인 (옵션)
        CreateSecondaryShockwave(center);

        return shockwave;
    }

    // 보조 충격파 (더 얇고 빠른)
    void CreateSecondaryShockwave(Vector2 center)
    {
        GameObject secondary = new GameObject("SecondaryShockwave");
        secondary.transform.position = center;
        RegisterEffect(secondary);

        LineRenderer lr = secondary.AddComponent<LineRenderer>();
        lr.startWidth = 0.15f;
        lr.endWidth = 0.05f;
        lr.positionCount = 64;

        // 재질 설정 (캐시 사용)
        Material material = GetOrCreateMaterial("Sprites/Default", new Color(1f, 1f, 0.5f, 0.6f));
        lr.material = material;

        Coroutine routine = StartCoroutine(AnimateSecondaryShockwave(lr, center));
        activeCoroutines.Add(routine);
    }

    IEnumerator AnimateSecondaryShockwave(LineRenderer lr, Vector2 center)
    {
        float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float radius = Mathf.Lerp(0.3f, groundSlamRadius * 1.8f, t * 1.5f);

            for (int i = 0; i < lr.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2 / lr.positionCount;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                lr.SetPosition(i, new Vector3(point.x, point.y, 0));
            }

            // 알파값 감소
            Color color = lr.material.color;
            color.a = Mathf.Lerp(0.6f, 0f, t);
            lr.material.color = color;

            yield return null;
        }

        if (lr != null && lr.gameObject != null)
        {
            UnregisterEffect(lr.gameObject);
            Destroy(lr.gameObject);
        }
    }

    // 충격파 색상 업데이트
    void UpdateShockwaveColor(LineRenderer lr, float t)
    {
        if (lr.material == null) return;

        // 시간에 따라 색상 변화: 빨강 -> 오렌지 -> 노랑 -> 흰색
        Color color;
        if (t < 0.3f)
            color = Color.Lerp(Color.red, new Color(1f, 0.5f, 0f), t / 0.3f); // 빨강 -> 오렌지
        else if (t < 0.6f)
            color = Color.Lerp(new Color(1f, 0.5f, 0f), Color.yellow, (t - 0.3f) / 0.3f); // 오렌지 -> 노랑
        else
            color = Color.Lerp(Color.yellow, Color.white, (t - 0.6f) / 0.4f); // 노랑 -> 흰색

        color.a = Mathf.Lerp(0.8f, 0.3f, t); // 점점 투명해짐
        lr.material.color = color;
    }

    // 중앙 폭발 이펙트
    GameObject CreateCentralExplosion(Vector2 center)
    {
        GameObject explosion = new GameObject("CentralExplosion");
        explosion.transform.position = center;

        // 여러 원형 스프라이트로 구성
        for (int i = 0; i < 3; i++)
        {
            GameObject ring = new GameObject($"ExplosionRing_{i}");
            ring.transform.position = center;
            ring.transform.parent = explosion.transform;

            SpriteRenderer sr = ring.AddComponent<SpriteRenderer>();
            
            // 텍스처 풀에서 가져오기
            Texture2D texture = GetTextureFromPool(16, 16);
            if (texture != null)
            {
                Sprite sprite = CreateCircleSprite(texture, 16, new Color(1f, 0.8f - i * 0.2f, 0.3f, 0.7f - i * 0.2f));
                sr.sprite = sprite;
            }

            Coroutine routine = StartCoroutine(AnimateExplosionRing(ring, i * 0.1f));
            activeCoroutines.Add(routine);
        }

        return explosion;
    }

    IEnumerator AnimateExplosionRing(GameObject ring, float delay)
    {
        yield return new WaitForSeconds(delay);

        Transform ringTransform = ring.transform;
        SpriteRenderer sr = ring.GetComponent<SpriteRenderer>();

        if (sr == null) yield break;

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 크기 확대
            float scale = Mathf.Lerp(0.1f, 2f, t);
            ringTransform.localScale = Vector3.one * scale;

            // 서서히 사라짐
            Color color = sr.color;
            color.a = Mathf.Lerp(0.7f, 0f, t);
            sr.color = color;

            yield return null;
        }

        Destroy(ring);
    }

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

    IEnumerator Pattern_FeatherStorm()
    {
        isInPattern = true;
        rb.velocity = Vector2.zero;

        Debug.Log("패턴: 깃털 폭풍");

        // --------------------------------------------------
        // 1. 우측으로 빠르게 이동
        // --------------------------------------------------
        Vector2 startPos = transform.position;
        Vector2 targetPos = featherStormPosition;

        float moveTime = 0.8f;
        float elapsedTime = 0f;

        // 대시 시작 시 강한 밀어내기
        PushPlayerToLeft(featherStormPosition.x, 30f);

        while (elapsedTime < moveTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveTime;

            // 부드러운 이동
            float easedT = EaseOutCubic(t);
            transform.position = Vector2.Lerp(startPos, targetPos, easedT);

            // 이동 중 지속적인 밀어내기 (처음에는 강하게, 나중에는 약하게)
            float pushForce = Mathf.Lerp(15f, 5f, t);
            PushPlayerToLeft(featherStormPosition.x, pushForce);

            yield return null;
        }

        // --------------------------------------------------
        // 2. 보스 위치 고정 확인
        // --------------------------------------------------
        transform.position = targetPos;
        Debug.Log($"보스 이동 완료: {transform.position.x}, 플레이어 위치: {player.position.x}");

        // --------------------------------------------------
        // 3. 추가 강제 밀어내기 (플레이어가 여전히 오른쪽에 있을 경우)
        // --------------------------------------------------
        if (player.position.x > featherStormPosition.x)
        {
            Debug.Log($"플레이어가 보스보다 오른쪽에 있음! 강제 밀어내기 실행");
            ForcePushPlayerToLeft(featherStormPosition.x - 5f);
        }

        // --------------------------------------------------
        // 4. 깃털 연사
        // --------------------------------------------------
        yield return new WaitForSeconds(0.5f);

        List<GameObject> spawnedFeathers = new List<GameObject>();

        for (int i = 0; i < featherCount; i++)
        {
            // 깃털 발사 위치
            Vector2 spawnPos = new Vector2(transform.position.x - 5, (transform.position.y - 2) + Random.Range(0, 3) * 2);

            GameObject feather = Instantiate(featherPrefab, spawnPos, featherPrefab.transform.rotation);
            spawnedFeathers.Add(feather);

            // FeatherProjectile 컴포넌트 가져오기
            FeatherProjectile featherProjectile = feather.GetComponent<FeatherProjectile>();
            if (featherProjectile != null)
            {
                // 속도 설정
                featherProjectile.speed = featherSpeed;
            }

            // Rigidbody2D 설정 (Kinematic으로)
            Rigidbody2D featherRb = feather.GetComponent<Rigidbody2D>();
            if (featherRb != null)
            {
                featherRb.isKinematic = true; // 다른 물체에 영향을 받지 않음
            }

            yield return new WaitForSeconds(0.5f);
        }

        // 일정 시간 후 깃털 정리
        yield return new WaitForSeconds(2f);
        
        foreach (var feather in spawnedFeathers)
        {
            if (feather != null)
                Destroy(feather);
        }

        yield return new WaitForSeconds(0.5f);
        isInPattern = false;
    }

    // 플레이어를 왼쪽으로 밀어내는 함수
    void PushPlayerToLeft(float targetX, float force)
    {
        if (player == null) return;

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        LaserShooter playerScript = player.GetComponent<LaserShooter>();

        if (playerRb == null || playerScript == null || playerScript.isDead) return;

        // 플레이어가 targetX보다 오른쪽에 있으면 왼쪽으로 밀기
        if (player.position.x > targetX)
        {
            Vector2 pushDirection = Vector2.left;
            playerRb.AddForce(pushDirection * force * 100f, ForceMode2D.Force);
        }
        else
        {
            // 이미 왼쪽에 있더라도 약간 더 왼쪽으로 밀기
            Vector2 pushDirection = Vector2.left;
            playerRb.AddForce(pushDirection * force * 10f, ForceMode2D.Force);
        }
    }

    // 강제로 플레이어를 특정 X 위치로 밀어내는 함수
    void ForcePushPlayerToLeft(float targetX)
    {
        if (player == null) return;

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        LaserShooter playerScript = player.GetComponent<LaserShooter>();

        if (playerRb == null || playerScript == null || playerScript.isDead) return;

        // 현재 플레이어가 목표보다 오른쪽에 있는지 확인
        if (player.position.x > targetX)
        {
            // 강한 임펄스로 즉시 왼쪽으로 밀기
            float distance = player.position.x - targetX;
            float impulseForce = Mathf.Clamp(distance * 5f, 10f, 50f);

            Vector2 pushDirection = Vector2.left;
            playerRb.AddForce(pushDirection * impulseForce, ForceMode2D.Impulse);

            Debug.Log($"강제 밀어내기: 거리={distance}, 힘={impulseForce}");

            PushPlayerToLeft(featherStormPosition.x, 30f);

            // 0.5초 후에도 여전히 오른쪽에 있으면 위치 강제 조정
            Coroutine routine = StartCoroutine(CheckAndForcePosition(targetX));
            activeCoroutines.Add(routine);
        }
    }

    // 위치 확인 후 필요시 강제 이동
    IEnumerator CheckAndForcePosition(float targetX)
    {
        yield return new WaitForSeconds(0.5f);

        if (player == null) yield break;

        // 여전히 목표보다 오른쪽에 있으면 강제 이동
        if (player.position.x > targetX)
        {
            Debug.Log($"플레이어 강제 이동: {player.position.x} -> {targetX}");
            Vector3 newPos = player.position;
            newPos.x = targetX;
            player.position = newPos;

            // 속도 초기화
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.velocity = new Vector2(0, playerRb.velocity.y);
            }
        }
    }

    // Easing 함수
    float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    IEnumerator Pattern_MeteorShower()
    {
        isInPattern = true;

        // 기존 중력 스케일 저장
        float originalGravityScale = rb.gravityScale;

        // 중력 비활성화
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;

        Debug.Log("패턴: 메테오 샤워");

        // 1. 수직 상승 (화면 밖으로)
        Vector2 startPos = transform.position;

        // 현재 카메라 기준으로 화면 상단보다 위로
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            float screenTop = mainCamera.transform.position.y + mainCamera.orthographicSize;
            Vector2 ascendTarget = new Vector2(startPos.x, screenTop + 10f);

            float ascendTime = 1f;
            float elapsedTime = 0f;

            // 충돌 비활성화 (상승 중)
            if (bossCollider != null) bossCollider.enabled = false;

            while (elapsedTime < ascendTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / ascendTime;

                float newY = Mathf.Lerp(startPos.y, ascendTarget.y, t);
                transform.position = new Vector2(startPos.x, newY);

                yield return null;
            }

            // 2. 메테오 생성
            Coroutine meteorRoutine = StartCoroutine(SpawnMeteors(meteorCount, ascendTarget));
            activeCoroutines.Add(meteorRoutine);
            yield return meteorRoutine;

            // 3. 하강
            elapsedTime = 0f;
            while (elapsedTime < ascendTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / ascendTime;

                float currentY = transform.position.y;
                float newY = Mathf.Lerp(currentY, startPos.y, t);
                transform.position = new Vector2(startPos.x, newY);

                yield return null;
            }
        }

        // 충돌 다시 활성화
        if (bossCollider != null) bossCollider.enabled = true;

        // 중력 스케일 복원
        rb.gravityScale = originalGravityScale;

        yield return new WaitForSeconds(0.5f);
        isInPattern = false;
    }

    IEnumerator SpawnMeteors(int count, Vector2 ascendTarget)
    {
        List<GameObject> warningLasers = new List<GameObject>();
        List<GameObject> meteors = new List<GameObject>();

        try
        {
            for (int i = 0; i < count; i++)
            {
                // 각 메테오 생성 시마다 현재 카메라 위치를 가져옴
                Camera mainCamera = Camera.main;
                if (mainCamera == null) yield break;

                float cameraHeight = 2f * mainCamera.orthographicSize;
                float cameraWidth = cameraHeight * mainCamera.aspect;

                Vector3 cameraPos = mainCamera.transform.position;
                float leftBound = cameraPos.x - cameraWidth / 2f;
                float rightBound = cameraPos.x + cameraWidth / 2f;
                float bottomBound = cameraPos.y - cameraHeight / 2f;
                float topBound = cameraPos.y + cameraHeight / 2f;

                // 랜덤 위치 생성 (화면 내 범위)
                float randomX = Random.Range(leftBound + 1f, rightBound - 1f);

                // 화면 상단보다 조금 위에서 생성 (화면 밖)
                float spawnY = topBound + 3f;
                Vector2 spawnPos = new Vector2(randomX, spawnY);

                // 바닥 위치 (화면 하단)
                Vector2 targetPos = new Vector2(randomX, bottomBound + 0.5f);

                // 경고 레이저 생성 (현재 카메라 기준)
                GameObject warningLaser = CreateWarningLaser(targetPos, mainCamera);
                if (warningLaser != null)
                {
                    warningLasers.Add(warningLaser);
                    RegisterEffect(warningLaser);
                }

                // 메테오 생성
                Coroutine meteorCoroutine = StartCoroutine(SpawnMeteorWithDelay(spawnPos, targetPos, warningLaser, meteors));
                activeCoroutines.Add(meteorCoroutine);

                yield return new WaitForSeconds(0.2f);
            }

            // 모든 메테오 생성 완료 대기
            yield return new WaitForSeconds(count * 0.2f + meteorWarningTime + 0.5f);
        }
        finally
        {
            // 리소스 정리
            foreach (var laser in warningLasers)
            {
                if (laser != null)
                {
                    UnregisterEffect(laser);
                    Destroy(laser);
                }
            }

            foreach (var meteor in meteors)
            {
                if (meteor != null)
                    Destroy(meteor);
            }
        }
    }

    IEnumerator SpawnMeteorWithDelay(Vector2 spawnPos, Vector2 targetPos, GameObject warningLaser, List<GameObject> meteorList)
    {
        // 경고 레이저 표시 시간
        yield return new WaitForSeconds(meteorWarningTime);

        // 경고 레이저 제거
        if (warningLaser != null)
        {
            UnregisterEffect(warningLaser);
            Destroy(warningLaser);
        }

        // 메테오 생성 및 낙하
        GameObject meteor = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);
        meteorList.Add(meteor);

        // 메테오 크기 랜덤 설정
        float meteorScale = Random.Range(0.8f, 1.5f);
        meteor.transform.localScale = Vector3.one * meteorScale;

        // Rigidbody2D가 없으면 추가
        Rigidbody2D meteorRb = meteor.GetComponent<Rigidbody2D>();
        if (meteorRb == null)
        {
            meteorRb = meteor.AddComponent<Rigidbody2D>();
            meteorRb.gravityScale = 0f;
        }

        float fallTime = 0.5f;
        float elapsed = 0f;

        while (elapsed < fallTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallTime;

            if (meteor != null)
            {
                // 직선으로 낙하
                meteor.transform.position = Vector2.Lerp(spawnPos, targetPos, t);
            }
            yield return null;
        }

        // 충돌 처리
        if (meteor != null)
        {
            // 메테오 폭발 이펙트
            GameObject explosion = CreateExplosionEffect(targetPos, meteorScale);
            if (explosion != null)
            {
                RegisterEffect(explosion);
                Destroy(explosion, 1f);
            }
            
            Destroy(meteor);
            meteorList.Remove(meteor);
        }
    }

    GameObject CreateWarningLaser(Vector2 targetPos, Camera mainCamera = null)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return null;

        // 빨간색 경고 레이저 생성
        GameObject warningLaser = new GameObject("WarningLaser");

        // LineRenderer 추가
        LineRenderer lineRenderer = warningLaser.AddComponent<LineRenderer>();

        // LineRenderer 설정
        lineRenderer.startWidth = 0.3f;
        lineRenderer.endWidth = 0.3f;
        lineRenderer.positionCount = 2;

        // LineRenderer 머티리얼 설정 (캐시 사용)
        Material material = GetOrCreateMaterial("Sprites/Default", Color.red);
        lineRenderer.material = material;

        // 빨간색 그래디언트
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
            new GradientColorKey(Color.red, 0.0f),
            new GradientColorKey(Color.red, 1.0f)
            },
            new GradientAlphaKey[] {
            new GradientAlphaKey(0.8f, 0.0f),
            new GradientAlphaKey(0.8f, 1.0f)
            }
        );
        lineRenderer.colorGradient = gradient;

        // 라인 위치 설정 (위에서 아래로)
        float topY = mainCamera.transform.position.y + mainCamera.orthographicSize;

        lineRenderer.SetPosition(0, new Vector3(targetPos.x, topY, 0));
        lineRenderer.SetPosition(1, new Vector3(targetPos.x, targetPos.y, 0));

        // 점멸 효과 코루틴 시작
        var effect = warningLaser.AddComponent<WarningLaserEffect>();
        effect.lineRenderer = lineRenderer;

        return warningLaser;
    }

    GameObject CreateExplosionEffect(Vector2 position, float scale)
    {
        // 간단한 폭발 이펙트 (원형 스프라이트)
        GameObject explosion = new GameObject("Explosion");
        SpriteRenderer explosionSr = explosion.AddComponent<SpriteRenderer>();

        // 텍스처 풀에서 가져오기
        Texture2D texture = GetTextureFromPool(64, 64);
        if (texture != null)
        {
            // 기본 원형 스프라이트 생성
            Vector2 explosionPos = new Vector2(position.x, position.y + 2);
            Sprite sprite = CreateCircleSprite(texture, 32, Color.yellow);
            explosionSr.sprite = sprite;
            explosion.transform.position = explosionPos;
            explosion.transform.localScale = Vector3.one * scale * 5f;

            // 폭발 애니메이션
            Coroutine explosionRoutine = StartCoroutine(ExplosionAnimation(explosion));
            activeCoroutines.Add(explosionRoutine);
        }

        return explosion;
    }

    Sprite CreateCircleSprite(Texture2D texture, int segments, Color color)
    {
        if (texture == null)
            return null;

        // 기존 텍스처 재활용
        Color[] clearColors = new Color[texture.width * texture.height];
        for (int i = 0; i < clearColors.Length; i++)
            clearColors[i] = Color.clear;
        texture.SetPixels(clearColors);

        // 원 그리기
        Vector2 center = new Vector2(texture.width / 2, texture.height / 2);
        float radius = texture.width / 2 - 2;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * 2 * Mathf.PI / segments;
            int x = Mathf.RoundToInt(center.x + Mathf.Cos(angle) * radius);
            int y = Mathf.RoundToInt(center.y + Mathf.Sin(angle) * radius);

            if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
            {
                // 외곽선 두껍게
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int px = x + dx;
                        int py = y + dy;
                        if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                        {
                            texture.SetPixel(px, py, color);
                        }
                    }
                }
            }
        }

        texture.Apply();

        Sprite sprite = Sprite.Create(texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));

        return sprite;
    }

    IEnumerator ExplosionAnimation(GameObject explosion)
    {
        SpriteRenderer sr = explosion.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 크기 확대
            float scale = Mathf.Lerp(1f, 3f, t);
            explosion.transform.localScale = Vector3.one * scale * 5;

            // 서서히 사라짐
            Color color = sr.color;
            color.a = Mathf.Lerp(1f, 0f, t);
            sr.color = color;

            yield return null;
        }

        Destroy(explosion);
    }

    // 경고 레이저 점멸 효과 컴포넌트
    public class WarningLaserEffect : MonoBehaviour
    {
        public LineRenderer lineRenderer;
        private float timer = 0f;
        private bool isVisible = true;

        void Start()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();
        }

        void Update()
        {
            if (lineRenderer == null) return;

            timer += Time.deltaTime;

            // 0.1초 간격으로 점멸
            if (timer >= 0.1f)
            {
                timer = 0f;
                isVisible = !isVisible;

                // 라인 알파값 조절
                Gradient gradient = lineRenderer.colorGradient;
                GradientAlphaKey[] alphaKeys = gradient.alphaKeys;
                alphaKeys[0].alpha = isVisible ? 0.8f : 0.3f;
                alphaKeys[1].alpha = isVisible ? 0.8f : 0.3f;

                gradient.SetKeys(gradient.colorKeys, alphaKeys);
                lineRenderer.colorGradient = gradient;
            }
        }

        void OnDestroy()
        {
            // Material 정리
            if (lineRenderer != null && lineRenderer.material != null)
            {
                Destroy(lineRenderer.material);
            }
        }
    }

    // ----------------------------------------------------------
    //              분노 모드 패턴 (강화판)
    // ----------------------------------------------------------

    IEnumerator Pattern_GroundSlam_Rage()
    {
        Debug.Log("분노 패턴: 강화 땅울림 포효");

        // 3연속 땅울림
        for (int i = 0; i < 3; i++)
        {
            yield return StartCoroutine(Pattern_GroundSlam());
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator Pattern_FeatherStorm_Rage()
    {
        isInPattern = true;
        rb.velocity = Vector2.zero;

        Debug.Log("분노 패턴: 강화 깃털 폭풍");

        // 더 빠른 이동
        //anim.SetTrigger("dash");
        Vector2 targetPos = featherStormPosition;
        float moveTime = 0.4f;
        float elapsedTime = 0f;

        while (elapsedTime < moveTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveTime;
            transform.position = Vector2.Lerp(transform.position, targetPos, t);
            yield return null;
        }

        // 더 많은 깃털, 더 빠른 발사
        //anim.SetTrigger("attack");
        yield return new WaitForSeconds(0.3f);

        List<GameObject> rageFeathers = new List<GameObject>();
        int rageFeatherCount = featherCount * 2;
        
        for (int i = 0; i < rageFeatherCount; i++)
        {
            Vector2 playerDir = (player.position - transform.position).normalized;
            float randomAngle = Random.Range(-45f, 15f);
            Vector2 shootDir = Quaternion.Euler(0, 0, randomAngle) * playerDir;

            GameObject feather = Instantiate(featherPrefab, transform.position, Quaternion.identity);
            rageFeathers.Add(feather);
            
            Rigidbody2D featherRb = feather.GetComponent<Rigidbody2D>();

            if (featherRb != null)
            {
                featherRb.velocity = shootDir * (featherSpeed * 1.5f);
                float rotationAngle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
                feather.transform.rotation = Quaternion.Euler(0, 0, rotationAngle);
            }

            yield return new WaitForSeconds(0.1f);
        }

        // 일정 시간 후 깃털 정리
        yield return new WaitForSeconds(3f);
        
        foreach (var feather in rageFeathers)
        {
            if (feather != null)
                Destroy(feather);
        }

        yield return new WaitForSeconds(0.3f);
        isInPattern = false;
    }

    IEnumerator Pattern_MeteorShower_Rage()
    {
        isInPattern = true;

        // 기존 중력 스케일 저장
        float originalGravityScale = rb.gravityScale;

        // 중력 비활성화
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;

        Debug.Log("분노 패턴: 강화 메테오 샤워");

        // 더 빠른 상승
        //anim.SetTrigger("ascend");
        Vector2 startPos = transform.position;
        Vector2 ascendTarget = startPos + Vector2.up * (meteorShowerHeight * 1.2f);

        float ascendTime = 0.6f;
        float elapsedTime = 0f;

        if (bossCollider != null) bossCollider.enabled = false;

        while (elapsedTime < ascendTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / ascendTime;
            transform.position = Vector2.Lerp(startPos, ascendTarget, t);
            yield return null;
        }

        // 더 많은 메테오, 더 빠른 낙하
        Coroutine meteorRoutine = StartCoroutine(SpawnMeteors(rageMeteorCount, ascendTarget));
        activeCoroutines.Add(meteorRoutine);
        yield return meteorRoutine;

        // 더 빠른 하강
        yield return new WaitForSeconds(1.5f);
        //anim.SetTrigger("descend");

        elapsedTime = 0f;
        while (elapsedTime < ascendTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / ascendTime;
            transform.position = Vector2.Lerp(ascendTarget, startPos, t);
            yield return null;
        }

        if (bossCollider != null) bossCollider.enabled = true;

        // 중력 스케일 복원
        rb.gravityScale = originalGravityScale;

        yield return new WaitForSeconds(0.3f);
        isInPattern = false;
    }

    // ----------------------------------------------------------
    //                 보스 데미지 처리
    // ----------------------------------------------------------
    public void ApplyDamage(float damage)
    {
        currentHP -= damage;

        // 피격 효과
        Coroutine flashRoutine = StartCoroutine(FlashRed());
        activeCoroutines.Add(flashRoutine);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    IEnumerator FlashRed()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white;
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

        //anim.SetTrigger("die");
        Debug.Log("스테이지 2 보스 사망!");

        // 생성된 모든 이펙트 정리
        CleanupAllEffects();

        // Material 캐시 정리
        ClearMaterialCache();

        // Texture 풀 정리
        ClearTexturePool();

        // 보상 드롭 또는 스테이지 클리어 처리
        Destroy(gameObject, 3f);
    }

    // ----------------------------------------------------------
    //                 메모리 관리 메서드
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
        foreach (var texture in texturePool)
        {
            if (texture != null && texture.width == width && texture.height == height)
            {
                texturePool.Enqueue(texture);
                return texture;
            }
        }
        
        Texture2D newTexture = new Texture2D(width, height);
        texturePool.Enqueue(newTexture);
        return newTexture;
    }

    private void ReturnTextureToPool(Texture2D texture)
    {
        if (texture != null)
            texturePool.Enqueue(texture);
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
    //                 시각화 (에디터 디버그)
    // ----------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // 깃털 폭풍 위치 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(featherStormPosition, Vector3.one * 2f);
    }
}