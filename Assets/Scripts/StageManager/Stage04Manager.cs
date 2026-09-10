using System.Collections;
using TMPro;
using UnityEngine;

public class Stage04Manager : MonoBehaviour
{
    public StageData stage;
    public TextMeshProUGUI QuestionText;
    public AnswerManager answerManager;
    public ImageData imageData;
    public SpriteRenderer object1;
    public SpriteRenderer object2;
    public VerbsController verbsController;

    [SerializeField] private GameObject Panel;
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AnswerStage04 stage04Answer;

    [Header("Point選択後、問題を開始するまでの待ち時間")]
    [SerializeField] private float voiceDelay = 3f;

    private TaskData currentTask;
    private int speakerClickCount = 0;
    private string textForShow;
    private Coroutine startQuestionCoroutine;

    private void Start()
    {
        // Stage04はPoint選択チュートリアル完了まで問題を開始しない。
    }

    public void StartQuestionsAfterPointSelected()
    {
        if (startQuestionCoroutine != null) StopCoroutine(startQuestionCoroutine);
        startQuestionCoroutine = StartCoroutine(StartQuestionsAfterDelay());
    }

    private IEnumerator StartQuestionsAfterDelay()
    {
        yield return new WaitForSeconds(voiceDelay);
        startQuestionCoroutine = null;
        ShowNextQuestion();
    }

    public void ShowNextQuestion()
    {
        if (stage == null || stage.tasks == null || stage.tasks.Length == 0)
        {
            Debug.LogWarning("Stage04Manager: StageDataに問題がありません");
            return;
        }

        currentTask = stage.tasks[Random.Range(0, stage.tasks.Length)];
        if (answerManager != null) answerManager.ReturnResult(currentTask);
        textForShow = MakeSentenceJP(currentTask);
        SetImages(currentTask);
        if (verbsController != null) verbsController.SetVerb(currentTask.verb, currentTask);
        if (stage04Answer != null) stage04Answer.SetTask(currentTask);
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
            Debug.LogWarning("Stage04Manager: 問題音声を再生できません");
            return;
        }
        voiceAudioSource.Stop();
        voiceAudioSource.PlayOneShot(currentTask.voiceClip);
        speakerClickCount++;
        if (speakerClickCount >= 4) ShowText(textForShow);
    }
}
