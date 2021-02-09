using Microsoft.WindowsAzure.Storage.Table;
using UnityEngine;

public class Edge : TableEntity
{
    public string Name { get; set; }
    public string SpatialAnchorId { get; set; }
    public string ConnectedName { get; set; }
    public string ConnectedSpatialAnchorId { get; set; }
    public double Distance { get; set; }
    public string Id { get; set; }

    public Edge() { }

    public Edge(string id)
    {
        Id = id;
        RowKey = id;
    }
}
