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
    private bool CaseQuestion => question.TidyPlaces.Count == 1 && question.TidyPlaces[0] == Stage5Place.Pencase;
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
        public List<Move> events = new();
        public bool completed;
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
        foreach (var front in canvas.GetComponentsInChildren<Transform>(true))
            if (front.name == "BookFront") front.gameObject.SetActive(false);
        levelQuestions = data == null ? new List<Stage5Question>() : data.GetQuestions(level);
        question = levelQuestions.Count == 0 ? null : levelQuestions[0];
        if (question == null || !question.TryValidate(out _) || deskArea == null || bookStand == null)
        {
            Debug.LogError("Stage5: 選択レベルの問題、音声、DeskArea、BookStandを確認してください。", this);
            enabled = false;
            return;
        }
        if (CaseQuestion)
        {
            if (pencase == null) { Debug.LogError("Pencaseがありません。", this); enabled = false; return; }
            foreach (Transform child in pencase)
            {
                var image = child.GetComponent<Image>();
                if (image == null || image.sprite == null) continue;
                books.Add(new Book { id = child.name, front = image.sprite,
                    frontSize = image.rectTransform.rect.size, authoredAngle = image.rectTransform.localEulerAngles.z });
                image.gameObject.SetActive(false);
            }
            Bind(pencase.gameObject, e => CaseClick(e.position));
        }
        else
        {
            foreach (Transform child in bookStand)
            {
                var spine = child.GetComponent<Image>();
                var front = child.Find("BookFront")?.GetComponent<Image>();
                if (spine == null || front == null) continue;
                var book = new Book { id = child.name, front = front.sprite, spine = spine.sprite,
                    frontSize = front.rectTransform.rect.size, spineSize = spine.rectTransform.rect.size };
                var slot = new Slot { id = child.name, view = spine, originalSize = spine.rectTransform.sizeDelta,
                    originalPosition = spine.rectTransform.anchoredPosition, originalLocalPosition = spine.rectTransform.localPosition };
                front.raycastTarget = false;
                spine.raycastTarget = true;
                SetAlpha(spine, 0);
                Bind(spine.gameObject, e => SlotClick(slot));
                books.Add(book);
                slots.Add(slot);
            }
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
            var expected = CaseQuestion ? Stage5Place.Pencase : Stage5Place.BookStand;
            if (!entry.TryValidate(out var error) || entry.TidyPlaces.Count != 1 || entry.TidyPlaces[0] != expected
                || (CaseQuestion && entry.DeskObjects.Count != 1))
            {
                Debug.LogError(error ?? "対象の親と机に残す画像を確認してください。", this);
                enabled = false; return;
            }
            foreach (var required in entry.DeskObjects)
                if (!books.Exists(b => b.front == required))
                { Debug.LogError(entry.TaskId + ": 対応する子オブジェクトがありません。", this); enabled = false; return; }
        }
        recorder = new StageLevelResultRecorder(5, level, levelQuestions.Count);
        BeginQuestion();
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
            Scatter(book);
        }
        attempt = new Attempt { attemptId = Guid.NewGuid().ToString("N"), taskId = question.TaskId,
            level = level, startedAtUtc = DateTime.UtcNow.ToString("o"), initial = Snapshot() };
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
        if (CaseQuestion) { image.useSpriteMesh = true; image.preserveAspect = false; }
        return image;
    }
    private void Scatter(Book book)
    {
        book.deskView = NewImage("Scattered_" + book.id, deskArea, book.front, book.frontSize);
        float angle = UnityEngine.Random.Range(-65f, 65f);
        book.deskView.rectTransform.localEulerAngles = new Vector3(0, 0, angle);
        for (int tries = 0; tries < 1000; tries++)
        {
            book.deskView.rectTransform.anchoredPosition = RandomDeskPosition(book.frontSize, angle);
            if (!CaseQuestion || !books.Exists(other => other != book && other.deskView != null
                && other.deskView.gameObject.activeSelf && Overlaps(book.deskView.rectTransform, other.deskView.rectTransform))) break;
            if (tries == 999) { Debug.LogError("DeskAreaが狭く、重ならずに配置できません。", this); enabled = false; }
        }
        Bind(book.deskView.gameObject, e => { if (held == null) Pick(book); else if (held == book && CaseQuestion) RotateHeld(); else SurfaceClick(e.position); });
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
        heldFrom = book.inCase ? "Pencase" : book.slot == null ? "DeskArea" : "BookStand/" + book.slot.id;
        if (book.slot != null) { SetAlpha(book.slot.view, 0); book.slot.book = null; book.slot = null; }
        else SetAlpha(book.deskView, 0);
        held = book;
        cursor.sprite = book.front;
        cursor.rectTransform.sizeDelta = book.frontSize;
        cursor.rectTransform.localEulerAngles = new Vector3(0, 0, CaseQuestion ? book.currentAngle : 0);
        SetAlpha(cursor, 1);
        cursor.transform.SetAsLastSibling();
        Log(book, "pick", heldFrom, "Cursor");
        if (CaseQuestion) RotateHeld();
    }
    private void RotateHeld()
    {
        if (!CanAct || held == null || !CaseQuestion) return;
        float step;
        if (held.id.Equals("Eraser", StringComparison.OrdinalIgnoreCase)) step = 90f;
        else if (held.id.IndexOf("pencil", StringComparison.OrdinalIgnoreCase) >= 0) step = 180f;
        else return;
        held.currentAngle = Mathf.Repeat(held.currentAngle + step, 360f);
        cursor.rectTransform.localEulerAngles = new Vector3(0, 0, held.currentAngle);
        Log(held, "rotate", "Cursor", "Cursor");
    }
    private void SlotClick(Slot slot)
    {
        if (!CanAct) return;
        if (held == null) { if (slot.book != null) Pick(slot.book); return; }
        if (slot.book != null) return;
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
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(deskArea, point, UiCamera, out var local)) return;
        var half = RotatedHalf(held.frontSize, CaseQuestion ? held.currentAngle : 0);
        var area = deskArea.rect;
        local.x = Mathf.Clamp(local.x, area.xMin + half.x, area.xMax - half.x);
        local.y = Mathf.Clamp(local.y, area.yMin + half.y, area.yMax - half.y);
        StartCoroutine(PlaceDesk(local));
    }
    private IEnumerator PlaceDesk(Vector2 local)
    {
        busy = true;
        var book = held;
        cursor.sprite = book.front;
        cursor.rectTransform.sizeDelta = book.frontSize;
        yield return Animate(deskArea.TransformPoint(local));
        if (book.deskView == null)
        {
            book.deskView = NewImage("Scattered_" + book.id, deskArea, book.front, book.frontSize);
            Bind(book.deskView.gameObject, e => { if (held == null) Pick(book); else if (held == book && CaseQuestion) RotateHeld(); else SurfaceClick(e.position); });
        }
        book.deskView.rectTransform.SetParent(deskArea, false);
        book.inCase = false;
        book.deskView.rectTransform.localEulerAngles = new Vector3(0, 0, CaseQuestion ? book.currentAngle : 0);
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
    private bool IsCorrect(out string reason)
    {
        if (held != null) { reason = "持っている本を置いてね"; return false; }
        if (CaseQuestion) return IsCaseCorrect(out reason);
        foreach (var book in books)
        {
            bool required = false;
            foreach (var sprite in question.DeskObjects)
                if (book.front == sprite) required = true;
            if (required)
            {
                if (book.slot != null || !FullyInsideDesk(book.deskView))
                {
                    reason = book.id + "を机の上に置いてね";
                    return false;
                }
            }
            else if (book.slot == null || book.slot.book != book
                || book.spineSize.y > book.slot.originalSize.y + 0.01f)
            {
                reason = book.id + "を本立てに戻してね";
                return false;
            }
        }
        reason = null;
        return true;
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
        if (CaseQuestion && RectTransformUtility.RectangleContainsScreenPoint(pencase, point, UiCamera)) CaseClick(point);
        else DeskClick(point);
    }
    private void CaseClick(Vector2 point)
    {
        if (!CanAct || held == null) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(pencase, point, UiCamera, out var local)) return;
        var probe = NewImage("PlacementProbe", pencase, held.front, held.frontSize);
        probe.raycastTarget = false;
        SetAlpha(probe, 0);
        probe.rectTransform.anchoredPosition = local;
        probe.rectTransform.localEulerAngles = new Vector3(0, 0, held.currentAngle);
        bool inside = FullyInside(probe.rectTransform, pencase);
        bool overlaps = books.Exists(other => other != held && other.inCase
            && Overlaps(probe.rectTransform, other.deskView.rectTransform));
        Destroy(probe.gameObject);
        if (!inside || overlaps)
        {
            Log(held, inside ? "overlap_rejected" : "outside_rejected", "Cursor", "Pencase");
            return;
        }
        StartCoroutine(PlaceCase(local));
    }
    private IEnumerator PlaceCase(Vector2 local)
    {
        busy = true;
        var item = held;
        yield return Animate(pencase.TransformPoint(local));
        item.deskView.rectTransform.SetParent(pencase, false);
        item.deskView.rectTransform.anchoredPosition = local;
        item.deskView.rectTransform.localEulerAngles = new Vector3(0, 0, item.currentAngle);
        item.inCase = true;
        SetAlpha(item.deskView, 1);
        item.deskView.transform.SetAsLastSibling();
        held = null;
        SetAlpha(cursor, 0);
        Log(item, "place", heldFrom, "Pencase");
        busy = false;
        EvaluateAnswer(false);
    }
    private bool IsCaseCorrect(out string reason)
    {
        int deskCount = 0;
        foreach (var item in books)
        {
            if (!item.inCase)
            {
                if (!FullyInsideDesk(item.deskView) || item.front != question.DeskObjects[0])
                { reason = "指定の物を机に残してね"; return false; }
                deskCount++;
            }
            else if (!FullyInside(item.deskView.rectTransform, pencase))
            { reason = "ペンケースに収めてね"; return false; }
        }
        for (int i = 0; i < books.Count; i++)
            for (int j = i + 1; j < books.Count; j++)
                if (books[i].inCase && books[j].inCase && Overlaps(books[i].deskView.rectTransform, books[j].deskView.rectTransform))
                { reason = "重ならないように置いてね"; return false; }
        reason = null;
        return deskCount == 1;
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
        if (!CaseQuestion || attempt == null) return;
        var red = books.Find(b => b.id.Equals("Redpencil", StringComparison.OrdinalIgnoreCase));
        if (red == null) return;
        attempt.redPencilPosition = red.deskView.rectTransform.anchoredPosition;
        if (red == held) { attempt.redPencilPlacement = "held"; attempt.pencilOrder.Clear(); return; }
        if (!red.inCase) { attempt.redPencilPlacement = "desk"; attempt.pencilOrder.Clear(); return; }
        var pencils = books.FindAll(b => b.inCase && b != held && b.id.StartsWith("Pencil", StringComparison.OrdinalIgnoreCase));
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
        voice.clip = question.QuestionVoice;
        voice.Play();
        while (voice.isPlaying) yield return null;
        if (begin)
        {
            started = Time.unscaledTime;
            recorder.BeginQuestion(questionIndex + 1, question.TaskId, "", Time.realtimeSinceStartupAsDouble);
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
        foreach (var book in books)
        {
            var rect = book.slot != null ? book.slot.view.rectTransform : book.deskView?.rectTransform;
            result.Add(new Placement { objectId = book.id, frontSprite = book.front.name,
                location = book == held ? "Cursor" : book.inCase ? "Pencase" : book.slot != null ? "BookStand/" + book.slot.id : "DeskArea",
                position = rect != null ? rect.anchoredPosition : Vector2.zero,
                rotation = book == held && CaseQuestion ? book.currentAngle : rect != null ? rect.localEulerAngles.z : 0 });
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
