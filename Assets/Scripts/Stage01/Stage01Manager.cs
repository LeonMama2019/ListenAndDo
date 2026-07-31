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

    void Start()
    {
        int randomIndex = Random.Range(0, stage.tasks.Length);


    TaskData task = stage.tasks[randomIndex];

      
        // TaskDataに入っているVerbDataを取得
        VerbData verb = task.verb;

      
      
     

       bool retcode =  answerManager.ReturnResult(task);

        //日本語だったら～
        string Textforshow = MakeSentenceJP(task);
        ShowText(Textforshow);     

        SetImages(task);
        //　動詞
        verbsController.SetVerb(verb, task);


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





}
