using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class HILFloorUp : MonoBehaviour
{
    int activeObject = 0;
    public GameObject floorD;
    public GameObject floorE;
    public GameObject floorF;
    public GameObject floorG;
    public GameObject floorH;

    public void OnPointerClick(PointerEventData eventData)
    {
        EventFloorUp();
    }

    //my event Floor up
    [Serializable]
    public class MyOwnEvent : UnityEvent { }

    [SerializeField]
    private MyOwnEvent myOwnEvent = new MyOwnEvent();
    public MyOwnEvent onMyOwnEvent { get { return myOwnEvent; } set { myOwnEvent = value; } }

    public void EventFloorUp()
    {
        onMyOwnEvent.Invoke();

        // Activate the next floor
        ActivateObject(activeObject);

        // Update active object
        activeObject = ++activeObject;

        // Reset on floor H
        if (activeObject >= 4)
        {
            activeObject = 0;
        }
    }

    void ActivateObject(int activeObject)
    {
        switch (activeObject)
        {
            //Case HIL D to HIL E
            case 0:
                floorD.SetActive(false);
                floorE.SetActive(true);
                floorF.SetActive(false);
                floorG.SetActive(false);
                floorH.SetActive(false);
                break;
            //Case HIL E to HIL F
            case 1:
                floorD.SetActive(false);
                floorE.SetActive(false);
                floorF.SetActive(true);
                floorG.SetActive(false);
                floorH.SetActive(false);
                break;
            //Case HIL F to HIL G
            case 2:
                floorD.SetActive(false);
                floorE.SetActive(false);
                floorF.SetActive(false);
                floorG.SetActive(true);
                floorH.SetActive(false);
                break;
            //Case HIL G to HIL H
            case 3:
                floorD.SetActive(false);
                floorE.SetActive(false);
                floorF.SetActive(false);
                floorG.SetActive(false);
                floorH.SetActive(true);
                break;
            // Virtual case for loop
            case 4:
                floorD.SetActive(true);
                floorE.SetActive(false);
                floorF.SetActive(false);
                floorG.SetActive(false);
                floorH.SetActive(false);
                break;
        }
    }
 }
