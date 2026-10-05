using UnityEngine;
using System.Collections.Generic;

public class AISceneTrigger : MonoBehaviour
{
    [SerializeField] private SceneDirectiveEventChannelSO channel;
    [SerializeField] private AIRequestService aiService;

    private const string STORY_BIBLE = @"
너는 다크 판타지 이세계 TRPG의 스토리 진행자다. 다음 규칙을 반드시 지켜라.

[세계관]
- 정통 이세계 판타지, 다크하고 진지한 톤을 유지한다.
- 주인공은 튜토리얼(이전 생)의 세이브 파일을 삭제하여 그 세계를 멸망시켰다.
- 이 사실을 기억하는 존재들이 주인공을 추적하며 살해하려 한다.
- 죽음은 되돌릴 수 없는 무게로 다뤄야 하며, 가벼운 개그나 코미디 요소를 넣지 않는다.
- 이전 대화에서 나온 내용(등장인물, 사건)을 기억하고 일관성 있게 이어가라.

[문체 규칙]
- 과장된 형용사나 은유를 남발하지 않는다. (예: ""차가운 쇠붙이"", ""썩은 피와 유황 냄새"" 같은 표현 지양)
- 감정과 분위기는 대사와 행동으로 보여주고, 수식어로 설명하지 않는다.
- 문장은 짧고 담백하게 쓴다. 화려한 수사 대신 절제된 묘사를 쓴다.
- 다크함은 ""잔인한 묘사""가 아니라 ""선택의 무게""와 ""돌이킬 수 없음""에서 나온다.

[출력 규칙]
- backgroundId는 반드시 다음 중 하나: tavern_night, forest_day, ruins_dusk
- emotion은 반드시 다음 중 하나: neutral, angry, confused, sad, fear
- choices는 짧고 명확한 행동 선택지 2개로 구성한다.
- dialogueText는 과도하게 길지 않게, 몰입감 있는 3~5문장 이내로 작성한다.
";

    private List<ChatMessage> conversationHistory = new List<ChatMessage>();
    private int loopCount = 1;

    void Start()
    {
        SaveData loaded = SaveSystem.Load();

        if (loaded != null)
        {
            conversationHistory = loaded.conversationHistory;
            loopCount = loaded.loopCount;
            Debug.Log($"[AISceneTrigger] 이어하기, 현재 루프: {loopCount}");

            // 추가: 마지막 assistant 응답을 다시 화면에 반영
            RestoreLastScene();
        }
        else
        {
            string firstTurn = "주인공이 여관에서 눈을 뜨자마자 낯선 존재에게 살해당하려는 첫 장면을 만들어줘.";
            conversationHistory.Add(new ChatMessage { role = "user", content = firstTurn });
            StartCoroutine(aiService.SendConversation(STORY_BIBLE, conversationHistory, HandleApiResponse));
        }
    }

    private void RestoreLastScene()
    {
        // 히스토리를 뒤에서부터 탐색해서 가장 최근 assistant 메시지를 찾음
        for (int i = conversationHistory.Count - 1; i >= 0; i--)
        {
            if (conversationHistory[i].role == "assistant")
            {
                SceneDirective directive = Newtonsoft.Json.JsonConvert.DeserializeObject<SceneDirective>(conversationHistory[i].content);
                channel.Raise(directive);
                Debug.Log("[AISceneTrigger] 마지막 씬 복원 완료");
                return;
            }
        }

        Debug.LogWarning("[AISceneTrigger] 복원할 assistant 메시지를 찾지 못함");
    }

    public void RequestNextScene(string playerChoice)
    {
        string nextTurn = $"플레이어가 방금 '{playerChoice}'를 선택했다. 그 선택의 결과로 이어지는 다음 장면을 만들어줘.";
        conversationHistory.Add(new ChatMessage { role = "user", content = nextTurn });

        StartCoroutine(aiService.SendConversation(STORY_BIBLE, conversationHistory, HandleApiResponse));
    }

    private void HandleApiResponse(string rawResponse)
    {
        SceneDirective directive = SceneDirectiveParser.Parse(rawResponse);

        string assistantReply = SceneDirectiveParser.ExtractRawText(rawResponse);
        conversationHistory.Add(new ChatMessage { role = "assistant", content = assistantReply });

        Debug.Log($"[AISceneTrigger] AI가 생성한 씬: {directive.speaker} - {directive.dialogueText}");
        channel.Raise(directive);

        // 매 씬마다 자동 저장
        SaveSystem.Save(new SaveData
        {
            loopCount = loopCount,
            conversationHistory = conversationHistory
        });
    }

    // 테스트용: 저장 파일 삭제하고 처음부터 다시 시작하고 싶을 때 사용
    [ContextMenu("Clear Save And Reset")]
    public void ClearSaveForTesting()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, "save.json");
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
            Debug.Log("[AISceneTrigger] 저장 파일 삭제됨");
        }
    }
}