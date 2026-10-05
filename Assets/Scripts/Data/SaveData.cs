using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int loopCount;
    public List<ChatMessage> conversationHistory;
}