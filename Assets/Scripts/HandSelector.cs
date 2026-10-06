using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HandSelector : MonoBehaviour, IPointerClickHandler
{
    public enum HandSide { Left, Right }

    [Header("このImageが表す手")]
    [SerializeField] private HandSide handSide;

    [Header("選択時に鳴らす音")]
    [SerializeField] private AudioClip voiceClip;
    [SerializeField] private Stage1Tutorial tutorialOnSelect;
    [SerializeField] private Stage1Level1Quiz level1Quiz;

    private AudioSource audioSource;
    private Image handImage;

    private static GameObject cursorObject;
    private static Image cursorImage;
    private static RectTransform cursorRect;
    private static Canvas rootCanvas;

    private void Awake()
    {
        handImage = GetComponent<Image>();
        rootCanvas = GetComponentInParent<Canvas>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        SpeechSpeedSource.Bind(audioSource);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SelectHand();
    }

    public void SelectHand()
    {
        // チュートリアル2の右手では、手の名前と次の説明が重ならないようにする。
        bool advancedTutorial = tutorialOnSelect != null && tutorialOnSelect.OnRightHandSelected();
        bool inQuiz = level1Quiz != null && level1Quiz.IsRunning;
        if (inQuiz && !level1Quiz.SelectHand(handSide)) return;
        if (!advancedTutorial && !inQuiz && voiceClip != null)
            audioSource.PlayOneShot(voiceClip);

        if (handImage == null || handImage.sprite == null)
            return;

        CreateCursorIfNeeded();

        cursorImage.sprite = handImage.sprite;
        cursorImage.preserveAspect = true;
        cursorObject.SetActive(true);

        Cursor.visible = false;
        UpdateCursorPosition();
    }

    private void Update()
    {
        if (cursorObject != null && cursorObject.activeSelf)
            UpdateCursorPosition();
    }

    private void CreateCursorIfNeeded()
    {
        if (cursorObject != null)
            return;

        cursorObject = new GameObject("SelectedHandCursor",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        cursorObject.transform.SetParent(rootCanvas.transform, false);
        cursorObject.transform.SetAsLastSibling();

        cursorRect = cursorObject.GetComponent<RectTransform>();
        cursorRect.sizeDelta = new Vector2(100f, 100f);
        cursorRect.pivot = new Vector2(0.5f, 0.5f);

        cursorImage = cursorObject.GetComponent<Image>();
        cursorImage.raycastTarget = false;
        cursorImage.preserveAspect = true;
    }

    private void UpdateCursorPosition()
    {
        if (rootCanvas == null || cursorRect == null)
            return;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, Input.mousePosition, cam, out Vector2 localPoint))
        {
            cursorRect.anchoredPosition = localPoint;
        }
    }

    public static void ResetCursor()
    {
        if (cursorObject != null) cursorObject.SetActive(false);
        Cursor.visible = true;
    }

    private void OnDisable()
    {
        // シーン終了時などにOSカーソルが消えたままにならないようにする
        if (!gameObject.scene.isLoaded)
            Cursor.visible = true;
    }
}
