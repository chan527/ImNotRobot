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

    [Header("Stage Inspector")]
    [Tooltip("앞쪽 인덱스는 _fixedStage 수만큼 고정 배치되고, 그 이후 인덱스부터 무작위로 등장합니다.")]
    [SerializeField] public List<GameObject> stagePrefabList = new List<GameObject>(); // 프리팹 방식일 경우

    [Header("Timer Settings")]
    [SerializeField] private float stageLimitTime = 60f; // 스테이지당 제한시간 (초 단위)
    private float _currentTimer = 60f;
    private bool _isTimerRunning = true;

    private List<int> _selectedStageList = new List<int>();
    bool _isTimeCustomed = false;
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
        if (!_isTimeCustomed)
            _currentTimer = stageLimitTime;

        _isTimerRunning = true;
    }
    public void TimeDown(float stageCustomTime)
    {
        _currentTimer = stageCustomTime;
        _isTimeCustomed = true;
    }


    public void StageClear()
    {
        Debug.Log($"[Stage {_currentStage}] 스테이지 클리어!");

        if (_currentStage < _maxStage)
        {
            _currentStage++;
            UpdateStageUI(_currentStage);
        }
        else
        {
            Debug.Log("모든 스테이지 올 클리어!");
        }
    }

    public void StageFailed()
    {
        Debug.Log($"[Stage {_currentStage}] 실패 - 1스테이지로 리셋");

        _currentStage = 1;
        ChooseStageSequence(); // 실패 시 고정 구간 이후 랜덤 순서 재구성
        UpdateStageUI(_currentStage);
    }

    private void UpdateStageUI(int stage)
    {
        // 등록된 모든 퍼즐을 비활성화 (이전 퍼즐 잔재 제거)
        DisableAllStagePrefabs();
        _isTimeCustomed = false;
        int targetIndex = stage - 1;

        // 유효성 검사: 선택된 인덱스 리스트 및 프리팹 리스트 범위 안인지 확인
        if (targetIndex >= 0 && targetIndex < _selectedStageList.Count)
        {
            int prefabIndex = _selectedStageList[targetIndex];

            if (prefabIndex >= 0 && prefabIndex < stagePrefabList.Count && stagePrefabList[prefabIndex] != null)
            {
                StartTimer();
                stagePrefabList[prefabIndex].SetActive(true);
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

        // 음수 입력 방지 및 전체 프리팹 개수를 초과하지 않도록 보장
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

        // 4. 셔플된 무작위 인덱스들을 최종 리스트 뒤에 합치기
        _selectedStageList.AddRange(randomIndices);

        // 경고 메시지 처리
        if (stagePrefabList.Count < _maxStage)
        {
            Debug.LogWarning($"[CGameManager] 등록된 프리팹 수({stagePrefabList.Count})가 목표 스테이지 수({_maxStage})보다 적습니다.");
        }

        Debug.Log($"[설정 완료] 고정 스테이지 수: {actualFixedCount}개 | 최종 구성 순서: {string.Join(", ", _selectedStageList)}");
    }
}