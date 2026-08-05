using UnityEngine;
using TMPro;
using System.Collections;
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
    public TutorialManager tutorialManager;
    private TaskData currentTask;

    [Header("問題音声を再生するAudioSource")]
    [SerializeField] private AudioSource voiceAudioSource;


    private IEnumerator Start()
    {
        

        int randomIndex = Random.Range(0, stage.tasks.Length);

        TaskData task = stage.tasks[randomIndex];
        currentTask = task;

        VerbData verb = task.verb;

        bool retcode = answerManager.ReturnResult(task);

        string textForShow = MakeSentenceJP(task);
        ShowText(textForShow);

        SetImages(task);

        verbsController.SetVerb(verb, task);

        // 音声を再生
        PlayCurrentVoice();

        // 音声が終わるまで待つ
        if (voiceAudioSource != null)
        {
            while (voiceAudioSource.isPlaying)
            {

                yield return null;
            }
            yield return new WaitForSeconds(2f);
        }

        // 音声終了後にチュートリアル開始
        if (!tutorialManager.IsTutorialCompleted("Stage01"))
        {
            tutorialManager.StartTutorial("Stage01");
        }
    }

    //　ランダムで不正解側のイメージを取得
    Sprite GetRandomWrongImage(Sprite answer)
    {
        Sprite randomSprite;

        do
        {
            int index = Random.Range(0, imageData.answerImages.Length);
            randomSprite = imageData.answerImages[index];

        } while (randomSprite == answer);

        return randomSprite;
    }

    void SetImages(TaskData task)
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
    string MakeSentenceJP(TaskData task)
    {
        string phrase = "";
        if(task.targetAdjective != null)
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

    void ShowText(string question)
    {

        QuestionText.text = question;

    }

    public void PlayCurrentVoice()
    {
        tutorialManager.StopAnimationSpeaker();
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
    }

   
  }
