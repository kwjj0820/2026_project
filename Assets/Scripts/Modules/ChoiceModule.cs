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
            Destroy(child.gameObject);
        }

        foreach (string choiceText in directive.choices)
        {
            GameObject newButton = Instantiate(buttonPrefab, container);
            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = choiceText;

            // 클릭 이벤트 등록 (추가된 부분)
            Button btn = newButton.GetComponent<Button>();
            string capturedChoice = choiceText; // 클로저 문제 방지용
            btn.onClick.AddListener(() => sceneTrigger.RequestNextScene(capturedChoice));
        }
    }
}