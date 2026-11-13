using System;
using System.Collections.Generic;
using System.Linq;

public class ChoiceDefinition
{
    public string id;
    public string text;
    public string nextEventId;
    public string requiredClue;
    public string gainedClue;
    public bool restartLoop;
}

public class StoryEventDefinition
{
    public string id;
    public string text;
    public string emotion = "confused";
    public ChoiceDefinition[] choices;
}

// Fixed draft content. Deletion UI and the weapon's origin are never personified.
public class ActOneStory
{
    public const string WeaponClue = "pursuer_notices_weapon";
    public const string IdentityClue = "familiar_words";
    public const string LoopClue = "pursuer_remembers_loop";
    private readonly Dictionary<string, StoryEventDefinition> events;
    public GameState State { get; private set; }
    public StoryEventDefinition Current => events[State.currentEventId];

    public ActOneStory(GameState state = null)
    {
        events = BuildEvents();
        State = state ?? new GameState();
        State.Normalize();
        if (!events.ContainsKey(State.currentEventId))
            throw new ArgumentException("Unknown Act One event: " + State.currentEventId);
    }

    public List<ChoiceDefinition> AvailableChoices()
    {
        return Current.choices.Where(choice => choice.requiredClue == null
            || State.memory.clueIds.Contains(choice.requiredClue)).ToList();
    }

    public bool Select(string expectedEventId, string choiceId)
    {
        if (State.currentEventId != expectedEventId)
            return false;
        ChoiceDefinition choice = AvailableChoices().Find(item => item.id == choiceId);
        if (choice == null)
            return false;
        if (choice.gainedClue != null)
            State.DiscoverClue(choice.gainedClue);
        if (choice.restartLoop)
        {
            State.BeginNextLoop();
            HashSet<string> memories;
            if (!State.memory.pursuerMemories.TryGetValue("stranger", out memories) || memories == null)
                State.memory.pursuerMemories["stranger"] = memories = new HashSet<string>();
            memories.Add("previous_confrontation");
        }
        else
            State.currentEventId = choice.nextEventId;
        if (State.currentEventId == "pursuer_truce")
            State.memory.knowledgeIds.Add("act_one_completed");
        return true;
    }

    public SceneDirective CreateScene()
    {
        List<ChoiceDefinition> choices = AvailableChoices();
        return new SceneDirective
        {
            backgroundId = "tavern_night", speaker = "stranger", emotion = Current.emotion,
            dialogueText = Current.text, loopCount = State.loopCount,
            choices = choices.Select(choice => choice.text).ToList(),
            choiceIds = choices.Select(choice => choice.id).ToList()
        };
    }

    private static ChoiceDefinition Choice(string id, string text, string next,
        string required = null, string gained = null, bool restart = false)
    {
        return new ChoiceDefinition { id = id, text = text, nextEventId = next,
            requiredClue = required, gainedClue = gained, restartLoop = restart };
    }

    private static Dictionary<string, StoryEventDefinition> BuildEvents()
    {
        var list = new[]
        {
            new StoryEventDefinition { id = "tavern_opening", text = "여관의 침대에서 눈을 뜬다. 허리에는 건네받았던 검이 있다. 문 앞에 선 사람이 네 손을 보다가 검으로 시선을 옮긴다.\n\"그걸 아직도 차고 있네.\"", choices = new[] {
                Choice("ask", "누구인지 묻는다", "weapon_reaction", gained: WeaponClue),
                Choice("run", "문 쪽으로 달린다", "escape_failed", gained: WeaponClue),
                Choice("hide_weapon", "검을 이불 아래 숨긴다", "conversation_window", required: WeaponClue) } },
            new StoryEventDefinition { id = "weapon_reaction", emotion = "angry", text = "그가 검집을 붙잡는다.\n\"이거 쓸모 있을 거야. 그렇게 말했었지.\"\n검을 건네던 날의 말이다. 네가 입을 열기도 전에 그가 다가온다.", choices = new[] {
                Choice("remember", "그 말을 기억한다", "loop_death", gained: IdentityClue) } },
            new StoryEventDefinition { id = "escape_failed", emotion = "angry", text = "문고리에 손이 닿기 전에 그가 길을 막는다. 네 검이 책상 모서리에 부딪친다. 그는 그 소리를 듣고 잠깐 멈춘다.\n\"두고 가지도 않았구나.\"", choices = new[] {
                Choice("push", "문을 밀고 나가려 한다", "loop_death") } },
            new StoryEventDefinition { id = "loop_death", emotion = "angry", text = "바닥에 쓰러진다. 문 아래로 들어오던 빛이 가려진다. 그가 검을 집어 들지만, 끝내 가져가지 않는다. 더는 몸이 움직이지 않는다.", choices = new[] {
                Choice("next_loop", "다시 눈을 뜬다", "tavern_opening", restart: true) } },
            new StoryEventDefinition { id = "conversation_window", text = "그는 네 빈 허리를 살핀다. 문을 닫지는 않는다.\n\"이번에는 어디에 뒀지?\"\n아직 검을 꺼내지 않았는데도, 그는 이미 찾고 있다.", choices = new[] {
                Choice("question", "이번이라는 말을 되묻는다", "shared_loop", gained: LoopClue),
                Choice("quote", "검을 건네던 날의 말을 반복한다", "recognition", required: IdentityClue),
                Choice("escape", "그 틈에 빠져나간다", "escape_failed") } },
            new StoryEventDefinition { id = "recognition", text = "\"이거 쓸모 있을 거야.\"\n네가 그 말을 하자 그는 문고리에서 손을 뗀다.\n\"그 다음에 내가 뭘 했는지도 기억하나?\"\n대답을 기다리던 그가 네 손의 위치를 본다.\n\"지난번에도 거기에 손을 뒀지.\"", choices = new[] {
                Choice("previous", "지난번 일을 어떻게 아는지 묻는다", "shared_loop", gained: LoopClue) } },
            new StoryEventDefinition { id = "shared_loop", text = "\"문으로 달리거나, 내게 묻거나. 그리고 다시 이 방이지.\"\n그는 네가 숨긴 검 쪽을 본다.\n\"불이 났을 때도 그걸 잡았어. 손을 놓을 수가 없었지.\"\n말을 끝낸 뒤에도 그는 문 앞에 서 있다.", choices = new[] {
                Choice("negotiate", "죽여도 끝나지 않았다고 말한다", "pursuer_truce", required: LoopClue),
                Choice("fight", "검을 꺼내 맞선다", "loop_death") } },
            new StoryEventDefinition { id = "pursuer_truce", text = "그가 문 옆으로 물러선다.\n\"오늘은 나가.\"\n네가 검을 챙기자 그가 검집의 탄 자국을 손끝으로 짚는다.\n\"그날은 없던 자국이네.\"\n문이 열린다. 복도 끝에서 아침빛이 들어온다.", choices = new[] {
                Choice("leave", "여관을 나선다", "act_one_end") } },
            new StoryEventDefinition { id = "act_one_end", text = "여관 밖으로 나온다. 뒤에서 문이 닫힌다. 검집에 남은 자국은 닦이지 않는다.\n\n1막 초안 종료 — 첫 추적자의 공격을 멈췄다.", choices = new ChoiceDefinition[0] }
        };
        return list.ToDictionary(item => item.id);
    }
}
