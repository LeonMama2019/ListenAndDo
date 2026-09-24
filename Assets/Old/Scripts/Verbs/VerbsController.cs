using UnityEngine;

public class VerbsController : MonoBehaviour
{

    public void SetVerb(VerbData verb, TaskData task)
    {

        Debug.Log(verb);
        if(verb != null)
        {
            switch(verb.english)
            {
                case "touch":
                    // Handle touch verb
                    OnMouseDown(task.answerImage);

                    break;
                case "hit":
                    // Handle hit verb
                    break;
                // Add more cases as needed
            }
        }
            
        
            
            
            



    }

    //sawatta
    private void OnMouseDown(object correctObject)
    {
        if (correctObject == this)
        {

            Debug.Log(gameObject.name + "をタッチしました");
        }


        // ここに処理を書く
    }
}
