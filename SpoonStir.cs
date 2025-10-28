using UnityEngine;
using System.Collections;

public class SpoonStir : MonoBehaviour
{
    [Header("Settings")]
    public string interactionName = "stir";
    
    [Header("Stirring Settings")]
    public Vector3 stirringPosition; // Position above the pot for stirring
    public float stirringDuration = 2.5f; // How long to stir
    public float stirRadius = 0.3f; // Radius of circular stirring motion
    
    [Header("Audio")]
    public AudioClip stirringSound; // Assign "spoon_mixing.mp3"
    
    private Vector3 originalPosition;
    private bool isStirring = false;
    private bool hasBeenUsed = false;
    private SpriteRenderer spriteRenderer;
    private int originalSortingOrder;

    void Start()
    {
        originalPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalSortingOrder = spriteRenderer.sortingOrder;
        
        // Set stirring position above the pot - ADJUSTED HIGHER
        GameObject pot = GameObject.Find("Pot (idle) Frame 1_0");
        if (pot != null)
        {
            stirringPosition = new Vector3(pot.transform.position.x, pot.transform.position.y + 1.2f, transform.position.z);
        }
        
        Debug.Log($"[SpoonStir] {gameObject.name} ready.");
    }

    void Update()
    {
        // Check for mouse click on spoon
        if (Input.GetMouseButtonDown(0) && !isStirring && !hasBeenUsed)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(mousePos);

            if (hit != null && hit.gameObject == gameObject)
            {
                StartCoroutine(PerformStirring());
            }
        }
    }

    private IEnumerator PerformStirring()
    {
        isStirring = true;
        hasBeenUsed = true;
        
        Debug.Log("[SpoonStir] Starting stirring sequence!");

        // Bring spoon to front so it appears above the pot
        spriteRenderer.sortingOrder = 10;

        // Move spoon to stirring position above pot
        float moveTime = 0.5f;
        Vector3 startPos = transform.position;
        
        // Move to stirring position
        float elapsedTime = 0;
        while (elapsedTime < moveTime)
        {
            transform.position = Vector3.Lerp(startPos, stirringPosition, elapsedTime / moveTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = stirringPosition;

        // Play stirring sound
        if (stirringSound != null)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.PlayOneShot(stirringSound);
        }

        // Perform circular stirring animation
        float stirTime = 0;
        Vector3 stirCenter = stirringPosition;
        
        while (stirTime < stirringDuration)
        {
            // Circular stirring motion
            float angle = (stirTime / stirringDuration) * 8f * Mathf.PI; // 4 full circles
            float x = Mathf.Cos(angle) * stirRadius;
            float y = Mathf.Sin(angle) * stirRadius * 0.5f; // Flatten the circle a bit
            
            transform.position = stirCenter + new Vector3(x, y, 0);
            
            stirTime += Time.deltaTime;
            yield return null;
        }

        // Return to original position
        elapsedTime = 0;
        while (elapsedTime < moveTime)
        {
            transform.position = Vector3.Lerp(stirringPosition, originalPosition, elapsedTime / moveTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPosition;

        // Reset sorting order
        spriteRenderer.sortingOrder = originalSortingOrder;

        // INSTRUCTION UPDATE: Move to next step (Step 6: "Use the ladle to pour...")
        if (InstructionManager.Instance != null)
        {
            InstructionManager.Instance.NextStep();
            Debug.Log("[SpoonStir] Moving to next instruction step!");
        }

        Debug.Log("[SpoonStir] Stirring complete!");
        isStirring = false;
    }

    // Allow the spoon to be used again (if needed for testing)
    [ContextMenu("Reset Spoon")]
    public void ResetSpoon()
    {
        hasBeenUsed = false;
        isStirring = false;
        transform.position = originalPosition;
        spriteRenderer.sortingOrder = originalSortingOrder;
    }
}