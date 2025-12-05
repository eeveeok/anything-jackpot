using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuNavigation : MonoBehaviour
{
    public Button[] buttons;

    public RectTransform arrowLeft;
    public RectTransform arrowRight;

    private int index = 0;

    void Start()
    {
        // 처음 선택될 버튼
        EventSystem.current.SetSelectedGameObject(buttons[index].gameObject);

        // 화살표 위치 업데이트
        UpdateArrows();
    }

    void Update()
    {
        // ↓ 아래 버튼 선택
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            index++;
            if (index >= buttons.Length)
                index = 0; // 순환

            SelectCurrentButton();
        }

        // ↑ 위 버튼 선택
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            index--;
            if (index < 0)
                index = buttons.Length - 1; // 순환

            SelectCurrentButton();
        }
    }

    void SelectCurrentButton()
    {
        EventSystem.current.SetSelectedGameObject(buttons[index].gameObject);
        UpdateArrows();
    }

    void UpdateArrows()
    {
        RectTransform target = buttons[index].GetComponent<RectTransform>();

        // 화살표 보이게
        arrowLeft.gameObject.SetActive(true);
        arrowRight.gameObject.SetActive(true);

        // 왼쪽 화살표 위치 조정
        arrowLeft.position = new Vector3(
            target.position.x - (target.rect.width * 0.65f),
            target.position.y,
            target.position.z
        );

        // 오른쪽 화살표 위치 조정
        arrowRight.position = new Vector3(
            target.position.x + (target.rect.width * 0.65f),
            target.position.y,
            target.position.z
        );
    }
}

