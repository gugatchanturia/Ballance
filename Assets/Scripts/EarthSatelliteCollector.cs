
using System.Collections.Generic;
using UnityEngine;

public class EarthSatelliteCollector : MonoBehaviour
{
    [Header("References")]
    public Rigidbody earth;
    public Transform gravityField;

    [Header("Collected Satellite Orbits")]
    public float orbitRadius = 0.65f;
    public float orbitHeight = 0.25f;
    public float orbitSpeed = 85f;

    private readonly List<SatellitePickup> allSatellites =
        new List<SatellitePickup>();

    private readonly List<SatellitePickup> collectedSatellites =
        new List<SatellitePickup>();

    public int CollectedCount => collectedSatellites.Count;
    public int TotalCount => allSatellites.Count;

    private void Update()
    {
        if (earth == null || gravityField == null)
            return;

        // Earth is deactivated after being absorbed by the Sun.
        // Hide collected satellites when the victory animation ends.
        if (!earth.gameObject.activeInHierarchy)
        {
            foreach (SatellitePickup satellite in collectedSatellites)
            {
                if (satellite != null &&
                    satellite.gameObject.activeSelf)
                {
                    satellite.gameObject.SetActive(false);
                }
            }

            return;
        }

        Vector3 right = gravityField.right;
        Vector3 forward = gravityField.forward;
        Vector3 up = gravityField.up;

        int count = collectedSatellites.Count;

        for (int i = 0; i < count; i++)
        {
            SatellitePickup satellite = collectedSatellites[i];

            if (satellite == null)
                continue;

            float angle = (
                Time.time * orbitSpeed +
                i * 360f / Mathf.Max(1, count)
            ) * Mathf.Deg2Rad;

            Vector3 offset =
                right * Mathf.Cos(angle) * orbitRadius +
                forward * Mathf.Sin(angle) * orbitRadius +
                up * orbitHeight;

            satellite.transform.position =
                earth.position + offset;

            satellite.transform.rotation =
                Quaternion.LookRotation(forward, up);
        }
    }

    public void Register(SatellitePickup satellite)
    {
        if (satellite != null &&
            !allSatellites.Contains(satellite))
        {
            allSatellites.Add(satellite);
        }
    }

    public void Collect(SatellitePickup satellite)
    {
        if (satellite == null ||
            collectedSatellites.Contains(satellite))
            return;

        collectedSatellites.Add(satellite);

        Debug.Log(
            "Satellite collected! " +
            collectedSatellites.Count + "/" +
            allSatellites.Count
        );
    }

    private void OnGUI()
    {
        int collected = collectedSatellites.Count;
        int total = allSatellites.Count;

        if (total == 0)
            return;

        Rect panel = new Rect(15f, 15f, 190f, 45f);

        GUI.Box(panel, "");

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 18;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor =
            new Color(0.78f, 0.92f, 1f);

        GUI.Label(
            panel,
            "SATELLITES  " + collected + "/" + total,
            style
        );
    }
}
