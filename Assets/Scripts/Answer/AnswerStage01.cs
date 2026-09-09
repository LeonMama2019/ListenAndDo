using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

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
    [SerializeField] private CircleConfirmEffect judge1Effect;
    [SerializeField] private CircleConfirmEffect judge2Effect;
    [SerializeField] private Stage01Manager stage01Manager;

    [Header("チュートリアル開始までの時間")]
    [SerializeField] private float waitTime = 6f;

    [Header("正解IMG表示後、次の問題までの時間")]
    [SerializeField] private float nextQuestionDelay = 2f;

    [Header("Stage01 終了")]
    [SerializeField] private int totalQuestions = 5;
    [SerializeField] private GameObject TutorialFinishPanel;
    [SerializeField] private float finishPanelDuration = 5f;

    private int completedQuestions = 0;
    private float handTutorialTime = 0f;
    private TaskData currentTask;
    private float speakerTutorialTime = 0f;
    private bool handTutorialShown = false;
    private bool speakerTutorialShown = false;
    private bool handTutorialStartedByAnswer = false;
    private bool isAnswerProcessing = false;
    private bool finishPanelActive = false;
    private float questionStartTime;
    private int attemptNumber = 0;
    public int SpeakerClickCount;
    private GameObject previousMouseOverObject = null;
    public GameObject HandPanel;

    private void Start()
    {
        if (TutorialFinishPanel != null) TutorialFinishPanel.SetActive(false);
    }

    private void Update()
    {
        if (finishPanelActive)
        {
            if (Input.GetMouseButtonDown(0)) LoadTopScene();
            return;
        }

        if (handListSelector == null || isAnswerProcessing) return;
        bool handSelected = handListSelector.IsHandSelected();
        bool overObject1 = IsMouseOverObject(object1);
        bool overObject2 = IsMouseOverObject(object2);
        bool isMouseOverObject = overObject1 || overObject2;
        GameObject currentMouseOverObject = null;
        if (overObject1) currentMouseOverObject = object1;
        else if (overObject2) currentMouseOverObject = object2;
        if (currentMouseOverObject != null && currentMouseOverObject != previousMouseOverObject) Judge(currentMouseOverObject);
        previousMouseOverObject = currentMouseOverObject;
        if (handTutorialStartedByAnswer && handSelected) OnHandSelected();
        CheckHandTutorial(handSelected, isMouseOverObject);
        CheckSpeakerTutorial(handSelected, isMouseOverObject);
    }

    private void CheckHandTutorial(bool handSelected, bool isMouseOverObject)
    {
        if (!handSelected && isMouseOverObject)
        {
            handTutorialTime += Time.deltaTime;
            if (handTutorialTime >= waitTime && !handTutorialShown)
            {
                handTutorialShown = true;
                handTutorialStartedByAnswer = true;
                if (tutorialStage01 != null) tutorialStage01.StartTutorial();
            }
        }
        else if (!IsMouseOverHandPanel()) { handTutorialTime = 0f; return; }
        else { handTutorialTime = 0f; if (!handTutorialStartedByAnswer) handTutorialShown = false; }
    }

    private void CheckSpeakerTutorial(bool handSelected, bool isMouseOverObject)
    {
        if (handSelected && !isMouseOverObject)
        {
            speakerTutorialTime += Time.deltaTime;
            if (speakerTutorialTime >= waitTime && !speakerTutorialShown && !IsMouseOverHandPanel())
            {
                speakerTutorialShown = true;
                if (tutorialStage01 != null) tutorialStage01.SpeakerTutorial();
            }
            else if (IsMouseOverHandPanel()) speakerTutorialTime = 0f;
        }
        else { speakerTutorialTime = 0f; speakerTutorialShown = false; }
    }

    public void OnHandSelected()
    {
        if (!handTutorialStartedByAnswer) return;
        if (tutorialStage01 != null) tutorialStage01.EndTutorial();
        handTutorialStartedByAnswer = false;
        handTutorialTime = 0f;
        handTutorialShown = false;
    }

    private bool IsMouseOverObject(GameObject obj)
    {
        if (obj == null || Camera.main == null) return false;
        Collider2D col = obj.GetComponent<Collider2D>();
        if (col == null) return false;
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return col.OverlapPoint(mousePosition);
    }

    private void Judge(GameObject target)
    {
        if (currentTask == null || isAnswerProcessing || target == null) return;
        string selectedHand = handListSelector != null ? handListSelector.GetCurrentHandAction() : string.Empty;
        if (string.IsNullOrEmpty(selectedHand)) return;
        bool isCorrectHand = IsCorrectHand(currentTask);
        bool isCorrectObject = IsCorrectObject(target);
        bool isCorrect = isCorrectHand && isCorrectObject;
        RecordAnswer(target, isCorrect);
        if (!isCorrectHand || !isCorrectObject) return;
        if (target == object1) CorrectAnswer(judge1, judge1Effect);
        else if (target == object2) CorrectAnswer(judge2, judge2Effect);
    }

    private bool IsCorrectObject(GameObject target)
    {
        if (currentTask == null || currentTask.answerImage == null) return false;
        if (target == object1 && object1Renderer != null) return object1Renderer.sprite == currentTask.answerImage;
        if (target == object2 && object2Renderer != null) return object2Renderer.sprite == currentTask.answerImage;
        return false;
    }

    private void RecordAnswer(GameObject target, bool isCorrect)
    {
        float objectSelectedAt = Time.realtimeSinceStartup;
        attemptNumber++;
        AnswerLogEntry entry = new AnswerLogEntry
        {
            questionId = currentTask.name, attemptNumber = attemptNumber,
            correctObject = GetSpriteName(currentTask.answerImage), selectedObject = GetSelectedObjectName(target),
            correctHand = currentTask.verb != null ? currentTask.verb.name.Replace("Verb_", "") : string.Empty,
            selectedHand = handListSelector != null ? handListSelector.GetCurrentHandAction() : string.Empty,
            isCorrect = isCorrect, objectSelectionTime = objectSelectedAt - questionStartTime,
            answerTime = Time.realtimeSinceStartup - questionStartTime, answeredAt = DateTime.Now.ToString("o")
        };
        AnswerLogManager.AddAnswer(entry);
    }

    private string GetSelectedObjectName(GameObject target)
    {
        if (target == object1 && object1Renderer != null) return GetSpriteName(object1Renderer.sprite);
        if (target == object2 && object2Renderer != null) return GetSpriteName(object2Renderer.sprite);
        return target != null ? target.name : string.Empty;
    }

    private string GetSpriteName(Sprite sprite) => sprite != null ? sprite.name : string.Empty;

    private void CorrectAnswer(GameObject judge, CircleConfirmEffect effect)
    {
        if (isAnswerProcessing) return;
        isAnswerProcessing = true;
        completedQuestions++;
        judge1.SetActive(judge == judge1);
        judge2.SetActive(judge == judge2);
        if (effect != null) effect.ShowCircleAndConfirm(OnCorrectImageShown);
        else OnCorrectImageShown();
    }

    private void OnCorrectImageShown() { StartCoroutine(NextQuestionCoroutine()); }

    private IEnumerator NextQuestionCoroutine()
    {
        yield return new WaitForSeconds(nextQuestionDelay);
        if (judge1Effect != null) judge1Effect.ResetEffect();
        if (judge2Effect != null) judge2Effect.ResetEffect();
        if (judge1 != null) judge1.SetActive(false);
        if (judge2 != null) judge2.SetActive(false);
        previousMouseOverObject = null;
        handTutorialTime = 0f;
        speakerTutorialTime = 0f;
        speakerTutorialShown = false;

        if (completedQuestions >= totalQuestions)
        {
            if (TutorialFinishPanel != null)
            {
                TutorialFinishPanel.SetActive(true);
                finishPanelActive = true;
                StartCoroutine(ReturnToTopAfterDelay());
            }
            else
            {
                Debug.LogWarning("AnswerStage01: TutorialFinishPanelが設定されていません");
                LoadTopScene();
            }
            yield break;
        }

        isAnswerProcessing = false;
        if (stage01Manager != null) stage01Manager.ShowNextQuestion();
        else Debug.LogWarning("AnswerStage01: Stage01Managerが設定されていません");
    }

    private IEnumerator ReturnToTopAfterDelay()
    {
        yield return new WaitForSeconds(finishPanelDuration);
        LoadTopScene();
    }

    public void OnFinishPanelClicked() { LoadTopScene(); }

    private void LoadTopScene()
    {
        if (!finishPanelActive && completedQuestions < totalQuestions) return;
        finishPanelActive = false;
        StopAllCoroutines();
        SceneManager.LoadScene("Top");
    }

    public void OnObjectClicked()
    {
        if (currentTask == null || isAnswerProcessing) return;
        GameObject target = null;
        if (IsMouseOverObject(object1)) target = object1;
        else if (IsMouseOverObject(object2)) target = object2;
        if (target == null || target == previousMouseOverObject) return;
        Judge(target);
    }

    private bool IsCorrectHand(TaskData task)
    {
        if (task == null || task.verb == null || handListSelector == null) return false;
        return handListSelector.GetCurrentHandAction() == task.verb.name.Replace("Verb_", "");
    }

    public void SetTask(TaskData task)
    {
        currentTask = task;
        questionStartTime = Time.realtimeSinceStartup;
        attemptNumber = 0;
    }

    private bool IsMouseOverHandPanel()
    {
        if (HandPanel == null) return false;
        RectTransform rectTransform = HandPanel.GetComponent<RectTransform>();
        if (rectTransform == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, null);
    }
}