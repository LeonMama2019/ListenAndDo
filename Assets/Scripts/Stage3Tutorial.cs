using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Stage3Tutorial : MonoBehaviour
{
    [SerializeField] private GameObject[] steps = new GameObject[5];
    [SerializeField, Min(0f)] private float promptDelay = 2f;
    private Stage3ObjectData data;
    private Stage3ObjectEntry cup;
    private Image cupView, carried;
    private Stage3BoxTarget yellowBox;
    private Canvas canvas;
    private AudioSource instructionVoice;
    private UIBlink blinking;
    private Func<bool> inputBlocked;
    private int step;
    private bool canSelect;
    private Camera UiCamera => canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

    public bool Begin(Stage3ObjectData source, Image[] views, Stage3BoxTarget[] boxes, Func<bool> blocked)
    {
        if (steps == null || steps.Length != 5 || Array.Exists(steps, x => x == null))
        { Debug.LogError("Stage3 Tutorial: assign steps 1-5.", this); return false; }
        data = source;
        foreach (var entry in data.Items) if (entry.ObjectId == "blue_cup") cup = entry;
        foreach (var box in boxes) if (box.BoxId == "yellow_box") yellowBox = box;
        if (cup == null || !cup.IsReady || yellowBox == null)
        { Debug.LogError("Stage3 Tutorial: blue_cup and yellow_box are required.", this); return false; }
        inputBlocked = blocked;
        canvas = GetComponentInParent<Canvas>().rootCanvas;
        cupView = views[0];
        // Use a fixed display, independent of the level's random question generator.
        for (int i = 0; i < views.Length; i++)
            views[i].sprite = i == 0 ? cup.Sprite : data.Items[i].Sprite;
        foreach (var graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        foreach (var audio in GetComponentsInChildren<AudioSource>(true))
        { audio.playOnAwake = false; audio.Stop(); }
        instructionVoice = gameObject.AddComponent<AudioSource>();
        instructionVoice.playOnAwake = false;
        instructionVoice.spatialBlend = 0;
        SpeechSpeedSource.Bind(instructionVoice);
        var boxInput = yellowBox.GetComponent<Stage3TutorialBoxInput>();
        if (boxInput == null) boxInput = yellowBox.gameObject.AddComponent<Stage3TutorialBoxInput>();
        boxInput.Tutorial = this;
        ConnectButton(1);
        ConnectButton(2);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Show(1);
        return true;
    }

    private void ConnectButton(int number)
    {
        var buttons = steps[number - 1].GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() =>
            {
                if (step == number && !Blocked) Show(number + 1);
            });
            if (button.targetGraphic != null) button.targetGraphic.raycastTarget = true;
        }
        if (buttons.Length == 0) Debug.LogError("Stage3 Tutorial: missing step " + number + " Button.", this);
    }
    private bool Blocked => inputBlocked != null && inputBlocked();
    private void Show(int number)
    {
        StopAllCoroutines();
        StopBlink();
        foreach (var audio in GetComponentsInChildren<AudioSource>(true)) audio.Stop();
        step = number;
        canSelect = false;
        for (int i = 0; i < steps.Length; i++) steps[i].SetActive(i == number - 1);
        if (number == 3)
        {
            // Reveal the selection hint only after the full problem and pause.
            steps[2].SetActive(false);
            StartCoroutine(ProblemThenHint());
        }
        else
        {
            PlayStepVoice();
            if (number == 4) Blink(yellowBox.Image);
        }
    }
    private IEnumerator ProblemThenHint()
    {
        yield return Play(cup.Voice);
        foreach (var entry in data.Boxes)
            if (entry.ObjectId == "yellow_box") { yield return Play(entry.Voice); break; }
        yield return Play(data.EndVoice);
        yield return new WaitForSecondsRealtime(promptDelay);
        steps[2].SetActive(true);
        PlayStepVoice();
        Blink(cupView);
        canSelect = true;
    }
    private IEnumerator Play(AudioClip clip)
    {
        if (clip == null) yield break;
        instructionVoice.clip = clip;
        instructionVoice.Play();
        while (instructionVoice.isPlaying) yield return null;
    }
    private void PlayStepVoice()
    {
        var audio = steps[step - 1].GetComponent<AudioSource>();
        if (audio == null || audio.clip == null)
        { Debug.LogWarning("Stage3 Tutorial " + step + ": assign its AudioSource voice.", this); return; }
        SpeechSpeedSource.Bind(audio);
        audio.Play();
    }
    private void Blink(Graphic graphic)
    {
        blinking = graphic.GetComponent<UIBlink>();
        if (blinking == null) blinking = graphic.gameObject.AddComponent<UIBlink>();
        blinking.StartBlinking();
    }
    private void StopBlink()
    {
        if (blinking != null) blinking.StopBlinking();
        blinking = null;
    }
    public void SelectItem(int slot, Vector2 point)
    {
        if (step != 3 || !canSelect || slot != 0 || Blocked) return;
        StopBlink();
        var go = new GameObject("Stage3TutorialCup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        carried = go.GetComponent<Image>();
        carried.sprite = cup.Sprite;
        carried.preserveAspect = true;
        carried.raycastTarget = false;
        var rect = carried.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        var corners = new Vector3[4];
        cupView.rectTransform.GetWorldCorners(corners);
        rect.sizeDelta = (Vector2)(canvas.transform.InverseTransformPoint(corners[2]) - canvas.transform.InverseTransformPoint(corners[0]));
        MoveCursor(point);
        Cursor.visible = false;
        Show(4);
    }
    private void Update()
    {
        if (step != 4 || carried == null || Blocked) return;
        if (Input.touchCount > 0) MoveCursor(Input.GetTouch(0).position);
        else if (Input.mousePresent) MoveCursor(Input.mousePosition);
    }
    private void MoveCursor(Vector2 point)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, point, UiCamera, out var local);
        carried.rectTransform.anchoredPosition = local;
    }
    public void PlaceCup(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left || step != 4 || carried == null || Blocked) return;
        StopBlink();
        var rect = carried.rectTransform;
        rect.SetParent(yellowBox.Rect, true);
        rect.localScale *= 0.4f;
        var bounds = yellowBox.Rect.rect;
        rect.position = yellowBox.Rect.TransformPoint(new Vector3(bounds.center.x, bounds.yMin + bounds.height * 0.65f, 0f));
        carried = null;
        Cursor.visible = true;
        Show(5);
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        StopBlink();
        foreach (var audio in GetComponentsInChildren<AudioSource>(true)) audio.Stop();
        if (carried != null) Destroy(carried.gameObject);
        carried = null;
        Cursor.visible = true;
    }
}

public class Stage3TutorialBoxInput : MonoBehaviour, IPointerDownHandler
{
    public Stage3Tutorial Tutorial;
    public void OnPointerDown(PointerEventData e)
    {
        if (Tutorial != null && Tutorial.isActiveAndEnabled) Tutorial.PlaceCup(e);
    }
}
