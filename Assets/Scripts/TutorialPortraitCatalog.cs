using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Narrative/Tutorial Portrait Catalog")]
public class TutorialPortraitCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string speaker;
        public Sprite portrait;
    }

    [SerializeField] private Entry[] entries = new Entry[0];

    public Sprite Find(string speaker)
    {
        foreach (Entry entry in entries)
            if (entry != null && entry.speaker == speaker) return entry.portrait;
        return null;
    }
}
