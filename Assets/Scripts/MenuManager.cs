using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;   // Panel_MainMenu
    public GameObject creditPanel;     // Panel_Credit

    // Start 버튼 → 게임 시작
    public void OnClickStart()
    {
        SceneManager.LoadScene("TestScene");   // 메인 게임 씬 이름
    }

    // Exit 버튼 → 게임 종료
    public void OnClickExit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;  // 에디터에서는 이것도 필요
#endif
    }

    // Credit 버튼 → 크레딧 패널 활성화
    public void OnClickCredit()
    {
        creditPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    // Close 버튼 → 크레딧 패널 닫기
    public void OnClickCloseCredit()
    {
        creditPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
}
