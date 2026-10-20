using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Lives on the root Canvas: hiding Panel must not stop input, speech or held items.
[DefaultExecutionOrder(-100)]
public class Stage6Controller : MonoBehaviour
{
    [SerializeField] private Stage6Data data;
    [SerializeField] private float nextQuestionDelay = 3f;
    private class Item
    {
        public string id, sceneName;
        public Stage6Storage home;
        public Image view;
        public RectTransform originalParent;
        public Vector2 size, originalSizeDelta, originalAnchorMin, originalAnchorMax, originalPivot;
        public Vector3 originalPosition, originalScale;
        public Quaternion originalRotation;
        public bool stored, delivered;
    }
    private Canvas canvas;
    private Transform panel, work, classRoom, ground, backPanel, check;
    private RectTransform desk, pencase, odouguArea;
    private Image cursor;
    private AudioSource voice;
    private List<Stage6Question> questions;
    private readonly List<Item> items = new();
    private readonly HashSet<int> fulfilled = new();
    private Item held;
    private Stage6Question question;
    private StageLevelResultRecorder recorder;
    private int index, level;
    private bool accepting, completed, heldRotated;
    private Coroutine speech;
    private Transform tutorialRoot;
    private int tutorialStep;
    private bool tutorialBusy;
    private Camera UiCamera => canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    private bool CanAct => accepting && !completed && !(backPanel != null && backPanel.gameObject.activeInHierarchy);

    private void Awake()
    {
        canvas = GetComponent<Canvas>().rootCanvas;
        foreach (var old in canvas.GetComponentsInChildren<Stage5Prototype>(true)) old.enabled = false;
        foreach (var old in canvas.GetComponentsInChildren<Stage2Quiz>(true)) old.enabled = false;
        foreach (var old in canvas.GetComponentsInChildren<HandSelector>(true)) old.enabled = false;
        foreach (var old in canvas.GetComponentsInChildren<StageLevelMenuButton>(true)) old.enabled = false;
    }
    private void Start()
    {
        panel = Find(canvas.transform, "Panel");
        work = Find(panel, "WorkPanel");
        classRoom = Find(canvas.transform, "Class1");
        ground = Find(canvas.transform, "Ground");
        desk = Find(work, "DeskArea") as RectTransform;
        pencase = Find(work, "Pencase") as RectTransform;
        var odougu = Find(work, "Odougu") as RectTransform;
        odouguArea = Find(odougu, "OdouguArea") as RectTransform;
        backPanel = Find(canvas.transform, "BackReturn");
        check = Find(canvas.transform, "Check");
        Find(canvas.transform, "Tutorial")?.gameObject.SetActive(false);
        if (data == null || panel == null || work == null || classRoom == null || ground == null
            || desk == null || pencase == null || odouguArea == null)
        { Fail("Stage6Dataまたはシーンのパネル・収納先が未設定です。"); return; }
        if (!data.TryValidate(out var error)) { Fail(error); return; }
        Canvas.ForceUpdateCanvases();
        Register(pencase, Stage6Storage.Pencase);
        Register(odougu, Stage6Storage.Odougu);
        odouguArea.SetAsFirstSibling();
        var deskHit = desk.GetComponent<Image>();
        if (deskHit != null) deskHit.color = Color.clear;
        foreach (var entry in data.Items)
            if (!items.Exists(x => x.id == entry.id))
            { Fail("アイテムに対応するシーン画像がありません: " + entry.id); return; }
        cursor = NewImage("Stage6Cursor", (RectTransform)canvas.transform, null, Vector2.one);
        cursor.raycastTarget = false;
        cursor.gameObject.SetActive(false);
        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        SpeechSpeedSource.Bind(voice);
        BindSurface(desk, e => DeskClick(e.position));
        BindSurface(pencase, e => Store(Stage6Storage.Pencase, e.position));
        BindSurface(odouguArea, e => Store(Stage6Storage.Odougu, e.position));
        BindSurface(odougu, e => Store(Stage6Storage.Odougu, e.position));
        var hit = odouguArea.GetComponent<Image>();
        if (hit != null) hit.color = Color.clear;
        foreach (var label in work.GetComponentsInChildren<TMP_Text>(true)) label.enabled = false;
        Connect(Find(panel, "Class1button"), () => ShowRoom(Stage6Room.Class1));
        Connect(Find(panel, "Groundbutton"), () => ShowRoom(Stage6Room.Ground));
        Connect(Find(panel, "Speaker"), Replay);
        Connect(Find(panel, "Back"), () => { if (CanAct) canvas.GetComponent<BackReturnController>()?.Open(); });
        SetupRoom(classRoom, Stage6Room.Class1);
        SetupRoom(ground, Stage6Room.Ground);
        if (check != null) check.SetParent(canvas.transform, true);
        level = StageLevelMenu.SelectedStage == 6 ? StageLevelMenu.SelectedLevel : 1;
        if (StageLevelMenu.IsTutorialReplay(6)) { BeginTutorial(); return; }
        questions = data.GetQuestions(level);
        if (questions.Count == 0) { Fail("出題できる問題がありません。"); return; }
        recorder = new StageLevelResultRecorder(6, level, questions.Count);
        Begin();
    }
    private void BeginTutorial()
    {
        tutorialRoot = Find(canvas.transform, "Tutorial");
        if (tutorialRoot == null) { Fail("Stage6 Tutorialがありません。"); return; }
        accepting = false;
        ShowWork();
        check?.gameObject.SetActive(false);
        foreach (var item in items) RestoreTutorialItem(item);
        foreach (var audio in tutorialRoot.GetComponentsInChildren<AudioSource>(true))
        { audio.playOnAwake = false; audio.Stop(); }
        // Banners are hints, not an input-blocking full-screen overlay.
        foreach (var graphic in tutorialRoot.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
        foreach (string name in new[] { "topbanner", "downbanner" })
        {
            var banner = tutorialRoot.Find(name);
            if (banner == null) continue;
            Connect(banner.Find("nextbutton"), TutorialNext);
            var buttonImage = banner.Find("nextbutton")?.GetComponent<Graphic>();
            if (buttonImage != null) buttonImage.raycastTarget = true;
        }
        Connect(Find(panel, "Back"), () => StageLevelMenu.OpenStageMenu(6));
        Connect(Find(panel, "Speaker"), ReplayTutorialVoice);
        tutorialRoot.gameObject.SetActive(true);
        tutorialRoot.SetAsLastSibling();
        ShowTutorialStep(1);
    }
    private void TutorialNext()
    {
        if (tutorialStep == 6 || tutorialStep == 7 || (tutorialStep == 8 && held != null)) return;
        if (tutorialBusy || voice.isPlaying || (backPanel != null && backPanel.gameObject.activeInHierarchy)) return;
        // Keep later authored explanations accessible while their interactions are added.
        if (Find(tutorialRoot, (tutorialStep + 1).ToString()) != null)
            ShowTutorialStep(tutorialStep + 1);
        else StageLevelMenu.OpenStageMenu(6);
    }
    private void ShowTutorialStep(int number)
    {
        tutorialStep = number;
        voice.Stop();
        foreach (string name in new[] { "topbanner", "downbanner" })
        {
            var banner = tutorialRoot.Find(name);
            if (banner == null) continue;
            bool showBanner = false;
            foreach (Transform child in banner)
            {
                if (!int.TryParse(child.name, out int n)) continue;
                bool show = n == number;
                child.gameObject.SetActive(show);
                showBanner |= show;
            }
            banner.Find("nextbutton")?.gameObject.SetActive(true);
            banner.gameObject.SetActive(showBanner);
        }
        ReplayTutorialVoice();
        if (number == 2 || number == 3)
            StartCoroutine(TutorialTidy(number == 2 ? Stage6Storage.Pencase : Stage6Storage.Odougu));
        else if (number == 4 || number == 5)
            StartCoroutine(TutorialSpecial(number == 4 ? "Clay" : "Paste"));
        else if (number == 6) StartCoroutine(TutorialCrayonPickup());
        else if (number == 9) StartCoroutine(TutorialBlinkRoomButtons());
        else SetTutorialBusy(false);
    }
    private void ReplayTutorialVoice()
    {
        if (tutorialRoot == null) return;
        var stepView = Find(tutorialRoot, tutorialStep.ToString());
        var source = stepView != null ? stepView.GetComponent<AudioSource>() : null;
        if (source == null || source.clip == null) return;
        voice.clip = source.clip;
        voice.Play();
    }
    private void SetTutorialBusy(bool busy)
    {
        tutorialBusy = busy;
        UpdateTutorialNextButtons();
    }
    private void UpdateTutorialNextButtons()
    {
        if (tutorialRoot == null || !tutorialRoot.gameObject.activeInHierarchy) return;
        bool ready = tutorialStep != 6 && tutorialStep != 7
            && !(tutorialStep == 8 && held != null) && !tutorialBusy && !voice.isPlaying
            && !(backPanel != null && backPanel.gameObject.activeInHierarchy);
        foreach (string name in new[] { "topbanner", "downbanner" })
        {
            var target = tutorialRoot.Find(name + "/nextbutton");
            if (target == null) continue;
            target.gameObject.SetActive(true);
            var button = target.GetComponent<Button>();
            if (button != null) button.interactable = ready;
        }
    }
    private void RestoreTutorialItem(Item item)
    {
        var rect = item.view.rectTransform;
        rect.SetParent(item.originalParent, false);
        rect.anchorMin = item.originalAnchorMin; rect.anchorMax = item.originalAnchorMax;
        rect.pivot = item.originalPivot; rect.sizeDelta = item.originalSizeDelta;
        rect.localPosition = item.originalPosition; rect.localRotation = item.originalRotation;
        rect.localScale = item.originalScale;
        item.stored = true; item.delivered = false;
        item.view.gameObject.SetActive(true);
    }
    private IEnumerator TutorialTidy(Stage6Storage home)
    {
        SetTutorialBusy(true);
        var targets = items.FindAll(item => item.home == home);
        var colors = new Dictionary<Item, Color>();
        var starts = new Dictionary<Item, Vector3>();
        var rotations = new Dictionary<Item, Quaternion>();
        for (int i = 0; i < targets.Count; i++)
        {
            var item = targets[i];
            colors[item] = item.view.color;
            var rect = item.view.rectTransform;
            rect.SetParent(desk, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = item.size; rect.localScale = Vector3.one;
            rect.localRotation = item.originalRotation;
            // A spread-out demonstration keeps the highlighted object visible.
            int columns = Mathf.Min(3, targets.Count);
            int rows = Mathf.CeilToInt((float)targets.Count / columns);
            rect.localPosition = new Vector2(
                Mathf.Lerp(desk.rect.xMin, desk.rect.xMax, ((i % columns) + 0.5f) / columns),
                Mathf.Lerp(desk.rect.yMax, desk.rect.yMin, ((i / columns) + 0.5f) / rows));
            item.stored = false;
        }
        float elapsed = 0f;
        while (elapsed < 2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.PingPong(elapsed * 3f, 1f);
            foreach (var item in targets)
            {
                bool highlight = item.home == home;
                if (highlight)
                {
                    var color = colors[item]; color.a *= Mathf.Lerp(0.2f, 1f, alpha);
                    item.view.color = color;
                }
            }
            yield return null;
        }
        foreach (var item in targets)
        {
            item.view.color = colors[item];
            starts[item] = item.view.rectTransform.position;
            rotations[item] = item.view.rectTransform.rotation;
        }
        elapsed = 0f;
        const float duration = 0.45f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
            foreach (var item in targets)
            {
                var rect = item.view.rectTransform;
                rect.position = Vector3.Lerp(starts[item], item.originalParent.TransformPoint(item.originalPosition), t);
                rect.rotation = Quaternion.Slerp(rotations[item], item.originalParent.rotation * item.originalRotation, t);
            }
            yield return null;
        }
        foreach (var item in targets) RestoreTutorialItem(item);
        SetTutorialBusy(false);
    }
    private IEnumerator TutorialSpecial(string sceneName)
    {
        SetTutorialBusy(true);
        // Start the desk demonstration alongside narration, then blink in storage.
        var item = items.Find(entry => entry.home == Stage6Storage.Odougu
            && entry.sceneName.Equals(sceneName, StringComparison.OrdinalIgnoreCase));
        if (item == null)
        {
            Debug.LogError("Stage6 Tutorial: " + sceneName + "がありません。", this);
            SetTutorialBusy(false);
            yield break;
        }
        var rect = item.view.rectTransform;
        rect.SetParent(desk, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = item.size;
        rect.localScale = Vector3.one;
        rect.localRotation = item.originalRotation;
        rect.localPosition = desk.rect.center;
        item.stored = false;
        item.view.gameObject.SetActive(true);
        yield return TutorialBlink(item.view, 2, 0.25f);
        Vector3 start = rect.position;
        Quaternion rotation = rect.rotation;
        Vector3 destination = item.originalParent.TransformPoint(item.originalPosition);
        const float duration = 0.45f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
            rect.position = Vector3.Lerp(start, destination, t);
            rect.rotation = Quaternion.Slerp(rotation, item.originalParent.rotation * item.originalRotation, t);
            yield return null;
        }
        RestoreTutorialItem(item);
        yield return TutorialBlink(item.view, 3, 0.6f);
        SetTutorialBusy(false);
    }
    private IEnumerator TutorialBlinkRoomButtons()
    {
        SetTutorialBusy(true);
        var classButton = Find(panel, "Class1button")?.GetComponent<Image>();
        var groundButton = Find(panel, "Groundbutton")?.GetComponent<Image>();
        // Run both three-blink cues together.
        Coroutine classBlink = classButton != null
            ? StartCoroutine(TutorialBlink(classButton, 3, 0.4f)) : null;
        Coroutine groundBlink = groundButton != null
            ? StartCoroutine(TutorialBlink(groundButton, 3, 0.4f)) : null;
        if (classBlink != null) yield return classBlink;
        if (groundBlink != null) yield return groundBlink;
        SetTutorialBusy(false);
    }
    private IEnumerator TutorialBlink(Image image, int count, float halfPeriod)
    {
        Color original = image.color;
        for (int i = 0; i < count; i++)
        {
            Color dim = original; dim.a *= 0.2f;
            image.color = dim;
            yield return new WaitForSecondsRealtime(halfPeriod);
            image.color = original;
            yield return new WaitForSecondsRealtime(halfPeriod);
        }
    }
    private IEnumerator TutorialCrayonPickup()
    {
        SetTutorialBusy(true);
        var item = items.Find(entry => entry.home == Stage6Storage.Odougu
            && entry.sceneName.Equals("Crayon", StringComparison.OrdinalIgnoreCase));
        if (item == null)
        {
            Debug.LogError("Stage6 Tutorial: Crayonがありません。", this);
            SetTutorialBusy(false);
            yield break;
        }
        var rect = item.view.rectTransform;
        rect.SetParent(desk, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = item.size; rect.localScale = Vector3.one;
        rect.localRotation = item.originalRotation;
        rect.localPosition = desk.rect.center;
        item.stored = false;
        item.view.gameObject.SetActive(true);

        // A visible demo pointer approaches the crayon before pressing it.
        var pointerSprite = TutorialPointerSprite();
        var pointer = NewImage("Stage6TutorialPointer", (RectTransform)canvas.transform,
            pointerSprite, new Vector2(40, 50));
        pointer.raycastTarget = false;
        pointer.rectTransform.pivot = new Vector2(0, 1);
        Vector3 destination = rect.position;
        Vector3 start = canvas.transform.TransformPoint(
            canvas.transform.InverseTransformPoint(destination) + new Vector3(110, -90));
        for (float elapsed = 0; elapsed < 0.6f; elapsed += Time.unscaledDeltaTime)
        {
            pointer.rectTransform.position = Vector3.Lerp(start, destination,
                Mathf.SmoothStep(0, 1, elapsed / 0.6f));
            yield return null;
        }
        pointer.rectTransform.position = destination;
        yield return new WaitForSecondsRealtime(0.3f);
        // Brief compression plus click rays makes the automatic press visible.
        rect.localScale = Vector3.one * 0.92f;
        pointer.rectTransform.localScale = Vector3.one * 0.8f;
        var rays = new List<Image>();
        for (int i = 0; i < 4; i++)
        {
            float angle = (45 + i * 90) * Mathf.Deg2Rad;
            var ray = NewImage("ClickRay", (RectTransform)canvas.transform, null, new Vector2(6, 18));
            ray.color = new Color(1f, 0.65f, 0.05f);
            ray.raycastTarget = false;
            ray.rectTransform.position = canvas.transform.TransformPoint(
                canvas.transform.InverseTransformPoint(destination)
                + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * 28f);
            ray.rectTransform.localRotation = Quaternion.Euler(0, 0, i * 90 - 45);
            rays.Add(ray);
        }
        yield return new WaitForSecondsRealtime(0.2f);
        foreach (var ray in rays) Destroy(ray.gameObject);
        rect.localScale = Vector3.one;
        // Transfer the same sprite to the actual held-item cursor.
        held = item; heldRotated = false;
        item.view.gameObject.SetActive(false);
        cursor.sprite = item.view.sprite;
        cursor.rectTransform.sizeDelta = item.size;
        cursor.rectTransform.localRotation = rect.localRotation;
        cursor.gameObject.SetActive(true);
        FollowCursor();
        Destroy(pointer.gameObject);
        Destroy(pointerSprite.texture);
        Destroy(pointerSprite);
        // Let step 6 narration finish, then show the held crayon for about two seconds.
        while (voice.isPlaying) yield return null;
        yield return new WaitForSecondsRealtime(2f);
        SetTutorialBusy(false);
        ShowTutorialStep(7);
    }
    private static Sprite TutorialPointerSprite()
    {
        // Small code-drawn UI arrow: no additional scene or asset reference is needed.
        const int width = 32, height = 40;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        bool Inside(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return false;
            return (y <= 28 && x <= y * 0.65f)
                || (y >= 20 && y <= 36 && x >= 7 + (y - 20) * 0.4f && x <= 13 + (y - 20) * 0.4f);
        }
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                bool inside = Inside(x, y);
                bool edge = inside && (!Inside(x - 1, y) || !Inside(x + 1, y)
                    || !Inside(x, y - 1) || !Inside(x, y + 1));
                texture.SetPixel(x, height - 1 - y, inside ? (edge ? Color.black : Color.white) : Color.clear);
            }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0, 1), 100);
    }
    private void Register(RectTransform parent, Stage6Storage home)
    {
        foreach (Transform child in parent)
        {
            var image = child.GetComponent<Image>();
            if (image == null || image.sprite == null || child.name.EndsWith("Area", StringComparison.Ordinal)) continue;
            Stage6Item entry = null;
            foreach (var candidate in data.Items)
                if (candidate.home == home && candidate.sprite == image.sprite) { entry = candidate; break; }
            var item = new Item { id = entry?.id, sceneName = child.name, home = home, view = image,
                originalParent = parent, size = image.rectTransform.rect.size,
                originalPosition = image.rectTransform.localPosition, originalRotation = image.rectTransform.localRotation,
                originalScale = image.rectTransform.localScale, originalSizeDelta = image.rectTransform.sizeDelta,
                originalAnchorMin = image.rectTransform.anchorMin, originalAnchorMax = image.rectTransform.anchorMax,
                originalPivot = image.rectTransform.pivot };
            items.Add(item);
            image.raycastTarget = true;
            BindSurface(child, e => ClickItem(item, e.position));
        }
    }
    private void Begin()
    {
        accepting = false; completed = false; held = null; fulfilled.Clear();
        question = questions[index];
        cursor.gameObject.SetActive(false);
        check?.gameObject.SetActive(false);
        ShowWork();
        foreach (var item in items)
        {
            item.delivered = false;
            item.stored = !question.scatterPlaces.Contains(item.home);
            var rect = item.view.rectTransform;
            rect.SetParent(item.originalParent, false);
            rect.anchorMin = item.originalAnchorMin; rect.anchorMax = item.originalAnchorMax;
            rect.pivot = item.originalPivot; rect.sizeDelta = item.originalSizeDelta;
            rect.localPosition = item.originalPosition;
            rect.localRotation = item.originalRotation;
            rect.localScale = item.originalScale;
            item.view.gameObject.SetActive(true); item.view.raycastTarget = true;
            if (item.stored) continue;
            rect.SetParent(desk, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = item.size;
            rect.localScale = Vector3.one;
            bool pencil = item.home == Stage6Storage.Pencase && item.sceneName.IndexOf("pencil", StringComparison.OrdinalIgnoreCase) >= 0;
            rect.localRotation = item.originalRotation * Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, pencil ? 2 : 4) * (pencil ? 180 : 90));
            // Calculate bounds after rotation so even long pencils stay on the desk.
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            float ex = 0, ey = 0;
            foreach (var c in corners)
            {
                Vector3 d = desk.InverseTransformPoint(c) - rect.localPosition;
                ex = Mathf.Max(ex, Mathf.Abs(d.x)); ey = Mathf.Max(ey, Mathf.Abs(d.y));
            }
            rect.anchoredPosition = new Vector2(Range(desk.rect.xMin + ex, desk.rect.xMax - ex), Range(desk.rect.yMin + ey, desk.rect.yMax - ey));
        }
        ConfigurePeople(classRoom, Stage6Room.Class1);
        ConfigurePeople(ground, Stage6Room.Ground);
        speech = StartCoroutine(Instruction(true));
    }
    private static float Range(float min, float max) => min > max ? (min + max) * 0.5f : UnityEngine.Random.Range(min, max);
    private void ConfigurePeople(Transform room, Stage6Room location)
    {
        foreach (Stage6Person person in Enum.GetValues(typeof(Stage6Person)))
        {
            var target = room.Find(person.ToString());
            if (target != null) target.gameObject.SetActive(question.people.Exists(x => x.room == location && x.person == person));
        }
    }
    private void SetupRoom(Transform room, Stage6Room location)
    {
        foreach (Stage6Person person in Enum.GetValues(typeof(Stage6Person)))
        {
            var target = room.Find(person.ToString());
            if (target == null) continue;
            var recipient = person;
            BindSurface(target, e => Deliver(location, recipient));
        }
        var authoredReturn = room.Find("BackToWork");
        if (authoredReturn != null)
        {
            Connect(authoredReturn, () => { if (CanAct) ShowWork(); });
            return;
        }
        // A small persistent return button for each destination; holding state stays on Canvas.
        var image = NewImage("Stage6Return", (RectTransform)room, null, new Vector2(150, 60));
        image.color = new Color(1, 0.75f, 0.35f);
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0, 1);
        image.rectTransform.anchoredPosition = new Vector2(95, -45);
        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(image.transform, false);
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        var existing = canvas.GetComponentInChildren<TMP_Text>(true);
        if (existing != null) label.font = existing.font;
        label.text = "もどる"; label.fontSize = 30; label.color = Color.black;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        label.rectTransform.sizeDelta = new Vector2(150, 60);
        Connect(image.transform, () => { if (CanAct) ShowWork(); });
    }
    private void ShowWork()
    {
        classRoom.gameObject.SetActive(false); ground.gameObject.SetActive(false);
        panel.gameObject.SetActive(true); work.gameObject.SetActive(true);
    }
    private void ShowRoom(Stage6Room room)
    {
        if (!CanAct) return;
        panel.gameObject.SetActive(false);
        classRoom.gameObject.SetActive(room == Stage6Room.Class1);
        ground.gameObject.SetActive(room == Stage6Room.Ground);
        FollowCursor();
    }
    private void Update()
    {
        if (held != null) FollowCursor();
        UpdateTutorialNextButtons();
    }
    private void FollowCursor()
    {
        if (cursor == null || held == null) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, (Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition), UiCamera, out var local))
            cursor.rectTransform.anchoredPosition = local;
        cursor.transform.SetAsLastSibling();
        if (backPanel != null && backPanel.gameObject.activeInHierarchy) backPanel.SetAsLastSibling();
    }
    private void ClickItem(Item item, Vector2 point)
    {
        if (!CanAct || item.delivered) return;
        if (held != null)
        {
            // An item must forward the click to the surface it is actually on.
            // In particular, stored objects can cover the container's background.
            if (item.view.rectTransform.parent == desk) DeskClick(point);
            else Store(item.home, point);
            return;
        }
        held = item; heldRotated = false;
        item.view.gameObject.SetActive(false);
        cursor.sprite = item.view.sprite;
        cursor.rectTransform.sizeDelta = item.size;
        cursor.rectTransform.localRotation = item.view.rectTransform.localRotation;
        cursor.gameObject.SetActive(true);
        FollowCursor();
    }
    private void DeskClick(Vector2 point)
    {
        if (tutorialRoot != null && tutorialRoot.gameObject.activeInHierarchy)
        {
            if ((tutorialStep != 7 && tutorialStep != 8) || tutorialBusy || voice.isPlaying || held == null
                || (backPanel != null && backPanel.gameObject.activeInHierarchy)
                || !RectTransformUtility.RectangleContainsScreenPoint(desk, point, UiCamera)) return;
            if (tutorialStep == 7)
            {
                cursor.rectTransform.Rotate(0, 0, 90f);
                heldRotated = true;
                ShowTutorialStep(8);
            }
            else
            {
                // Reuse normal desk placement, including inward adjustment near the edges.
                Place(desk, point, false);
                UpdateTutorialNextButtons();
            }
            return;
        }
        if (!CanAct || held == null) return;
        if (!RectTransformUtility.RectangleContainsScreenPoint(desk, point, UiCamera)) return;
        // Each pickup starts a new cycle: pickup, rotate on desk, put down on desk.
        // Storage clicks bypass this cycle and attempt to place immediately.
        if (!heldRotated)
        {
            float step = held.home == Stage6Storage.Pencase && held.sceneName.IndexOf("pencil", StringComparison.OrdinalIgnoreCase) >= 0 ? 180 : 90;
            cursor.rectTransform.Rotate(0, 0, step);
            heldRotated = true;
            return;
        }
        Place(desk, point, false);
    }
    private void Store(Stage6Storage home, Vector2 point)
    {
        if (!CanAct || held == null) return;
        if (held.home != home) { recorder.RecordWrongAnswer(); return; }
        if (home == Stage6Storage.Pencase
            && held.sceneName.IndexOf("pencil", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            StorePencil();
            return;
        }
        Place(home == Stage6Storage.Pencase ? pencase : odouguArea, point, true);
    }
    private void StorePencil()
    {
        // Each physical pencil has its own authored slot, including Pencil1/2/3.
        // Restore already stored contents too so manual placement cannot block the last slot.
        foreach (var item in items)
        {
            if (item.home != Stage6Storage.Pencase || item.delivered
                || (item != held && !item.stored)) continue;
            var rect = item.view.rectTransform;
            rect.SetParent(item.originalParent, false);
            rect.anchorMin = item.originalAnchorMin;
            rect.anchorMax = item.originalAnchorMax;
            rect.pivot = item.originalPivot;
            rect.sizeDelta = item.originalSizeDelta;
            rect.localPosition = item.originalPosition;
            rect.localRotation = item.originalRotation;
            rect.localScale = item.originalScale;
            item.stored = true;
            item.view.gameObject.SetActive(true);
        }
        held = null;
        heldRotated = false;
        cursor.gameObject.SetActive(false);
        CheckComplete();
    }
    private void Place(RectTransform area, Vector2 point, bool stored)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, point, UiCamera, out var local)) return;
        var item = held;
        var rect = item.view.rectTransform;
        var oldParent = rect.parent; var oldPosition = rect.localPosition; var oldRotation = rect.localRotation;
        var oldSize = rect.sizeDelta; var oldScale = rect.localScale;
        var oldAnchorMin = rect.anchorMin; var oldAnchorMax = rect.anchorMax; var oldPivot = rect.pivot;
        rect.SetParent(area, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = item.size; rect.localScale = Vector3.one;
        rect.localPosition = local; rect.localRotation = cursor.rectTransform.localRotation;
        if (!stored)
        {
            // Like Stage5, shift an edge click inward instead of rejecting the drop.
            // Measure after rotation in the destination's space.
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var corner in corners)
            {
                Vector2 offset = area.InverseTransformPoint(corner) - rect.localPosition;
                min = Vector2.Min(min, offset); max = Vector2.Max(max, offset);
            }
            var bounds = area.rect;
            float left = bounds.xMin - min.x, right = bounds.xMax - max.x;
            float bottom = bounds.yMin - min.y, top = bounds.yMax - max.y;
            local.x = left <= right ? Mathf.Clamp(local.x, left, right) : (left + right) * 0.5f;
            local.y = bottom <= top ? Mathf.Clamp(local.y, bottom, top) : (bottom + top) * 0.5f;
            rect.localPosition = local;
        }
        bool valid = Stage6PlacementGeometry.FullyInside(rect, area);
        if (stored) valid &= !items.Exists(other => other != item && other.stored && !other.delivered && other.home == item.home
            && !AllowsOverlap(item, other) && Stage6PlacementGeometry.Overlaps(rect, other.view.rectTransform));
        if (!valid)
        {
            rect.SetParent(oldParent, false);
            rect.anchorMin = oldAnchorMin; rect.anchorMax = oldAnchorMax; rect.pivot = oldPivot;
            rect.localPosition = oldPosition; rect.localRotation = oldRotation;
            rect.sizeDelta = oldSize; rect.localScale = oldScale;
            return;
        }
        item.stored = stored;
        rect.SetAsLastSibling(); item.view.gameObject.SetActive(true);
        held = null; heldRotated = false; cursor.gameObject.SetActive(false);
        CheckComplete();
    }
    private static bool AllowsOverlap(Item a, Item b) => a.home == Stage6Storage.Odougu && b.home == Stage6Storage.Odougu
        && Stackable(a.sceneName) && Stackable(b.sceneName);
    private static bool Stackable(string id)
    {
        string key = id.Replace(" ", "").ToLowerInvariant();
        return key == "colorpencil" || key == "coloredpencil" || key == "colordpencil" || key == "crayon" || key == "scissors" || key == "tape";
    }
    private void Deliver(Stage6Room room, Stage6Person person)
    {
        if (!CanAct || held == null) return;
        // The carried delivery item is exempt; every remaining object must be tidied.
        // Picking it up from the desk must not make a correct delivery impossible.
        if (items.Exists(x => x != held && !IsTidied(x))) return;
        int match = question.deliveries.FindIndex(d => d.room == room && d.person == person && d.itemId == held.id);
        if (match < 0 || fulfilled.Contains(match)) { recorder.RecordWrongAnswer(); return; }
        fulfilled.Add(match); held.delivered = true; held.stored = false;
        var target = (room == Stage6Room.Class1 ? classRoom : ground).Find(person.ToString()) as RectTransform;
        if (target != null)
        {
            var rect = held.view.rectTransform;
            rect.SetParent(target, false); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = new Vector2(0, 50f);
            rect.sizeDelta = held.size; rect.localRotation = Quaternion.identity; rect.localScale = Vector3.one * 0.6f;
            held.view.raycastTarget = false; held.view.gameObject.SetActive(true);
        }
        held = null; cursor.gameObject.SetActive(false);
        CheckComplete();
    }
    private void CheckComplete()
    {
        if (!CanAct || held != null || fulfilled.Count != question.deliveries.Count) return;
        if (items.Exists(item => !IsTidied(item))) return;
        completed = true; accepting = false;
        if (check != null)
        {
            check.gameObject.SetActive(true); check.SetAsLastSibling();
            foreach (var graphic in check.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            var sound = check.GetComponent<AudioSource>();
            if (sound != null && sound.clip != null) sound.PlayOneShot(sound.clip);
        }
        recorder.RecordCorrect(Time.realtimeSinceStartupAsDouble);
        StartCoroutine(Next());
    }
    private bool IsTidied(Item item)
    {
        if (item.delivered) return true;
        var area = item.home == Stage6Storage.Pencase ? pencase : odouguArea;
        // Place validates bounds and overlaps before setting stored. Authored items in
        // the unscattered container are already stored, even at its decorative edges.
        return item.stored && (item.view.rectTransform.parent == area
            || item.view.rectTransform.parent == item.originalParent);
    }
    private IEnumerator Next()
    {
        yield return new WaitForSecondsRealtime(nextQuestionDelay);
        while (backPanel != null && backPanel.gameObject.activeInHierarchy) yield return null;
        index++;
        if (index < questions.Count) Begin();
        else { StageLevelMenu.MarkCompleted(6, level); StageResultScreenDisplay.OpenAfterGame(6, level); }
    }
    private IEnumerator Instruction(bool begin)
    {
        accepting = false;
        foreach (var clip in question.instructionVoices)
        {
            voice.clip = clip; voice.Play();
            while (voice.isPlaying) yield return null;
        }
        if (begin) recorder.BeginQuestion(index + 1, question.taskId, JsonUtility.ToJson(question), Time.realtimeSinceStartupAsDouble);
        accepting = true; speech = null;
    }
    private void Replay()
    {
        if (!CanAct || speech != null) return;
        recorder.RecordReplay(); speech = StartCoroutine(Instruction(false));
    }
    private void OnDestroy() { if (voice != null) voice.Stop(); }
    private void Fail(string error) { Debug.LogError("Stage6: " + error, this); enabled = false; }
    private static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
        return null;
    }
    private static void BindSurface(Transform target, Action<PointerEventData> callback)
    {
        if (target == null) return;
        var image = target.GetComponent<Image>(); if (image != null) image.raycastTarget = true;
        var handler = target.GetComponent<Stage5ClickTarget>() ?? target.gameObject.AddComponent<Stage5ClickTarget>();
        handler.Bind(callback);
    }
    private static void Connect(Transform target, Action action)
    {
        if (target == null) return;
        var button = target.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>();
        button.targetGraphic = target.GetComponent<Graphic>(); button.transition = Selectable.Transition.None;
        button.onClick = new Button.ButtonClickedEvent(); button.onClick.AddListener(() => action());
    }
    private static Image NewImage(string name, RectTransform parent, Sprite sprite, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var image = go.GetComponent<Image>(); image.rectTransform.SetParent(parent, false);
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = Vector2.one * 0.5f;
        image.rectTransform.sizeDelta = size; image.sprite = sprite; return image;
    }
}

