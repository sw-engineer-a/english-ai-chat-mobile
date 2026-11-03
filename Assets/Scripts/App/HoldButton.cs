using UnityEngine;
using UnityEngine.EventSystems;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public DragonApp App;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (App != null) App.PressStart();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (App != null) App.ReleaseStart();
    }
}
