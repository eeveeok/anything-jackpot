// LaserProjectile.cs
using UnityEngine;

public class LaserProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float duration;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // 일정 시간 후 자동 파괴
        Destroy(gameObject, duration);

        // 이동 방향 설정
        if (rb != null)
        {
            rb.velocity = direction * speed;
        }
    }

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection;

        // 방향에 맞게 회전
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    public void SetDuration(float newDuration)
    {
        duration = newDuration;
    }

    void Update()
    {
        // Rigidbody2D를 사용하지 않는 경우 수동 이동
        if (rb == null)
        {
            transform.position += (Vector3)direction * speed * Time.deltaTime;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어와 충돌 체크
        if (collision.CompareTag("Player"))
        {
            LaserShooter playerScript = collision.GetComponent<LaserShooter>();
            if (playerScript != null && !playerScript.isDead)
            {
                playerScript.PlayerDie();
            }

            // 플레이어와 충돌 시 레이저 제거
            Destroy(gameObject);
        }

        // 땅과 충돌 시 레이저 제거
        if (collision.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // 파괴 효과
        CreateDestructionEffect();
    }

    void CreateDestructionEffect()
    {
        GameObject effect = new GameObject("LaserDestructionEffect");
        effect.transform.position = transform.position;

        SpriteRenderer effectRenderer = effect.AddComponent<SpriteRenderer>();
        effectRenderer.sprite = Resources.Load<Sprite>("CircleSprite"); // 또는 다른 스프라이트
        effectRenderer.color = spriteRenderer != null ? spriteRenderer.color : Color.white;

        Destroy(effect, 0.3f);
    }
}