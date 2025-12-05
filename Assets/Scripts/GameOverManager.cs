using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverPanel;

    public Text gameOverText;
    public Text continueText;

    public GameObject yesButtonObj;
    public GameObject noButtonObj;
    public GameObject navigationObject;

    void Start()
    {
        gameOverPanel.SetActive(false);
        gameOverText.gameObject.SetActive(false);
        continueText.gameObject.SetActive(false);
        yesButtonObj.SetActive(false);
        noButtonObj.SetActive(false);
        navigationObject.SetActive(false);
    }

    void Update()
    {
        // 테스트용: T키를 누르면 즉시 게임오버 UI 출력
        if (Input.GetKeyDown(KeyCode.T))
        {
            TriggerGameOver();
        }
    }

    public void TriggerGameOver()
    {
        Time.timeScale = 0f;
        StartCoroutine(GameOverSequence());
    }

    IEnumerator GameOverSequence()
    {
        gameOverPanel.SetActive(true);

    
        gameOverText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(1.0f);

        continueText.gameObject.SetActive(true);

        yesButtonObj.SetActive(true);
        noButtonObj.SetActive(true);
        navigationObject.SetActive(true);
    }

    public void OnClickYes()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnClickNo()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Title");
    }
}
