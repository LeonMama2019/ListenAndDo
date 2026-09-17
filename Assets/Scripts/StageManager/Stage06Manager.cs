using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Stage06Manager : MonoBehaviour
{
    [Header("Stage06で混ぜるLevel（Touch / Hit / Pick / Point）")]
    [SerializeField] private StageData level1;
    [SerializeField] private StageData level2;
    [SerializeField] private StageData level3;
    [SerializeField] private StageData level4;

    public TextMeshProUGUI QuestionText;
    public AnswerManager answerManager;
    public ImageData imageData;
    public SpriteRenderer object1;
    public SpriteRenderer object2;
    public VerbsController verbsController;

    [SerializeField] private GameObject Panel;
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AnswerStage06 stage06Answer;

    private readonly List<TaskData> questionPool = new List<TaskData>();
    private TaskData currentTask;
    private int speakerClickCount = 0;
    private string textForShow;

    private void Start()
    {
        BuildQuestionPool();
        ShowNextQuestion();
    }

    private void BuildQuestionPool()
    {
        questionPool.Clear();
        AddTasks(level1);
        AddTasks(level2);
        AddTasks(level3);
        AddTasks(level4);
    }

    private void AddTasks(StageData level)
    {
        if (level == null || level.tasks == null) return;
        foreach (TaskData task in level.tasks)
        {
            if (task != null) questionPool.Add(task);
        }
    }

    public void ShowNextQuestion()
    {
        if (questionPool.Count == 0)
        {
            Debug.LogWarning("Stage06Manager: Level1〜4に問題がありません");
            return;
        }

        currentTask = questionPool[Random.Range(0, questionPool.Count)];
        if (answerManager != null) answerManager.ReturnResult(currentTask);
        textForShow = MakeSentenceJP(currentTask);
        SetImages(currentTask);
        if (verbsController != null) verbsController.SetVerb(currentTask.verb, currentTask);
        if (stage06Answer != null) stage06Answer.SetTask(currentTask);
        speakerClickCount = 0;
        if (Panel != null) Panel.SetActive(false);
        PlayCurrentVoice();
    }

    private Sprite GetRandomWrongImage(Sprite answer)
    {
        if (imageData == null || imageData.answerImages == null || imageData.answerImages.Length == 0) return null;
        Sprite randomSprite;
        do { randomSprite = imageData.answerImages[Random.Range(0, imageData.answerImages.Length)]; }
        while (imageData.answerImages.Length > 1 && randomSprite == answer);
        return randomSprite;
    }

    private void SetImages(TaskData task)
    {
        if (task == null || object1 == null || object2 == null) return;
        Sprite answer = task.answerImage;
        Sprite wrong = GetRandomWrongImage(answer);
        bool answerLeft = Random.Range(0, 2) == 0;
        object1.sprite = answerLeft ? answer : wrong;
        object2.sprite = answerLeft ? wrong : answer;

        SpriteDisplayNormalizer.Normalize(object1);
        SpriteDisplayNormalizer.Normalize(object2);
    }

    private string MakeSentenceJP(TaskData task)
    {
        string phrase = "";
        if (task.targetAdjective != null) phrase += task.targetAdjective.kanji;
        if (task.referenceObject != null) phrase += task.referenceObject.kanji;
        if (task.targetObject != null) phrase += task.targetObject.kanji;
        if (task.verb != null) phrase += task.verb.kanji;
        return phrase;
    }

    private void ShowText(string question)
    {
        if (Panel != null) Panel.SetActive(true);
        if (QuestionText != null) QuestionText.text = question;
    }

    public void PlayCurrentVoice()
    {
        if (voiceAudioSource == null || currentTask == null || currentTask.voiceClip == null)
        {
            Debug.LogWarning("Stage06Manager: 問題音声を再生できません");
            return;
        }
        voiceAudioSource.Stop();
        voiceAudioSource.PlayOneShot(currentTask.voiceClip);
        speakerClickCount++;
        LevelResultStore.RecordSpeakerReplay();
        if (speakerClickCount >= 4) ShowText(textForShow);
    }
}
