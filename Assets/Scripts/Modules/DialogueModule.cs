using UnityEngine;
using TMPro;

public class DialogueModule : MonoBehaviour
{
    [SerializeField] private SceneDirectiveEventChannelSO channel;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI dialogueText;

    private void OnEnable()
    {
        channel.OnEventRaised += HandleDirective;
    }

    private void OnDisable()
    {
        channel.OnEventRaised -= HandleDirective;
    }

    private void HandleDirective(SceneDirective directive)
    {
        speakerText.text = directive.speaker;
        dialogueText.text = directive.dialogueText;
        Debug.Log($"[DialogueModule] {directive.speaker}: {directive.dialogueText}");
    }
}