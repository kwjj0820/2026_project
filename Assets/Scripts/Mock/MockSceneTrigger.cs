using UnityEngine;
using System.Collections.Generic;

public class MockSceneTrigger : MonoBehaviour
{
    [SerializeField] private SceneDirectiveEventChannelSO channel;
    private List<SceneDirective> mockScenes;
    private int currentIndex = 0;

    void Start()
    {
        mockScenes = new List<SceneDirective>
        {
            new SceneDirective
            {
                backgroundId = "tavern_night",
                speaker = "stranger",
                dialogueText = "Scene_1_Text.",
                emotion = "confused",
                loopCount = 1,
                choices = new List<string>()
            },
            new SceneDirective
            {
                backgroundId = "tavern_night",
                speaker = "stranger",
                dialogueText = "Scene_2_Text.",
                emotion = "angry",
                loopCount = 1,
                choices = new List<string> { "Choice_1", "Choice_2" }
            }
        };

        PlayCurrentScene();
    }

    private void PlayCurrentScene()
    {
        channel.Raise(mockScenes[currentIndex]);
    }

    // 버튼이나 스페이스바 등에서 호출할 함수
    public void NextScene()
    {
        currentIndex++;
        if (currentIndex < mockScenes.Count)
        {
            PlayCurrentScene();
        }
        else
        {
            Debug.Log("[MockSceneTrigger] 더 이상 씬 없음");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("스페이스바 눌림 감지"); // 이 줄 추가
            NextScene();
        }
    }
}