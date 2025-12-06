using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PauseNavigation : MonoBehaviour
{
    public Button[] pauseButtons; // 계속하기, 나가기
    private int index = 0;

    void Awake()
    {
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }


    private void OnEnable()
    {
        index = 0;
        EventSystem.current.SetSelectedGameObject(pauseButtons[index].gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            index++;
            if (index >= pauseButtons.Length)
                index = 0;

            EventSystem.current.SetSelectedGameObject(pauseButtons[index].gameObject);
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            index--;
            if (index < 0)
                index = pauseButtons.Length - 1;

            EventSystem.current.SetSelectedGameObject(pauseButtons[index].gameObject);
        }
    }
}
