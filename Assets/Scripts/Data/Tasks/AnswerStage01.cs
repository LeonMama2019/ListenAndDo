using UnityEngine;

public class Stage01Answer : MonoBehaviour
{
    [Header("判定したいオブジェクト")]
    [SerializeField] private GameObject object1;
    [SerializeField] private GameObject object2;
    public HandListSelector handListSelector;
    public TutorialStage01 tutorialStage01;
    private float stayTime = 0f;
    private string HandName;
    private bool tutorialStarted = false;

    void Update()
    {
        // もうチュートリアルを開始したら何もしない
        if (tutorialStarted)
            return;

        HandName = handListSelector.GetCurrentHandName();

        bool isMouseOver =
            IsMouseOverObject(object1) ||
            IsMouseOverObject(object2);

        if (isMouseOver && HandName == "")
        {
            stayTime += Time.deltaTime;

            if (stayTime >= 5f)
            {
                tutorialStarted = true;

                Debug.Log("StartTutorialyonnderu");
                tutorialStage01.StartTutorial();
            }
        }
        else if(!isMouseOver)
        {
            stayTime += Time.deltaTime;

            if (stayTime >= 5f)
            {
                tutorialStarted = true;

                tutorialStage01.SpeakerTutorial();
            }


            // Debug.Log(HandName);
        }


    }

    bool IsMouseOverObject(GameObject obj)
    {
      
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Collider2D col = obj.GetComponent<Collider2D>();

        if (col == null)
        {
            Debug.LogWarning(obj.name + " にCollider2Dがありません");
            return false;
        }

        return col.OverlapPoint(mousePos);
    }


    //５秒以上カーソルを選んでいなかったらカーソルを選ぶ

}