// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Text.Json.Nodes;

using Prowl.Editor.Core;
using Prowl.Editor.GUI.Panels;
using Prowl.Runtime;
using Prowl.Runtime.Resources;

using Xunit;

namespace Prowl.Editor.Test;

public class InspectorLockTests : EditorTestHarness
{
    [Fact]
    public void InspectorStartsUnlocked()
    {
        var panel = new InspectorPanel();
        Assert.False(panel.IsLocked);
    }

    [Fact]
    public void ToggleLock_WhenNoSelection_DoesNotLock()
    {
        Selection.Clear();
        var panel = new InspectorPanel();
        panel.ToggleLock();
        Assert.False(panel.IsLocked);
    }

    [Fact]
    public void ToggleLock_WhenGameObjectSelected_LocksTarget()
    {
        var go = new GameObject("TestObject");
        Scene.Current.Add(go);
        Selection.Select(go);

        var panel = new InspectorPanel();
        panel.ToggleLock();

        Assert.True(panel.IsLocked);
        Assert.Same(go, panel.InspectedTarget);
        Assert.Contains(go, panel.InspectedObjects);
    }

    [Fact]
    public void LockedInspector_IgnoresSelectionChanges()
    {
        var goA = new GameObject("ObjectA");
        var goB = new GameObject("ObjectB");
        Scene.Current.Add(goA);
        Scene.Current.Add(goB);

        Selection.Select(goA);
        var panel = new InspectorPanel();
        panel.ToggleLock();

        Assert.True(panel.IsLocked);
        Assert.Same(goA, panel.InspectedTarget);

        // Select a different object
        Selection.Select(goB);

        // Inspector must still inspect goA
        Assert.True(panel.IsLocked);
        Assert.Same(goA, panel.InspectedTarget);

        // Clear selection entirely
        Selection.Clear();

        // Inspector must still inspect goA
        Assert.True(panel.IsLocked);
        Assert.Same(goA, panel.InspectedTarget);
    }

    [Fact]
    public void ToggleLock_UnlocksAndSyncsWithActiveSelection()
    {
        var goA = new GameObject("ObjectA");
        var goB = new GameObject("ObjectB");
        Scene.Current.Add(goA);
        Scene.Current.Add(goB);

        Selection.Select(goA);
        var panel = new InspectorPanel();
        panel.ToggleLock();
        Assert.True(panel.IsLocked);

        // Select B while locked
        Selection.Select(goB);
        Assert.Same(goA, panel.InspectedTarget);

        // Unlock
        panel.ToggleLock();
        Assert.False(panel.IsLocked);
        Assert.Same(goB, panel.InspectedTarget);
    }

    [Fact]
    public void Serialization_PreservesLockState()
    {
        var go = new GameObject("PersistentObject");
        Scene.Current.Add(go);
        Selection.Select(go);

        var panel = new InspectorPanel();
        panel.ToggleLock();
        Assert.True(panel.IsLocked);

        var json = new JsonObject();
        bool serialized = panel.SerializeState(json);
        Assert.True(serialized);
        Assert.True(json["isLocked"]?.GetValue<bool>());

        var restoredPanel = new InspectorPanel();
        restoredPanel.RestoreState(json);

        Assert.True(restoredPanel.IsLocked);
        Assert.Same(go, restoredPanel.InspectedTarget);
    }

    [Fact]
    public void LockedInspector_WhenGameObjectDestroyed_UnlocksGracefully()
    {
        var go = new GameObject("DestructibleObject");
        Scene.Current.Add(go);
        Selection.Select(go);

        var panel = new InspectorPanel();
        panel.ToggleLock();
        Assert.True(panel.IsLocked);
        Assert.Same(go, panel.InspectedTarget);

        // Explicitly dispose the object
        go.Dispose();
        Assert.True(go.IsDisposed);

        // Accessing IsLocked checks lifecycle and auto-unlocks
        Assert.False(panel.IsLocked);
    }
}
