using UnityEngine;

public class FeatherProjectile : MonoBehaviour
{
    public float speed = 10f;

    private bool hasHit = false;
    private Vector2 direction = Vector2.left; // 항상 왼쪽으로 고정

    void Start()
    {
        // Rigidbody2D 설정
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Kinematic으로 설정하여 다른 물리 객체에 영향을 받지 않음
            rb.isKinematic = true;
        }

        // 방향에 맞게 초기 회전 설정
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Update()
    {
        if (hasHit) return;

        // 항상 왼쪽으로 이동
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // 충돌 시
        if (other.CompareTag("Player") || other.CompareTag("Ground"))
        {
            DestroyFeather();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;

        // 물리 충돌이 발생하면 파괴
        if (!collision.collider.CompareTag("Player") &&
            !collision.collider.CompareTag("Boss") &&
            !collision.collider.CompareTag("Ground"))
        {
            DestroyFeather();
        }
    }

    void DestroyFeather()
    {
        hasHit = true;

        // 충돌 이펙트 생성 (선택사항)
        CreateHitEffect();

        Destroy(gameObject);
    }

    void CreateHitEffect()
    {
        // 깃털 충돌 이펙트 생성
        GameObject effect = new GameObject("FeatherHitEffect");
        effect.transform.position = transform.position;

        SpriteRenderer sr = effect.AddComponent<SpriteRenderer>();
        // 깃털 충돌 스프라이트 또는 파티클 생성 가능

        Destroy(effect, 0.5f);
    }

    // 외부에서 속도 설정 가능 (옵션)
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }
}