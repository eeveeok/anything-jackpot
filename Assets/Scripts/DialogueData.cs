using UnityEngine;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Dialogue/New Dialogue")]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] lines;
}
