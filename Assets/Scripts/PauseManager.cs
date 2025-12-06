using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public GameObject pausePanel;
    private bool isPaused = false;
    public LaserShooter player;   // 플레이어 스크립트 연결


    void Start()
    {
        pausePanel.SetActive(false);  // 시작할 때 항상 숨김
        Time.timeScale = 1f;          // 게임 속도 정상화
    }


    void Update()
    {
        // ESC 키로 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f; // 게임 멈춤

        if (player != null)
            player.enabled = false;   // 일시정지 동안 조작 금지
    }

    public void Resume()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f; // 게임 재개

        if (player != null)
            player.enabled = true;    // 게임 재개 시 조작 가능
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TestScene");
    }
}
