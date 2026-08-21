using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    private const string Stage01Key = "Stage01";

    [SerializeField] private TutorialStage01 tutorialStage01;

    public bool IsTutorialCompleted(string stageName)
    {
        return PlayerPrefs.GetInt(stageName, 0) == 1;
    }

    public bool IsStage01TutorialActive()
    {
        return !IsTutorialCompleted(Stage01Key);
    }

    public void CompleteTutorial(string stageName)
    {
        PlayerPrefs.SetInt(stageName, 1);
        PlayerPrefs.Save();
    }

    public void CompleteStage01Tutorial()
    {
        CompleteTutorial(Stage01Key);
        tutorialStage01?.StopAllHints();
    }

    // Inspector/Event から呼んでいる場合の互換用。
    // 初回プレイ時だけ Hand ヒントを表示する。
    public void StartTutorial(string stageName)
    {
        if (stageName == Stage01Key && !IsTutorialCompleted(stageName))
        {
            tutorialStage01?.ShowHandHint();
        }
    }

    // 旧 Animation Event との互換用。進行判断はここでは行わない。
    public void OnHandListAnimationComplete(string stageName)
    {
        if (stageName == Stage01Key)
        {
            tutorialStage01?.StopHandHint();
        }
    }

    // 旧 Speaker の Button/Event との互換用。
    public void StopAnimationSpeaker()
    {
        tutorialStage01?.StopSpeakerHintAfterCurrentLoop();
    }
}