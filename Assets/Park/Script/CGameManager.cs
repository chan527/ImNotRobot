using System.Collections.Generic;
using UnityEngine;

public class CGameManager : MonoBehaviour
{
    // 외부에서 CMainGameManager.Instance 로 접근
    public static CGameManager Instance { get; private set; }
    [SerializeField] private int _currentStage = 1;
    [SerializeField] private int _maxStage = 5;

    [Header("Stage Inspector")]
    [SerializeField]public List<GameObject> stagePrefabList = new List<GameObject>(); // 프리팹 방식일 경우

    
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
        ChooseStageSequence();
        UpdateStageUI(_currentStage);
    }

    /// <summary>
    /// 팀원 퍼즐 성공 시 호출하는 함수
    /// </summary>
    public void StageSuccess()
    {
        Debug.Log($"[Stage {_currentStage}] 스테이지 클리어!");

        if (_currentStage < _maxStage)
        {
            _currentStage++;
            UpdateStageUI(_currentStage);
            // TODO: 활성화/비활성화 또는 씬 로드 로직 추가 예정
        }
        else
        {
            Debug.Log("모든 스테이지 올 클리어!");
        }
    }
    public void OnStageFailed()
    {
        Debug.Log($"[Stage {_currentStage}] 실패 - 셔플 재구성 후 1스테이지로 리셋");

        _currentStage = 1;
        ChooseStageSequence(); // 실패 시 무작위 순서 재구성
        UpdateStageUI(_currentStage);
        // TODO: 게임 오버 팝업 연동 또는 씬 재시도 로직 추가 예정
    }

    private void UpdateStageUI(int stage)
    {
        //  등록된 모든 퍼즐을 비활성화 (이전 퍼즐 잔재 제거)
        DisableAllStagePrefabs();

        int targetIndex = stage - 1;

        // 유효성 검사 후 해당 순서의 퍼즐 오브젝트 활성화
        if (targetIndex >= 0 && targetIndex < _maxStage)
        {
            int PrefabIndex = _selectedStageList[targetIndex];

            stagePrefabList[PrefabIndex].SetActive(true);
            Debug.Log($"Stage{stage} 스테이지 활성화");

        }
    }

    /// <summary>
    /// Inspector에 등록된 모든 퍼즐 오브젝트를 비활성화하는 함수
    /// </summary>
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
    public void ChooseStageSequence()
    {
        _selectedStageList.Clear();

        if (stagePrefabList == null || stagePrefabList.Count == 0)
        {
            Debug.LogError("[CGameManager] stagePrefabList가 비어 있습니다!");
            return;
        }

        // 1. 등록된 프리팹 개수만큼 순서대로 인덱스 추가 (0, 1, 2, ...)
        for (int i = 0; i < stagePrefabList.Count; i++)
        {
            _selectedStageList.Add(i);
        }

        // 2. Fisher-Yates 셔플 알고리즘으로 무작위 섞기
        for (int i = 0; i < _selectedStageList.Count; i++)
        {
            int randomIndex = Random.Range(i, _selectedStageList.Count);

            int temp = _selectedStageList[i];
            _selectedStageList[i] = _selectedStageList[randomIndex];
            _selectedStageList[randomIndex] = temp;
        }

        // 3. 프리팹 수와 _maxStage 수 비교 경고 로그
        if (stagePrefabList.Count < _maxStage)
        {
            Debug.LogWarning($"[CGameManager] 등록된 프리팹 수({stagePrefabList.Count})가 목표 스테이지 수({_maxStage})보다 적습니다.");
        }

        Debug.Log($"무작위 셔플된 인덱스 목록: {string.Join(", ", _selectedStageList)}");
    }
}


