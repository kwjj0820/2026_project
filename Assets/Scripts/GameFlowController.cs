using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    private const string SaveFile = "act_one_save.json";
    private SceneDirectiveEventChannelSO channel;
    private ActOneStory story;
    public string CurrentEventId => story != null ? story.State.currentEventId : null;

    public void Initialize(SceneDirectiveEventChannelSO eventChannel)
    {
        channel = eventChannel;
        SaveData save = SaveSystem.Load(SaveFile);
        try
        {
            story = new ActOneStory(save != null ? save.gameState : null);
        }
        catch (System.ArgumentException exception)
        {
            Debug.LogError("[ActOne] 저장된 사건을 찾을 수 없음: " + exception.Message);
            return;
        }
        Debug.Log("[ActOne] " + (save == null ? "새 게임" : "이어하기")
            + " | 저장 경로: " + System.IO.Path.Combine(Application.persistentDataPath, SaveFile));
        Present();
    }

    public void SelectChoice(string eventId, string choiceId)
    {
        if (story == null)
            return;
        int previousLoop = story.State.loopCount;
        int previousClues = story.State.memory.clueIds.Count;
        if (!story.Select(eventId, choiceId))
        {
            Debug.LogWarning("[ActOne] 무효/이전 장면 선택 무시: " + choiceId);
            return;
        }
        Debug.Log("[ActOne] 선택: " + choiceId);
        if (previousClues != story.State.memory.clueIds.Count)
            Debug.Log("[ActOne] 단서: " + string.Join(", ", story.State.memory.clueIds));
        if (previousLoop != story.State.loopCount)
            Debug.Log("[ActOne] 회차 전환: " + previousLoop + " → " + story.State.loopCount);
        Present();
    }

    public void StartFresh()
    {
        story = new ActOneStory();
        Debug.Log("[ActOne] 새 테스트 시작 (기존 AI 저장은 유지)");
        Present();
    }

    private void Present()
    {
        SceneDirective scene = story.CreateScene();
        SaveSystem.Save(new SaveData { gameState = story.State, lastScene = scene }, SaveFile);
        Debug.Log("[ActOne] 사건: " + CurrentEventId + " | 회차: " + story.State.loopCount
            + " | 선택지: " + scene.choices.Count);
        channel.Raise(scene);
    }
}
