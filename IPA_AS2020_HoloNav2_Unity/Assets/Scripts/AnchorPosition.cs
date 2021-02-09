using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AnchorPosition : MonoBehaviour
{
    public SpatialAnchor SpatialAnchorObject => spatialAnchorObject;

    [SerializeField]
    private GameObject toolTipPanel;
    [SerializeField]
    private TextMeshPro labelText;
    [SerializeField]
    private GameObject anchorVis;
    [SerializeField]
    private Material succeedMaterial;
    [SerializeField]
    private Material progressMaterial;
    [SerializeField]
    private Material targetMatrerial;
    [SerializeField]
    private Material navMaterial;
    [SerializeField]
    private Material passedMaterial;
    [SerializeField]
    private GameObject confettiePrefab;

    private bool isTarget = false;
    private BoxCollider m_Collider;
    

    private SpatialAnchor spatialAnchorObject;

    public void Init(SpatialAnchor source)
    {
        toolTipPanel.SetActive(true);
        spatialAnchorObject = source;
        //Workaround because TextMeshPro label is not ready until next frame
        StartCoroutine(DelayedInitCoroutine());
    }

    public Transform GetTransform()
    {
        return anchorVis.transform;
    }

    public void ConnectToAnchor(AnchorPosition target)
    {
        var lineDataProvider = GetComponent<SimpleLineDataProvider>();
        lineDataProvider.SetPoint(1, target.GetTransform().position);
        var mrLineRenderer = GetComponent<MixedRealityLineRenderer>();
        mrLineRenderer.enabled = true;
    }

    public void AnchorInProgress()
    {
        anchorVis.GetComponent<MeshRenderer>().material = progressMaterial;
    }

    public void AnchorConfirmed()
    {
        anchorVis.GetComponent<MeshRenderer>().material = succeedMaterial;
    }

    public void AnchorNavigation()
    {
        anchorVis.GetComponent<MeshRenderer>().material = navMaterial;
    }

    public void AnchorPassed()
    {
        anchorVis.GetComponent<MeshRenderer>().material = passedMaterial;
    }

    public void SetAsTarget(bool state)
    {
        isTarget = state;
        anchorVis.GetComponent<MeshRenderer>().material = targetMatrerial;
    }

    public void ResetBoxCollider()
    {
        m_Collider.size = new Vector3(0.1f, 0.1f, 0.1f);
        m_Collider.center = new Vector3(0f, 0f, 0f);
    }

    public void EnlargeBoxCollider()
    {
        m_Collider.size = new Vector3(3.0f, 2.0f, 3.0f);
        m_Collider.center = new Vector3(0f, 0.95f, 0f);
    }

    private IEnumerator DelayedInitCoroutine()
    {
        yield return null;
        if (spatialAnchorObject != null)
        {
            labelText.text = spatialAnchorObject.Name;

        }
    }

    private void  OnTriggerEnter(Collider other)
    {
        //Debug.Log($"Colliding with {labelText.text}");
        OnEnterAnchorPosition?.Invoke(this);

        if (isTarget)
        {
            Debug.Log("You've reached the target!");
            var confettie = Instantiate(confettiePrefab);
            confettie.transform.position = transform.position;
            confettie.SetActive(true);
            Destroy(confettie, 5f);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        m_Collider = GetComponent<BoxCollider>();
        ResetBoxCollider();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public delegate void EnterAnchorPosition(AnchorPosition anchorPos);
    public static event EnterAnchorPosition OnEnterAnchorPosition;
    public delegate void LeaveAnchorPosition(AnchorPosition anchorPos);
    public static event LeaveAnchorPosition OnLeaveAnchorPosition;
}
