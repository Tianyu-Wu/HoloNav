using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class CallAnchorScripts : MonoBehaviour
{

    //This function calls the scripts of children gameobjects of mini-map anchors.
    //Implemented by X.Brunner, november 2020

    public string destination;
    public Component[] addDestination;
    
    [SerializeField]
    private TMP_Dropdown destDropdown;

    [System.Obsolete]
    public void GetDestination()
    {
        //Get the destination from the drop-down menu
        destination = destDropdown.options[destDropdown.value].text;
        addDestination = GetComponentsInChildren<AddDestination>();

        foreach (AddDestination anchor in addDestination)
        {
            //Add a new destination
            anchor.addNewDestination(destination);
        }

        var updateAnchors = GetComponentsInChildren<UpdateMinimap>();

        foreach (UpdateMinimap anchor in updateAnchors)
        {
            //Upate the destination for each mini-map anchor gameobject
            anchor.destinationAnchor = destination;
        }

    }
}
