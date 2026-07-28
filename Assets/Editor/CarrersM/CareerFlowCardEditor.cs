using UnityEditor;
using UnityEngine;
using YARG.Menu.Career.CareerFlow;

/// <summary>
/// Custom Editor for CareerFlowCard that hides inherited CoverFlowCard serialized fields
/// which are not relevant for the Gear Crate card visual motif.
///
/// Shows only:
///   - _rectTransform, _canvasGroup (inherited — needed for layout)
///   - _crateFrame, _completionStamp, _dashboardPanel (CareerFlowCard-specific)
///
/// Hidden inherited fields:
///   - _posterImage, _grayscaleOverlay, _lockIcon, _accentBorder
///   - _gigTitleText, _bandLogoImage, _avgTrackText, _intensityText
///   - _activeContentPanel, _woodenFrame, _soldOutSash, _lockPanel
///   - _unlockRequirementText, _mysteryTracklistText, _hypeSlot1Text, _hypeSlot2Text
///   - _uiFlameEffect
/// </summary>
[CustomEditor(typeof(CareerFlowCard))]
[CanEditMultipleObjects]
public class CareerFlowCardEditor : UnityEditor.Editor
{
    private SerializedProperty _rectTransform;
    private SerializedProperty _canvasGroup;
    private SerializedProperty _crateFrame;
    private SerializedProperty _completionStamp;
    private SerializedProperty _dashboardPanel;

    private void OnEnable()
    {
        _rectTransform   = serializedObject.FindProperty("_rectTransform");
        _canvasGroup     = serializedObject.FindProperty("_canvasGroup");
        _crateFrame      = serializedObject.FindProperty("_crateFrame");
        _completionStamp = serializedObject.FindProperty("_completionStamp");
        _dashboardPanel  = serializedObject.FindProperty("_dashboardPanel");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("CareerFlowCard (Gear Crate)", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        // ── Inherited (essential) ──
        EditorGUILayout.PropertyField(_rectTransform);
        EditorGUILayout.PropertyField(_canvasGroup);

        EditorGUILayout.Space(4);

        // ── Crate Frame ──
        EditorGUILayout.LabelField("Crate Visuals", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_crateFrame);
        EditorGUILayout.PropertyField(_completionStamp);

        EditorGUILayout.Space(4);

        // ── Dashboard ──
        EditorGUILayout.LabelField("Dashboard", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_dashboardPanel);

        EditorGUILayout.Space(6);

        // ── Help summary ──
        EditorGUILayout.HelpBox(
            "This card uses CareerFlowController for data rendering. " +
            "Assign CrateFrame sprite/material and CompletionStamp sprite in the fields above.",
            MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}