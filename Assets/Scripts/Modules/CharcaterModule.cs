using UnityEngine;
using UnityEngine.UI;

public class CharacterModule : MonoBehaviour
{
    [SerializeField] private SceneDirectiveEventChannelSO channel;
    [SerializeField] private Image CharacterImage;

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
        string spriteKey = directive.speaker + "_" + directive.emotion;
        Debug.Log($"[CharacterModule] 찾는 스프라이트: {spriteKey}");

        Sprite charSprite = Resources.Load<Sprite>("Characters/" + spriteKey);

        if (charSprite == null)
        {
            Debug.LogWarning($"[CharacterModule] 캐릭터 스프라이트를 찾을 수 없음: {spriteKey}");
            return;
        }

        CharacterImage.sprite = charSprite;
        Debug.Log($"[CharacterModule] 캐릭터 스프라이트 적용 완료: {charSprite.name}");
    }
}