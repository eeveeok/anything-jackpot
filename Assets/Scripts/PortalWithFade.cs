using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalWithFade : MonoBehaviour
{
    public string sceneToLoad;                    // 이동할 씬 이름
    public float fadeDuration = 0.5f;             // 페이드 시간
    public float suctionSpeed = 5f;               // 빨려들어가는 속도
    public float rotateSpeed = 360f;              // 회전 속도

    // public MonoBehaviour playerMovementScript;  // 더 이상 사용 안 함
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

        // 1) 플레이어 이동 스크립트 비활성화 삭제
        // if (playerMovementScript != null)
        //     playerMovementScript.enabled = false;

        // Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        // 2) 중력 저장 후 제거 삭제
        // float originalGravity = 0f;
        // if (rb != null)
        // {
        //     originalGravity = rb.gravityScale;
        //     rb.gravityScale = 0f;
        //     rb.velocity = Vector2.zero;
        //     rb.angularVelocity = 0f;
        // }

        // 3) 페이드아웃 + 빨려들기 + 회전
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            if (fadeCanvas != null)
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

        // 4) 씬 이동
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("PortalWithFade: sceneToLoad가 비어 있습니다.");
        }

        // 여기서부터는 새 씬이라, 이 포탈 오브젝트/Canvas는 없어질 수도 있음.
        // 새 씬에서 따로 페이드인을 할 거면 그 씬에서 CanvasGroup을 따로 만들어서 처리하는 게 더 안전함.

        isProcessing = false;
    }
}








