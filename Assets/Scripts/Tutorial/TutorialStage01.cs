using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialStage01 : MonoBehaviour
{
    [Header("回答ボタン")]
    [SerializeField] private Button object1Button;
    [SerializeField] private Button object2Button;

    [Header("HandList")]
    [SerializeField] private GameObject handList;
    [SerializeField] private Animator handListAnimator;

    [Header("Speaker")]
    [SerializeField] private Animator speakerAnimator;

    [Header("Speakerチュートリアル")]
    [SerializeField] private GameObject darkPanel;

    [Header("AnswerStage01")]
    [SerializeField] private AnswerStage01 stage01Answer;

    [Header("チュートリアル音声を再生するAudioSource")]
    [SerializeField] private AudioSource voiceAudioSource;

    [Header("HandListを促す音声")]
    [SerializeField] private AudioClip stage01VoiceClip;

    [Header("Speakerを促す音声")]
    [SerializeField] private AudioClip stage01SpeakerClip;

    private bool onButton = false;
    private int tutorialCompleted = 0;
    private bool firstSpeakerTutorialActive = false;

    private Coroutine speakerStopCoroutine;

    public void StartTutorial()
    {
        if (PlayerPrefs.GetInt("Stage01", 0) == 1)
        {
            if (darkPanel != null)
                darkPanel.SetActive(false);

            SetAnswerInputEnabled(true);
            return;
        }

        // DarkPanel表示中は回答オブジェクトを触れないようにする
        SetAnswerInputEnabled(false);

        if (darkPanel != null)
            darkPanel.SetActive(true);

        firstSpeakerTutorialActive = true;
        SpeakerTutorial();
    }

    public void SpeakerTutorial()
    {
        Debug.Log("Speakerチュートリアル開始");

        if (speakerAnimator != null)
        {
            speakerAnimator.enabled = true;
            speakerAnimator.Rebind();
            speakerAnimator.Update(0f);

            // Controller上の実際のState名を直接再生する
            speakerAnimator.Play("SpeakerClip", 0, 0f);
        }
        else
        {
            Debug.LogWarning("Speaker Animatorが設定されていません");
        }

        PlayVoice(stage01SpeakerClip);
    }

    public void OnClickButton()
    {
        if (firstSpeakerTutorialActive)
        {
            firstSpeakerTutorialActive = false;

            if (darkPanel != null)
                darkPanel.SetActive(false);

            // DarkPanelが消えたら回答オブジェクトを再び有効にする
            SetAnswerInputEnabled(true);

            if (speakerStopCoroutine != null)
                StopCoroutine(speakerStopCoroutine);

            speakerStopCoroutine = StartCoroutine(StopSpeakerAfterCurrentLoop());
            return;
        }

        if (onButton)
            return;

        onButton = true;
        tutorialCompleted++;

        if (speakerStopCoroutine != null)
            StopCoroutine(speakerStopCoroutine);

        speakerStopCoroutine = StartCoroutine(StopSpeakerAfterCurrentLoop());

        if (tutorialCompleted >= 2)
            OnTutorialComplete();
    }

    private IEnumerator StopSpeakerAfterCurrentLoop()
    {
        if (speakerAnimator == null)
            yield break;

        yield return null;

        AnimatorStateInfo stateInfo = speakerAnimator.GetCurrentAnimatorStateInfo(0);
        float finishTime = Mathf.Floor(stateInfo.normalizedTime) + 1f;

        while (speakerAnimator.enabled)
        {
            stateInfo = speakerAnimator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.normalizedTime >= finishTime)
                break;

            yield return null;
        }

        speakerAnimator.enabled = false;
        speakerStopCoroutine = null;
    }

    public void OnClickHand()
    {
        if (handListAnimator != null)
            handListAnimator.enabled = false;

        tutorialCompleted++;
        StartCoroutine(DelaySpeakerTutorial());
    }

    private IEnumerator DelaySpeakerTutorial()
    {
        yield return new WaitForSeconds(3f);
        SpeakerTutorial();
    }

    private void OnTutorialComplete()
    {
        if (!onButton)
            return;

        SetAnswerInputEnabled(true);

        PlayerPrefs.SetInt("Stage01", 1);
        PlayerPrefs.Save();
    }

    private void SetAnswerInputEnabled(bool enabled)
    {
        if (object1Button != null)
            object1Button.interactable = enabled;

        if (object2Button != null)
            object2Button.interactable = enabled;

        // 回答判定側も止める。Button以外のCollider入力にも効かせる。
        if (stage01Answer != null)
            stage01Answer.enabled = enabled;
    }

    private void PlayVoice(AudioClip clip)
    {
        if (voiceAudioSource == null || clip == null)
        {
            Debug.LogWarning("AudioSourceまたはチュートリアル音声が設定されていません");
            return;
        }

        voiceAudioSource.Stop();
        voiceAudioSource.PlayOneShot(clip);
    }

    public void EndTutorial()
    {
        SetAnswerInputEnabled(true);

        if (handListAnimator != null)
            handListAnimator.enabled = false;

        if (darkPanel != null)
            darkPanel.SetActive(false);
    }
}