using UnityEngine;
using UnityEngine.UI;

public class HideButton_Ctrl : MonoBehaviour
{
    [SerializeField] Button ok_Btn;
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
        
    }
}
