using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Joystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public Image background;
    public Image handle;
    public Vector2 inputVector;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background.rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out position))
        {
            position.x = (position.x / background.rectTransform.sizeDelta.x) * 2;
            position.y = (position.y / background.rectTransform.sizeDelta.y) * 2;

            inputVector = new Vector2(position.x, position.y);
            inputVector = (inputVector.magnitude > 1.0f) ? inputVector.normalized : inputVector;

            handle.rectTransform.anchoredPosition = new Vector2(
                inputVector.x * (background.rectTransform.sizeDelta.x / 2),
                inputVector.y * (background.rectTransform.sizeDelta.y / 2));
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        handle.rectTransform.anchoredPosition = Vector2.zero;
    }

    public float Horizontal()
    {
        return inputVector.x;
    }

    public float Vertical()
    {
        return inputVector.y;
    }
}