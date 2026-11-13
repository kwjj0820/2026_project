using System;
using System.Collections.Generic;

[Serializable]
public class GameState
{
    public int loopCount = 1;
    public string currentEventId = "tavern_opening";
    public LoopState currentLoop = new LoopState();
    public PersistentMemory memory = new PersistentMemory();

    public void Normalize()
    {
        loopCount = Math.Max(1, loopCount);
        currentEventId = string.IsNullOrWhiteSpace(currentEventId) ? "tavern_opening" : currentEventId;
        currentLoop = currentLoop ?? new LoopState();
        memory = memory ?? new PersistentMemory();
        currentLoop.flags = currentLoop.flags ?? new Dictionary<string, bool>();
        currentLoop.relationships = currentLoop.relationships ?? new Dictionary<string, int>();
        memory.clueIds = memory.clueIds ?? new HashSet<string>();
        memory.knowledgeIds = memory.knowledgeIds ?? new HashSet<string>();
        memory.pursuerMemories = memory.pursuerMemories ?? new Dictionary<string, HashSet<string>>();
    }

    public bool DiscoverClue(string clueId)
    {
        if (string.IsNullOrWhiteSpace(clueId))
            throw new ArgumentException("A clue ID is required.", nameof(clueId));
        Normalize();
        return memory.clueIds.Add(clueId);
    }

    public void BeginNextLoop()
    {
        Normalize();
        loopCount++;
        currentEventId = "tavern_opening";
        currentLoop = new LoopState();
    }
}

[Serializable]
public class LoopState
{
    public string locationId = "tavern_night";
    public Dictionary<string, bool> flags = new Dictionary<string, bool>();
    public Dictionary<string, int> relationships = new Dictionary<string, int>();
}

[Serializable]
public class PersistentMemory
{
    public HashSet<string> clueIds = new HashSet<string>();
    public HashSet<string> knowledgeIds = new HashSet<string>();
    public Dictionary<string, HashSet<string>> pursuerMemories = new Dictionary<string, HashSet<string>>();
}
