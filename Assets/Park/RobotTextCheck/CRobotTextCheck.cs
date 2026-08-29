using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class CRobotTextCheck : MonoBehaviour
{
    [Header("UI Component References")]
    [SerializeField] private Button captchaButton;         // 클릭받을 체크박스 버튼
    [SerializeField] private Image buttonImage;            // 버튼의 Image 컴포넌트
    [SerializeField] private GameObject loadingIcon;       // 회전시킬 '캡챠 동그라미' 오브젝트
    [SerializeField] private Text sentenceText;            // 문장 표시용 Legacy Text

    [Header("Sprites")]
    [SerializeField] private Sprite defaultBoxSprite;      // 기본 빈 체크박스 스프라이트
    [SerializeField] private Sprite checkSprite;           // 성공 체크 스프라이트 (V)
    [SerializeField] private Sprite failSprite;            // 실패 X 표시 스프라이트 (X)

    [Header("Settings")]
    [SerializeField] private float rotateDuration = 1.0f;   // 로딩 회전 시간 (1초)
    [SerializeField] private float rotateSpeed = 360f;      // 초당 회전 각도

    private CancellationTokenSource _cts;
    private bool _isChecked = false;
    private bool _isCorrectAnswer = false; // 정답 문장 여부 저장
    private bool _isProcessing = false;
    private Action _onFailCallback;        // 오답 선택 시 실행할 콜백

    public bool IsChecked => _isChecked;
    public bool IsCorrectAnswer => _isCorrectAnswer;

    private void Awake()
    {
        if (captchaButton == null) captchaButton = GetComponent<Button>();
        if (buttonImage == null && captchaButton != null) buttonImage = captchaButton.GetComponent<Image>();

        if (captchaButton != null)
        {
            captchaButton.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnDisable()
    {
        CancelTask();
    }

    /// <summary>
    /// [수정된 함수] 인자 3개(텍스트, 정답 여부, 실패시 실행할 콜백)를 정상 수신하도록 오버로딩 정의
    /// </summary>
    public void SetSentenceText(string text, bool isCorrect, Action onFailCallback)
    {
        if (sentenceText != null) sentenceText.text = text;
        _isCorrectAnswer = isCorrect;
        _onFailCallback = onFailCallback;
        ResetState();
    }

    public void SetSentenceText(string text, bool isCorrect)
    {
        SetSentenceText(text, isCorrect, null);
    }

    public void SetSentenceText(string text)
    {
        SetSentenceText(text, false, null);
    }

    public void ResetState()
    {
        CancelTask();
        _isChecked = false;
        _isProcessing = false;

        if (captchaButton != null) captchaButton.interactable = true;
        if (buttonImage != null && defaultBoxSprite != null) buttonImage.sprite = defaultBoxSprite;

        if (loadingIcon != null)
        {
            loadingIcon.transform.localRotation = Quaternion.identity;
            loadingIcon.SetActive(false);
        }
    }

    private void OnButtonClicked()
    {
        if (_isProcessing || _isChecked) return; // 이미 검증 중이거나 체크된 상태면 무시
        ProcessClickAsync().Forget();
    }

    /// <summary>
    /// 클릭 시 1초 회전 후 즉시 V 또는 X 판정 연출
    /// </summary>
    private async UniTaskVoid ProcessClickAsync()
    {
        _isProcessing = true;
        if (captchaButton != null) captchaButton.interactable = false;
        if (loadingIcon != null) loadingIcon.SetActive(true);

        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;

        try
        {
            float elapsed = 0f;
            while (elapsed < rotateDuration)
            {
                elapsed += Time.deltaTime;
                if (loadingIcon != null)
                {
                    loadingIcon.transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            // 1초 로딩 회전 아이콘 끄기
            if (loadingIcon != null) loadingIcon.SetActive(false);

            _isChecked = true;

            if (_isCorrectAnswer)
            {
                // 정답 문장일 때: V 표시 후 유지
                if (buttonImage != null && checkSprite != null)
                {
                    buttonImage.sprite = checkSprite;
                }
            }
            else
            {
                // 오답 문장일 때: 
                // 1. 먼저 X 스프라이트로 이미지 교체
                if (buttonImage != null && failSprite != null)
                {
                    buttonImage.sprite = failSprite;
                }

                // 2. 화면에 X 이미지가 렌더링될 수 있도록 1프레임 대기
                await UniTask.Yield(PlayerLoopTiming.Update, token);

                // 3. X 표시를 플레이어가 인지할 수 있도록 0.5초(500ms) 동안 유지 대기
                await UniTask.Delay(500, cancellationToken: token);

                // 4. 0.5초 대기 후 게임 매니저의 실패/재시작 콜백 실행
                _onFailCallback?.Invoke();
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (loadingIcon != null) loadingIcon.SetActive(false);
            _isProcessing = false;
        }
    }

    public void TriggerVerification(bool isSuccess, Action onComplete = null)
    {
        if (_isProcessing) return;
        ProcessVerificationAsync(isSuccess, onComplete).Forget();
    }

    private async UniTaskVoid ProcessVerificationAsync(bool isSuccess, Action onComplete = null)
    {
        _isProcessing = true;
        if (captchaButton != null) captchaButton.interactable = false;
        if (loadingIcon != null) loadingIcon.SetActive(true);

        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;

        try
        {
            float elapsed = 0f;
            while (elapsed < rotateDuration)
            {
                elapsed += Time.deltaTime;
                if (loadingIcon != null)
                {
                    loadingIcon.transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            if (buttonImage != null)
            {
                if (isSuccess && checkSprite != null) buttonImage.sprite = checkSprite;
                else if (!isSuccess && failSprite != null) buttonImage.sprite = failSprite;
            }

            onComplete?.Invoke();
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (loadingIcon != null) loadingIcon.SetActive(false);
            if (captchaButton != null) captchaButton.interactable = true;
            _isProcessing = false;
        }
    }

    private void CancelTask()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }
}