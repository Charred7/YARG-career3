using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Text;
using System.Collections.Generic;

public class UIHierarchyDumper
{
    [MenuItem("Tools/Debug/Dump UI Hierarchy To Clipboard", false, 0)]
    public static void DumpSelected()
    {
        var selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogError("⚠️ Please select a GameObject in your Hierarchy first!");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== START UI DUMP ===");
        sb.AppendLine($"Target Selected: {selected.name}");
        sb.AppendLine("==========================================================");

        // 1. Walk UP to the absolute root Canvas
        sb.AppendLine("\n### [PART 1: THE ANCESTRY] (Root down to your selection) ###");
        List<Transform> chain = new List<Transform>();
        Transform curr = selected.transform;
        while (curr != null)
        {
            chain.Insert(0, curr);
            curr = curr.parent;
        }

        for (int i = 0; i < chain.Count; i++)
        {
            string indent = new string(' ', i * 2);
            DumpNode(chain[i], sb, indent, chain[i] == selected.transform);
        }

        // 2. Walk DOWN through every single child of the selection
        sb.AppendLine("\n### [PART 2: THE SUBTREE] (Your selection down to the floor) ###");
        DumpTreeRecursive(selected.transform, sb, 0);

        sb.AppendLine("\n=== END UI DUMP ===");

        string finalOutput = sb.ToString();
        
        // Push straight to OS Clipboard so Gemini gets 100% of it un-truncated
        GUIUtility.systemCopyBuffer = finalOutput;
        Debug.Log(finalOutput);
        Debug.Log("<color=green><b>✔ DUMP COPIED TO CLIPBOARD! Just hit Ctrl+V in your chat.</b></color>");
    }

    private static void DumpTreeRecursive(Transform t, StringBuilder sb, int depth)
    {
        string indent = new string(' ', depth * 2);
        DumpNode(t, sb, indent, false);

        for (int i = 0; i < t.childCount; i++)
        {
            DumpTreeRecursive(t.GetChild(i), sb, depth + 1);
        }
    }

    private static void DumpNode(Transform t, StringBuilder sb, string indent, bool isTarget)
    {
        RectTransform rt = t as RectTransform;
        string marker = isTarget ? " <------ [YOUR SELECTION]" : "";

        sb.Append($"{indent}[{t.name}]{marker} ");
        sb.Append(t.gameObject.activeInHierarchy ? "(Active)" : "(INACTIVE)");

        // We use raw float .ToString() here intentionally so Unity doesn't round 0.00004 to 0.
        sb.Append($"\n{indent}  ├─ WorldPos: {t.position} | LocalPos: {t.localPosition}");
        sb.Append($"\n{indent}  ├─ LocalScale: {t.localScale}");
        sb.Append($"\n{indent}  ├─ LossyScale (Actual True World Scale): {t.lossyScale}");

        if (rt != null)
        {
            sb.Append($"\n{indent}  ├─ RectSize: {rt.rect.width} x {rt.rect.height}");
            sb.Append($" | sizeDelta: {rt.sizeDelta}");
            sb.Append($" | anchoredPos: {rt.anchoredPosition}");
            sb.Append($"\n{indent}  ├─ Anchors: Min({rt.anchorMin}) Max({rt.anchorMax}) | Pivot: {rt.pivot}");
        }

        // Check for layout-altering components
        var canvas = t.GetComponent<Canvas>();
        if (canvas != null)
            sb.Append($"\n{indent}  ✿ CANVAS: Mode={canvas.renderMode}, ScaleFactor={canvas.scaleFactor}");

        var scaler = t.GetComponent<CanvasScaler>();
        if (scaler != null)
            sb.Append($"\n{indent}  ✿ SCALER: Mode={scaler.uiScaleMode}, RefRes={scaler.referenceResolution}");

        var arf = t.GetComponent<AspectRatioFitter>();
        if (arf != null)
            sb.Append($"\n{indent}  ✿ ASPECT_FITTER: Mode={arf.aspectMode}, Ratio={arf.aspectRatio}");

        var lg = t.GetComponent<LayoutGroup>();
        if (lg != null)
            sb.Append($"\n{indent}  ✿ LAYOUT_GROUP: ({lg.GetType().Name})");

        var csf = t.GetComponent<ContentSizeFitter>();
        if (csf != null)
            sb.Append($"\n{indent}  ✿ CONTENT_FITTER: H={csf.horizontalFit}, V={csf.verticalFit}");

        sb.AppendLine();
    }
}