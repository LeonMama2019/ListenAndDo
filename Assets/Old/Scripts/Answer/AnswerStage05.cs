using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AnswerStage05 : MonoBehaviour
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
    [SerializeField] private Stage05Manager stage05Manager;

    [Header("正解IMG表示後、次の問題までの時間")]
    [SerializeField] private float nextQuestionDelay = 2f;

    [Header("Stage05 終了")]
    [SerializeField] private int totalQuestions = 7;
    [SerializeField] private GameObject FinishPanel;
    [SerializeField] private float finishPanelDuration = 5f;

    private TaskData currentTask;
    private bool isAnswerProcessing = false;
    private GameObject previousMouseOverObject = null;
    private float questionStartTime;
    private int attemptNumber = 0;
    private int completedQuestions = 0;
    private bool finishPanelActive = false;

    private void Start()
    {
        if (FinishPanel != null) FinishPanel.SetActive(false);
    }

    private void Update()
    {
        if (finishPanelActive)
        {
            if (Input.GetMouseButtonDown(0)) LoadTopScene();
            return;
        }
        if (handListSelector == null || isAnswerProcessing) return;
        GameObject currentMouseOverObject = null;
        if (IsMouseOverObject(object1)) currentMouseOverObject = object1;
        else if (IsMouseOverObject(object2)) currentMouseOverObject = object2;
        if (currentMouseOverObject != null && currentMouseOverObject != previousMouseOverObject)
        {
            Debug.Log($"[Stage05 DEBUG] Collider entered: {currentMouseOverObject.name}");
            Judge(currentMouseOverObject);
        }
        previousMouseOverObject = currentMouseOverObject;
    }

    private bool IsMouseOverObject(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("[Stage05 DEBUG] Object reference is null");
            return false;
        }
        if (Camera.main == null)
        {
            Debug.LogWarning("[Stage05 DEBUG] Camera.main is null");
            return false;
        }
        Collider2D col = obj.GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogWarning($"[Stage05 DEBUG] Collider2D missing: {obj.name}");
            return false;
        }
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return col.OverlapPoint(mousePosition);
    }

    private void Judge(GameObject target)
    {
        if (currentTask == null)
        {
            Debug.LogWarning("[Stage05 DEBUG] currentTask is null");
            return;
        }
        if (target == null || isAnswerProcessing) return;

        string selectedHand = handListSelector != null ? handListSelector.GetCurrentHandAction() : string.Empty;
        string correctHand = currentTask.verb != null ? currentTask.verb.name.Replace("Verb_", "") : "<null>";
        bool handMatch = IsCorrectHand(currentTask);
        bool objectMatch = IsCorrectObject(target);

        Debug.Log(
            $"[Stage05 DEBUG] Target={target.name} | " +
            $"SelectedHand='{selectedHand}' | CorrectHand='{correctHand}' | HandMatch={handMatch} | " +
            $"SelectedSprite='{GetSelectedObjectName(target)}' | CorrectSprite='{GetSpriteName(currentTask.answerImage)}' | ObjectMatch={objectMatch}"
        );

        if (string.IsNullOrEmpty(selectedHand))
        {
            Debug.LogWarning("[Stage05 DEBUG] No hand selected");
            return;
        }

        bool isCorrect = handMatch && objectMatch;
        RecordAnswer(target, isCorrect);
        if (!isCorrect) return;

        Debug.Log($"[Stage05 DEBUG] CORRECT -> showing judge for {target.name}");
        if (target == object1) CorrectAnswer(judge1, judge1Effect);
        else if (target == object2) CorrectAnswer(judge2, judge2Effect);
    }

    private bool IsCorrectHand(TaskData task)
    {
        if (task == null || task.verb == null || handListSelector == null) return false;
        return string.Equals(
            handListSelector.GetCurrentHandAction(),
            task.verb.name.Replace("Verb_", ""),
            StringComparison.OrdinalIgnoreCase
        );
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
        if (isAnswerProcessing) return;
        isAnswerProcessing = true;
        completedQuestions++;
        Debug.Log($"[Stage05 DEBUG] CorrectAnswer called | judge={(judge != null ? judge.name : "null")} | effect={(effect != null ? effect.name : "null")}");
        if (judge1 != null) judge1.SetActive(judge == judge1);
        if (judge2 != null) judge2.SetActive(judge == judge2);
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

        if (completedQuestions >= totalQuestions)
        {
            if (FinishPanel != null)
            {
                FinishPanel.SetActive(true);
                finishPanelActive = true;
                StartCoroutine(ReturnToTopAfterDelay());
            }
            else
            {
                Debug.LogWarning("AnswerStage05: FinishPanelが設定されていません");
                LoadTopScene();
            }
            yield break;
        }

        isAnswerProcessing = false;
        if (stage05Manager != null) stage05Manager.ShowNextQuestion();
        else Debug.LogWarning("AnswerStage05: Stage05Managerが設定されていません");
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
        LevelResultStore.CompleteCurrentSession();
        SceneManager.LoadScene("Top");
    }

    public void SetTask(TaskData task)
    {
        currentTask = task;
        questionStartTime = Time.realtimeSinceStartup;
        attemptNumber = 0;
        Debug.Log($"[Stage05 DEBUG] SetTask: {(task != null ? task.name : "null")} | Verb={(task != null && task.verb != null ? task.verb.name : "null")} | AnswerImage={(task != null && task.answerImage != null ? task.answerImage.name : "null")}");
    }
}
