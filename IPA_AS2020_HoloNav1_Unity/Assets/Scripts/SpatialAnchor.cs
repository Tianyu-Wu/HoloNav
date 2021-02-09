using Microsoft.WindowsAzure.Storage.Table;
using UnityEngine;

public class SpatialAnchor : TableEntity
{
    public string Name { get; set; }
    public string SpatialAnchorId { get; set; }
    
    public string Type { get; set; }

    public string AnchorPosition { get; set; }

    public string WorldToLocalMatrix { get; set; }

    public SpatialAnchor() { }

    public SpatialAnchor(string name)
    {
        Name = name;
        RowKey = name;
    }
}