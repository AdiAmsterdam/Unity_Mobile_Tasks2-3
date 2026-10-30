using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MobileInputController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference touchPress;
    [SerializeField] private InputActionReference touchPosition;
    [SerializeField] private InputActionReference secondaryTouchPress;
    [SerializeField] private InputActionReference secondaryTouchPosition;

    [Header("Gesture Settings")]
    [SerializeField] private float swipeThreshold = 50f;
    [SerializeField] private float gestureThreshold = 20f;
    [SerializeField] private float pinchMovementThreshold = 0.1f;
    [SerializeField] private float rotationMovementThreshold = 0.1f;
    [SerializeField] private float doubleTapTime = 0.35f;
    [SerializeField] private float doubleTapDistance = 40f;

    public event Action SwipeLeft;
    public event Action SwipeRight;
    public event Action<Vector2> SwipeUp;
    public event Action<Vector2> PrimaryTouchStarted;
    public event Action<Vector2> TouchMoved;
    public event Action PrimaryTouchEnded;
    public event Action<Vector2> DoubleTap;
    public event Action<float> Pinch;
    public event Action<float> Rotate;

    private Vector2 touchStartPosition;
    private Vector2 previousTouchPosition;
    private Vector2 previousSecondaryTouchPosition;
    private bool gestureDecided;
    private bool isHorizontalSwipe;
    private bool twoFingerGestureInProgress;
    private bool ignorePrimaryTouch;
    private float lastTapTime = -1f;
    private Vector2 lastTapPosition;

    private void OnEnable()
    {
        if (!ReferencesAreAssigned())
            return;

        touchPress.action.started += OnTouchStarted;
        touchPress.action.canceled += OnTouchEnded;
        touchPosition.action.performed += OnTouchMoved;
        secondaryTouchPress.action.started += OnSecondaryTouchStarted;
        secondaryTouchPress.action.canceled += OnSecondaryTouchEnded;

        touchPress.action.Enable();
        touchPosition.action.Enable();
        secondaryTouchPress.action.Enable();
        secondaryTouchPosition.action.Enable();
    }

    private void OnDisable()
    {
        if (!ReferencesAreAssigned())
            return;

        touchPress.action.started -= OnTouchStarted;
        touchPress.action.canceled -= OnTouchEnded;
        touchPosition.action.performed -= OnTouchMoved;
        secondaryTouchPress.action.started -= OnSecondaryTouchStarted;
        secondaryTouchPress.action.canceled -= OnSecondaryTouchEnded;

        touchPress.action.Disable();
        touchPosition.action.Disable();
        secondaryTouchPress.action.Disable();
        secondaryTouchPosition.action.Disable();

        twoFingerGestureInProgress = false;
        ignorePrimaryTouch = false;
    }

    private void Update()
    {
        if (!touchPress || !touchPosition || !secondaryTouchPress || !secondaryTouchPosition || ignorePrimaryTouch || Time.timeScale == 0f) return;
        if (!AreTwoFingersTouching())
        {
            if (touchPress.action.IsPressed())
                previousTouchPosition = touchPosition.action.ReadValue<Vector2>();

            return;
        }

        Vector2 currentTouchPosition = touchPosition.action.ReadValue<Vector2>();
        Vector2 currentSecondaryPosition = secondaryTouchPosition.action.ReadValue<Vector2>();
        Vector2 primaryMovement = currentTouchPosition - previousTouchPosition;
        Vector2 secondaryMovement = currentSecondaryPosition - previousSecondaryTouchPosition;

        bool primaryMoved = primaryMovement.magnitude > pinchMovementThreshold;
        bool secondaryMoved = secondaryMovement.magnitude > pinchMovementThreshold;

        if (primaryMoved && secondaryMoved)
        {
            float previousDistance = Vector2.Distance(previousTouchPosition, previousSecondaryTouchPosition);
            float currentDistance = Vector2.Distance(currentTouchPosition, currentSecondaryPosition);
            Pinch?.Invoke(currentDistance - previousDistance);
        }
        else if (!primaryMoved &&
                 Mathf.Abs(secondaryMovement.x) > rotationMovementThreshold)
        {
            Rotate?.Invoke(secondaryMovement.x);
        }

        previousTouchPosition = currentTouchPosition;
        previousSecondaryTouchPosition = currentSecondaryPosition;
    }

    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        touchStartPosition = touchPosition.action.ReadValue<Vector2>();
        previousTouchPosition = touchStartPosition;
        ignorePrimaryTouch = Time.timeScale == 0f || IsJoystickTouch(touchStartPosition);

        if (ignorePrimaryTouch)
        {
            twoFingerGestureInProgress = true;
            return;
        }

        gestureDecided = false;
        isHorizontalSwipe = false;
        twoFingerGestureInProgress = secondaryTouchPress.action.IsPressed();

        if (twoFingerGestureInProgress)
            previousSecondaryTouchPosition = secondaryTouchPosition.action.ReadValue<Vector2>();

        PrimaryTouchStarted?.Invoke(touchStartPosition);
    }

    private void OnTouchMoved(InputAction.CallbackContext context)
    {
        if (ignorePrimaryTouch || !touchPress.action.IsPressed() || Time.timeScale == 0f || AreTwoFingersTouching())
            return;

        Vector2 currentPosition = context.ReadValue<Vector2>();
        Vector2 movementFromStart = currentPosition - touchStartPosition;
        Vector2 movement = currentPosition - previousTouchPosition;
        previousTouchPosition = currentPosition;

        if (!gestureDecided && movementFromStart.magnitude >= gestureThreshold)
        {
            gestureDecided = true;
            isHorizontalSwipe = Mathf.Abs(movementFromStart.x) > Mathf.Abs(movementFromStart.y);
        }

        TouchMoved?.Invoke(movement);
    }

    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        if (context.control == null || !context.control.device.added)
        {
            ignorePrimaryTouch = false;
            twoFingerGestureInProgress = false;
            PrimaryTouchEnded?.Invoke();
            return;
        }
        if (ignorePrimaryTouch)
        {
            ignorePrimaryTouch = false;
            twoFingerGestureInProgress = false;
            PrimaryTouchEnded?.Invoke();
            return;
        }

        Vector2 touchEndPosition = touchPosition.action.ReadValue<Vector2>();
        Vector2 totalMovement = touchEndPosition - touchStartPosition;

        if (!twoFingerGestureInProgress &&
            isHorizontalSwipe &&
            Mathf.Abs(totalMovement.x) >= swipeThreshold)
        {
            if (totalMovement.x > 0f)
                SwipeRight?.Invoke();
            else
                SwipeLeft?.Invoke();
        }
        else if (!twoFingerGestureInProgress &&
                 totalMovement.y >= swipeThreshold &&
                 totalMovement.y > Mathf.Abs(totalMovement.x))
        {
            SwipeUp?.Invoke(touchEndPosition);
        }

        if (!twoFingerGestureInProgress && totalMovement.magnitude <= gestureThreshold)
            CheckForDoubleTap(touchEndPosition);

        PrimaryTouchEnded?.Invoke();
        twoFingerGestureInProgress = false;
        ignorePrimaryTouch = false;
    }

    private void OnSecondaryTouchStarted(InputAction.CallbackContext context)
    {
        if (ignorePrimaryTouch || !touchPress.action.IsPressed())
            return;

        twoFingerGestureInProgress = true;
        previousTouchPosition = touchPosition.action.ReadValue<Vector2>();
        previousSecondaryTouchPosition = secondaryTouchPosition.action.ReadValue<Vector2>();
    }

    private void OnSecondaryTouchEnded(InputAction.CallbackContext context)
    {
    }

    private bool IsJoystickTouch(Vector2 position)
    {
        // All menu/card/joystick touches must be excluded from model gestures.
        if (EventSystem.current == null) return false;
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
        return hits.Count > 0;
    }

    private void CheckForDoubleTap(Vector2 tapPosition)
    {
        float now = Time.unscaledTime;
        bool isSecondTap =
            lastTapTime >= 0f &&
            now - lastTapTime <= doubleTapTime &&
            Vector2.Distance(tapPosition, lastTapPosition) <= doubleTapDistance;

        if (isSecondTap)
        {
            DoubleTap?.Invoke(tapPosition);
            lastTapTime = -1f;
            return;
        }

        lastTapTime = now;
        lastTapPosition = tapPosition;
    }

    private bool AreTwoFingersTouching()
    {
        return touchPress.action.IsPressed() &&
               secondaryTouchPress.action.IsPressed();
    }

    private bool ReferencesAreAssigned()
    {
        if (touchPress &&
            touchPosition &&
            secondaryTouchPress &&
            secondaryTouchPosition)
        {
            return true;
        }

        Debug.LogError("Assign all four touch actions on MobileInputController.", this);
        return false;
    }
}


