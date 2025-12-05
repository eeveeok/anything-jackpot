using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;   

public class PortalWithFade : MonoBehaviour
{
    public string sceneToLoad;                    // 이동할 씬 이름
    public float fadeDuration = 0.5f;             // 페이드 시간
    public float suctionSpeed = 5f;               // 빨려들어가는 속도
    public float rotateSpeed = 360f;              // 회전 속도

    public MonoBehaviour playerMovementScript;    // 플레이어 이동 스크립트
    public CanvasGroup fadeCanvas;                // 페이드용 CanvasGroup
    public Transform portalCenter;                // 포탈 중심 (Portal 오브젝트)

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

            fadeCanvas.alpha = Mathf.Lerp(0f, 1f, t);

            if (portalCenter != null)
            {
                player.transform.position = Vector3.Lerp(
                    player.transform.position,
                    portalCenter.position,
                    t * 0.35f
                );
            }

            player.transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

            yield return null;
        }

        //4) 위치 이동 → "씬 이동"으로 변경
        SceneManager.LoadScene(sceneToLoad);

        // 씬이 로드될 때까지 기다리기
        yield return null;

        // 여기서부터는 새 씬 
        // 새 씬에서 FadeCanvas를 자동으로 0으로 두기 위해
        fadeCanvas.alpha = 1f;

        // 5) 페이드인
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            fadeCanvas.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        isProcessing = false;
    }
}







