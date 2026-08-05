using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialStage01 : MonoBehaviour
{


    public Button object1Button;
    public Button object2Button;
   
    public GameObject Handlist;
    public Animator handListAnimator;
    public Animator speakerAnimator;
    [Header("チュートリアル音声を再生するAudioSource")]
    [SerializeField] private AudioSource voiceAudioSource;

    [Header("Stage01のチュートリアル音声")]
    [SerializeField] private AudioClip stage01VoiceClip;
    [SerializeField] private AudioClip stage01SpeakerClip;
    public bool onButton = false;
    private int tutorialCompleted = 0;
    private Coroutine speakerStopCoroutine;

    // オブジェクトの二つは無効状態にする
    public void StartTutorial()
    {
        object1Button.interactable = false;
        object2Button.interactable = false;
        speakerAnimator.enabled = false;
        Debug.Log("StartTutorialyobareta");
        //音声.
        // Stage01の音声を再生
        if (voiceAudioSource != null && stage01VoiceClip != null)
        {
            Debug.Log("StartTutorialkokoka?");
            voiceAudioSource.Stop();
            voiceAudioSource.PlayOneShot(stage01VoiceClip);
        }
        else
        {
            Debug.LogWarning("AudioSourceかStage01音声が設定されていません");
        }

        //枠のアニメーションを起動
        handListAnimator.SetTrigger("Start");


    }
    public bool SpeakerTutorial()
    {
        speakerAnimator.enabled = true;
        //スピーカーアニメーション
        speakerAnimator.SetTrigger("Start");

      
        // Stage01の音声を再生
        if (voiceAudioSource != null && stage01SpeakerClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.PlayOneShot(stage01SpeakerClip);
        }
        else
        {
            Debug.LogWarning("AudioSourceかStage01音声が設定されていません");
        }
      
                
        return onButton;


    }
    public void OnClickButton()
    {
        // 連打で重複処理されないようにする
       // if (onButton)
         //   return;

        onButton = true;
        tutorialCompleted++;

        // 現在の1周が終わったところで停止する
        speakerStopCoroutine =
            StartCoroutine(StopSpeakerAfterCurrentLoop());

        if (tutorialCompleted >= 2)
        {
            OnHandListAnimationComplete();
        }
    }

    private IEnumerator StopSpeakerAfterCurrentLoop()
    {
        if (speakerAnimator == null)
            yield break;

        // Animatorが最新状態になるまで1フレーム待つ
        yield return null;

        AnimatorStateInfo stateInfo =
            speakerAnimator.GetCurrentAnimatorStateInfo(0);

        /*
         * normalizedTimeはループするたび、
         * 0～1、1～2、2～3……と増える。
         *
         * 現在が2.4なら、3.0まで待つ。
         */
        float finishTime =
            Mathf.Floor(stateInfo.normalizedTime) + 1f;

        while (speakerAnimator.enabled)
        {
            stateInfo =
                speakerAnimator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.normalizedTime >= finishTime)
                break;

            yield return null;
        }

        // 現在の1周が完全に終わってから停止
        speakerAnimator.enabled = false;
        speakerStopCoroutine = null;
    }
    public void onClickHand()
    {
        handListAnimator.enabled = false;
        tutorialCompleted++;
        if(tutorialCompleted == 2)
        {
            OnHandListAnimationComplete();

        }
     

        StartCoroutine(DelayStart());

      
    }
    private IEnumerator DelayStart()
    {
        yield return new WaitForSeconds(3f);

        // 3秒後に実行したい処理
        Debug.Log("3秒経過！");
        SpeakerTutorial();
    }

    //枠のアニメーションを起動したら
    public  void OnHandListAnimationComplete()
    {
       
        if (onButton)
        {

            //オブジェクトの二つは有効状態にする
            object1Button.interactable = true;
            object2Button.interactable = true;
            //

            // チュートリアルが終了したことを記録する
            PlayerPrefs.SetInt("Stage01", 1);
            PlayerPrefs.Save();

                    
          
        }


    }




}
