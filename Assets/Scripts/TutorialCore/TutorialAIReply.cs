using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class TutorialAIReply
{
    public string reply;
    public string actionId;

    public static TutorialAIReply Parse(string raw, IEnumerable<string> allowedActions)
    {
        JObject response = JObject.Parse(raw);
        if (response.Value<string>("stop_reason") == "max_tokens")
            throw new JsonException("응답이 도중에 잘렸습니다.");
        var content = response["content"] as JArray;
        JObject textBlock = content == null ? null : content.OfType<JObject>().FirstOrDefault(item => item.Value<string>("type") == "text");
        JObject result = JObject.Parse(textBlock?.Value<string>("text") ?? "");
        var reply = new TutorialAIReply { reply = result.Value<string>("reply"), actionId = result.Value<string>("actionId") };
        if (string.IsNullOrWhiteSpace(reply.reply) || reply.reply.Length > 1200 || reply.actionId == null
            || !allowedActions.Contains(reply.actionId))
            throw new JsonException("응답 형식이나 현재 행동 범위를 확인할 수 없습니다.");
        return reply;
    }
}
