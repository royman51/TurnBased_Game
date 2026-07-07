using UnityEngine;

public class CameraCursorLookRotation : MonoBehaviour
{
    [Header("기본 로테이션")]
    [SerializeField] private Vector3 centerRotation = new Vector3(5.012f, 0f, 0f);

    [Header("왼쪽으로 볼 때 로테이션")]
    [SerializeField] private Vector3 leftRotation = new Vector3(5.000f, -2.007f, -0.175f);

    [Header("오른쪽으로 볼 때 로테이션")]
    [SerializeField] private Vector3 rightRotation = new Vector3(4.977f, 3.391f, 0.294f);

    [Header("위쪽으로 볼 때 로테이션")]
    [SerializeField] private Vector3 upRotation = new Vector3(4.709f, 0f, 0f);

    [Header("아래쪽으로 볼 때 로테이션")]
    [SerializeField] private Vector3 downRotation = new Vector3(5.309f, 0f, 0f);

    [Header("마우스 감도")]
    [SerializeField] private float mouseSensitivityX = 0.01f;
    [SerializeField] private float mouseSensitivityY = 0.007f;

    [Header("회전 부드러움")]
    [SerializeField] private float rotationSmoothSpeed = 8f;

    [Header("원래 방향으로 돌아오는 속도")]
    [SerializeField] private float returnSpeed = 5f;

    [Header("방향 반전")]
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;

    private Vector3 lastMousePosition;

    private float targetHorizontal;
    private float targetVertical;

    private float currentHorizontal;
    private float currentVertical;

    private void Start()
    {
        lastMousePosition = Input.mousePosition;
        transform.localRotation = Quaternion.Euler(centerRotation);
    }

    private void Update()
    {
        Vector3 currentMousePosition = Input.mousePosition;

        float mouseDeltaX = currentMousePosition.x - lastMousePosition.x;
        float mouseDeltaY = currentMousePosition.y - lastMousePosition.y;

        float xDirection = invertX ? -1f : 1f;
        float yDirection = invertY ? -1f : 1f;

        if (Mathf.Abs(mouseDeltaX) > 0.01f)
        {
            targetHorizontal += mouseDeltaX * mouseSensitivityX * xDirection;
            targetHorizontal = Mathf.Clamp(targetHorizontal, -1f, 1f);
        }
        else
        {
            targetHorizontal = Mathf.Lerp(
                targetHorizontal,
                0f,
                Time.deltaTime * returnSpeed
            );
        }

        if (Mathf.Abs(mouseDeltaY) > 0.01f)
        {
            targetVertical += mouseDeltaY * mouseSensitivityY * yDirection;
            targetVertical = Mathf.Clamp(targetVertical, -1f, 1f);
        }
        else
        {
            targetVertical = Mathf.Lerp(
                targetVertical,
                0f,
                Time.deltaTime * returnSpeed
            );
        }

        currentHorizontal = Mathf.Lerp(
            currentHorizontal,
            targetHorizontal,
            Time.deltaTime * rotationSmoothSpeed
        );

        currentVertical = Mathf.Lerp(
            currentVertical,
            targetVertical,
            Time.deltaTime * rotationSmoothSpeed
        );

        Vector3 horizontalRotation = centerRotation;

        if (currentHorizontal < 0f)
        {
            horizontalRotation = Vector3.Lerp(
                centerRotation,
                leftRotation,
                Mathf.Abs(currentHorizontal)
            );
        }
        else if (currentHorizontal > 0f)
        {
            horizontalRotation = Vector3.Lerp(
                centerRotation,
                rightRotation,
                currentHorizontal
            );
        }

        Vector3 verticalRotation = centerRotation;

        if (currentVertical > 0f)
        {
            verticalRotation = Vector3.Lerp(
                centerRotation,
                upRotation,
                currentVertical
            );
        }
        else if (currentVertical < 0f)
        {
            verticalRotation = Vector3.Lerp(
                centerRotation,
                downRotation,
                Mathf.Abs(currentVertical)
            );
        }

        Vector3 finalRotation = centerRotation;
        finalRotation += horizontalRotation - centerRotation;
        finalRotation += verticalRotation - centerRotation;

        Quaternion targetRotation = Quaternion.Euler(finalRotation);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            Time.deltaTime * rotationSmoothSpeed
        );

        lastMousePosition = currentMousePosition;
    }
}