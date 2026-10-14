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
            || key == "crayon" || key == "scissors" || key == "tape";
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
        if (held == null || cursor == null || busy) return;
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
        Destroy(probe.gameObject);
        if (!inside || overlaps)
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
    // Test rendered tight sprite meshes; transparent margins do not block placement.
    private static bool Overlaps(RectTransform first, RectTransform second)
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
    private static void SetAlpha(Image image, float alpha) { var color = image.color; color.a = alpha; image.color = color; }
    private static void Bind(GameObject go, Action<UnityEngine.EventSystems.PointerEventData> action)
    {
        var click = go.GetComponent<Stage5ClickTarget>() ?? go.AddComponent<Stage5ClickTarget>();
        click.Bind(action);
    }
    private Transform Find(string name)
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
        return null;
    }
}


