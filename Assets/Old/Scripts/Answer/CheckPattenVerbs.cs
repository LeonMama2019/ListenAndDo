using UnityEngine;

// 動詞のパターン処理


public class CheckPattenVerbs : MonoBehaviour
{
     
    public bool checkedVerbs(VerbData verb)
    {

        switch (verb.english)
        {
            case "take":
                Debug.Log("取る処理");
                break;

            case "put":
                Debug.Log("置く処理");
                break;

            case "eat":
                Debug.Log("食べる処理");
                break;

            default:
                Debug.Log("未対応の動詞です");
                break;
        }

        return false;
    }


}
