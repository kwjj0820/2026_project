using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class TutorialRecord
{
    public string id;
    public string kind;
    public string title;
    public string source;
    public string description;
}

[Serializable]
public class TutorialMessage
{
    public string speaker;
    public string text;
    public string location;
}

[Serializable]
public class TutorialProgress
{
    public int version = 1;
    public string location = "room";
    public HashSet<string> records = new HashSet<string>();
    public HashSet<string> tools = new HashSet<string>();
    public HashSet<string> flags = new HashSet<string>();
    public List<TutorialMessage> transcript = new List<TutorialMessage>();
    public string lastText;
    public string notice;
}

public class TutorialAction
{
    public string id;
    public string label;
    public TutorialAction(string actionId, string actionLabel) { id = actionId; label = actionLabel; }
}

// Authoritative tutorial rules. Player claims and generated dialogue never grant evidence.
public class TutorialSession
{
    public const string Misdelivery = "wrong_store";
    public const string Ownership = "ownership";
    public TutorialProgress Progress { get; private set; }
    public static readonly TutorialRecord[] RecordDefinitions =
    {
        Record("adventurer", "물건", "길드 등록증", "방 안의 가방", "자신의 이름과 길드 표식이 적힌 등록증. 검과 여행용 짐도 곁에 있다."),
        Record("order", "문서", "여관 주문서", "여관 / 길드 의뢰 사본", "여관으로 식재료 두 상자. 주문 번호 17. 여관에는 한 상자만 도착했다."),
        Record("testimony", "증언", "배달원의 진술", "배달원과의 대화", "창고에서 두 상자를 받았다. 둘 다 여관 뒤편에 내려놓았다고 한다. 사실 여부는 미확인."),
        Record("delivery_slip", "문서", "배달표", "배달원", "주문 17, 두 상자. 각 상자에 같은 주문 번호가 붙어 있다."),
        Record("doors", "관찰", "뒤편의 두 출입문", "골목 조사", "서로 가까운 문 두 개. 하나는 여관 부엌, 다른 하나는 옆 가게 창고다."),
        Record("crate", "물건", "옆 창고의 상자 표식", "가게 창고 조사", "문 옆 식재료 상자의 표식에 17이 적혀 있다. 아직 가져오지는 않았다.")
    };

    private static TutorialRecord Record(string id, string kind, string title, string source, string text)
    {
        return new TutorialRecord { id = id, kind = kind, title = title, source = source, description = text };
    }

    public TutorialSession(TutorialProgress progress = null)
    {
        Progress = progress ?? new TutorialProgress();
        if (Progress.version != 1 || !new[] { "room", "guild", "yard", "alley", "store", "inn", "complete" }.Contains(Progress.location))
            throw new ArgumentException("지원하지 않는 튜토리얼 저장 상태입니다.");
        Progress.records = Progress.records ?? new HashSet<string>();
        Progress.tools = Progress.tools ?? new HashSet<string>();
        Progress.flags = Progress.flags ?? new HashSet<string>();
        Progress.transcript = Progress.transcript ?? new List<TutorialMessage>();
        if (Progress.lastText == null)
            Say("문을 두드리는 소리에 잠에서 깬다. 침대 옆에는 검과 가방이 놓여 있다.\n\"밥 가져왔는데, 들어가도 돼?\"", "여관 주인의 딸");
    }

    public string LocationTitle => new Dictionary<string, string>
    {
        { "room", "여관 · 아침" }, { "guild", "길드 접수처" }, { "yard", "배송 집결지" },
        { "alley", "여관 뒤 골목" }, { "store", "옆 가게 창고" }, { "inn", "여관 · 저녁" }, { "complete", "튜토리얼 완료" }
    }[Progress.location];

    public string Actor => Progress.location == "room" || Progress.location == "inn" ? "여관 주인의 딸"
        : Progress.location == "guild" ? "길드 접수원" : Progress.location == "yard" ? "배달원"
        : Progress.location == "store" ? "가게 주인" : "주변 조사";

    public string Objective => Progress.location == "complete" ? "튜토리얼 종료. 월드 삭제와 본편 첫 조우는 다음 제작 단계입니다."
        : Progress.flags.Contains("reported") ? "하루를 마쳤습니다. 여관에서 쉬세요."
        : Progress.flags.Contains("recovered") ? "찾은 식재료를 여관에 전달하세요."
        : Progress.tools.Contains(Ownership) ? "가게 주인에게 소유권 확인 근거를 제시하세요."
        : Progress.flags.Contains("door_identified") ? "옆 창고를 조사하고 상자의 주인을 확인하세요."
        : Progress.tools.Contains(Misdelivery) ? "배달원에게 잘못된 창고 가능성을 제시하세요."
        : Progress.flags.Contains("accepted") ? "배달원에게 묻고 여관 뒤편을 조사하세요. 기록 두 개를 연결해 보세요."
        : Progress.records.Contains("adventurer") ? "여관의 부탁을 확인하고 길드에서 의뢰를 받으세요."
        : "아침 대화를 나누고 방 안의 짐을 살펴보세요.";

    public List<TutorialAction> Actions()
    {
        var actions = new List<TutorialAction>();
        switch (Progress.location)
        {
            case "room":
                Add(actions, "breakfast", "아침 식사를 받는다", !Has("ate"));
                Add(actions, "ask_order", "길드에 갈 일이 있는지 묻는다", !Progress.records.Contains("order"));
                Add(actions, "inspect_room", "방 안의 짐을 살핀다", !Progress.records.Contains("adventurer"));
                Add(actions, "go_guild", "등록증을 챙겨 길드로 간다", Progress.records.Contains("adventurer"));
                break;
            case "guild":
                Add(actions, "accept", "배송 확인 의뢰를 받는다", !Has("accepted"));
                Add(actions, "go_yard", "배달원을 만나러 간다", Has("accepted"));
                Add(actions, "go_alley", "여관 뒤편을 먼저 조사한다", Has("accepted"));
                Add(actions, "go_room", "여관 방으로 돌아간다", !Has("recovered"));
                break;
            case "yard":
                Add(actions, "interview", "어디에 배달했는지 묻는다", !Progress.records.Contains("testimony"));
                Add(actions, "slip", "배달표를 보여 달라고 한다", !Progress.records.Contains("delivery_slip"));
                Add(actions, "go_alley", "여관 뒤편을 조사한다");
                Add(actions, "go_guild", "길드로 돌아간다");
                break;
            case "alley":
                Add(actions, "inspect_doors", "출입문과 주변을 살핀다", !Progress.records.Contains("doors"));
                Add(actions, "go_yard", "배달원에게 돌아간다");
                Add(actions, "go_store", "확인한 창고에 들어간다", Has("door_identified"));
                Add(actions, "go_guild", "길드로 간다");
                break;
            case "store":
                Add(actions, "inspect_crate", "문 옆 상자의 표식을 확인한다", !Progress.records.Contains("crate"));
                Add(actions, "take_unproven", "상자를 가져가겠다고 한다", !Has("recovered"));
                Add(actions, "go_alley", "골목으로 나온다");
                break;
            case "inn":
                Add(actions, "report", "식재료와 확인한 내용을 전달한다", !Has("reported"));
                Add(actions, "sleep", "방으로 올라가 잠든다", Has("reported"));
                break;
        }
        if (Has("recovered") && Progress.location != "inn" && Progress.location != "complete")
            Add(actions, "go_inn", "물건을 들고 여관으로 돌아간다");
        return actions;
    }

    private static void Add(List<TutorialAction> actions, string id, string label, bool visible = true)
    {
        if (visible) actions.Add(new TutorialAction(id, label));
    }

    private bool Has(string flag) { return Progress.flags.Contains(flag); }
    private void Collect(string id) { Progress.records.Add(id); }

    public bool Perform(string id)
    {
        if (!Actions().Any(action => action.id == id)) return false;
        string text;
        switch (id)
        {
            case "breakfast":
                Progress.flags.Add("ate");
                text = "그녀가 쟁반을 내려놓는다.\n\"그릇은 내려올 때 가져다줘. 오늘도 길드 가?\"\n식사를 마친다. 허기는 장면으로만 표현하며 별도 스탯은 아직 없다."; break;
            case "ask_order": Collect("order"); text = "\"길드 갈 거면 이것 좀 물어봐 줘. 두 상자 시켰는데 하나만 왔어.\"\n그녀에게 주문서를 받는다.\n[안내] 출처가 있는 정보는 오른쪽 기록장에 남는다."; break;
            case "inspect_room": Collect("adventurer"); text = "가방에서 길드 등록증을 찾는다. 자기 이름이 적혀 있다. 검과 여행용 짐을 챙긴다.\n[안내] 물건을 조사하면 이동이나 대화에 쓸 정보를 얻을 수 있다."; break;
            case "accept":
                Progress.flags.Add("accepted"); Collect("order");
                text = "\"여관 배송 확인 건이 하나 있어요. 빠진 상자가 어디 있는지 확인해 주세요.\"\n의뢰를 받는다. 주문서 사본에는 주문 17, 두 상자라고 적혀 있다."; break;
            case "interview": Collect("testimony"); text = "\"두 개 다 받았고, 여관 뒤에 내려놨어. 하나씩 따로 옮겼지.\"\n배달원의 말을 기록한다.\n[안내] 증언은 그 사람이 한 말이며, 확인된 사실과는 다르다."; break;
            case "slip": Collect("delivery_slip"); text = "배달원이 접어 둔 종이를 편다. 주문 17, 두 상자.\n\"상자에도 번호가 붙어 있어.\"\n배달표를 기록한다."; break;
            case "inspect_doors": Collect("doors"); text = "여관 부엌문 옆에 비슷한 문이 하나 더 있다. 옆 가게의 창고다.\n[안내] 기록 두 개를 선택해 연결해 보자. 일치뿐 아니라, 착오의 가능성도 근거가 된다."; break;
            case "inspect_crate": Collect("crate"); text = "문 옆의 식재료 상자에 17이라는 표식이 있다.\n가게 주인이 묻는다.\n\"찾는 물건이야? 누구 건데?\"\n상자를 가져가려면 주문과 연결되는 근거가 필요하다."; break;
            case "take_unproven": text = "\"잠깐. 네 물건이라는 건 확인하고 가져가.\"\n상자를 옮기지 않는다. 기록장에서 표식과 배송 기록을 대조할 수 있다."; break;
            case "report":
                Progress.flags.Add("reported");
                text = "\"옆집에 있었어? 어쩐지.\"\n그녀가 상자를 받아 부엌에 둔다.\n" + (Has("ate") ? "\"아침 그릇도 챙겼지? 저녁은 여기 둘게.\"" : "\"아침도 안 먹고 나갔잖아. 앉아, 밥 줄게.\"") + "\n의뢰를 마쳤다. 배달원은 두 상자를 옮겼지만 한 상자는 다른 문 안에 있었다."; break;
            case "sleep": Progress.location = "complete"; Progress.flags.Add("completed"); text = "방으로 돌아와 장비를 내려놓고 눕는다.\n\n튜토리얼 완료\n질문 → 기록 수집 → 정보 연결 → 근거 제시를 경험했다.\n\n이번 버전은 여기까지다. 월드 삭제 및 첫 적대 조우는 아직 실행하지 않는다."; break;
            default:
                Progress.location = id.Substring(3);
                text = ArrivalText(); break;
        }
        Say(text, Actor);
        return true;
    }

    private string ArrivalText()
    {
        switch (Progress.location)
        {
            case "room": return "여관 방으로 돌아온다. 창가에 검집을 내려놓는다.";
            case "guild": return "길드 접수처에 들어선다. 접수원이 의뢰 목록에서 고개를 든다.";
            case "yard": return "배달원이 빈 수레를 정리하고 있다.\n\"여관에서 왔어? 무슨 일인데?\"";
            case "alley": return "여관 뒤편으로 돌아온다. 부엌에서 그릇 부딪치는 소리가 들린다.";
            case "store": return "배달원이 가리킨 문을 두드린다. 가게 주인이 안으로 들여보낸다. 문 옆에 상자 하나가 놓여 있다.";
            case "inn": return "여관으로 돌아온다. 그녀가 부엌에서 나와 들고 온 물건을 본다.";
            default: return "주변을 살핀다.";
        }
    }

    public bool Connect(string first, string second)
    {
        if (first == second || !Progress.records.Contains(first) || !Progress.records.Contains(second))
        {
            Progress.notice = "수집한 서로 다른 기록 두 개를 선택하세요."; return false;
        }
        var pair = new HashSet<string> { first, second };
        string tool = pair.SetEquals(new[] { "testimony", "doors" }) ? Misdelivery
            : pair.SetEquals(new[] { "crate", "delivery_slip" }) ? Ownership : null;
        if (tool == null)
        {
            Progress.notice = "현재 기록만으로는 새로운 활용 수단을 만들 근거가 부족합니다. 불이익은 없습니다."; return false;
        }
        bool added = Progress.tools.Add(tool);
        Progress.notice = added ? "활용 수단 등록: " + ToolTitle(tool) : "이미 등록한 활용 수단입니다.";
        return true;
    }

    public bool Present(string tool)
    {
        if (!Progress.tools.Contains(tool)) { Progress.notice = "먼저 기록을 연결해 활용 수단을 등록하세요."; return false; }
        if (tool == Misdelivery && Progress.location == "yard" && !Has("door_identified"))
        {
            Progress.flags.Add("door_identified");
            Say("문이 두 개였다는 점을 짚어 묻는다. 배달원이 골목으로 함께 간다.\n\"여기였는데…… 이쪽이 여관 문이 아니야?\"\n옆 가게 창고를 확인할 수 있게 됐다.", "배달원");
            return true;
        }
        if (tool == Ownership && Progress.location == "store" && !Has("recovered"))
        {
            Progress.flags.Add("recovered");
            Say("상자의 표식과 배달표를 함께 보여준다.\n\"17이면 여관 거네. 가져가. 내 물건도 아니라 어디서 왔나 했어.\"\n식재료 상자를 돌려받는다.", "가게 주인");
            return true;
        }
        Progress.notice = "지금 상대에게서 이 근거로 새로 확인할 내용은 없습니다. 배달원이나 가게 주인에게 제시해 보세요.";
        return false;
    }

    public void Say(string text, string speaker)
    {
        Progress.lastText = text;
        Progress.notice = null;
        Progress.transcript.Add(new TutorialMessage { speaker = speaker, text = text, location = Progress.location });
        if (Progress.transcript.Count > 60) Progress.transcript.RemoveRange(0, Progress.transcript.Count - 60);
    }

    public static string ToolTitle(string id)
    {
        return id == Misdelivery ? "다른 창고에 배달했을 가능성" : id == Ownership ? "상자와 주문의 일치" : id;
    }

    public static string ToolDescription(string id)
    {
        return id == Misdelivery ? "배달원의 진술 + 뒤편의 두 출입문. 착오 가능성이지 확정된 사실은 아니다. 배달원에게 제시한다."
            : "상자 표식 + 배달표의 주문 번호가 일치한다. 가게 주인에게 소유권 확인 근거로 제시한다.";
    }
}
