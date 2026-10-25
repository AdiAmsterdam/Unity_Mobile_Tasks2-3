using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class MobileJoystickController : MonoBehaviour
{
    [SerializeField] private ItemViewportController itemViewport;
    [SerializeField] private Camera orbitCamera;
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private float orbitSpeed = 100f;
    [SerializeField] private float minimumPitch = -70f;
    [SerializeField] private float maximumPitch = 70f;

    private OnScreenStick leftStick;
    private OnScreenStick rightStick;
    private Vector2Control leftControl;
    private Vector2Control rightControl;
    private Sprite circleSprite;
    private Transform orbitTarget;
    private Vector3 orbitPivot;
    private bool hasOrbitTarget;

    private void Awake()
    {
        if (itemViewport == null)
            itemViewport = FindAnyObjectByType<ItemViewportController>();

        if (orbitCamera == null)
            orbitCamera = Camera.main;

        CreateEventSystem();
        CreateJoysticks();
    }

    private void LateUpdate()
    {
        if (leftControl == null && leftStick != null)
            leftControl = leftStick.control as Vector2Control;
        if (rightControl == null && rightStick != null)
            rightControl = rightStick.control as Vector2Control;

        if (itemViewport == null || orbitCamera == null)
            return;

        Transform target = itemViewport.CurrentItemTransform;
        if (target == null)
            return;

        if (target != orbitTarget)
        {
            if (hasOrbitTarget)
            {
                Vector3 targetShift = target.position - orbitPivot;
                orbitCamera.transform.position += targetShift;
            }

            orbitTarget = target;
            orbitPivot = target.position;
            hasOrbitTarget = true;
        }

        Vector2 move = leftControl != null ? leftControl.ReadValue() : Vector2.zero;
        Vector3 forward = Vector3.ProjectOnPlane(orbitCamera.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 movement = (right * move.x + forward * move.y) * movementSpeed * Time.deltaTime;
        target.position += movement;
        orbitPivot += movement;
        orbitCamera.transform.position += movement;

        Vector2 look = rightControl != null ? rightControl.ReadValue() : Vector2.zero;
        if (look.sqrMagnitude > 0.0001f)
        {
            Vector3 offset = orbitCamera.transform.position - orbitPivot;
            offset = Quaternion.AngleAxis(look.x * orbitSpeed * Time.deltaTime, Vector3.up) * offset;

            float pitch = Mathf.Asin(Mathf.Clamp(offset.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
            float targetPitch = Mathf.Clamp(
                pitch - look.y * orbitSpeed * Time.deltaTime,
                minimumPitch,
                maximumPitch);
            Vector3 viewDirection = -offset.normalized;
            Vector3 pitchAxis = Vector3.Cross(Vector3.up, viewDirection).normalized;
            if (pitchAxis.sqrMagnitude < 0.001f)
                pitchAxis = orbitCamera.transform.right;
            offset = Quaternion.AngleAxis(targetPitch - pitch, pitchAxis) * offset;

            orbitCamera.transform.position = orbitPivot + offset;
            orbitCamera.transform.LookAt(orbitPivot);
        }
    }

    private void OnDestroy()
    {
        if (circleSprite != null)
        {
            Destroy(circleSprite.texture);
            Destroy(circleSprite);
        }
    }

    private void CreateEventSystem()
    {
        EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
            return;
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            foreach (BaseInputModule module in eventSystem.GetComponents<BaseInputModule>())
                Destroy(module);

            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private void CreateJoysticks()
    {
        GameObject canvasObject = new GameObject(
            "Mobile Joysticks",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        circleSprite = CreateCircleSprite(128);
        leftStick = CreateStick(canvas.transform, "Move Stick", "<Gamepad>/leftStick", false);
        rightStick = CreateStick(canvas.transform, "Camera Stick", "<Gamepad>/rightStick", true);
    }

    private OnScreenStick CreateStick(Transform parent, string label, string path, bool rightSide)
    {
        GameObject baseObject = new GameObject(label + " Base", typeof(RectTransform), typeof(Image));
        baseObject.transform.SetParent(parent, false);

        RectTransform baseRect = baseObject.GetComponent<RectTransform>();
        Vector2 corner = rightSide ? new Vector2(1f, 0f) : Vector2.zero;
        baseRect.anchorMin = corner;
        baseRect.anchorMax = corner;
        baseRect.pivot = new Vector2(0.5f, 0.5f);
        baseRect.anchoredPosition = rightSide ? new Vector2(-135f, 135f) : new Vector2(135f, 135f);
        baseRect.sizeDelta = new Vector2(220f, 220f);

        Image baseImage = baseObject.GetComponent<Image>();
        baseImage.sprite = circleSprite;
        baseImage.color = new Color(0.08f, 0.1f, 0.14f, 0.55f);
        baseImage.raycastTarget = false;

        GameObject inputObject = new GameObject(label, typeof(RectTransform), typeof(Image));
        inputObject.SetActive(false);
        inputObject.transform.SetParent(baseObject.transform, false);

        RectTransform inputRect = inputObject.GetComponent<RectTransform>();
        inputRect.anchorMin = Vector2.zero;
        inputRect.anchorMax = Vector2.one;
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;

        Image inputImage = inputObject.GetComponent<Image>();
        inputImage.sprite = circleSprite;
        inputImage.color = new Color(1f, 1f, 1f, 0.01f);
        inputImage.raycastTarget = true;

        OnScreenStick stick = inputObject.AddComponent<OnScreenStick>();
        stick.controlPath = path;
        stick.behaviour = OnScreenStick.Behaviour.RelativePositionWithStaticOrigin;
        stick.movementRange = 65f;
        stick.useIsolatedInputActions = true;

        GameObject knobObject = new GameObject(label + " Knob", typeof(RectTransform), typeof(Image));
        knobObject.transform.SetParent(inputObject.transform, false);
        RectTransform knobRect = knobObject.GetComponent<RectTransform>();
        knobRect.anchorMin = new Vector2(0.5f, 0.5f);
        knobRect.anchorMax = new Vector2(0.5f, 0.5f);
        knobRect.pivot = new Vector2(0.5f, 0.5f);
        knobRect.sizeDelta = new Vector2(72f, 72f);

        Image knobImage = knobObject.GetComponent<Image>();
        knobImage.sprite = circleSprite;
        knobImage.color = new Color(0.75f, 0.86f, 1f, 0.92f);
        knobImage.raycastTarget = false;

        inputObject.SetActive(true);
        return stick;
    }

    private Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Joystick Circle";
        texture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        float radius = (size - 1f) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                byte alpha = (byte)(Mathf.Clamp01(radius + 0.75f - distance) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}


