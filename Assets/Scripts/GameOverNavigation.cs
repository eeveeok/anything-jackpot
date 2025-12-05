using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameOverNavigation : MonoBehaviour
{
    public Button yesButton;
    public Button noButton;

    public RectTransform arrow;  // 화살표 이미지

    private int index = 0; // 0 = yes, 1 = no

    private Vector3 selectedScale = new Vector3(1.15f, 1.15f, 1f);
    private Vector3 normalScale = new Vector3(1f, 1f, 1f);

    void OnEnable()
    {
        index = 0;
        arrow.gameObject.SetActive(true);
        HighlightButtons();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            index = 0;
            HighlightButtons();
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            index = 1;
            HighlightButtons();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            if (index == 0) yesButton.onClick.Invoke();
            else noButton.onClick.Invoke();
        }
    }

    void HighlightButtons()
    {
        yesButton.transform.localScale = (index == 0 ? selectedScale : normalScale);
        noButton.transform.localScale = (index == 1 ? selectedScale : normalScale);

        Button target = (index == 0) ? yesButton : noButton;

        // 화살표 위치 조정
        RectTransform targetRT = target.GetComponent<RectTransform>();
        arrow.position = new Vector3(
            targetRT.position.x - targetRT.rect.width * 0.3f,
            targetRT.position.y,
            targetRT.position.z
        );

        EventSystem.current.SetSelectedGameObject(target.gameObject);
    }
}
