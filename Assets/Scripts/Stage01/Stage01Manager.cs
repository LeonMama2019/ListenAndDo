using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Stage01Manager : MonoBehaviour
{
    public StageData stage;
    public TextMeshProUGUI QuestionText;
    public AnswerManager answerManager;
    public ImageData imageData;
    public SpriteRenderer object1;
    public SpriteRenderer object2;
    public VerbsController verbsController;

    [SerializeField] private GameObject Panel;

    [Header("問題音声を再生するAudioSource")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AnswerStage01 stage01Answer;

    private readonly List<TaskData> remainingTasks = new List<TaskData>();
    private TaskData currentTask;
    private int SpeakerClickCount;
    private string textForShow;
    private bool stageFinished;

    public int TotalQuestionCount
    {
        get { return stage != null && stage.tasks != null ? stage.tasks.Length : 0; }
    }

    private void Start()
    {
        StartStage();
    }

    private void StartStage()
    {
        remainingTasks.Clear();
        stageFinished = false;

        if (stage == null || stage.tasks == null || stage.tasks.Length == 0)
        {
            Debug.LogWarning("Stage01Manager: StageDataに問題がありません");
            FinishStage();
            return;
        }

        foreach (TaskData task in stage.tasks)
        {
            if (task != null)
            {
                remainingTasks.Add(task);
            }
        }

        ShuffleTasks();
        ShowNextQuestion();
    }

    private void ShuffleTasks()
    {
        for (int i = remainingTasks.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            TaskData temporary = remainingTasks[i];
            remainingTasks[i] = remainingTasks[randomIndex];
            remainingTasks[randomIndex] = temporary;
        }
    }

    public void ShowNextQuestion()
    {
        if (stageFinished)
            return;

        if (remainingTasks.Count == 0)
        {
            FinishStage();
            return;
        }

        currentTask = remainingTasks[0];
        remainingTasks.RemoveAt(0);

        VerbData verb = currentTask.verb;
        answerManager?.ReturnResult(currentTask);

        textForShow = MakeSentenceJP(currentTask);
        SetImages(currentTask);

        verbsController?.SetVerb(verb, currentTask);

        if (stage01Answer != null)
        {
            stage01Answer.SetStageManager(this);
            stage01Answer.SetTask(currentTask);
        }

        SpeakerClickCount = 0;

        if (Panel != null)
        {
            Panel.SetActive(false);
        }

        PlayCurrentVoice();
    }

    private void FinishStage()
    {
        stageFinished = true;
        currentTask = null;

        if (voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
        }

        if (stage01Answer != null)
        {
            stage01Answer.SetTask(null);
        }

        ShowText("終了");
        Debug.Log("Stage1の全問題が終了しました");
    }

    private Sprite GetRandomWrongImage(Sprite answer)
    {
        if (imageData == null || imageData.answerImages == null ||
            imageData.answerImages.Length == 0)
        {
            Debug.LogWarning("Stage01Manager: 不正解画像が登録されていません");
            return null;
        }

        Sprite randomSprite;

        do
        {
            int index = Random.Range(0, imageData.answerImages.Length);
            randomSprite = imageData.answerImages[index];
        }
        while (randomSprite == answer && imageData.answerImages.Length > 1);

        return randomSprite;
    }

    private void SetImages(TaskData task)
    {
        Sprite answer = task.answerImage;
        Sprite wrong = GetRandomWrongImage(answer);

        bool answerLeft = Random.Range(0, 2) == 0;

        if (answerLeft)
        {
            object1.sprite = answer;
            object2.sprite = wrong;
        }
        else
        {
            object1.sprite = wrong;
            object2.sprite = answer;
        }
    }

    private string MakeSentenceJP(TaskData task)
    {
        string phrase = "";

        if (task.targetAdjective != null)
        {
            phrase += task.targetAdjective.kanji;
        }

        if (task.referenceObject != null)
        {
            phrase += task.referenceObject.kanji;
        }

        if (task.targetObject != null)
        {
            phrase += task.targetObject.kanji;
        }

        if (task.verb != null)
        {
            phrase += task.verb.kanji;
        }

        return phrase;
    }

    private void ShowText(string text)
    {
        if (Panel != null)
        {
            Panel.SetActive(true);
        }

        if (QuestionText != null)
        {
            QuestionText.text = text;
        }
    }

    public void PlayCurrentVoice()
    {
        if (stageFinished)
            return;

        if (voiceAudioSource == null)
        {
            Debug.LogWarning("Voice Audio Sourceが設定されていません");
            return;
        }

        if (currentTask == null)
        {
            Debug.LogWarning("currentTaskが設定されていません");
            return;
        }

        if (currentTask.voiceClip == null)
        {
            Debug.LogWarning(
                $"現在のTask「{currentTask.name}」にVoice Clipが設定されていません");
            return;
        }

        voiceAudioSource.Stop();
        voiceAudioSource.PlayOneShot(currentTask.voiceClip);

        SpeakerClickCount++;

        if (SpeakerClickCount >= 4)
        {
            ShowText(textForShow);
        }
    }
}
