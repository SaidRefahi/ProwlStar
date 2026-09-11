// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Editor.Core;
using Prowl.Editor.GUI;
using Prowl.Editor.Theming;
using Prowl.OrigamiUI;
using Prowl.PaperUI;
using Prowl.Rosetta;
using Prowl.Runtime;

namespace Prowl.Editor.Inspector;

[CustomEditor(typeof(BoxCollider))]
public class BoxColliderEditor : CustomEditor
{
    public override void OnGUI(Paper paper, string id, object target)
    {
        var box = (BoxCollider)target;
        DrawDefaultInspector(paper, id, box);

        Origami.Separator(paper, $"{id}_fit_sep").Show();
        Origami.Button(paper, $"{id}_fit_mesh", $"{EditorIcons.Expand}  {Loc.Get("collider.fit_to_mesh")}", () =>
        {
            Undo.Snapshot(box);
            box.AutoFitToMesh();
        }).Show();
    }
}
