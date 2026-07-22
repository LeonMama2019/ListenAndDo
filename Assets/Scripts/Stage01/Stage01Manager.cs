using UnityEngine;
using TMPro;

public class Stage01Manager : MonoBehaviour
{
    public StageData stage;
    public TextMeshProUGUI QuestionText;
    public AnswerManager answerManager;

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
        makeSentenceJP(task);
        ShowText(verb.hiragana);
    }
    void makeSentenceJP(TaskData task)
    {
        string frase;
        //  objcet
        frase = task.referenceObject.kanji;

        //frase = task.





    }

    void ShowText(string question)
    {

        QuestionText.text = question;

    }


}
