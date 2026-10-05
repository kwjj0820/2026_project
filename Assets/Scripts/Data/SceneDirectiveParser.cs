using Newtonsoft.Json.Linq;

public static class SceneDirectiveParser
{
    public static SceneDirective Parse(string rawApiResponse)
    {
        string innerJson = ExtractRawText(rawApiResponse);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<SceneDirective>(innerJson);
    }

    // 추가된 함수
    public static string ExtractRawText(string rawApiResponse)
    {
        var outer = JObject.Parse(rawApiResponse);
        return outer["content"][0]["text"].ToString();
    }
}