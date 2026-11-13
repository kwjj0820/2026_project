using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

public static class SaveSystem
{
    private static string GetSavePath(string fileName)
    {
        if (Path.GetFileName(fileName) != fileName || string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Save filename must not contain a directory.");
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    public static void Save(SaveData data, string fileName = "save.json")
    {
        string SavePath = GetSavePath(fileName);
        try
        {
            data.gameState.Normalize();
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            Directory.CreateDirectory(Application.persistentDataPath);
            string temporaryPath = SavePath + ".tmp";
            File.WriteAllText(temporaryPath, json);
            if (File.Exists(SavePath))
                File.Replace(temporaryPath, SavePath, SavePath + ".bak");
            else
                File.Move(temporaryPath, SavePath);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is JsonException)
        {
            Debug.LogError($"[SaveSystem] 저장 실패: {exception.Message}");
        }
    }

    public static SaveData Load(string fileName = "save.json")
    {
        string SavePath = GetSavePath(fileName);
        foreach (string path in new[] { SavePath, SavePath + ".bak" })
        {
            if (!File.Exists(path))
                continue;
            try
            {
                return Deserialize(File.ReadAllText(path));
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is JsonException)
            {
                Debug.LogWarning($"[SaveSystem] 불러오기 실패 ({Path.GetFileName(path)}): {exception.Message}");
            }
        }
        return null;
    }

    public static SaveData Deserialize(string json)
    {
        JObject source = JObject.Parse(json);
        int version = source.Value<int?>("saveVersion") ?? 1;
        if (version < 1 || version > SaveData.CurrentVersion)
            throw new JsonSerializationException($"Unsupported save version: {version}");
        SaveData data = source.ToObject<SaveData>();
        data.gameState = data.gameState ?? new GameState();
        if (version == 1)
            data.gameState.loopCount = source.Value<int?>("loopCount") ?? 1;
        data.gameState.Normalize();
        data.conversationHistory = data.conversationHistory ?? new System.Collections.Generic.List<ChatMessage>();
        if (data.lastScene == null)
        {
            for (int i = data.conversationHistory.Count - 1; i >= 0; i--)
            {
                ChatMessage message = data.conversationHistory[i];
                if (message == null || message.role != "assistant")
                    continue;
                try
                {
                    data.lastScene = JsonConvert.DeserializeObject<SceneDirective>(message.content);
                    if (data.lastScene != null)
                        break;
                }
                catch (JsonException) { }
            }
        }
        if (data.lastScene != null)
            data.lastScene.loopCount = data.gameState.loopCount;
        data.saveVersion = SaveData.CurrentVersion;
        return data;
    }

    public static bool HasSaveFile()
    {
        string path = GetSavePath("save.json");
        return File.Exists(path) || File.Exists(path + ".bak");
    }
}
