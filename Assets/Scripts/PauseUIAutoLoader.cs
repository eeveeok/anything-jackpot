using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseUIAutoLoader : MonoBehaviour
{
    public GameObject pauseUIPrefab;

    void Awake()
    {
        // Title 씬에서는 Pause UI 필요 없음
        if (SceneManager.GetActiveScene().name == "Title")
            return;

        // 이미 PauseManager가 있다면 생성할 필요 없음
        if (FindObjectOfType<PauseManager>() != null)
            return;

        GameObject ui = Instantiate(pauseUIPrefab);
        DontDestroyOnLoad(ui);
    }
}
