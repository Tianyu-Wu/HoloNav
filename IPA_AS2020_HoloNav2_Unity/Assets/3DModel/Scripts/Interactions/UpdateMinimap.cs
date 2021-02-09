using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UpdateMinimap : MonoBehaviour
{
    public GameObject anchorObject;
    public string currentAnchorID;

    //public string proximityAnchor;
    public string destinationAnchor;

    //This function displays the met anchors during the navigation.
    //proximityAnchor: each new anchor which the user meets during the navigation.
    //destinationAnchor: anchor destination of user.
    //Implemented by X.Brunner, november 2020
    void Awake()
    {
        AnchorPosition.OnEnterAnchorPosition += UpdateCurrentPosition;
    }

    void OnDestroy()
    {
        AnchorPosition.OnEnterAnchorPosition -= UpdateCurrentPosition;
    }

    private void UpdateCurrentPosition(AnchorPosition anchorPos)
    {
        string name = anchorPos.SpatialAnchorObject.Name;
        string destination = destinationAnchor;
        updateMinimap(name.Substring(0,name.Length-1), destination.Substring(0, destination.Length - 1));
    }

    public void updateMinimap(string proximityAnchor, string destinationAnchor)
    {

        if (currentAnchorID == proximityAnchor)
        {
            //Vizualize the new position on the Minimap
            NewMinimapPosition(anchorObject);
        }

        //Select all the others anchors (without the destination anchor)
        else if (currentAnchorID != proximityAnchor && currentAnchorID != destinationAnchor)
        {
            //Change color to establisch the History of met anchors
            HistoryPosition(anchorObject);
        }
    }

    void NewMinimapPosition(GameObject anchorObj)
    {
        //Get the Renderer component from the sphere (anchor)
        var anchorRenderer = anchorObj.GetComponent<Renderer>();
        
        anchorRenderer.enabled = true;

        Color32 color = new Color32(240,255,0,1);

        //Colorzine the new goal in red
        anchorRenderer.material.SetColor("_Color", color);
    }

    void HistoryPosition(GameObject anchorObj)
    {
        //Get the Renderer component from the sphere (anchor)
        var anchorRenderer = anchorObj.GetComponent<Renderer>();

        Color32 color = new Color32(143, 252, 255, 1);

        //Colorzine the new goal in red
        anchorRenderer.material.SetColor("_Color", color);
    }
}
