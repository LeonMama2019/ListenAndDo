using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Level 1 / Task 1 review prototype. Uses the authored scene layout as home positions.
public class Stage5Prototype : MonoBehaviour
{
    [SerializeField] private Stage5TaskData data;
    [SerializeField] private RectTransform deskArea;
    [SerializeField] private RectTransform bookStand;
    [SerializeField] private float moveSeconds = 0.35f;

    private class Book
    {
        public string id;
        public Sprite front, spine;
        public Vector2 frontSize, spineSize;
        public Image deskView;
        public Slot slot;
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
    }
    private readonly List<Book> books = new();
    private readonly List<Slot> slots = new();
    private Stage5Question question;
    private Canvas canvas;
    private Image cursor;
    private TMP_Text status;
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
        question = data == null ? null : data.GetQuestions(1).Find(q => q.TaskId == "1");
        if (question == null || !question.TryValidate(out _) || deskArea == null || bookStand == null)
        {
            Debug.LogError("Stage5: Level 1 Task ID 1、音声、DeskArea、BookStandを確認してください。", this);
            enabled = false;
            return;
        }
        if (question.TidyPlaces.Count != 1 || question.TidyPlaces[0] != Stage5Place.BookStand)
        {
            Debug.LogError("この確認用試作はBookStandだけを対象にします。", this);
            enabled = false;
            return;
        }
        foreach (Transform child in bookStand)
        {
            var spine = child.GetComponent<Image>();
            var front = child.Find("BookFront")?.GetComponent<Image>();
            if (spine == null || front == null) continue;
            var book = new Book { id = child.name, front = front.sprite, spine = spine.sprite,
                frontSize = front.rectTransform.rect.size, spineSize = spine.rectTransform.rect.size };
            var slot = new Slot { id = child.name, view = spine, originalSize = spine.rectTransform.sizeDelta, originalPosition = spine.rectTransform.anchoredPosition, originalLocalPosition = spine.rectTransform.localPosition };
            front.color = new Color(1, 1, 1, 0);
            front.raycastTarget = false;
            spine.raycastTarget = true;
            SetAlpha(spine, 0);
            Bind(spine.gameObject, e => SlotClick(slot));
            books.Add(book);
            slots.Add(slot);
        }
        foreach (var required in question.DeskObjects)
            if (!books.Exists(b => b.front == required))
            {
                Debug.LogError("机に残すSpriteに対応するBookFrontがありません: " + required.name, this);
                enabled = false;
                return;
            }
        var areaImage = deskArea.GetComponent<Image>();
        if (areaImage != null) { SetAlpha(areaImage, 0); areaImage.raycastTarget = true; }
        Bind(deskArea.gameObject, e => DeskClick(e.position));
        cursor = NewImage("Stage5Cursor", (RectTransform)canvas.transform, null, Vector2.one);
        cursor.raycastTarget = false;
        SetAlpha(cursor, 0);
        CreateControls();
        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        SpeechSpeedSource.Bind(voice);
        var speaker = Find("Speaker")?.GetComponent<Button>();
        if (speaker != null)
        {
            speaker.onClick = new Button.ButtonClickedEvent();
            speaker.onClick.AddListener(Replay);
        }
        foreach (var book in books) Scatter(book);
        started = Time.unscaledTime;
        attempt = new Attempt { attemptId = Guid.NewGuid().ToString("N"), taskId = question.TaskId,
            startedAtUtc = DateTime.UtcNow.ToString("o"), initial = Snapshot() };
        Save();
        StartCoroutine(Instruction());
    }

    private void CreateControls()
    {
        status = NewText("Stage5Instruction", new Vector2(0, 245), new Vector2(720, 65));
        status.text = question.QuestionText;
        var buttonImage = NewImage("Stage5AnswerButton", (RectTransform)transform, null, new Vector2(210, 55));
        buttonImage.rectTransform.anchoredPosition = new Vector2(230, -260);
        buttonImage.color = new Color(0.9f, 0.65f, 0.25f);
        var button = buttonImage.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(CheckAnswer);
        var label = NewText("Stage5AnswerLabel", Vector2.zero, new Vector2(210, 55), buttonImage.rectTransform);
        label.text = "できた！";
    }
    private TMP_Text NewText(string name, Vector2 position, Vector2 size, RectTransform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent != null ? parent : transform, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        var text = go.GetComponent<TextMeshProUGUI>();
        foreach (var existing in canvas.GetComponentsInChildren<TMP_Text>(true))
            if (existing != text && existing.font != null) { text.font = existing.font; break; }
        text.fontSize = 24;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.12f, 0.1f, 0.2f);
        text.raycastTarget = false;
        return text;
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
        return image;
    }
    private void Scatter(Book book)
    {
        book.deskView = NewImage("Scattered_" + book.id, deskArea, book.front, book.frontSize);
        float angle = UnityEngine.Random.Range(-65f, 65f);
        book.deskView.rectTransform.localEulerAngles = new Vector3(0, 0, angle);
        book.deskView.rectTransform.anchoredPosition = RandomDeskPosition(book.frontSize, angle);
        Bind(book.deskView.gameObject, e => { if (held == null) Pick(book); else DeskClick(e.position); });
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
        heldFrom = book.slot == null ? "DeskArea" : "BookStand/" + book.slot.id;
        if (book.slot != null) { SetAlpha(book.slot.view, 0); book.slot.book = null; book.slot = null; }
        else SetAlpha(book.deskView, 0);
        held = book;
        cursor.sprite = book.front;
        cursor.rectTransform.sizeDelta = book.frontSize;
        cursor.rectTransform.localRotation = Quaternion.identity;
        SetAlpha(cursor, 1);
        cursor.transform.SetAsLastSibling();
        Log(book, "pick", heldFrom, "Cursor");
    }
    private void SlotClick(Slot slot)
    {
        if (!CanAct) return;
        if (held == null) { if (slot.book != null) Pick(slot.book); return; }
        if (slot.book != null) { status.text = "ここには本が入っているよ"; return; }
        // Capacity is the original authored slot HEIGHT only; width does not restrict placement.
        if (held.spineSize.y > slot.originalSize.y + 0.01f)
        {
            status.text = "この本は高さが合わないよ";
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
        status.text = question.QuestionText;
        busy = false;
    }
    private void DeskClick(Vector2 point)
    {
        if (!CanAct || held == null) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(deskArea, point, UiCamera, out var local)) return;
        var half = held.frontSize * 0.5f;
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
            Bind(book.deskView.gameObject, e => { if (held == null) Pick(book); else DeskClick(e.position); });
        }
        book.deskView.rectTransform.localRotation = Quaternion.identity;
        book.deskView.rectTransform.anchoredPosition = local;
        SetAlpha(book.deskView, 1);
        book.deskView.transform.SetAsLastSibling();
        held = null;
        SetAlpha(cursor, 0);
        Log(book, "place", heldFrom, "DeskArea");
        status.text = question.QuestionText;
        busy = false;
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
    private void CheckAnswer()
    {
        if (!CanAct) return;
        bool correct = held == null;
        foreach (var book in books)
        {
            bool required = false;
            foreach (var sprite in question.DeskObjects) if (book.front == sprite) required = true;
            correct &= required ? book.slot == null && book != held && FullyInsideDesk(book.deskView)
                : book.slot != null && book.slot.book == book && book.spineSize.y <= book.slot.originalSize.y + 0.01f;
        }
        attempt.events.Add(new Move { order = attempt.events.Count + 1, seconds = Time.unscaledTime - started,
            action = correct ? "answer_correct" : "answer_incorrect", arrangement = Snapshot() });
        status.text = correct ? "正解！赤い本が机に出せたね" : "机に残す本と、本立てを確認してね";
        completed = correct;
        attempt.completed = correct;
        Save();
    }
    private bool FullyInsideDesk(Image view)
    {
        if (view == null || view.color.a <= 0) return false;
        var corners = new Vector3[4];
        view.rectTransform.GetWorldCorners(corners);
        foreach (var corner in corners)
            if (!deskArea.rect.Contains((Vector2)deskArea.InverseTransformPoint(corner))) return false;
        return true;
    }
    private IEnumerator Instruction()
    {
        accepting = false;
        voice.clip = question.QuestionVoice;
        voice.Play();
        while (voice.isPlaying) yield return null;
        accepting = true;
    }
    private void Replay()
    {
        if (!CanAct) return;
        attempt.events.Add(new Move { order = attempt.events.Count + 1, seconds = Time.unscaledTime - started,
            action = "replay", arrangement = Snapshot() });
        Save();
        StartCoroutine(Instruction());
    }
    private List<Placement> Snapshot()
    {
        var result = new List<Placement>();
        foreach (var book in books)
        {
            var rect = book.slot != null ? book.slot.view.rectTransform : book.deskView?.rectTransform;
            result.Add(new Placement { objectId = book.id, frontSprite = book.front.name,
                location = book == held ? "Cursor" : book.slot != null ? "BookStand/" + book.slot.id : "DeskArea",
                position = rect != null ? rect.anchoredPosition : Vector2.zero,
                rotation = rect != null ? rect.localEulerAngles.z : 0 });
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
