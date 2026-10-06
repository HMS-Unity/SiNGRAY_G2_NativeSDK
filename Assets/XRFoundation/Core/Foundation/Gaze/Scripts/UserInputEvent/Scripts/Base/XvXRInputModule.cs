using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Singray.UI.Input
{ 

/// <summary>
///Process custom ray input
/// </summary>
[RequireComponent(typeof(EventSystem))]
    [DisallowMultipleComponent]
public class XvXRInputModule : MixedRealityInputModule
    {

      //  

    private XvInputControllerBase[] inputControllerBases;

    protected override void Awake()
    {
        base.Awake();
        inputControllerBases = FindObjectsOfType<XvInputControllerBase>();

         EventSystem.current.sendNavigationEvents = false;
    }
    public override void Process()
    {
        base.Process();//Process built-in events

        ProcessAllRaycast();
    }
    private void ProcessAllRaycast()
    {

           ///Iterate over all input modules
        for (int i = 0; i < inputControllerBases.Length; i++)
        {
                //Skip inactive or unavailable modules
            if (!inputControllerBases[i].isActiveAndEnabled) {
                continue;
            }

            if (inputControllerBases[i].Is3DInput)
            {
                    //For 3D ray input, collect all UI elements intersected by the ray
                XvRaycaster customRaycaster = inputControllerBases[i].customRaycaster;
                RaycastResult result = customRaycaster.FirstRaycastResult();//Get the first element

                Process2DOr3DRaycast(inputControllerBases[i], result);//Process the current UI element


            }
            else
            {
                    //For screen-point input, retrieve the point
                PointerEventData pointerEventData = new PointerEventData(eventSystem);
                  //Get the custom screen input point
                pointerEventData.position = inputControllerBases[i].screenPosition;
                    //Set the button to LeftButton
                pointerEventData.button = PointerEventData.InputButton.Left;

                    //Use EventSystem to find the object hit by the ray
                eventSystem.RaycastAll(pointerEventData, m_RaycastResultCache);
                RaycastResult result = FindFirstRaycast(m_RaycastResultCache);
                Process2DOr3DRaycast(inputControllerBases[i], result);
            }

        }

        // }
    }
    private void Process2DOr3DRaycast(XvInputControllerBase inputControllerBase, RaycastResult result)
    {
       
        ///Get mouse wheel input
        var scrollDelta = inputControllerBase.GetScrollDelta();


        var customEventData = inputControllerBase.CustomEventData;
        if (customEventData == null) { return; }

        customEventData.Reset();
        customEventData.delta = Vector2.zero;
        customEventData.scrollDelta = scrollDelta;//Mouse scroll delta


        customEventData.pointerCurrentRaycast = result;//Set the current raycast information
            //Movement between frames
        customEventData.screenPositionDeltadelta = inputControllerBase.PositionDeltadelta.magnitude;

        //customEventData.position = inputControllerBase.CustomEventData.position;
        customEventData.position = inputControllerBase.screenPosition;
       



        ProcessPress(customEventData);
        ProcessMove(customEventData);
        ProcessDrag(customEventData);

        // Scroll event
        if (result.isValid && !Mathf.Approximately(scrollDelta.sqrMagnitude, 0.0f))
        {
            var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(result.gameObject);
            ExecuteEvents.ExecuteHierarchy(scrollHandler, customEventData, ExecuteEvents.scrollHandler);
        }
        if (eventSystem.sendNavigationEvents)
        {
            SendSubmitEventToSelectedObject();
        }
        SendUpdateEventToSelectedObject();
    }
  

    #region Process mouse press and release events
    protected void ProcessPress(CustomEventData eventData)
    {
        if (eventData.GetKeyPress())
        {
            //Check whether the button is pressed
            if (!eventData.pressPrecessed)
            {
                ProcessPressDown(eventData);

            }
            // Debug.LogError(eventData.eligibleForClick);
        }
        else if (eventData.pressPrecessed)
        {

            ProcessPressUp(eventData);
        }
    }
    //protected void DeselectIfSelectionChanged(GameObject currentOverGo, BaseEventData pointerEvent)
    //{
    //    // Selection tracking
    //    var selectHandlerGO = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
    //    // if we have clicked something new, deselect the old thing
    //    // leave 'selection handling' up to the press event though.
    //    if (selectHandlerGO != eventSystem.currentSelectedGameObject)
    //        eventSystem.SetSelectedGameObject(null, pointerEvent);
    //}
    protected void ProcessPressDown(CustomEventData eventData)
    {
        //Currently selected object
        var currentOverGo = eventData.pointerCurrentRaycast.gameObject;
        //Set the pressed state
        eventData.pressPrecessed = true;

        //Set click eligibility
        eventData.eligibleForClick = true;
        //Pointer position changed
        eventData.delta = Vector2.zero;
        //Set the drag state
        eventData.dragging = false;
        //Whether to apply the drag threshold
        eventData.useDragThreshold = true;
        //Set mouse position
        eventData.pressPosition = eventData.position;
        //Set the object pressed this frame
        eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;
            //Deselect the current GameObject if the pointer now targets a different one
            DeselectIfSelectionChanged(currentOverGo, eventData);
        eventData.button = PointerEventData.InputButton.Left;

            //Find the IPointerDownHandler and dispatch the event immediately on press
            var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.pointerDownHandler);

            //Find the IPointerClickHandler without invoking it; on release, check whether the target is unchanged
            var newClick = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
        // 
        if (newPressed == null)
        {
               
                newPressed = newClick;
        }
        //Without time scaling, this time equals Time.time
        var time = Time.unscaledTime;

        if (newPressed == eventData.lastPress)
        {
            //Set the click count
            if (time < (eventData.clickTime))
            {
                ++eventData.clickCount;
            }
            else
            {
                eventData.clickCount = 1;
            }
            //Set the click time
            eventData.clickTime = time;
        }
        else
        {
            eventData.clickCount = 1;
        }
            //Object that handles pointerDownHandler
            eventData.pointerPress = newPressed;
            //Store the IPointerClickHandler target to determine whether to dispatch a click on release
            eventData.pointerClick = newClick;

        if (newPressed != null)
        {
            //Debug.LogError("newPressed==" + newPressed.name);
        }
        if (newClick != null)
        {
           // Debug.LogError("newClick==" + newClick.name);
        }
        //Original pointer target
        eventData.rawPointerPress = currentOverGo;
        //
        eventData.clickTime = time;

        // Find the object that handles dragging
        eventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

        if (eventData.pointerDrag != null)
        {
            //Initialize the drag event
            ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.initializePotentialDrag);
        }
    }

    protected void ProcessPressUp(CustomEventData eventData)
    {

        //Get the current object
        var currentOverGo = eventData.pointerCurrentRaycast.gameObject;



        if (eventData.pointerPress != null)
        {
            //Dispatch pointer-up to the pressed object
            ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

        }
        //Check whether the pointer is still over the element that was clicked
        var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

        // Dispatch the button click event
        if (eventData.pointerClick != null && eventData.pointerClick == pointerUpHandler && eventData.eligibleForClick)
        {
           ExecuteEvents.Execute(eventData.pointerClick, eventData, ExecuteEvents.pointerClickHandler);
        }
        else if (eventData.pointerDrag != null && eventData.dragging)
        {
            //If released over another element while dragging, dispatch a drop event
            ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.dropHandler);
        }
        //Clear the pressed state
        eventData.pressPrecessed = false;

        eventData.eligibleForClick = false;
        //Clear the pointer-press target
        eventData.pointerPress = null;
        eventData.rawPointerPress = null;

        //If an object is being dragged, dispatch the drag event
        if (eventData.pointerDrag != null && eventData.dragging)
        {
            ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.endDragHandler);
        }

        eventData.dragging = false;
        eventData.pointerDrag = null;

        //Reapply pointer enter/exit events to refresh the state
        //This lets a newly hovered object receive events that were previously suppressed by a press on another object
        if (currentOverGo != eventData.pointerEnter)
        {
            HandlePointerExitAndEnter(eventData, null);
            HandlePointerExitAndEnter(eventData, currentOverGo);
        }
    }

    #endregion
    #region Process move events
    new protected void ProcessMove(PointerEventData eventData)
    {
        var hoverGO = eventData.pointerCurrentRaycast.gameObject;
        if (eventData.pointerEnter != hoverGO)
        {
            HandlePointerExitAndEnter(eventData, hoverGO);
        }

    }
    #endregion

    #region Process drag events
    protected bool ShouldStartDrag(CustomEventData eventData)
    {

        bool isDrag = (eventData.screenPositionDeltadelta > 5);

      //  Debug.LogError("isDrag  ==" + isDrag + "  " + eventData.screenPositionDeltadelta);
        return isDrag;
    }

    protected void ProcessDrag(CustomEventData eventData)
    {
        eventData.button = PointerEventData.InputButton.Left;
        //Debug.LogError("Dragging: " + eventData.pointerDrag != null + " " + !eventData.dragging + "  " + ShouldStartDrag(eventData));
        if (eventData.pointerDrag != null && !eventData.dragging && ShouldStartDrag(eventData))
        {
            ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.beginDragHandler);
            eventData.dragging = true;


        }




        if (eventData.dragging && eventData.pointerDrag != null)
        {
            if (eventData.pointerPress != eventData.pointerDrag)
            {
                ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

                eventData.eligibleForClick = false;
                eventData.pointerPress = null;
                eventData.rawPointerPress = null;
            }
            ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.dragHandler);
            //Debug.LogError(eventData.pointerDrag.name);

        }
    }

    #endregion

    new protected bool SendSubmitEventToSelectedObject()
    {
        if (eventSystem.currentSelectedGameObject == null)
            return false;

        var data = GetBaseEventData();
        if (input.GetButtonDown("Submit"))
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.submitHandler);

        if (input.GetButtonDown("Cancel"))
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.cancelHandler);
        return data.used;
    }
    new protected bool SendUpdateEventToSelectedObject()
    {
        if (eventSystem.currentSelectedGameObject == null)
            return false;

        var data = GetBaseEventData();
        ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
        return data.used;
    }
}
}

