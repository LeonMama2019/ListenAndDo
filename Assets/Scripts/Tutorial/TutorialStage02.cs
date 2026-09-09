using UnityEngine;

public class TutorialStage02 : MonoBehaviour
{
    [Header("Stage02 チュートリアル")]
    [SerializeField] private GameObject darkPanel;

    [Header("HandList")]
    [SerializeField] private Animator handListAnimator;

    [Header("チュートリアル音声")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AudioClip selectHitVoiceClip;

    private bool openingTutorialActive = false;

    private void Start()
    {
        StartOpeningTutorial();
    }

    private void StartOpeningTutorial()
    {
        openingTutorialActive = true;

        if (darkPanel != null)
            darkPanel.SetActive(true);

        if (handListAnimator != null)
        {
            handListAnimator.enabled = true;
            handListAnimator.Play("HandListPulse", 0, 0f);
            handListAnimator.Update(0f);
        }

        PlayVoice(selectHitVoiceClip);
    }

    // HandListSelectorから、手が選択・変更されるたびに呼ぶ。
    // Stage02では hit が選ばれるまでチュートリアルを終了しない。
    public void OnHandChanged(string handAction)
    {
        if (!openingTutorialActive)
            return;

        if (handAction != "hit")
            return;

        openingTutorialActive = false;

        if (darkPanel != null)
            darkPanel.SetActive(false);

        if (handListAnimator != null)
        {
            handListAnimator.enabled = false;
            handListAnimator.transform.localScale = Vector3.one;
        }

        Stage02Manager manager = FindFirstObjectByType<Stage02Manager>();
        if (manager != null)
            manager.StartQuestionsAfterHitSelected();
        else
            Debug.LogWarning("TutorialStage02: Stage02Managerが見つかりません");
    }

    private void PlayVoice(AudioClip clip)
    {
        if (voiceAudioSource == null || clip == null)
        {
            Debug.LogWarning("TutorialStage02: AudioSourceまたは音声が設定されていません");
            return;
        }

        voiceAudioSource.Stop();
        voiceAudioSource.PlayOneShot(clip);
    }
}