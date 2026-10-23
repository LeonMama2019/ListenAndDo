using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Audio-only Stage5: free placement in Pencase (Lv1), books (Lv2).
public class Stage5Prototype : MonoBehaviour
{
    [SerializeField] private Stage5TaskData data;
    [SerializeField] private RectTransform deskArea;
    [SerializeField] private RectTransform bookStand;
    [SerializeField] private float moveSeconds = 0.35f;
    [SerializeField] private float nextQuestionDelay = 3f;
    private RectTransform pencase;
    private int level;
    private RectTransform odougu, odouguArea;
    private bool Uses(Stage5Place place) => question != null && ContainsPlace(question.TidyPlaces, place);
    private static bool ContainsPlace(IReadOnlyList<Stage5Place> places, Stage5Place place)
    {
        foreach (var entry in places) if (entry == place) return true;
        return false;
    }
    private RectTransform HomeArea(Book item) => item.home == Stage5Place.Pencase ? pencase : odouguArea;
    private bool FreeItem(Book item) => item != null && item.home != Stage5Place.BookStand;
    private List<Stage5Question> levelQuestions;
    private int questionIndex;
    private StageLevelResultRecorder recorder;

    private class Book
    {
        public string id;
        public Sprite front, spine;
        public Vector2 frontSize, spineSize;
        public Image deskView;
        public Slot slot;
        public bool inCase;

        public Stage5Place home;
        public float authoredAngle, currentAngle;
    }
    private class Slot
    {
        public string id;
        public Image view;
        public Vector2 originalSize, originalPosition;
        public Vector3 originalLocalPosition;
        public Book book;
    }
    [Serializable] private class Placement
    {
        public string objectId, frontSprite, location;
        public Vector2 position;
        public float rotation;
        public int drawOrder, stackLayer;
        public List<string> overlappingBelow = new();
    }
    [Serializable] private class Move
    {
        public int order;
        public float seconds;
        public string objectId, sprite, action, from, to;
        public List<Placement> arrangement;
    }
    [Serializable] private class Attempt
    {
        public string attemptId, taskId, startedAtUtc;
        public int level = 1;
        public List<Placement> initial;
        public List<string> requestedObjects = new();
        public List<string> instructionClips = new();
        public List<Move> events = new();
        public bool completed;
        public List<Placement> finalArrangement;
        public int maxStackDepth;
        public string redPencilPlacement;
        public Vector2 redPencilPosition;
        public List<string> pencilOrder = new();
    }
    private readonly List<Book> books = new();
    private readonly List<Slot> slots = new();
    private Stage5Question question;
    private Canvas canvas;
    private Image cursor;
    private AudioSource voice;
    private Book held;
    private bool heldRotated;
    private string heldFrom;
    private bool busy, accepting, completed;
    private float started;
    private Attempt attempt;
    private GameObject backPanel;
    private Camera UiCamera => canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    private bool CanAct => accepting && !busy && !completed && (backPanel == null || !backPanel.activeInHierarchy);

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>().rootCanvas;
        backPanel = Find("BackReturn")?.gameObject;
        // Copied scene has a dice tutorial. Leave assets intact, disable its UI for this prototype.
        Find("Tutorial")?.gameObject.SetActive(false);
        foreach (var old in canvas.GetComponentsInChildren<Stage2Quiz>(true)) old.enabled = false;
        foreach (var old in canvas.GetComponentsInChildren<HandSelector>(true)) old.enabled = false;
        Find("Check")?.gameObject.SetActive(false);
        level = StageLevelMenu.SelectedStage == 5 ? StageLevelMenu.SelectedLevel : 1;
        pencase = Find("Pencase") as RectTransform;
        odougu = Find("Odougu") as RectTransform;
        odouguArea = Find("OdouguArea") as RectTransform;
        foreach (var front in canvas.GetComponentsInChildren<Transform>(true))
            if (front.name == "BookFront") front.gameObject.SetActive(false);
        if (StartFirstTutorial()) return;
        if (data != null) data.MigrateFromScene(gameObject.scene);
        levelQuestions = data == null ? new List<Stage5Question>() : data.GetQuestions(level, gameObject.scene);
        question = levelQuestions.Count == 0 ? null : levelQuestions[0];
        if (question == null || !question.TryValidate(out _) || deskArea == null)
        {
            Debug.LogError("Stage5: 選択レベルの問題、音声、DeskArea、BookStandを確認してください。", this);
            enabled = false;
            return;
        }
        if (Uses(Stage5Place.Pencase) && !RegisterFreeItems(pencase, pencase, Stage5Place.Pencase)) return;
        if (Uses(Stage5Place.Odougu) && !RegisterFreeItems(odougu, odouguArea, Stage5Place.Odougu)) return;
        if (Uses(Stage5Place.BookStand))
        {
            if (bookStand == null) { Debug.LogError("BookStandがありません。", this); enabled = false; return; }
            foreach (Transform child in bookStand)
            {
                var spine = child.GetComponent<Image>();
                var front = child.Find("BookFront")?.GetComponent<Image>();
                if (spine == null || front == null) continue;
                var book = new Book { id = child.name, home = Stage5Place.BookStand, front = front.sprite, spine = spine.sprite,
                    frontSize = front.rectTransform.rect.size, spineSize = spine.rectTransform.rect.size };
                var slot = new Slot { id = child.name, view = spine, originalSize = spine.rectTransform.sizeDelta,
                    originalPosition = spine.rectTransform.anchoredPosition, originalLocalPosition = spine.rectTransform.localPosition };
                front.raycastTarget = false;
                spine.raycastTarget = true;
                SetAlpha(spine, 0);
                Bind(spine.gameObject, e => { if (held == null) SlotClick(slot); else ShelfClick(e.position); });
                books.Add(book); slots.Add(slot);
            }
            var shelfImage = bookStand.GetComponent<Image>();
            if (shelfImage != null) shelfImage.raycastTarget = true;
            Bind(bookStand.gameObject, e => ShelfClick(e.position));
        }
        var areaImage = deskArea.GetComponent<Image>();
        if (areaImage != null) { SetAlpha(areaImage, 0); areaImage.raycastTarget = true; }
        Bind(deskArea.gameObject, e => SurfaceClick(e.position));
        cursor = NewImage("Stage5Cursor", (RectTransform)canvas.transform, null, Vector2.one);
        cursor.raycastTarget = false;
        SetAlpha(cursor, 0);
        foreach (var text in GetComponentsInChildren<TMP_Text>(true)) text.enabled = false;
        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        SpeechSpeedSource.Bind(voice);
        var speaker = Find("Speaker")?.GetComponent<Button>();
        if (speaker != null)
        {
            speaker.onClick = new Button.ButtonClickedEvent();
            speaker.onClick.AddListener(Replay);
        }
        foreach (var entry in levelQuestions)
        {
            if (!entry.TryValidate(out var error) || !SamePlaces(entry.TidyPlaces, question.TidyPlaces))
            {
                Debug.LogError(error ?? "対象の親と机に残す画像を確認してください。", this);
                enabled = false; return;
            }
            foreach (var required in entry.DeskObjects)
                if (books.FindAll(b => b.front == required).Count < CountRequired(entry, required))
                { Debug.LogError(entry.TaskId + ": 対応する子オブジェクトがありません。", this); enabled = false; return; }
        }
        recorder = new StageLevelResultRecorder(5, level, levelQuestions.Count);
        BeginQuestion();
    }

    private static bool SamePlaces(IReadOnlyList<Stage5Place> a, IReadOnlyList<Stage5Place> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var place in a) if (!ContainsPlace(b, place)) return false;
        return true;
    }
    private bool RegisterFreeItems(RectTransform parent, RectTransform area, Stage5Place home)
    {
        if (parent == null || area == null)
        { Debug.LogError(home + "の親または収納Areaがありません。", this); enabled = false; return false; }
        foreach (Transform child in parent)
        {
            if (child == area) continue;
            var image = child.GetComponent<Image>();
            if (image == null || image.sprite == null) continue;
            books.Add(new Book { id = child.name, home = home, front = image.sprite,
                frontSize = image.rectTransform.rect.size, authoredAngle = image.rectTransform.localEulerAngles.z });
            image.gameObject.SetActive(false);
        }
        var graphic = area.GetComponent<Image>();
        if (graphic == null) graphic = area.gameObject.AddComponent<Image>();
        // The authored Area is a hit region; do not display its placeholder artwork.
        if (area != parent) SetAlpha(graphic, 0);
        graphic.raycastTarget = true;
        Bind(area.gameObject, e => ContainerClick(home, e.position));
        if (area != parent) Bind(parent.gameObject, e => ContainerClick(home, e.position));
        return true;
    }
    private static bool AllowsOverlap(Book a, Book b)
    {
        return a.home == Stage5Place.Odougu && b.home == Stage5Place.Odougu
            && Stackable(a.id) && Stackable(b.id);
    }
    private static bool Stackable(string id)
    {
        string key = id.Replace(" ", "").ToLowerInvariant();
        return key == "colorpencil" || key == "coloredpencil" || key == "colordpencil"
            || key == "crayon" || key == "scissors" || key == "tape" || key == "clay";
    }
    // Evaluate the proposed drop as the newest (topmost) item, without changing the scene.
    private bool ClayStacksValid(Book moving = null, RectTransform proposed = null)
    {
        foreach (var clay in books)
        {
            if (clay.home != Stage5Place.Odougu
                || !clay.id.Replace(" ", "").Equals("Clay", StringComparison.OrdinalIgnoreCase)
                || (clay != moving && (!clay.inCase || clay == held))) continue;
            var clayRect = clay == moving ? proposed : clay.deskView.rectTransform;
            int above = 0;
            foreach (var other in books)
            {
                if (other == clay || other.home != Stage5Place.Odougu
                    || (other != moving && (!other.inCase || other == held))) continue;
                bool isAbove = other == moving || (clay != moving
                    && other.deskView.transform.GetSiblingIndex() > clay.deskView.transform.GetSiblingIndex());
                var otherRect = other == moving ? proposed : other.deskView.rectTransform;
                if (isAbove && Overlaps(clayRect, otherRect) && ++above > 1) return false;
            }
        }
        return true;
    }

    private void BeginQuestion()
    {
        question = levelQuestions[questionIndex];
        completed = false;
        accepting = false;
        busy = false;
        held = null;
        SetAlpha(cursor, 0);
        Find("Check")?.gameObject.SetActive(false);
        foreach (var slot in slots)
        {
            slot.book = null;
            slot.view.rectTransform.sizeDelta = slot.originalSize;
            slot.view.rectTransform.anchoredPosition = slot.originalPosition;
            SetAlpha(slot.view, 0);
        }
        foreach (var book in books)
        {
            if (book.deskView != null)
            {
                book.deskView.gameObject.SetActive(false);
                Destroy(book.deskView.gameObject);
            }
            book.deskView = null;
            book.slot = null;
            book.inCase = false;
            book.currentAngle = book.authoredAngle;
        }
        foreach (var book in books) Scatter(book);
        attempt = new Attempt { attemptId = Guid.NewGuid().ToString("N"), taskId = question.TaskId,
            level = level, startedAtUtc = DateTime.UtcNow.ToString("o"), initial = Snapshot() };
        foreach (var sprite in question.DeskObjects) attempt.requestedObjects.Add(sprite.name);
        foreach (var clip in question.GetVoices()) if (clip != null) attempt.instructionClips.Add(clip.name);
        started = Time.unscaledTime;
        Save();
        StartCoroutine(Instruction(true));
    }

    private IEnumerator NextQuestion()
    {
        yield return new WaitForSecondsRealtime(nextQuestionDelay);
        while (backPanel != null && backPanel.activeInHierarchy) yield return null;
        questionIndex++;
        if (questionIndex < levelQuestions.Count) BeginQuestion();
        else
        {
            StageLevelMenu.MarkCompleted(5, level);
            StageResultScreenDisplay.OpenAfterGame(5, level);
        }
    }

    private Image NewImage(string name, RectTransform parent, Sprite sprite, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var image = go.GetComponent<Image>();
        image.rectTransform.SetParent(parent, false);
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        image.rectTransform.sizeDelta = size;
        image.sprite = sprite;
        image.preserveAspect = true;
        image.useSpriteMesh = true;
        return image;
    }
    private void Scatter(Book book)
    {
        book.deskView = NewImage("Scattered_" + book.id, deskArea, book.front, book.frontSize);
        book.deskView.preserveAspect = !FreeItem(book);
        float angle;
        if (FreeItem(book))
        {
            float step = book.home == Stage5Place.Pencase
                && book.id.IndexOf("pencil", StringComparison.OrdinalIgnoreCase) >= 0 ? 180f : 90f;
            angle = Mathf.Repeat(book.authoredAngle + UnityEngine.Random.Range(0, Mathf.RoundToInt(360f / step)) * step, 360f);
        }
        else angle = UnityEngine.Random.Range(-65f, 65f);
        book.deskView.rectTransform.localEulerAngles = new Vector3(0, 0, angle);
        book.currentAngle = angle;
        for (int tries = 0; tries < 1000; tries++)
        {
            book.deskView.rectTransform.anchoredPosition = RandomDeskPosition(book.frontSize, angle);
            if (!FreeItem(book) || !books.Exists(other => other != book && other.home == book.home && !AllowsOverlap(book, other) && other.deskView != null
                && other.deskView.gameObject.activeSelf && Overlaps(book.deskView.rectTransform, other.deskView.rectTransform))) break;
            if (tries == 999) { Debug.LogError("DeskAreaが狭く、重ならずに配置できません。", this); enabled = false; }
        }
        Bind(book.deskView.gameObject, e => ItemClick(book, e.position));
    }
    private Vector2 RandomDeskPosition(Vector2 size, float angle)
    {
        var area = deskArea.rect;
        var half = RotatedHalf(size, angle);
        return new Vector2(UnityEngine.Random.Range(area.xMin + half.x, area.xMax - half.x),
            UnityEngine.Random.Range(area.yMin + half.y, area.yMax - half.y));
    }
    private static Vector2 RotatedHalf(Vector2 size, float angle)
    {
        float c = Mathf.Abs(Mathf.Cos(angle * Mathf.Deg2Rad)), s = Mathf.Abs(Mathf.Sin(angle * Mathf.Deg2Rad));
        return new Vector2(c * size.x + s * size.y, s * size.x + c * size.y) * 0.5f;
    }
    private void Update()
    {
        // Tutorial narration locks storage, but must never freeze the held cursor.
        if (tutorialHeld != null && cursor != null)
        {
            FollowCursor();
            return;
        }
        if (held == null || cursor == null || busy) return;
        FollowCursor();
    }
    private void FollowCursor()
    {
        Vector2 point = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, point, UiCamera, out var local))
            cursor.rectTransform.anchoredPosition = local;
    }
    private void Pick(Book book)
    {
        if (!CanAct || held != null) return;
        heldFrom = book.inCase ? book.home.ToString() : book.slot == null ? "DeskArea" : "BookStand/" + book.slot.id;
        if (book.slot != null) { SetAlpha(book.slot.view, 0); book.slot.book = null; book.slot = null; }
        else SetAlpha(book.deskView, 0);
        held = book;
        heldRotated = false;
        cursor.preserveAspect = !FreeItem(book);
        cursor.sprite = book.front;
        cursor.rectTransform.sizeDelta = book.frontSize;
        cursor.rectTransform.localEulerAngles = new Vector3(0, 0, book.currentAngle);
        SetAlpha(cursor, 1);
        cursor.transform.SetAsLastSibling();
        Log(book, "pick", heldFrom, "Cursor");

    }
    private void ItemClick(Book item, Vector2 point)
    {
        if (!CanAct) return;
        if (held != null) SurfaceClick(point);
        else Pick(item);
    }
    private void RotateHeldOnDesk()
    {
        float step = held.home == Stage5Place.Pencase
            && held.id.IndexOf("pencil", StringComparison.OrdinalIgnoreCase) >= 0 ? 180f : 90f;
        held.currentAngle = Mathf.Repeat(held.currentAngle + step, 360f);
        cursor.rectTransform.localEulerAngles = new Vector3(0, 0, held.currentAngle);
        heldRotated = true;
        Log(held, "rotate", "Cursor", "Cursor");
    }
    private void ShelfClick(Vector2 screenPoint)
    {
        if (!CanAct || held == null || held.home != Stage5Place.BookStand) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bookStand, screenPoint, UiCamera, out var point)) return;
        bool left = point.x < bookStand.rect.center.x;
        Slot best = null;
        foreach (var slot in slots)
        {
            Vector2 center = bookStand.InverseTransformPoint(slot.view.rectTransform.parent.TransformPoint(slot.originalLocalPosition));
            if ((center.x < bookStand.rect.center.x) != left || slot.book != null
                || held.spineSize.y > slot.originalSize.y + 0.01f) continue;
            // Keep tall spaces available when a smaller book fits a shorter space.
            if (best == null || slot.originalSize.y < best.originalSize.y) best = slot;
        }
        if (best == null)
        {
            Log(held, "side_unavailable", "Cursor", left ? "BookStand/Left" : "BookStand/Right");
            return;
        }
        StartCoroutine(Store(best));
    }
    private void SlotClick(Slot slot)
    {
        if (!CanAct) return;
        if (held == null) { if (slot.book != null) Pick(slot.book); return; }
        if (held.home != Stage5Place.BookStand || slot.book != null) return;
        // Capacity is the original authored slot HEIGHT only; width does not restrict placement.
        if (held.spineSize.y > slot.originalSize.y + 0.01f)
        {
            recorder.RecordWrongAnswer();
            Log(held, "height_rejected", "Cursor", "BookStand/" + slot.id);
            return;
        }
        StartCoroutine(Store(slot));
    }
    private IEnumerator Store(Slot slot)
    {
        busy = true;
        var book = held;
        cursor.sprite = book.spine;
        cursor.rectTransform.sizeDelta = book.spineSize;
        var rect = slot.view.rectTransform;
        // Keep authored bottom edge; small books remain small even in a large slot.
        var size = new Vector2(slot.originalSize.x, book.spineSize.y);
        var targetLocal = new Vector3(0, -slot.originalSize.y * rect.pivot.y + size.y * rect.pivot.y, 0);
        Vector2 destination = slot.originalPosition + (Vector2)targetLocal;
        Vector3 target = rect.parent.TransformPoint(slot.originalLocalPosition + targetLocal);
        yield return Animate(target);
        slot.view.sprite = book.spine;
        rect.sizeDelta = size;
        rect.anchoredPosition = destination;
        slot.view.preserveAspect = true;
        slot.book = book;
        book.slot = slot;
        SetAlpha(slot.view, 1);
        if (book.deskView != null) { Destroy(book.deskView.gameObject); book.deskView = null; }
        held = null;
        SetAlpha(cursor, 0);
        Log(book, "place", heldFrom, "BookStand/" + slot.id);
        busy = false;
        EvaluateAnswer(false);
    }
    private void DeskClick(Vector2 point)
    {
        if (!CanAct || held == null) return;
        if (!RectTransformUtility.RectangleContainsScreenPoint(deskArea, point, UiCamera)) return;
        if (!heldRotated) { RotateHeldOnDesk(); return; }
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(deskArea, point, UiCamera, out var local)) return;
        var half = RotatedHalf(held.frontSize, held.currentAngle);
        var area = deskArea.rect;
        local.x = Mathf.Clamp(local.x, area.xMin + half.x, area.xMax - half.x);
        local.y = Mathf.Clamp(local.y, area.yMin + half.y, area.yMax - half.y);
        StartCoroutine(PlaceDesk(local));
    }
    private IEnumerator PlaceDesk(Vector2 local)
    {
        busy = true;
        var book = held;
        cursor.preserveAspect = !FreeItem(book);
        cursor.sprite = book.front;
        cursor.rectTransform.sizeDelta = book.frontSize;
        yield return Animate(deskArea.TransformPoint(local));
        if (book.deskView == null)
        {
            book.deskView = NewImage("Scattered_" + book.id, deskArea, book.front, book.frontSize);
            Bind(book.deskView.gameObject, e => ItemClick(book, e.position));
        }
        book.deskView.preserveAspect = !FreeItem(book);
        book.deskView.rectTransform.SetParent(deskArea, false);
        book.inCase = false;
        book.deskView.rectTransform.localEulerAngles = new Vector3(0, 0, book.currentAngle);
        book.deskView.rectTransform.anchoredPosition = local;
        SetAlpha(book.deskView, 1);
        book.deskView.transform.SetAsLastSibling();
        held = null;
        SetAlpha(cursor, 0);
        Log(book, "place", heldFrom, "DeskArea");
        busy = false;
        EvaluateAnswer(false);
    }
    private IEnumerator Animate(Vector3 target)
    {
        Vector3 from = cursor.transform.position;
        float elapsed = 0;
        while (elapsed < moveSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            cursor.transform.position = Vector3.Lerp(from, target, Mathf.SmoothStep(0, 1, elapsed / Mathf.Max(0.01f, moveSeconds)));
            yield return null;
        }
        cursor.transform.position = target;
    }
    private static int CountRequired(Stage5Question entry, Sprite sprite)
    {
        int count = 0;
        foreach (var required in entry.DeskObjects) if (required == sprite) count++;
        return count;
    }
    private bool IsCorrect(out string reason)
    {
        if (held != null) { reason = "持っている物を置いてね"; return false; }
        var remaining = new List<Sprite>(question.DeskObjects);
        foreach (var item in books)
        {
            if (item.inCase)
            {
                if (!FreeItem(item) || item.deskView.rectTransform.parent != HomeArea(item)
                    || !FullyInside(item.deskView.rectTransform, HomeArea(item)))
                { reason = "元の収納先に収めてね"; return false; }
            }
            else if (item.slot != null)
            {
                if (item.home != Stage5Place.BookStand || item.slot.book != item
                    || item.spineSize.y > item.slot.originalSize.y + 0.01f)
                { reason = "本立てに戻してね"; return false; }
            }
            else if (!FullyInsideDesk(item.deskView) || !remaining.Remove(item.front))
            { reason = "指定の物を指定数だけ机に残してね"; return false; }
        }
        for (int i = 0; i < books.Count; i++)
            for (int j = i + 1; j < books.Count; j++)
                if (books[i].inCase && books[j].inCase && books[i].home == books[j].home
                    && !AllowsOverlap(books[i], books[j])
                    && Overlaps(books[i].deskView.rectTransform, books[j].deskView.rectTransform))
                { reason = "重ならないように置いてね"; return false; }
        if (!ClayStacksValid()) { reason = "ねんどの上は1つまで置けるよ"; return false; }
        reason = remaining.Count == 0 ? null : "机に残す物が足りません";
        return remaining.Count == 0;
    }

    private void CheckAnswer() => EvaluateAnswer(true);

    private void EvaluateAnswer(bool explicitCheck)
    {
        if (!CanAct) return;
        bool correct = IsCorrect(out var reason);
        // Automatic checks only record completion, not intermediate arrangements as mistakes.
        if (!correct && !explicitCheck) return;
        attempt.events.Add(new Move { order = attempt.events.Count + 1, seconds = Time.unscaledTime - started,
            action = correct ? "answer_correct" : "answer_incorrect", arrangement = Snapshot() });
        if (!correct) recorder.RecordWrongAnswer();
        completed = correct;
        attempt.completed = correct;
        if (correct)
        {
            var check = Find("Check");
            if (check != null)
            {
                check.gameObject.SetActive(true);
                check.SetAsLastSibling();
                var image = check.GetComponent<Image>();
                if (image != null) image.raycastTarget = false;
                var sound = check.GetComponent<AudioSource>();
                if (sound != null)
                {
                    if (sound.clip != null) sound.PlayOneShot(sound.clip);
                    else Debug.LogWarning("CheckのAudioSourceにピンポーンのClipを設定してください。", check);
                }
            }
        }
        Save();
        if (correct)
        {
            recorder.SetStage5PlacementLog(JsonUtility.ToJson(attempt));
            recorder.RecordCorrect(Time.realtimeSinceStartupAsDouble);
            StartCoroutine(NextQuestion());
        }
    }
    private void SurfaceClick(Vector2 point)
    {
        if (Uses(Stage5Place.BookStand) && RectTransformUtility.RectangleContainsScreenPoint(bookStand, point, UiCamera)) ShelfClick(point);
        else if (Uses(Stage5Place.Pencase) && RectTransformUtility.RectangleContainsScreenPoint(pencase, point, UiCamera)) ContainerClick(Stage5Place.Pencase, point);
        else if (Uses(Stage5Place.Odougu) && RectTransformUtility.RectangleContainsScreenPoint(odouguArea, point, UiCamera)) ContainerClick(Stage5Place.Odougu, point);
        else DeskClick(point);
    }
    private void ContainerClick(Stage5Place home, Vector2 point)
    {
        if (!CanAct || held == null) return;
        if (held.home != home)
        { Log(held, "wrong_container_rejected", "Cursor", home.ToString()); return; }
        var area = HomeArea(held);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, point, UiCamera, out var local)) return;
        var probe = NewImage("PlacementProbe", area, held.front, held.frontSize);
        probe.preserveAspect = false;
        probe.raycastTarget = false;
        SetAlpha(probe, 0);
        probe.rectTransform.anchoredPosition = local;
        probe.rectTransform.localEulerAngles = new Vector3(0, 0, held.currentAngle);
        bool inside = FullyInside(probe.rectTransform, area);
        bool overlaps = books.Exists(other => other != held && other.inCase && other.home == home
            && !AllowsOverlap(held, other) && Overlaps(probe.rectTransform, other.deskView.rectTransform));
        bool clayStackValid = home != Stage5Place.Odougu || ClayStacksValid(held, probe.rectTransform);
        Destroy(probe.gameObject);
        if (!inside || overlaps || !clayStackValid)
        { Log(held, inside ? "overlap_rejected" : "outside_rejected", "Cursor", home.ToString()); return; }
        StartCoroutine(PlaceContainer(local));
    }
    private IEnumerator PlaceContainer(Vector2 local)
    {
        busy = true;
        var item = held;
        var area = HomeArea(item);
        yield return Animate(area.TransformPoint(local));
        item.deskView.rectTransform.SetParent(area, false);
        item.deskView.rectTransform.anchoredPosition = local;
        item.deskView.rectTransform.localEulerAngles = new Vector3(0, 0, item.currentAngle);
        item.inCase = true;
        SetAlpha(item.deskView, 1);
        item.deskView.transform.SetAsLastSibling();
        held = null;
        SetAlpha(cursor, 0);
        Log(item, "place", heldFrom, item.home.ToString());
        busy = false;
        EvaluateAnswer(false);
    }
    private static Vector3[] VisibleVertices(RectTransform rect)
    {
        var image = rect.GetComponent<Image>();
        var sprite = image.sprite;
        var vertices = sprite.vertices;
        var size = rect.rect.size;
        if (image.preserveAspect)
        {
            float ratio = sprite.rect.width / sprite.rect.height;
            if (size.x / size.y > ratio) size.x = size.y * ratio;
            else size.y = size.x / ratio;
        }
        var result = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector2 normalized = (vertices[i] * sprite.pixelsPerUnit + sprite.pivot) / sprite.rect.size;
            Vector2 local = rect.rect.center + (normalized - Vector2.one * 0.5f) * size;
            result[i] = rect.TransformPoint(local);
        }
        return result;
    }
    private static bool FullyInside(RectTransform item, RectTransform area)
    {
        foreach (var vertex in VisibleVertices(item))
        {
            Vector2 p = area.InverseTransformPoint(vertex);
            if (p.x < area.rect.xMin - 0.01f || p.x > area.rect.xMax + 0.01f
                || p.y < area.rect.yMin - 0.01f || p.y > area.rect.yMax + 0.01f) return false;
        }
        return true;
    }
    private static readonly Dictionary<Sprite, List<Rect>> scissorsMasks = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetScissorsMasks() => scissorsMasks.Clear();

    private static bool IsScissors(RectTransform rect)
    {
        var image = rect.GetComponent<Image>();
        return image != null && image.sprite != null
            && image.sprite.name.StartsWith("scissors", StringComparison.OrdinalIgnoreCase);
    }

    private static Rect DrawRect(RectTransform transform)
    {
        var image = transform.GetComponent<Image>();
        var size = transform.rect.size;
        if (image.preserveAspect)
        {
            float ratio = image.sprite.rect.width / image.sprite.rect.height;
            if (size.x / size.y > ratio) size.x = size.y * ratio;
            else size.y = size.x / ratio;
        }
        return new Rect(transform.rect.center - size * 0.5f, size);
    }

    // Exact horizontal runs of non-transparent source pixels. Equal runs on adjacent rows
    // are merged, retaining handle holes and the empty space next to the blade.
    private static List<Rect> ScissorsMask(Sprite sprite)
    {
        if (scissorsMasks.TryGetValue(sprite, out var cached)) return cached;
        if (!sprite.texture.isReadable || sprite.packed)
        {
            Debug.LogError("Stage5: scissors画像はRead/Writeを有効にし、Sprite Atlasに含めず使用してください。");
            scissorsMasks[sprite] = null;
            return null;
        }
        var pixels = sprite.texture.GetPixels32();
        int textureWidth = sprite.texture.width;
        int xOffset = Mathf.RoundToInt(sprite.rect.x), yOffset = Mathf.RoundToInt(sprite.rect.y);
        int width = Mathf.RoundToInt(sprite.rect.width), height = Mathf.RoundToInt(sprite.rect.height);
        const byte minimumAlpha = 26; // Ignore almost invisible antialiased edges (10%).
        var runs = new List<Rect>();
        var previous = new Dictionary<long, int>();
        for (int y = 0; y < height; y++)
        {
            var current = new Dictionary<long, int>();
            int x = 0;
            while (x < width)
            {
                while (x < width && pixels[(yOffset + y) * textureWidth + xOffset + x].a < minimumAlpha) x++;
                int start = x;
                while (x < width && pixels[(yOffset + y) * textureWidth + xOffset + x].a >= minimumAlpha) x++;
                if (start == x) continue;
                long key = ((long)start << 32) | (uint)x;
                if (previous.TryGetValue(key, out int index))
                {
                    var run = runs[index]; run.yMax = (y + 1f) / height; runs[index] = run;
                    current[key] = index;
                }
                else
                {
                    current[key] = runs.Count;
                    runs.Add(Rect.MinMaxRect((float)start / width, (float)y / height, (float)x / width, (y + 1f) / height));
                }
            }
            previous = current;
        }
        scissorsMasks[sprite] = runs;
        return runs;
    }

    private static Vector2[] RunCorners(RectTransform item, Rect normalized, RectTransform space)
    {
        var draw = DrawRect(item);
        float x0 = draw.xMin + normalized.xMin * draw.width, x1 = draw.xMin + normalized.xMax * draw.width;
        float y0 = draw.yMin + normalized.yMin * draw.height, y1 = draw.yMin + normalized.yMax * draw.height;
        return new[] {
            (Vector2)space.InverseTransformPoint(item.TransformPoint(new Vector3(x0, y0))),
            (Vector2)space.InverseTransformPoint(item.TransformPoint(new Vector3(x0, y1))),
            (Vector2)space.InverseTransformPoint(item.TransformPoint(new Vector3(x1, y1))),
            (Vector2)space.InverseTransformPoint(item.TransformPoint(new Vector3(x1, y0))) };
    }
    private static bool QuadsOverlap(Vector2[] a, Vector2[] b)
    {
        foreach (var polygon in new[] { a, b })
            for (int i = 0; i < 2; i++)
            {
                var edge = polygon[i + 1] - polygon[i];
                var axis = new Vector2(-edge.y, edge.x).normalized;
                float aMin = float.PositiveInfinity, aMax = float.NegativeInfinity;
                float bMin = float.PositiveInfinity, bMax = float.NegativeInfinity;
                for (int j = 0; j < 4; j++)
                {
                    float pa = Vector2.Dot(a[j], axis), pb = Vector2.Dot(b[j], axis);
                    aMin = Mathf.Min(aMin, pa); aMax = Mathf.Max(aMax, pa);
                    bMin = Mathf.Min(bMin, pb); bMax = Mathf.Max(bMax, pb);
                }
                if (aMax <= bMin + 0.00001f || bMax <= aMin + 0.00001f) return false;
            }
        return true;
    }

    private static bool Overlaps(RectTransform first, RectTransform second)
    {
        if (!IsScissors(first) && !IsScissors(second)) return MeshOverlaps(first, second);
        if (!IsScissors(first)) { var swap = first; first = second; second = swap; }
        var mask = ScissorsMask(first.GetComponent<Image>().sprite);
        if (mask == null) return MeshOverlaps(first, second);
        var full = new Rect(0, 0, 1, 1);
        var otherRect = RunCorners(second, full, second);
        if (!QuadsOverlap(RunCorners(first, full, second), otherRect)) return false;
        List<Rect> otherMask = null;
        if (IsScissors(second))
        {
            otherMask = ScissorsMask(second.GetComponent<Image>().sprite);
            if (otherMask == null) return MeshOverlaps(first, second);
        }
        foreach (var run in mask)
        {
            var quad = RunCorners(first, run, second);
            if (!QuadsOverlap(quad, otherRect)) continue;
            if (otherMask == null) return true;
            foreach (var otherRun in otherMask)
                if (QuadsOverlap(quad, RunCorners(second, otherRun, second))) return true;
        }
        return false;
    }

    // Existing mesh rule is kept unchanged for pairs without scissors.
    private static bool MeshOverlaps(RectTransform first, RectTransform second)
    {
        var a = VisibleVertices(first); var b = VisibleVertices(second);
        var space = first.GetComponentInParent<Canvas>().rootCanvas.transform;
        for (int i = 0; i < a.Length; i++) a[i] = space.InverseTransformPoint(a[i]);
        for (int i = 0; i < b.Length; i++) b[i] = space.InverseTransformPoint(b[i]);
        var at = first.GetComponent<Image>().sprite.triangles;
        var bt = second.GetComponent<Image>().sprite.triangles;
        for (int i = 0; i < at.Length; i += 3)
            for (int j = 0; j < bt.Length; j += 3)
                if (TrianglesOverlap(new[] { a[at[i]], a[at[i + 1]], a[at[i + 2]] },
                    new[] { b[bt[j]], b[bt[j + 1]], b[bt[j + 2]] })) return true;
        return false;
    }
    private static bool TrianglesOverlap(Vector3[] a, Vector3[] b)
    {
        foreach (var polygon in new[] { a, b })
            for (int i = 0; i < 3; i++)
            {
                Vector2 edge = polygon[(i + 1) % 3] - polygon[i];
                Vector2 axis = new Vector2(-edge.y, edge.x).normalized;
                float aMin = float.PositiveInfinity, aMax = float.NegativeInfinity;
                float bMin = float.PositiveInfinity, bMax = float.NegativeInfinity;
                for (int j = 0; j < 3; j++)
                {
                    float pa = Vector2.Dot(a[j], axis), pb = Vector2.Dot(b[j], axis);
                    aMin = Mathf.Min(aMin, pa); aMax = Mathf.Max(aMax, pa);
                    bMin = Mathf.Min(bMin, pb); bMax = Mathf.Max(bMax, pb);
                }
                // Treat subpixel contact at the authored edges as touching.
                if (aMax <= bMin + 0.5f || bMax <= aMin + 0.5f) return false;
            }
        return true;
    }
    private void RecordRedPencil()
    {
        if (!Uses(Stage5Place.Pencase) || attempt == null) return;
        var red = books.Find(b => b.id.Equals("Redpencil", StringComparison.OrdinalIgnoreCase));
        if (red == null) return;
        attempt.redPencilPosition = red.deskView.rectTransform.anchoredPosition;
        if (red == held) { attempt.redPencilPlacement = "held"; attempt.pencilOrder.Clear(); return; }
        if (!red.inCase) { attempt.redPencilPlacement = "desk"; attempt.pencilOrder.Clear(); return; }
        var pencils = books.FindAll(b => b.home == Stage5Place.Pencase && b.inCase && b != held && b.id.StartsWith("Pencil", StringComparison.OrdinalIgnoreCase));
        pencils.Add(red);
        // Sort across the authored pencil direction, keeping eraser out of the pencil order.
        float angle = red.authoredAngle * Mathf.Deg2Rad;
        Vector2 axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        pencils.Sort((a, b) => Vector2.Dot(a.deskView.rectTransform.anchoredPosition, axis)
            .CompareTo(Vector2.Dot(b.deskView.rectTransform.anchoredPosition, axis)));
        attempt.pencilOrder.Clear();
        foreach (var pencil in pencils) attempt.pencilOrder.Add(pencil.id);
        int index = pencils.IndexOf(red);
        attempt.redPencilPlacement = index == 0 || index == pencils.Count - 1 ? "edge" : "mixed";
    }
    private bool FullyInsideDesk(Image view)
    {
        if (view == null || view.color.a <= 0) return false;
        var corners = new Vector3[4];
        view.rectTransform.GetWorldCorners(corners);
        foreach (var corner in corners)
            {
                Vector2 local = deskArea.InverseTransformPoint(corner);
                var bounds = deskArea.rect;
                const float tolerance = 0.5f;
                if (local.x < bounds.xMin - tolerance || local.x > bounds.xMax + tolerance
                    || local.y < bounds.yMin - tolerance || local.y > bounds.yMax + tolerance)
                    return false;
            }
        return true;
    }
    private IEnumerator Instruction(bool begin = false)
    {
        accepting = false;
        foreach (var clip in question.GetVoices())
        {
            if (clip == null) continue;
            voice.clip = clip;
            voice.Play();
            while (voice.isPlaying) yield return null;
        }
        if (begin)
        {
            started = Time.unscaledTime;
            recorder.BeginQuestion(questionIndex + 1, question.TaskId, question.QuestionText, Time.realtimeSinceStartupAsDouble);
        }
        accepting = true;
    }
    private void Replay()
    {
        if (!CanAct) return;
        attempt.events.Add(new Move { order = attempt.events.Count + 1, seconds = Time.unscaledTime - started,
            action = "replay", arrangement = Snapshot() });
        Save();
        recorder.RecordReplay();
        StartCoroutine(Instruction());
    }
    private List<Placement> Snapshot()
    {
        var result = new List<Placement>();
        var storedTools = books.FindAll(item => item.home == Stage5Place.Odougu && item.inCase && item != held);
        storedTools.Sort((a, b) => a.deskView.transform.GetSiblingIndex().CompareTo(b.deskView.transform.GetSiblingIndex()));
        var depths = new Dictionary<Book, int>();
        var below = new Dictionary<Book, List<string>>();
        foreach (var item in storedTools)
        {
            int depth = 1;
            var lowerIds = new List<string>();
            foreach (var lower in storedTools)
            {
                if (lower == item) break;
                if (!Overlaps(item.deskView.rectTransform, lower.deskView.rectTransform)) continue;
                lowerIds.Add(lower.id);
                depth = Mathf.Max(depth, depths[lower] + 1);
            }
            depths[item] = depth;
            below[item] = lowerIds;
        }
        foreach (var book in books)
        {
            var rect = book == held ? cursor.rectTransform : book.slot != null ? book.slot.view.rectTransform : book.deskView?.rectTransform;
            result.Add(new Placement { objectId = book.id, frontSprite = book.front.name,
                location = book == held ? "Cursor" : book.inCase ? book.home.ToString() : book.slot != null ? "BookStand/" + book.slot.id : "DeskArea",
                position = rect != null ? rect.anchoredPosition : Vector2.zero,
                rotation = book == held && FreeItem(book) ? book.currentAngle : rect != null ? rect.localEulerAngles.z : 0,
                drawOrder = rect != null ? rect.GetSiblingIndex() : -1,
                stackLayer = depths.TryGetValue(book, out var depth) ? depth : 0,
                overlappingBelow = below.TryGetValue(book, out var ids) ? ids : new List<string>() });
        }
        return result;
    }
    private void Log(Book book, string action, string from, string to)
    {
        attempt.events.Add(new Move { order = attempt.events.Count + 1, seconds = Time.unscaledTime - started,
            objectId = book.id, sprite = book.front.name, action = action, from = from, to = to, arrangement = Snapshot() });
        Save();
    }
    private void Save()
    {
        RecordRedPencil();
        attempt.finalArrangement = Snapshot();
        attempt.maxStackDepth = 0;
        foreach (var placement in attempt.finalArrangement)
            attempt.maxStackDepth = Mathf.Max(attempt.maxStackDepth, placement.stackLayer);
        string json = JsonUtility.ToJson(attempt);
        PlayerPrefs.SetString("ListenAndDo.Stage5.Prototype." + attempt.attemptId, json);
        PlayerPrefs.SetString("ListenAndDo.Stage5.Prototype.Latest", json);
        PlayerPrefs.Save();
    }

    private const string TutorialDoneKey = "ListenAndDo.Stage5.Tutorial.Completed";
    private Transform tutorialRoot;
    private readonly List<TutorialItem> tutorialItems = new();
    private readonly HashSet<Stage5Place> tutorialFinished = new();
    private readonly Dictionary<Stage5Place, CanvasGroup> tutorialHomes = new();
    private TutorialItem tutorialHeld;
    private int tutorialStep;
    private bool tutorialBusy;
    private bool tutorialAnimating;
    private AudioSource tutorialVoice;
    private Coroutine tutorialNarration;
    private class TutorialItem
    {
        public Image original, view;
        public Stage5Place home;
        public string id;
        public bool stored;
    }

    private bool StartFirstTutorial()
    {
        tutorialRoot = Find("Tutorial");
        if (level != 1 || tutorialRoot == null
            || (!StageLevelMenu.IsTutorialReplay(5) && PlayerPrefs.GetInt(TutorialDoneKey, 0) != 0)) return false;
        foreach (Stage5Place home in Enum.GetValues(typeof(Stage5Place)))
        {
            var parent = TutorialHome(home);
            if (parent == null)
            {
                Debug.LogError("Stage5 Tutorial: " + home + "がありません。", this);
                enabled = false;
                return true;
            }
            var group = parent.GetComponent<CanvasGroup>();
            if (group == null) group = parent.gameObject.AddComponent<CanvasGroup>();
            tutorialHomes.Add(home, group);
            foreach (Transform child in parent)
            {
                var original = child.GetComponent<Image>();
                var front = home == Stage5Place.BookStand ? child.Find("BookFront")?.GetComponent<Image>() : original;
                if (child.name == "OdouguArea" || original == null || front == null || front.sprite == null) continue;
                var item = new TutorialItem { original = original, home = home, id = child.name };
                item.view = NewImage("Tutorial_" + child.name, deskArea, front.sprite, front.rectTransform.rect.size);
                item.view.rectTransform.anchoredPosition = RandomDeskPosition(front.rectTransform.rect.size, 0);
                // Preserve authored placement for the return animation; only the copy moves.
                original.gameObject.SetActive(false);
                if (home == Stage5Place.BookStand) front.gameObject.SetActive(false);
                tutorialItems.Add(item);
                Bind(item.view.gameObject, e => TutorialPick(item));
            }
            var graphic = parent.GetComponent<Image>();
            if (graphic != null) graphic.raycastTarget = true;
            Bind(parent.gameObject, e => TutorialStore(home));
            if (home == Stage5Place.Odougu && odouguArea != null)
            {
                var areaGraphic = odouguArea.GetComponent<Image>();
                if (areaGraphic == null) areaGraphic = odouguArea.gameObject.AddComponent<Image>();
                SetAlpha(areaGraphic, 0);
                areaGraphic.raycastTarget = true;
                Bind(odouguArea.gameObject, e => TutorialStore(home));
            }
        }
        cursor = NewImage("TutorialCursor", (RectTransform)canvas.transform, null, Vector2.one);
        cursor.raycastTarget = false;
        SetAlpha(cursor, 0);
        tutorialVoice = gameObject.AddComponent<AudioSource>();
        tutorialVoice.playOnAwake = false;
        SpeechSpeedSource.Bind(tutorialVoice);
        Find("Speaker")?.gameObject.SetActive(false);
        tutorialRoot.gameObject.SetActive(true);
        tutorialRoot.SetAsLastSibling();
        foreach (var graphic in tutorialRoot.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        StageTutorialBackButton.Bind(canvas, 5);
        foreach (var text in tutorialRoot.GetComponentsInChildren<TMP_Text>(true)) text.enabled = true;
        var close = tutorialRoot.Find("Button")?.GetComponent<Button>();
        if (close != null)
        {
            close.onClick = new Button.ButtonClickedEvent();
            close.onClick.AddListener(FinishTutorial);
            if (close.targetGraphic != null) close.targetGraphic.raycastTarget = true;
        }
        ShowTutorial(1);
        StartCoroutine(TutorialPulse());
        return true;
    }

    private RectTransform TutorialHome(Stage5Place home)
        => home == Stage5Place.BookStand ? bookStand : home == Stage5Place.Pencase ? pencase : odougu;

    private void ShowTutorial(int step, Stage5Place? selected = null, AudioClip objectVoice = null)
    {
        tutorialStep = step;
        foreach (Transform child in tutorialRoot) child.gameObject.SetActive(false);
        var panel = tutorialRoot.Find(step.ToString());
        if (panel != null)
        {
            panel.gameObject.SetActive(true);
            if (step >= 2 && step <= 4)
                foreach (Transform child in panel)
                    child.gameObject.SetActive(selected.HasValue && child.name.Trim().Trim('\'').Trim() == selected.Value.ToString());
        }
        // Reuse the authored selection prompt while waiting for another category.
        if (step >= 2 && step <= 4 && !selected.HasValue)
            tutorialRoot.Find("1")?.gameObject.SetActive(true);
        var button = tutorialRoot.Find("Button");
        if (button != null) button.gameObject.SetActive(step == 10);
        if (tutorialNarration != null) StopCoroutine(tutorialNarration);
        tutorialVoice.Stop();
        Transform spoken = selected.HasValue ? panel?.Find(selected.Value.ToString()) : panel;
        tutorialNarration = StartCoroutine(TutorialSpeak(spoken, objectVoice));
    }

    private IEnumerator TutorialSpeak(Transform panel, AudioClip objectVoice)
    {
        tutorialBusy = true;
        if (objectVoice != null)
        {
            tutorialVoice.clip = objectVoice;
            tutorialVoice.Play();
            while (tutorialVoice.isPlaying) yield return null;
        }
        if (panel != null)
            foreach (var source in panel.GetComponentsInChildren<AudioSource>(false))
                if (source.clip != null)
                {
                    tutorialVoice.clip = source.clip;
                    tutorialVoice.Play();
                    while (tutorialVoice.isPlaying) yield return null;
                }
        tutorialBusy = false;
    }

    private void TutorialPick(TutorialItem item)
    {
        if (tutorialAnimating || tutorialStep > 4 || tutorialHeld != null || item.stored
            || tutorialFinished.Contains(item.home) || (backPanel != null && backPanel.activeInHierarchy)) return;
        tutorialHeld = item;
        // Place the sprite at the pointer immediately, including during narration.
        SetAlpha(item.view, 0);
        cursor.sprite = item.view.sprite;
        cursor.rectTransform.sizeDelta = item.view.rectTransform.sizeDelta;
        cursor.transform.position = item.view.transform.position;
        FollowCursor();
        SetAlpha(cursor, 1);
        cursor.transform.SetAsLastSibling();
        foreach (var entry in tutorialHomes)
            TutorialHome(entry.Key).gameObject.SetActive(entry.Key == item.home);
        var objectVoice = item.home == Stage5Place.Odougu && data != null
            ? data.GetObjectVoice(item.view.sprite) : null;
        ShowTutorial(2 + tutorialFinished.Count, item.home, objectVoice);
    }

    private void TutorialStore(Stage5Place home)
    {
        if (tutorialBusy || tutorialHeld == null || tutorialHeld.home != home
            || (backPanel != null && backPanel.activeInHierarchy)) return;
        StartCoroutine(TutorialStoreGroup(home));
    }

    private IEnumerator TutorialStoreGroup(Stage5Place home)
    {
        tutorialAnimating = true;
        tutorialBusy = true;
        tutorialHomes[home].alpha = 1;
        var selected = tutorialHeld;
        selected.view.transform.position = cursor.transform.position;
        SetAlpha(selected.view, 1);
        SetAlpha(cursor, 0);
        tutorialHeld = null;
        yield return TutorialReturn(selected);
        foreach (var item in tutorialItems)
            if (item.home == home && !item.stored) yield return TutorialReturn(item);
        tutorialFinished.Add(home);
        foreach (var entry in tutorialHomes)
        {
            entry.Value.alpha = 1;
            TutorialHome(entry.Key).gameObject.SetActive(true);
        }
        tutorialBusy = false;
        tutorialAnimating = false;
        if (tutorialFinished.Count < 3) ShowTutorial(2 + tutorialFinished.Count);
        else StartCoroutine(TutorialSpecials());
    }

    private IEnumerator TutorialReturn(TutorialItem item)
    {
        SetAlpha(item.view, 1);
        var rect = item.view.rectTransform;
        Vector3 start = rect.position, destination = item.original.transform.position;
        float duration = Mathf.Max(0.35f, moveSeconds);
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            rect.position = Vector3.Lerp(start, destination, Mathf.SmoothStep(0, 1, t / duration));
            yield return null;
        }
        rect.position = destination;
        item.view.gameObject.SetActive(false);
        item.original.gameObject.SetActive(true);
        SetAlpha(item.original, 1);
        item.stored = true;
    }

    private IEnumerator TutorialBlink(Image image, int count, float halfPeriod)
    {
        for (int i = 0; i < count; i++)
        {
            SetAlpha(image, 0.2f);
            yield return new WaitForSecondsRealtime(halfPeriod);
            SetAlpha(image, 1);
            yield return new WaitForSecondsRealtime(halfPeriod);
        }
    }

    private IEnumerator TutorialSpecials()
    {
        tutorialAnimating = true;
        foreach (var spec in new[] { "Clay", "Paste" })
        {
            ShowTutorial(spec == "Clay" ? 5 : 6);
            while (tutorialBusy) yield return null;
            tutorialBusy = true;
            var item = tutorialItems.Find(entry => entry.id.Equals(spec, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                item.original.gameObject.SetActive(false);
                item.stored = false;
                item.view.rectTransform.SetParent(deskArea, false);
                item.view.rectTransform.anchoredPosition = Vector2.zero;
                item.view.gameObject.SetActive(true);
                yield return TutorialBlink(item.view, 2, 0.25f);
                yield return TutorialReturn(item);
                if (spec == "Clay")
                {
                    var scissors = tutorialItems.Find(entry =>
                        entry.id.Equals("Scissors", StringComparison.OrdinalIgnoreCase));
                    if (scissors != null)
                    {
                        scissors.original.rectTransform.SetAsLastSibling();
                        yield return TutorialBlink(scissors.original, 3, 0.6f);
                    }
                    else Debug.LogError("Stage5 Tutorial: Scissorsがありません。", this);
                }
                else yield return TutorialBlink(item.original, 3, 0.6f);
            }
            else Debug.LogError("Stage5 Tutorial: " + spec + "がありません。", this);
        }
        foreach (var front in canvas.GetComponentsInChildren<Transform>(true))
            if (front.name == "BookFront") front.gameObject.SetActive(false);
        yield return TutorialManipulationDemo();
        tutorialAnimating = false;
        ShowTutorial(10);
    }

    // Demonstrate all three clicks automatically; no player input is required.
    private IEnumerator TutorialManipulationDemo()
    {
        var item = tutorialItems.Find(entry => entry.home == Stage5Place.Odougu
            && entry.id.Equals("Crayon", StringComparison.OrdinalIgnoreCase));
        if (item == null)
        {
            Debug.LogError("Stage5 Tutorial: Crayonがありません。", this);
            yield break;
        }
        var rect = item.view.rectTransform;
        item.original.gameObject.SetActive(false);
        item.stored = false;
        rect.SetParent(deskArea, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchoredPosition = Vector2.zero;
        item.view.gameObject.SetActive(true);
        SetAlpha(item.view, 1);
        var pointerSprite = TutorialPointerSprite();
        var pointer = NewImage("Stage5TutorialPointer", (RectTransform)canvas.transform,
            pointerSprite, new Vector2(40, 50));
        pointer.raycastTarget = false;
        pointer.rectTransform.pivot = new Vector2(0, 1);
        pointer.transform.position = canvas.transform.TransformPoint(
            canvas.transform.InverseTransformPoint(rect.position) + new Vector3(110, -90));
        try
        {
            ShowTutorial(7);
            yield return TutorialDemoMove(pointer.rectTransform, rect.position, 0.6f);
            yield return TutorialDemoClick(pointer, rect);
            Vector3 lifted = canvas.transform.TransformPoint(
                canvas.transform.InverseTransformPoint(rect.position) + new Vector3(0, 55));
            yield return TutorialDemoMove(rect, lifted, 0.45f, pointer.rectTransform);
            while (tutorialBusy) yield return null;
            yield return new WaitForSecondsRealtime(2f);

            ShowTutorial(8);
            // Keep the object held while demonstrating the second click on DeskArea.
            Vector3 deskPoint = deskArea.TransformPoint(deskArea.rect.center);
            yield return TutorialDemoMove(rect, deskPoint, 0.6f, pointer.rectTransform);
            yield return TutorialDemoClick(pointer, rect);
            Quaternion startRotation = rect.localRotation;
            Quaternion endRotation = startRotation * Quaternion.Euler(0, 0, 90);
            for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                rect.localRotation = Quaternion.Slerp(startRotation, endRotation, t / 0.5f);
                yield return null;
            }
            rect.localRotation = endRotation;
            while (tutorialBusy) yield return null;
            yield return new WaitForSecondsRealtime(2f);

            ShowTutorial(9);
            Vector3 storagePoint = odouguArea != null ? odouguArea.position : odougu.position;
            yield return TutorialDemoMove(rect, storagePoint, 0.8f, pointer.rectTransform);
            yield return TutorialDemoClick(pointer, rect);
            yield return TutorialReturn(item);
            pointer.gameObject.SetActive(false);
            while (tutorialBusy) yield return null;
            yield return new WaitForSecondsRealtime(2f);
        }
        finally
        {
            Destroy(pointer.gameObject);
            Destroy(pointerSprite.texture);
            Destroy(pointerSprite);
        }
    }

    private IEnumerator TutorialDemoMove(RectTransform rect, Vector3 destination, float duration,
        RectTransform pointer = null)
    {
        Vector3 start = rect.position;
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            rect.position = Vector3.Lerp(start, destination, Mathf.SmoothStep(0, 1, t / duration));
            if (pointer != null) pointer.position = rect.position;
            yield return null;
        }
        rect.position = destination;
        if (pointer != null) pointer.position = destination;
    }

    private IEnumerator TutorialDemoClick(Image pointer, RectTransform item)
    {
        yield return new WaitForSecondsRealtime(0.3f);
        pointer.rectTransform.localScale = Vector3.one * 0.8f;
        item.localScale = Vector3.one * 0.92f;
        var rays = new List<Image>();
        for (int i = 0; i < 4; i++)
        {
            float angle = (45 + i * 90) * Mathf.Deg2Rad;
            var ray = NewImage("ClickRay", (RectTransform)canvas.transform, null, new Vector2(6, 18));
            ray.color = new Color(1f, 0.65f, 0.05f);
            ray.raycastTarget = false;
            ray.rectTransform.position = canvas.transform.TransformPoint(
                canvas.transform.InverseTransformPoint(pointer.rectTransform.position)
                + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * 28f);
            ray.rectTransform.localRotation = Quaternion.Euler(0, 0, i * 90 - 45);
            rays.Add(ray);
        }
        yield return new WaitForSecondsRealtime(0.2f);
        foreach (var ray in rays) Destroy(ray.gameObject);
        pointer.rectTransform.localScale = Vector3.one;
        item.localScale = Vector3.one;
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
    private IEnumerator TutorialPulse()
    {
        while (tutorialRoot != null && tutorialRoot.gameObject.activeSelf)
        {
            if (!tutorialBusy && (backPanel == null || !backPanel.activeInHierarchy))
            {
                float alpha = 0.4f + 0.6f * (0.5f + 0.5f * Mathf.Cos(Time.unscaledTime * 5));
                if (tutorialHeld != null)
                {
                    tutorialHomes[tutorialHeld.home].alpha = alpha;
                    // Pointer following runs in Update independently of narration.
                }
                else if (tutorialStep <= 4)
                    foreach (var item in tutorialItems)
                        if (!item.stored) SetAlpha(item.view, alpha);
            }
            yield return null;
        }
    }

    private void FinishTutorial()
    {
        if (tutorialStep != 10 || tutorialBusy) return;
        PlayerPrefs.SetInt(TutorialDoneKey, 1);
        PlayerPrefs.Save();
        if (!StageLevelMenu.ReturnFromTutorial(5))
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);
    }


    private static void SetAlpha(Image image, float alpha) { var color = image.color; color.a = alpha; image.color = color; }
    private static void Bind(GameObject go, Action<UnityEngine.EventSystems.PointerEventData> action)
    {
        var click = go.GetComponent<Stage5ClickTarget>();
        if (click == null) click = go.AddComponent<Stage5ClickTarget>();
        click.Bind(action);
    }
    private static bool IsTutorialChild(Transform child)
    {
        for (var parent = child.parent; parent != null; parent = parent.parent)
            if (parent.name == "Tutorial") return true;
        return false;
    }

    private Transform Find(string name)
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name && (name == "Tutorial" || !IsTutorialChild(child))) return child;
        return null;
    }
}



