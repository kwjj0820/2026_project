using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

// Standalone runner: isolates persistence from the player's actual save directory.
public class ChatMessage { public string role; public string content; }
namespace UnityEngine
{
    public static class Application { public static string persistentDataPath; }
    public static class Debug
    {
        public static void LogWarning(object message) { Console.WriteLine(message); }
        public static void LogError(object message) { throw new Exception(message.ToString()); }
    }
}

public static class ActOneVerification
{
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        Console.WriteLine("PASS: " + name);
    }

    private static void Select(ActOneStory story, string id)
    {
        Check(story.Select(story.State.currentEventId, id), "choice " + id);
    }

    public static void Main()
    {
        ApplicationPath();
        var story = new ActOneStory();
        Check(!story.Select("tavern_opening", "hide_weapon"), "locked choice rejected");
        Check(!story.Select("tavern_opening", "invalid"), "unknown choice rejected");
        Select(story, "ask");
        Check(!story.Select("tavern_opening", "ask"), "stale double click rejected");
        Select(story, "remember");
        var beforeDeath = SaveSystem.Deserialize(JsonConvert.SerializeObject(new SaveData { gameState = story.State }));
        story = new ActOneStory(beforeDeath.gameState);
        Check(story.Current.id == "loop_death" && story.State.loopCount == 1, "death screen restored without increment");
        story.State.currentLoop.flags["temporary"] = true;
        Select(story, "next_loop");
        Check(story.State.loopCount == 2 && story.State.currentLoop.flags.Count == 0, "loop resets temporary state");
        Check(story.State.memory.clueIds.Contains(ActOneStory.IdentityClue), "clues survive death");
        Check(story.AvailableChoices().Exists(c => c.id == "hide_weapon"), "new choice unlocked");
        Select(story, "hide_weapon");
        Select(story, "quote");
        Select(story, "previous");
        Select(story, "negotiate");
        Select(story, "leave");
        Check(story.Current.id == "act_one_end" && story.AvailableChoices().Count == 0, "act ends without paid API");
        Check(story.State.memory.knowledgeIds.Contains("act_one_completed"), "completion persists");
        SaveSystem.Save(new SaveData { gameState = story.State, lastScene = story.CreateScene() }, "act_one_save.json");
        var restored = new ActOneStory(SaveSystem.Load("act_one_save.json").gameState);
        Check(restored.Current.id == "act_one_end", "ending saved and restored");
        Check(!File.Exists(Path.Combine(UnityEngine.Application.persistentDataPath, "save.json")), "AI save untouched");
        var alternate = new ActOneStory();
        Select(alternate, "run"); Select(alternate, "push"); Select(alternate, "next_loop");
        Select(alternate, "hide_weapon"); Select(alternate, "question"); Select(alternate, "negotiate"); Select(alternate, "leave");
        Check(alternate.Current.id == "act_one_end", "escape route remains completable");

        // Explore reachable states; verifies all exposed IDs/targets and bounded-loop routes.
        var queue = new Queue<GameState>();
        var visited = new HashSet<string>();
        queue.Enqueue(new GameState());
        while (queue.Count > 0)
        {
            GameState state = queue.Dequeue();
            var candidate = new ActOneStory(state);
            string key = state.currentEventId + ":" + state.loopCount + ":" + string.Join(",", Sorted(state.memory.clueIds));
            if (!visited.Add(key) || state.loopCount > 3) continue;
            var scene = candidate.CreateScene();
            Check(scene.choices.Count == scene.choiceIds.Count, "labels match IDs: " + state.currentEventId);
            foreach (var choice in candidate.AvailableChoices())
            {
                GameState copy = JsonConvert.DeserializeObject<GameState>(JsonConvert.SerializeObject(state));
                var branch = new ActOneStory(copy);
                branch.Select(state.currentEventId, choice.id);
                queue.Enqueue(branch.State);
            }
        }
        Console.WriteLine("PASS: explored " + visited.Count + " reachable states");
    }

    private static List<string> Sorted(HashSet<string> values)
    {
        var result = new List<string>(values); result.Sort(); return result;
    }

    private static void ApplicationPath()
    {
        UnityEngine.Application.persistentDataPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "act-one-test-" + Guid.NewGuid());
    }
}
