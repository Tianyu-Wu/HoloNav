using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using UnityEngine;

/// <summary>
/// This solver determines the position and orientation of an object as a directional indicator. 
/// From the point of reference of the SolverHandler Tracked Target, this indicator will orient towards the DirectionalTarget supplied.
/// If the Directional Target is deemed within view of our frame of reference, then all renderers under this Solver will be disabled. They will be enabled otherwise
/// </summary>
public class MinimapIndicator : Solver
{
    /// <summary>
    /// The GameObject transform to point the indicator towards when this object is not in view. 
    /// The frame of reference for viewing is defined by the Solver Handler Tracked Target Type
    /// </summary>
    [Tooltip("The GameObject transform to point the indicator towards when this object is not in view.\nThe frame of reference for viewing is defined by the Solver Handler Tracked Target Type")]
    public Transform DirectionalTarget;

    /// <summary>
    /// Multiplier factor to increase or decrease FOV range for testing if object is visible and thus turn off indicator
    /// </summary>
    [Tooltip("Multiplier factor to increase or decrease FOV range for testing if object is visible and thus turn off indicator")]
    [Min(0.1f)]
    public float VisibilityScaleFactor = 1.25f;

    /// <summary>
    /// The offset from center to place the indicator. If frame of reference is the camera, then the object will be this distance from center of screen
    /// </summary>
    [Tooltip("The offset from center to place the indicator. If frame of reference is the camera, then the object will be this distance from center of screen")]
    [Min(0.0f)]
    public float ViewOffset = 0.3f;

    public float Buffer = 1f;

    private bool indicatorShown = false;

    protected override void Start()
    {
        base.Start();
        SetIndicatorVisibility(ShouldShowIndicator());
    }

    private void Update()
    {
        bool showIndicator = ShouldShowIndicator();
        if (showIndicator != indicatorShown)
        {
            SetIndicatorVisibility(showIndicator);
        }
    }

    private bool ShouldShowIndicator()
    {
        return true;
    }

    private void SetIndicatorVisibility(bool showIndicator)
    {
        SolverHandler.UpdateSolvers = showIndicator;

        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = showIndicator;
        }

        indicatorShown = showIndicator;
    }

    public override void SolverUpdate()
    {
        // SolverUpdate is generally called in LateUpdate, at a time when it's possible that the DirectionalTarget
        // has already been destroyed. This ensures that if the object has been destroyed, we don't access invalid
        if (DirectionalTarget == null)
        {
            return;
        }

        // This is the frame of reference to use when solving for the position of this.gameobject
        // The frame of reference will likely be the main camera
        var solverReferenceFrame = SolverHandler.TransformTarget;

        Vector3 trackerToTargetDirection = (DirectionalTarget.position).normalized;

        // Project the vector (from the frame of reference (SolverHandler target) to the Directional Target) onto the "viewable" plane
        Vector3 indicatorDirection = new Vector3(trackerToTargetDirection.x, 0, trackerToTargetDirection.z).normalized;

        // The final position is translated from the center of the frame of reference plane along the indicator direction vector.
        GoalPosition = solverReferenceFrame.position - new Vector3(0f, 0f, -0.2f);
    }
}