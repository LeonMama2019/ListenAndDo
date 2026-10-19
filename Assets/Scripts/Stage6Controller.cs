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
        BindSurface(desk, SurfaceClick);
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
        questions = data.GetQuestions(level);
        if (questions.Count == 0) { Fail("出題できる問題がありません。"); return; }
        recorder = new StageLevelResultRecorder(6, level, questions.Count);
        Begin();
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
    private void Update() { if (held != null) FollowCursor(); }
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
        if (held != null) { SurfaceClick(point); return; }
        held = item; heldRotated = false;
        item.view.gameObject.SetActive(false);
        cursor.sprite = item.view.sprite;
        cursor.rectTransform.sizeDelta = item.size;
        cursor.rectTransform.localRotation = item.view.rectTransform.localRotation;
        cursor.gameObject.SetActive(true);
        FollowCursor();
    }
    private void SurfaceClick(PointerEventData e) => SurfaceClick(e.position);
    private void SurfaceClick(Vector2 point)
    {
        if (!CanAct || held == null) return;
        if (RectTransformUtility.RectangleContainsScreenPoint(pencase, point, UiCamera)) Store(Stage6Storage.Pencase, point);
        else if (RectTransformUtility.RectangleContainsScreenPoint(odouguArea, point, UiCamera)) Store(Stage6Storage.Odougu, point);
        else if (RectTransformUtility.RectangleContainsScreenPoint(desk, point, UiCamera))
        {
            // Same sequence as Stage5: pick, rotate, then put down.
            if (!heldRotated)
            {
                float step = held.home == Stage6Storage.Pencase && held.sceneName.IndexOf("pencil", StringComparison.OrdinalIgnoreCase) >= 0 ? 180 : 90;
                cursor.rectTransform.Rotate(0, 0, step); heldRotated = true; return;
            }
            Place(desk, point, false);
        }
    }
    private void Store(Stage6Storage home, Vector2 point)
    {
        if (!CanAct || held == null) return;
        if (held.home != home) { recorder.RecordWrongAnswer(); return; }
        Place(home == Stage6Storage.Pencase ? pencase : odouguArea, point, true);
    }
    private void Place(RectTransform area, Vector2 point, bool stored)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, point, UiCamera, out var local)) return;
        var item = held;
        var rect = item.view.rectTransform;
        var oldParent = rect.parent; var oldPosition = rect.localPosition; var oldRotation = rect.localRotation;
        var oldSize = rect.sizeDelta; var oldScale = rect.localScale;
        rect.SetParent(area, false); rect.sizeDelta = item.size; rect.localScale = Vector3.one;
        rect.anchoredPosition = local; rect.localRotation = cursor.rectTransform.localRotation;
        bool valid = Stage6PlacementGeometry.FullyInside(rect, area);
        if (stored) valid &= !items.Exists(other => other != item && other.stored && !other.delivered && other.home == item.home
            && !AllowsOverlap(item, other) && Stage6PlacementGeometry.Overlaps(rect, other.view.rectTransform));
        if (!valid)
        {
            rect.SetParent(oldParent, false); rect.localPosition = oldPosition; rect.localRotation = oldRotation;
            rect.sizeDelta = oldSize; rect.localScale = oldScale;
            return;
        }
        item.stored = stored;
        rect.SetAsLastSibling(); item.view.gameObject.SetActive(true);
        held = null; cursor.gameObject.SetActive(false);
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
        // Previously delivered items count as tidied; all other objects must already be stored.
        if (!held.stored || items.Exists(x => x != held && !x.delivered && !x.stored)) return;
        int match = question.deliveries.FindIndex(d => d.room == room && d.person == person && d.itemId == held.id);
        if (match < 0 || fulfilled.Contains(match)) { recorder.RecordWrongAnswer(); return; }
        fulfilled.Add(match); held.delivered = true; held.stored = false;
        var target = (room == Stage6Room.Class1 ? classRoom : ground).Find(person.ToString()) as RectTransform;
        if (target != null)
        {
            var rect = held.view.rectTransform;
            rect.SetParent(target, false); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = new Vector2(0, -target.rect.height * 0.15f);
            rect.sizeDelta = held.size; rect.localRotation = Quaternion.identity; rect.localScale = Vector3.one * 0.6f;
            held.view.raycastTarget = false; held.view.gameObject.SetActive(true);
        }
        held = null; cursor.gameObject.SetActive(false);
        CheckComplete();
    }
    private void CheckComplete()
    {
        if (!CanAct || held != null || fulfilled.Count != question.deliveries.Count) return;
        foreach (var item in items)
        {
            if (item.delivered) continue;
            var area = item.home == Stage6Storage.Pencase ? pencase : odouguArea;
            if (!item.stored || (item.view.rectTransform.parent != area && item.view.rectTransform.parent != item.originalParent)
                || !Stage6PlacementGeometry.FullyInside(item.view.rectTransform, area)) return;
        }
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

