using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragonApp : MonoBehaviour
{
    ProfileData profile;
    Voice voice;
    Font font;
    Canvas canvas;
    RectTransform root;
    RectTransform body;
    Text levelText;
    Text moodText;
    Text strengthText;
    Text scoreText;
    Text characterText;
    Text animText;
    Text captionText;
    RawImage bgImage;
    RawImage characterImage;
    UiMotion characterMotion;
    UiMotion captionMotion;
    string lastPose = "wave";
    InputField typeField;
    string talkMode = "";
    string lastQuestion = "";
    string shopType = "glasses";
    bool shopItems = true;
    int lastTopic = 1;
    int chosenTopicId = 1;
    Coroutine englishLoad;
    int badStreak;
    int unanswered;
    int touchStreak;
    bool scoreCenturyPaid;
    bool conversationLocked;
    bool lowWarned;
    bool pendingGreet;
    bool englishWasReady;
    Coroutine pendingAsk;
    string lineBuffer = "";
    InputField nameField;
    InputField dobField;
    Button twoWayButton;
    Button oneWayButton;
    Button startButton;
    Button captionButton;
    Button characterHitButton;
    RectTransform micImpulseRoot;
    Image[] micBars;
    Text micImpulseLabel;

    static readonly Color Ink = new Color(0.18f, 0.24f, 0.30f, 1f);
    static readonly Color Cream = new Color(1f, 0.97f, 0.93f, 0.94f);
    static readonly Color Coral = new Color(0.91f, 0.42f, 0.22f, 1f);
    static readonly Color Teal = new Color(0.18f, 0.55f, 0.58f, 1f);
    static readonly Color SoftTeal = new Color(0.78f, 0.9f, 0.91f, 1f);
    static readonly Color Dock = new Color(1f, 0.98f, 0.95f, 0.97f);
    static readonly Color Stage = new Color(0.97f, 0.945f, 0.91f, 1f);

    static readonly string[] DoubtLines =
    {
        "Is this sentence right?",
        "I don't get you.",
        "I don't get you, can you repeat?"
    };

    static readonly string[] MissingLines =
    {
        "I don't get you. Can you ask another question?",
        "Well, I don't get you. Please ask another question?",
        "I am sorry, but please ask another question?"
    };

    void Awake()
    {
        profile = SaveGame.LoadProfile();
        RollStats();
        voice = gameObject.AddComponent<Voice>();
        voice.SetSpeaker(profile.speakerSex);
        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        BuildCanvas();
        EnsureEnglish();
        if (!SaveGame.HasSerial()) ShowSerial();
        else if (!ProfileReady()) ShowProfile(true);
        else EnterTalk(true);
    }

    void Update()
    {
        bool ready = EnglishBank.Ready;
        if (ready != englishWasReady)
        {
            englishWasReady = ready;
            ApplyTalkInteractable();
            if (ready && pendingGreet && !conversationLocked)
            {
                pendingGreet = false;
                Say("Welcome to GOGO English Conversation Session. My name is GOGO. Please ask questions in English.", "bow");
            }
            else if (ready)
                RefreshCaptionAfterLoad();
        }

        UpdateMicImpulse();

        if (captionText == null) return;

        if (voice != null && voice.Listening)
        {
#if UNITY_EDITOR || !UNITY_ANDROID
            if (typeField != null) voice.SetLiveTyped(typeField.text);
#endif
            string live = voice.LiveCaption;
            if (!string.IsNullOrEmpty(live))
            {
                captionText.text = "You: " + live;
            }
            else if (voice.UsesTypedInput)
            {
                if (voice.VoiceHeard)
                    captionText.text = "● Mic hears you — type English, then release";
                else if (voice.MicOpen || voice.MicSeen)
                    captionText.text = "● Mic on — type English here, then release";
                else
                    captionText.text = "● Type English in the box, then release";
            }
            else if (voice.VoiceHeard)
                captionText.text = "● Hearing you… keep speaking";
            else if (!string.IsNullOrEmpty(voice.MicStatus) && voice.MicStatus != "Mic on — speak" && voice.MicStatus != "Mic on — speak English" && voice.MicStatus != "Opening mic…")
                captionText.text = "● " + voice.MicStatus;
            else if (!voice.MicOpen)
                captionText.text = "● No mic — type English, then release";
            else
                captionText.text = "● Mic on — speak English now";
            return;
        }

        if (!ready)
            captionText.text = EnglishBank.Status;
    }

    void RefreshCaptionAfterLoad()
    {
        if (captionText == null) return;
        if (!string.IsNullOrEmpty(lineBuffer) && profile.caption)
            captionText.text = lineBuffer;
        else if (captionText.text.StartsWith("Opening English") || captionText.text == EnglishBank.Status)
            captionText.text = "";
    }

    bool TalkBlocked()
    {
        return conversationLocked || !EnglishBank.Ready;
    }

    void ApplyTalkInteractable()
    {
        bool on = !TalkBlocked();
        SetButtonOn(twoWayButton, on);
        SetButtonOn(oneWayButton, on);
        SetButtonOn(startButton, on);
        SetButtonOn(captionButton, on);
        SetButtonOn(characterHitButton, on);
        if (typeField != null) typeField.interactable = on;
        if (!on && captionText != null && !EnglishBank.Ready)
            captionText.text = EnglishBank.Status;
    }

    static void SetButtonOn(Button button, bool on)
    {
        if (button == null) return;
        button.interactable = on;
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            Color baseColor = image.color;
            baseColor.a = on ? 1f : 0.4f;
            image.color = baseColor;
            var colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.14f);
            colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f);
            button.colors = colors;
        }
    }

    void EnsureEnglish()
    {
        var level = Mathf.Clamp(profile.level, 1, 6);
        if (EnglishBank.Ready && EnglishBank.Level == level) return;

        if (englishLoad != null)
        {
            StopCoroutine(englishLoad);
            englishLoad = null;
            EnglishBank.Abort();
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        englishLoad = StartCoroutine(EnglishBank.Load(level));
#else
        EnglishBank.LoadNow(level);
        englishWasReady = EnglishBank.Ready;
#endif
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) RecordToday(false);
    }

    void OnApplicationQuit()
    {
        RecordToday(true);
    }

    void RollStats()
    {
        var now = DateTime.UtcNow;
        if (string.IsNullOrEmpty(profile.lastStatReset)) profile.lastStatReset = now.ToString("o");
        DateTime last;
        if (DateTime.TryParse(profile.lastStatReset, out last) && (now - last).TotalHours >= 24)
        {
            profile.mood = 50;
            profile.strength = 50;
            profile.lastStatReset = now.ToString("o");
        }
        var day = now.ToString("yyyy/MM/dd");
        if (profile.today != day)
        {
            profile.today = day;
            profile.talksToday = 0;
            profile.topScoreToday = profile.score;
        }
        profile.mood = Mathf.Max(0, profile.mood);
        profile.strength = Mathf.Max(0, profile.strength);
        profile.score = Mathf.Clamp(profile.score, 1, 100);
    }

    void RecordToday(bool resetScore)
    {
        if (profile.talksToday > 0)
        {
            var row = profile.history.Find(item => item.date == profile.today);
            if (row == null)
            {
                row = new HistoryRow { date = profile.today };
                profile.history.Insert(0, row);
            }
            row.conversations = profile.talksToday;
            row.score = Mathf.Max(row.score, profile.topScoreToday);
        }
        if (resetScore) profile.score = 0;
        SaveGame.SaveProfile(profile);
    }

    bool ProfileReady()
    {
        return !GrammarCheck.IsBlank(profile.name) && !GrammarCheck.IsBlank(profile.dob);
    }

    void BuildCanvas()
    {
        if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
        {
            var events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<StandaloneInputModule>();
        }
        var canvasGo = new GameObject("UI");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        root = canvasGo.GetComponent<RectTransform>();
        bgImage = Backdrop(root, ArtDb.Background("classroom"), Vector2.zero, Vector2.one);
        body = Panel(root, Vector2.zero, Vector2.one, new Color(0, 0, 0, 0));
        body.GetComponent<Image>().raycastTarget = false;
        var bar = SoftCard(root, new Vector2(0.04f, 0.905f), new Vector2(0.96f, 0.985f), Cream);
        levelText = Label(bar, "Lv 1", 28, TextAnchor.MiddleCenter, Ink);
        SetRect(levelText.rectTransform, new Vector2(0.02f, 0.48f), new Vector2(0.2f, 0.95f));
        moodText = Label(bar, "Mood 50", 26, TextAnchor.MiddleCenter, Ink);
        SetRect(moodText.rectTransform, new Vector2(0.2f, 0.48f), new Vector2(0.42f, 0.95f));
        strengthText = Label(bar, "PS 50", 26, TextAnchor.MiddleCenter, Ink);
        SetRect(strengthText.rectTransform, new Vector2(0.42f, 0.48f), new Vector2(0.62f, 0.95f));
        scoreText = Label(bar, "Score 1  ·  ◆ 50", 24, TextAnchor.MiddleLeft, Ink);
        SetRect(scoreText.rectTransform, new Vector2(0.04f, 0.05f), new Vector2(0.72f, 0.48f));
        var menu = SoftButton(bar, "Menu", Coral, 26);
        SetRect(menu.GetComponent<RectTransform>(), new Vector2(0.74f, 0.18f), new Vector2(0.97f, 0.82f));
        menu.onClick.AddListener(ShowMenu);
    }

    void RefreshStats()
    {
        levelText.text = "Lv " + profile.level;
        moodText.text = "Mood " + profile.mood;
        strengthText.text = "PS " + profile.strength;
        scoreText.text = "Score " + profile.score + "  ·  ◆ " + profile.diamonds;
        PaintBackground();
    }

    void PaintBackground()
    {
        if (bgImage == null) return;
        Texture2D texture = ArtDb.Background(profile.backgroundId);
        if (texture != null)
        {
            bgImage.texture = texture;
            bgImage.color = Color.white;
        }
        else
        {
            bgImage.texture = null;
            if (profile.backgroundId == "night") bgImage.color = new Color(0.12f, 0.14f, 0.2f);
            else if (profile.backgroundId == "park") bgImage.color = new Color(0.86f, 0.95f, 0.84f);
            else bgImage.color = new Color(0.91f, 0.96f, 1f);
        }
    }

    void ClearBody()
    {
        for (var i = body.childCount - 1; i >= 0; i--) Destroy(body.GetChild(i).gameObject);
        characterText = null;
        animText = null;
        captionText = null;
        characterImage = null;
        characterMotion = null;
        captionMotion = null;
        typeField = null;
        nameField = null;
        dobField = null;
        twoWayButton = null;
        oneWayButton = null;
        startButton = null;
        captionButton = null;
        characterHitButton = null;
        micImpulseRoot = null;
        micBars = null;
        micImpulseLabel = null;
    }

    void EnterTalk(bool greet)
    {
        conversationLocked = profile.strength < 10 || profile.mood < 10;
        pendingGreet = greet && !conversationLocked;
        englishWasReady = EnglishBank.Ready;
        ShowTalk();
        RefreshStats();
        if (conversationLocked)
        {
            pendingGreet = false;
            Say("Sorry, I am too tired to continue the conversation. Please excuse me. See you tomorrow.", "leave");
            return;
        }
        if (greet && EnglishBank.Ready)
        {
            pendingGreet = false;
            Say("Welcome to GOGO English Conversation Session. My name is GOGO. Please ask questions in English.", "bow");
        }
    }

    void ShowTalk()
    {
        ClearBody();
        var face = profile.characterId == "minion" ? "Minion" : "Little Dragon";

        // Character hero — cream stage matches production art background
        SoftCard(body, new Vector2(0.12f, 0.4f), new Vector2(0.88f, 0.88f), Stage);
        characterImage = Backdrop(body, ArtDb.Character(profile.characterId, lastPose), new Vector2(0.14f, 0.42f), new Vector2(0.86f, 0.9f));
        characterImage.raycastTarget = false;
        characterMotion = characterImage.gameObject.AddComponent<UiMotion>();
        characterMotion.bobAmount = 10f;
        characterMotion.bobSpeed = 1.4f;
        characterMotion.SetBob(true);

        characterHitButton = SoftButton(body, "", new Color(1, 1, 1, 0.01f), 24);
        SetRect(characterHitButton.GetComponent<RectTransform>(), new Vector2(0.14f, 0.42f), new Vector2(0.86f, 0.9f));
        characterHitButton.onClick.AddListener(TouchCharacter);

        var nameChip = SoftCard(body, new Vector2(0.3f, 0.365f), new Vector2(0.7f, 0.415f), Cream);
        characterText = Label(nameChip, face, 30, TextAnchor.MiddleCenter, Ink);
        characterText.fontStyle = FontStyle.Bold;
        animText = Label(body, AnimationName(lastPose), 22, TextAnchor.MiddleCenter, new Color(0.3f, 0.36f, 0.4f, 0.9f));
        SetRect(animText.rectTransform, new Vector2(0.2f, 0.33f), new Vector2(0.8f, 0.365f));

        // Single bottom sheet
        SoftCard(body, new Vector2(0.035f, 0.02f), new Vector2(0.965f, 0.32f), Dock);

        var speech = SoftCard(body, new Vector2(0.08f, 0.215f), new Vector2(0.92f, 0.3f), Color.white);
        captionText = Label(speech, "", 28, TextAnchor.MiddleLeft, Ink);
        SetRect(captionText.rectTransform, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f));
        captionMotion = speech.gameObject.AddComponent<UiMotion>();

#if UNITY_ANDROID && !UNITY_EDITOR
        typeField = SoftInput(body, "Type English, then hold Start", new Vector2(0.08f, 0.145f), new Vector2(0.92f, 0.2f));
#else
        typeField = SoftInput(body, "Type English here, then hold Start", new Vector2(0.08f, 0.145f), new Vector2(0.92f, 0.2f));
#endif

        Color twoColor = talkMode == "two" ? Teal : SoftTeal;
        Color oneColor = talkMode == "one" ? Teal : SoftTeal;
        Color twoText = talkMode == "two" ? Color.white : Ink;
        Color oneText = talkMode == "one" ? Color.white : Ink;

        twoWayButton = SoftButton(body, "Two way", twoColor, 26);
        SetRect(twoWayButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0.07f), new Vector2(0.32f, 0.125f));
        SetButtonLabelColor(twoWayButton, twoText);
        twoWayButton.onClick.AddListener(() => { if (TalkBlocked()) return; talkMode = "two"; ShowTalk(); });

        startButton = SoftButton(body, "Hold to talk", Coral, 28);
        SetRect(startButton.GetComponent<RectTransform>(), new Vector2(0.35f, 0.06f), new Vector2(0.65f, 0.135f));
        var hold = startButton.gameObject.AddComponent<HoldButton>();
        hold.App = this;
        BuildMicImpulse(body);

        oneWayButton = SoftButton(body, "One way", oneColor, 26);
        SetRect(oneWayButton.GetComponent<RectTransform>(), new Vector2(0.68f, 0.07f), new Vector2(0.92f, 0.125f));
        SetButtonLabelColor(oneWayButton, oneText);
        oneWayButton.onClick.AddListener(() => { if (TalkBlocked()) return; talkMode = "one"; StopAsk(); ShowTalk(); });

        captionButton = SoftButton(body, profile.caption ? "Caption on" : "Caption off", new Color(0.42f, 0.48f, 0.52f, 1f), 24);
        SetRect(captionButton.GetComponent<RectTransform>(), new Vector2(0.3f, 0.028f), new Vector2(0.7f, 0.06f));
        captionButton.onClick.AddListener(() => { if (TalkBlocked()) return; profile.caption = !profile.caption; SaveGame.SaveProfile(profile); ShowTalk(); });

        if (!EnglishBank.Ready)
            captionText.text = EnglishBank.Status;
        else if (captionText != null)
            captionText.text = profile.caption ? lineBuffer : "";
        if (captionMotion != null && profile.caption && !string.IsNullOrEmpty(lineBuffer) && EnglishBank.Ready)
            captionMotion.FadeIn(0.25f);
        ApplyTalkInteractable();
    }

    public void PressStart()
    {
        if (TalkBlocked()) return;
        StopAsk();
        voice.Stop();
        if (talkMode == "") talkMode = "one";
#if UNITY_ANDROID && !UNITY_EDITOR
        RequestMicPermission();
#endif
        voice.BeginListen();
        if (animText != null) animText.text = "Listening";
        if (captionText != null)
        {
            captionText.text = voice.UsesTypedInput
                ? "● Mic on — type English, then release"
                : "● Listening… speak English now";
        }
        ShowMicImpulse(true);
        if (voice.UsesTypedInput && typeField != null)
            typeField.ActivateInputField();
    }

    public void ReleaseStart()
    {
        if (TalkBlocked()) return;
        StartCoroutine(FinishListen());
    }

    void BuildMicImpulse(Transform parent)
    {
        micImpulseRoot = SoftCard(parent, new Vector2(0.22f, 0.138f), new Vector2(0.78f, 0.208f), new Color(0.12f, 0.18f, 0.22f, 0.95f));
        micImpulseRoot.gameObject.name = "MicImpulse";
        micImpulseLabel = Label(micImpulseRoot, "Mic 0%", 20, TextAnchor.MiddleLeft, new Color(0.9f, 0.95f, 0.97f, 1f));
        SetRect(micImpulseLabel.rectTransform, new Vector2(0.03f, 0.1f), new Vector2(0.42f, 0.9f));

        const int barCount = 12;
        micBars = new Image[barCount];
        for (int i = 0; i < barCount; i++)
        {
            var go = new GameObject("Bar" + i);
            go.transform.SetParent(micImpulseRoot, false);
            var rect = go.AddComponent<RectTransform>();
            float x0 = 0.44f + i * 0.045f;
            rect.anchorMin = new Vector2(x0, 0.18f);
            rect.anchorMax = new Vector2(x0 + 0.035f, 0.82f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.35f, 0.85f, 0.72f, 1f);
            image.raycastTarget = false;
            micBars[i] = image;
        }
        ShowMicImpulse(false);
    }

    void ShowMicImpulse(bool on)
    {
        if (micImpulseRoot != null) micImpulseRoot.gameObject.SetActive(on);
    }

    void UpdateMicImpulse()
    {
        bool listening = voice != null && voice.Listening;
        if (micImpulseRoot != null && micImpulseRoot.gameObject.activeSelf != listening)
            ShowMicImpulse(listening);
        if (!listening || micBars == null) return;

        float level = voice.MicLevel;
        int pct = Mathf.RoundToInt(level * 100f);
        if (micImpulseLabel != null)
        {
            string status = voice.MicStatus;
            if (string.IsNullOrEmpty(status)) status = "Mic";
            micImpulseLabel.text = status + "  " + pct + "%";
            micImpulseLabel.color = voice.VoiceHeard
                ? new Color(0.45f, 1f, 0.7f, 1f)
                : new Color(0.9f, 0.95f, 0.97f, 1f);
        }

        for (int i = 0; i < micBars.Length; i++)
        {
            float gate = (i + 1) / (float)micBars.Length;
            float phase = Mathf.Abs(Mathf.Sin(Time.time * (12f + i * 0.7f) + i));
            float lit = level >= gate * 0.55f ? Mathf.Clamp01(level * (0.5f + 0.5f * phase)) : level * 0.15f;
            float height = Mathf.Lerp(0.16f, 1f, lit);
            var rect = micBars[i].rectTransform;
            float mid = 0.5f;
            float half = height * 0.34f;
            float x0 = 0.44f + i * 0.045f;
            rect.anchorMin = new Vector2(x0, mid - half);
            rect.anchorMax = new Vector2(x0 + 0.035f, mid + half);
            float hot = Mathf.Clamp01((level - 0.25f) / 0.75f);
            micBars[i].color = Color.Lerp(
                new Color(0.3f, 0.9f, 0.75f, 1f),
                new Color(1f, 0.5f, 0.15f, 1f),
                hot);
        }

        if (startButton != null)
        {
            float pulse = 1f + level * 0.12f;
            startButton.transform.localScale = new Vector3(pulse, pulse, 1f);
        }
    }

    IEnumerator FinishListen()
    {
        bool hadMic = voice.MicSeen || voice.MicOpen;
        bool heard = voice.VoiceHeard;
        bool speechOn = voice.SpeechReady;
        string liveBefore = voice.LiveCaption;

        // Soft-stop: keep speech callbacks so final words can still arrive.
        voice.EndListen();
        ShowMicImpulse(false);
        if (startButton != null) startButton.transform.localScale = Vector3.one;
        if (captionText != null) captionText.text = "● Getting your words…";

        var wait = 0f;
        while (wait < 2.5f && string.IsNullOrEmpty(voice.LastHeard) && string.IsNullOrEmpty(voice.LiveCaption))
        {
            wait += Time.deltaTime;
            yield return null;
        }
        // One extra frame for pendingLive/pendingHeard to flush in Voice.Update.
        yield return null;

        var typed = typeField != null ? typeField.text : "";
        var live = !string.IsNullOrEmpty(voice.LiveCaption) ? voice.LiveCaption : liveBefore;
        var text = !string.IsNullOrEmpty(voice.LastHeard)
            ? voice.LastHeard
            : (!string.IsNullOrEmpty(live) ? live : typed);

        voice.DisposeListen();

        if (typeField != null) typeField.text = "";
        if (string.IsNullOrEmpty(text))
        {
            string hint;
            string status = voice.MicStatus;
            bool stated = !string.IsNullOrEmpty(status)
                && status != "Mic on — speak"
                && status != "Mic on — speak English"
                && status != "Opening mic…";
            if (stated)
                hint = status;
            else if (voice.UsesTypedInput)
            {
                if (heard)
                    hint = "Mic works (bars moved). On PC type English in the box, then hold Start.";
                else if (hadMic)
                    hint = "Mic is ready. Type an English sentence in the box, then hold Start.";
                else
                    hint = "No mic found. Type English in the box, then hold Start.";
            }
            else if (!hadMic)
                hint = "Cannot open the mic. Allow mic permission, or type.";
            else if (!speechOn)
                hint = "Mic works, but speech is off. Check mic permission, or type.";
            else if (heard)
                hint = "Heard sound, but no words yet. Speak clearer English, or type.";
            else
                hint = "No words yet. Hold Start, speak English, then release — or type.";
            if (captionText != null)
                captionText.text = hint;
            if (animText != null) animText.text = AnimationName(lastPose);
            if (typeField != null) typeField.ActivateInputField();
            yield break;
        }
        if (captionText != null) captionText.text = "You: " + text;
        OnUserText(text);
    }

    void OnUserText(string text)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0) return;
        if (TalkBlocked()) return;
        unanswered = 0;
        StopAsk();
        if (profile.caption) SetLine("You: " + text);
        if (!GrammarCheck.Accept(text))
        {
            badStreak++;
            var before = profile.score - 3;
            profile.score = Mathf.Max(1, profile.score - 3);
            if (before < 0) profile.strength = Mathf.Max(0, profile.strength - 2);
            // Never scold for a short hello-style line — coach a full sentence instead.
            string doubt = GrammarCheck.Coach(text);
            if (badStreak >= 10)
                doubt = "Please try a full sentence, like: How are you?";
            else if (badStreak >= 5)
                doubt = DoubtLines[badStreak % DoubtLines.Length];
            Say(doubt, "unsure");
            AfterChange();
            return;
        }
        badStreak = 0;
        if (profile.score >= 100) profile.strength += 2;
        else profile.score = Mathf.Min(100, profile.score + 5);
        if (profile.score >= 100 && !scoreCenturyPaid)
        {
            scoreCenturyPaid = true;
            profile.strength += 10;
            profile.mood += 10;
            Say("Your English is very good.", "thumb");
        }
        NoteQuestion(text);
        profile.talks++;
        profile.talksToday++;
        profile.topScoreToday = Mathf.Max(profile.topScoreToday, profile.score);
        GrowByTalkCount(profile.talksToday);
        if (GrammarCheck.IsGreeting(text))
        {
            Say("Hello! Please ask me a question in English.", "wave");
        }
        else if (!EnglishBank.Ready)
        {
            Say(EnglishBank.Status, "unsure");
        }
        else
        {
            string answer;
            int topic;
            if (!EnglishBank.TryAnswer(text, out answer, out topic))
            {
                Say(MissingLines[profile.talks % MissingLines.Length], profile.talks % 2 == 0 ? "unsure" : "sorry");
            }
            else
            {
                lastTopic = topic;
                Say(answer, "nod");
            }
        }
        if (talkMode == "two") pendingAsk = StartCoroutine(AskLater(lastTopic));
        if (profile.talksToday == 10) Say("Very good. Your English is good.", "smile");
        if (profile.talksToday >= 50 && DateTime.Now.Hour >= 22) Say("I am very tired.", "sleep");
        AfterChange();
    }

    void GrowByTalkCount(int count)
    {
        if (count >= 5 && count <= 7)
        {
            profile.strength += 2;
            profile.mood += 2;
        }
        else if (count >= 8 && count <= 10)
        {
            profile.strength += 3;
            profile.mood += 3;
        }
        else if (count > 10)
        {
            profile.strength = Mathf.Max(0, profile.strength - 5);
        }
    }

    void NoteQuestion(string text)
    {
        var key = text.ToLower();
        if (key == profile.lastQuestion) profile.sameQuestionCount++;
        else
        {
            profile.lastQuestion = key;
            profile.sameQuestionCount = 1;
        }
        if (profile.sameQuestionCount > 3) profile.mood = Mathf.Max(0, profile.mood - 3);
    }

    IEnumerator AskLater(int topic)
    {
        yield return new WaitForSeconds(3f);
        if (talkMode != "two" || conversationLocked) yield break;
        unanswered++;
        if (unanswered > 5)
        {
            Say("Good bye.", "leave");
            Say("I am very tired.", "sleep");
            yield break;
        }
        var question = EnglishBank.Question(topic, lastQuestion);
        lastQuestion = question;
        lastTopic = topic;
        Say(question, "talk");
        pendingAsk = StartCoroutine(AskLater(topic));
    }

    void StopAsk()
    {
        if (pendingAsk == null) return;
        StopCoroutine(pendingAsk);
        pendingAsk = null;
    }

    void Say(string text, string animation)
    {
        lastPose = animation;
        lineBuffer = "GOGO: " + text;
        if (captionText != null) captionText.text = profile.caption ? lineBuffer : "";
        if (animText != null) animText.text = AnimationName(animation);
        if (characterImage != null)
        {
            Texture2D pose = ArtDb.Character(profile.characterId, animation);
            if (pose != null) characterImage.texture = pose;
        }
        if (characterMotion != null) characterMotion.Pulse();
        if (captionMotion != null && profile.caption) captionMotion.FadeIn(0.28f);
        voice.Speak(text);
        if (profile.strength >= 30 && profile.mood >= 30) lowWarned = false;
        if (!lowWarned && profile.strength >= 10 && profile.mood >= 10 && (profile.strength < 30 || profile.mood < 30))
        {
            lowWarned = true;
            voice.Speak(profile.strength <= profile.mood ? "I am hungry." : "I am tired.");
            if (animText != null) animText.text = profile.strength <= profile.mood ? "Hungry" : "Tired";
        }
        if (profile.strength < 10 || profile.mood < 10)
        {
            if (!conversationLocked)
            {
                voice.Speak("Sorry, I am too tired to continue the conversation. Please excuse me. See you tomorrow.");
            }
            conversationLocked = true;
            if (animText != null) animText.text = "Leaves";
        }
        else conversationLocked = false;
        SaveGame.SaveProfile(profile);
        RefreshStats();
    }

    void AfterChange()
    {
        if (profile.strength < 10 || profile.mood < 10) conversationLocked = true;
        SaveGame.SaveProfile(profile);
        RefreshStats();
    }

    void SetLine(string text)
    {
        lineBuffer = text;
        if (captionText != null && profile.caption) captionText.text = text;
    }

    void TouchCharacter()
    {
        if (TalkBlocked()) return;
        touchStreak++;
        if (touchStreak >= 4) Say("I am angry. I do not want to talk to you.", "angry");
        else if (touchStreak >= 3) Say("Don't touch.", "no");
        else Say("What's up?", "unsure");
    }

    static string AnimationName(string id)
    {
        switch (id)
        {
            case "wave": return "Waves hello";
            case "bow": return "Bows";
            case "unsure": return "Looks unsure";
            case "sorry": return "Looks sorry";
            case "thumb": return "Thumbs up";
            case "smile": return "Smiles";
            case "no": return "Shakes head";
            case "thanks": return "Bows thank you";
            case "sleep": return "Falls asleep";
            case "hungry": return "Bends, hungry";
            case "leave": return "Leaves";
            case "nod": return "Nods";
            case "angry": return "Angry";
            case "talk": return "Talking";
            default: return "Hello";
        }
    }

    void ShowMenu()
    {
        Popup("Menu", card =>
        {
            var names = new[] { "Conversation", "Topics", "Shop", "Diamond Shop", "Setting", "History", "Help" };
            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var button = Button(card, name, i == 0 ? new Color(0.2f, 0.55f, 0.35f) : new Color(0.95f, 0.6f, 0.25f));
                SetRect(button.GetComponent<RectTransform>(), new Vector2(0.08f, 0.74f - i * 0.09f), new Vector2(0.92f, 0.82f - i * 0.09f));
                button.onClick.AddListener(() =>
                {
                    ClosePopup();
                    OpenPage(name);
                });
            }
        });
    }

    void OpenPage(string name)
    {
        if (name == "Conversation") EnterTalk(true);
        else if (name == "Topics") ShowTopics();
        else if (name == "Shop") ShowShop();
        else if (name == "Diamond Shop") ShowDiamonds();
        else if (name == "Setting") ShowSettings(false);
        else if (name == "History") ShowHistory();
        else ShowHelp();
    }

    void ShowTopics()
    {
        ClearBody();
        RefreshStats();
        if (profile.level < 2)
        {
            Alert("Topics open from level 2.");
            return;
        }
        if (!EnglishBank.Ready)
        {
            Alert(EnglishBank.Status);
            return;
        }
        var topicTitle = Label(body, "Topic: " + EnglishBank.TopicName(chosenTopicId), 40, TextAnchor.MiddleCenter, Color.black);
        SetRect(topicTitle.rectTransform, new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.9f));
        captionText = Label(body, "", 34, TextAnchor.UpperLeft, Color.black);
        SetRect(captionText.rectTransform, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.7f));
        var topics = Button(body, "Topics", new Color(0.2f, 0.45f, 0.75f));
        SetRect(topics.GetComponent<RectTransform>(), new Vector2(0.04f, 0.08f), new Vector2(0.32f, 0.16f));
        topics.onClick.AddListener(PickTopic);
        var start = Button(body, "Start", new Color(0.9f, 0.45f, 0.2f));
        SetRect(start.GetComponent<RectTransform>(), new Vector2(0.36f, 0.08f), new Vector2(0.64f, 0.16f));
        start.onClick.AddListener(() => StartCoroutine(ReadTopic()));
        var caption = Button(body, "Caption", new Color(0.35f, 0.4f, 0.5f));
        SetRect(caption.GetComponent<RectTransform>(), new Vector2(0.68f, 0.08f), new Vector2(0.96f, 0.16f));
        caption.onClick.AddListener(() => { profile.caption = !profile.caption; SaveGame.SaveProfile(profile); });
    }

    void PickTopic()
    {
        Popup("Topics", card =>
        {
            var content = Scroll(card, new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.8f));
            var names = EnglishBank.TopicNames;
            var shown = 0;
            for (var i = 1; i < names.Length; i++)
            {
                if (string.IsNullOrEmpty(names[i])) continue;
                var id = i;
                var title = names[i];
                var button = Button(content, title, new Color(0.2f, 0.45f, 0.75f), 26);
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.04f, 1);
                rect.anchorMax = new Vector2(0.96f, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.sizeDelta = new Vector2(0, 64);
                rect.anchoredPosition = new Vector2(0, -shown * 72);
                button.onClick.AddListener(() => chosenTopicId = id);
                shown++;
            }
            content.sizeDelta = new Vector2(0, shown * 72);
            var set = Button(card, "Setting", new Color(0.2f, 0.65f, 0.35f));
            SetRect(set.GetComponent<RectTransform>(), new Vector2(0.25f, 0.04f), new Vector2(0.75f, 0.14f));
            set.onClick.AddListener(() => { ClosePopup(); ShowTopics(); });
        });
    }

    IEnumerator ReadTopic()
    {
        var sentences = EnglishBank.Script(chosenTopicId, 4);
        if (sentences.Length == 0)
        {
            Alert(EnglishBank.Ready ? "No lines for this topic yet." : EnglishBank.Status);
            yield break;
        }
        foreach (var sentence in sentences)
        {
            if (profile.caption && captionText != null) captionText.text = sentence;
            if (animText != null) animText.text = "Talking";
            voice.Speak(sentence);
            yield return new WaitForSeconds(2.2f);
        }
    }

    void ShowShop()
    {
        ClearBody();
        RefreshStats();
        var items = Button(body, "Item List", shopItems ? new Color(0.2f, 0.65f, 0.35f) : new Color(0.95f, 0.55f, 0.2f));
        SetRect(items.GetComponent<RectTransform>(), new Vector2(0.04f, 0.9f), new Vector2(0.48f, 0.97f));
        items.onClick.AddListener(() => { shopItems = true; ShowShop(); });
        var owned = Button(body, "Purchased List", shopItems ? new Color(0.95f, 0.55f, 0.2f) : new Color(0.2f, 0.65f, 0.35f));
        SetRect(owned.GetComponent<RectTransform>(), new Vector2(0.52f, 0.9f), new Vector2(0.96f, 0.97f));
        owned.onClick.AddListener(() => { shopItems = false; ShowShop(); });
        if (shopItems) ShowItemList();
        else ShowOwned();
    }

    void ShowItemList()
    {
        for (var i = 0; i < ContentDb.ItemTypes.Length; i++)
        {
            var type = ContentDb.ItemTypes[i];
            var button = Button(body, type, type == shopType ? new Color(0.25f, 0.45f, 0.85f) : new Color(0.95f, 0.55f, 0.2f));
            var y = 0.8f - i * 0.09f;
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0.02f, y), new Vector2(0.28f, y + 0.08f));
            var pick = type;
            button.onClick.AddListener(() => { shopType = pick; ShowShop(); });
        }
        var catalog = ContentDb.Items();
        var shown = 0;
        foreach (var item in catalog)
        {
            if (item.type != shopType) continue;
            var top = 0.78f - shown * 0.18f;
            var card = Panel(body, new Vector2(0.32f, top), new Vector2(0.98f, top + 0.16f), new Color(1, 1, 1, 0.9f));
            Label(card, item.name + "\nDiamond: " + item.price + "   PS +" + item.strength + "   Mood +" + item.mood, 26, TextAnchor.UpperLeft, Color.black);
            var buy = Button(card, "Buy", new Color(0.2f, 0.65f, 0.35f));
            SetRect(buy.GetComponent<RectTransform>(), new Vector2(0.05f, 0.05f), new Vector2(0.45f, 0.35f));
            var preview = Button(card, "Preview", new Color(0.2f, 0.45f, 0.75f));
            SetRect(preview.GetComponent<RectTransform>(), new Vector2(0.55f, 0.05f), new Vector2(0.95f, 0.35f));
            var copy = item;
            buy.onClick.AddListener(() => BuyItem(copy));
            preview.onClick.AddListener(() => Alert(copy.name + "\nDiamond " + copy.price + "\nPS +" + copy.strength + "\nMood +" + copy.mood));
            shown++;
        }
    }

    void ShowOwned()
    {
        var wornTitle = Label(body, profile.characterId == "minion" ? "Minion is wearing purchased items" : "Little Dragon is wearing purchased items", 30, TextAnchor.MiddleCenter, Color.black);
        SetRect(wornTitle.rectTransform, new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.88f));
        var shown = 0;
        foreach (var item in ContentDb.Items())
        {
            if (!profile.ownedItems.Contains(item.id)) continue;
            var worn = profile.wornItems.Contains(item.id);
            var top = 0.72f - shown * 0.12f;
            var use = Button(body, item.name + (worn ? "  USE" : "  NO USE"), worn ? new Color(0.25f, 0.45f, 0.85f) : new Color(0.95f, 0.55f, 0.2f));
            SetRect(use.GetComponent<RectTransform>(), new Vector2(0.1f, top), new Vector2(0.9f, top + 0.1f));
            var id = item.id;
            use.onClick.AddListener(() =>
            {
                if (profile.wornItems.Contains(id)) profile.wornItems.Remove(id);
                else profile.wornItems.Add(id);
                SaveGame.SaveProfile(profile);
                ShowShop();
            });
            shown++;
        }
    }

    void BuyItem(ShopItem item)
    {
        if (profile.ownedItems.Contains(item.id))
        {
            Alert("Purchased!");
            return;
        }
        if (profile.diamonds < item.price)
        {
            Alert("You are lack of diamond. Please buy diamond.");
            return;
        }
        Confirm("Are you sure to buy?", () =>
        {
            profile.diamonds -= item.price;
            profile.ownedItems.Add(item.id);
            profile.strength += item.strength;
            profile.mood += item.mood;
            profile.purchases.Insert(0, new PurchaseRow
            {
                date = DateTime.Now.ToString("yyyy/MM/dd"),
                diamonds = item.price,
                item = item.name
            });
            var line = profile.strength >= 100 && profile.mood >= 100 ? "Good. Thank you." : "Thank you.";
            Say(line, "thanks");
            Alert("Purchased!");
            ShowShop();
        });
    }

    void ShowDiamonds()
    {
        ClearBody();
        RefreshStats();
        var shopTitle = Label(body, "Diamond Shop", 42, TextAnchor.MiddleCenter, Color.black);
        SetRect(shopTitle.rectTransform, new Vector2(0.1f, 0.9f), new Vector2(0.9f, 0.98f));
        for (var i = 0; i < ContentDb.DiamondPacks.Length; i++)
        {
            var pack = ContentDb.DiamondPacks[i];
            var col = i % 2;
            var row = i / 2;
            var button = Button(body, pack.diamonds + "\n" + pack.price, new Color(0.55f, 0.75f, 0.95f));
            var x = 0.08f + col * 0.46f;
            var y = 0.74f - row * 0.16f;
            SetRect(button.GetComponent<RectTransform>(), new Vector2(x, y), new Vector2(x + 0.4f, y + 0.14f));
            int amount = pack.diamonds;
            button.onClick.AddListener(() => BuyDiamonds(amount));
        }
        var yHist = 0.22f;
        var historyTitle = Label(body, "History", 30, TextAnchor.MiddleLeft, Color.black);
        SetRect(historyTitle.rectTransform, new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.3f));
        foreach (var row in profile.purchases)
        {
            if (yHist < 0.05f) break;
            var line = Label(body, row.date + "  " + row.item + "  " + row.diamonds, 26, TextAnchor.MiddleLeft, Color.black);
            SetRect(line.rectTransform, new Vector2(0.06f, yHist), new Vector2(0.94f, yHist + 0.05f));
            yHist -= 0.05f;
        }
    }

    void BuyDiamonds(int amount)
    {
        Popup("Buy diamonds", card =>
        {
            Label(card, "IMEI\n" + (PhoneId.Imei() == "" ? "Not available on this device" : PhoneId.Imei()), 28, TextAnchor.UpperLeft, Color.black);
            var serial = Input(card, "Serial number", new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.58f));
            var ok = Button(card, "OK", new Color(0.2f, 0.65f, 0.35f));
            SetRect(ok.GetComponent<RectTransform>(), new Vector2(0.08f, 0.32f), new Vector2(0.45f, 0.42f));
            var cancel = Button(card, "Cancel", new Color(0.7f, 0.3f, 0.3f));
            SetRect(cancel.GetComponent<RectTransform>(), new Vector2(0.55f, 0.32f), new Vector2(0.92f, 0.42f));
            var qr = Button(card, "Check QR code", new Color(0.2f, 0.45f, 0.75f));
            SetRect(qr.GetComponent<RectTransform>(), new Vector2(0.15f, 0.16f), new Vector2(0.85f, 0.26f));
            ok.onClick.AddListener(() => FinishDiamond(serial.text, amount));
            qr.onClick.AddListener(() => FinishDiamond(serial.text, amount));
            cancel.onClick.AddListener(ClosePopup);
        });
    }

    void FinishDiamond(string serial, int amount)
    {
        ClosePopup();
        if ((serial ?? "").Trim() != SaveGame.DiamondPassword)
        {
            Alert("The serial number is incorrect.");
            return;
        }
        profile.diamonds += amount;
        profile.purchases.Insert(0, new PurchaseRow
        {
            date = DateTime.Now.ToString("yyyy/MM/dd"),
            diamonds = amount,
            item = amount + " diamonds"
        });
        SaveGame.SaveProfile(profile);
        Alert("It is purchased properly.");
        ShowDiamonds();
    }

    void ShowHistory()
    {
        ClearBody();
        RefreshStats();
        var historyHeading = Label(body, "History", 48, TextAnchor.MiddleCenter, Color.black);
        SetRect(historyHeading.rectTransform, new Vector2(0.1f, 0.88f), new Vector2(0.9f, 0.97f));
        var y = 0.8f;
        if (profile.history.Count == 0)
        {
            var empty = Label(body, "No score yet. Talk with GOGO, then leave the app to record today.", 32, TextAnchor.MiddleCenter, Color.black);
            SetRect(empty.rectTransform, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.65f));
            return;
        }
        foreach (var row in profile.history)
        {
            if (y < 0.05f) break;
            var line = Label(body, row.date + "  Conversation: " + row.conversations + "  score: " + row.score, 30, TextAnchor.MiddleLeft, Color.black);
            SetRect(line.rectTransform, new Vector2(0.06f, y), new Vector2(0.94f, y + 0.07f));
            y -= 0.08f;
        }
    }

    void ShowHelp()
    {
        ClearBody();
        RefreshStats();
        var help = Label(body, "This is my help screen.\n\nHold Start and speak English, or type a sentence with a capital letter and . ? !\nTwo way: GOGO asks a question 3 seconds after an answer.\nOne way: you ask, GOGO answers.\nTopics start at level 2 and use the age CSV for that level.\nAge 3-5, 6-8, 9-10, 11-12, 13-15, and 15+ are the six English files.\nShop items use diamonds and raise mood and strength.\nIf mood or strength is under 10, GOGO stops until you buy an item.\n\nApp serial: 123456789\nDiamond serial: 1234567890", 32, TextAnchor.UpperLeft, Color.black);
        SetRect(help.rectTransform, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f));
    }

    void ShowSettings(bool first)
    {
        if (nameField != null) profile.name = nameField.text;
        if (dobField != null) profile.dob = dobField.text;
        ClearBody();
        RefreshStats();
        nameField = Input(body, "User name", new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.91f));
        nameField.text = profile.name;
        var male = Button(body, "Sex M", profile.sex == "M" ? new Color(0.2f, 0.65f, 0.35f) : new Color(0.7f, 0.75f, 0.8f));
        SetRect(male.GetComponent<RectTransform>(), new Vector2(0.08f, 0.74f), new Vector2(0.48f, 0.81f));
        var female = Button(body, "Sex F", profile.sex == "F" ? new Color(0.2f, 0.65f, 0.35f) : new Color(0.7f, 0.75f, 0.8f));
        SetRect(female.GetComponent<RectTransform>(), new Vector2(0.52f, 0.74f), new Vector2(0.92f, 0.81f));
        male.onClick.AddListener(() => { profile.sex = "M"; ShowSettings(first); });
        female.onClick.AddListener(() => { profile.sex = "F"; ShowSettings(first); });
        dobField = Input(body, "Date of birth YYYY-MM-DD", new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.71f));
        dobField.text = profile.dob;
        var speakM = Button(body, "Speaker M", profile.speakerSex == "M" ? new Color(0.2f, 0.65f, 0.35f) : new Color(0.7f, 0.75f, 0.8f));
        SetRect(speakM.GetComponent<RectTransform>(), new Vector2(0.08f, 0.54f), new Vector2(0.48f, 0.61f));
        var speakF = Button(body, "Speaker F", profile.speakerSex == "F" ? new Color(0.2f, 0.65f, 0.35f) : new Color(0.7f, 0.75f, 0.8f));
        SetRect(speakF.GetComponent<RectTransform>(), new Vector2(0.52f, 0.54f), new Vector2(0.92f, 0.61f));
        speakM.onClick.AddListener(() => { profile.speakerSex = "M"; voice.SetSpeaker("M"); ShowSettings(first); });
        speakF.onClick.AddListener(() => { profile.speakerSex = "F"; voice.SetSpeaker("F"); ShowSettings(first); });
        AddChoiceRow(0.42f, "Character", ContentDb.Characters, profile.characterId, profile.ownedCharacters, id => profile.characterId = id);
        AddChoiceRow(0.3f, "Background", ContentDb.Backgrounds, profile.backgroundId, profile.ownedBackgrounds, id => profile.backgroundId = id);
        var ages = new[] { "3-5", "6-8", "9-10", "11-12", "13-15", "15+" };
        for (var level = 1; level <= ages.Length; level++)
        {
            var chosen = level;
            var button = Button(body, ages[level - 1], profile.level == level ? new Color(0.2f, 0.65f, 0.35f) : new Color(0.7f, 0.75f, 0.8f), 22);
            var x = 0.03f + (level - 1) * 0.162f;
            SetRect(button.GetComponent<RectTransform>(), new Vector2(x, 0.18f), new Vector2(x + 0.152f, 0.25f));
            button.onClick.AddListener(() => { profile.level = chosen; ShowSettings(first); });
        }
        var caption = Button(body, profile.caption ? "Caption YES" : "Caption NO", new Color(0.2f, 0.45f, 0.75f));
        SetRect(caption.GetComponent<RectTransform>(), new Vector2(0.08f, 0.1f), new Vector2(0.48f, 0.17f));
        caption.onClick.AddListener(() => { profile.caption = !profile.caption; ShowSettings(first); });
        var save = Button(body, "SAVE", new Color(0.9f, 0.45f, 0.2f));
        SetRect(save.GetComponent<RectTransform>(), new Vector2(0.52f, 0.1f), new Vector2(0.92f, 0.17f));
        save.onClick.AddListener(() =>
        {
            if (GrammarCheck.IsBlank(nameField.text) || !IsDate(dobField.text))
            {
                Alert(first ? "Please insert the data correctly." : "Please input data correctly.");
                return;
            }
            profile.name = nameField.text.Trim();
            profile.dob = dobField.text.Trim();
            voice.SetSpeaker(profile.speakerSex);
            SaveGame.SaveProfile(profile);
            EnsureEnglish();
            EnterTalk(first);
        });
    }

    void AddChoiceRow(float y, string title, CatalogItem[] options, string current, List<string> owned, Action<string> set)
    {
        var label = Label(body, title, 28, TextAnchor.MiddleLeft, Color.black);
        SetRect(label.rectTransform, new Vector2(0.06f, y + 0.06f), new Vector2(0.4f, y + 0.1f));
        for (var i = 0; i < options.Length; i++)
        {
            var option = options[i];
            var price = option.price == 0 ? "Free" : option.price.ToString();
            var button = Button(body, option.name + " " + price, current == option.id ? new Color(0.2f, 0.65f, 0.35f) : new Color(0.7f, 0.75f, 0.8f));
            var x = 0.06f + i * 0.32f;
            SetRect(button.GetComponent<RectTransform>(), new Vector2(x, y), new Vector2(x + 0.3f, y + 0.06f));
            button.onClick.AddListener(() => ChooseCosmetic(option.id, option.name, option.price, owned, set));
        }
    }

    void ChooseCosmetic(string id, string name, int price, List<string> owned, Action<string> set)
    {
        if (owned.Contains(id))
        {
            set(id);
            SaveGame.SaveProfile(profile);
            ShowSettings(false);
            return;
        }
        if (profile.diamonds < price)
        {
            Alert("You are lack of diamond. Please buy diamond.");
            return;
        }
        Confirm("Are you sure to buy?", () =>
        {
            profile.diamonds -= price;
            owned.Add(id);
            set(id);
            SaveGame.SaveProfile(profile);
            Alert("Purchased!");
            ShowSettings(false);
        });
    }

    void ShowSerial()
    {
        ClearBody();
        characterImage = Backdrop(body, ArtDb.Character("dragon", "wave"), new Vector2(0.2f, 0.55f), new Vector2(0.8f, 0.9f));
        characterImage.raycastTarget = false;
        characterText = Label(body, "Hello!", 40, TextAnchor.MiddleCenter, new Color(0.1f, 0.3f, 0.5f));
        SetRect(characterText.rectTransform, new Vector2(0.1f, 0.48f), new Vector2(0.9f, 0.56f));
        animText = Label(body, "Waves hello", 28, TextAnchor.MiddleCenter, Color.black);
        SetRect(animText.rectTransform, new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.48f));
        Popup("Serial number", card =>
        {
            var imeiLabel = Label(card, "IMEI", 26, TextAnchor.MiddleLeft, Color.black);
            SetRect(imeiLabel.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.8f));
            var imei = Input(card, "Phone IMEI (optional)", new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.72f));
            imei.text = PhoneId.Imei();
            var serialLabel = Label(card, "Serial number", 26, TextAnchor.MiddleLeft, Color.black);
            SetRect(serialLabel.rectTransform, new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.58f));
            var serial = Input(card, "Enter 123456789", new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.5f));
            var ok = Button(card, "OK", new Color(0.2f, 0.65f, 0.35f));
            SetRect(ok.GetComponent<RectTransform>(), new Vector2(0.25f, 0.14f), new Vector2(0.75f, 0.28f));
            ok.onClick.AddListener(() =>
            {
                if (!SaveGame.TrySaveSerial(serial.text, imei.text))
                {
                    Alert("The serial number is incorrect.", ShowSerial);
                    return;
                }
                ClosePopup();
                if (!ProfileReady()) ShowProfile(true);
                else EnterTalk(true);
            });
        });
    }

    void ShowProfile(bool first)
    {
        ShowSettings(first);
    }

    bool IsDate(string text)
    {
        DateTime parsed;
        return DateTime.TryParse(text, out parsed);
    }

    void Alert(string message)
    {
        Alert(message, null);
    }

    void Alert(string message, Action after)
    {
        Popup(message, card =>
        {
            var ok = Button(card, "OK", new Color(0.2f, 0.45f, 0.75f));
            SetRect(ok.GetComponent<RectTransform>(), new Vector2(0.3f, 0.12f), new Vector2(0.7f, 0.24f));
            ok.onClick.AddListener(() =>
            {
                ClosePopup();
                if (after != null) after();
            });
        });
    }

    void Confirm(string message, Action yes)
    {
        Popup(message, card =>
        {
            var buy = Button(card, "BUY", new Color(0.2f, 0.65f, 0.35f));
            SetRect(buy.GetComponent<RectTransform>(), new Vector2(0.08f, 0.15f), new Vector2(0.45f, 0.28f));
            var cancel = Button(card, "CANCEL", new Color(0.7f, 0.3f, 0.3f));
            SetRect(cancel.GetComponent<RectTransform>(), new Vector2(0.55f, 0.15f), new Vector2(0.92f, 0.28f));
            buy.onClick.AddListener(() => { ClosePopup(); yes(); });
            cancel.onClick.AddListener(ClosePopup);
        });
    }

    void Popup(string title, Action<RectTransform> fill)
    {
        ClosePopup();
        var dim = Panel(root, Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.45f));
        dim.gameObject.name = "Popup";
        var card = Panel(dim, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.82f), Color.white);
        var heading = Label(card, title, 36, TextAnchor.UpperCenter, Color.black);
        SetRect(heading.rectTransform, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.96f));
        fill(card);
    }

    void ClosePopup()
    {
        var popup = root.Find("Popup");
        if (popup != null) Destroy(popup.gameObject);
    }

    RawImage Backdrop(Transform parent, Texture2D texture, Vector2 min, Vector2 max)
    {
        var go = new GameObject("Backdrop");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        SetRect(rect, min, max);
        var image = go.AddComponent<RawImage>();
        image.texture = texture;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    RectTransform Panel(Transform parent, Vector2 min, Vector2 max, Color color)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        SetRect(rect, min, max);
        go.AddComponent<Image>().color = color;
        return rect;
    }

    Text Label(Transform parent, string text, int size, TextAnchor anchor, Color color)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        SetRect(rect, Vector2.zero, Vector2.one);
        var label = go.AddComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.alignment = anchor;
        label.color = color;
        label.text = text;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        return label;
    }

    RectTransform Scroll(RectTransform parent, Vector2 min, Vector2 max)
    {
        var viewport = Panel(parent, min, max, new Color(0.95f, 0.97f, 1f));
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Panel(viewport, new Vector2(0, 1), new Vector2(1, 1), new Color(1, 1, 1, 0));
        content.pivot = new Vector2(0.5f, 1);
        content.GetComponent<Image>().raycastTarget = false;
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        return content;
    }

    Button Button(Transform parent, string text, Color color, int size = 28)
    {
        return SoftButton(parent, text, color, size);
    }

    Button SoftButton(Transform parent, string text, Color color, int size)
    {
        var go = new GameObject(string.IsNullOrEmpty(text) ? "Button" : text);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        var button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.14f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        var label = Label(go.transform, text, size, TextAnchor.MiddleCenter, Color.white);
        label.fontStyle = FontStyle.Bold;
        var brightness = (color.r + color.g + color.b) / 3f;
        if (brightness > 0.72f || color.a < 0.4f) label.color = Ink;
        label.rectTransform.offsetMin = new Vector2(10, 6);
        label.rectTransform.offsetMax = new Vector2(-10, -6);
        return button;
    }

    static void SetButtonLabelColor(Button button, Color color)
    {
        if (button == null) return;
        Text label = button.GetComponentInChildren<Text>();
        if (label != null) label.color = color;
    }

    RectTransform SoftCard(Transform parent, Vector2 min, Vector2 max, Color color)
    {
        var card = Panel(parent, min, max, color);
        return card;
    }

    InputField Input(Transform parent, string hint, Vector2 min, Vector2 max)
    {
        return SoftInput(parent, hint, min, max);
    }

    InputField SoftInput(Transform parent, string hint, Vector2 min, Vector2 max)
    {
        var go = new GameObject(hint);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        SetRect(rect, min, max);
        go.AddComponent<Image>().color = Color.white;
        var field = go.AddComponent<InputField>();
        var text = Label(go.transform, "", 28, TextAnchor.MiddleLeft, Ink);
        text.rectTransform.offsetMin = new Vector2(22, 10);
        text.rectTransform.offsetMax = new Vector2(-22, -10);
        var placeholder = Label(go.transform, hint, 26, TextAnchor.MiddleLeft, new Color(0.45f, 0.5f, 0.55f));
        placeholder.rectTransform.offsetMin = new Vector2(22, 10);
        placeholder.rectTransform.offsetMax = new Vector2(-22, -10);
        field.textComponent = text;
        field.placeholder = placeholder;
        return field;
    }

    static int AndroidSdk()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION");
            return version.GetStatic<int>("SDK_INT");
        }
        catch
        {
            return 0;
        }
#else
        return 0;
#endif
    }

    static void RequestMicPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (AndroidSdk() < 23) return;
        try
        {
            AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("requestPermissions", new string[] { "android.permission.RECORD_AUDIO" }, 0);
        }
        catch (Exception error)
        {
            Debug.LogWarning(error.Message);
        }
#endif
    }

    static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
