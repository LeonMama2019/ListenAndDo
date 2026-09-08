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

    /// <summary>
    /// Stage01の最初にSpeakerを押してもらうチュートリアル。
    /// Stage01のチュートリアルが未完了の時だけ実行する。
    /// </summary>
    public void StartTutorial()
    {
        if (PlayerPrefs.GetInt("Stage01", 0) == 1)
        {
            if (darkPanel != null)
                darkPanel.SetActive(false);

            return;
        }

        if (object1Button != null)
            object1Button.interactable = false;

        if (object2Button != null)
            object2Button.interactable = false;

        if (darkPanel != null)
            darkPanel.SetActive(true);

        firstSpeakerTutorialActive = true;
        SpeakerTutorial();
    }

    /// <summary>
    /// Speakerを押すように促すチュートリアル
    /// </summary>
    public void SpeakerTutorial()
    {
        Debug.Log("Speakerチュートリアル開始");

        if (speakerAnimator != null)
        {
            speakerAnimator.enabled = true;
            speakerAnimator.ResetTrigger("Start");
            speakerAnimator.SetTrigger("Start");
        }

        PlayVoice(stage01SpeakerClip);
    }

    /// <summary>
    /// Speakerが押された時
    /// </summary>
    public void OnClickButton()
    {
        // 最初のSpeakerチュートリアル中なら、クリックでDarkPanelを消す
        if (firstSpeakerTutorialActive)
        {
            firstSpeakerTutorialActive = false;

            if (darkPanel != null)
                darkPanel.SetActive(false);

            if (speakerStopCoroutine != null)
                StopCoroutine(speakerStopCoroutine);

            speakerStopCoroutine = StartCoroutine(StopSpeakerAfterCurrentLoop());
            return;
        }

        // 既存チュートリアル用。連打で何度も加算されるのを防ぐ
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

    /// <summary>
    /// Speakerアニメーションの現在の一周が
    /// 終わってからAnimatorを止める
    /// </summary>
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

    /// <summary>
    /// HandListが押された時
    /// </summary>
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

    /// <summary>
    /// チュートリアル全体が完了した時
    /// </summary>
    private void OnTutorialComplete()
    {
        if (!onButton)
            return;

        if (object1Button != null)
            object1Button.interactable = true;

        if (object2Button != null)
            object2Button.interactable = true;

        PlayerPrefs.SetInt("Stage01", 1);
        PlayerPrefs.Save();
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
        if (object1Button != null)
            object1Button.interactable = true;

        if (object2Button != null)
            object2Button.interactable = true;

        if (handListAnimator != null)
            handListAnimator.enabled = false;

        if (darkPanel != null)
            darkPanel.SetActive(false);
    }
}