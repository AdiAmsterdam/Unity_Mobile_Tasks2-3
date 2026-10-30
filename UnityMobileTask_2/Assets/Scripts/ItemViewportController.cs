using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemViewportController : MonoBehaviour
{
    [Header("Items")]
    [SerializeField] private List<ItemData> items;
    [SerializeField] private Transform spawnPoint;

    [Header("Interaction")]
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private float moveSpeed = 0.01f;
    [SerializeField] private float rotationSpeed = 0.25f;
    [SerializeField] private float pinchScaleMultiplier = 0.01f;
    [SerializeField] private float minimumScale = 0.1f;

    [Header("Input")]
    [SerializeField] private MobileInputController inputController;

    public event Action<ItemData> SelectionConfirmed;
    public event Action<ItemData, Transform> CurrentItemChanged;

    public Transform CurrentItemTransform => GetCurrentItem() != null ? GetCurrentItem().transform : null;
    public ItemData CurrentItemData => HasItems() && currentIndex >= 0 && currentIndex < items.Count ? items[currentIndex] : null;
    public string CurrentItemId => CurrentItemData != null ? CurrentItemData.GetId() : string.Empty;
    public IReadOnlyList<ItemData> Items => items;

    private readonly List<GameObject> itemInstances = new List<GameObject>();
    private int currentIndex;
    private bool touchStartedOnCurrentItem;
    private Camera ActiveCamera => interactionCamera != null ? interactionCamera : Camera.main;

    private void OnEnable()
    {
        if (inputController == null)
        {
            Debug.LogError("InputController is not assigned.", this);
            return;
        }

        inputController.SwipeLeft += OnSwipeLeft;
        inputController.SwipeRight += OnSwipeRight;
        inputController.SwipeUp += OnSwipeUp;
        inputController.PrimaryTouchStarted += OnPrimaryTouchStarted;
        inputController.TouchMoved += OnTouchMoved;
        inputController.PrimaryTouchEnded += OnPrimaryTouchEnded;
        inputController.DoubleTap += OnDoubleTap;
        inputController.Pinch += OnPinch;
        inputController.Rotate += OnRotate;
    }

    private void OnDisable()
    {
        if (!inputController)
            return;

        inputController.SwipeLeft -= OnSwipeLeft;
        inputController.SwipeRight -= OnSwipeRight;
        inputController.SwipeUp -= OnSwipeUp;
        inputController.PrimaryTouchStarted -= OnPrimaryTouchStarted;
        inputController.TouchMoved -= OnTouchMoved;
        inputController.PrimaryTouchEnded -= OnPrimaryTouchEnded;
        inputController.DoubleTap -= OnDoubleTap;
        inputController.Pinch -= OnPinch;
        inputController.Rotate -= OnRotate;
    }

    private void Start()
    {
        if (items == null || items.Count == 0 || spawnPoint == null)
            return;

        itemInstances.Clear();
        for (int i = 0; i < items.Count; i++)
            itemInstances.Add(null);

        DisplayItem(currentIndex);
    }

    private void OnSwipeLeft()
    {
        if (!touchStartedOnCurrentItem)
            NextItem();
    }

    private void OnSwipeRight()
    {
        if (!touchStartedOnCurrentItem)
            PreviousItem();
    }

    private void OnSwipeUp(Vector2 screenPosition)
    {
        if (touchStartedOnCurrentItem)
            ConfirmCurrentItem();
    }

    private void OnPrimaryTouchStarted(Vector2 screenPosition)
    {
        touchStartedOnCurrentItem = IsScreenPointOnCurrentItem(screenPosition);
    }

    private void OnPrimaryTouchEnded()
    {
        touchStartedOnCurrentItem = false;
    }

    public void NextItem()
    {
        if (!HasItems())
            return;

        currentIndex = (currentIndex + 1) % items.Count;
        DisplayItem(currentIndex);
    }

    public void PreviousItem()
    {
        if (!HasItems())
            return;

        currentIndex = (currentIndex - 1 + items.Count) % items.Count;
        DisplayItem(currentIndex);
    }

    public bool SelectItemById(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId) || !HasItems())
            return false;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null || items[i].GetId() != itemId)
                continue;

            currentIndex = i;
            DisplayItem(currentIndex);
            return GetCurrentItem() != null;
        }

        return false;
    }

    public void ConfirmCurrentItem()
    {
        ItemData selectedData = CurrentItemData;
        if (selectedData == null || GetCurrentItem() == null)
            return;

        Debug.Log("Confirmed selection: " + selectedData.GetDisplayName(), this);
        SelectionConfirmed?.Invoke(selectedData);
    }

    private void DisplayItem(int index)
    {
        GameObject selectedItem = GetOrCreateItem(index);
        if (selectedItem == null)
            return;

        for (int i = 0; i < itemInstances.Count; i++)
        {
            if (itemInstances[i] != null)
                itemInstances[i].SetActive(false);
        }

        selectedItem.SetActive(true);
        CurrentItemChanged?.Invoke(items[index], selectedItem.transform);
    }

    private GameObject GetOrCreateItem(int index)
    {
        if (items[index] == null || items[index].prefab == null)
            return null;

        if (itemInstances[index] == null)
        {
            itemInstances[index] = Instantiate(
                items[index].prefab,
                spawnPoint.position,
                spawnPoint.rotation,
                spawnPoint);
        }

        return itemInstances[index];
    }

    private void OnTouchMoved(Vector2 screenMovement)
    {
        if (!touchStartedOnCurrentItem)
            return;

        GameObject currentItem = GetCurrentItem();
        Camera cameraToUse = ActiveCamera;
        if (currentItem == null || cameraToUse == null)
            return;

        Vector3 worldMovement =
            cameraToUse.transform.right * screenMovement.x +
            cameraToUse.transform.up * screenMovement.y;

        currentItem.transform.position += worldMovement * moveSpeed;
    }

    private void OnPinch(float distanceChange)
    {
        if (!touchStartedOnCurrentItem)
            return;

        GameObject currentItem = GetCurrentItem();
        if (currentItem == null)
            return;

        float scaleAmount = distanceChange * pinchScaleMultiplier;
        Vector3 newScale = currentItem.transform.localScale + Vector3.one * scaleAmount;
        currentItem.transform.localScale = Vector3.Max(newScale, Vector3.one * minimumScale);
    }

    private void OnRotate(float horizontalMovement)
    {
        if (!touchStartedOnCurrentItem)
            return;

        GameObject currentItem = GetCurrentItem();
        if (currentItem == null)
            return;

        currentItem.transform.Rotate(
            Vector3.up,
            horizontalMovement * rotationSpeed,
            Space.World);
    }

    private void OnDoubleTap(Vector2 screenPosition)
    {
        if (IsScreenPointOnCurrentItem(screenPosition))
            ConfirmCurrentItem();
    }

    private bool IsScreenPointOnCurrentItem(Vector2 screenPosition)
    {
        GameObject currentItem = GetCurrentItem();
        Camera cameraToUse = ActiveCamera;
        if (currentItem == null || cameraToUse == null)
            return false;

        Renderer[] renderers = currentItem.GetComponentsInChildren<Renderer>();
        foreach (Renderer itemRenderer in renderers)
        {
            if (!itemRenderer.enabled || !itemRenderer.gameObject.activeInHierarchy)
                continue;

            Bounds bounds = itemRenderer.bounds;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            float minScreenX = float.PositiveInfinity;
            float minScreenY = float.PositiveInfinity;
            float maxScreenX = float.NegativeInfinity;
            float maxScreenY = float.NegativeInfinity;
            bool hasVisibleCorner = false;

            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 worldCorner = new Vector3(
                    (corner & 1) == 0 ? min.x : max.x,
                    (corner & 2) == 0 ? min.y : max.y,
                    (corner & 4) == 0 ? min.z : max.z);

                Vector3 screenCorner = cameraToUse.WorldToScreenPoint(worldCorner);
                if (screenCorner.z <= 0f)
                    continue;

                hasVisibleCorner = true;
                minScreenX = Mathf.Min(minScreenX, screenCorner.x);
                minScreenY = Mathf.Min(minScreenY, screenCorner.y);
                maxScreenX = Mathf.Max(maxScreenX, screenCorner.x);
                maxScreenY = Mathf.Max(maxScreenY, screenCorner.y);
            }

            if (hasVisibleCorner &&
                screenPosition.x >= minScreenX && screenPosition.x <= maxScreenX &&
                screenPosition.y >= minScreenY && screenPosition.y <= maxScreenY)
            {
                return true;
            }
        }

        return false;
    }

    private GameObject GetCurrentItem()
    {
        if (currentIndex < 0 || currentIndex >= itemInstances.Count)
            return null;

        return itemInstances[currentIndex];
    }

    private bool HasItems()
    {
        return items != null && items.Count > 0;
    }
}


