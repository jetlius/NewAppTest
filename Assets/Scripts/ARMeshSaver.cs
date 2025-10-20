// ARMeshSaver.cs
// Put on any GameObject. Assign your ARMeshManager in the Inspector.
// Call SaveOBJ() to write an OBJ of the current reconstructed mesh.

using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARMeshSaver : MonoBehaviour
{
    [Header("References")]
    public ARMeshManager meshManager;

    [Header("Options")]
    public string fileName = "RoomScan.obj";
    public bool includeNormals = true;     // make sure ARMeshManager.normals is enabled if you want these
    public bool worldSpace = true;         // export in world space so it opens correctly in DCCs

    bool Validate()
    {
        if (meshManager == null)
        {
            Debug.LogError("[ARMeshSaver] Assign an ARMeshManager.");
            return false;
        }
        return true;
    }

    [ContextMenu("Save OBJ")]
    public void SaveOBJ()
    {
        if (!Validate()) return;

        try
        {
            var filters = meshManager.GetComponentsInChildren<MeshFilter>();
            if (filters == null || filters.Length == 0)
            {
                Debug.LogWarning("[ARMeshSaver] No mesh chunks found yet. Walk around a bit first.");
                return;
            }

            string path = Path.Combine(Application.persistentDataPath, fileName);
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.WriteLine("# ARFoundation runtime mesh export");
                writer.WriteLine("# Chunks: " + filters.Length);

                int vertexOffset = 0;

                foreach (var f in filters)
                {
                    if (f == null || f.sharedMesh == null) continue;
                    Mesh m = f.sharedMesh;

                    writer.WriteLine("o " + (f.name ?? "chunk"));

                    // Transform to world space if desired
                    Matrix4x4 l2w = worldSpace ? f.transform.localToWorldMatrix : Matrix4x4.identity;
                    Matrix4x4 nMat = worldSpace ? l2w.inverse.transpose : Matrix4x4.identity;

                    // Vertices
                    var verts = m.vertices;
                    for (int i = 0; i < verts.Length; i++)
                    {
                        Vector3 v = l2w.MultiplyPoint3x4(verts[i]);
                        writer.WriteLine($"v {v.x:R} {v.y:R} {v.z:R}");
                    }

                    // Normals (optional)
                    bool writeNormals = includeNormals && m.normals != null && m.normals.Length == m.vertexCount;
                    if (writeNormals)
                    {
                        var norms = m.normals;
                        for (int i = 0; i < norms.Length; i++)
                        {
                            Vector3 n = nMat.MultiplyVector(norms[i]).normalized;
                            writer.WriteLine($"vn {n.x:R} {n.y:R} {n.z:R}");
                        }
                    }

                    // Faces
                    var tris = m.triangles; // works with 32-bit indices too
                    for (int t = 0; t < tris.Length; t += 3)
                    {
                        int a = tris[t + 0] + 1 + vertexOffset;
                        int b = tris[t + 1] + 1 + vertexOffset;
                        int c = tris[t + 2] + 1 + vertexOffset;

                        if (writeNormals)
                            writer.WriteLine($"f {a}//{a} {b}//{b} {c}//{c}");
                        else
                            writer.WriteLine($"f {a} {b} {c}");
                    }

                    vertexOffset += m.vertexCount;
                }
            }

            Debug.Log($"[ARMeshSaver] Saved OBJ to: {path}");
#if UNITY_EDITOR
            Debug.Log($"Open file: {path}");
#endif
        }
        catch (Exception e)
        {
            Debug.LogError("[ARMeshSaver] Failed to save OBJ: " + e.Message);
        }
    }
}