using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CasinoDiceRoller : MonoBehaviour
{
    [Header("Dice")]
    [SerializeField] private Rigidbody[] dice = new Rigidbody[2];

    [Header("Face Rotations")]
    // Index 0 = 1, Index 5 = 6
    [SerializeField] private Vector3[] faceUpRotations = new Vector3[6];

    [Header("Roll Animation")]
    [SerializeField] private float rollDuration = 2.5f;
    [SerializeField] private float rotationSpeed = 850f;
    [SerializeField] private float bounceHeight = 0.65f;
    [SerializeField] private float bounceSpeed = 5f;

    [Header("Final Alignment")]
    [SerializeField] private float alignmentDuration = 0.65f;
    [SerializeField] private float landingDuration = 0.3f;

    [Header("Events")]
    public UnityEvent<int, int> OnDiceStopped;
    public UnityEvent<int> OnRollCompleted;

    private bool isRolling;

    public bool IsRolling => isRolling;

    private Vector3[] basePositions = new Vector3[2];
    private Vector3[] spinDirections = new Vector3[2];

    // Rolls to a total; the two faces are picked at random (each phone can show a different pair).
    public void Roll(int targetTotal)
    {
        if (isRolling || targetTotal < 2 || targetTotal > 12)
            return;

        // Random valid combination.
        int first = Random.Range(
            Mathf.Max(1, targetTotal - 6),
            Mathf.Min(6, targetTotal - 1) + 1
        );

        StartCoroutine(RollRoutine(first, targetTotal - first));
    }

    // Rolls to these exact faces (the server's dice), so every player sees the same result.
    public void Roll(int face1, int face2)
    {
        if (isRolling || face1 < 1 || face1 > 6 || face2 < 1 || face2 > 6)
            return;

        StartCoroutine(RollRoutine(face1, face2));
    }

    private IEnumerator RollRoutine(int first, int second)
    {
        isRolling = true;

        int total = first + second;

        int[] results = { first, second };

        Quaternion[] targetRotations = new Quaternion[2];

        for (int i = 0; i < 2; i++)
        {
            Rigidbody rb = dice[i];

            // Disable physics movement.
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;

            basePositions[i] = rb.transform.localPosition;

            // Random rotation direction.
            spinDirections[i] = Random.onUnitSphere;

            targetRotations[i] = Quaternion.Euler(
                faceUpRotations[results[i] - 1]
            );
        }

        float elapsed = 0f;

        // PHASE 1: Full bouncing and random spinning.
        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;

            AnimateDice(
                1f,
                spinDirections,
                rotationSpeed,
                null,
                0f
            );

            yield return null;
        }

        // PHASE 2: Align while continuing full bouncing.
        Quaternion[] startRotations = new Quaternion[2];

        for (int i = 0; i < 2; i++)
            startRotations[i] = dice[i].transform.localRotation;

        elapsed = 0f;

        while (elapsed < alignmentDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / alignmentDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < 2; i++)
            {
                Transform tr = dice[i].transform;

                // Keep the full bounce.
                SetBouncePosition(i, 1f);

                // Smoothly align the desired face.
                tr.localRotation = Quaternion.Slerp(
                    startRotations[i],
                    targetRotations[i],
                    smoothT
                );
            }

            yield return null;
        }

        // PHASE 3: Correct face achieved. Land naturally.
        for (int i = 0; i < 2; i++)
            dice[i].transform.localRotation = targetRotations[i];

        elapsed = 0f;

        while (elapsed < landingDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / landingDuration);
            float bounceMultiplier = 1f - Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < 2; i++)
                SetBouncePosition(i, bounceMultiplier);

            yield return null;
        }

        // Exact final position and rotation.
        for (int i = 0; i < 2; i++)
        {
            dice[i].transform.localPosition = basePositions[i];
            dice[i].transform.localRotation = targetRotations[i];
        }

        OnDiceStopped?.Invoke(first, second);
        OnRollCompleted?.Invoke(total);

        isRolling = false;
    }

    private void AnimateDice(
        float bounceMultiplier,
        Vector3[] directions,
        float spinSpeed,
        Quaternion[] targets,
        float alignment)
    {
        for (int i = 0; i < 2; i++)
        {
            SetBouncePosition(i, bounceMultiplier);

            dice[i].transform.Rotate(
                directions[i] * spinSpeed * Time.deltaTime,
                Space.Self
            );
        }
    }

    private void SetBouncePosition(int index, float multiplier)
    {
        Transform tr = dice[index].transform;

        float bounce = Mathf.Abs(
            Mathf.Sin(Time.time * bounceSpeed)
        ) * bounceHeight * multiplier;

        // X and Z remain exactly unchanged.
        tr.localPosition = new Vector3(
            basePositions[index].x,
            basePositions[index].y + bounce,
            basePositions[index].z
        );
    }
}