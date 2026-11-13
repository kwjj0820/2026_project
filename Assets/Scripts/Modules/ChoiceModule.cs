using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChoiceModule : MonoBehaviour
{
    [SerializeField] private SceneDirectiveEventChannelSO channel;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private AISceneTrigger sceneTrigger; // 추가

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
        foreach (Transform child in container)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        for (int i = 0; i < directive.choices.Count; i++)
        {
            string choiceText = directive.choices[i];
            GameObject newButton = Instantiate(buttonPrefab, container);
            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = choiceText;

            // 클릭 이벤트 등록 (추가된 부분)
            Button btn = newButton.GetComponent<Button>();
            string capturedChoice = directive.choiceIds != null && i < directive.choiceIds.Count
                ? directive.choiceIds[i] : choiceText;
            string capturedEvent = sceneTrigger.CurrentEventId;
            btn.onClick.AddListener(() =>
            {
                if (!btn.interactable)
                    return;
                btn.interactable = false;
                sceneTrigger.RequestChoice(capturedEvent, capturedChoice);
            });
        }
    }
}
