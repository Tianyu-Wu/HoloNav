using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class DataManager : MonoBehaviour
{
    public bool IsReady { get; private set; }

    [Header("Base Settings")]
    [SerializeField]
    private string connectionString;
    [Header("Table Settings")]
    [SerializeField]
    private string spatialAnchorTableName = "spatialAnchors";
    [SerializeField]
    private string adjacentListTableName = "adjacentList";
    [SerializeField]
    private string partitionKey = "main";
    [SerializeField]
    private bool tryCreateTableOnStart = true;

    [Header("UX")]
    [SerializeField]
    private DialogController dialogController;

    [Header("Events")]
    [SerializeField]
    private UnityEvent onDataManagerReady;

    private CloudStorageAccount storageAccount;
    private CloudTableClient cloudTableClient;
    private CloudTable spatialAnchorTable;
    private CloudTable adjacentListTable;
    

    private async void Awake()
    {
        // connect to azure storage
        storageAccount = CloudStorageAccount.Parse(connectionString);
        cloudTableClient = storageAccount.CreateCloudTableClient();
        spatialAnchorTable = cloudTableClient.GetTableReference(spatialAnchorTableName);
        adjacentListTable = cloudTableClient.GetTableReference(adjacentListTableName);
        if (tryCreateTableOnStart)
        {
            try
            {
                if (await spatialAnchorTable.CreateIfNotExistsAsync())
                {
                    Debug.Log($"Created table {spatialAnchorTableName}.");
                }
                if (await adjacentListTable.CreateIfNotExistsAsync())
                {
                    Debug.Log($"Created table {adjacentListTableName}.");
                }
            }
            catch (StorageException ex)
            {
                Debug.LogError("Failed to connect with Azure Storage.\nIf you are running with the default storage emulator configuration, please make sure you have started the storage emulator.");
                Debug.LogException(ex);
            }
        }

        IsReady = true;
        onDataManagerReady?.Invoke();
        AcquireInitialData();

    }

    /// <summary>
    /// Insert a new or update an TrackedObjectProject instance on the table storage.
    /// </summary>
    /// <param name="spatialAnchor">Instance to write or update.</param>
    /// <returns>Success result.</returns>
    public async Task<bool> UploadOrUpdate(SpatialAnchor spatialAnchor)
    {
        if (string.IsNullOrWhiteSpace(spatialAnchor.PartitionKey))
        {
            spatialAnchor.PartitionKey = partitionKey;
        }

        var insertOrMergeOperation = TableOperation.InsertOrMerge(spatialAnchor);
        var result = await spatialAnchorTable.ExecuteAsync(insertOrMergeOperation);

        return result.Result != null;
    }

    /// <summary>
    /// Get all TrackedObjectProjects from the table.
    /// </summary>
    /// <returns>List of all TrackedObjectProjects from table.</returns>
    public async Task<List<SpatialAnchor>> GetAllSpatialAnchors()
    {
        var query = new TableQuery<SpatialAnchor>();
        var segment = await spatialAnchorTable.ExecuteQuerySegmentedAsync(query, null);

        return segment.Results;
    }

    /// <summary>
    /// Find a TrackedObjectProject by a given Id (partition key).
    /// </summary>
    /// <param name="id">Id/Partition Key to search by.</param>
    /// <returns>Found TrackedObjectProject, null if nothing is found.</returns>
    public async Task<SpatialAnchor> FindSpatialAnchorById(string id)
    {
        var retrieveOperation = TableOperation.Retrieve<SpatialAnchor>(partitionKey, id);
        var result = await spatialAnchorTable.ExecuteAsync(retrieveOperation);
        var trackedObject = result.Result as SpatialAnchor;

        return trackedObject;
    }

    /// <summary>
    /// Find a TrackedObjectProject by its name.
    /// </summary>
    /// <param name="spatialAnchorName">Name to search by.</param>
    /// <returns>Found TrackedObjectProject, null if nothing is found.</returns>
    public async Task<SpatialAnchor> FindSpatialAnchorByName(string spatialAnchorName)
    {
        var query = new TableQuery<SpatialAnchor>().Where(
            TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, spatialAnchorName)));
        var segment = await spatialAnchorTable.ExecuteQuerySegmentedAsync(query, null);

        return segment.Results.FirstOrDefault();
    }

    /// <summary>
    /// Delete a TrackedObjectProject from the table.
    /// </summary>
    /// <param name="instance">Object to delete.</param>
    /// <returns>Success result of deletion.</returns>
    public async Task<bool> DeleteSpatialAnchor(SpatialAnchor instance)
    {
        var deleteOperation = TableOperation.Delete(instance);
        var result = await spatialAnchorTable.ExecuteAsync(deleteOperation);

        return result.HttpStatusCode == (int)HttpStatusCode.OK;
    }

    public async Task<bool> UpdateAdjacentTable(Edge edge)
    {
        if (string.IsNullOrWhiteSpace(edge.PartitionKey))
        {
            edge.PartitionKey = partitionKey;
        }

        var insertOrMergeOperation = TableOperation.InsertOrMerge(edge);
        var result = await adjacentListTable.ExecuteAsync(insertOrMergeOperation);

        return result.Result != null;
    }

    public async Task<List<Edge>> GetAdjacentList()
    {
        var query = new TableQuery<Edge>();
        var segment = await adjacentListTable.ExecuteQuerySegmentedAsync(query, null);

        return segment.Results;
    }

    public async Task<bool> DeleteEdge(Edge instance)
    {
        var deleteOperation = TableOperation.Delete(instance);
        var result = await adjacentListTable.ExecuteAsync(deleteOperation);

        return result.HttpStatusCode == (int)HttpStatusCode.OK;
    }

    public async void AcquireInitialData()
    {
        
        // get all spatial anchors
        var anchorRecords = await GetAllSpatialAnchors();
        Debug.Log(anchorRecords);
        // get all stored edges
        var edgeRecords = await GetAdjacentList();
        Debug.Log(edgeRecords);

        // if there is existing spatial anchor
        if (anchorRecords.Count > 0)
        {
            Debug.Log("Found " + anchorRecords.Count + " spatial anchors");
            // populate dropdown lists
            foreach(SpatialAnchor anchor in anchorRecords)
            {
                // add entry to createdSpatialAnchor
                AnchorMap.Instance.AddSpatialAnchor(anchor);
            }
        } else
        {
            // if no existing spatial anchors
            // set notfound dialog active
            Debug.Log("No existing spatial anchor found");
            dialogController.OpenDialog("No Existing Spatial Anchors Found", "We did not find spatial anchors from the cloud. Start creating spatial anchors and uploading them to the cloud for navigation.\n Do you want to create a new spatial anchor now?","create");
        }

        // if there is existing edge record
        if (edgeRecords.Count > 0)
        {
            Debug.Log("Found " + edgeRecords.Count + " edge records");
            // populate dropdown lists
            foreach (Edge edge in edgeRecords)
            {
                // add entry to createdSpatialAnchor
                AnchorMap.Instance.AddEdge(edge);
            }
        }

        // initialize scene
        AnchorMap.Instance.InitScene();

    }


}
