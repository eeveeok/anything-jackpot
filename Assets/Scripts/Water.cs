using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Water : MonoBehaviour
{
    [Header("물리 설정")]
    public float gravityMultiplier = 0.4f;
    public float waterDrag = 3.5f;

    // 플레이어의 원래 중력을 저장할 변수
    private float savedGravity;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 1. 들어오는 순간 원래 중력값 저장
                savedGravity = rb.gravityScale;

                // 2. 중력을 낮춤 (부력 효과)
                rb.gravityScale = savedGravity * gravityMultiplier;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 3. 물 저항 구현 (속도 감쇄)
                float dampingFactor = 1f - (waterDrag * Time.fixedDeltaTime);

                // 음수가 되지 않도록 방어
                if (dampingFactor < 0) dampingFactor = 0;

                rb.velocity *= dampingFactor;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 4. 나갈 때 원래 중력으로 복구
                rb.gravityScale = savedGravity;
            }
        }
    }
}
