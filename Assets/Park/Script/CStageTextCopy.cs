using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CStageTextCopy : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Text captchaDisplayArea; // 캡챠 텍스트
    [SerializeField] private InputField playerInputField; // 입력창
    [SerializeField] private Button submitButton;     // 확인 버튼

    [Header("Captcha Settings")]
    [SerializeField] private int captchaLength = 4;    // 이미지처럼 4자리 난수 (원하는 길이 설정 가능)

    private string _currentAnswer = ""; // 현재 정답

    private void Awake()
    {
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmitButtonClicked);
        }
    }

    private void OnEnable()
    {
        // 오브젝트가 켜질 때마다 새로운 캡챠 생성
        GenerateNewCaptcha();
    }

    /// <summary>
    /// 무작위 난수 캡챠 생성 및 표시
    /// </summary>
    public void GenerateNewCaptcha()
    {
        // 1. 이미지 형태처럼 영문 대문자+숫자 무작위 4자리 생성 (예: Y86X)
        _currentAnswer = GenerateRandomString(captchaLength);

        // 2. 텍스트 기울임 및 높낮이 변형 적용
        string distortedText = DistortText(_currentAnswer);

        // 3. UI 갱신
        if (captchaDisplayArea != null)
        {
            captchaDisplayArea.text = distortedText;
        }

        if (playerInputField != null)
        {
            playerInputField.text = "";
            playerInputField.ActivateInputField();
        }
    }

    /// <summary>
    /// Rich Text를 이용해 글자별 위치 및 기울임 연출
    /// </summary>
    /// 
    private string DistortText(string rawText)
    {
        string result = "";

        foreach (char c in rawText)
        {
            // 1. 글자 크기 변동 (Legacy Text 지원)
            int fontSize = Random.Range(30, 52);

            // 2. 50% 확률로 기울임(i) 및 굵게(b) 적용 (Legacy Text 지원)
            bool isItalic = Random.value > 0.5f;
            bool isBold = Random.value > 0.5f;

            string charStr = c.ToString();
            if (isItalic) charStr = $"<i>{charStr}</i>";
            if (isBold) charStr = $"<b>{charStr}</b>";

            // 3. 태그 조립 및 무작위 자간 띄우기
            result += $"<size={fontSize}>{charStr}</size> ";
        }

        return result;
    }

    /// <summary>
    /// 확인 버튼 클릭 시 검증
    /// </summary>
    private void OnSubmitButtonClicked()
    {
        if (playerInputField == null) return;

        string userInput = playerInputField.text.Trim().ToUpper();

        if (userInput == _currentAnswer)
        {
            Debug.Log("[Captcha] 정답입니다!");
            if (CGameManager.Instance != null)
            {
                CGameManager.Instance.StageClear();
            }
        }
        else
        {
            Debug.Log("[Captcha] 오답입니다!");
            if (CGameManager.Instance != null)
            {
                CGameManager.Instance.StageFailed();
            }
        }
    }

    /// <summary>
    /// 영문 대문자 + 숫자 혼합 무작위 문자열 생성
    /// </summary>
    private string GenerateRandomString(int length)
    {
        // 헷갈리기 쉬운 O, 0, I, 1 등은 제거한 문자 조합
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        char[] stringChars = new char[length];

        for (int i = 0; i < length; i++)
        {
            stringChars[i] = chars[Random.Range(0, chars.Length)];
        }

        return new string(stringChars);
    }
}