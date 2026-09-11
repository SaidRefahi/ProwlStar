// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Editor.Inspector;
using Prowl.Runtime;
using Prowl.Runtime.Resources;
using Prowl.Vector;

using Xunit;

namespace Prowl.Editor.Test;

public class BoxColliderFitTests : EditorTestHarness
{
    private static Mesh CreateCustomMesh(Float3 min, Float3 max)
    {
        var mesh = new Mesh();
        mesh.Vertices = [
            min,
            new Float3(max.X, min.Y, min.Z),
            new Float3(min.X, max.Y, min.Z),
            max
        ];
        mesh.RecalculateBounds();
        return mesh;
    }

    [Fact]
    public void AutoFitToMesh_SiblingMeshRenderer_FitsExactBounds()
    {
        var go = new GameObject("CubeObject");
        Scene.Current.Add(go);

        var mr = go.AddComponent<MeshRenderer>();
        var mesh = Mesh.CreateCube(new Float3(2f, 4f, 6f));
        mr.Mesh = new AssetRef<Mesh>(mesh);

        var box = go.AddComponent<BoxCollider>();
        bool fitted = box.AutoFitToMesh();

        Assert.True(fitted);
        Assert.Equal(new Float3(2f, 4f, 6f), box.Size);
        Assert.Equal(Float3.Zero, box.Center);
        Assert.Equal(Float3.Zero, box.Rotation);
    }

    [Fact]
    public void AutoFitToMesh_OffsetMesh_CalculatesCenterAndSize()
    {
        var go = new GameObject("OffsetObject");
        Scene.Current.Add(go);

        var mr = go.AddComponent<MeshRenderer>();
        var mesh = CreateCustomMesh(new Float3(1f, 2f, 3f), new Float3(4f, 6f, 8f));
        mr.Mesh = new AssetRef<Mesh>(mesh);

        var box = go.AddComponent<BoxCollider>();
        bool fitted = box.AutoFitToMesh();

        Assert.True(fitted);
        Assert.Equal(new Float3(3f, 4f, 5f), box.Size);
        Assert.Equal(new Float3(2.5f, 4f, 5.5f), box.Center);
    }

    [Fact]
    public void AutoFitToMesh_NoMesh_ReturnsFalseAndKeepsDefaults()
    {
        var go = new GameObject("EmptyObject");
        Scene.Current.Add(go);

        var box = go.AddComponent<BoxCollider>();
        box.Size = new Float3(1f, 1f, 1f);
        box.Center = Float3.Zero;

        bool fitted = box.AutoFitToMesh();

        Assert.False(fitted);
        Assert.Equal(new Float3(1f, 1f, 1f), box.Size);
        Assert.Equal(Float3.Zero, box.Center);
    }

    [Fact]
    public void AutoFitToMesh_ChildMeshes_EncapsulatesTransformedBounds()
    {
        var root = new GameObject("Root");
        Scene.Current.Add(root);

        var child1 = new GameObject("Child1");
        child1.SetParent(root);
        child1.Transform.LocalPosition = new Float3(-5f, 0f, 0f);
        var mr1 = child1.AddComponent<MeshRenderer>();
        mr1.Mesh = new AssetRef<Mesh>(Mesh.CreateCube(new Float3(2f, 2f, 2f)));

        var child2 = new GameObject("Child2");
        child2.SetParent(root);
        child2.Transform.LocalPosition = new Float3(5f, 0f, 0f);
        var mr2 = child2.AddComponent<MeshRenderer>();
        mr2.Mesh = new AssetRef<Mesh>(Mesh.CreateCube(new Float3(2f, 2f, 2f)));

        var box = root.AddComponent<BoxCollider>();
        bool fitted = box.AutoFitToMesh();

        Assert.True(fitted);
        // Child 1 spans X: [-6, -4], Child 2 spans X: [4, 6]
        // Combined X span: [-6, 6] -> Size X = 12, Center X = 0
        // Y and Z spans: [-1, 1] -> Size Y,Z = 2, Center Y,Z = 0
        Assert.Equal(12f, box.Size.X, 0.01f);
        Assert.Equal(2f, box.Size.Y, 0.01f);
        Assert.Equal(2f, box.Size.Z, 0.01f);
        Assert.Equal(0f, box.Center.X, 0.01f);
        Assert.Equal(0f, box.Center.Y, 0.01f);
        Assert.Equal(0f, box.Center.Z, 0.01f);
    }

    [Fact]
    public void Reset_RestoresMeshFit()
    {
        var go = new GameObject("CubeObject");
        Scene.Current.Add(go);

        var mr = go.AddComponent<MeshRenderer>();
        mr.Mesh = new AssetRef<Mesh>(Mesh.CreateCube(new Float3(3f, 5f, 7f)));

        var box = go.AddComponent<BoxCollider>();
        box.Size = new Float3(100f, 100f, 100f);
        box.Center = new Float3(50f, 50f, 50f);

        box.Reset();

        Assert.Equal(new Float3(3f, 5f, 7f), box.Size);
        Assert.Equal(Float3.Zero, box.Center);
    }

    [Fact]
    public void AddComponentWithUndo_AutomaticallyFitsMesh()
    {
        var go = new GameObject("ObjectWithMesh");
        Scene.Current.Add(go);

        var mr = go.AddComponent<MeshRenderer>();
        mr.Mesh = new AssetRef<Mesh>(Mesh.CreateCube(new Float3(4f, 2f, 8f)));

        var box = (BoxCollider)GameObjectInspector.AddComponentWithUndo(go, typeof(BoxCollider))!;

        Assert.NotNull(box);
        Assert.Equal(new Float3(4f, 2f, 8f), box.Size);
        Assert.Equal(Float3.Zero, box.Center);
    }

    [Fact]
    public void AutoFitToMesh_ZeroDimensionMesh_ClampedToMinimumSize()
    {
        var go = new GameObject("FlatObject");
        Scene.Current.Add(go);

        var mr = go.AddComponent<MeshRenderer>();
        var mesh = CreateCustomMesh(new Float3(-1f, 0f, -1f), new Float3(1f, 0f, 1f));
        mr.Mesh = new AssetRef<Mesh>(mesh);

        var box = go.AddComponent<BoxCollider>();
        bool fitted = box.AutoFitToMesh();

        Assert.True(fitted);
        Assert.Equal(2f, box.Size.X, 0.01f);
        Assert.True(box.Size.Y >= 0.001f);
        Assert.Equal(2f, box.Size.Z, 0.01f);
    }
}
