using UnityEngine;
using UnityEngine.InputSystem;

// Unity Input System manual: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/Sensors.html
// LightSensor.lightLevel reports lux on supported Android hardware. Sensors start disabled.
// Physical room lighting must be tested on an Android device; Windows/iOS do not expose this sensor.
public class AmbientLightController : MonoBehaviour
{
    [Header("Scene lighting")]
    [SerializeField] private Light mainDirectionalLight;
    [SerializeField] private float darkestRoomLux = 0f;
    [SerializeField] private float brightestRoomLux = 500f;
    [SerializeField] private float dimSceneIntensity = 0.35f;
    [SerializeField] private float brightSceneIntensity = 2f;
    [SerializeField] private float lightingResponse = 2f;
    [SerializeField] private bool driveAmbientIntensity = true;
    [SerializeField] private float dimAmbientIntensity = 0.25f;
    [SerializeField] private float brightAmbientIntensity = 1f;

    [Header("Low-light response")]
    [SerializeField] private Light characterLight;
    [SerializeField] private float turnOnBelowLux = 20f;
    [SerializeField] private float turnOffAboveLux = 35f;
    [SerializeField] private Vector3 characterLightLocalPosition = new Vector3(0f, 0.6f, 0f);
    [SerializeField] private Vector3 characterLightLocalRotation;

    private LightSensor lightSensor;
    private ItemViewportController itemViewport;
    private float startingLightIntensity;
    private float startingAmbientIntensity;
    private bool startingCharacterLightState;

    private void Awake()
    {
        if (mainDirectionalLight == null)
            mainDirectionalLight = RenderSettings.sun;

        if (mainDirectionalLight != null)
            startingLightIntensity = mainDirectionalLight.intensity;

        startingAmbientIntensity = RenderSettings.ambientIntensity;

        if (characterLight != null)
        {
            startingCharacterLightState = characterLight.enabled;
            characterLight.enabled = false;
        }

        itemViewport = FindAnyObjectByType<ItemViewportController>();
    }

    private void OnEnable()
    {
        itemViewport = FindAnyObjectByType<ItemViewportController>();
        if (itemViewport != null)
            itemViewport.CurrentItemChanged += AttachCharacterLight;

        lightSensor = LightSensor.current;
        if (lightSensor == null)
        {
            Debug.LogWarning("This device does not have a supported ambient light sensor.", this);
            return;
        }

        InputSystem.EnableDevice(lightSensor);
    }

    private void OnDisable()
    {
        if (itemViewport != null)
            itemViewport.CurrentItemChanged -= AttachCharacterLight;

        if (lightSensor != null && lightSensor.enabled)
            InputSystem.DisableDevice(lightSensor);

        if (mainDirectionalLight != null)
            mainDirectionalLight.intensity = startingLightIntensity;

        RenderSettings.ambientIntensity = startingAmbientIntensity;

        if (characterLight != null)
            characterLight.enabled = startingCharacterLightState;
    }

    private void Update()
    {
        if (lightSensor == null || !lightSensor.enabled)
            return;

        float lux = Mathf.Max(0f, lightSensor.lightLevel.ReadValue());
        float maxLux = Mathf.Max(brightestRoomLux, darkestRoomLux + 1f);
        float brightness = Mathf.InverseLerp(darkestRoomLux, maxLux, lux);
        float blend = 1f - Mathf.Exp(-Mathf.Max(0f, lightingResponse) * Time.deltaTime);

        if (mainDirectionalLight != null)
        {
            float target = Mathf.Lerp(dimSceneIntensity, brightSceneIntensity, brightness);
            mainDirectionalLight.intensity = Mathf.Lerp(mainDirectionalLight.intensity, target, blend);
        }

        if (driveAmbientIntensity)
        {
            float target = Mathf.Lerp(dimAmbientIntensity, brightAmbientIntensity, brightness);
            RenderSettings.ambientIntensity = Mathf.Lerp(RenderSettings.ambientIntensity, target, blend);
        }

        if (characterLight != null)
        {
            if (lux < turnOnBelowLux)
                characterLight.enabled = true;
            else if (lux > turnOffAboveLux)
                characterLight.enabled = false;
        }
    }

    private void AttachCharacterLight(ItemData item, Transform itemTransform)
    {
        if (characterLight == null || itemTransform == null)
            return;

        characterLight.transform.SetParent(itemTransform, false);
        characterLight.transform.localPosition = characterLightLocalPosition;
        characterLight.transform.localRotation = Quaternion.Euler(characterLightLocalRotation);
    }
}


