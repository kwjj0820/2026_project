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
        return SendStructuredConversation(systemPrompt, history, SCHEMA, onResult,
            error => Debug.LogError("[AIRequestService] " + error));
    }

    public IEnumerator SendStructuredConversation(string systemPrompt, List<ChatMessage> history,
        string schema, Action<string> onResult, Action<string> onError)
    {
#if !UNITY_EDITOR
        onError?.Invoke("AI 직접 호출은 Unity Editor에서만 사용할 수 있습니다.");
        yield break;
#else
        string apiKey = Environment.GetEnvironmentVariable(API_KEY_ENVIRONMENT_VARIABLE);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            onError?.Invoke("ANTHROPIC_API_KEY를 설정한 뒤 Unity Hub와 Editor를 다시 시작하세요.");
            yield break;
        }

        string messagesJson = BuildMessagesJson(history);

        string body = Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            model = "claude-sonnet-4-6", max_tokens = 1000, system = systemPrompt,
            messages = Newtonsoft.Json.Linq.JArray.Parse(messagesJson),
            output_config = new { format = new { type = "json_schema", schema = Newtonsoft.Json.Linq.JObject.Parse(schema) } }
        });

        using (var request = new UnityWebRequest(ENDPOINT, "POST"))
        {
            request.timeout = 45;
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
                onError?.Invoke($"API 요청 실패 (HTTP {request.responseCode}): {request.error}");
        }
#endif
    }

    private string BuildMessagesJson(List<ChatMessage> history)
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(history);
    }
}
