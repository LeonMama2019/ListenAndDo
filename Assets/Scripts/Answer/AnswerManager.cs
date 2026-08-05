using UnityEngine;

public class AnswerManager : MonoBehaviour
{
    public CheckPattenVerbs checkPattenVerbs;

    public bool ReturnResult(TaskData task)
    {
        

        // 動詞の正解パターン
        GetAnswerVerbs(task.verb);

        //形容詞のパターン

        //名詞のパターン

        //場所のパターン

        //



        return true;
    }
   
    //Verbs
    public void GetAnswerVerbs(VerbData verb)
    {
        checkPattenVerbs.checkedVerbs(verb);



    }


    //Ajective


    //Name



    // まずカーソルが画像の上に止まったかを判定する






}
