using UnityEngine;
using TMPro;
using System;

public class FloatingText : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float fadeSpeed = 3f;
    public float lifeTime = 0.8f;

    [Header("Spawn Position Settings")]
    public float verticalStartOffset = 1.0f;   // Výška (Y)
    public float horizontalStartOffset = 0.0f; // Posun do strany (X) - Zkus sem dát tøeba 0.5
    public Vector2 randomSpread = new Vector2(0.5f, 0.2f); // Náhodný rozptyl

    private TMP_Text textMesh;
    private Color textColor;
    private float timer;
    private Vector3 moveDirection;

    void Awake()
    {
        textMesh = GetComponent<TMP_Text>();
        moveDirection = Vector3.up;
    }

    public void Setup(int damage, bool isCrit)
    {
        textMesh.text = damage.ToString();

        if (isCrit)
        {
            textMesh.fontSize *= 1.3f;
            textMesh.color = Color.yellow;
            textMesh.text += "";
            moveSpeed *= 1.2f;
            // Crit letí šikmo
            moveDirection = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 1f, 0f).normalized;
        }
        else
        {
            textMesh.color = Color.red;
        }

        textColor = textMesh.color;

        // --- VÝPOÈET POZICE ---
        // 1. Základní posun (H + V)
        Vector3 fixedOffset = new Vector3(horizontalStartOffset, verticalStartOffset, 0f);

        // 2. Náhodný rozptyl
        Vector3 spreadOffset = new Vector3(
            UnityEngine.Random.Range(-randomSpread.x, randomSpread.x),
            UnityEngine.Random.Range(-randomSpread.y, randomSpread.y),
            0f
        );

        // Aplikace na pozici
        transform.position += fixedOffset + spreadOffset;
    }

    void Update()
    {
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer > lifeTime * 0.5f)
        {
            float fadeAmount = fadeSpeed * Time.deltaTime;
            textColor.a = Mathf.Max(0f, textColor.a - fadeAmount);
            textMesh.color = textColor;
        }

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}