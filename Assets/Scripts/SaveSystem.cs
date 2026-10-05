using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static void Save(SaveData data)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveSystem] 저장 완료: {SavePath}");
    }

    public static SaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[SaveSystem] 저장 파일 없음, 새 게임 시작");
            return null;
        }

        string json = File.ReadAllText(SavePath);
        SaveData data = JsonConvert.DeserializeObject<SaveData>(json);
        Debug.Log($"[SaveSystem] 불러오기 완료, 루프 회차: {data.loopCount}");
        return data;
    }

    public static bool HasSaveFile()
    {
        return File.Exists(SavePath);
    }
}