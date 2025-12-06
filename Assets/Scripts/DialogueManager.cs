using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialoguePanel;
    public Text dialogueText;
    public GameObject nextIndicator;
    public GameObject portraitImage;

    [Header("Typing Settings")]
    public float typingSpeed = 0.04f;

    [Header("Dialogue Data (7 NPCs)")]
    public DialogueData npc1Dialogue;
    public DialogueData npc2Dialogue;
    public DialogueData npc3Dialogue;
    public DialogueData npc4Dialogue;
    public DialogueData npc5Dialogue;
    public DialogueData npc6Dialogue;
    public DialogueData npc7Dialogue;

    [Header("Player Control Block")]
    public LaserShooter player;  // 플레이어 오브젝트 연결


    private DialogueData currentDialogue;
    private int currentIndex = 0;
    private bool isTyping = false;
    private bool isDialogueActive = false;

   // public LaserShooter playerController;   // Inspector에서 플레이어 오브젝트 연결


    public static DialogueManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);   // 씬 이동해도 유지
        }
        else
        {
            Destroy(gameObject);             // 중복 생성 방지
        }
    }


    void Start()
    {
        dialoguePanel.SetActive(false);
        nextIndicator.SetActive(false);
        portraitImage.SetActive(false);   // 처음에 초상화 OFF
        DialogueManager.Instance.StartDialogue(npc1Dialogue);
    }

    void Update()
    {
        // 테스트용: 숫자 키로 대화 시작
        if (!isDialogueActive)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                StartDialogue(npc1Dialogue);

            if (Input.GetKeyDown(KeyCode.Alpha2))
                StartDialogue(npc2Dialogue);

            if (Input.GetKeyDown(KeyCode.Alpha3))
                StartDialogue(npc3Dialogue);

            if (Input.GetKeyDown(KeyCode.Alpha4))
                StartDialogue(npc4Dialogue);

            if (Input.GetKeyDown(KeyCode.Alpha5))
                StartDialogue(npc5Dialogue);

            if (Input.GetKeyDown(KeyCode.Alpha6))
                StartDialogue(npc6Dialogue);

            if (Input.GetKeyDown(KeyCode.Alpha7))
                StartDialogue(npc7Dialogue);
        }

        // 대화 진행 중 스페이스바로 넘기기
        if (isDialogueActive && Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                StopAllCoroutines();
                dialogueText.text = currentDialogue.lines[currentIndex].text;
                isTyping = false;
                nextIndicator.SetActive(true);
            }
            else
            {
                NextLine();
            }
        }
    }

    public void StartDialogue(DialogueData dialogueData)
    {
        currentDialogue = dialogueData;
        currentIndex = 0;
        isDialogueActive = true;

        // 플레이어 조작 OFF
        if (player != null)
            player.enabled = false;

        dialoguePanel.SetActive(true);
        nextIndicator.SetActive(false);

        ShowLine();
    }

    void ShowLine()
    {
        DialogueLine line = currentDialogue.lines[currentIndex];

        // 주인공일 때만 초상화 ON
        if (line.speaker == "player")
            portraitImage.SetActive(true);
        else
            portraitImage.SetActive(false);

        dialogueText.text = "";
        StartCoroutine(TypeText(line.text));
    }

    IEnumerator TypeText(string line)
    {
        isTyping = true;
        nextIndicator.SetActive(false);

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        nextIndicator.SetActive(true);
    }

    void NextLine()
    {
        currentIndex++;

        if (currentIndex < currentDialogue.lines.Length)
        {
            ShowLine();
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
        dialogueText.text = "";
        portraitImage.SetActive(false);

        // 플레이어 조작 ON
        if (player != null)
            player.enabled = true;
    }
}
