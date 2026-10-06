using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SpeechSpeedSlider : MonoBehaviour, IPointerDownHandler, IDragHandler,
    IPointerUpHandler, IInitializePotentialDragHandler
{
    [SerializeField] private RectTransform knob;
    [Tooltip("背景画像内の青いレールの左端・右端（0〜1）。")]
    [SerializeField] private Vector2 railRange = new Vector2(0.21f, 0.79f);
    [SerializeField] private TMP_Text valueLabel;
    private RectTransform rect;
    private float knobY;
    private int? draggingPointer;

    private void Awake()
    {
        rect = (RectTransform)transform;
        if (knob == null) knob = transform.Find("Nob") as RectTransform;
        if (knob == null) { Debug.LogError("SpeechSpeedSlider: assign the Nob RectTransform.", this); enabled = false; return; }
        knobY = knob.anchoredPosition.y;
        if (valueLabel == null)
        {
            var go = new GameObject("SpeechSpeedValue", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            valueLabel = go.GetComponent<TextMeshProUGUI>();
            valueLabel.rectTransform.anchorMin = valueLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            valueLabel.rectTransform.anchoredPosition = new Vector2(0, -rect.rect.height * 0.28f);
            valueLabel.rectTransform.sizeDelta = new Vector2(90, 24);
            valueLabel.fontSize = 16;
            valueLabel.alignment = TextAlignmentOptions.Center;
            valueLabel.color = new Color(0.02f, 0.12f, 0.35f);
            valueLabel.raycastTarget = false;
        }
        Refresh(SpeechPlaybackSpeed.Value);
    }
    private void OnEnable() { SpeechPlaybackSpeed.Changed += Refresh; }
    private void Start() { Refresh(SpeechPlaybackSpeed.Value); }
    private void OnDisable() { SpeechPlaybackSpeed.Changed -= Refresh; draggingPointer = null; }
    private void OnDestroy() { SpeechPlaybackSpeed.CancelQuestion(); }
    private void Refresh(float multiplier)
    {
        if (knob == null || rect == null) return;
        float normalized = Mathf.InverseLerp(SpeechPlaybackSpeed.Minimum, SpeechPlaybackSpeed.Maximum, multiplier);
        float x = rect.rect.xMin + rect.rect.width * Mathf.Lerp(railRange.x, railRange.y, normalized);
        knob.anchorMin = knob.anchorMax = new Vector2(0.5f, 0.5f);
        knob.anchoredPosition = new Vector2(x - rect.rect.center.x, knobY);
        if (valueLabel != null) valueLabel.text = multiplier.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "×";
    }
    private void SetFromPointer(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position,
            eventData.pressEventCamera, out var point)) return;
        float left = rect.rect.xMin + rect.rect.width * railRange.x;
        float right = rect.rect.xMin + rect.rect.width * railRange.y;
        float normalized = Mathf.InverseLerp(left, right, point.x);
        SpeechPlaybackSpeed.SetValue(Mathf.Lerp(SpeechPlaybackSpeed.Minimum, SpeechPlaybackSpeed.Maximum, normalized));
    }
    public void OnInitializePotentialDrag(PointerEventData eventData) { eventData.useDragThreshold = false; }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || draggingPointer.HasValue) return;
        draggingPointer = eventData.pointerId; SetFromPointer(eventData);
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (draggingPointer == eventData.pointerId) SetFromPointer(eventData);
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (draggingPointer != eventData.pointerId) return;
        SetFromPointer(eventData); draggingPointer = null;
    }
}
