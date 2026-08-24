using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Stage01Manager : MonoBehaviour
{
    public StageData stage;
    public TextMeshProUGUI QuestionText;
    public AnswerManager answerManager;
    public ImageData imageData;
    public SpriteRenderer object1;
    public SpriteRenderer object2;
    public VerbsController verbsController;

    private TaskData currentTask;
    private int SpeakerClickCount = 0;
    private string textForShow;

    [SerializeField] private GameObject Panel;

    [Header("問題音声を再生するAudioSource")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AnswerStage01 stage01Answer;

    private void Start()
    {
        ShowNextQuestion();
    }

    /// <summary>
    /// 新しい問題を選び、画像・動詞・正解判定用Task・音声を更新する。
    /// 初回も2問目以降もこのメソッドを使う。
    /// </summary>
    public void ShowNextQuestion()
    {
        if (stage == null || stage.tasks == null || stage.tasks.Length == 0)
        {
            Debug.LogWarning("Stage01Manager: StageDataに問題がありません");
            return;
        }

        int randomIndex = Random.Range(0, stage.tasks.Length);
        currentTask = stage.tasks[randomIndex];

        VerbData verb = currentTask.verb;

        answerManager.ReturnResult(currentTask);

        textForShow = MakeSentenceJP(currentTask);
        SetImages(currentTask);

        verbsController.SetVerb(verb, currentTask);
        stage01Answer.SetTask(currentTask);

        SpeakerClickCount = 0;

        if (Panel != null)
        {
            Panel.SetActive(false);
        }

        PlayCurrentVoice();
    }

    // ランダムで不正解側のイメージを取得
    private Sprite GetRandomWrongImage(Sprite answer)
    {
        Sprite randomSprite;

        do
        {
            int index = Random.Range(0, imageData.answerImages.Length);
            randomSprite = imageData.answerImages[index];
        }
        while (randomSprite == answer);

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

    private void ShowText(string question)
    {
        Panel.SetActive(true);
        QuestionText.text = question;
    }

    public void PlayCurrentVoice()
    {
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
            Debug.LogWarning($"現在のTask「{currentTask.name}」にVoice Clipが設定されていません");
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
