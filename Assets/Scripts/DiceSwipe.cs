using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DiceSwipe : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Sprite[] faces = new Sprite[6];
    [SerializeField, Min(1f)] private float swipeThreshold = 30f;
    private Image image;
    private Vector2 start;
    private int? pointer;
    private int top = 1, bottom = 6, left = 4, right = 3, front = 2, back = 5;
    public int CurrentValue => top;
    public bool InputEnabled { get; set; } = true;
    public event System.Action<DiceSwipe, string> Rolled;
    // Screen up/down correspond to the back/front edges of the die.
    public int ValueAt(string direction) => direction == "Left" ? left :
        direction == "Right" ? right : direction == "Up" ? back : front;
    public string Orientation => $"{top},{bottom},{left},{right},{front},{back}";
    public void Shuffle()
    {
        for (int i = 0; i < 20; i++)
            switch (Random.Range(0, 4))
            { case 0: RollLeft(); break; case 1: RollRight(); break;
              case 2: RollUp(); break; default: RollDown(); break; }
        Refresh();
    }

    // Stage2-only setup: use physical rolls so visible and hidden faces stay consistent.
    public void SetVisibleValue(int value)
    {
        if (value < 1 || value > 6) throw new System.ArgumentOutOfRangeException(nameof(value));
        if (value == bottom) { RollRight(); RollRight(); }
        else if (value == left) RollRight();
        else if (value == right) RollLeft();
        else if (value == front) RollUp();
        else if (value == back) RollDown();
        Refresh();
    }

    private void Awake()
    {
        image = GetComponent<Image>();
        for (int i = 0; i < faces.Length; i++)
            if (faces[i] != null && faces[i] == image.sprite)
            {
                // Rotate the initial die to match its selected face.
                int target = i + 1;
                if (target == bottom) { RollRight(); RollRight(); }
                else if (target == left) RollRight();
                else if (target == right) RollLeft();
                else if (target == front) RollUp();
                else if (target == back) RollDown();
                break;
            }
        Refresh();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!InputEnabled || pointer.HasValue || e.button != PointerEventData.InputButton.Left) return;
        pointer = e.pointerId;
        start = e.position;
    }
    public void OnPointerUp(PointerEventData e) => Finish(e);
    public void OnBeginDrag(PointerEventData e) { }
    public void OnDrag(PointerEventData e) { }
    public void OnEndDrag(PointerEventData e) => Finish(e);

    private void Finish(PointerEventData e)
    {
        if (pointer != e.pointerId) return;
        pointer = null;
        if (!InputEnabled) return;
        Vector2 delta = e.position - start;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) delta /= Mathf.Max(0.01f, canvas.rootCanvas.scaleFactor);
        if (Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) < swipeThreshold) return;
        string direction;
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            if (delta.x > 0) { RollRight(); direction = "Right"; } else { RollLeft(); direction = "Left"; }
        }
        else
        {
            if (delta.y > 0) { RollUp(); direction = "Up"; } else { RollDown(); direction = "Down"; }
        }
        Refresh();
        Rolled?.Invoke(this, direction);
    }
    private void RollRight() { int t = top; top = left; left = bottom; bottom = right; right = t; }
    private void RollLeft() { int t = top; top = right; right = bottom; bottom = left; left = t; }
    private void RollUp() { int t = top; top = front; front = bottom; bottom = back; back = t; }
    private void RollDown() { int t = top; top = back; back = bottom; bottom = front; front = t; }
    private void Refresh()
    {
        if (top <= faces.Length && faces[top - 1] != null) image.sprite = faces[top - 1];
    }
    private void OnDisable() => pointer = null;
}
