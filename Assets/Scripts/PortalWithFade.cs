using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalWithFade : MonoBehaviour
{
    public Vector3 targetPosition;              // Teleport 위치
    public float fadeDuration = 0.5f;           // 페이드 시간
    public float suctionSpeed = 5f;             // 빨려들어가는 속도
    public float rotateSpeed = 360f;            // 회전 속도 (초당 360도)

    public MonoBehaviour playerMovementScript;  // 플레이어 이동 스크립트
    public CanvasGroup fadeCanvas;              // 페이드용 CanvasGroup
    public Transform portalCenter;              // 포탈 중심 (Portal 오브젝트)

    private bool isProcessing = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player")) return;
        if (isProcessing) return;

        StartCoroutine(FadeTeleport(collision.collider));
    }

    private IEnumerator FadeTeleport(Collider2D player)
    {
        isProcessing = true;

        // 1) 플레이어 이동 스크립트 비활성화
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        // 2) 중력 저장 후 제거
        float originalGravity = 0f;
        if (rb != null)
        {
            originalGravity = rb.gravityScale;
            rb.gravityScale = 0f;

            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 3) 페이드아웃 + 빨려들기 + 회전
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            // 화면 페이드아웃
            fadeCanvas.alpha = Mathf.Lerp(0f, 1f, t);

            // 빨려들기
            if (portalCenter != null)
            {
                player.transform.position = Vector3.Lerp(
                    player.transform.position,
                    portalCenter.position,
                    t * 0.35f
                );
            }

            // 회전 효과
            player.transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

            yield return null;
        }

        // 4) 순간이동
        player.transform.position = targetPosition;

        yield return new WaitForSeconds(0.05f);

        // 5) 페이드인
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            fadeCanvas.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        // 6) 중력 복원
        if (rb != null)
            rb.gravityScale = originalGravity;

        // 7) 이동 스크립트 재활성화
        if (playerMovementScript != null)
            playerMovementScript.enabled = true;

        isProcessing = false;
    }
}





