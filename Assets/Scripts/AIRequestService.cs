using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class ChatMessage
{
    public string role;    // "user" 또는 "assistant"
    public string content;
}

public class AIRequestService : MonoBehaviour
{
    private const string API_KEY_ENVIRONMENT_VARIABLE = "ANTHROPIC_API_KEY";
    private const string ENDPOINT = "https://api.anthropic.com/v1/messages";

    private const string SCHEMA = @"{
        ""type"": ""object"",
        ""properties"": {
            ""backgroundId"": { ""type"": ""string"", ""enum"": [""tavern_night"", ""forest_day"", ""ruins_dusk""] },
            ""speaker"": { ""type"": ""string"" },
            ""dialogueText"": { ""type"": ""string"" },
            ""emotion"": { ""type"": ""string"", ""enum"": [""neutral"", ""angry"", ""confused"", ""sad"", ""fear""] },
            ""loopCount"": { ""type"": ""integer"" },
            ""choices"": {
                ""type"": ""array"",
                ""items"": { ""type"": ""string"" }
            }
        },
        ""required"": [""backgroundId"", ""speaker"", ""dialogueText"", ""emotion"", ""loopCount"", ""choices""],
        ""additionalProperties"": false
    }";

    public IEnumerator SendConversation(string systemPrompt, List<ChatMessage> history, Action<string> onResult)
    {
#if !UNITY_EDITOR
        Debug.LogError("AI direct requests are available only in the Unity Editor. Configure a server proxy before distributing a build.");
        yield break;
#endif
        string apiKey = Environment.GetEnvironmentVariable(API_KEY_ENVIRONMENT_VARIABLE);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Debug.LogError("Set ANTHROPIC_API_KEY in your local environment and restart Unity Hub and the Unity Editor.");
            yield break;
        }

        string messagesJson = BuildMessagesJson(history);

        string body = $@"{{
            ""model"": ""claude-sonnet-4-6"",
            ""max_tokens"": 500,
            ""system"": ""{EscapeJson(systemPrompt)}"",
            ""messages"": {messagesJson},
            ""output_config"": {{
                ""format"": {{
                    ""type"": ""json_schema"",
                    ""schema"": {SCHEMA}
                }}
            }}
        }}";

        var request = new UnityWebRequest(ENDPOINT, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("x-api-key", apiKey);
        request.SetRequestHeader("anthropic-version", "2023-06-01");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            onResult?.Invoke(request.downloadHandler.text);
        else
            Debug.LogError($"API Error: {request.error} / {request.downloadHandler.text}");
    }

    private string BuildMessagesJson(List<ChatMessage> history)
    {
        var sb = new StringBuilder("[");
        for (int i = 0; i < history.Count; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append($@"{{ ""role"": ""{history[i].role}"", ""content"": ""{EscapeJson(history[i].content)}"" }}");
        }
        sb.Append("]");
        return sb.ToString();
    }

    private string EscapeJson(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n")
            .Replace("\r", "\\n");
    }
}
