using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CGameManager : MonoBehaviour
{
    // 외부에서 CGameManager.Instance 로 접근
    public static CGameManager Instance { get; private set; }

    [SerializeField] private int _currentStage = 1;
    [SerializeField] private int _maxStage = 5;

    [Tooltip("순서대로 고정 진행할 스테이지 개수를 설정합니다 (예: 3 설정 시 1~3 스테이지 고정).")]
    [SerializeField] private int _fixedStage = 3;

    [Header("UI Settings")]
    [Tooltip("실패 시 화면 중앙에 띄울 Fail Text GameObject를 연결하세요.")]
    [SerializeField] private GameObject failTextObject;

    [Tooltip("일반 스테이지 클리어 시 화면 중앙에 띄울 Success Text GameObject를 연결하세요.")]
    [SerializeField] private GameObject successTextObject;

    [Tooltip("모든 스테이지를 최종 클리어(MaxStage 도달)했을 때 화면 중앙에 띄울 All Clear Text GameObject를 연결하세요.")]
    [SerializeField] private GameObject allClearTextObject;

    [Header("Stage Inspector")]
    [Tooltip("앞쪽 인덱스는 _fixedStage 수만큼 고정 배치되고, 그 이후 인덱스부터 무작위로 등장합니다.")]
    [SerializeField] public List<GameObject> stagePrefabList = new List<GameObject>(); // 프리팹 방식일 경우

    [Header("Timer Settings")]
    [SerializeField] private float stageLimitTime = 60f; // 스테이지당 제한시간 (초 단위)
    private float _currentTimer = 60f;
    private bool _isTimerRunning = true;
    private bool _stageClear;
    private bool _isTimeDown = false;

    private List<int> _selectedStageList = new List<int>();

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

    private void Start()
    {
        // 게임 시작 시 UI 텍스트들이 켜져있다면 숨김 처리
        if (failTextObject != null) failTextObject.SetActive(false);
        if (successTextObject != null) successTextObject.SetActive(false);
        if (allClearTextObject != null) allClearTextObject.SetActive(false);

        ChooseStageSequence();
        UpdateStageUI(_currentStage);
    }

    private void Update()
    {
        // 타이머 카운트다운 로직
        if (_isTimerRunning)
        {
            _currentTimer -= Time.deltaTime;

            if (CMainTextManager.Instance != null)
            {
                CMainTextManager.Instance.SetTimerText(_currentTimer);
            }

            if (_currentTimer <= 0f)
            {
                _currentTimer = 0f;
                _isTimerRunning = false;

                Debug.Log($"[Stage {_currentStage}] 시간 초과!");
                StageFailed(); // 제한시간 초과 시 실패 처리
            }
        }
    }

    public void StartTimer()
    {
        if (!_isTimeDown)
            _currentTimer = stageLimitTime;
        _isTimerRunning = true;
    }

    public void TimeDown(float timeLimit)
    {
        _currentTimer = timeLimit;
        _isTimeDown = true;
    }

    public void StageClear()
    {
        if (_stageClear)
            return;
        _stageClear = true;
        Debug.Log($"[Stage {_currentStage}] 스테이지 클리어!");
        

        // 진행 중인 타이머 멈춤 및 Success 코루틴 실행
        _isTimerRunning = false;
        StartCoroutine(StageClearRoutine());
    }
    private IEnumerator StageClearRoutine()
    {
        bool isFinalStage = (_currentStage >= _maxStage);

        if (!isFinalStage)
        {
            // 일반 스테이지 클리어 로직
            if (successTextObject != null)
            {
                yield return StartCoroutine(PopUpAnimation(successTextObject, 1.0f));
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            _currentStage++;
            UpdateStageUI(_currentStage);
        }
        else
        {
            // 1. 최종 올 클리어 팝업 애니메이션 재생 (약 1초 소요)
            if (allClearTextObject != null)
            {
                yield return StartCoroutine(PopUpAnimation(allClearTextObject, 1.0f));
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            Debug.Log("모든 스테이지 올 클리어 완료!");

            // 2. 추가 대기 시간 (팝업이 닫히고 잠시 후 자연스럽게 종료하고 싶을 때)
            yield return new WaitForSeconds(0.5f);

            // 3. 게임 종료 호출
            QuitGame();
        }
    }
    public void QuitGame()
    {
        Debug.Log("게임을 종료합니다.");

#if UNITY_EDITOR
        // 유니티 에디터에서 실행 중일 때 플레이 모드 종료
        UnityEditor.EditorApplication.isPlaying = false;
#else
    // 실제 빌드된 게임(PC, Mobile 등) 종료
    Application.Quit();
#endif
    }

    public void StageFailed()
    {
        Debug.Log($"[Stage {_currentStage}] 실패 - 1초 후 1스테이지로 리셋됩니다.");

        // 진행 중인 타이머 멈춤 및 Fail 코루틴 실행
        _isTimerRunning = false;
        StartCoroutine(StageFailedRoutine());
    }

    private IEnumerator StageFailedRoutine()
    {
        // 1. Fail 팝업 애니메이션 재생 (약 1초 소요)
        if (failTextObject != null)
        {
            yield return StartCoroutine(PopUpAnimation(failTextObject, 1.0f));
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        // 2. 게임 리셋 및 재시작
        _currentStage = 1;
        ChooseStageSequence(); // 실패 시 고정 구간 이후 랜덤 순서 재구성
        UpdateStageUI(_currentStage);
    }

    /// <summary>
    /// 지정된 오브젝트를 빠르게 커지게 만든 뒤 유지했다가 비활성화하는 팝업 코루틴
    /// </summary>
    private IEnumerator PopUpAnimation(GameObject targetObj, float totalDuration)
    {
        targetObj.SetActive(true);

        Transform targetTransform = targetObj.transform;
        Vector3 defaultScale = Vector3.one; // 기본 크기 (1, 1, 1)

        // 0.25초 동안 0에서 1로 확대
        float popDuration = 0.25f;
        float elapsed = 0f;

        targetTransform.localScale = Vector3.zero;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;

            // SmoothStep으로 튕기듯 부드럽게 커지는 연출
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            targetTransform.localScale = Vector3.Lerp(Vector3.zero, defaultScale, smoothT);

            yield return null;
        }

        targetTransform.localScale = defaultScale;

        // 전체 1초 중 남아있는 시간 동안 화면 유지
        float remainingTime = totalDuration - popDuration;
        if (remainingTime > 0f)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        targetObj.SetActive(false);

    }

    private void UpdateStageUI(int stage)
    {
        // 등록된 모든 퍼즐을 비활성화 (이전 퍼즐 잔재 제거)

        _stageClear = false;
        _isTimeDown = false;
        DisableAllStagePrefabs();
        int targetIndex = stage - 1;

        // 유효성 검사: 선택된 인덱스 리스트 및 프리팹 리스트 범위 안인지 확인
        if (targetIndex >= 0 && targetIndex < _selectedStageList.Count)
        {
            int prefabIndex = _selectedStageList[targetIndex];

            if (prefabIndex >= 0 && prefabIndex < stagePrefabList.Count && stagePrefabList[prefabIndex] != null)
            {
                stagePrefabList[prefabIndex].SetActive(true);
                StartTimer();
                Debug.Log($"Stage {stage} (프리팹 인덱스: {prefabIndex}) 활성화");
            }
            else
            {
                Debug.LogError($"[CGameManager] stagePrefabList[{prefabIndex}]가 유효하지 않습니다.");
            }
        }
        else
        {
            Debug.LogError($"[CGameManager] targetIndex({targetIndex})가 생성된 스테이지 순서 범위를 벗어났습니다.");
        }
    }

    private void DisableAllStagePrefabs()
    {
        if (stagePrefabList == null) return;

        foreach (var puzzle in stagePrefabList)
        {
            if (puzzle != null)
            {
                puzzle.SetActive(false);
            }
        }
    }

    /// <summary>
    /// _fixedStage 개수만큼 인덱스를 순서대로 고정하고,
    /// 그 이후 스테이지부터 무작위로 섞어 리스트를 구성하는 함수
    /// </summary>
    public void ChooseStageSequence()
    {
        _selectedStageList.Clear();

        if (stagePrefabList == null || stagePrefabList.Count == 0)
        {
            Debug.LogError("[CGameManager] stagePrefabList가 비어 있습니다!");
            return;
        }

        int actualFixedCount = Mathf.Clamp(_fixedStage, 0, stagePrefabList.Count);

        // 1. 고정 스테이지 순서대로 추가 (0 ~ actualFixedCount - 1)
        for (int i = 0; i < actualFixedCount; i++)
        {
            _selectedStageList.Add(i);
        }

        // 2. 고정 구간 이후의 인덱스들을 셔플용 리스트에 추가
        List<int> randomIndices = new List<int>();
        for (int i = actualFixedCount; i < stagePrefabList.Count; i++)
        {
            randomIndices.Add(i);
        }

        // 3. 남은 인덱스들에 대해 Fisher-Yates 셔플 진행
        for (int i = 0; i < randomIndices.Count; i++)
        {
            int randomIndex = Random.Range(i, randomIndices.Count);

            int temp = randomIndices[i];
            randomIndices[i] = randomIndices[randomIndex];
            randomIndices[randomIndex] = temp;
        }

        // 4. 셔플된 무작위 인덱스들을 최종 리스트에 합치기
        _selectedStageList.AddRange(randomIndices);

        if (stagePrefabList.Count < _maxStage)
        {
            Debug.LogWarning($"[CGameManager] 등록된 프리팹 수({stagePrefabList.Count})가 목표 스테이지 수({_maxStage})보다 적습니다.");
        }

        Debug.Log($"[설정 완료] 고정 스테이지 수: {actualFixedCount}개 | 최종 구성 순서: {string.Join(", ", _selectedStageList)}");
    }
}