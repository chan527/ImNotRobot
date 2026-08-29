using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CRobotTextCheckCanvasManager : MonoBehaviour
{
    [System.Serializable]
    public struct SentenceData
    {
        [TextArea(1, 3)]
        public string sentence; // 문장 텍스트
        public bool isCorrect;  // 정답 문장 여부 (체크해야 하는 문장이면 true)
    }

    [Header("UI References")]
    [SerializeField] private Transform itemContainer;          // ScrollView의 Content (생성된 항목들이 들어갈 위치)
    [SerializeField] private GameObject captchaBoxPrefab;      // CaptchaBox 프리팹 (CRobotTextCheck 붙은 프리팹 1개)
    [SerializeField] private Button verifyButton;              // 하단 VERIFY 버튼

    [Header("Sentence Data + correct check")]
    [SerializeField] private List<SentenceData> sentenceList = new List<SentenceData>();

    // 자동으로 생성된 CaptchaBox들을 담아둘 내부 리스트 (인스펙터에 안 노출됨)
    private List<CRobotTextCheck> _spawnedBoxes = new List<CRobotTextCheck>();

    private void Awake()
    {
        if (verifyButton != null)
        {
            verifyButton.onClick.AddListener(OnVerifyClicked);
        }
    }

    private void OnEnable()
    {
        InitStage();
    }

    public void InitStage()
    {
        // 1. 기존에 생성되어 있던 자식 항목들 제거
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }
        _spawnedBoxes.Clear();

        // 2. sentenceList 개수(20개)만큼 CaptchaBox 프리팹을 자동으로 생성
        foreach (var data in sentenceList)
        {
            GameObject obj = Instantiate(captchaBoxPrefab, itemContainer);
            CRobotTextCheck box = obj.GetComponent<CRobotTextCheck>();

            if (box != null)
            {
                // 텍스트, 정답 여부, 오답(X)시 실행할 OnStageFailed 콜백 전달
                box.SetSentenceText(data.sentence, data.isCorrect, OnStageFailed);
                _spawnedBoxes.Add(box);
            }
        }
    }

    /// <summary>
    /// 오답(X)을 눌렀을 때 즉시 호출되는 스테이지 재시작 함수
    /// </summary>
    private void OnStageFailed()
    {
        Debug.Log("[Manager] 오답 선택! 스테이지를 재시작합니다.");

        if (CGameManager.Instance != null)
        {
            CGameManager.Instance.StageFailed(); // 게임 매니저의 재시작/실패 함수
        }
        else
        {
            InitStage(); // CGameManager가 없으면 캔버스 자체 재초기화
        }
    }

    /// <summary>
    /// VERIFY 버튼 클릭 시 검증 (모든 정답 문장이 체크되었는지 확인)
    /// </summary>
    private void OnVerifyClicked()
    {
        int count = Mathf.Min(_spawnedBoxes.Count, sentenceList.Count);
        bool isAllTargetChecked = true;

        for (int i = 0; i < count; i++)
        {
            // 정답 문장인데 아직 체크를 안 한 항목이 있는지 확인
            if (sentenceList[i].isCorrect && !_spawnedBoxes[i].IsChecked)
            {
                isAllTargetChecked = false;
                break;
            }
        }

        if (isAllTargetChecked)
        {
            Debug.Log("[Manager] 모든 정답 문장 체크 완료! 스테이지 클리어");
            if (CGameManager.Instance != null)
            {
                CGameManager.Instance.StageClear();
            }
        }
        else
        {
            Debug.Log("[Manager] 아직 찾아야 할 정답 문장이 남아있습니다!");
        }
    }
}