using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityScript.Steps;
using Debug = UnityEngine.Debug;

public class AnchorMap : MonoBehaviour
{
    public static AnchorMap Instance;
    
    // a dictionary of created spatial anchors - key: anchor name; value: spatial anchor object
    private Dictionary<string, SpatialAnchor> CreatedSpatialAnchors = new Dictionary<string, SpatialAnchor>();

    // a dictionary of created edges - key: id; value: edge object
    private Dictionary<string, Edge> CreatedEdges = new Dictionary<string, Edge>();

    private List<AnchorPosition> SceneAnchorPositions = new List<AnchorPosition>();

    // a dictionary of adjacency list
    private Dictionary<string, Dictionary<string, double>> AdjacencyList = new Dictionary<string, Dictionary<string, double>>();

    // keep a track of current and previous spatial anchor
    private AnchorPosition previousAnchorPosition;
    private AnchorPosition currentAnchorPosition;
    private Edge currentEdge;
    private Dictionary<string, SpatialAnchor> spatialAnchorsInQuery = new Dictionary<string, SpatialAnchor>();
    private LinkedList<string> anchorList = new LinkedList<string>();
    private bool inNavigationMode = false;

    [Header("Manager")]
    [SerializeField]
    private DataManager dataManager;

    [Header("UX")]
    [SerializeField]
    private GameObject navigationModeButton;
    [SerializeField]
    private GameObject selectEntryPanel;
    [SerializeField]
    private GameObject createAnchorPanel;
    [SerializeField]
    private GameObject navigationPanel;
    [SerializeField]
    private TMP_Dropdown spatialAnchorDropdown;
    [SerializeField]
    private TMP_Text anchorNameInputField;
    [SerializeField]
    private InteractableToggleCollection anchorTypeRadialSet;
    [SerializeField]
    private TMP_Dropdown originDropdown;
    [SerializeField]
    private TMP_Dropdown destDropdown;
    [SerializeField]
    private GameObject indicator;
    [SerializeField]
    private DialogController dialogController;
    [SerializeField]
    private GameObject progressIndicatorRotatingOrbsGo = null;

    private IProgressIndicator progressIndicatorRotatingOrbs;


    [Header("Prefabs")]
    [SerializeField]
    private AnchorPosition anchorPositionPrefab;


    #region Unity Lifecycle
    void Awake()
    {
        Instance = this;

        // subscribe to spatial anchor events
        AnchorManager.OnCreateAnchorSucceeded += HandleCreateAnchorSucceeded;
        AnchorManager.OnCreateAnchorFailed += HandleCreateAnchorFailed;
        AnchorManager.OnASAAnchorFound += HandleAnchorFound;

        // subscribe to dialog events
        DialogController.OnStartCreationConfirmed += StartAnchorCreationWorkflow;
        DialogController.OnStartNavigationConfirmed += StartNavigation;

        // subscribe to anchor object events
        AnchorPosition.OnEnterAnchorPosition += UpdateCurrentPosition;

        // progress indicator
        progressIndicatorRotatingOrbs = progressIndicatorRotatingOrbsGo.GetComponent<IProgressIndicator>();


    }


    void OnDestroy()
    {
        // unsubscribe to spatial anchor events
        AnchorManager.OnCreateAnchorSucceeded -= HandleCreateAnchorSucceeded;
        AnchorManager.OnCreateAnchorFailed -= HandleCreateAnchorFailed;
        AnchorManager.OnASAAnchorFound -= HandleAnchorFound;

        // unsubscribe to dialog events
        DialogController.OnStartCreationConfirmed -= StartAnchorCreationWorkflow;
        DialogController.OnStartNavigationConfirmed -= StartNavigation;

        // unsubscribe to anchor object events
        AnchorPosition.OnEnterAnchorPosition -= UpdateCurrentPosition;


    }
    #endregion

    #region Public Methods
    public void InitScene()
    {
        Debug.Log("Initialize scene.");
        // clear dropdown entries
        spatialAnchorDropdown.options.Clear();
        originDropdown.options.Clear();
        destDropdown.options.Clear();

        // populate created spatials anchors to the dropdown menu
        if (CreatedSpatialAnchors.Count > 0)
        {
            foreach (KeyValuePair<string, SpatialAnchor> anchor in CreatedSpatialAnchors)
            {
                spatialAnchorDropdown.options.Add(new TMP_Dropdown.OptionData() { text = anchor.Value.Name });

                // add only the main anchors
                if (anchor.Value.Type == anchorTypeRadialSet.ToggleList[0].name)
                {
                    // add entry to the dropdown
                    originDropdown.options.Add(new TMP_Dropdown.OptionData() { text = anchor.Value.Name });
                    destDropdown.options.Add(new TMP_Dropdown.OptionData() { text = anchor.Value.Name });
                }
            }

            // enable navigation mode
            navigationModeButton.GetComponent<PressableButtonHoloLens2>().enabled = true;
            navigationModeButton.GetComponent<Interactable>().enabled = true;
        }

        // initialize anchor map
        InitMap();

    }

    public void ResetScene()
    {
        Debug.Log("Reset scene.");
        foreach (AnchorPosition anchorPos in SceneAnchorPositions)
        {
            Debug.Log($"Destroy anchorPos: {anchorPos.SpatialAnchorObject.Name}");
            Destroy(anchorPos.gameObject);
        }
        SceneAnchorPositions.Clear();

        // destroy current/previousAnchorPosition
        Debug.Log("Destroy currentAnchorPosition");
        if (currentAnchorPosition != null)
        {
            Destroy(currentAnchorPosition.gameObject);
        }

        Debug.Log("Destroy previousAnchorPosition");
        if (previousAnchorPosition != null)
        {
            Destroy(previousAnchorPosition.gameObject);
        }

        // reset anchorsToQuery
        Debug.Log("Reset spatialAnchorsInQuery list");
        spatialAnchorsInQuery.Clear();

        // stop watcher
        CanStopWatcher?.Invoke();

        // stop indicator
        CloseProgressIndicator(progressIndicatorRotatingOrbs);
    }

    public void CheckExistAnchors()
    {
        if (SceneAnchorPositions.Count > 0)
        {
            // move createAnchorPanel to the front of user
            createAnchorPanel.transform.position = Camera.main.transform.position + Camera.main.transform.forward / 2;
            createAnchorPanel.SetActive(true);
            InitializeAnchorProfile();

        }
        else if (CreatedSpatialAnchors.Count > 0)
        {
            if (inNavigationMode)
            {
                navigationPanel.transform.position = Camera.main.transform.position + Camera.main.transform.forward / 2;
                navigationPanel.SetActive(true);

            }
            else
            {
                selectEntryPanel.transform.position = Camera.main.transform.position + Camera.main.transform.forward / 2;
                selectEntryPanel.SetActive(true);
            }
        }
        else
        {
            SetInNavigationMode(false);
            dialogController.OpenDialog("No Existing Spatial Anchors Found", "We did not find spatial anchors from the cloud. Start creating spatial anchors and uploading them to the cloud for navigation.\n Do you want to create a new spatial anchor now?", "create");
        }
    }

    public void SetInNavigationMode(bool state)
    {
        inNavigationMode = state;
    }

    public string[] GetSelectEntryInformation()
    {
        // get selected information about the spatial anchor from selectEntryPanel
        string currentAnchorName = spatialAnchorDropdown.options[spatialAnchorDropdown.value].text;
        Debug.Log(currentAnchorName);

        // query the spatial anchor table by name to get the SpatialAnchor as the currentAnchor
        var spatialAnchorToQuery = CreatedSpatialAnchors[currentAnchorName];

        // initialize in query list
        spatialAnchorsInQuery = new Dictionary<string, SpatialAnchor>();

        // construct the query string
        Debug.Log($"Trying to finding object {spatialAnchorToQuery.Name} with anchor-id {spatialAnchorToQuery.SpatialAnchorId}");
        string[] idsToQuery = new string[] { spatialAnchorToQuery.SpatialAnchorId };
        spatialAnchorsInQuery.Add(spatialAnchorToQuery.SpatialAnchorId, spatialAnchorToQuery);

        return idsToQuery;
    }

    public AnchorPosition UpdateNewAnchorPositionInformation()
    {
        // get the information from the createAnchorPanel
        // update information of the currentSpatialAnchor
        var newSpatialAnchor = new SpatialAnchor(anchorNameInputField.text);
        newSpatialAnchor.Type = anchorTypeRadialSet.ToggleList[anchorTypeRadialSet.CurrentIndex].name;
        newSpatialAnchor.AnchorPosition = currentAnchorPosition.transform.position.ToString();
        newSpatialAnchor.WorldToLocalMatrix = currentAnchorPosition.transform.worldToLocalMatrix.ToString();

        // if there is previousSpatial anchor
        if (previousAnchorPosition != null)
        {
            // update information of the edge
            currentEdge.ConnectedName = anchorNameInputField.text;
            currentEdge.Distance = Vector3.Distance(currentAnchorPosition.transform.position, previousAnchorPosition.transform.position);
        }

        // initialize anchorPosition
        currentAnchorPosition.Init(newSpatialAnchor);

        // disable tap to place of the currentAnchorPosition
        currentAnchorPosition.GetComponent<TapToPlace>().enabled = false;

        // reset anchorNameInputFile TEXT
        anchorNameInputField.text = "";

        return currentAnchorPosition;
    }

    public string[] GetOriginDestinationInformation()
    {
        // show indicator
        progressIndicatorRotatingOrbsGo.SetActive(true);
        OpenProgressIndicator(progressIndicatorRotatingOrbs);
        //ToggleIndicator(progressIndicatorRotatingOrbs);
        // reset queried anchors
        spatialAnchorsInQuery = new Dictionary<string, SpatialAnchor>();
        Debug.Log($"Get origin information");
        string originName = originDropdown.options[originDropdown.value].text;
        string destName = destDropdown.options[destDropdown.value].text;

        // find the closest path
        FindShortestPath(originName, destName);
        string[] idsToQuery = new string[anchorList.Count];

        int i = 0;
        for(LinkedListNode<string> it = anchorList.First; it != null;)
        {
            var anchor = GetSpatialAnchor(it.Value);
            spatialAnchorsInQuery.Add(anchor.SpatialAnchorId, anchor);
            idsToQuery[i] = anchor.SpatialAnchorId;
            i++;
            it = it.Next;
        }

        Debug.Log($"Found '{idsToQuery.Length}' anchors to be found.");
        return idsToQuery;
    }
    #endregion




    #region Event Handlers
    private void HandleCreateAnchorFailed()
    {
        Debug.Log($"Failed to upload '{currentAnchorPosition.SpatialAnchorObject.Name}' -- '{currentAnchorPosition.SpatialAnchorObject.SpatialAnchorId}' to the cloud.");
        CancelAnchorProfile();
    }

    private async void HandleCreateAnchorSucceeded()
    {
        // change the color the currentAnchor to succeed
        currentAnchorPosition.AnchorConfirmed();

        // upload spatial anchor entity to azure table
        var uploadTableSuccess = await dataManager.UploadOrUpdate(currentAnchorPosition.SpatialAnchorObject);
        if (uploadTableSuccess)
        {
            Debug.Log($"Successful upload '{currentAnchorPosition.SpatialAnchorObject.SpatialAnchorId}' to cloud table");
        }
        else
        {
            Debug.Log($"Failed to upload '{currentAnchorPosition.SpatialAnchorObject.SpatialAnchorId}' to cloud table");
            CancelAnchorProfile();
            return;
        }

        if (previousAnchorPosition != null)
        {
            // upload spatial anchor entity to azure table
            currentEdge.ConnectedSpatialAnchorId = currentAnchorPosition.SpatialAnchorObject.SpatialAnchorId;
            currentEdge.Id = currentEdge.Name + ',' + currentEdge.ConnectedName;
            currentEdge.RowKey = currentEdge.Id;
            // upload Edge entity to azure table
            uploadTableSuccess = await dataManager.UpdateAdjacentTable(currentEdge);
            if (uploadTableSuccess)
            {
                Debug.Log($"Successful upload '{currentEdge.Id}' to cloud table");
            }
            else
            {
                Debug.Log($"Failed to upload '{currentEdge.Id}' to cloud table");
                CancelAnchorProfile();
                // Notify AnchorFeedbackScript
                return;
            }
        }

        Debug.Log("Add current anchorPosition to dict");
        // add the currentAnchorPosition to the dictionary
        SceneAnchorPositions.Add(currentAnchorPosition);
        Debug.Log($"Currently added '{SceneAnchorPositions.Count}' anchors during this session");
        // add the currentSpatialAnchor to the dictionary
        AddSpatialAnchor(currentAnchorPosition.SpatialAnchorObject);
        // add edge
        CreateEdge(previousAnchorPosition.SpatialAnchorObject, currentAnchorPosition.SpatialAnchorObject, Vector3.Distance(previousAnchorPosition.transform.position, currentAnchorPosition.transform.position));

        // add entries to dropdown list
        Debug.Log("Add created anchor to dropdown list");
        spatialAnchorDropdown.options.Add(new TMP_Dropdown.OptionData() { text = currentAnchorPosition.SpatialAnchorObject.Name });
        originDropdown.options.Add(new TMP_Dropdown.OptionData() { text = currentAnchorPosition.SpatialAnchorObject.Name });
        destDropdown.options.Add(new TMP_Dropdown.OptionData() { text = currentAnchorPosition.SpatialAnchorObject.Name });
    }

    private void HandleAnchorFound(string anchorId, AnchorPosition anchorPos)
    {
        
        anchorPos.Init(spatialAnchorsInQuery[anchorId]);
        anchorPos.AnchorConfirmed();
        if (!SceneAnchorPositions.Contains(anchorPos))
        {
            SceneAnchorPositions.Add(anchorPos);
            Debug.Log($"Add anchor position of '{anchorId}' to the scene");
        } else
        {
            Destroy(anchorPos);
        }

        currentAnchorPosition = anchorPos;


        Debug.Log($"Anchor '{spatialAnchorsInQuery[anchorId].Name}' have been found, removing from the query list ");
        spatialAnchorsInQuery.Remove(anchorId);


        if (spatialAnchorsInQuery.Count > 0)
        {
            Debug.Log($"continue finding '{spatialAnchorsInQuery.Count}' more spatial anchors... ... ...");
        } else
        {
            Debug.Log("successully found all spatial anchors queried");
            if (inNavigationMode)
            {
                dialogController.OpenDialog("Ready to start the navigation", "Now we have successfully located both your selected origin and destination and are ready to start navigating. Do you want to start the navigation now?", "navigate");
                progressIndicatorRotatingOrbsGo.SetActive(false);
                CloseProgressIndicator(progressIndicatorRotatingOrbs);
                //ToggleIndicator(progressIndicatorRotatingOrbs);
            } else
            {
                // show indicator
                indicator.GetComponent<PositionIndicator>().SetTarget(currentAnchorPosition.transform);
                indicator.SetActive(true);
                // show dialog confirm whether wanting to create a new anchor
                dialogController.OpenDialog("Creating a new spatial anchor", "Do you want to create a new spatial anchor?", "create");
            }
            CanStopWatcher?.Invoke();
        }
    }

    private void StartAnchorCreationWorkflow()
    {
        // hide indicator
        indicator.SetActive(false);
        // move createAnchorPanel to the front of user
        createAnchorPanel.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 2;
        // set createAnchorPanel active
        createAnchorPanel.SetActive(true);
        // initialize anchor profile
        InitializeAnchorProfile();
    }

    private void StartNavigation()
    {
        foreach (AnchorPosition anchorPos in SceneAnchorPositions)
        {
            // change the material of anchorPos to navMaterial
            anchorPos.AnchorNavigation();
            // enlarge boxcollider of anchorPos for updating user's position
            anchorPos.EnlargeBoxCollider();
            //if (anchorPos.SpatialAnchorObject.Name == anchorList[1])
            if (anchorPos.SpatialAnchorObject.Name == anchorList.First.Value)
            {
                // set indicator target to the next
                indicator.GetComponent<PositionIndicator>().SetTarget(anchorPos.transform);
            }
            if (anchorPos.SpatialAnchorObject.Name == anchorList.Last.Value)
            {
                // set the destination anchorPos as target
                anchorPos.SetAsTarget(true);
            }
        }
        indicator.SetActive(true);

    }

    private void GenerateTransitionLines()
    {
        LinkedListNode<string> it = anchorList.First;

        while(it != anchorList.Last)
        {
            AnchorPosition baseAnchor = null;
            AnchorPosition targetAnchor = null;
            foreach(AnchorPosition anchorPos in SceneAnchorPositions)
            {
                if (anchorPos.SpatialAnchorObject.Name == it.Value)
                {
                    baseAnchor = anchorPos;
                }

                if (anchorPos.SpatialAnchorObject.Name == it.Next.Value)
                {
                    targetAnchor = anchorPos;
                }
            }

            if (baseAnchor!=null && targetAnchor!=null)
            {
                baseAnchor.ConnectToAnchor(targetAnchor);
            } else
            {
                Debug.Log($"Cannot find the anchor connecting to {it.Value}");
                break;
            }

            it = it.Next;
        }
    }

    private void UpdateCurrentPosition(AnchorPosition anchorPos)
    {
        if(inNavigationMode) 
        { 
            Debug.Log($"User's current position is around '{anchorPos.SpatialAnchorObject.Name}'");
            Debug.Log(anchorList.Count);
            // remove anchorPos from anchor list
            anchorList.Remove(anchorPos.SpatialAnchorObject.Name);
            anchorPos.AnchorPassed();
            anchorPos.ResetBoxCollider();

            // update target to the next anchor
            if (anchorList.Count > 0)
            {
                foreach (AnchorPosition sceneAnchorPos in SceneAnchorPositions)
                {
                    //if(sceneAnchorPos.SpatialAnchorObject.Name == anchorList.First anchorList[1])
                    if (sceneAnchorPos.SpatialAnchorObject.Name == anchorList.First.Value)
                    {
                        Debug.Log($"Set the target to the next anchor '{sceneAnchorPos.SpatialAnchorObject.Name}'");
                        indicator.GetComponent<PositionIndicator>().SetTarget(sceneAnchorPos.transform);
                    }
                }
            } else
            {
                SetInNavigationMode(false);
                foreach(AnchorPosition sceneAnchorPos in SceneAnchorPositions)
                {
                    // reset box collider
                    sceneAnchorPos.ResetBoxCollider();
                    
                }
                dialogController.OpenDialogSimple("You've reached your destination!!", "Congratulations! You have successfully reached your destination.");
                indicator.SetActive(false);
            }
        }
    }
    #endregion

    #region Public Methods -- class-related
    public SpatialAnchor GetSpatialAnchor(string name)
    {
        return CreatedSpatialAnchors[name];
    }

    public Edge GetEdge(string name)
    {
        return CreatedEdges[name];
    }

    public void AddSpatialAnchor(SpatialAnchor anchor)
    {
        if (CreatedSpatialAnchors == null)
        {
            CreatedSpatialAnchors = new Dictionary<string, SpatialAnchor>();
        }
        if (!CreatedSpatialAnchors.ContainsKey(anchor.Name))
        {
            CreatedSpatialAnchors.Add(anchor.Name, anchor);
        }
    }

    public void AddEdge(Edge edge)
    {
        if (CreatedEdges == null)
        {
            CreatedEdges = new Dictionary<string, Edge>();
        }
        if (!CreatedEdges.ContainsKey(edge.Id))
        {
            CreatedEdges.Add(edge.Id, edge);
        }
    }

    public void CreateEdge(SpatialAnchor anchor1, SpatialAnchor anchor2, double distance)
    {
        if (CreatedEdges == null)
        {
            CreatedEdges = new Dictionary<string, Edge>();
        }
        if ((!CreatedEdges.ContainsKey(anchor1.SpatialAnchorId + ',' + anchor2.SpatialAnchorId)) && (!CreatedEdges.ContainsKey(anchor2.SpatialAnchorId + ',' + anchor1.SpatialAnchorId)))
        {
            Edge edge = new Edge();
            edge.Name = anchor1.Name;
            edge.SpatialAnchorId = anchor1.SpatialAnchorId;
            edge.ConnectedName = anchor2.Name;
            edge.ConnectedSpatialAnchorId = anchor2.SpatialAnchorId;
            edge.Id = anchor1.Name + ',' + anchor2.Name;
            edge.RowKey = edge.Id;
            edge.Distance = distance;

            CreatedEdges.Add(edge.Id, edge);
        }

    }

    public void AddAnchorPositionToScene(AnchorPosition anchorPos)
    {
        SceneAnchorPositions.Add(anchorPos);
    }

    public AnchorPosition GetAnchorPosition(string name)
    {
        foreach(AnchorPosition anchorPos in SceneAnchorPositions)
        {
            if (anchorPos.SpatialAnchorObject.Name == name)
            {
                return anchorPos;
            }
        }

        return null;
    }
    #endregion


    #region Private Methods
    private void InitializeAnchorProfile()
    {   // assign currentAnchorPosition to previousAnchorPosition
        previousAnchorPosition = currentAnchorPosition;
        // create an AnchorPosition instance in front of the camera
        currentAnchorPosition = Instantiate(anchorPositionPrefab, Camera.main.transform.position + Camera.main.transform.forward * 2, Quaternion.LookRotation(Camera.main.transform.forward));
        Debug.Log($"Initialized a new anchorPosition '{currentAnchorPosition}'");
        // if has previousSpatialAnchor
        if (previousAnchorPosition != null)
        {
            // create a new Edge class as the currentEdge
            currentEdge = new Edge();
            currentEdge.Name = previousAnchorPosition.SpatialAnchorObject.Name;
            currentEdge.SpatialAnchorId = previousAnchorPosition.SpatialAnchorObject.SpatialAnchorId;
        }

    }

    public void CancelAnchorProfile()
    {

        // remove the instance of AnchorPosition
        Destroy(currentAnchorPosition.gameObject);
        // assign previousSpatialAnchor to currentSpatialAnchor
        currentAnchorPosition = previousAnchorPosition;
        previousAnchorPosition = SceneAnchorPositions[SceneAnchorPositions.Count-1];
    }

    private void InitMap()
    {
        Debug.Log("Initialize Adjacency Map");
        if (AdjacencyList == null)
        {
            AdjacencyList = new Dictionary<string, Dictionary<string, double>>();
        } 
        foreach (KeyValuePair<string, Edge> entry in CreatedEdges)
        {
            Edge edge = entry.Value;
            if (!AdjacencyList.ContainsKey(edge.Name))
            {
                Dictionary<string, double> edgeList = new Dictionary<string, double>();
                edgeList.Add(edge.ConnectedName, edge.Distance);
                AdjacencyList.Add(edge.Name, edgeList);
            }
            else if (!AdjacencyList[edge.Name].ContainsKey(edge.ConnectedName))
            {
                AdjacencyList[edge.Name].Add(edge.ConnectedName, edge.Distance);
            }

            if (!AdjacencyList.ContainsKey(edge.ConnectedName))
            {
                Dictionary<string, double> edgeList = new Dictionary<string, double>();
                edgeList.Add(edge.Name, edge.Distance);
                AdjacencyList.Add(edge.ConnectedName, edgeList);
            }
            else if (!AdjacencyList[edge.ConnectedName].ContainsKey(edge.Name))
            {
                AdjacencyList[edge.ConnectedName].Add(edge.Name, edge.Distance);
            }
        }
    }

    private void FindShortestPath(string origin, string dest)
    {
        anchorList.Clear();
        List<string> idList = new List<string>();
        List<string> vertexSet = new List<string>();
        List<double> dist = new List<double>();
        List<string> prev = new List<string>();

        foreach(KeyValuePair<string, Dictionary<string, double>> node in AdjacencyList)
        {
            if (node.Key == origin)
            {
                dist.Add(0);
            } else
            {
                dist.Add(double.PositiveInfinity);
            }
            prev.Add("");
            idList.Add(node.Key);
            vertexSet.Add(node.Key);
        }

        
        while (vertexSet.Count > 0)
        {

            double minDist = double.PositiveInfinity;
            var u = "";
            // find u in vertexSet with the smallest dist
            foreach(string vertex in vertexSet)
            {
                int vertexId = idList.IndexOf(vertex);
                if (dist[vertexId] < minDist)
                {
                    minDist = dist[vertexId];
                    u = idList[vertexId];
                }
            }

            vertexSet.Remove(u);


            if (u == dest)
            {
                break;
            }

            foreach (KeyValuePair<string, double> d in AdjacencyList[u])
            {
                double alt = minDist + d.Value;
                int id = idList.IndexOf(d.Key);
                if (alt < dist[id])
                {
                    dist[id] = alt;
                    prev[id] = u;
                }
            }
        }

        // reverse to find the shortest path nodes
        //List<string> anchorNameList = new List<string>();
        //LinkedList<string> anchorNameList = new LinkedList<string>();
        int destId = idList.IndexOf(dest);
        var cand = dest;


        if (prev[destId] != "" || cand == origin)
        {
            while (cand != "")
            {
                anchorList.AddFirst(cand);
                //anchorNameList.Insert(0, cand);
                destId = idList.IndexOf(cand);
                cand = prev[destId];
            }
        }

        Debug.Log("Found shortest path.");
        foreach(string name in anchorList)
        {
            Debug.Log(name);
        }
    }

    private async void ToggleIndicator(IProgressIndicator indicator)
    {
        // If the indicator is opening or closing, wait for that to finish before trying to open / close it
        // Otherwise the indicator will display an error and take no action
        await indicator.AwaitTransitionAsync();

        switch (indicator.State)
        {
            case ProgressIndicatorState.Closed:
                await indicator.OpenAsync();
                break;

            case ProgressIndicatorState.Open:
                await indicator.CloseAsync();
                break;
        }
    }

    private async void CloseProgressIndicator(IProgressIndicator indicator)
    {
        await indicator.AwaitTransitionAsync();

        if (indicator.State == ProgressIndicatorState.Open)
        {
            await indicator.CloseAsync();
        }
    }

    private async void OpenProgressIndicator(IProgressIndicator indicator)
    {
        await indicator.AwaitTransitionAsync();

        if (indicator.State == ProgressIndicatorState.Closed)
        {
            await indicator.OpenAsync();
        }
    }
    #endregion

    #region Public Events
    public delegate void CanStopWatcherDelegate();
    public static event CanStopWatcherDelegate CanStopWatcher;
    #endregion

}
