using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AnchorManager : MonoBehaviour
{
    [Header("Anchor Manager")]
    [SerializeField]
    private SpatialAnchorManager cloudManager;

    [Header("UX")]
    [SerializeField]
    private AnchorPosition anchorPositionPrefab;

    private CloudSpatialAnchor currentCloudAnchor;
    private AnchorLocateCriteria anchorLocateCriteria;
    private CloudSpatialAnchorWatcher currentWatcher;

    private readonly Queue<Action> dispatchQueue = new Queue<Action>();

    #region Unity Lifecycle
    void Awake()
    {

    }

    // Start is called before the first frame update
    void Start()
    {
        // Subscribe to Azure Spatial Anchor events
        cloudManager.AnchorLocated += HandleAnchorLocated;
        // Notify subscribers
        //cloudManager.LocateAnchorsCompleted += HandleLocateAnchorsCompleted;

        cloudManager.SessionUpdated += (sender, args) =>
        {
            Debug.Log($"Spatial Anchors Status Updated to: {args.Status}");
        };
        cloudManager.LogDebug += (sender, args) =>
        {
            Debug.Log($"CloudManager Debug: {args.Message}");
        };

        // Subscribe to StopWatcher event
        AnchorMap.CanStopWatcher += StopCurrentWatcher;

    }

    // Update is called once per frame
    void Update()
    {
        lock (dispatchQueue)
        {
            if (dispatchQueue.Count > 0)
            {
                dispatchQueue.Dequeue()();
            }
        }
    }

    void OnDestroy()
    {
        if (cloudManager != null && cloudManager.Session != null)
        {
            cloudManager.DestroySession();
        }

        if (currentWatcher != null)
        {
            currentWatcher.Stop();
            currentWatcher = null;
        }

        StopAzureSession();
    }
    #endregion

    #region Public Methods
    public void FindAnchor()
    {
        // get selected information about the spatial anchor from selectEntryPanel
        string[] idsToQuery = AnchorMap.Instance.GetSelectEntryInformation();
        
        // query the cloud spatial anchor by anchor id to get the localCloudAnchor
        FindAzureAnchor(idsToQuery);
        
    }

    public void CreateAnchor()
    {
        // initialize anchorPosition
        var anchorPosToCreate = AnchorMap.Instance.UpdateNewAnchorPositionInformation();

        // create cloud spatial anchor
        CreateAzureAnchor(anchorPosToCreate);
    }

    public void startNavigationWorkflow()
    {
        var idsToQuery = AnchorMap.Instance.GetOriginDestinationInformation();
        FindAzureAnchor(idsToQuery);
        //OnAllAnchorFound += HandleOnAllAnchorFound;

    }
    #endregion


    #region Azure Spatial Anchor Functions
    private async void StartAzureSession()
    {
        Debug.Log("\nAnchorModuleScript.StartAzureSession()");
        // Notify AnchorFeedbackScript
        OnStartASASession?.Invoke();

        Debug.Log("Starting Azure session... please wait...");

        if (cloudManager.Session == null)
        {
            // Creates a new session if one does not exist
            await cloudManager.CreateSessionAsync();
        }

        // Starts the session if not already started
        await cloudManager.StartSessionAsync();

        Debug.Log("Azure session started successfully");
    }

    private async void StopAzureSession()
    {
        Debug.Log("\nAnchorModuleScript.StopAzureSession()");
        // Notify AnchorFeedbackScript
        OnEndASASession?.Invoke();

        Debug.Log("Stopping Azure session... please wait...");

        // Stops any existing session
        cloudManager.StopSession();

        // Resets the current session if there is one, and waits for any active queries to be stopped
        await cloudManager.ResetSessionAsync();

        Debug.Log("Azure session stopped successfully");
    }

    private async void FindAzureAnchor(string[] idsToQuery)
    {

        if (cloudManager.Session == null)
        {
            // Creates a new session if one does not exist
            Debug.Log("\ncloudManager.CreateSessionAsync()");
            await cloudManager.CreateSessionAsync();
        }

        // Starts the session if not already started
        Debug.Log("\ncloudManager.StartSessionAsync()");
        await cloudManager.StartSessionAsync();

        // Notify AnchorFeedbackScript
        OnFindASAAnchor?.Invoke();

        // create a query criteria by anchor id
        Debug.Log($"Trying to finding anchors with anchor-id {idsToQuery}");
        anchorLocateCriteria = new AnchorLocateCriteria { Identifiers = idsToQuery };

        // query the cloud anchor
        // Start watching for Anchors
        if (cloudManager != null && cloudManager.Session != null)
        {
            Debug.Log("\ncurrentWatcher = cloudManager.Session.CreateWatcher(anchorLocateCriteria)");
            currentWatcher = cloudManager.Session.CreateWatcher(anchorLocateCriteria);
        }
        else
        {
            Debug.Log("Attempt to create watcher failed, no session exists");
            currentWatcher = null;
        }

    }

    private async void CreateAzureAnchor(AnchorPosition anchorPos)
    {
        // change the color of the currentAnchor to inprogress
        anchorPos.AnchorInProgress();

        if (cloudManager.Session == null)
        {
            // Creates a new session if one does not exist
            Debug.Log("\ncloudManager.CreateSessionAsync()");
            await cloudManager.CreateSessionAsync();
        }

        // Starts the session if not already started
        Debug.Log("\ncloudManager.StartSessionAsync()");
        await cloudManager.StartSessionAsync();

        // Notify AnchorFeedbackScript
        OnCreateAnchorStarted?.Invoke();

        // Create local cloud anchor
        var localCloudAnchor = new CloudSpatialAnchor();

        // Create native XR anchor at the location of the object
        anchorPos.gameObject.CreateNativeAnchor();
        Debug.Log("anchorPosition.gameObject.CreateNativeAnchor()");

        // create a cloud anchor at the position of the local anchor
        localCloudAnchor.LocalAnchor = anchorPos.gameObject.FindNativeAnchor().GetPointer();
        Debug.Log("anchorPosition.gameObject.FindNativeAnchor().GetPointer()");

        // Check to see if we got the local XR anchor pointer
        if (localCloudAnchor.LocalAnchor == IntPtr.Zero)
        {
            Debug.Log("Didn't get the local anchor...");
            return;
        }
        else
        {
            Debug.Log("Local anchor created");
        }

        // Set expiration (when anchor will be deleted from Azure)
        localCloudAnchor.Expiration = DateTimeOffset.Now.AddDays(90);

        // upload cloud anchor to the cloud
        while (!cloudManager.IsReadyForCreate)
        {
            // check with the create progress
            await Task.Delay(330);
            var createProgress = cloudManager.SessionStatus.RecommendedForCreateProgress;
            UnityDispatcher.InvokeOnAppThread(() => Debug.Log($"Move your device to capture more environment data: {createProgress:0%}"));
        }
        Debug.Log("cloudManager is ready.");

        try
        {

            // Actually save
            Debug.Log("await cloudManager.CreateAnchorAsync(localCloudAnchor)");
            await cloudManager.CreateAnchorAsync(localCloudAnchor);
            Debug.Log("Anchor created!");

            // Store
            currentCloudAnchor = localCloudAnchor;
            localCloudAnchor = null;

            // Success?
            var success = currentCloudAnchor != null;

            if (success)
            {
                // update the spaital anchor id of the currentSpatialAnchor
                Debug.Log($"Azure anchor with ID '{currentCloudAnchor.Identifier}' created successfully");
                anchorPos.SpatialAnchorObject.SpatialAnchorId = currentCloudAnchor.Identifier;

                // Update the current Azure anchor ID
                Debug.Log($"Current Azure anchor ID updated to '{currentCloudAnchor.Identifier}'");

                OnCreateAnchorSucceeded?.Invoke();
            }
            else
            {
                Debug.Log($"Failed to save cloud anchor with ID '{currentCloudAnchor.Identifier}' to Azure");
                // Notify AnchorFeedbackScript
                OnCreateAnchorFailed?.Invoke();

            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex.ToString());
        }

        // StopAzureSession();  
    }
    #endregion

    #region Helper Functions

    private void QueueOnUpdate(Action updateAction)
    {
        lock (dispatchQueue)
        {
            dispatchQueue.Enqueue(updateAction);
        }
    }
    #endregion


    #region Event Handlers
    private void HandleAnchorLocated(object sender, AnchorLocatedEventArgs args)
    {
        QueueOnUpdate(new Action(() => Debug.Log($"Anchor recognized as a possible Azure anchor")));

        if (args.Status == LocateAnchorStatus.Located || args.Status == LocateAnchorStatus.AlreadyTracked)
        {
            

            QueueOnUpdate(() =>
            {
                Debug.Log($"Azure anchor located successfully");
                currentCloudAnchor = args.Anchor;
                Debug.Log(args.Anchor.Identifier);

#if WINDOWS_UWP || UNITY_WSA
                // HoloLens: The position will be set based on the unityARUserAnchor that was located.

                

                // Create a local anchor at the location of the object in question
                var newAnchorPosition = Instantiate(anchorPositionPrefab);
                newAnchorPosition.gameObject.CreateNativeAnchor();

                // Notify AnchorFeedbackScript
                OnCreateLocalAnchor?.Invoke();

                // On HoloLens, if we do not have a cloudAnchor already, we will have already positioned the
                // object based on the passed in worldPos/worldRot and attached a new world anchor,
                // so we are ready to commit the anchor to the cloud if requested.
                // If we do have a cloudAnchor, we will use it's pointer to setup the world anchor,
                // which will position the object automatically.
                if (currentCloudAnchor == null)
                {
                    return;
                }
                Debug.Log("Local anchor position successfully set to Azure anchor position");

                newAnchorPosition.gameObject.GetComponent<UnityEngine.XR.WSA.WorldAnchor>().SetNativeSpatialAnchorPtr(currentCloudAnchor.LocalAnchor);

#elif UNITY_ANDROID || UNITY_IOS
                Pose anchorPose = Pose.identity;
                anchorPose = currentCloudAnchor.GetPose();

                Debug.Log($"Setting object to anchor pose with position '{anchorPose.position}' and rotation '{anchorPose.rotation}'");
                transform.position = anchorPose.position;
                transform.rotation = anchorPose.rotation;

                // Create a native anchor at the location of the object in question
                gameObject.CreateNativeAnchor();

                // Notify AnchorFeedbackScript
                OnCreateLocalAnchor?.Invoke();

#endif
                Debug.Log($"Local anchor position at '{newAnchorPosition.transform.position}'");
                OnASAAnchorFound?.Invoke(currentCloudAnchor.Identifier, newAnchorPosition);

            });
        }
        else
        {
            QueueOnUpdate(new Action(() => Debug.Log($"Attempt to locate Anchor with ID '{args.Identifier}' failed, locate anchor status was not 'Located' but '{args.Status}'")));
        }
    }

    private void StopCurrentWatcher()
    {
        currentWatcher?.Stop();
    }
    #endregion

    #region Public Events
    public delegate void StartASASessionDelegate();
    public static event StartASASessionDelegate OnStartASASession;

    public delegate void EndASASessionDelegate();
    public static event EndASASessionDelegate OnEndASASession;

    public delegate void CreateAnchorDelegate();
    public static event CreateAnchorDelegate OnCreateAnchorStarted;
    public static event CreateAnchorDelegate OnCreateAnchorSucceeded;
    public static event CreateAnchorDelegate OnCreateAnchorFailed;

    public delegate void CreateLocalAnchorDelegate();
    public static event CreateLocalAnchorDelegate OnCreateLocalAnchor;

    public delegate void RemoveLocalAnchorDelegate();
    public static event RemoveLocalAnchorDelegate OnRemoveLocalAnchor;

    public delegate void FindAnchorDelegate();
    public static event FindAnchorDelegate OnFindASAAnchor;

    public delegate void AnchorFoundDelegate(string anchorId, AnchorPosition anchorPos);
    public static event AnchorFoundDelegate OnASAAnchorFound;

    public delegate void AnchorLocatedDelegate();
    public static event AnchorLocatedDelegate OnASAAnchorLocated;

    public delegate void AllAnchorFoundDelegate();
    public static event AllAnchorFoundDelegate OnAllAnchorFound;
    #endregion
}
