using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TractorBeam : MonoBehaviour
{
    [Header("X축 끌어당김 설정")]
    public bool pullRight = true;               // true면 오른쪽, false면 왼쪽으로 당김
    public float horizontalPullForce = 200f;    // X축 끌어당기는 힘
    public float maxHorizontalSpeed = 50f;      // X축 최대 속도

    [Header("Y축 정렬 설정")]
    public float verticalPullForce = 30f;       // Y축 스프링 강도
    public float verticalDamping = 4f;          // Y축 감쇠(제동)
    public float maxVerticalSpeed = 18f;        // Y축 최대 속도
    public float verticalDeadZone = 0.05f;      // 빔 중심 근처에서 힘을 주지 않는 구간

    private Transform tr;
    private float beamY;
    private BoxCollider2D col;

    // 플레이어 중력 저장용
    private float originalGravityScale = 1f;

    void Start()
    {
        tr = transform;
        beamY = transform.position.y;

        col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        // 플레이어 중력 잠시 끄기
        originalGravityScale = rb.gravityScale;
        rb.gravityScale = 0f;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        // 빔에서 나가면 중력 복구
        rb.gravityScale = originalGravityScale;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        // ============================
        // 1) X축 끌어당김
        // ============================
        float directionX = pullRight ? 1f : -1f;

        if (Mathf.Abs(rb.velocity.x) < maxHorizontalSpeed)
        {
            rb.AddForce(new Vector2(directionX * horizontalPullForce, 0f), ForceMode2D.Force);
        }

        // ============================
        // 2) Y축 정렬 (스프링 + 데드존)
        // ============================
        float deltaY = beamY - rb.position.y;
        float absDeltaY = Mathf.Abs(deltaY);

        // 빔 중심 근처면 Y축 힘은 거의 안 줌 (살짝 떠다닐 수 있게)
        if (absDeltaY < verticalDeadZone)
        {
            // 살짝만 속도 줄여서 흔들림만 줄임
            Vector2 v = rb.velocity;
            v.y *= 0.8f;
            rb.velocity = v;
            return;
        }

        // 스프링 힘: 위치 차이 * 스프링 강도
        float springForce = deltaY * verticalPullForce;

        // 감쇠 힘: 현재 속도에 비례해서 제동
        float dampingForce = rb.velocity.y * verticalDamping;

        float finalVerticalForce = springForce - dampingForce;

        if (Mathf.Abs(rb.velocity.y) < maxVerticalSpeed)
        {
            rb.AddForce(new Vector2(0f, finalVerticalForce), ForceMode2D.Force);
        }
    }
}
