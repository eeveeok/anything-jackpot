using UnityEngine;
using System.Collections;

public class LaserShooter : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 4f;
    public float jumpForce = 8f;

    [Header("레이저 설정")]
    public GameObject laserBeamPrefab;
    public float laserCooldown = 0.05f;
    public float laserSpawnDistance = 0.7f; // 캐릭터 중심에서 레이저 시작 거리

    [Header("반동 설정")]
    public float recoilForce = 5f;
    public float maxRecoilVelocity = 8f;

    private Rigidbody2D rb;
    private Animator animator;
    private float horizontalInput;
    private bool isGrounded;
    private bool isFacingRight = true;
    private bool isLaserActive = false;
    private float lastLaserTime = 0f;
    private GameObject currentLaser;
    private Camera mainCamera;

    // Ground Check
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask whatIsGround;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
    }

    void Update()
    {
        HandleInput();
        CheckGround();
        UpdateFacingDirection();
    }

    void FixedUpdate()
    {
        Move();
        HandleLaser();
    }

    void HandleInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);

            animator.SetBool("Jump",true);
        }

        if (Input.GetKeyDown(KeyCode.X) || Input.GetMouseButtonDown(0))
        {
            StartLaser();

            animator.SetBool("Shoot", true);
        }
        if (Input.GetKeyUp(KeyCode.X) || Input.GetMouseButtonUp(0))
        {
            StopLaser();

            animator.SetBool("Shoot", false);
        }
    }

    void Move()
    {
        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);

        animator.SetFloat("Speed", rb.velocity.magnitude);
    }

    void CheckGround()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, whatIsGround);
        isGrounded = colliders.Length > 0;

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
    }

    void StartLaser()
    {
        if (Time.time >= lastLaserTime + laserCooldown)
        {
            isLaserActive = true;
            CreateLaserBeam();
        }
    }

    void StopLaser()
    {
        isLaserActive = false;
        if (currentLaser != null)
        {
            Destroy(currentLaser);
            currentLaser = null;
        }
    }

    void HandleLaser()
    {
        if (isLaserActive && Time.time >= lastLaserTime + laserCooldown)
        {
            if (currentLaser != null)
            {
                Destroy(currentLaser);
            }

            CreateLaserBeam();
            ApplyRecoil();

            lastLaserTime = Time.time;
        }
    }

    void CreateLaserBeam()
    {
        if (laserBeamPrefab != null)
        {
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
                laserScript.characterCenter = transform; // 캐릭터 중심 전달
                laserScript.mainCamera = mainCamera;
                laserScript.spawnDistance = laserSpawnDistance;
            }
        }
    }

    void ApplyRecoil()
    {
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = new Vector2(
            mousePosition.x - transform.position.x,
            mousePosition.y - transform.position.y
        ).normalized;

        Vector2 recoilDirection = -direction;
        recoilDirection.y *= 0.7f;

        rb.velocity += recoilDirection * recoilForce;

        if (rb.velocity.magnitude > maxRecoilVelocity)
        {
            rb.velocity = rb.velocity.normalized * maxRecoilVelocity;
        }
    }
}