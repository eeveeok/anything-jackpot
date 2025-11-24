using UnityEngine;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Dialogue/New Dialogue")]
public class DialogueData : ScriptableObject
{
    [TextArea(2, 5)]
    public string[] lines;
}
