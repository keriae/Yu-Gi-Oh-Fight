using System;
using UnityEngine;
using UnityEngine.EventSystems;


namespace YuGiOhFight.Client.UI
{
public class DoWhenOnDrag : MonoBehaviour, IDragHandler
{
    public Action action;

    public void OnDrag(PointerEventData eventData)
    {
        action?.Invoke();
    }
}
}
