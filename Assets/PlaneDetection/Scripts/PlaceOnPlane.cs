using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaceOnPlane : MonoBehaviour
{
    private ARPlaneManager planeManager;
    private ARRaycastManager raycastManager;
    private ARAnchorManager anchorManager;
    private ARSessionOrigin sessionOrigin;
    private Camera arCamera;
    private GameObject placeOnPlaneObject;
    [SerializeField] private GameObject placeOnPlaneObjectPrefab;
    [SerializeField] private GameObject placementIndicator;
    [SerializeField] private float minPlaneSize;
    [SerializeField] private float scaleFactor;
    private static List<ARRaycastHit> hits;
    private void Start()
    {
        planeManager = GetComponent<ARPlaneManager>();
        sessionOrigin = GetComponent<ARSessionOrigin>();
        raycastManager = GetComponent<ARRaycastManager>();
        anchorManager = GetComponent<ARAnchorManager>();
        hits = new List<ARRaycastHit>();
        arCamera = sessionOrigin.camera;
    }

    private void Update()
    {
        ScanPlaneToPlaceObject();
    }

    private Pose pose;
    private void ScanPlaneToPlaceObject()
    {
        if (placeOnPlaneObject == null)
        {
            TogglePlanes(true);
            Vector3 origin = arCamera.transform.position;
            Vector3 direction = arCamera.transform.forward;
            ARPlane arPlane = null;
            Ray ray = new Ray(origin, direction * arCamera.farClipPlane);
            if (raycastManager.Raycast(ray, hits, TrackableType.PlaneWithinPolygon) && MeasurePlane(out ARPlane plane))
            {
                pose = hits[0].pose;
                arPlane = planeManager.GetPlane(hits[0].trackableId);
                Quaternion rotation = Quaternion.LookRotation(Vector3.forward, hits[0].pose.up);
                placementIndicator.SetActive(true);
                sessionOrigin.MakeContentAppearAt(placementIndicator.transform, pose.position, rotation);
                if (placementIndicator.activeInHierarchy && Input.touchCount > 0) 
                {
                    Touch touch = Input.GetTouch(0);
                    switch (touch.phase)
                    {
                        case TouchPhase.Began:
                            PlaceARObjectOnPlane(pose,arPlane);
                            break;
                        case TouchPhase.Ended or TouchPhase.Canceled :
                            Debug.Log("Touch ended");
                            break;
                    }
                }
            }

        }
    }

    private void PlaceARObjectOnPlane(Pose pose,ARPlane plane)
    {
        Vector3 position = pose.position;
        Vector3 direction = arCamera.transform.position - pose.position;
        direction.y = 0f;
        Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);
        ARAnchor anchor = anchorManager.AttachAnchor(plane, pose);
        placeOnPlaneObject = Instantiate(placeOnPlaneObjectPrefab,anchor.transform);
        placeOnPlaneObject.transform.localScale = Vector3.one * scaleFactor;
        placementIndicator.SetActive(false);
        TogglePlanes(false);
        sessionOrigin.MakeContentAppearAt(placeOnPlaneObject.transform, position, lookRotation);
    }
    
    private bool MeasurePlane(out ARPlane arPlane)
    {
        arPlane = null;
        foreach (var plane in planeManager.trackables)
        {
            if ((plane.size.x * plane.size.y) > minPlaneSize)
            {
                arPlane = plane;
                return true;
            }
        }
        return false;
    }
    
    private void TogglePlanes(bool status)
    {
        planeManager.enabled = status;
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(status);
        }
    }
}
