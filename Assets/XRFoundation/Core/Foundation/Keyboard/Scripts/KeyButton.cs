using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Singray.UI.Keyboard
{ 
public class KeyButton : Button
{
    
    private float distance=-20;
   
    protected override void OnEnable()
    {
        base.OnEnable();
       
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        SetDistance(distance);

    }
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        SetDistance(0);

    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        SetDistance(distance);



    }

    private void SetDistance(float dis) {
     Vector3 localPosition=transform.localPosition;
        localPosition.z = dis;
        transform.localPosition = localPosition;
    }
}
}
