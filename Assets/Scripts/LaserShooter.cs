using System.Collections;
using UnityEngine;

public class LaserShooter : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("레이저 설정")]
    public GameObject laserBeamPrefab;
    public float laserSpawnDistance = 0.7f;

    [Header("반동 세부 설정")]
    public float initialRecoilForce = 12f;
    public float continuousRecoilForce = 1000f;
    public float maxRecoilVelocity = 2000f;

    [Header("반동 세부 설정")]
    public float verticalRecoilMultiplier = 0.5f;
    public float recoilSmoothing = 0.1f;

    [Header("체크포인트")]
    public Transform checkpoint;   // 인스펙터에서 연결
    public float respawnDelay = 2.0f;
    public bool isDead = false;

    [Header("무적 상태")]
    public float invincibilityDuration = 2f;  // 무적 지속 시간
    public float blinkInterval = 0.1f;       // 깜빡임 간격

    [Header("물리 설정")]
    public float normalDrag = 0.5f;
    public float bounceDrag = 3f; // 바운스 중 높은 드래그로 빠른 감속

    [HideInInspector]
    public bool isBounced = false;       // 튕기는지 여부

    public PhysicsMaterial2D airMaterial;
    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private bool isFacingRight = true;
    private bool isLaserActive = false;
    private bool isInvincible = false;       // 무적 상태 여부
    private GameObject currentLaser;
    private Camera mainCamera;

    // Ground Check
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask whatIsGround;
    private Vector3 lastGroundedPosition; // 마지막으로 땅에 닿은 위치 (체크포인트 위함)
    private bool wasGroundedLastFrame;    // 이전 프레임의 접지 상태

    // 애니메이션
    private Animator animator;
    private readonly int isLaserHash = Animator.StringToHash("IsLaser");

    // 반동 관련
    private Vector2 recoilVelocity;

    // 렌더러 참조
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null && airMaterial != null)
        {
            col.sharedMaterial = airMaterial;
        }

        // 초기 위치 저장
        lastGroundedPosition = transform.position;
        wasGroundedLastFrame = true;
    }

    void Update()
    {
        /////
        //Debug.Log(isBounced);

        HandleInput();
        UpdateFacingDirection();
        UpdateAnimation();
    }

    void FixedUpdate()
    {
        Move();

        // 고정 프레임에서 접지 체크
        CheckGround();

        // 레이저가 활성화되어 있으면 계속 반동 적용
        if (isLaserActive)
        {
            ApplyContinuousRecoil();
        }
    }

    void HandleInput()
    {
        if (isDead)
        {
            horizontalInput = 0f;
            return;
        }

        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.sharedMaterial = airMaterial;
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        // 레이저 발사/중지 입력
        if ((Input.GetMouseButtonDown(0)) && !isLaserActive)
        {
            StartLaser();
            animator.SetBool("Fire", true);
        }
        if ((Input.GetKeyUp(KeyCode.X) || Input.GetMouseButtonUp(0)) && isLaserActive)
        {
            StopLaser();
            animator.SetBool("Fire", false);
        }
    }

    void Move()
    {
        float currentMoveSpeed = isLaserActive ? moveSpeed * 0.6f : moveSpeed;

        Vector2 newVelocity = new Vector2(horizontalInput * currentMoveSpeed, rb.velocity.y);
        if (!isBounced)
        {
            rb.velocity = newVelocity;
        }

        animator.SetFloat("Speed", newVelocity.magnitude);
    }

    void CheckGround()
    {
        bool previouslyGrounded = isGrounded;

        // OverlapCircle으로 접지 상태 체크 (더 정확함)
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);

        // Raycast를 추가로 사용하여 더 정확한 체크 (선택사항)
        // isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckRadius, whatIsGround);

        animator.SetBool("Jump", !isGrounded);

        if (isGrounded)
        {
            rb.sharedMaterial = null;

            // 땅에 닿은 순간 위치 저장
            if (!wasGroundedLastFrame)
            {
                // 공중에서 땅에 닿은 순간
                lastGroundedPosition = transform.position;
            }
            else
            {
                // 계속 땅에 있는 동안에도 주기적으로 위치 업데이트 (안정적인 위치만)
                // 특정 조건(예: 이동 속도가 느릴 때)에서만 업데이트
                if (Mathf.Abs(rb.velocity.x) < 0.1f)
                {
                    lastGroundedPosition = transform.position;
                }
            }
        }
        else
        {
            // 공중에 있는 동안 마지막 접지 위치 유지
            // 떨어지기 시작할 때의 위치를 저장
            if (wasGroundedLastFrame && !isGrounded)
            {
                // 땅에서 떨어지는 순간의 위치 저장 (안전한 위치)
                lastGroundedPosition = transform.position;
            }
        }

        // 현재 프레임의 접지 상태 저장
        wasGroundedLastFrame = isGrounded;
    }

    void UpdateFacingDirection()
    {
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        if (mousePosition.x > transform.position.x && !isFacingRight)
        {
            Flip();
        }
        else if (mousePosition.x < transform.position.x && isFacingRight)
        {
            Flip();
        }
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

        transform.localPosition = new Vector2(transform.localPosition.x, transform.localPosition.y);
    }

    void UpdateAnimation()
    {
        if (animator != null)
        {
            animator.SetBool(isLaserHash, isLaserActive);
        }
    }

    void StartLaser()
    {
        if (laserBeamPrefab != null)
        {
            isLaserActive = true;

            // 마우스 위치를 기반으로 방향 계산
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = new Vector2(
                mousePosition.x - transform.position.x,
                mousePosition.y - transform.position.y
            ).normalized;

            // 캐릭터 중심에서 spawnDistance만큼 떨어진 위치 계산
            Vector2 spawnPos = (Vector2)transform.position + direction * laserSpawnDistance;

            // 레이저 생성
            currentLaser = Instantiate(laserBeamPrefab, spawnPos, Quaternion.identity);

            LaserBeam laserScript = currentLaser.GetComponent<LaserBeam>();
            if (laserScript != null)
            {
                laserScript.direction = direction;
                laserScript.characterCenter = transform;
                laserScript.mainCamera = mainCamera;
                laserScript.spawnDistance = laserSpawnDistance;
            }

            // 초기 반동 적용
            ApplyInitialRecoil(direction);
        }
    }

    void StopLaser()
    {
        isLaserActive = false;

        if (currentLaser != null)
        {
            LaserBeam laserScript = currentLaser.GetComponent<LaserBeam>();
            if (laserScript != null)
            {
                laserScript.SetActive(false);
            }

            Destroy(currentLaser, 0.1f);
            currentLaser = null;
        }
    }

    void ApplyInitialRecoil(Vector2 laserDirection)
    {
        // 바운드 중일 때는 반동 힘 감소
        float currentRecoilForce = initialRecoilForce;
        float currentVerticalMultiplier = verticalRecoilMultiplier;

        if (isBounced)
        {
            currentRecoilForce *= 0.3f;
            currentVerticalMultiplier *= 0.3f;
        }

        // 초기 반동은 레이저 발사 방향의 반대
        Vector2 recoilDirection = -laserDirection.normalized;
        Vector2 recoil = recoilDirection * currentRecoilForce;
        recoil.y *= currentVerticalMultiplier;

        rb.AddForce(recoil, ForceMode2D.Impulse);

        // 최대 속도 제한
        if (rb.velocity.magnitude > maxRecoilVelocity)
        {
            rb.velocity = rb.velocity.normalized * maxRecoilVelocity;
        }
    }

    void ApplyContinuousRecoil()
    {
        // 바운드 중일 때는 반동 힘 감소
        float currentRecoilForce = continuousRecoilForce;
        float currentVerticalMultiplier = verticalRecoilMultiplier * 0.3f;

        if (isBounced)
        {
            currentRecoilForce *= 0.3f;
            currentVerticalMultiplier *= 0.3f;
        }

        // 실시간 마우스 위치로 반동 방향 계산
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 currentDirection = new Vector2(
            mousePosition.x - transform.position.x,
            mousePosition.y - transform.position.y
        ).normalized;

        // 반동 방향은 현재 레이저 방향의 반대
        Vector2 recoilDirection = -currentDirection.normalized;

        // 지속적인 반동 적용
        Vector2 targetRecoil = recoilDirection * currentRecoilForce;
        targetRecoil.y *= currentVerticalMultiplier;

        // 부드러운 반동 적용
        Vector2 smoothRecoil = Vector2.SmoothDamp(
            Vector2.zero,
            targetRecoil,
            ref recoilVelocity,
            recoilSmoothing
        );

        rb.AddForce(smoothRecoil, ForceMode2D.Force);

        // 최대 속도 제한
        if (rb.velocity.magnitude > maxRecoilVelocity)
        {
            rb.velocity = rb.velocity.normalized * maxRecoilVelocity;
        }
    }

    // --------------------------------------------------
    // 죽음 & 부활 처리
    // --------------------------------------------------
    public void PlayerDie()
    {
        if (isDead || isInvincible) return;
        isDead = true;

        // 이동, 레이저 중지
        rb.velocity = Vector2.zero;
        StopLaser();

        // 보이지 않게
        SetPlayerVisible(false);

        // 2초 후 부활
        StartCoroutine(RespawnDelay());
    }

    private IEnumerator RespawnDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    private void Respawn()
    {
        // 체크포인트 존재하면 체크포인트, 없으면 마지막 착지 위치
        Vector3 respawnPos = checkpoint != null ? checkpoint.position : lastGroundedPosition;

        // 높이를 약간 보정 (땅 위에 서있도록)
        respawnPos.y += 2.5f;

        transform.position = respawnPos;

        // 속도 초기화
        rb.velocity = Vector2.zero;

        // 다시 보이게
        SetPlayerVisible(true);

        // 상태 초기화
        isDead = false;
        isGrounded = false;
        wasGroundedLastFrame = false;

        // 무적 상태 시작
        StartCoroutine(InvincibilityRoutine());
    }

    // 무적 상태 코루틴
    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        // 깜빡임 효과 시작
        StartCoroutine(BlinkEffect());

        // 무적 지속 시간 대기
        yield return new WaitForSeconds(invincibilityDuration);

        // 무적 상태 종료
        isInvincible = false;

        // 깜빡임 중지 (SpriteRenderer를 항상 보이도록)
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    // 깜빡임 효과 코루틴
    private IEnumerator BlinkEffect()
    {
        float endTime = Time.time + invincibilityDuration;

        while (Time.time < endTime && isInvincible)
        {
            if (spriteRenderer != null)
            {
                // SpriteRenderer를 토글하여 깜빡임 효과
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }

            // 깜빡임 간격만큼 대기
            yield return new WaitForSeconds(blinkInterval);
        }

        // 무적 상태가 끝나면 항상 보이도록 설정
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    private void SetPlayerVisible(bool visible)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = visible;

        if (animator != null)
            animator.enabled = visible;

        rb.isKinematic = !visible;
    }

    // --------------------------------------------------
    // 충돌 처리 (에너지 웨이브, 보스 공격 등)
    // --------------------------------------------------
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead || isInvincible) return;

        if (collision.CompareTag("BossAttack") || collision.CompareTag("Spike"))
        {
            PlayerDie();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isBounced && (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Spike")))
        {
            isBounced = false;
        }
    }

    // 에디터에서 groundCheck 범위 시각화
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    // 공개 프로퍼티
    public bool IsGrounded => isGrounded;
    public bool IsLaserActive => isLaserActive;
    public bool IsInvincible => isInvincible; // 무적 상태 확인용
}