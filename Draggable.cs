using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using Yarn.Unity;

[RequireComponent(typeof(CanvasGroup))]
public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Drag Settings")]
    public string ingredientName;
    public bool isSticky = false;
    public bool becomesDropZoneOnStick = false;
    public DropTarget dropZoneToEnable;

    [Header("Animation Settings")]
    public bool selfManagedAnimations = true;
    public float dropSuccessDuration = 1f;

    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalPosition;

    private Animator animator;
    private bool isLocked = false;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        animator = GetComponent<Animator>();

        originalPosition = transform.position;

        if (dropZoneToEnable != null)
            dropZoneToEnable.gameObject.SetActive(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked) return;

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked) return;

        // FIXED: Removed scaleFactor to prevent distortion
        rectTransform.anchoredPosition += eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLocked) return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // If no valid drop, fallback to reset
        ReturnToOriginalPosition();
        PlayDropAnimation(false);
    }

    public void OnValidDrop(Vector3 dropWorldPosition, RectTransform lockTarget)
    {
        StartCoroutine(HandleDropSuccess(dropWorldPosition, lockTarget));
    }

    public void OnInvalidDrop()
    {
        ReturnToOriginalPosition();
        PlayDropAnimation(false);
    }

    private IEnumerator HandleDropSuccess(Vector3 dropWorldPosition, RectTransform lockTarget)
    {
        isLocked = true;
        transform.position = dropWorldPosition;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        PlayDropAnimation(true);

        yield return new WaitForSeconds(dropSuccessDuration);

        if (isSticky)
        {
            LockToDropTarget(lockTarget);
        }
        else
        {
            ReturnToOriginalPosition();
        }

        isLocked = false;
    }

    private void LockToDropTarget(RectTransform lockTarget)
    {
        rectTransform.SetParent(lockTarget);
        rectTransform.anchoredPosition = Vector2.zero;

        if (becomesDropZoneOnStick && dropZoneToEnable != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            foreach (var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>())
            {
                graphic.raycastTarget = false;
            }
            dropZoneToEnable.transform.SetParent(transform.parent);
            dropZoneToEnable.gameObject.SetActive(true);
            dropZoneToEnable.Reactivate();
        }

        if (selfManagedAnimations)
                PlayAnimation("Idle");
    }

    [YarnCommand("BecomeDraggable")]
    public void BecomeDraggable()
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        originalPosition = transform.position;
        foreach (var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>())
        {
            graphic.raycastTarget = true;
        }
        dropZoneToEnable.gameObject.SetActive(false);
        dropZoneToEnable.Deactivate();
        dropZoneToEnable.transform.SetParent(gameObject.transform);
    }

    private void ReturnToOriginalPosition()
    {
        transform.position = originalPosition;
        if (selfManagedAnimations)
            PlayAnimation("Idle");
    }

    private void PlayDropAnimation(bool success)
    {
        if (!selfManagedAnimations || animator == null) return;

        //string trigger = success ? "DropSuccess" : "DropFail";
        string trigger = "Stick";
        if (success)
            trigger = "DropSuccess";
        
        animator.SetTrigger(trigger);
    }

    private void PlayAnimation(string animName)
    {
        if (animator != null)
            animator.Play(animName);
    }
}