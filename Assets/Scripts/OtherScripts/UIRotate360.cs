using UnityEngine;

public class UIRotate360 : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 180f;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        rectTransform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
    }
}