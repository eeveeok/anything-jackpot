using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthUIManager : MonoBehaviour
{
    [Header("Mask Icons (Left to Right)")]
    public Image[] masks;                // 하트 3개
    public Sprite normalMaskSprite;
    public Sprite brokenMaskSprite;

    [Header("Health Settings")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Blink Settings")]
    public float blinkInterval = 0.15f;   // 깜빡임 속도 (빠르게/느리게)
    public int blinkCount = 3;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    void Update()
    {
        // 테스트용 E키로 데미지
        if (Input.GetKeyDown(KeyCode.E))
        {
            TakeDamage(1);
        }
    }

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateUI();

        // 전체 하트 깜빡임
        StartCoroutine(BlinkAllMasks());
    }

    void UpdateUI()
    {
        for (int i = 0; i < masks.Length; i++)
        {
            if (i < currentHealth)
                masks[i].sprite = normalMaskSprite;
            else
                masks[i].sprite = brokenMaskSprite;
        }
    }

    IEnumerator BlinkAllMasks()
    {
       
        for (int i = 0; i < blinkCount; i++)
        {
            // 하트 전부 꺼짐
            foreach (Image img in masks)
                img.enabled = false;

            yield return new WaitForSeconds(blinkInterval);

            // 하트 전부 켜짐
            foreach (Image img in masks)
                img.enabled = true;

            yield return new WaitForSeconds(blinkInterval);
        }
    }
}
