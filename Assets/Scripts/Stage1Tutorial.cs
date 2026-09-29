using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Stage1Tutorial : MonoBehaviour
{
    [SerializeField] private GameObject step1;
    [SerializeField] private GameObject step2;
    [SerializeField] private GameObject step3;
    [SerializeField] private GameObject step4;
    [SerializeField] private GameObject okButton;
    [SerializeField] private UIBlink rightHandBlink;
    [SerializeField] private AudioClip tutorial1;
    [SerializeField] private AudioClip tutorial2;
    [SerializeField] private AudioClip tutorial3;
    [SerializeField] private AudioClip tutorial4;

    private int currentStep;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        currentStep = 1;
        gameObject.SetActive(true);
        ShowStep(currentStep);
    }

    // OKボタンの On Click() から呼ぶ。
    public void NextTutorialStep()
    {
        if (currentStep >= 3)
            return;

        currentStep++;
        ShowStep(currentStep);
    }

    // RightHandから呼ぶ。3の間だけ右手の選択で4へ進む。
    public bool OnRightHandSelected()
    {
        if (currentStep != 3)
            return false;

        currentStep = 4;
        ShowStep(currentStep);
        return true;
    }

    private void ShowStep(int step)
    {
        if (step1 != null) step1.SetActive(step == 1);
        if (step2 != null) step2.SetActive(step == 2);
        if (step3 != null) step3.SetActive(step == 3);
        if (step4 != null) step4.SetActive(step == 4);
        if (okButton != null) okButton.SetActive(step != 3);
        if (rightHandBlink != null)
        {
            if (step == 3) rightHandBlink.StartBlinking();
            else rightHandBlink.StopBlinking();
        }

        audioSource.Stop();
        AudioClip clip = step switch
        {
            1 => tutorial1,
            2 => tutorial2,
            3 => tutorial3,
            4 => tutorial4,
            _ => null
        };
        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}
