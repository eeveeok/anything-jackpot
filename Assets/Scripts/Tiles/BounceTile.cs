using UnityEngine;

public enum BounceDirection
{
    Right,  // 오른쪽으로 튕기기
    Left    // 왼쪽으로 튕기기
}

[RequireComponent(typeof(Collider2D))]
public class BounceTile : MonoBehaviour
{
    [Header("바운드 설정")]
    [SerializeField] private BounceDirection bounceDirection = BounceDirection.Right;
    [SerializeField] private float bounceForce = 15f; // 튕기는 힘
    [SerializeField] private float upwardForce = 5f;  // 위쪽으로 추가되는 힘
    [SerializeField] private float cooldownTime = 0.5f; // 튕김 후 쿨다운

    private bool canBounce = true;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // 방향에 따라 색상 표시
        UpdateVisual();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 플레이어와 충돌했는지 확인
        if (collision.gameObject.CompareTag("Player") && canBounce)
        {
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                collision.gameObject.GetComponent<LaserShooter>().isBounced = true;
                BouncePlayer(playerRb);
                StartCoroutine(Cooldown());
            }
        }
    }

    void BouncePlayer(Rigidbody2D playerRb)
    {
        // 플레이어의 현재 수직 속도를 초기화
        playerRb.velocity = new Vector2(playerRb.velocity.x, 0f);

        // 방향 벡터 계산
        Vector2 bounceVector = GetBounceDirection();

        // 힘 적용
        bounceVector.y += upwardForce / bounceForce;
        bounceVector = bounceVector.normalized;

        playerRb.AddForce(bounceVector * bounceForce, ForceMode2D.Impulse);
    }

    Vector2 GetBounceDirection()
    {
        switch (bounceDirection)
        {
            case BounceDirection.Right:
                return Vector2.right;
            case BounceDirection.Left:
                return Vector2.left;
            default:
                return Vector2.right;
        }
    }

    System.Collections.IEnumerator Cooldown()
    {
        canBounce = false;

        // 쿨다운 시각적 피드백
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.gray;
        }

        yield return new WaitForSeconds(cooldownTime);

        canBounce = true;

        // 원래 색상 복구
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    void UpdateVisual()
    {
        // 에디터에서 시각적으로 방향 표시
        if (spriteRenderer != null)
        {
            switch (bounceDirection)
            {
                case BounceDirection.Right:
                    spriteRenderer.color = new Color(0.5f, 0.8f, 1f, 1f); // 파란색 계열
                    break;
                case BounceDirection.Left:
                    spriteRenderer.color = new Color(1f, 0.7f, 0.5f, 1f); // 주황색 계열
                    break;
            }
        }
    }

    // 에디터에서 방향 변경 시 자동 업데이트
    void OnValidate()
    {
        UpdateVisual();
    }

    void OnDrawGizmosSelected()
    {
        // 방향 표시 기즈모
        Gizmos.color = bounceDirection == BounceDirection.Right ? Color.blue : Color.red;

        Vector2 direction = GetBounceDirection();
        Vector3 endPoint = transform.position + (Vector3)direction * 1.5f;

        Gizmos.DrawLine(transform.position, endPoint);
        Gizmos.DrawWireSphere(endPoint, 0.2f);

        // 위쪽 요소 표시
        Gizmos.color = Color.green;
        Vector3 upwardEnd = transform.position + Vector3.up * (upwardForce / bounceForce);
        Gizmos.DrawLine(transform.position, upwardEnd);
    }
}