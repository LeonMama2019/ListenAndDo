using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Stage01Manager : MonoBehaviour
{
    public StageData stage;
    public TextMeshProUGUI QuestionText;
    public AnswerManager answerManager;
    public ImageData imageData;

   

    void Start()
    {
        int randomIndex = Random.Range(0, stage.tasks.Length);


    TaskData task = stage.tasks[randomIndex];

      
        // TaskData‚É“ü‚Á‚Ä‚¢‚éVerbData‚ğæ“¾
        VerbData verb = task.verb;

        // VerbData‚Ì’†g‚ğæ“¾
        Debug.Log(verb.english);
        Debug.Log(verb.hiragana);
        Debug.Log(verb.kanji);

       bool retcode =  answerManager.ReturnResult(task);

        //“ú–{Œê‚¾‚Á‚½‚ç`
        string Textforshow = MakeSentenceJP(task);
        ShowText(Textforshow);
    }

    void DummyImage()
    {

        Image objectImage;

    }
    string MakeSentenceJP(TaskData task)
    {
        string phrase = "";

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
