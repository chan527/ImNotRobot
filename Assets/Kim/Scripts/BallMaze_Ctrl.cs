using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class BallMaze_Ctrl : MonoBehaviour
{
    [SerializeField] Slider ctrl_Slider;
    [SerializeField] GameObject maze;
    [SerializeField] Rigidbody rb;

    [SerializeField] TextMeshProUGUI remainTime_Text;
    float targetAngle;
    float currentAngle;

    [SerializeField] float rotateSpeed = 90f;

    [SerializeField] float moveSpeed = 3f;

    private Vector3 moveDir = Vector3.down;
    private float previousAngle;

    float remainTime = 300f;
    private void Start()
    {
        previousAngle = ctrl_Slider.value * 360f;

        ctrl_Slider.onValueChanged.AddListener(ChangeDirection);
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveDir * moveSpeed;
        remainTime = remainTime - Time.fixedDeltaTime;

        remainTime_Text.text = remainTime.ToString("F1");
        if(remainTime <= 0)
        {
            //종료 호출
        }
    }

    private void OnEnable()
    {
        remainTime = 300f;
        remainTime_Text.text = remainTime.ToString("F1");
    }
    private void ChangeDirection(float value)
    {
        float currentAngle = value * 360f;

        float deltaAngle = Mathf.DeltaAngle(
            previousAngle,
            currentAngle
        );

        moveDir =
            Quaternion.AngleAxis(deltaAngle, Vector3.forward)
            * moveDir;

        moveDir.Normalize();

        previousAngle = currentAngle;
    }
}
