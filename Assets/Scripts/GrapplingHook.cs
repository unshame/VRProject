using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(LineRenderer))]
public class GrapplingHook : MonoBehaviour {

    public Camera cam;
    public float maxDistance = 100;
    public float addedRadius = 1;

    public LayerMask hitLayer;
    private RaycastHit hit;
    private LineRenderer rope;
    private bool isHooked = false;
    private float distance;

    void Start () {
        MeshFilter filter = GetComponent(typeof(MeshFilter)) as MeshFilter;
        Mesh mesh = filter.mesh;

        Vector3[] normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++)
            normals[i] = -normals[i];
        mesh.normals = normals;

        for (int m = 0; m < mesh.subMeshCount; m++) {
            int[] triangles = mesh.GetTriangles(m);
            for (int i = 0; i < triangles.Length; i += 3) {
                int temp = triangles[i + 0];
                triangles[i + 0] = triangles[i + 1];
                triangles[i + 1] = temp;
            }
            mesh.SetTriangles(triangles, m);
        }

        MeshCollider collider = GetComponent<MeshCollider>();
        collider.sharedMesh = filter.mesh;

        rope = GetComponent<LineRenderer>();
    }
	
	void Update () {
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            ShootHook();
        }
        if (Input.GetKeyDown(KeyCode.Mouse1)) {
            ResetRope();
        }

        if (isHooked) {
            if (Vector3.Distance(cam.transform.position, hit.point) > distance + addedRadius) {
                AttachHook();
            }
            else {
                UpdateRope();
            }
        }
	}

    private void ShootHook() {
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, maxDistance, hitLayer)) {
            AttachHook();
        }
        else {
            ResetRope();
        }
    }

    private void AttachHook() {
        isHooked = true;

        distance = Vector3.Distance(hit.point, cam.transform.position) + addedRadius/2;
        transform.position = hit.point;
        transform.localScale = new Vector3(distance * 2, distance * 2, distance * 2);

        rope.enabled = true;
        UpdateRope();
    }

    private void ResetRope() {
        transform.localScale = Vector3.zero;
        rope.enabled = false;
        isHooked = false;
    }

    private void UpdateRope() {
        var position = cam.transform.position;
        position.y -= 1;
        rope.SetPositions(new Vector3[] { position , hit.point });
    }
}
