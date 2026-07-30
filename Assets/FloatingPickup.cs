using UnityEngine;
using System.Collections;

public class FloatingPickup : MonoBehaviour
{
    [Header("Intro Animation")]
    [SerializeField] private float startHeight = 2f;
    [SerializeField] private Vector3 startScale = Vector3.zero;
    [SerializeField] private float introDuration = 1f;

    [Header("Idle Bob")]
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 60f;

    private Vector3 targetPosition;
    private Vector3 targetScale;
    private float bobTimer;

    private void Awake()
    {
        targetPosition = transform.position;
        targetScale = transform.localScale;

        transform.position = targetPosition + Vector3.up * startHeight;
        transform.localScale = startScale;

        StartCoroutine(IntroAnimation());
    }

    private IEnumerator IntroAnimation()
    {
        float timer = 0f;

        Vector3 startPos = transform.position;
        Vector3 endPos = targetPosition;

        while (timer < introDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / introDuration);

            // Smooth easing
            float eased = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPos, endPos, eased);
            transform.localScale = Vector3.Lerp(startScale, targetScale, eased);

            yield return null;
        }

        transform.position = targetPosition;
        transform.localScale = targetScale;
    }

    private void Update()
    {
        // Spin
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // Bob
        bobTimer += Time.deltaTime * bobSpeed;

        Vector3 pos = targetPosition;
        pos.y += Mathf.Sin(bobTimer) * bobHeight;

        transform.position = pos;
    }
}
