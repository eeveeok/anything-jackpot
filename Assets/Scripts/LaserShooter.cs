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

    public PhysicsMaterial2D airMaterial;
    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private bool isFacingRight = true;
    private bool isLaserActive = false;
    private GameObject currentLaser;
    private Camera mainCamera;

    // Ground Check
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask whatIsGround;
    private Vector3 lastGroundedPosition; // 마지막으로 땅에 닿은 위치 (체크포인트 위함)

    // 애니메이션
    private Animator animator;
    private readonly int isLaserHash = Animator.StringToHash("IsLaser");

    // 반동 관련
    private Vector2 recoilVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        animator = GetComponent<Animator>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null && airMaterial != null)
        {
            col.sharedMaterial = airMaterial;
        }
    }

    void Update()
    {
        HandleInput();
        CheckGround();
        UpdateFacingDirection();
        UpdateAnimation();
    }

    void FixedUpdate()
    {
        Move();

        // 레이저가 활성화되어 있으면 계속 반동 적용
        if (isLaserActive)
        {
            ApplyContinuousRecoil();
        }
    }

    void HandleInput()
    {
        if (isDead) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.sharedMaterial = airMaterial;
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        // 레이저 발사/중지 입력
        if ((Input.GetKeyDown(KeyCode.X) || Input.GetMouseButtonDown(0)) && !isLaserActive)
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
        rb.velocity = newVelocity;

        animator.SetFloat("Speed", newVelocity.magnitude);
    }

    void CheckGround()
    {
        isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.1f, whatIsGround);

        animator.SetBool("Jump", !isGrounded);

        if (isGrounded)
        {
            rb.sharedMaterial = null;
            lastGroundedPosition = transform.position;  // 마지막 착지 위치 기록
        }
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
        // 초기 반동은 레이저 발사 방향의 반대
        Vector2 recoilDirection = -laserDirection.normalized;
        Vector2 recoil = recoilDirection * initialRecoilForce;
        recoil.y *= verticalRecoilMultiplier;

        rb.AddForce(recoil, ForceMode2D.Impulse);

        // 최대 속도 제한
        if (rb.velocity.magnitude > maxRecoilVelocity)
        {
            rb.velocity = rb.velocity.normalized * maxRecoilVelocity;
        }
    }

    void ApplyContinuousRecoil()
    {
        // 실시간 마우스 위치로 반동 방향 계산
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 currentDirection = new Vector2(
            mousePosition.x - transform.position.x,
            mousePosition.y - transform.position.y
        ).normalized;

        // 반동 방향은 현재 레이저 방향의 반대
        Vector2 recoilDirection = -currentDirection.normalized;

        // 지속적인 반동 적용
        Vector2 targetRecoil = recoilDirection * continuousRecoilForce;
        targetRecoil.y *= verticalRecoilMultiplier * 0.3f;

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

        // 디버그: 반동 힘 시각화
        Debug.DrawRay(transform.position, recoilDirection * 3f, Color.cyan);
        Debug.DrawRay(transform.position, currentDirection * 2f, Color.yellow); // 현재 레이저 방향
    }

    // --------------------------------------------------
    // 죽음 & 부활 처리
    // --------------------------------------------------
    public void PlayerDie()
    {
        if (isDead) return;
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
        yield return new WaitForSeconds(2f);
        Respawn();
    }

    private void Respawn()
    {
        // 체크포인트 존재하면 체크포인트, 없으면 마지막 착지 위치
        Vector3 respawnPos = checkpoint != null ? checkpoint.position : lastGroundedPosition;
        transform.position = respawnPos;

        // 다시 보이게
        SetPlayerVisible(true);

        // 상태 초기화
        isDead = false;
    }

    private void SetPlayerVisible(bool visible)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.enabled = visible;

        //Collider2D col = GetComponent<Collider2D>();
        //if (col != null)
        //    col.enabled = visible;

        if (animator != null)
            animator.enabled = visible;
    }

    // --------------------------------------------------
    // 충돌 처리 (에너지 웨이브, 보스 공격 등)
    // --------------------------------------------------
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        if (collision.CompareTag("BossAttack"))
        {
            PlayerDie();
        }
    }

    // 공개 프로퍼티
    public bool IsGrounded => isGrounded;
    public bool IsLaserActive => isLaserActive;
}