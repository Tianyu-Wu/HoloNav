using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class AddDestination : MonoBehaviour
{
    public GameObject anchorObject; 
    public string currentAnchorID;


    //This function diplays the goal in yellow and disable all others anchors on the mini-map
    //X.Brunner 22.11.2020

    public void addNewDestination(string newDestination)
    {
        if (currentAnchorID == newDestination.Substring(0,newDestination.Length-1))
        {
            //Visualize the new destination on the mini-map
            ActivateDestination(anchorObject);
        } else {
            //Hide all others points on the mini-map
            DisableDestination(anchorObject);
        }
    }

    public void ActivateDestination(GameObject anchorObj)
    {
        //Set active the new goal
        anchorObj.SetActive(true);

        //Get the Renderer component from the sphere (anchor)
        var anchorRenderer = anchorObj.GetComponent<Renderer>();

        //Colorzine the new goal in yellow
        Color goalColor = new Color(207, 0, 15, 1);
        anchorRenderer.material.SetColor("_Color", goalColor);
    }

    public void DisableDestination(GameObject anchorObj)
    {
        //Get the Renderer component from the sphere (anchor)
        var anchorRenderer = anchorObj.GetComponent<Renderer>();
        
        //Disable visibility of others anchors
        anchorRenderer.enabled = false;
    }
}