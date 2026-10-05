using System;
using System.Collections.Generic;

[Serializable]
public class SceneDirective
{
    public string backgroundId;
    public string speaker;
    public string dialogueText;
    public string emotion;
    public int loopCount;
    public List<string> choices;
}