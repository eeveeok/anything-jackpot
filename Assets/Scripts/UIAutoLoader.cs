using UnityEngine;
using UnityEngine.SceneManagement;

public class UIAutoLoader : MonoBehaviour
{
    // Inspector에서 DialogueUI 프리팹을 넣을 공간
    public GameObject dialogueUI;

    void Awake()
    {
        string scene = SceneManager.GetActiveScene().name;

        // Title 같은 특정 씬에서는 UI 생성 안 함
        if (scene == "Title")
            return;

        // 이미 DialogueManager가 존재한다면(=UI가 이미 생성됨)
        if (FindObjectOfType<DialogueManager>() != null)
            return;

        // UI 생성
        GameObject ui = Instantiate(dialogueUI);
        DontDestroyOnLoad(ui);
    }
}
