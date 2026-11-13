using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public const int CurrentVersion = 2;
    public int saveVersion = CurrentVersion;
    public GameState gameState = new GameState();
    public SceneDirective lastScene;
    public List<ChatMessage> conversationHistory = new List<ChatMessage>();
}
