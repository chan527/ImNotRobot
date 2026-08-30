using UnityEngine;
using UnityEngine.UI;

public class HideButton_Ctrl : MonoBehaviour
{
    [SerializeField] Button ok_Btn;
    [SerializeField] Image arrow_Img;
    [SerializeField] RectTransform arrow_RectTransform;
    void Start()
    {
        ok_Btn.onClick.AddListener(() =>
        {
            CGameManager.Instance.StageClear();
        });
    }

    // Update is called once per frame
    void Update()
    {
        PointBtn();
    }

    void PointBtn()
    {

        if (ok_Btn == null)
            return;

        Vector2 direction =
            ok_Btn.transform.position - arrow_RectTransform.position;

        // 기본 화살표가 위쪽(↑)을 보고 있으므로
        // Vector2.up 기준으로 목표 방향까지의 각도를 계산
        float angle =
            Vector2.SignedAngle(Vector2.up, direction);

        arrow_RectTransform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }
}
