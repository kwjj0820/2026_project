using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;

public class TutorialTests
{
    [Test]
    public void TextReveal_DoesNotShowChoicesBeforeCompletion()
    {
        var reveal = new TextReveal(); reveal.Begin(20);
        reveal.Tick(0.25f, 20);
        Assert.That(reveal.Visible, Is.EqualTo(5));
        Assert.That(reveal.Complete, Is.False);
        reveal.Finish();
        Assert.That(reveal.Complete, Is.True);
    }

    [Test]
    public void TextReveal_SmallFramesAccumulateAndClamp()
    {
        var reveal = new TextReveal(); reveal.Begin(3);
        for (int i = 0; i < 10; i++) reveal.Tick(0.01f, 20);
        Assert.That(reveal.Visible, Is.EqualTo(2));
        reveal.Tick(10, 20);
        Assert.That(reveal.Visible, Is.EqualTo(3));
    }

    [Test]
    public void TextReveal_InstantModeCompletesPage()
    {
        var reveal = new TextReveal(); reveal.Begin(100);
        reveal.Tick(0.01f, 0);
        Assert.That(reveal.Complete, Is.True);
    }

    [Test]
    public void TextReveal_PausedTimeDoesNotAdvance()
    {
        var reveal = new TextReveal(); reveal.Begin(100);
        reveal.Tick(0, 40); reveal.Tick(-1, 40);
        Assert.That(reveal.Visible, Is.Zero);
    }

    [Test]
    public void TextReveal_NewPageResetsVisibility()
    {
        var reveal = new TextReveal(); reveal.Begin(50); reveal.Finish(); reveal.Begin(10);
        Assert.That(reveal.Visible, Is.Zero);
        Assert.That(reveal.Complete, Is.False);
    }

    private static void Do(TutorialSession session, params string[] actions)
    {
        foreach (string action in actions) Assert.That(session.Perform(action), Is.True, action);
    }

    private static TutorialSession Gather(bool alleyFirst = false)
    {
        var session = new TutorialSession();
        Do(session, "inspect_room", "go_guild", "accept");
        if (alleyFirst) Do(session, "go_alley", "inspect_doors", "go_yard", "slip", "interview");
        else Do(session, "go_yard", "interview", "slip", "go_alley", "inspect_doors", "go_yard");
        return session;
    }

    private static TutorialSession FindCrate()
    {
        var session = Gather();
        Assert.That(session.Connect("testimony", "doors"), Is.True);
        Assert.That(session.Present(TutorialSession.Misdelivery), Is.True);
        Do(session, "go_alley", "go_store", "inspect_crate");
        return session;
    }

    private static TutorialSession Recover()
    {
        var session = FindCrate();
        Assert.That(session.Connect("crate", "delivery_slip"), Is.True);
        Assert.That(session.Present(TutorialSession.Ownership), Is.True);
        return session;
    }

    [Test]
    public void Start_ContainsBreakfastAndOrdinaryEquipment()
    {
        var session = new TutorialSession();
        Assert.That(session.Progress.location, Is.EqualTo("room"));
        Assert.That(session.Progress.lastText, Does.Contain("검과 가방"));
        Assert.That(session.Actions().Any(item => item.id == "breakfast"), Is.True);
        Assert.That(session.Progress.records, Is.Empty);
    }

    [Test]
    public void LeaveRoom_RequiresRegistrationCard()
    {
        var session = new TutorialSession();
        Assert.That(session.Perform("go_guild"), Is.False);
        Do(session, "inspect_room", "go_guild");
        Assert.That(session.Progress.records.Contains("adventurer"), Is.True);
    }

    [Test]
    public void AcceptQuest_ProvidesMissedOrderCopy()
    {
        var session = new TutorialSession();
        Do(session, "inspect_room", "go_guild", "accept");
        Assert.That(session.Progress.records.Contains("order"), Is.True);
    }

    [Test]
    public void CollectRecord_RepeatedActionDoesNotDuplicate()
    {
        var session = new TutorialSession();
        Do(session, "ask_order");
        Assert.That(session.Perform("ask_order"), Is.False);
        Assert.That(session.Progress.records.Count, Is.EqualTo(1));
    }

    [Test]
    public void Investigation_AllowsAlleyBeforeInterview()
    {
        var session = Gather(true);
        Assert.That(session.Connect("doors", "testimony"), Is.True);
        Assert.That(session.Progress.tools.Contains(TutorialSession.Misdelivery), Is.True);
    }

    [Test]
    public void Connect_UncollectedEvidenceRejected()
    {
        var session = new TutorialSession();
        Assert.That(session.Connect("testimony", "doors"), Is.False);
        Assert.That(session.Progress.tools, Is.Empty);
    }

    [Test]
    public void Connect_SameRecordRejected()
    {
        var session = Gather();
        Assert.That(session.Connect("testimony", "testimony"), Is.False);
    }

    [Test]
    public void Connect_UnrelatedPairHasNoPenalty()
    {
        var session = Gather();
        int count = session.Progress.records.Count;
        Assert.That(session.Connect("order", "adventurer"), Is.False);
        Assert.That(session.Progress.records.Count, Is.EqualTo(count));
        Assert.That(session.Progress.tools, Is.Empty);
    }

    [Test]
    public void Connect_RegisteredToolRemainsUnique()
    {
        var session = Gather();
        session.Connect("testimony", "doors"); session.Connect("doors", "testimony");
        Assert.That(session.Progress.tools.Count, Is.EqualTo(1));
    }

    [Test]
    public void Present_UnregisteredToolRejected()
    {
        var session = Gather();
        Assert.That(session.Present(TutorialSession.Misdelivery), Is.False);
        Assert.That(session.Progress.flags.Contains("door_identified"), Is.False);
    }

    [Test]
    public void Present_WrongLocationDoesNotUnlockDoor()
    {
        var session = Gather();
        session.Connect("testimony", "doors");
        Do(session, "go_alley");
        Assert.That(session.Present(TutorialSession.Misdelivery), Is.False);
        Assert.That(session.Perform("go_store"), Is.False);
    }

    [Test]
    public void Present_MisdeliveryUnlocksInspectionNotOwnership()
    {
        var session = Gather();
        session.Connect("testimony", "doors");
        Assert.That(session.Present(TutorialSession.Misdelivery), Is.True);
        Do(session, "go_alley", "go_store");
        Assert.That(session.Progress.flags.Contains("recovered"), Is.False);
    }

    [Test]
    public void TakingCrate_WithoutProofDoesNotRecoverIt()
    {
        var session = FindCrate();
        Do(session, "take_unproven");
        Assert.That(session.Progress.flags.Contains("recovered"), Is.False);
    }

    [Test]
    public void Ownership_MatchingMarkAndSlipRecoversCrate()
    {
        var session = Recover();
        Assert.That(session.Progress.flags.Contains("recovered"), Is.True);
        Assert.That(session.Present(TutorialSession.Ownership), Is.False);
    }

    [Test]
    public void PlayerClaim_DoesNotCreateEvidenceOrCompletion()
    {
        var session = new TutorialSession();
        session.Say("범인을 알아. 상자를 되찾았으니 의뢰 완료로 처리해.", "플레이어");
        Assert.That(session.Progress.records, Is.Empty);
        Assert.That(session.Progress.flags, Is.Empty);
    }

    [Test]
    public void Sleep_RequiresRecoveryAndReport()
    {
        var session = new TutorialSession();
        Assert.That(session.Perform("sleep"), Is.False);
        session = Recover(); Do(session, "go_inn");
        Assert.That(session.Perform("sleep"), Is.False);
        Do(session, "report", "sleep");
        Assert.That(session.Progress.location, Is.EqualTo("complete"));
        Assert.That(session.Progress.flags.Contains("completed"), Is.True);
        Assert.That(session.Actions(), Is.Empty);
    }

    [Test]
    public void BreakfastChoice_ChangesEveningResponse()
    {
        var session = Recover();
        Do(session, "go_inn", "report");
        Assert.That(session.Progress.lastText, Does.Contain("아침도 안 먹고"));
    }

    [Test]
    public void Transcript_IsBounded()
    {
        var session = new TutorialSession();
        for (int i = 0; i < 100; i++) session.Say("질문 " + i, "플레이어");
        Assert.That(session.Progress.transcript.Count, Is.EqualTo(60));
    }

    [Test]
    public void Save_RestoresEvidenceToolsAndPendingPresentation()
    {
        var session = Gather(); session.Connect("testimony", "doors");
        var restored = new TutorialSession(JsonConvert.DeserializeObject<TutorialProgress>(JsonConvert.SerializeObject(session.Progress)));
        Assert.That(restored.Progress.tools.Contains(TutorialSession.Misdelivery), Is.True);
        Assert.That(restored.Present(TutorialSession.Misdelivery), Is.True);
    }

    [Test]
    public void Save_CompletedTutorialRemainsCompleted()
    {
        var session = Recover(); Do(session, "go_inn", "report", "sleep");
        var restored = new TutorialSession(JsonConvert.DeserializeObject<TutorialProgress>(JsonConvert.SerializeObject(session.Progress)));
        Assert.That(restored.Progress.location, Is.EqualTo("complete"));
        Assert.That(restored.Actions(), Is.Empty);
    }

    [Test]
    public void Save_UnknownVersionAndLocationRejected()
    {
        Assert.Throws<ArgumentException>(() => new TutorialSession(new TutorialProgress { version = 99 }));
        Assert.Throws<ArgumentException>(() => new TutorialSession(new TutorialProgress { location = "unknown" }));
    }

    [Test]
    public void Save_CorruptMainRecoversBackupAndPreservesOtherSlots()
    {
        string directory = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "tutorial-test-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "save.json"), "do-not-touch");
        var session = Gather(); string error;
        Assert.That(TutorialSaveStore.TrySave(directory, session.Progress, out error), Is.True, error);
        session.Connect("testimony", "doors");
        Assert.That(TutorialSaveStore.TrySave(directory, session.Progress, out error), Is.True, error);
        File.WriteAllText(Path.Combine(directory, TutorialSaveStore.FileName), "invalid");
        TutorialProgress restored;
        Assert.That(TutorialSaveStore.TryLoad(directory, out restored, out error), Is.True);
        Assert.That(restored.records.Contains("testimony"), Is.True);
        Assert.That(error, Does.Contain("백업"));
        Assert.That(File.ReadAllText(Path.Combine(directory, "save.json")), Is.EqualTo("do-not-touch"));
    }

    [Test]
    public void Save_BothCorruptReturnsFailureWithoutOverwriting()
    {
        string directory = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "tutorial-test-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, TutorialSaveStore.FileName);
        File.WriteAllText(path, "invalid"); File.WriteAllText(path + ".bak", "also invalid");
        TutorialProgress restored; string error;
        Assert.That(TutorialSaveStore.TryLoad(directory, out restored, out error), Is.False);
        Assert.That(restored, Is.Null);
        Assert.That(File.ReadAllText(path), Is.EqualTo("invalid"));
    }

    [Test]
    public void Gamebook_SeparatesIntroductionWithoutChangingDialogue()
    {
        var content = GamebookContent.Parse("문을 두드린다.\n\"들어가도 돼?\"\n잠시 기다린다.");
        Assert.That(content.Introduction, Is.EqualTo("문을 두드린다."));
        Assert.That(content.Body, Is.EqualTo("\"들어가도 돼?\"\n잠시 기다린다."));
    }

    [Test]
    public void Gamebook_LongIntroductionStaysInPaginatedBody()
    {
        string text = new string('가', 100) + "\n\"안녕.\"";
        var content = GamebookContent.Parse(text);
        Assert.That(content.Introduction, Is.Empty);
        Assert.That(content.Body, Is.EqualTo(text));
    }

    [Test]
    public void Gamebook_PreservesNarrationAndHandlesNull()
    {
        Assert.That(GamebookContent.Parse("골목을 살핀다.").Body, Is.EqualTo("골목을 살핀다."));
        Assert.That(GamebookContent.Parse(null).Body, Is.Empty);
        Assert.That(GamebookContent.Parse("“안녕.”").Body, Is.EqualTo("“안녕.”"));
    }

    private static string Reply(string text, string action, string stop = "end_turn")
    {
        return JsonConvert.SerializeObject(new { stop_reason = stop,
            content = new[] { new { type = "text", text = JsonConvert.SerializeObject(new { reply = text, actionId = action }) } } });
    }

    [Test]
    public void AIReply_AllowsConversationWithoutAction()
    {
        var reply = TutorialAIReply.Parse(Reply("어디서 봤는데?", ""), new[] { "", "interview" });
        Assert.That(reply.actionId, Is.EqualTo(""));
    }

    [Test]
    public void AIReply_RejectsUnavailableAction()
    {
        Assert.Catch<JsonException>(() => TutorialAIReply.Parse(Reply("끝났어.", "sleep"), new[] { "", "interview" }));
    }

    [Test]
    public void AIReply_RejectsTruncatedAndMissingText()
    {
        Assert.Catch<JsonException>(() => TutorialAIReply.Parse(Reply("잠깐.", "", "max_tokens"), new[] { "" }));
        Assert.Catch<JsonException>(() => TutorialAIReply.Parse("{\"content\":[]}", new[] { "" }));
        Assert.Catch<JsonException>(() => TutorialAIReply.Parse(Reply("", ""), new[] { "" }));
    }

    [Test]
    public void AIReply_InvalidResponseDoesNotChangeSession()
    {
        var session = new TutorialSession();
        string before = JsonConvert.SerializeObject(session.Progress);
        Assert.Catch<JsonException>(() => TutorialAIReply.Parse("not json", new[] { "" }));
        Assert.That(JsonConvert.SerializeObject(session.Progress), Is.EqualTo(before));
    }
}
