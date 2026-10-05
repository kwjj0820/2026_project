using UnityEngine;
using Newtonsoft.Json.Linq;
using System.IO;

public class MockTestTrigger : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetSprite; // 인스펙터에서 연결

    void Start()
    {
        // 1. 가짜 응답 생성
        string fakeResponse = @"{
            ""content"": [{ ""type"": ""text"", ""text"": ""red"" }]
        }";

        // 2. 파일로 저장 (아래 SaveResponse 함수 호출)
        SaveResponse(fakeResponse);

        // 3. 텍스트 추출 (아래 ExtractText 함수 호출)
        string extractedText = ExtractText(fakeResponse);
        Debug.Log($"추출된 텍스트: {extractedText}");

        // 4. 오브젝트에 반영
        ApplyColor(extractedText);
    }

    // ---- 아래는 이 스크립트 안에 있는 "다른 함수들" ----

    void SaveResponse(string json)
    {
        string path = Path.Combine(Application.persistentDataPath, "last_response.json");
        File.WriteAllText(path, json);
        Debug.Log($"저장 경로: {path}");
    }

    string ExtractText(string rawJson)
    {
        var obj = JObject.Parse(rawJson);
        return obj["content"][0]["text"].ToString();
    }

    void ApplyColor(string text)
    {
        if (text.Contains("red")) targetSprite.color = Color.red;
        else if (text.Contains("blue")) targetSprite.color = Color.blue;
        else if (text.Contains("green")) targetSprite.color = Color.green;
    }
}