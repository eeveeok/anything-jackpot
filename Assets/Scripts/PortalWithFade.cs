using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalWithFade : MonoBehaviour
{
    public Vector3 targetPosition;            // 순간이동할 위치
    public float fadeDuration = 0.5f;         // 페이드 시간
    public MonoBehaviour playerMovementScript; // 플레이어 이동 스크립트
    public CanvasGroup fadeCanvas;            // 흰 화면 CanvasGroup

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

        // 1) 플레이어 이동 스크립트 즉시 비활성화 → 입력 정지
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        // 2) Rigidbody2D 속도 완전 초기화 → 즉시 정지
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;   // 순간적으로 중력도 제거 → 깔끔하게 멈춤
        }

        // 페이드아웃 (화면 → 흰색)
        yield return StartCoroutine(Fade(0f, 1f));

        // 위치 순간이동
        player.transform.position = targetPosition;

        // 0.05초 정도 대기 (화면 안정)
        yield return new WaitForSeconds(0.05f);

        // 페이드인 (흰색 → 화면 복귀)
        yield return StartCoroutine(Fade(1f, 0f));

        // 중력 되돌리기
        if (rb != null)
            rb.gravityScale = 3f;

        // 이동 스크립트 다시 활성화
        if (playerMovementScript != null)
            playerMovementScript.enabled = true;

        isProcessing = false;
    }

    private IEnumerator Fade(float start, float end)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            fadeCanvas.alpha = Mathf.Lerp(start, end, t);
            yield return null;
        }

        fadeCanvas.alpha = end;
    }
}

