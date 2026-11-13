using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Dialogue-first UI. The journal and settings are modal, transient presentation state.
public class TutorialView : MonoBehaviour
{
    private TutorialController controller;
    private TMP_FontAsset font;
    private TMP_FontAsset dynamicFont;
    private GameObject canvasObject;
    private RectTransform root;
    private RectTransform journal;
    private RectTransform settings;
    private RectTransform log;
    private RectTransform inventory;
    private RectTransform inventoryList;
    private RectTransform choices;
    private RectTransform composer;
    private TextMeshProUGUI sceneText;
    private TextMeshProUGUI sceneSpeaker;
    private TextMeshProUGUI introduction;
    private SoftPortrait portrait;
    private TutorialPortraitCatalog portraits;
    private TextMeshProUGUI readingHint;
    private Button readerButton;
    private Button directInput;
    private Button speedButton;
    private Toggle instantText;
    private readonly TextReveal reveal = new TextReveal();
    private readonly TextReveal introductionReveal = new TextReveal();
    private TutorialMessage displayedMessage;
    private int currentPage;
    private int pageCount = 1;
    private int pageStart;
    private int pageEnd;
    private int lastAdvanceFrame = -1;
    private int speedIndex = 1;
    private bool directInputOpen;
    public bool CanChoose => introductionReveal.Complete && reveal.Complete && currentPage >= pageCount - 1;
    private RectTransform attachment;
    private RectTransform toast;
    private TextMeshProUGUI title;
    private TextMeshProUGUI objective;
    private TextMeshProUGUI status;
    private TextMeshProUGUI menuStatus;
    private TextMeshProUGUI modeHint;
    private TextMeshProUGUI recordCount;
    private TextMeshProUGUI attachmentText;
    private TextMeshProUGUI toastText;
    private TextMeshProUGUI selectedDescription;
    private TextMeshProUGUI connectionFeedback;
    private RectTransform messageList;
    private RectTransform actionList;
    private RectTransform recordList;
    private RectTransform toolList;
    private ScrollRect historyScroll;
    private TMP_InputField input;
    private Toggle useAI;
    private Button send;
    private Button combine;
    private Button present;
    private Button restart;
    private TutorialSession currentSession;
    private List<string> currentSelection;
    private string currentStatus;
    private bool isBusy;
    private bool isBlocked;
    private string recordFilter = "전체";
    private string transcriptSignature;
    private bool initializedCounts;
    private readonly HashSet<string> knownRecords = new HashSet<string>();
    private readonly HashSet<string> knownTools = new HashSet<string>();
    private float toastExpires;
    private bool scrollToLatest;
    private readonly List<Button> filterButtons = new List<Button>();
    public string AttachedTool { get; private set; }

    private static readonly Color Ink = new Color(0.91f, 0.89f, 0.84f);
    private static readonly Color Muted = new Color(0.66f, 0.67f, 0.65f);
    private static readonly Color Copper = new Color(0.73f, 0.56f, 0.35f);
    private static readonly Color Night = new Color(0.045f, 0.055f, 0.063f);
    private static readonly Color Paper = new Color(0.84f, 0.80f, 0.69f);
    private static readonly Color PaperInk = new Color(0.16f, 0.18f, 0.17f);
    private static readonly Color PaperMuted = new Color(0.31f, 0.33f, 0.30f);

    public void Initialize(TutorialController owner, Font sourceFont = null)
    {
        controller = owner;
        speedIndex = Mathf.Clamp(PlayerPrefs.GetInt("tutorial.reader.speed", 1), 0, 2);
        var source = FindFirstObjectByType<TextMeshProUGUI>();
        font = source != null ? source.font : TMP_Settings.defaultFontAsset;
        Font original = sourceFont != null ? sourceFont : font != null ? font.sourceFontFile : null;
        if (original != null)
        {
            dynamicFont = TMP_FontAsset.CreateFontAsset(original, 64, 9,
                GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (dynamicFont != null)
            {
                dynamicFont.name = "Tutorial NanumGothic Dynamic SDF";
                font = dynamicFont;
                Debug.Log("[Tutorial] 폰트: 원본 TTF → SDFAA / padding 9 / 전용 머티리얼");
            }
            else Debug.LogError("[Tutorial] 원본 TTF로 폰트를 생성하지 못했습니다.");
        }
        else Debug.LogWarning("[Tutorial] Tutorial Source Font에 NanumGothic.ttf를 연결하세요.");

        canvasObject = new GameObject("TutorialCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        root = (RectTransform)canvasObject.transform;
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440, 900);
        scaler.matchWidthOrHeight = 0.5f;
        portraits = Resources.Load<TutorialPortraitCatalog>("UI/TutorialPortraits");
        BuildDialogue();
        BuildJournal();
        BuildSettings();
        BuildLogAndInventory();
        toast = Panel("RecordToast", root, 0.66f, 0.845f, 0.96f, 0.915f, new Color(0.16f, 0.20f, 0.19f));
        toast.GetComponent<Image>().raycastTarget = false;
        toastText = Text("Message", toast, 0.05f, 0.12f, 0.95f, 0.88f, 17, Ink);
        toast.gameObject.SetActive(false);
    }

    private void BuildDialogue()
    {
        Panel("Background", root, 0, 0, 1, 1, Night);
        Panel("LeftShadow", root, 0, 0, 0.12f, 1, new Color(0.035f, 0.043f, 0.05f));
        Panel("RightShadow", root, 0.88f, 0, 1, 1, new Color(0.035f, 0.043f, 0.05f));
        var chapter = Text("Chapter", root, 0.29f, 0.955f, 0.71f, 0.98f, 12, Copper);
        chapter.text = "어느 평범한 하루  /  튜토리얼";
        chapter.alignment = TextAlignmentOptions.Center;
        title = Text("Location", root, 0.29f, 0.90f, 0.71f, 0.95f, 28, Ink);
        title.alignment = TextAlignmentOptions.Center;
        Panel("HeaderRule", root, 0.29f, 0.888f, 0.71f, 0.889f, new Color(0.25f, 0.25f, 0.23f));
        objective = Text("Objective", root, 0.29f, 0.86f, 0.71f, 0.885f, 12, Muted);
        objective.gameObject.SetActive(false); // The objective remains available in the inventory.

        RectTransform surface = Panel("Reader", root, 0.29f, 0.37f, 0.71f, 0.87f, Color.clear);
        readerButton = surface.gameObject.AddComponent<Button>();
        readerButton.targetGraphic = surface.GetComponent<Image>();
        readerButton.transition = Selectable.Transition.None;
        readerButton.onClick.AddListener(AdvanceReading);
        introduction = Text("Introduction", surface, 0, 0.81f, 1, 1, 21, Ink);
        introduction.lineSpacing = 8;
        var portraitObject = new GameObject("NPCPortrait", typeof(RectTransform), typeof(SoftPortrait));
        var portraitRect = (RectTransform)portraitObject.transform;
        portraitRect.SetParent(surface, false);
        Anchor(portraitRect, 0.05f, 0.23f, 0.95f, 0.78f);
        portrait = portraitObject.GetComponent<SoftPortrait>();
        portrait.raycastTarget = false;
        sceneSpeaker = Text("Speaker", surface, 0, 0.195f, 1, 0.24f, 14, Copper);
        sceneSpeaker.alignment = TextAlignmentOptions.Center;
        sceneText = Text("SceneText", surface, 0, 0, 1, 0.19f, 22, Ink);
        sceneText.overflowMode = TextOverflowModes.Page;
        sceneText.lineSpacing = 7;
        readingHint = Text("ReadingHint", root, 0.29f, 0.342f, 0.71f, 0.365f, 12, Muted);
        readingHint.alignment = TextAlignmentOptions.Right;

        choices = Panel("Choices", root, 0, 0, 1, 1, Color.clear);
        choices.GetComponent<Image>().raycastTarget = false;
        ScrollRect ignored;
        actionList = Scroll("Actions", choices, 0.29f, 0.15f, 0.71f, 0.335f, out ignored);
        directInput = Button("직접 입력하기", choices, 0.29f, 0.105f, 0.71f, 0.143f, ToggleDirectInput);
        QuietButton(directInput);
        BuildInput();
        attachment = Panel("Attachment", choices, 0.29f, 0.075f, 0.71f, 0.103f, new Color(0.11f, 0.12f, 0.12f));
        attachmentText = Text("Evidence", attachment, 0.02f, 0.05f, 0.68f, 0.95f, 12, Ink);
        present = Button("근거 제시", attachment, 0.71f, 0.02f, 0.91f, 0.98f,
            () => { if (CanChoose && AttachedTool != null) controller.Present(AttachedTool); });
        var detach = Button("해제", attachment, 0.92f, 0.02f, 0.99f, 0.98f, () => controller.ClearAttachment());
        foreach (Button button in new[] { present, detach })
        {
            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            label.fontSize = 12;
            label.alignment = TextAlignmentOptions.Center;
        }

        modeHint = Text("ModeHint", composer, 0.02f, 0, 0.98f, 0.22f, 12, Muted);
        status = Text("Status", root, 0.29f, 0.057f, 0.71f, 0.075f, 11, Muted);
        Panel("FooterRule", root, 0.29f, 0.055f, 0.71f, 0.056f, new Color(0.25f, 0.25f, 0.23f));
        FooterButton("기록장", 0.29f, 0.388f, () => OpenJournal(true)).gameObject.name = "OpenJournal";
        FooterButton("소지품", 0.394f, 0.492f, () => OpenInventory(true));
        FooterButton("대화 로그", 0.498f, 0.606f, () => OpenLog(true));
        FooterButton("설정", 0.612f, 0.71f, () => OpenSettings(true)).gameObject.name = "OpenSettings";
        recordCount = Text("RecordCount", root, 0.74f, 0.023f, 0.91f, 0.045f, 12, Muted);
    }

    private Button FooterButton(string label, float left, float right, UnityEngine.Events.UnityAction action)
    {
        var button = Button(label, root, left, 0.012f, right, 0.05f, action);
        QuietButton(button);
        var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        text.fontSize = 15;
        text.color = Copper;
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static void QuietButton(Button button)
    {
        var colors = button.colors;
        colors.normalColor = new Color(0.065f, 0.07f, 0.075f);
        colors.highlightedColor = new Color(0.17f, 0.17f, 0.16f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.targetGraphic.color = Color.white;
    }

    private void BuildInput()
    {
        composer = Panel("Composer", choices, 0.29f, 0.15f, 0.71f, 0.28f, new Color(0.10f, 0.11f, 0.12f));
        Panel("ComposerEdge", composer, 0, 0, 0.003f, 1, Copper);
        RectTransform box = Panel("FreeInput", composer, 0.02f, 0.30f, 0.80f, 0.85f, Color.clear);
        input = box.gameObject.AddComponent<TMP_InputField>();
        input.transition = Selectable.Transition.None;
        input.targetGraphic = box.GetComponent<Image>();
        input.customCaretColor = true;
        input.caretColor = Ink;
        input.selectionColor = new Color(0.25f, 0.45f, 0.5f, 0.55f);
        input.textViewport = box;
        input.textComponent = Text("Text", box, 0, 0, 1, 1, 21, Ink);
        var placeholder = Text("Placeholder", box, 0, 0, 1, 1, 20, Muted);
        placeholder.text = "말이나 행동을 직접 입력하세요.";
        input.placeholder = placeholder;
        input.characterLimit = 500;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.richText = false;
        input.onSubmit.AddListener(value => SubmitInput(value));
        send = Button("전송  →", composer, 0.82f, 0.15f, 0.98f, 0.85f, () => SubmitInput(input.text));
        composer.gameObject.SetActive(false);
    }

    private void SubmitInput(string value)
    {
        if (CanChoose && !ModalOpen && !isBusy) controller.Submit(value, useAI.isOn);
    }

    private void ToggleDirectInput()
    {
        if (!CanChoose || isBusy) return;
        directInputOpen = !directInputOpen;
        composer.gameObject.SetActive(directInputOpen);
        actionList.parent.gameObject.SetActive(!directInputOpen);
        directInput.GetComponentInChildren<TextMeshProUGUI>(true).text = directInputOpen ? "직접 입력 닫기" : "직접 입력하기";
        if (directInputOpen) input.ActivateInputField();
        else input.DeactivateInputField();
    }

    private void BuildJournal()
    {
        journal = Modal("JournalOverlay", () => OpenJournal(false));
        RectTransform sheet = Panel("JournalSheet", journal, 0.07f, 0.07f, 0.93f, 0.93f, Paper);
        Panel("Spine", sheet, 0.36f, 0.08f, 0.362f, 0.82f, new Color(0.65f, 0.61f, 0.52f));
        Text("Title", sheet, 0.035f, 0.89f, 0.70f, 0.96f, 32, PaperInk).text = "여행 기록";
        Text("Subtitle", sheet, 0.035f, 0.835f, 0.82f, 0.887f, 17, PaperMuted).text = "증언은 주장입니다. 물건과 관찰을 대조해 근거를 만드세요.";
        Button("닫기", sheet, 0.87f, 0.90f, 0.965f, 0.96f, () => OpenJournal(false), true);
        string[] filters = { "전체", "증언", "증거", "관찰" };
        for (int i = 0; i < filters.Length; i++)
        {
            string filter = filters[i];
            var button = Button(filter, sheet, 0.035f + i * 0.079f, 0.75f, 0.108f + i * 0.079f, 0.805f,
                () => { recordFilter = filter; RefreshCurrent(); }, true);
            filterButtons.Add(button);
        }
        ScrollRect ignored;
        recordList = Scroll("Records", sheet, 0.03f, 0.09f, 0.34f, 0.73f, out ignored);
        Text("SelectionTitle", sheet, 0.39f, 0.75f, 0.96f, 0.81f, 20, PaperInk).text = "기록 살펴보기 · 최대 두 개 선택";
        RectTransform details = Scroll("RecordDetails", sheet, 0.39f, 0.48f, 0.96f, 0.735f, out ignored);
        selectedDescription = ListText(details, "", 20, PaperInk);
        combine = Button("두 기록 연결하기", sheet, 0.39f, 0.405f, 0.96f, 0.465f, controller.Connect, true);
        connectionFeedback = Text("ConnectionFeedback", sheet, 0.39f, 0.335f, 0.96f, 0.397f, 16, PaperMuted);
        Text("ToolsTitle", sheet, 0.39f, 0.275f, 0.96f, 0.33f, 20, PaperInk).text = "활용 수단  /  눌러 대화에 첨부";
        toolList = Scroll("Tools", sheet, 0.39f, 0.085f, 0.96f, 0.27f, out ignored);
        journal.gameObject.SetActive(false);
    }

    private void BuildSettings()
    {
        settings = Modal("SettingsOverlay", () => OpenSettings(false));
        RectTransform panel = Panel("MenuSheet", settings, 0.25f, 0.10f, 0.75f, 0.90f, new Color(0.09f, 0.12f, 0.13f));
        Text("Title", panel, 0.07f, 0.87f, 0.75f, 0.96f, 29, Ink).text = "설정";
        Button("닫기", panel, 0.79f, 0.88f, 0.94f, 0.95f, () => OpenSettings(false));
        RectTransform toggleBox = Panel("AIOption", panel, 0.07f, 0.74f, 0.93f, 0.84f, Color.clear);
        useAI = toggleBox.gameObject.AddComponent<Toggle>();
        RectTransform checkBox = Panel("CheckBox", toggleBox, 0, 0.23f, 0.055f, 0.77f, new Color(0.27f, 0.30f, 0.30f));
        RectTransform mark = Panel("Check", checkBox, 0.2f, 0.2f, 0.8f, 0.8f, Copper);
        SetControlColors(useAI, checkBox.GetComponent<Image>(), false);
        useAI.graphic = mark.GetComponent<Image>();
        useAI.isOn = false;
        Text("Label", toggleBox, 0.085f, 0, 1, 1, 22, Ink).text = "AI 자유 대화";
        useAI.onValueChanged.AddListener(value => UpdateModeHint());
        Text("AIHelp", panel, 0.07f, 0.59f, 0.93f, 0.73f, 18, Muted).text =
            "켜면 자유 입력을 전송할 때 API 비용이 발생합니다.\nEditor와 환경 변수 ANTHROPIC_API_KEY가 필요합니다.\n끄면 행동 버튼과 기록장으로 진행할 수 있습니다.";
        speedButton = Button(SpeedLabel(), panel, 0.07f, 0.47f, 0.51f, 0.55f, () =>
        {
            speedIndex = (speedIndex + 1) % 3;
            PlayerPrefs.SetInt("tutorial.reader.speed", speedIndex); PlayerPrefs.Save();
            speedButton.GetComponentInChildren<TextMeshProUGUI>().text = SpeedLabel();
        });
        RectTransform instantBox = Panel("InstantText", panel, 0.55f, 0.47f, 0.93f, 0.55f, Color.clear);
        instantText = instantBox.gameObject.AddComponent<Toggle>();
        RectTransform instantCheck = Panel("CheckBox", instantBox, 0, 0.22f, 0.11f, 0.78f, Color.white);
        RectTransform instantMark = Panel("Check", instantCheck, 0.2f, 0.2f, 0.8f, 0.8f, Copper);
        SetControlColors(instantText, instantCheck.GetComponent<Image>(), false);
        instantText.graphic = instantMark.GetComponent<Image>();
        instantText.isOn = PlayerPrefs.GetInt("tutorial.reader.instant", 0) == 1;
        Text("Label", instantBox, 0.15f, 0, 1, 1, 19, Ink).text = "즉시 표시";
        instantText.onValueChanged.AddListener(value =>
        {
            PlayerPrefs.SetInt("tutorial.reader.instant", value ? 1 : 0); PlayerPrefs.Save();
            if (value) { introductionReveal.Finish(); reveal.Finish(); ApplyReveal(); }
        });
        Text("ReaderHelp", panel, 0.07f, 0.39f, 0.93f, 0.46f, 16, Muted).text = "본문 클릭 / Space: 즉시 완성 또는 다음 페이지";
        restart = Button("튜토리얼 처음부터", panel, 0.07f, 0.28f, 0.93f, 0.36f, controller.ResetTutorial);
        Text("ResetHelp", panel, 0.07f, 0.215f, 0.93f, 0.27f, 15, Muted).text = "두 번 누르면 튜토리얼 진행을 새로 저장합니다.";
        menuStatus = Text("MenuStatus", panel, 0.07f, 0.04f, 0.93f, 0.22f, 17, Ink);
        settings.gameObject.SetActive(false);
    }

    public void ClearInput() { input.text = ""; }
    public void ClearAttachedTool() { AttachedTool = null; }

    private string SpeedLabel() { return "글자 속도 · " + new[] { "느리게", "보통", "빠르게" }[speedIndex]; }

    private void BuildLogAndInventory()
    {
        log = Modal("LogOverlay", () => OpenLog(false));
        RectTransform logSheet = Panel("LogSheet", log, 0.16f, 0.10f, 0.84f, 0.90f, new Color(0.09f, 0.12f, 0.13f));
        Text("Title", logSheet, 0.05f, 0.87f, 0.75f, 0.96f, 28, Ink).text = "대화 로그";
        Button("닫기", logSheet, 0.83f, 0.89f, 0.96f, 0.96f, () => OpenLog(false));
        messageList = Scroll("DialogueHistory", logSheet, 0.05f, 0.05f, 0.95f, 0.85f, out historyScroll);
        log.gameObject.SetActive(false);
        inventory = Modal("InventoryOverlay", () => OpenInventory(false));
        RectTransform bag = Panel("InventorySheet", inventory, 0.23f, 0.18f, 0.77f, 0.82f, Paper);
        Text("Title", bag, 0.06f, 0.83f, 0.75f, 0.95f, 28, PaperInk).text = "소지품";
        Button("닫기", bag, 0.81f, 0.86f, 0.95f, 0.95f, () => OpenInventory(false), true);
        ScrollRect ignored;
        inventoryList = Scroll("Items", bag, 0.06f, 0.09f, 0.94f, 0.79f, out ignored);
        inventory.gameObject.SetActive(false);
    }

    public void Render(TutorialSession session, List<string> selected, string message, bool busy, bool blocked)
    {
        bool differentSession = currentSession != session;
        currentSession = session;
        currentSelection = selected;
        currentStatus = message;
        isBusy = busy;
        isBlocked = blocked;
        title.text = session.LocationTitle;
        objective.text = "지금 할 일  ·  " + session.Objective;
        recordCount.text = "기록 " + session.Progress.records.Count + " / 근거 " + session.Progress.tools.Count;
        menuStatus.text = message;
        status.text = PlayerStatus(message, busy, blocked);
        UpdateModeHint();
        NotifyNewRecords(session);

        if (AttachedTool != null && !session.Progress.tools.Contains(AttachedTool)) AttachedTool = null;
        attachment.gameObject.SetActive(AttachedTool != null || selected.Count > 0);
        attachmentText.text = AttachedTool != null ? "첨부  ·  " + TutorialSession.ToolTitle(AttachedTool) : "첨부한 기록 " + selected.Count + "개";
        bool active = !busy && !blocked;
        useAI.interactable = active;
        instantText.interactable = true;
        restart.interactable = !busy;
        combine.interactable = active && selected.Count == 2;
        foreach (Button button in filterButtons) button.interactable = !busy;

        string signature = string.Join("\u001f", session.Progress.transcript.Select(line => line.speaker + "\u001e" + line.text));
        bool newDialogue = signature != transcriptSignature;
        if (newDialogue)
        {
            transcriptSignature = signature;
            Clear(messageList);
            foreach (TutorialMessage line in session.Progress.transcript)
                Message(line);
        }
        TutorialMessage latest = session.Progress.transcript.LastOrDefault(line => line.speaker != "플레이어");
        if (differentSession || !ReferenceEquals(displayedMessage, latest))
        {
            displayedMessage = latest;
            BeginReading(latest != null ? latest.text : session.Progress.lastText,
                latest != null ? latest.speaker : session.Actor);
        }
        DrawActions(session, active);
        DrawJournal(session, selected, active);
        DrawInventory(session);
        UpdateReadingControls();
        Canvas.ForceUpdateCanvases();
        if (newDialogue)
        {
            historyScroll.verticalNormalizedPosition = 0;
            scrollToLatest = true;
        }
        if (toast.gameObject.activeSelf) toast.SetAsLastSibling();
    }

    private void DrawActions(TutorialSession session, bool active)
    {
        Clear(actionList);
        var actions = session.Actions();
        for (int i = 0; i < actions.Count; i++)
        {
            string id = actions[i].id;
            var button = ListButton(actionList, actions[i].label, () =>
            {
                if (CanChoose && !ModalOpen && !isBusy) controller.Act(id);
            }, false);
            var colors = button.colors;
            colors.normalColor = new Color(0.22f, 0.225f, 0.23f);
            colors.highlightedColor = new Color(0.32f, 0.31f, 0.29f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = 19;
            button.interactable = active;
        }
        if (actions.Count == 0)
            ListText(actionList, "하루를 마쳤습니다. 여기까지가 현재 튜토리얼입니다.", 19, Muted);
    }

    private void DrawInventory(TutorialSession session)
    {
        Clear(inventoryList);
        ListText(inventoryList, "검\n처음부터 가지고 있던 평범한 무기.", 22, PaperInk);
        ListText(inventoryList, "여행 가방\n여행에 필요한 짐을 넣어 둔 가방.", 22, PaperInk);
        if (session.Progress.records.Contains("adventurer"))
            ListText(inventoryList, "길드 등록증\n자신의 이름과 길드 표식이 적혀 있다.", 22, PaperInk);
        if (session.Progress.flags.Contains("recovered") && !session.Progress.flags.Contains("reported"))
            ListText(inventoryList, "식재료 상자\n여관에 전달할 물건. 주문 번호 17.", 22, PaperInk);
        if (session.Progress.flags.Contains("reported"))
            ListText(inventoryList, "식재료 상자는 여관에 전달했다.", 18, PaperMuted);
        ListText(inventoryList, "증언과 문서·물건의 조사 기록은 기록장에서 확인한다.", 18, PaperMuted);
        ListText(inventoryList, "지금 할 일\n" + session.Objective, 18, PaperInk);
    }

    private void BeginReading(string text, string speaker)
    {
        directInputOpen = false;
        input.DeactivateInputField();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        sceneSpeaker.text = speaker;
        GamebookContent content = GamebookContent.Parse(text);
        Sprite sprite = portraits != null ? portraits.Find(speaker) : null;
        bool isNpc = speaker == "여관 주인의 딸" || speaker == "길드 접수원" || speaker == "배달원" || speaker == "가게 주인";
        if (sprite == null && isNpc)
        {
            var candidates = Resources.LoadAll<Sprite>(speaker == "배달원" ? "Characters/stranger_angry" : "Characters/stranger_confused");
            if (candidates.Length > 0) sprite = candidates[0];
        }
        bool illustrated = sprite != null && isNpc && content.Body.Length < 450;
        portrait.SetPortrait(illustrated ? sprite : null);
        portrait.gameObject.SetActive(illustrated);
        introduction.text = illustrated ? content.Introduction : "";
        sceneText.text = illustrated ? content.Body : text ?? "";
        Anchor(sceneText.rectTransform, 0, 0, 1, illustrated ? 0.19f : 0.90f);
        Anchor(sceneSpeaker.rectTransform, 0, illustrated ? 0.195f : 0.93f, 1, illustrated ? 0.24f : 1);
        sceneText.maxVisibleCharacters = int.MaxValue;
        sceneText.pageToDisplay = 1;
        Canvas.ForceUpdateCanvases();
        introduction.maxVisibleCharacters = int.MaxValue;
        introduction.ForceMeshUpdate(true);
        // If the current font/resolution wraps the introduction too far, keep it paginated.
        if (introduction.preferredHeight > introduction.rectTransform.rect.height)
        {
            introduction.text = "";
            sceneText.text = text ?? "";
        }
        introduction.ForceMeshUpdate(true);
        introductionReveal.Begin(introduction.textInfo.characterCount);
        if (instantText.isOn) introductionReveal.Finish();
        sceneText.ForceMeshUpdate(true);
        pageCount = Mathf.Max(1, sceneText.textInfo.pageCount);
        currentPage = 0;
        BeginPage();
        lastAdvanceFrame = Time.frameCount;
    }

    private void BeginPage()
    {
        sceneText.pageToDisplay = currentPage + 1;
        int total = sceneText.textInfo.characterCount;
        if (sceneText.textInfo.pageCount > 0)
        {
            TMP_PageInfo page = sceneText.textInfo.pageInfo[currentPage];
            pageStart = Mathf.Clamp(page.firstCharacterIndex, 0, total);
            pageEnd = Mathf.Clamp(page.lastCharacterIndex + 1, pageStart, total);
        }
        else { pageStart = 0; pageEnd = total; }
        reveal.Begin(pageEnd - pageStart);
        if (instantText.isOn) reveal.Finish();
        ApplyReveal();
    }

    public void AdvanceReading()
    {
        if (ModalOpen || isBusy || (Application.isPlaying && lastAdvanceFrame == Time.frameCount)) return;
        lastAdvanceFrame = Time.frameCount;
        if (!introductionReveal.Complete) { introductionReveal.Finish(); ApplyReveal(); }
        else if (!reveal.Complete) { reveal.Finish(); ApplyReveal(); }
        else if (currentPage < pageCount - 1) { currentPage++; BeginPage(); }
    }

    private void ApplyReveal()
    {
        introduction.maxVisibleCharacters = introductionReveal.Visible;
        sceneText.maxVisibleCharacters = introductionReveal.Complete ? pageStart + reveal.Visible : 0;
        UpdateReadingControls();
    }

    private void UpdateReadingControls()
    {
        if (currentSession == null) return;
        choices.gameObject.SetActive(CanChoose && !isBusy);
        composer.gameObject.SetActive(directInputOpen && CanChoose && !isBusy);
        actionList.parent.gameObject.SetActive(!directInputOpen);
        directInput.GetComponentInChildren<TextMeshProUGUI>(true).text = directInputOpen ? "직접 입력 닫기" : "직접 입력하기";
        bool active = !isBusy && !isBlocked && CanChoose && currentSession.Progress.location != "complete";
        directInput.interactable = active;
        send.interactable = active;
        input.interactable = active;
        present.interactable = active && AttachedTool != null;
        readerButton.interactable = !isBusy && (!introductionReveal.Complete || !reveal.Complete || currentPage < pageCount - 1);
        readingHint.text = isBusy ? "답변을 기다리는 중입니다…" : !introductionReveal.Complete || !reveal.Complete
            ? "클릭 또는 Space로 즉시 표시" : currentPage < pageCount - 1
            ? "클릭 또는 Space로 다음 페이지  ·  " + (currentPage + 1) + " / " + pageCount
            : "아래에서 행동을 선택하거나 직접 입력하세요.";
    }

    private void DrawJournal(TutorialSession session, List<string> selected, bool active)
    {
        Clear(recordList);
        var records = TutorialSession.RecordDefinitions.Where(item => session.Progress.records.Contains(item.id)
            && (recordFilter == "전체" || item.kind == recordFilter || (recordFilter == "증거" && (item.kind == "물건" || item.kind == "문서")))).ToList();
        foreach (TutorialRecord record in records)
        {
            string id = record.id;
            var button = ListButton(recordList, (selected.Contains(id) ? "[선택] " : "") + record.title,
                () => controller.SelectRecord(id), true);
            button.interactable = !isBusy;
        }
        if (records.Count == 0) ListText(recordList, "이 분류에 수집한 기록이 없습니다.", 18, PaperMuted);
        selectedDescription.text = selected.Count == 0
            ? "왼쪽에서 기록을 골라 출처와 내용을 확인하세요.\n\n서로 관련 있는 두 기록을 선택하면 연결을 시도할 수 있습니다."
            : string.Join("\n\n", TutorialSession.RecordDefinitions.Where(item => selected.Contains(item.id))
                .Select(item => item.kind + " · " + item.title + "\n출처: " + item.source + "\n" + item.description));
        connectionFeedback.text = session.Progress.notice ?? "연결한 내용은 확정 사실이 아니라, 확인에 사용할 근거일 수 있습니다.";
        Clear(toolList);
        foreach (string tool in session.Progress.tools.OrderBy(item => item))
        {
            string captured = tool;
            ListButton(toolList, (AttachedTool == tool ? "첨부됨 · " : "") + TutorialSession.ToolTitle(tool), () =>
            {
                AttachedTool = captured;
                OpenJournal(false);
                RefreshCurrent();
            }, true).interactable = active;
        }
        if (session.Progress.tools.Count == 0)
            ListText(toolList, "정보를 연결하면 사용할 수 있는 근거가 이곳에 남습니다.", 18, PaperMuted);
        for (int i = 0; i < filterButtons.Count; i++)
        {
            string[] labels = { "전체", "증언", "증거", "관찰" };
            var label = filterButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
            label.text = (recordFilter == labels[i] ? "· " : "") + labels[i];
        }
    }

    private void Message(TutorialMessage line)
    {
        var go = new GameObject("DialogueMessage", typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(messageList, false);
        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 14, 10, 18);
        layout.spacing = 9;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var speaker = ListText((RectTransform)go.transform, line.speaker, 15, line.speaker == "플레이어" ? Muted : Copper);
        speaker.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;
        ListText((RectTransform)go.transform, line.text, 23, Ink);
    }

    private void NotifyNewRecords(TutorialSession session)
    {
        if (initializedCounts)
        {
            var added = TutorialSession.RecordDefinitions.Where(item => session.Progress.records.Contains(item.id) && !knownRecords.Contains(item.id)).ToList();
            var tools = session.Progress.tools.Where(item => !knownTools.Contains(item)).ToList();
            if (tools.Count > 0) ShowToast("새 근거  ·  " + TutorialSession.ToolTitle(tools[0]));
            else if (added.Count > 0) ShowToast("기록 추가  ·  " + added[0].title + (added.Count > 1 ? " 외 " + (added.Count - 1) + "개" : ""));
        }
        initializedCounts = true;
        knownRecords.Clear(); knownRecords.UnionWith(session.Progress.records);
        knownTools.Clear(); knownTools.UnionWith(session.Progress.tools);
    }

    private void ShowToast(string value)
    {
        toastText.text = value;
        toastExpires = Time.unscaledTime + 5f;
        toast.gameObject.SetActive(true);
    }

    private string PlayerStatus(string value, bool busy, bool blocked)
    {
        if (busy) return "답변을 기다리는 중입니다…";
        if (blocked) return value;
        if (value == null || value.Contains("자동 저장") || value.StartsWith("새 튜토리얼") ||
            value.StartsWith("새 게임") || value.StartsWith("이어하기") || value.StartsWith("대화 응답") || value == "입력한 행동을 적용했습니다.")
            return "";
        return value;
    }

    private void UpdateModeHint()
    {
        if (modeHint == null || useAI == null) return;
        modeHint.text = useAI.isOn ? "자유 대화 사용 중  ·  Enter로 전송" : "자유 대화 꺼짐  ·  메뉴에서 켜거나 행동 버튼으로 진행";
    }

    private bool ModalOpen => journal != null && journal.gameObject.activeSelf || settings != null && settings.gameObject.activeSelf
        || log != null && log.gameObject.activeSelf || inventory != null && inventory.gameObject.activeSelf;

    private void OpenJournal(bool open)
    {
        SetModal(journal, open);
    }

    private void OpenSettings(bool open)
    {
        SetModal(settings, open);
    }

    private void OpenLog(bool open) { if (open) scrollToLatest = true; SetModal(log, open); }
    private void OpenInventory(bool open) { SetModal(inventory, open); }

    private void SetModal(RectTransform target, bool open)
    {
        input.DeactivateInputField();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        foreach (RectTransform modal in new[] { journal, settings, log, inventory })
            if (modal != null) modal.gameObject.SetActive(false);
        target.gameObject.SetActive(open);
        if (open)
        {
            target.SetAsLastSibling();
            toast.gameObject.SetActive(false);
            RefreshCurrent();
        }
    }

    private void RefreshCurrent()
    {
        if (currentSession != null) Render(currentSession, currentSelection, currentStatus, isBusy, isBlocked);
    }

    private void Update()
    {
        if (toast != null && toast.gameObject.activeSelf && Time.unscaledTime >= toastExpires)
            toast.gameObject.SetActive(false);
        if (currentSession != null && !ModalOpen && !isBusy)
        {
            if (!introductionReveal.Complete)
            {
                introductionReveal.Tick(Time.unscaledDeltaTime, instantText.isOn ? 0 : new[] { 22f, 42f, 72f }[speedIndex]);
                ApplyReveal();
            }
            else if (!reveal.Complete)
            {
                reveal.Tick(Time.unscaledDeltaTime, instantText.isOn ? 0 : new[] { 22f, 42f, 72f }[speedIndex]);
                ApplyReveal();
            }
            if (!input.isFocused && Input.GetKeyDown(KeyCode.Space)) AdvanceReading();
        }
        if (input != null && Input.GetKeyDown(KeyCode.Escape))
        {
            if (journal.gameObject.activeSelf) OpenJournal(false);
            else if (settings.gameObject.activeSelf) OpenSettings(false);
            else if (log.gameObject.activeSelf) OpenLog(false);
            else if (inventory.gameObject.activeSelf) OpenInventory(false);
            else if (!input.isFocused) OpenSettings(true);
        }
    }

    private void LateUpdate()
    {
        if (!scrollToLatest || log == null || !log.gameObject.activeInHierarchy) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(messageList);
        Canvas.ForceUpdateCanvases();
        historyScroll.verticalNormalizedPosition = 0;
        scrollToLatest = false;
    }

    private RectTransform Modal(string name, UnityEngine.Events.UnityAction close)
    {
        RectTransform overlay = Panel(name, root, 0, 0, 1, 1, Color.clear);
        RectTransform shade = Panel("Backdrop", overlay, 0, 0, 1, 1, new Color(0, 0, 0, 0.78f));
        var backdrop = shade.gameObject.AddComponent<Button>();
        backdrop.transition = Selectable.Transition.None;
        backdrop.targetGraphic = shade.GetComponent<Image>();
        backdrop.onClick.AddListener(close);
        return overlay;
    }

    private RectTransform Panel(string name, RectTransform parent, float x0, float y0, float x1, float y1, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        Anchor(rect, x0, y0, x1, y1);
        go.GetComponent<Image>().color = color;
        return rect;
    }

    private TextMeshProUGUI Text(string name, RectTransform parent, float x0, float y0, float x1, float y1, int size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        Anchor(rect, x0, y0, x1, y1);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = font;
        if (font != null) text.fontSharedMaterial = font.material;
        text.fontSize = size; text.color = color;
        text.richText = false; text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        return text;
    }

    private Button Button(string label, RectTransform parent, float x0, float y0, float x1, float y1,
        UnityEngine.Events.UnityAction callback, bool paper = false)
    {
        RectTransform rect = Panel(label, parent, x0, y0, x1, y1, Color.white);
        var button = rect.gameObject.AddComponent<Button>();
        SetControlColors(button, rect.GetComponent<Image>(), paper);
        var text = Text("Label", rect, 0.035f, 0.08f, 0.965f, 0.92f, 18, paper ? PaperInk : Ink);
        text.text = label;
        text.alignment = TextAlignmentOptions.Midline;
        button.onClick.AddListener(callback);
        return button;
    }

    private static void SetControlColors(Selectable control, Image background, bool paper)
    {
        background.color = Color.white;
        control.targetGraphic = background;
        control.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = control.colors;
        colors.normalColor = paper ? new Color(0.76f, 0.73f, 0.63f) : new Color(0.12f, 0.16f, 0.17f);
        colors.highlightedColor = paper ? new Color(0.91f, 0.87f, 0.75f) : new Color(0.22f, 0.28f, 0.28f);
        colors.selectedColor = paper ? new Color(0.88f, 0.81f, 0.63f) : new Color(0.20f, 0.28f, 0.29f);
        colors.pressedColor = paper ? new Color(0.67f, 0.64f, 0.54f) : new Color(0.08f, 0.12f, 0.13f);
        colors.disabledColor = paper ? new Color(0.71f, 0.69f, 0.62f) : new Color(0.08f, 0.10f, 0.11f);
        colors.colorMultiplier = 1;
        colors.fadeDuration = 0.08f;
        control.colors = colors;
    }

    private Button ListButton(RectTransform parent, string label, UnityEngine.Events.UnityAction callback, bool paper)
    {
        var button = Button(label, parent, 0, 0, 1, 1, callback, paper);
        var element = button.gameObject.AddComponent<LayoutElement>();
        element.minHeight = element.preferredHeight = 48;
        return button;
    }

    private TextMeshProUGUI ListText(RectTransform parent, string value, int size, Color color)
    {
        var text = Text("Content", parent, 0, 0, 1, 1, size, color);
        text.text = value;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private RectTransform Scroll(string name, RectTransform parent, float x0, float y0, float x1, float y1, out ScrollRect scroll)
    {
        RectTransform rect = Panel(name, parent, x0, y0, x1, y1, Color.clear);
        rect.gameObject.AddComponent<RectMask2D>();
        scroll = rect.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30;
        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var contentRect = (RectTransform)content.transform;
        contentRect.SetParent(rect, false);
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = Vector2.zero;
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.padding = new RectOffset(3, 3, 3, 3);
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRect; scroll.viewport = rect;
        return contentRect;
    }

    private RectTransform Row(RectTransform parent)
    {
        var row = new GameObject("ActionRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        var group = row.GetComponent<HorizontalLayoutGroup>();
        group.spacing = 10;
        group.childControlWidth = group.childControlHeight = true;
        group.childForceExpandWidth = true; group.childForceExpandHeight = false;
        row.GetComponent<LayoutElement>().preferredHeight = 44;
        return (RectTransform)row.transform;
    }

    private static void FlexibleCell(GameObject go, RectTransform parent = null)
    {
        if (parent != null) go.transform.SetParent(parent, false);
        var layout = go.AddComponent<LayoutElement>();
        layout.minWidth = 0; layout.flexibleWidth = 1;
        layout.minHeight = layout.preferredHeight = 44;
    }

    private static void Clear(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            child.gameObject.SetActive(false);
            DisposeObject(child.gameObject);
        }
    }

    private static void Anchor(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (canvasObject != null) DisposeObject(canvasObject);
        if (dynamicFont != null) DisposeObject(dynamicFont);
    }

    private static void DisposeObject(Object value)
    {
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
