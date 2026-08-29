using UnityEngine;
using UnityEngine.UI; 
public class CStageTextManager : MonoBehaviour
{
    public static CStageTextManager Instance { get; private set; }

    [Header("UI Reference")]
    [SerializeField] private Text stageText; // Legacy Text 컴포넌트 연결

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

    public void SetStageText(string message)
    {
        if (stageText != null)
        {
            stageText.text = message;
        }
        else
        {
            Debug.LogWarning("[MainUIManager] topStatusText가 Inspector에 연결되지 않았습니다.");
        }
    }
}