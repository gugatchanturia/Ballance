
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class CurvedFieldPhysicsTest : MonoBehaviour
{
    [Header("References")]
    public BlackHole blackHole;
    public BoxCollider flatFieldCollider;

    [Header("Opening")]
    public float openingRadius = 0.7f;

    private Mesh collisionMesh;
    private MeshCollider curvedCollider;

    private IEnumerator Start()
    {
        // Wait for GravityGrid to generate and deform
        // its surface around every black hole.
        yield return null;

        MeshFilter filter = GetComponent<MeshFilter>();

        if (filter.sharedMesh == null ||
            flatFieldCollider == null)
        {
            Debug.LogError(
                "CurvedFieldPhysicsTest: Missing grid mesh or flat collider.",
                this
            );
            yield break;
        }

        // Find every black hole belonging to this field.
        BlackHole[] blackHoles =
            transform.parent.GetComponentsInChildren<BlackHole>();

        if (blackHoles.Length == 0)
        {
            Debug.LogError(
                "CurvedFieldPhysicsTest: No black holes found.",
                this
            );
            yield break;
        }

        // Copy the already-deformed surface.
        collisionMesh = Instantiate(filter.sharedMesh);
        collisionMesh.name = "Physical Gravity Surface";

        Vector3[] vertices = collisionMesh.vertices;
        int[] originalTriangles = collisionMesh.triangles;

        List<int> remainingTriangles = new List<int>();

        Vector3[] holeCenters =
            new Vector3[blackHoles.Length];

        for (int i = 0; i < blackHoles.Length; i++)
        {
            holeCenters[i] =
                transform.InverseTransformPoint(
                    blackHoles[i].transform.position
                );
        }

        float radiusSquared =
            openingRadius * openingRadius;

        // Each group of six indices forms one grid quad.
        for (int i = 0; i < originalTriangles.Length; i += 6)
        {
            int a = originalTriangles[i];
            int d = originalTriangles[i + 2];

            Vector3 center =
                (vertices[a] + vertices[d]) * 0.5f;

            bool insideOpening = false;

            // Remove this quad if it belongs to ANY
            // black-hole opening.
            foreach (Vector3 holeCenter in holeCenters)
            {
                float dx = center.x - holeCenter.x;
                float dz = center.z - holeCenter.z;

                float distanceSquared =
                    dx * dx + dz * dz;

                if (distanceSquared < radiusSquared)
                {
                    insideOpening = true;
                    break;
                }
            }

            if (insideOpening)
                continue;

            for (int j = 0; j < 6; j++)
            {
                remainingTriangles.Add(
                    originalTriangles[i + j]
                );
            }
        }

        collisionMesh.SetTriangles(remainingTriangles, 0);
        collisionMesh.RecalculateBounds();

        // Both rendering and collision use the
        // same surface geometry.
        filter.sharedMesh = collisionMesh;

        curvedCollider = GetComponent<MeshCollider>();

        if (curvedCollider == null)
        {
            curvedCollider =
                gameObject.AddComponent<MeshCollider>();
        }

        curvedCollider.convex = false;
        curvedCollider.sharedMesh = null;
        curvedCollider.sharedMesh = collisionMesh;

        flatFieldCollider.enabled = false;

        // Physics controls the falling, not scripted
        // black-hole attraction or capture.
        foreach (BlackHole hole in blackHoles)
        {
            hole.enabled = false;
        }

        Debug.Log(
            "PHYSICS TEST: " + blackHoles.Length +
            " physical black-hole openings created."
        );
    }

    private void OnDestroy()
    {
        if (collisionMesh != null)
        {
            Destroy(collisionMesh);
        }
    }
}
