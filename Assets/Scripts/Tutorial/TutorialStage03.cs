using UnityEngine;

public class TutorialStage03 : MonoBehaviour
{
    [Header("Stage03 チュートリアル")]
    [SerializeField] private GameObject darkPanel;

    [Header("HandList")]
    [SerializeField] private Animator handListAnimator;

    [Header("チュートリアル音声")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AudioClip selectPickVoiceClip;

    private bool openingTutorialActive = false;
    private Vector3 originalHandListScale = Vector3.one;

    private void Start()
    {
        StartOpeningTutorial();
    }

    private void StartOpeningTutorial()
    {
        openingTutorialActive = true;
        if (darkPanel != null) darkPanel.SetActive(true);

        if (handListAnimator != null)
        {
            originalHandListScale = handListAnimator.transform.localScale;
            handListAnimator.enabled = true;
            handListAnimator.Play("HandListPulse", 0, 0f);
            handListAnimator.Update(0f);
        }

        PlayVoice(selectPickVoiceClip);
    }

    // Stage03ではPick（摘まむ）が選ばれるまでDarkPanelを解除しない。
    public void OnHandChanged(string handAction)
    {
        if (!openingTutorialActive || handAction != "pick") return;
        openingTutorialActive = false;

        if (darkPanel != null) darkPanel.SetActive(false);
        if (handListAnimator != null)
        {
            handListAnimator.enabled = false;
            handListAnimator.transform.localScale = originalHandListScale;
        }

        Stage03Manager manager = FindFirstObjectByType<Stage03Manager>();
        if (manager != null) manager.StartQuestionsAfterPickSelected();
        else Debug.LogWarning("TutorialStage03: Stage03Managerが見つかりません");
    }

    private void PlayVoice(AudioClip clip)
    {
        if (voiceAudioSource == null || clip == null)
        {
            Debug.LogWarning("TutorialStage03: AudioSourceまたは音声が設定されていません");
            return;
        }
        voiceAudioSource.Stop();
        voiceAudioSource.PlayOneShot(clip);
    }
}
