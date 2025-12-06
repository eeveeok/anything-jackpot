using System.Collections;
using UnityEngine;

public class CannonShooter : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("대포 발사 설정")]
    public GameObject cannonEffectPrefab;
    public float recoilForce = 30f;
    public float recoilDuration = 0.3f;
    public float maxRecoilVelocity = 30f;
    public float shootCooldown = 0.5f;

    [Header("발사 세부 설정")]
    public float verticalRecoilMultiplier = 0.3f;
    public float effectSpawnDistance = 0.5f;
    public float effectDuration = 0.5f;

    [Header("체크포인트")]
    public Transform checkpoint;
    public float respawnDelay = 2.0f;
    public bool isDead = false;

    [Header("무적 상태")]
    public float invincibilityDuration = 2f;
    public float blinkInterval = 0.1f;

    private bool isInvincible = false;
    private bool canShoot = true;
    private float lastShootTime = 0f;

    public PhysicsMaterial2D airMaterial;
    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private bool isFacingRight = true;
    private Camera mainCamera;

    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask whatIsGround;
    private Vector3 lastGroundedPosition;
    private bool wasGroundedLastFrame;

    private Animator animator;
    private readonly int isShootingHash = Animator.StringToHash("IsShooting");

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null && airMaterial != null)
            col.sharedMaterial = airMaterial;

        lastGroundedPosition = transform.position;
        wasGroundedLastFrame = true;
    }

    void Update()
    {
        if (isDead) return;

        HandleInput();
        UpdateFacingDirection();
        UpdateAnimation();
    }

    void FixedUpdate()
    {
        Move();
        CheckGround();
    }

    void HandleInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.sharedMaterial = airMaterial;
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        if (Input.GetMouseButtonDown(0) && canShoot && isGrounded)
        {
            ShootCannon();
        }
    }

    // 반동 중에는 rb.velocity.x 덮어쓰지 않도록 설정
    void Move()
    {
        float currentMoveSpeed = moveSpeed;

        bool recoilActive = Time.time - lastShootTime < recoilDuration * 0.5f;

        if (!recoilActive && isGrounded)
        {
            rb.velocity = new Vector2(horizontalInput * currentMoveSpeed, rb.velocity.y);
        }

        if (horizontalInput != 0)
        {
            rb.velocity = new Vector2(horizontalInput * currentMoveSpeed, rb.velocity.y);
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        }
    }

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);

        if (animator != null)
            animator.SetBool("Jump", !isGrounded);
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
            bool isShooting = Time.time - lastShootTime < recoilDuration * 0.5f;
            animator.SetBool(isShootingHash, isShooting);
        }
    }

    void ShootCannon()
    {
        // 마우스 z 고정
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f;

        Vector2 direction = (mousePosition - transform.position).normalized;

        // 발사 효과
        if (cannonEffectPrefab != null)
        {
            Vector2 effectPos = (Vector2)transform.position + direction * effectSpawnDistance;
            GameObject effect = Instantiate(cannonEffectPrefab, effectPos, Quaternion.identity);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            effect.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            Destroy(effect, effectDuration);
        }

        ApplyCannonRecoil(direction);

        lastShootTime = Time.time;
        StartCoroutine(ShootCooldownRoutine());
    }

    // direction 받아서 recoilDirection 계산
    void ApplyCannonRecoil(Vector2 direction)
    {
        Vector2 recoilDirection = -direction;
        Vector2 recoil = recoilDirection * recoilForce;

        recoil.y *= verticalRecoilMultiplier;

        // -----------------------------
        // 아래로 -15° ~ +15° 범위에서는 반동 더 강하게
        // -----------------------------
        float angle = Vector2.SignedAngle(Vector2.down, direction);
        // Vector2.down 기준: 0°, 좌우로 기울면 ±값

        if (Mathf.Abs(angle) <= 15f)
        {
            recoil *= 1.3f;    // ⭐ 반동 강화
        }
        // -----------------------------

        rb.AddForce(recoil, ForceMode2D.Impulse);

        if (rb.velocity.magnitude > maxRecoilVelocity)
            rb.velocity = rb.velocity.normalized * maxRecoilVelocity;

        StartCoroutine(ContinuousRecoilRoutine(recoilDirection));
    }

    IEnumerator ContinuousRecoilRoutine(Vector2 recoilDirection)
    {
        float elapsed = 0f;
        float half = recoilDuration * 0.5f;

        while (elapsed < half)
        {
            float t = elapsed / half;

            float curve = 1f - Mathf.Pow(1f - t, 3f);

            Vector2 add = recoilDirection * (recoilForce * 0.5f * curve * Time.fixedDeltaTime);

            add.y = 0f; // y축 반동 제거

            rb.AddForce(add, ForceMode2D.Force);

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        rb.velocity = new Vector2(rb.velocity.x, Mathf.Min(rb.velocity.y, 1f));
    }

    IEnumerator ShootCooldownRoutine()
    {
        canShoot = false;
        yield return new WaitForSeconds(shootCooldown);
        canShoot = true;
    }

    // --------------------------------------------------
    // 죽음 & 부활 처리 (LaserShooter와 동일)
    // --------------------------------------------------
    public void PlayerDie()
    {
        if (isDead || isInvincible) return;
        isDead = true;

        rb.velocity = Vector2.zero;
        SetPlayerVisible(false);

        StartCoroutine(RespawnDelay());
    }

    private IEnumerator RespawnDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    private void Respawn()
    {
        Vector3 respawnPos = checkpoint != null ? checkpoint.position : lastGroundedPosition;
        respawnPos.y += 2.5f;

        transform.position = respawnPos;
        rb.velocity = Vector2.zero;
        SetPlayerVisible(true);

        isDead = false;
        isGrounded = false;
        wasGroundedLastFrame = false;

        StartCoroutine(InvincibilityRoutine());
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        StartCoroutine(BlinkEffect());
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    private IEnumerator BlinkEffect()
    {
        float endTime = Time.time + invincibilityDuration;

        while (Time.time < endTime && isInvincible)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }
            yield return new WaitForSeconds(blinkInterval);
        }

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
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead || isInvincible) return;

        if (collision.CompareTag("BossAttack") || collision.CompareTag("Spike"))
        {
            PlayerDie();
        }
    }

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
    public bool CanShoot => canShoot;
    public bool IsInvincible => isInvincible;
    public bool IsDead => isDead;
}