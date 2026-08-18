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
    [SerializeField] private CircleConfirmEffect judge1Effect;
    [SerializeField] private CircleConfirmEffect judge2Effect;

    [Header("チュートリアル開始までの時間")]
    [SerializeField] private float waitTime = 5f; 

    // HandList用タイマー
    private float handTutorialTime = 0f;
    private TaskData currentTask;
    // Speaker用タイマー
    private float speakerTutorialTime = 0f;

    // 同じ状態で何度も呼ばないためのフラグ
    private bool handTutorialShown = false;
    private bool speakerTutorialShown = false;

    // AnswerStage01からHandチュートリアルを開始したか
    private bool handTutorialStartedByAnswer = false;
    public int SpeakerClickCount;
    private GameObject previousMouseOverObject = null;

    private void Update()
    {
        if (handListSelector == null)
            return;

        bool handSelected = handListSelector.IsHandSelected();

        bool overObject1 = IsMouseOverObject(object1);
        bool overObject2 = IsMouseOverObject(object2);

        bool isMouseOverObject =
            overObject1 || overObject2;

        // 今マウスが乗っているオブジェクト
        GameObject currentMouseOverObject = null;

        if (overObject1)
        {
            currentMouseOverObject = object1;
        }
        else if (overObject2)
        {
            currentMouseOverObject = object2;
        }

        // 新しくオブジェクトの上に乗った瞬間だけ判定
        if (currentMouseOverObject != null &&
            currentMouseOverObject != previousMouseOverObject)
        {
            Judge(currentMouseOverObject);
        }

        previousMouseOverObject = currentMouseOverObject;

        // 手を選んだらチュートリアル終了
        if (handTutorialStartedByAnswer && handSelected)
        {
            OnHandSelected();
        }

        CheckHandTutorial(
            handSelected,
            isMouseOverObject
        );

        CheckSpeakerTutorial(
            handSelected,
            isMouseOverObject
        );
    }

    /// <summary>
    /// 手を選んでいない状態で、
    /// 回答画像にマウスを置き続けた時のチュートリアル
    /// </summary>
    private void CheckHandTutorial(
        bool handSelected,
        bool isMouseOverObject)
    {
        if (!handSelected && isMouseOverObject)
        {
            handTutorialTime += Time.deltaTime;

            if (handTutorialTime >= waitTime &&
                !handTutorialShown)
            {
                handTutorialShown = true;
                handTutorialStartedByAnswer = true;

                if (tutorialStage01 != null)
                {
                    tutorialStage01.StartTutorial();
                }
            }
        }
        else
        {
            handTutorialTime = 0f;

            /*
             * 一度開始済みの場合は、
             * ここでShownをfalseに戻さない。
             *
             * 戻すと、手を選ぶ前にマウスを少し外しただけで
             * 再びチュートリアルが開始されるため。
             */
            if (!handTutorialStartedByAnswer)
            {
                handTutorialShown = false;
            }
        }
    }

    /// <summary>
    /// 手を選択済みで、
    /// 回答画像の外にマウスがある時のSpeakerチュートリアル
    /// </summary>
    private void CheckSpeakerTutorial(
        bool handSelected,
        bool isMouseOverObject)
    {
        if (handSelected && !isMouseOverObject)
        {
            speakerTutorialTime += Time.deltaTime;

            if (speakerTutorialTime >= waitTime &&
                !speakerTutorialShown)
            {
                speakerTutorialShown = true;

                Debug.Log(
                    "Speakerチュートリアル開始"
                );

                if (tutorialStage01 != null)
                {
                    tutorialStage01.SpeakerTutorial();
                }
            }

        }
        else
        {
            speakerTutorialTime = 0f;
            speakerTutorialShown = false;
        }
    }
   

    /// <summary>
    /// HandListSelectorで手を選択した時に呼ぶ
    /// </summary>
    public void OnHandSelected()
    {
        /*
         * 通常のTutorialStage01から始まった場合は
         * 何もしない。
         */
        if (!handTutorialStartedByAnswer)
            return;

        Debug.Log(
            "AnswerStage01から開始したHandチュートリアルを終了"
        );

        if (tutorialStage01 != null)
        {
            tutorialStage01.EndTutorial();
        }

        handTutorialStartedByAnswer = false;
        handTutorialTime = 0f;
        handTutorialShown = false;
    }

    private bool IsMouseOverObject(
        GameObject obj)
    {
        if (obj == null || Camera.main == null)
            return false;

        Collider2D col =
            obj.GetComponent<Collider2D>();

        if (col == null)
        {
            Debug.LogWarning(
                obj.name +
                " にCollider2Dがありません"
            );

            return false;
        }

        Vector2 mousePosition =
            Camera.main.ScreenToWorldPoint(
                Input.mousePosition
            );
        

        return col.OverlapPoint(mousePosition);
    }
    private void Judge(GameObject target)
    {
        if (currentTask == null)
            return;

        bool isCorrect = IsCorrectHand(currentTask);

        if (!isCorrect)
        {
            Debug.Log("手が違います");
            return;
        }
        else
        {
            if (target == object1 && object1Renderer.sprite == currentTask.answerImage)
            {

                judge1.SetActive(true);
                judge2.SetActive(false);

                judge1Effect.ShowCircleAndConfirm();
            }
            else if (target == object2 && object2Renderer.sprite == currentTask.answerImage)
            {
                judge1.SetActive(false);
                judge2.SetActive(true);

                judge2Effect.ShowCircleAndConfirm();
            }
            

        }

       
    }
    public void OnObjectClicked()
    {
        string handname = handListSelector.GetCurrentHandAction();

        if (currentTask == null)
        {
            Debug.LogWarning("currentTaskが設定されていません");
            return;
        }

        if (handname == "")
        {
            return;
        }

        bool isCorrect = IsCorrectHand(currentTask);

        bool overObject1 = IsMouseOverObject(object1);
        bool overObject2 = IsMouseOverObject(object2);

        if (isCorrect)
        {
            if (overObject1)
            {
                judge1.SetActive(true);
                judge1Effect.ShowCircleAndConfirm();
            }
            else if (overObject2)
            {
                judge2.SetActive(true);
                judge2Effect.ShowCircleAndConfirm();
            }

            Debug.Log("正解です！");
        }
        else
        {
            Debug.Log("不正解です！");
        }
    }

    private bool IsCorrectHand(
        TaskData task)
    {
        if (task == null ||
            task.verb == null ||
            handListSelector == null)
        {
            return false;
        }

        string selectedHandAction =
            handListSelector
                .GetCurrentHandAction();

        string correctVerbName =
            task.verb.name.Replace(
                "Verb_",
                ""
            );

        Debug.Log(
            $"選択した手：{selectedHandAction} / " +
            $"正解の動詞：{correctVerbName}"
        );

        return selectedHandAction ==
               correctVerbName;
    }
    public void SetTask(TaskData task)
    {
        currentTask = task;
    }
  
}