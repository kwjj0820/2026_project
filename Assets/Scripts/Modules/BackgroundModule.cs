using UnityEngine;
using UnityEngine.UI;

public class BackgroundModule : MonoBehaviour
{
    [SerializeField] private SceneDirectiveEventChannelSO channel;
    [SerializeField] private Image backgroundImage;

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
        Debug.Log($"[BackgroundModule] 받은 backgroundId: {directive.backgroundId}");

        Sprite bgSprite = Resources.Load<Sprite>("Backgrounds/" + directive.backgroundId);

        if (bgSprite == null)
        {
            Debug.LogWarning($"[BackgroundModule] 배경을 찾을 수 없음: {directive.backgroundId}");
            return;
        }

        backgroundImage.sprite = bgSprite;
        Debug.Log($"[BackgroundModule] 배경 적용 완료: {bgSprite.name}");
    }
}