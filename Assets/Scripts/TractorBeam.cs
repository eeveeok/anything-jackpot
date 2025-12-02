using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TractorBeam : MonoBehaviour
{
    [Header("X축 끌어당김 설정")]
    public bool pullRight = true;                // true면 오른쪽, false면 왼쪽으로 당김
    public float horizontalPullForce = 200f;     // X축 끌어당기는 힘
    public float maxHorizontalSpeed = 50f;       // X축 최대 속도

    [Header("Y축 정렬 설정")]
    public float verticalPullForce = 30f;        // Y축 스프링 강도
    public float verticalDamping = 4f;           // Y축 감쇠(제동)
    public float maxVerticalSpeed = 18f;         // Y축 최대 속도
    public float verticalDeadZone = 0.05f;       // 빔 중심 근처에서 힘을 주지 않는 구간

    private Transform tr;
    private float beamY;
    private BoxCollider2D col;

    // =========================================================================
    // [중첩 문제 해결용 정적 변수]
    // 모든 TractorBeam 인스턴스가 공유하는 장부
    // =========================================================================

    // 어떤 리지드바디가 현재 몇 개의 빔에 들어와 있는지 카운트 (참조 카운팅)
    private static Dictionary<Rigidbody2D, int> beamCounts = new Dictionary<Rigidbody2D, int>();

    // 해당 리지드바디가 맨 처음 빔에 들어왔을 때의 원래 중력 저장
    private static Dictionary<Rigidbody2D, float> originalGravities = new Dictionary<Rigidbody2D, float>();

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

        // 1. 장부에 이 플레이어가 처음 등록되는지 확인 (첫 번째 빔 진입)
        if (!beamCounts.ContainsKey(rb))
        {
            beamCounts.Add(rb, 0);

            // 처음 들어올 때만 '원래 중력을 저장
            // (이미 다른 빔 안에 있다면 중력이 0일 테니 저장하면 안 됨)
            originalGravities.Add(rb, rb.gravityScale);

            // 플레이어 중력 잠시 끄기
            rb.gravityScale = 0f;
        }

        // 2. 카운트 증가 (빔 진입 개수 +1)
        beamCounts[rb]++;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        // 장부에 등록되어 있다면 처리
        if (beamCounts.ContainsKey(rb))
        {
            // 1. 카운트 감소 (빔 하나 빠져나감)
            beamCounts[rb]--;

            // 2. 카운트가 0이 되면 (모든 빔에서 완전히 벗어남)
            if (beamCounts[rb] <= 0)
            {
                // 원래 중력 복구
                if (originalGravities.ContainsKey(rb))
                {
                    rb.gravityScale = originalGravities[rb];
                    originalGravities.Remove(rb); // 장부에서 중력 정보 삭제
                }

                beamCounts.Remove(rb); // 장부에서 카운트 정보 삭제
            }
        }
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