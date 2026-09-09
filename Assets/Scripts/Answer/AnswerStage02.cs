using System;
using System.Collections;
using UnityEngine;

public class AnswerStage02 : MonoBehaviour
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
    [SerializeField] private CircleConfirmEffect judge1Effect;
    [SerializeField] private CircleConfirmEffect judge2Effect;
    [SerializeField] private Stage02Manager stage02Manager;

    [Header("正解IMG表示後、次の問題までの時間")]
    [SerializeField] private float nextQuestionDelay = 2f;

    private TaskData currentTask;
    private bool isAnswerProcessing = false;
    private GameObject previousMouseOverObject = null;
    private float questionStartTime;
    private int attemptNumber = 0;

    private void Update()
    {
        if (handListSelector == null || isAnswerProcessing)
            return;

        GameObject currentMouseOverObject = null;
        if (IsMouseOverObject(object1)) currentMouseOverObject = object1;
        else if (IsMouseOverObject(object2)) currentMouseOverObject = object2;

        if (currentMouseOverObject != null && currentMouseOverObject != previousMouseOverObject)
            Judge(currentMouseOverObject);

        previousMouseOverObject = currentMouseOverObject;
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
        if (currentTask == null || target == null || isAnswerProcessing) return;

        string selectedHand = handListSelector.GetCurrentHandAction();
        if (string.IsNullOrEmpty(selectedHand)) return;

        bool isCorrectHand = IsCorrectHand(currentTask);
        bool isCorrectObject = IsCorrectObject(target);
        bool isCorrect = isCorrectHand && isCorrectObject;

        RecordAnswer(target, isCorrect);

        if (!isCorrect) return;

        if (target == object1) CorrectAnswer(judge1, judge1Effect);
        else if (target == object2) CorrectAnswer(judge2, judge2Effect);
    }

    private bool IsCorrectHand(TaskData task)
    {
        if (task == null || task.verb == null || handListSelector == null) return false;
        return handListSelector.GetCurrentHandAction() == task.verb.name.Replace("Verb_", "");
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
        attemptNumber++;
        AnswerLogEntry entry = new AnswerLogEntry
        {
            questionId = currentTask.name,
            attemptNumber = attemptNumber,
            correctObject = GetSpriteName(currentTask.answerImage),
            selectedObject = GetSelectedObjectName(target),
            correctHand = currentTask.verb != null ? currentTask.verb.name.Replace("Verb_", "") : string.Empty,
            selectedHand = handListSelector != null ? handListSelector.GetCurrentHandAction() : string.Empty,
            isCorrect = isCorrect,
            objectSelectionTime = Time.realtimeSinceStartup - questionStartTime,
            answerTime = Time.realtimeSinceStartup - questionStartTime,
            answeredAt = DateTime.Now.ToString("o")
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
        isAnswerProcessing = true;

        if (judge1 != null) judge1.SetActive(judge == judge1);
        if (judge2 != null) judge2.SetActive(judge == judge2);

        if (effect != null) effect.ShowCircleAndConfirm(OnCorrectImageShown);
        else OnCorrectImageShown();
    }

    private void OnCorrectImageShown()
    {
        StartCoroutine(NextQuestionCoroutine());
    }

    private IEnumerator NextQuestionCoroutine()
    {
        yield return new WaitForSeconds(nextQuestionDelay);

        if (judge1Effect != null) judge1Effect.ResetEffect();
        if (judge2Effect != null) judge2Effect.ResetEffect();
        if (judge1 != null) judge1.SetActive(false);
        if (judge2 != null) judge2.SetActive(false);

        previousMouseOverObject = null;
        isAnswerProcessing = false;

        if (stage02Manager != null)
            stage02Manager.ShowNextQuestion();
        else
            Debug.LogWarning("AnswerStage02: Stage02Managerが設定されていません");
    }

    public void SetTask(TaskData task)
    {
        currentTask = task;
        questionStartTime = Time.realtimeSinceStartup;
        attemptNumber = 0;
    }
}