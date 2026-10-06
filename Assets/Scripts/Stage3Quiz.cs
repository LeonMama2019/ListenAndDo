using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Stage3Quiz : MonoBehaviour
{
    [SerializeField] private Stage3ObjectData data;
    [SerializeField] private Image[] itemViews = new Image[5];
    [SerializeField] private Stage3BoxTarget[] boxes = new Stage3BoxTarget[3];
    [SerializeField] private Button speaker;
    [SerializeField] private GameObject check;
    [SerializeField] private GameObject backReturnPanel;
    [SerializeField] private int questionCount = 7;
    [SerializeField] private float nextQuestionDelay = 3f;
    [SerializeField, Range(0.1f, 1f)] private float placedScale = 0.4f;
    [SerializeField] private float dropSeconds = 0.35f;

    private readonly System.Random random = new();
    private readonly List<Stage3ObjectEntry> readyItems = new();
    private readonly Dictionary<string, Stage3ObjectEntry> boxEntries = new();
    private readonly List<GameObject> placedCopies = new();
    private readonly Dictionary<string, int> placementCounts = new();
    private readonly Stage3ObjectEntry[] visibleItems = new Stage3ObjectEntry[5];
    private Canvas canvas;
    private RectTransform canvasRect, cursorRect;
    private Image cursorImage;
    private AudioSource voice, checkAudio;
    private StageLevelResultRecorder recorder;
    private Stage3QuestionLog log;
    private Coroutine instruction;
    private int level, question, selectedSlot = -1;
    private bool accepting, dropping, finished;
    private double started;
    private Vector2 pointerPosition;
    private Camera UiCamera => canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    private bool ModalOpen => backReturnPanel != null && backReturnPanel.activeInHierarchy;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>().rootCanvas;
        canvasRect = (RectTransform)canvas.transform;
        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.spatialBlend = 0;
        if (check != null) { checkAudio = check.GetComponent<AudioSource>(); check.SetActive(false); }
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            foreach (var oldQuiz in root.GetComponentsInChildren<Stage2Quiz>(true)) oldQuiz.enabled = false;
            foreach (var oldQuiz in root.GetComponentsInChildren<Stage1Level1Quiz>(true)) oldQuiz.enabled = false;
            foreach (var hand in root.GetComponentsInChildren<HandSelector>(true)) hand.enabled = false;
        }
        // This scene was copied from Stage2; its tutorial is not a Stage3 tutorial.
        foreach (var t in GetComponentsInChildren<Stage2Tutorial>(true)) t.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (!ValidateSetup()) { enabled = false; return; }
        level = StageLevelMenu.SelectedStage == 3 ? Mathf.Clamp(StageLevelMenu.SelectedLevel, 1, 8) : 1;
        for (int i = 0; i < itemViews.Length; i++)
        {
            var target = itemViews[i].GetComponent<Stage3ItemTarget>();
            if (target == null) target = itemViews[i].gameObject.AddComponent<Stage3ItemTarget>();
            target.Bind(this, i);
            itemViews[i].raycastTarget = true;
            itemViews[i].preserveAspect = true;
        }
        foreach (var box in boxes) box.Image.sprite = boxEntries[box.BoxId].Sprite;
        if (speaker != null && speaker.onClick.GetPersistentEventCount() == 0)
            speaker.onClick.AddListener(ReplayInstruction);
        recorder = new StageLevelResultRecorder(3, level, questionCount);
        NextQuestion();
    }

    private bool ValidateSetup()
    {
        if (data == null || data.EndVoice == null || itemViews == null || itemViews.Length != 5 ||
            boxes == null || boxes.Length != 3 || questionCount < 1)
            return SetupError("Stage3: data, end voice, five item views and three boxes are required.");
        var itemIds = new HashSet<string>();
        foreach (var item in data.Items)
            if (item != null && item.IsReady && itemIds.Add(item.ObjectId)) readyItems.Add(item);
        if (readyItems.Count < 5) return SetupError("Stage3: register at least five distinct items with images and voices.");
        foreach (var view in itemViews) if (view == null) return SetupError("Stage3: an item view is missing.");
        foreach (var entry in data.Boxes)
            if (entry != null && entry.IsReady && !boxEntries.ContainsKey(entry.ObjectId)) boxEntries.Add(entry.ObjectId, entry);
        var sceneIds = new HashSet<string>();
        int selectedLevel = StageLevelMenu.SelectedStage == 3 ? StageLevelMenu.SelectedLevel : 1;
        foreach (var box in boxes)
        {
            if (box == null || string.IsNullOrWhiteSpace(box.BoxId) || !sceneIds.Add(box.BoxId) ||
                !boxEntries.TryGetValue(box.BoxId, out var entry))
                return SetupError("Stage3: each scene box needs a distinct ID matching Boxes in the data.");
            if (selectedLevel >= 4 && selectedLevel != 6 && entry.BoxAndVoice == null)
                return SetupError("Stage3: register the 'box and' voice for " + box.BoxId);
        }
        return true;
    }
    private bool SetupError(string message) { Debug.LogError(message, this); return false; }

    private void NextQuestion()
    {
        question++;
        accepting = false;
        ClearCursor();
        foreach (var copy in placedCopies) if (copy != null) Destroy(copy);
        placedCopies.Clear(); placementCounts.Clear();
        if (check != null) check.SetActive(false);
        var candidates = new List<Stage3ObjectEntry>(readyItems);
        Stage3QuestionPlan.Shuffle(candidates, random);
        log = new Stage3QuestionLog();
        for (int i = 0; i < 5; i++)
        {
            visibleItems[i] = candidates[i];
            itemViews[i].sprite = candidates[i].Sprite;
            log.visibleItems.Add(new Stage3VisibleItem {slot = i, objectId = candidates[i].ObjectId});
        }
        var boxIds = new List<string>();
        foreach (var box in boxes) boxIds.Add(box.BoxId);
        log.instructions = Stage3QuestionPlan.Create(level, boxIds, random);
        foreach (var group in log.instructions)
            foreach (var boxId in group.boxIds)
                log.targets.Add(new Stage3PlacementTarget { objectId = visibleItems[group.itemSlot].ObjectId, boxId = boxId });
        started = Time.realtimeSinceStartupAsDouble;
        recorder.BeginQuestion(question, "placement", "", started);
        recorder.SetPlacementQuestion(log);
        instruction = StartCoroutine(Speak());
    }

    private void Event(string kind, string objectId = "", string boxId = "", string detail = "")
    {
        log.events.Add(new Stage3QuestionEvent {kind = kind, objectId = objectId, boxId = boxId, detail = detail,
            seconds = (float)(Time.realtimeSinceStartupAsDouble - started)});
    }
    private IEnumerator PlayClip(AudioClip clip, string objectId = "", string boxId = "")
    {
        Event("audio", objectId, boxId, clip.name);
        voice.clip = clip; voice.Play();
        while (voice.isPlaying) yield return null;
    }
    private IEnumerator Speak()
    {
        accepting = false;
        ClearCursor();
        foreach (var group in log.instructions)
        {
            var item = visibleItems[group.itemSlot];
            yield return PlayClip(item.Voice, item.ObjectId);
            for (int i = 0; i < group.boxIds.Count; i++)
            {
                var box = boxEntries[group.boxIds[i]];
                yield return PlayClip(i < group.boxIds.Count - 1 ? box.BoxAndVoice : box.Voice,
                    item.ObjectId, box.ObjectId);
            }
        }
        yield return PlayClip(data.EndVoice);
        Event("instructionComplete");
        instruction = null;
        accepting = true;
    }
    public void ReplayInstruction()
    {
        if (!accepting || dropping || finished || instruction != null || ModalOpen) return;
        recorder.RecordReplay(); Event("replay");
        instruction = StartCoroutine(Speak());
    }

    public void SelectItem(int slot, Vector2 screenPoint)
    {
        if (!accepting || dropping || finished || ModalOpen || slot < 0 || slot >= 5) return;
        ClearCursor();
        selectedSlot = slot;
        cursorImage = CreateCopy(visibleItems[slot].Sprite, canvasRect, "Stage3ItemCursor");
        cursorRect = cursorImage.rectTransform;
        cursorRect.sizeDelta = CanvasSize(itemViews[slot].rectTransform);
        cursorRect.anchoredPosition = CanvasPoint(screenPoint);
        pointerPosition = screenPoint;
        Cursor.visible = false;
        Event("select", visibleItems[slot].ObjectId, detail: "slot:" + slot);
    }

    private void Update()
    {
        if (cursorRect == null || !accepting || dropping || ModalOpen) return;
        // A tap selects a copy; on touch screens the next touch can carry it to a box.
        if (Input.touchCount > 0) pointerPosition = Input.GetTouch(0).position;
        else if (Input.mousePresent) pointerPosition = Input.mousePosition;
        cursorRect.anchoredPosition = CanvasPoint(pointerPosition);
        foreach (var box in boxes)
        {
            if (!box.Contains(pointerPosition, UiCamera)) continue;
            StartCoroutine(Drop(box));
            break;
        }
    }
    private Vector2 CanvasPoint(Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, UiCamera, out var local);
        return local;
    }
    private Vector2 CanvasPoint(Vector3 worldPoint) => (Vector2)canvasRect.InverseTransformPoint(worldPoint);
    private Vector2 CanvasSize(RectTransform source)
    {
        var corners = new Vector3[4]; source.GetWorldCorners(corners);
        var bottomLeft = canvasRect.InverseTransformPoint(corners[0]);
        var topRight = canvasRect.InverseTransformPoint(corners[2]);
        return new Vector2(Mathf.Abs(topRight.x - bottomLeft.x), Mathf.Abs(topRight.y - bottomLeft.y));
    }
    private static Image CreateCopy(Sprite sprite, Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        return image;
    }
    private IEnumerator MoveCopy(RectTransform rect, Vector2 from, Vector2 to, Vector2 fromSize, Vector2 toSize)
    {
        float elapsed = 0;
        float duration = Mathf.Max(0.01f, dropSeconds);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
            rect.anchoredPosition = Vector2.Lerp(from, to, t);
            rect.sizeDelta = Vector2.Lerp(fromSize, toSize, t);
            yield return null;
        }
        rect.anchoredPosition = to; rect.sizeDelta = toSize;
    }
    private IEnumerator Drop(Stage3BoxTarget box)
    {
        dropping = true;
        var item = visibleItems[selectedSlot];
        var source = itemViews[selectedSlot].rectTransform;
        var copy = cursorRect;
        cursorRect = null; cursorImage = null; selectedSlot = -1;
        Cursor.visible = true;
        Stage3PlacementTarget match = log.targets.Find(t => !t.completed && t.objectId == item.ObjectId && t.boxId == box.BoxId);
        Event(match == null ? "wrongPlacement" : "placement", item.ObjectId, box.BoxId);
        if (match == null) recorder.RecordWrongAnswer();
        int count = placementCounts.TryGetValue(box.BoxId, out int existing) ? existing : 0;
        var bounds = box.Rect.rect;
        // Up to five different items per box, laid out in two rows so they remain visible.
        Vector2 localDestination = new Vector2(bounds.xMin + bounds.width * (0.25f + 0.25f * (count % 3)),
            bounds.yMin + bounds.height * (count < 3 ? 0.65f : 0.4f));
        Vector2 destination = CanvasPoint(box.Rect.TransformPoint(localDestination));
        Vector2 originalSize = copy.sizeDelta;
        yield return MoveCopy(copy, copy.anchoredPosition, destination, originalSize, originalSize * placedScale);
        if (match == null)
        {
            yield return MoveCopy(copy, destination, CanvasPoint(source.position), copy.sizeDelta, originalSize);
            Destroy(copy.gameObject);
            Event("returned", item.ObjectId, box.BoxId);
        }
        else
        {
            match.completed = true;
            // Attach the deposited copy to its box, preserving its on-screen size.
            copy.SetParent(box.Rect, true);
            copy.anchorMin = copy.anchorMax = new Vector2(0.5f, 0.5f);
            copy.position = box.Rect.TransformPoint(localDestination);
            placedCopies.Add(copy.gameObject);
            placementCounts[box.BoxId] = count + 1;
        }
        dropping = false;
        if (log.targets.TrueForAll(t => t.completed)) CompleteQuestion();
    }
    private void CompleteQuestion()
    {
        accepting = false;
        Event("correct");
        recorder.RecordCorrect(Time.realtimeSinceStartupAsDouble);
        if (check != null) check.SetActive(true);
        if (checkAudio != null) checkAudio.Play();
        StartCoroutine(Continue());
    }
    private IEnumerator Continue()
    {
        yield return new WaitForSecondsRealtime(nextQuestionDelay);
        if (question < questionCount) NextQuestion();
        else
        {
            finished = true;
            StageLevelMenu.MarkCompleted(3, level);
            StageResultScreenDisplay.OpenAfterGame(3, level);
        }
    }
    private void ClearCursor()
    {
        if (cursorRect != null) Destroy(cursorRect.gameObject);
        cursorRect = null; cursorImage = null; selectedSlot = -1;
        Cursor.visible = true;
    }
    private void OnDisable()
    {
        accepting = false;
        StopAllCoroutines();
        if (voice != null) voice.Stop();
        if (speaker != null) speaker.onClick.RemoveListener(ReplayInstruction);
        ClearCursor();
    }
}
