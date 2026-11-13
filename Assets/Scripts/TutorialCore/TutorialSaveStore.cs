using System;
using System.IO;
using Newtonsoft.Json;

public static class TutorialSaveStore
{
    public const string FileName = "tutorial_save.json";

    // The caller provides a directory so tests never touch real player saves.
    public static bool TryLoad(string directory, out TutorialProgress progress, out string error)
    {
        progress = null;
        error = null;
        string path = Path.Combine(directory, FileName);
        foreach (string candidate in new[] { path, path + ".bak" })
        {
            if (!File.Exists(candidate)) continue;
            try
            {
                var loaded = JsonConvert.DeserializeObject<TutorialProgress>(File.ReadAllText(candidate));
                if (loaded == null) throw new JsonSerializationException("저장 내용이 비어 있습니다.");
                var validated = new TutorialSession(loaded);
                progress = validated.Progress;
                error = candidate.EndsWith(".bak") ? "주 저장을 읽지 못해 백업에서 복원했습니다." : null;
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                || exception is JsonException || exception is ArgumentException)
            {
                error = "저장을 읽지 못했습니다: " + exception.Message;
            }
        }
        return error == null;
    }

    public static bool TrySave(string directory, TutorialProgress progress, out string error)
    {
        error = null;
        try
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, FileName);
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonConvert.SerializeObject(progress, Formatting.Indented));
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is JsonException)
        {
            error = "저장 실패: " + exception.Message;
            return false;
        }
    }
}
