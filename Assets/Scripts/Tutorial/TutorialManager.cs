using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public TutorialStage01 tutorialStage01;
   

    public void CompleteTutorial(string stageName)
    {
        // チュートリアルが終了したことを記録する
        PlayerPrefs.SetInt(stageName, 1);
        PlayerPrefs.Save();
    }
    public bool IsTutorialCompleted(string stageName)
    {
        // チュートリアルが終了したかどうかを判定する
        return PlayerPrefs.GetInt(stageName, 0) == 1;
    }

    public void StartTutorial(string stageName)
    {
       if(stageName == "Stage01")
        {
            tutorialStage01.StartTutorial();



        }


    }
    public void OnHandListAnimationComplete(string stageName)
    {
        if (stageName == "Stage01")
        {
            tutorialStage01.onClickHand();
        }
    }


    public void StopAnimationSpeaker()
    {
        tutorialStage01.OnClickButton();
    }




}
