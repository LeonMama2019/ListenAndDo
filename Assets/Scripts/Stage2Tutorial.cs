using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Stage2Tutorial : MonoBehaviour
{
    public const string CompletionKey = "Stage2.TutorialCompleted";
    [SerializeField] private GameObject[] steps = new GameObject[5];
    [SerializeField] private RectTransform swipe;
    [SerializeField] private RectTransform die;
    [SerializeField] private float swipeDistance = 160f;
    [SerializeField] private float motionSeconds = 1f;
    private Coroutine motion;
    private GameObject inputSurface;
    private Action completed;
    private int step;

    public void Begin(Action onCompleted)
    {
        completed = onCompleted;
        step = 0;
        foreach (var item in steps)
            if (item == null) { Debug.LogError("Assign Stage2 Tutorial steps 1-5.", this); return; }
        // Step AudioSources are played explicitly, once per transition.
        foreach (var childAudio in GetComponentsInChildren<AudioSource>(true))
        {
            childAudio.playOnAwake = false;
            childAudio.Stop();
        }
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        // Capture gestures across the whole screen, independently of the tutorial panel size.
        inputSurface = new GameObject("Stage2TutorialInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        inputSurface.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)inputSurface.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        inputSurface.GetComponent<Image>().color = Color.clear;
        inputSurface.AddComponent<Stage2TutorialInput>().Tutorial = this;
        Show(1);
    }
    private void Show(int number)
    {
        if (step == number) return;
        step = number;
        // Each explanation starts with 1 visible; gesture-driven rolls stay unchanged.
        if (die != null)
        {
            var tutorialDie = die.GetComponent<DiceSwipe>();
            if (tutorialDie != null) tutorialDie.SetVisibleValue(1);
        }
        foreach (var childAudio in GetComponentsInChildren<AudioSource>(true)) childAudio.Stop();
        if (motion != null) { StopCoroutine(motion); motion = null; }
        for (int i = 0; i < steps.Length; i++) steps[i].SetActive(i == step - 1);
        var stepAudio = steps[step - 1].GetComponent<AudioSource>();
        if (stepAudio != null && stepAudio.clip != null)
        {
            stepAudio.playOnAwake = false;
            stepAudio.Play();
        }
        else Debug.LogWarning("Stage2 Tutorial " + step + ": attach its voice to the step AudioSource.", this);
        if (swipe != null)
        {
            swipe.gameObject.SetActive(step == 2 || step == 3);
            if (step == 2 || step == 3) motion = StartCoroutine(AnimateSwipe());
        }
    }
    private IEnumerator AnimateSwipe()
    {
        Canvas.ForceUpdateCanvases();
        var parent = swipe.parent as RectTransform;
        Vector3 center = die != null ? die.TransformPoint(die.rect.center) : transform.position;
        Vector3 origin = parent.InverseTransformPoint(center);
        float half = Mathf.Max(1f, swipeDistance) * 0.5f;
        if (die != null)
        {
            Vector3[] corners = new Vector3[4]; die.GetWorldCorners(corners);
            Vector3 low = parent.InverseTransformPoint(corners[0]);
            Vector3 high = parent.InverseTransformPoint(corners[2]);
            half = (step == 2 ? Mathf.Abs(high.y - low.y) : Mathf.Abs(high.x - low.x)) * 0.5f;
        }
        Vector3 axis = step == 2 ? Vector3.up : Vector3.right;
        while (true)
        {
            float elapsed = 0;
            swipe.localPosition = origin - axis * half;
            yield return new WaitForSecondsRealtime(0.3f);
            while (elapsed < Mathf.Max(0.1f, motionSeconds))
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / Mathf.Max(0.1f, motionSeconds));
                swipe.localPosition = Vector3.Lerp(origin - axis * half, origin + axis * half, t);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }
    public void Gesture(Vector2 delta)
    {
        bool isSwipe = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) >= 30f;
        if (step >= 1 && step <= 3 && isSwipe && die != null)
        {
            var tutorialDie = die.GetComponent<DiceSwipe>();
            if (tutorialDie != null) tutorialDie.RollTutorialGesture(delta);
        }
        if (step == 1 && isSwipe) Show(2);
        else if (step == 2 && isSwipe && delta.y > Mathf.Abs(delta.x)) Show(3);
        else if (step == 3 && isSwipe && delta.x > Mathf.Abs(delta.y)) Show(4);
        else if (step == 4 && !isSwipe) Show(5);
        else if (step == 5 && !isSwipe)
        {
            PlayerPrefs.SetInt(CompletionKey, 1); PlayerPrefs.Save();
            var callback = completed; completed = null;
            gameObject.SetActive(false);
            callback?.Invoke();
        }
    }
    private void OnDisable()
    {
        if (motion != null) StopCoroutine(motion);
        motion = null;
        foreach (var childAudio in GetComponentsInChildren<AudioSource>(true)) childAudio.Stop();
        if (swipe != null) swipe.gameObject.SetActive(false);
        if (inputSurface != null) { inputSurface.SetActive(false); Destroy(inputSurface); }
    }
}

// Runtime-only transparent input surface; no existing dice input handlers are changed.
public class Stage2TutorialInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Stage2Tutorial Tutorial;
    private int? pointer;
    private Vector2 start;
    public void OnPointerDown(PointerEventData e)
    {
        if (pointer.HasValue || e.button != PointerEventData.InputButton.Left) return;
        pointer = e.pointerId; start = e.position;
    }
    public void OnPointerUp(PointerEventData e) => Finish(e);
    public void OnBeginDrag(PointerEventData e) { }
    public void OnDrag(PointerEventData e) { }
    public void OnEndDrag(PointerEventData e) => Finish(e);
    private void Finish(PointerEventData e)
    {
        if (pointer != e.pointerId) return;
        pointer = null;
        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        Tutorial.Gesture((e.position - start) / Mathf.Max(0.01f, canvas.scaleFactor));
    }
    private void OnDisable() => pointer = null;
}
