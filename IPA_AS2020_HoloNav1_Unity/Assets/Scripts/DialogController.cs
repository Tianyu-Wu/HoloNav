using Microsoft.MixedReality.Toolkit.Experimental.Dialog;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogController : MonoBehaviour
{
    [SerializeField] 
    private GameObject dialogPrefab;

    public void OpenDialog(string title, string text, string type)
    {
        Dialog myDialog = Dialog.Open(dialogPrefab, DialogButtonType.Yes | DialogButtonType.No, title, text, true);
        if (myDialog != null)
        {
            if (type == "create")
            {
                myDialog.OnClosed += OnClosedCreateDialogEvent;
            } else if (type == "navigate")
            {
                myDialog.OnClosed += OnClosedNavigateDialogEvent;
            }
            
        }
    }

    public void OpenDialogSimple(string title, string text)
    {
        Dialog myDialog = Dialog.Open(dialogPrefab, DialogButtonType.OK, title, text, true);

    }

    private void OnClosedNavigateDialogEvent(DialogResult obj)
    {
        // if clicked yes, invoke an startNavigationConfirmedEvent
        if (obj.Result == DialogButtonType.Yes)
        {
            OnStartNavigationConfirmed();
        }
    }

    private void OnClosedCreateDialogEvent(DialogResult obj)
    {
        // if clicked yes, invoke an startCreationConfirmedEvent
        if (obj.Result == DialogButtonType.Yes)
        {
            OnStartCreationConfirmed();
        }
    }

    public delegate void StartCreationConfirmed();
    public static event StartCreationConfirmed OnStartCreationConfirmed;
    public delegate void StartNavigationConfirmed();
    public static event StartNavigationConfirmed OnStartNavigationConfirmed;
}
