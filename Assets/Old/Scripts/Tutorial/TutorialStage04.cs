using UnityEngine;

public class TutorialStage04 : MonoBehaviour
{
    [Header("Stage04 チュートリアル")]
    [SerializeField] private GameObject darkPanel;

    [Header("HandList")]
    [SerializeField] private Animator handListAnimator;

    [Header("チュートリアル音声")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AudioClip selectPointVoiceClip;

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

        PlayVoice(selectPointVoiceClip);
    }

    // Stage04ではPoint（指差し）が選ばれるまでDarkPanelを解除しない。
    public void OnHandChanged(string handAction)
    {
        if (!openingTutorialActive || handAction != "point") return;
        openingTutorialActive = false;

        if (darkPanel != null) darkPanel.SetActive(false);
        if (handListAnimator != null)
        {
            handListAnimator.enabled = false;
            handListAnimator.transform.localScale = originalHandListScale;
        }

        Stage04Manager manager = FindFirstObjectByType<Stage04Manager>();
        if (manager != null) manager.StartQuestionsAfterPointSelected();
        else Debug.LogWarning("TutorialStage04: Stage04Managerが見つかりません");
    }

    private void PlayVoice(AudioClip clip)
    {
        if (voiceAudioSource == null || clip == null)
        {
            Debug.LogWarning("TutorialStage04: AudioSourceまたは音声が設定されていません");
            return;
        }
        voiceAudioSource.Stop();
        voiceAudioSource.PlayOneShot(clip);
    }
}
