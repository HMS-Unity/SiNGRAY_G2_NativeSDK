using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class RayCastTest : MonoBehaviour,IPointerClickHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
{
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.LogError("OnBeginDrag"+ this.name);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.LogError("OnDrag" + this.name);

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.LogError("OnEndDrag" + this.name);

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.LogError("OnPointerClick"+this.name);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
