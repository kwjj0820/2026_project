using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "SceneDirectiveEventChannel", menuName = "Events/SceneDirectiveEventChannel")]
public class SceneDirectiveEventChannelSO : ScriptableObject
{
    public UnityAction<SceneDirective> OnEventRaised;

    public void Raise(SceneDirective directive)
    {
        OnEventRaised?.Invoke(directive);
    }
}