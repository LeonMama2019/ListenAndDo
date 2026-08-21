using UnityEngine;

public class AnswerStage01 : MonoBehaviour
{
    [Header("判定したいオブジェクト")]
    [SerializeField] private GameObject object1;
    [SerializeField] private GameObject object2;
    [SerializeField] private SpriteRenderer object1Renderer;
    [SerializeField] private SpriteRenderer object2Renderer;
    [SerializeField] private GameObject judge1;
    [SerializeField] private GameObject judge2;

    [Header("参照")]
    [SerializeField] private HandListSelector handListSelector;
    [SerializeField] private TutorialStage01 tutorialStage01;
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private CircleConfirmEffect judge1Effect;
    [SerializeField] private CircleConfirmEffect judge2Effect;

    [Header("ヒントを出すまでの時間")]
    [SerializeField] private float handHintWaitTime = 6f;
    [SerializeField] private float speakerHintWaitTime = 6f;

    [Header("Hand選択エリア")]
    [SerializeField] private GameObject HandPanel;

    private TaskData currentTask;
    private GameObject previousPointedObject;

    private float handHintTimer;
    private float speakerHintTimer;
    private bool handHintShown;
    private bool speakerHintShown;
    private bool previousHandSelected;

    private void Update()
    {
        if (handListSelector == null)
            return;

        bool handSelected = handListSelector.IsHandSelected();
        bool overObject1 = IsPointerOverObject(object1);
        bool overObject2 = IsPointerOverObject(object2);
        bool pointerOverAnswerObject = overObject1 || overObject2;
        bool pointerOverHandPanel = IsPointerOverHandPanel();

        GameObject pointedObject = null;
        if (overObject1)
            pointedObject = object1;
        else if (overObject2)
            pointedObject = object2;

        // Hand が正しく選択されている時だけ回答判定する。
        if (pointedObject != null && pointedObject != previousPointedObject)
        {
            Judge(pointedObject);
        }
        previousPointedObject = pointedObject;

        // Hand を選んだ瞬間は Hand ヒントを終了し、
        // Speaker 用の「放置時間」をゼロから数え始める。
        if (handSelected && !previousHandSelected)
        {
            handHintTimer = 0f;
            handHintShown = false;
            speakerHintTimer = 0f;
            speakerHintShown = false;
            tutorialStage01?.StopHandHint();
        }

        // Hand を選び直した／解除した場合も Speaker タイマーをリセットする。
        if (!handSelected && previousHandSelected)
        {
            speakerHintTimer = 0f;
            speakerHintShown = false;
            tutorialStage01?.StopSpeakerHintAfterCurrentLoop();
        }

        if (ShouldRunTutorialHints())
        {
            CheckHandHint(handSelected, pointerOverAnswerObject);
            CheckSpeakerHint(handSelected, pointerOverAnswerObject, pointerOverHandPanel);
        }
        else
        {
            ResetHintTimers();
        }

        previousHandSelected = handSelected;
    }

    /// <summary>
    /// 初回プレイ中だけ補助ヒントを出す。
    /// TutorialManager が未設定の場合は、既存Sceneとの互換性のため有効扱いにする。
    /// </summary>
    private bool ShouldRunTutorialHints()
    {
        return tutorialManager == null || tutorialManager.IsStage01TutorialActive();
    }

    /// <summary>
    /// Hand未選択のまま回答オブジェクトを指し続けたら
    /// 「先にHandを選んでね」のヒントを出す。
    /// </summary>
    private void CheckHandHint(bool handSelected, bool pointerOverAnswerObject)
    {
        if (handSelected)
        {
            handHintTimer = 0f;
            return;
        }

        if (!pointerOverAnswerObject)
        {
            handHintTimer = 0f;
            return;
        }

        if (handHintShown)
            return;

        handHintTimer += Time.deltaTime;

        if (handHintTimer >= handHintWaitTime)
        {
            handHintShown = true;
            tutorialStage01?.ShowHandHint();
        }
    }

    /// <summary>
    /// Hand選択後、回答もHandの選び直しもせず放置されたら
    /// 「問題をもう一回聞く？」のSpeakerヒントを出す。
    /// </summary>
    private void CheckSpeakerHint(
        bool handSelected,
        bool pointerOverAnswerObject,
        bool pointerOverHandPanel)
    {
        if (!handSelected)
        {
            speakerHintTimer = 0f;
            speakerHintShown = false;
            return;
        }

        // 回答しようとしている最中やHandを選び直している最中は放置扱いにしない。
        if (pointerOverAnswerObject || pointerOverHandPanel)
        {
            speakerHintTimer = 0f;
            return;
        }

        if (speakerHintShown)
            return;

        speakerHintTimer += Time.deltaTime;

        if (speakerHintTimer >= speakerHintWaitTime)
        {
            speakerHintShown = true;
            tutorialStage01?.ShowSpeakerHint();
        }
    }

    private void ResetHintTimers()
    {
        handHintTimer = 0f;
        speakerHintTimer = 0f;
        handHintShown = false;
        speakerHintShown = false;
    }

    /// <summary>
    /// HandListSelector から呼べる互換メソッド。
    /// </summary>
    public void OnHandSelected()
    {
        handHintTimer = 0f;
        handHintShown = false;
        speakerHintTimer = 0f;
        speakerHintShown = false;
        tutorialStage01?.StopHandHint();
    }

    private bool IsPointerOverObject(GameObject obj)
    {
        if (obj == null || Camera.main == null)
            return false;

        Collider2D col = obj.GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogWarning(obj.name + " にCollider2Dがありません");
            return false;
        }

        Vector2 screenPosition;

        // タッチ中はタッチ位置を使う。PC/WebGLではマウス位置を使う。
        if (Input.touchCount > 0)
            screenPosition = Input.GetTouch(0).position;
        else
            screenPosition = Input.mousePosition;

        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        return col.OverlapPoint(worldPosition);
    }

    private void Judge(GameObject target)
    {
        if (currentTask == null)
            return;

        if (!IsCorrectHand(currentTask))
        {
            Debug.Log("手が違います");
            return;
        }

        bool correctTarget =
            (target == object1 && object1Renderer != null && object1Renderer.sprite == currentTask.answerImage) ||
            (target == object2 && object2Renderer != null && object2Renderer.sprite == currentTask.answerImage);

        if (!correctTarget)
            return;

        if (target == object1)
        {
            if (judge1 != null) judge1.SetActive(true);
            if (judge2 != null) judge2.SetActive(false);
            judge1Effect?.ShowCircleAndConfirm();
        }
        else
        {
            if (judge1 != null) judge1.SetActive(false);
            if (judge2 != null) judge2.SetActive(true);
            judge2Effect?.ShowCircleAndConfirm();
        }

        // 正解できた時点で Stage01 の初回チュートリアルは完了。
        if (tutorialManager != null && tutorialManager.IsStage01TutorialActive())
        {
            tutorialManager.CompleteStage01Tutorial();
        }
    }

    public void OnObjectClicked()
    {
        if (currentTask == null || handListSelector == null)
            return;

        string handName = handListSelector.GetCurrentHandAction();
        if (string.IsNullOrEmpty(handName))
            return;

        GameObject target = null;
        if (IsPointerOverObject(object1)) target = object1;
        else if (IsPointerOverObject(object2)) target = object2;

        if (target != null)
            Judge(target);
    }

    private bool IsCorrectHand(TaskData task)
    {
        if (task == null || task.verb == null || handListSelector == null)
            return false;

        string selectedHandAction = handListSelector.GetCurrentHandAction();
        string correctVerbName = task.verb.name.Replace("Verb_", "");

        Debug.Log($"選択した手：{selectedHandAction} / 正解の動詞：{correctVerbName}");
        return selectedHandAction == correctVerbName;
    }

    public void SetTask(TaskData task)
    {
        currentTask = task;
        previousPointedObject = null;
        ResetHintTimers();
    }

    private bool IsPointerOverHandPanel()
    {
        if (HandPanel == null)
            return false;

        RectTransform rectTransform = HandPanel.GetComponent<RectTransform>();
        if (rectTransform == null)
            return false;

        Vector2 screenPosition;
        if (Input.touchCount > 0)
            screenPosition = Input.GetTouch(0).position;
        else
            screenPosition = Input.mousePosition;

        return RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform,
            screenPosition,
            null
        );
    }
}