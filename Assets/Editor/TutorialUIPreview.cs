using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Explicit, non-gameplay preview: no player saves, network calls, or active-scene edits.
public static class TutorialUIPreview
{
    [MenuItem("Tools/Tutorial/Export UI Previews")]
    public static void Export()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var owner = new GameObject("TutorialUIPreview");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, scene);
        RenderTexture target = null;
        Texture2D image = null;
        RenderTexture previous = RenderTexture.active;
        try
        {
            var controller = owner.AddComponent<TutorialController>();
            var view = owner.AddComponent<TutorialView>();
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NanumFontSetup_TTF_GOTHIC/NanumGothic.ttf");
            view.Initialize(controller, font);
            var instant = (Toggle)typeof(TutorialView).GetField("instantText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
            instant.SetIsOnWithoutNotify(false);
            var session = new TutorialSession();
            foreach (string action in new[] { "breakfast", "ask_order", "inspect_room", "go_guild", "accept", "go_yard", "interview", "slip", "go_alley", "inspect_doors", "go_yard" })
                session.Perform(action);
            session.Connect("testimony", "doors");
            view.Render(session, new List<string> { "testimony", "doors" }, "자동 저장", false, false);
            if (view.CanChoose) throw new InvalidOperationException("Choices must be hidden while new text is revealing.");
            CompleteReading(view);
            var cameraObject = new GameObject("PreviewCamera", typeof(Camera));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = scene;
            camera.orthographic = true;
            camera.orthographicSize = 450;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 1 << 31;
            foreach (Transform child in owner.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            var canvas = owner.GetComponentInChildren<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            target = new RenderTexture(1440, 900, 24);
            target.Create();
            camera.targetTexture = target;
            image = new Texture2D(1440, 900, TextureFormat.RGB24, false);
            Directory.CreateDirectory("Logs/ui-preview");
            Capture(owner, camera, target, image, "Logs/ui-preview/dialogue.png");
            view.Render(new TutorialSession(), new List<string>(), "자동 저장", false, false);
            CompleteReading(view);
            var portrait = owner.GetComponentInChildren<SoftPortrait>();
            if (portrait == null || portrait.canvasRenderer == null || portrait.mainTexture == Texture2D.whiteTexture)
                throw new InvalidOperationException("Morning portrait must have its sprite and canvas renderer.");
            Capture(owner, camera, target, image, "Logs/ui-preview/morning.png");
            var toggleInput = typeof(TutorialView).GetMethod("ToggleDirectInput", BindingFlags.Instance | BindingFlags.NonPublic);
            toggleInput.Invoke(view, null);
            var composer = (RectTransform)typeof(TutorialView).GetField("composer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
            var actions = (RectTransform)typeof(TutorialView).GetField("actionList", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
            if (!composer.gameObject.activeInHierarchy || actions.parent.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Direct input must replace rather than overlap choices.");
            Capture(owner, camera, target, image, "Logs/ui-preview/input.png");
            toggleInput.Invoke(view, null);
            view.Render(session, new List<string> { "testimony", "doors" }, "자동 저장", false, false);
            CompleteReading(view);
            typeof(TutorialView).GetMethod("OpenJournal", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, new object[] { true });
            Capture(owner, camera, target, image, "Logs/ui-preview/journal.png");
            typeof(TutorialView).GetMethod("OpenLog", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, new object[] { true });
            Capture(owner, camera, target, image, "Logs/ui-preview/log.png");
            typeof(TutorialView).GetMethod("OpenInventory", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, new object[] { true });
            Capture(owner, camera, target, image, "Logs/ui-preview/inventory.png");
            typeof(TutorialView).GetMethod("OpenInventory", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, new object[] { false });
            session.Say(string.Join("\n", Enumerable.Repeat("여관 뒤편으로 걸어간다. 두 문 앞에 서서 주변을 다시 살핀다.", 30)), "주변 조사");
            view.Render(session, new List<string>(), "Long-page preview", false, false);
            int pageCount = (int)typeof(TutorialView).GetField("pageCount", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
            if (pageCount < 2) throw new InvalidOperationException("Long text must paginate.");
            CompleteReading(view);
            Capture(owner, camera, target, image, "Logs/ui-preview/reader-long.png");
            if (owner.GetComponentInChildren<SoftPortrait>() != null)
                throw new InvalidOperationException("Investigation must not retain the previous NPC portrait.");
            Debug.Log("[Tutorial] Reader checks passed: choices hidden until revealed; multi-page text completed.");
            Debug.Log("[Tutorial] UI previews exported: Logs/ui-preview/");
        }
        finally
        {
            RenderTexture.active = previous;
            foreach (var previewCamera in owner.scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Camera>()))
                previewCamera.targetTexture = null;
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void Capture(GameObject owner, Camera camera, RenderTexture target, Texture2D image, string path)
    {
        foreach (Transform child in owner.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
        // Repeated layout passes settle nested TMP preferred-height measurements.
        for (int i = 0; i < 3; i++)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in owner.GetComponentsInChildren<TextMeshProUGUI>()) text.ForceMeshUpdate();
        }
        Canvas.ForceUpdateCanvases();
        owner.GetComponentsInChildren<ScrollRect>(true).First(scroll => scroll.name == "DialogueHistory").verticalNormalizedPosition = 0;
        camera.Render();
        RenderTexture.active = target;
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());
    }

    private static void CompleteReading(TutorialView view)
    {
        for (int i = 0; i < 256 && !view.CanChoose; i++) view.AdvanceReading();
        if (!view.CanChoose) throw new InvalidOperationException("Reader did not finish its pages.");
    }
}
