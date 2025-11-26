using UnityEngine;

public class MouseCursor : MonoBehaviour
{
    public Texture2D cursorTexture;

    void Start()
    {
        // 1. 커서 표시
        Cursor.visible = true;

        // 2. 커서 고정 해제
        Cursor.lockState = CursorLockMode.None;

        // 3. 커서 아이콘 변경
        if (cursorTexture != null)
        {
            Vector2 hotSpot = new Vector2(cursorTexture.width / 2, cursorTexture.height / 2); // 중앙 클릭 지점
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
        }
        else
        {
            Debug.LogWarning("Cursor Texture is missing!");
        }
    }
}

