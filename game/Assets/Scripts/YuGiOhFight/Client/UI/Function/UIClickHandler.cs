using System;
using UnityEngine;
using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.UI
{
public class UIClickHandler : MonoBehaviour, IPointerDownHandler
{
    public Action action;
    public void OnPointerDown(PointerEventData eventData)
    {
        action?.Invoke();
    }
}
}
