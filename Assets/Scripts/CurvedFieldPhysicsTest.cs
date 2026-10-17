
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
    public float openingRadius = 0.45f;

    private Mesh collisionMesh;
    private MeshCollider curvedCollider;

    IEnumerator Start()
    {
        // Allow GravityGrid to generate and deform its mesh.
        yield return null;

        MeshFilter filter = GetComponent<MeshFilter>();

        if (filter.sharedMesh == null ||
            blackHole == null ||
            flatFieldCollider == null)
        {
            Debug.LogError(
                "CurvedFieldPhysicsTest: Missing reference or grid mesh.",
                this
            );
            yield break;
        }

        // Work on a separate mesh so the original
        // GravityGrid mesh remains untouched.
        collisionMesh = Instantiate(filter.sharedMesh);
        collisionMesh.name = "Physical Gravity Surface";

        Vector3[] vertices = collisionMesh.vertices;
        int[] originalTriangles = collisionMesh.triangles;

        List<int> remainingTriangles = new List<int>();

        Vector3 holeCenter = transform.InverseTransformPoint(
            blackHole.transform.position
        );

        float radiusSquared = openingRadius * openingRadius;

        // The grid was built as pairs of triangles forming quads.
        // Remove entire quads near the black hole's center.
        for (int i = 0; i < originalTriangles.Length; i += 6)
        {
            int a = originalTriangles[i];
            int d = originalTriangles[i + 2];

            Vector3 center = (vertices[a] + vertices[d]) * 0.5f;

            float dx = center.x - holeCenter.x;
            float dz = center.z - holeCenter.z;

            float distanceSquared = dx * dx + dz * dz;

            if (distanceSquared < radiusSquared)
                continue;

            for (int j = 0; j < 6; j++)
                remainingTriangles.Add(originalTriangles[i + j]);
        }

        collisionMesh.SetTriangles(remainingTriangles, 0);
        collisionMesh.RecalculateBounds();

        // Use the same geometry for visuals and collisions.
        filter.sharedMesh = collisionMesh;

        curvedCollider = GetComponent<MeshCollider>();

        if (curvedCollider == null)
            curvedCollider = gameObject.AddComponent<MeshCollider>();

        curvedCollider.convex = false;
        curvedCollider.sharedMesh = collisionMesh;

        // The curved surface replaces the flat collider.
        flatFieldCollider.enabled = false;

        // Disable scripted black-hole forces and capture
        // AFTER GravityGrid has generated its deformation.
        blackHole.enabled = false;

        Debug.Log(
            "PHYSICS TEST: Curved collision surface active. " +
            "Black-hole scripted forces disabled."
        );
    }

    void OnDestroy()
    {
        if (collisionMesh != null)
            Destroy(collisionMesh);
    }
}
