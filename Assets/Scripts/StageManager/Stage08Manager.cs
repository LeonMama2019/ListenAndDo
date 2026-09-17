using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Stage08Manager : MonoBehaviour
{
    [Header("Stage08で混ぜるLevel（Touch / Hit / Pick / Point）")]
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
    [SerializeField] private AnswerStage08 stage08Answer;

    [Header("Stage08 妨害音")]
    [Tooltip("未設定なら問題音声用AudioSourceを共用します")]
    [SerializeField] private AudioSource distractionAudioSource;
    [SerializeField] private AudioClip dropSound;
    [SerializeField] private float beforeSoundGap = 0.1f;
    [SerializeField] private float afterSoundGap = 0.1f;
    [SerializeField] private float doubleSoundGap = 0.2f;

    private readonly List<TaskData> questionPool = new List<TaskData>();
    private TaskData currentTask;
    private int speakerClickCount;
    private int questionNumber;
    private string textForShow;
    private Coroutine initialVoiceCoroutine;

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
            Debug.LogWarning("Stage08Manager: Level1〜4に問題がありません");
            return;
        }

        if (initialVoiceCoroutine != null) StopCoroutine(initialVoiceCoroutine);

        currentTask = questionPool[Random.Range(0, questionPool.Count)];
        questionNumber++;
        if (answerManager != null) answerManager.ReturnResult(currentTask);
        textForShow = MakeSentenceJP(currentTask);
        SetImages(currentTask);
        if (verbsController != null) verbsController.SetVerb(currentTask.verb, currentTask);
        if (stage08Answer != null) stage08Answer.SetTask(currentTask);
        speakerClickCount = 0;
        if (Panel != null) Panel.SetActive(false);

        initialVoiceCoroutine = StartCoroutine(PlayInitialQuestionSequence(questionNumber));
    }

    private IEnumerator PlayInitialQuestionSequence(int number)
    {
        AudioClip voiceClip = currentTask != null ? currentTask.voiceClip : null;
        if (voiceAudioSource == null || voiceClip == null)
        {
            Debug.LogWarning("Stage08Manager: 問題音声を再生できません");
            yield break;
        }

        switch (number)
        {
            case 1:
                yield return PlayDropAndWait(beforeSoundGap);
                PlayVoice(voiceClip);
                break;

            case 2:
                PlayVoice(voiceClip);
                yield return new WaitForSeconds(voiceClip.length * 0.5f);
                PlayDrop();
                break;

            case 3:
                PlayVoice(voiceClip);
                yield return new WaitForSeconds(voiceClip.length + afterSoundGap);
                PlayDrop();
                break;

            case 4:
                yield return PlayDropAndWait(beforeSoundGap);
                PlayVoice(voiceClip);
                yield return new WaitForSeconds(voiceClip.length * 0.5f);
                PlayDrop();
                break;

            case 5:
                PlayVoice(voiceClip);
                yield return new WaitForSeconds(voiceClip.length * 0.5f);
                PlayDrop();
                yield return new WaitForSeconds(voiceClip.length * 0.5f + afterSoundGap);
                PlayDrop();
                break;

            case 6:
                PlayVoice(voiceClip);
                yield return new WaitForSeconds(voiceClip.length + afterSoundGap);
                PlayDrop();
                yield return new WaitForSeconds(GetDropLength() + doubleSoundGap);
                PlayDrop();
                break;

            default:
                PlayVoice(voiceClip);
                break;
        }

        initialVoiceCoroutine = null;
    }

    private IEnumerator PlayDropAndWait(float gap)
    {
        PlayDrop();
        yield return new WaitForSeconds(GetDropLength() + gap);
    }

    private void PlayDrop()
    {
        if (dropSound == null)
        {
            Debug.LogWarning("Stage08Manager: Drop Soundが設定されていません");
            return;
        }

        AudioSource source = distractionAudioSource != null ? distractionAudioSource : voiceAudioSource;
        if (source != null) source.PlayOneShot(dropSound);
    }

    private float GetDropLength()
    {
        return dropSound != null ? dropSound.length : 0f;
    }

    private void PlayVoice(AudioClip clip)
    {
        if (voiceAudioSource == null || clip == null) return;
        voiceAudioSource.Stop();
        voiceAudioSource.PlayOneShot(clip);
    }

    public void PlayCurrentVoice()
    {
        if (voiceAudioSource == null || currentTask == null || currentTask.voiceClip == null)
        {
            Debug.LogWarning("Stage08Manager: 問題音声を再生できません");
            return;
        }

        PlayVoice(currentTask.voiceClip);
        speakerClickCount++;
        LevelResultStore.RecordSpeakerReplay();
        if (speakerClickCount >= 4) ShowText(textForShow);
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
}
