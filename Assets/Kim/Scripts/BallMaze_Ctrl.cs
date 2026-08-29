using UnityEngine;
using UnityEngine.UI;

public class BallMaze_Ctrl : MonoBehaviour
{
    [SerializeField] Transform maze;
    [SerializeField] Rigidbody mazeRb;
       
    float targetAngle;
    float currentAngle;

    [SerializeField] float rotateSpeed = 90f;

    [SerializeField] float moveSpeed = 3f;


    private float previousAngle;

    float rotateDir;
    private void Start()
    {

    }

    private void FixedUpdate()
    {
        if (rotateDir == 0)
        {
            return;
        }

        RotateMaze();
    }

    private void OnEnable()
    {

    }

    public void MoveBtnClick(int _num)
    {
        rotateDir = 1 + (-2 * _num);
        
    }

    public void MoveBtnUp()
    {
        Debug.Log("up");
        rotateDir = 0;  
    }
    private void ChangeDirection(float value)
    {
        float currentAngle = value * 360f;

        float deltaAngle = Mathf.DeltaAngle(
            previousAngle,
            currentAngle
        );

        previousAngle = currentAngle;
    }

    void RotateMaze()
    {
        //maze.localRotation = Quaternion.Euler(0f, 0f, value * 360f);
        //maze.localRotation = Quaternion.RotateTowards(maze.localRotation, Quaternion.Euler(0f, 0f, value * 360f),0.5f);
        
        float rotateAmount = rotateDir * rotateSpeed * Time.fixedDeltaTime;

        Quaternion deltaRotation = Quaternion.Euler(0f, 0f, rotateAmount);
        
        mazeRb.MoveRotation(mazeRb.rotation * deltaRotation);
    }
}
