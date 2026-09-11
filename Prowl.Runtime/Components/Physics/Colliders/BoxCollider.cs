// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Jitter2.Collision.Shapes;

using Prowl.Echo;
using Prowl.Runtime.Resources;
using Prowl.Vector;

namespace Prowl.Runtime;

[AddComponentMenu("Physics/Colliders/Box Collider")]
public sealed class BoxCollider : Collider
{
    [SerializeField] private Float3 size = new(1, 1, 1);

    /// <summary>
    /// Gets or sets the dimensions of the box.
    /// </summary>
    public Float3 Size
    {
        get => size;
        set
        {
            size = value;
            Rebuild();
        }
    }

    public override void Reset()
    {
        base.Reset();
        size = new Float3(1, 1, 1);
        AutoFitToMesh();
    }

    /// <summary>
    /// Fits the BoxCollider's Center and Size to match the attached or child mesh bounds.
    /// </summary>
    /// <returns>True if a mesh was found and the collider was resized; otherwise false.</returns>
    public bool AutoFitToMesh()
    {
        if (GameObject.IsNotValid())
            return false;

        // Check for MeshRenderer on the same GameObject
        var mr = GetComponent<MeshRenderer>();
        if (mr.IsValid() && !mr.Mesh.IsExplicitNull)
        {
            mr.Mesh.EnsureLoaded();
            var mesh = mr.Mesh.Res;
            if (mesh != null)
            {
                ApplyMeshBounds(mesh.bounds);
                return true;
            }
        }

        // Check for SkinnedMeshRenderer on the same GameObject
        var smr = GetComponent<SkinnedMeshRenderer>();
        if (smr.IsValid() && !smr.SharedMesh.IsExplicitNull)
        {
            smr.SharedMesh.EnsureLoaded();
            var mesh = smr.SharedMesh.Res;
            if (mesh != null)
            {
                ApplyMeshBounds(mesh.bounds);
                return true;
            }
        }

        // Check in children
        bool hasBounds = false;
        AABB combinedBounds = default;

        var childMeshRenderers = GetComponentsInChildren<MeshRenderer>(includeSelf: false, includeInactive: true);
        foreach (var child in childMeshRenderers)
        {
            if (child.IsNotValid() || child.Mesh.IsExplicitNull) continue;
            child.Mesh.EnsureLoaded();
            var mesh = child.Mesh.Res;
            if (mesh == null) continue;

            AABB childWorldBounds = child.Transform.TransformAABB(mesh.bounds);
            AABB childLocalBounds = Transform.InverseTransformAABB(childWorldBounds);
            if (!hasBounds)
            {
                combinedBounds = childLocalBounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(childLocalBounds);
            }
        }

        var childSkinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(includeSelf: false, includeInactive: true);
        foreach (var child in childSkinnedRenderers)
        {
            if (child.IsNotValid() || child.SharedMesh.IsExplicitNull) continue;
            child.SharedMesh.EnsureLoaded();
            var mesh = child.SharedMesh.Res;
            if (mesh == null) continue;

            AABB childWorldBounds = child.Transform.TransformAABB(mesh.bounds);
            AABB childLocalBounds = Transform.InverseTransformAABB(childWorldBounds);
            if (!hasBounds)
            {
                combinedBounds = childLocalBounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(childLocalBounds);
            }
        }

        if (hasBounds)
        {
            ApplyMeshBounds(combinedBounds);
            return true;
        }

        return false;
    }

    private void ApplyMeshBounds(AABB bounds)
    {
        Rotation = Float3.Zero;
        Center = bounds.Center;
        Size = new Float3(
            Maths.Max(bounds.Size.X, 0.001f),
            Maths.Max(bounds.Size.Y, 0.001f),
            Maths.Max(bounds.Size.Z, 0.001f)
        );
    }

    public override RigidBodyShape[] CreateShapes() => [new BoxShape(Maths.Max(size.X, 0.01f), Maths.Max(size.Y, 0.01f), Maths.Max(size.Z, 0.01f))];

    public override void DrawGizmos()
    {
        Debug.PushMatrix(GizmoMatrix);
        Debug.DrawWireCube(Float3.Zero, size * 0.5f, Color.Green);
        Debug.PopMatrix();
    }
}
