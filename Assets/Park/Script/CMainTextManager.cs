using UnityEngine;
using UnityEngine.UI; 
public class CMainTextManager : MonoBehaviour
{
    public static CMainTextManager Instance { get; private set; }

    [Header("UI Reference")]

    [SerializeField] private Text timerText; // 남은 시간 표시용 Legacy Text

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
             DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetTimerText(float remainingTime)
    {
        if (timerText != null)
        {
            timerText.text = $"TIME: {remainingTime:F0}s";
        }
    }

}