using UnityEngine;

public class Stage1Tutorial : MonoBehaviour
{
    [SerializeField] private GameObject step1;
    [SerializeField] private GameObject step2;
    [SerializeField] private GameObject step3;
    [SerializeField] private UIBlink rightHandBlink;

    private int currentStep;

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

    private void ShowStep(int step)
    {
        if (step1 != null) step1.SetActive(step == 1);
        if (step2 != null) step2.SetActive(step == 2);
        if (step3 != null) step3.SetActive(step == 3);
        if (rightHandBlink != null)
        {
            if (step == 3) rightHandBlink.StartBlinking();
            else rightHandBlink.StopBlinking();
        }
    }
}
