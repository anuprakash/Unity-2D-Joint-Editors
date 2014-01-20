﻿using System.Collections.Generic;
using System.Linq;
using toxicFork.GUIHelpers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomEditor(typeof (HingeJoint2D))]
[CanEditMultipleObjects]
public class HingeJoint2DEditor : JointEditor
{
    private const float AnchorEpsilon = JointEditorSettings.AnchorEpsilon;

    public void OnEnable()
    {
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
    }

    public void OnDisable()
    {
// ReSharper disable DelegateSubtraction
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
// ReSharper restore DelegateSubtraction
    }

    private void OnUndoRedoPerformed()
    {
//        foreach (HingeJoint2D hingeJoint2D in targets) {
//            PositionInfo.Record(hingeJoint2D);

//        }
    }

    public void OnPreSceneGUI()
    {
//                if (Event.current.type == EventType.keyDown)
//                {
//                    if ((Event.current.character + "").ToLower().Equals("f") || Event.current.keyCode == KeyCode.F)
//                    {
//                        //frame hotkey pressed
//                        Event.current.Use();
//        
//                        Bounds bounds;
//                        if (Selection.activeGameObject.renderer)
//                        {
//                            bounds = Selection.activeGameObject.renderer.bounds;
//                            using (new DisposableHandleColor(Color.red))
//                            {
//                                Handles.RectangleCap(0, bounds.center, Quaternion.identity, bounds.size.magnitude*0.5f);
//                            }
//                        }
//                        else
//                        {
//                            bounds = new Bounds((Vector2) Selection.activeGameObject.transform.position, Vector2.zero);
//                        }
//                        foreach (Transform selectedTransform in Selection.transforms)
//                        {
//                            bounds.Encapsulate((Vector2) selectedTransform.position);
//                        }
//        //				using (new DisposableHandleColor(Color.green)) {
//        ////					Handles.RectangleCap(0, bounds.center, Quaternion.identity, bounds.size.magnitude * 0.5f);
//        //				}
//        
//                        Vector2 midPoint = (JointEditorHelpers.GetAnchorPosition(hingeJoint2D) +
//                                            JointEditorHelpers.GetConnectedAnchorPosition(hingeJoint2D))*.5f;
//                        float distance = Vector2.Distance(midPoint, hingeJoint2D.transform.position);
//                        Bounds hingeBounds = new Bounds(midPoint, Vector2.one*distance*2);
//                        bounds.Encapsulate(hingeBounds);
//        
//                        using (new DisposableHandleColor(Color.blue))
//                        {
//                            Handles.RectangleCap(0, bounds.center, Quaternion.identity, bounds.size.magnitude*0.5f);
//                        }
//        
//                        SceneView.lastActiveSceneView.LookAt(bounds.center, Quaternion.identity, bounds.size.magnitude);
//                    }
//                }
    }

    public void OnSceneGUI()
    {
        HingeJoint2D hingeJoint2D = target as HingeJoint2D;
        if (hingeJoint2D == null)
        {
            return;
        }
        HingeJoint2DSettings settings = HingeJoint2DSettingsEditor.Get(hingeJoint2D);
        if (settings && !settings.showJointGizmos)
        {
            return;
        }


        List<Vector2> otherAnchors = new List<Vector2>();
        foreach (HingeJoint2D otherHingeObject in Selection.GetFiltered(typeof (HingeJoint2D), SelectionMode.Deep))
        {
            foreach (HingeJoint2D otherHingeJoint in otherHingeObject.GetComponents<HingeJoint2D>())
            {
                if (otherHingeJoint == hingeJoint2D)
                {
                    continue;
                }

                Vector2 otherWorldAnchor = GUIHelpers.Transform2DPoint(otherHingeJoint.transform,
                    otherHingeJoint.anchor);
                Vector2 otherConnectedWorldAnchor = otherHingeJoint.connectedBody
                    ? GUIHelpers.Transform2DPoint(
                        otherHingeJoint
                            .connectedBody
                            .transform,
                        otherHingeJoint
                            .connectedAnchor)
                    : otherHingeJoint.connectedAnchor;

                otherAnchors.Add(otherWorldAnchor);
                otherAnchors.Add(otherConnectedWorldAnchor);
            }
        }

        AnchorGUI(hingeJoint2D, otherAnchors);
    }

    private class AnchorInfo
    {
        public readonly int sliderID;
        public readonly int lockID;
        public readonly int radiusID;
        public readonly int lowerAngleID;
        public readonly int upperAngleID;
        public bool showRadius;

        public bool IsActive()
        {
            int hotControl = GUIUtility.hotControl;

            return hotControl == radiusID || hotControl == sliderID || hotControl == lockID;
        }

        public AnchorInfo()
        {
            sliderID = GUIUtility.GetControlID(FocusType.Passive);
            lockID = GUIUtility.GetControlID(FocusType.Passive);
            radiusID = GUIUtility.GetControlID(FocusType.Passive);
            lowerAngleID = GUIUtility.GetControlID(FocusType.Passive);
            upperAngleID = GUIUtility.GetControlID(FocusType.Passive);

            showRadius = true;
        }
    }

    private void AnchorGUI(HingeJoint2D hingeJoint2D, List<Vector2> otherAnchors)
    {
        HingeJoint2DSettings hingeSettings = HingeJoint2DSettingsEditor.Get(hingeJoint2D);

        bool anchorLock = hingeSettings != null && hingeSettings.lockAnchors;

        bool playing = EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPaused;
        if (playing)
        {
//            anchorLock = false;
        }

        Vector2 worldAnchor = JointEditorHelpers.GetMainAnchorPosition(hingeJoint2D);
        Vector2 worldConnectedAnchor = JointEditorHelpers.GetConnectedAnchorPosition(hingeJoint2D);

        bool overlapping = Vector2.Distance(worldConnectedAnchor, worldAnchor) <= AnchorEpsilon;

        bool changed = false;

        AnchorInfo main = new AnchorInfo(),
            connected = new AnchorInfo(),
            locked = new AnchorInfo();

        if (anchorLock)
        {
            if (playing || overlapping)
            {
                if (SingleAnchorGUI(hingeJoint2D, locked, otherAnchors, JointEditorHelpers.AnchorBias.Either))
                {
                    changed = true;
                }
            }
            else
            {
                //draw the locks instead, force them to show
                if (ToggleLockButton(main.lockID, hingeJoint2D, JointEditorHelpers.AnchorBias.Main))
                {
                    changed = true;
                }
                if (ToggleLockButton(connected.lockID, hingeJoint2D, JointEditorHelpers.AnchorBias.Connected))
                {
                    changed = true;
                }
            }
        }
        else
        {
            if (SingleAnchorGUI(hingeJoint2D, connected, otherAnchors, JointEditorHelpers.AnchorBias.Connected))
            {
                changed = true;
            }

            float handleSize = HandleUtility.GetHandleSize(worldConnectedAnchor)*editorSettings.orbitRangeScale;
            float distance = HandleUtility.DistanceToCircle(worldConnectedAnchor, handleSize*.5f);
            bool hovering = distance <= AnchorEpsilon;
            if (hovering)
            {
                connected.showRadius = false;
            }

            if (!overlapping)
            {
                if (SingleAnchorGUI(hingeJoint2D, main, otherAnchors, JointEditorHelpers.AnchorBias.Main))
                {
                    changed = true;
                }
            }
        }

//        DrawDiscs(hingeJoint2D, false);


        if (changed)
        {
            EditorUtility.SetDirty(hingeJoint2D);
        }
    }

    private static bool SingleAnchorGUI(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo,
        IEnumerable<Vector2> otherAnchors, JointEditorHelpers.AnchorBias bias)
    {
        int lockID = anchorInfo.lockID;

        bool changed = false;
        if (Event.current.shift)
        {
            if (bias == JointEditorHelpers.AnchorBias.Either)
            {
                //locked! show unlock
                if (ToggleUnlockButton(lockID, hingeJoint2D, bias))
                {
                    changed = true;
                }
            }
            else
            {
                if (ToggleLockButton(lockID, hingeJoint2D, bias))
                {
                    changed = true;
                }
            }
        }
        else
        {
            if (SliderGUI(hingeJoint2D, anchorInfo, otherAnchors, bias))
            {
                changed = true;
            }
        }

        if (anchorInfo.showRadius && RadiusGUI(hingeJoint2D, anchorInfo, bias))
        {
            changed = true;
        }

        if (DrawAngleLimits(hingeJoint2D, anchorInfo, bias))
        {
            changed = true;
        }
        DiscGui(hingeJoint2D, anchorInfo, bias);


        return changed;
    }


    private static bool DrawAngleLimits(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, JointEditorHelpers.AnchorBias bias)
    {
        bool changed = false;
        HingeJoint2DSettings settings = HingeJoint2DSettingsEditor.Get(hingeJoint2D);
        if (settings && !settings.showAngleLimits)
        {
            return false;
        }

        if (hingeJoint2D.useLimits)
        {
            JointAngleLimits2D limits = hingeJoint2D.limits;
            float minLimit = limits.min;
            float maxLimit = limits.max;

            if (hingeJoint2D.connectedBody)
            {
                Vector2 center = JointEditorHelpers.GetAnchorPosition(hingeJoint2D, bias);

                Vector2 mainPosition = JointEditorHelpers.GetTargetPosition(hingeJoint2D,
                    JointEditorHelpers.AnchorBias.Main);
                Vector2 connectedPosition = JointEditorHelpers.GetTargetPosition(hingeJoint2D, JointEditorHelpers.AnchorBias.Connected);

                float mainAngle, connectedAngle;

                if (settings) {
                    mainAngle = Mathf.Repeat(settings.mainAngle, 360f);
                    connectedAngle = Mathf.Repeat(settings.connectedAngle, 360f);
                }
                else {
                    mainAngle = JointEditorHelpers.AngleFromAnchor(center, mainPosition,
                        JointEditorHelpers.GetTargetRotation(hingeJoint2D, JointEditorHelpers.AnchorBias.Main));
                    connectedAngle = JointEditorHelpers.AngleFromAnchor(center, connectedPosition,
                        JointEditorHelpers.GetTargetRotation(hingeJoint2D, JointEditorHelpers.AnchorBias.Connected));
                }

                float handleSize = HandleUtility.GetHandleSize(center) * editorSettings.orbitRangeScale * .5f;
                float distanceFromCenter = (handleSize + (10 * HandleUtility.GetHandleSize(center) / 64));
                float finalAngle = JointEditorHelpers.AngleFromAnchor(center, mainPosition,
                        JointEditorHelpers.GetTargetRotation(hingeJoint2D, JointEditorHelpers.AnchorBias.Main)) + (connectedAngle - mainAngle);
                float minAngle = finalAngle + minLimit;
                float maxAngle = finalAngle + maxLimit;

                using (new DisposableHandleColor(editorSettings.angleAreaColor))
                {
                    Handles.DrawSolidArc(center, Vector3.forward, GUIHelpers.Rotated2DVector(minAngle) * distanceFromCenter,
                        maxLimit - minLimit, distanceFromCenter);
                }
                using (new DisposableHandleColor(editorSettings.angleLimitColor))
                {

                    Vector3 minEnd = center + GUIHelpers.Rotated2DVector(minAngle) * distanceFromCenter;
                    Handles.DrawLine(center, minEnd);

                    Vector3 maxEnd = center + GUIHelpers.Rotated2DVector(maxAngle) * distanceFromCenter;
                    Handles.DrawLine(center, maxEnd);

                    if ((minLimit < 0 && maxLimit < 0) || (minLimit > 0 && maxLimit > 0))
                    {
                        using (new DisposableHandleColor(Color.red))
                        {
                            Handles.DrawWireArc(center, Vector3.forward,
                                GUIHelpers.Rotated2DVector(minAngle),
                                maxLimit - minLimit, distanceFromCenter);
                        }
                    }
                    else
                    {
                        Handles.DrawWireArc(center, Vector3.forward, GUIHelpers.Rotated2DVector(minAngle),
                            maxLimit - minLimit, distanceFromCenter);
                    }

                    EditorGUI.BeginChangeCheck();
                    using (DisposableHandleDrawerBase drawer = new DisposableRectangleDrawer(Color.white, Color.black))
                    {
                        minAngle = GUIHelpers.AngleSlider(anchorInfo.lowerAngleID, drawer, center, minAngle,
                            distanceFromCenter, 10 * HandleUtility.GetHandleSize(minEnd) / 64);
                        maxAngle = GUIHelpers.AngleSlider(anchorInfo.upperAngleID, drawer, center, maxAngle,
                            distanceFromCenter, 10 * HandleUtility.GetHandleSize(maxEnd) / 64);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        GUIHelpers.RecordUndo("Change Angle Limits", hingeJoint2D);
                        limits.min = minAngle - finalAngle;
                        limits.max = maxAngle - finalAngle;
                        hingeJoint2D.limits = limits;
                        changed = true;
                    }
                }
            }
            else {
                Vector2 center = JointEditorHelpers.GetAnchorPosition(hingeJoint2D, bias);

                float handleSize = HandleUtility.GetHandleSize(center)*editorSettings.orbitRangeScale*.5f;

                Vector2 targetPosition = JointEditorHelpers.GetTargetPosition(hingeJoint2D, bias);

                float targetAngle;

                if (settings) {
                    targetAngle = bias == JointEditorHelpers.AnchorBias.Connected
                        ? settings.connectedAngle
                        : settings.mainAngle;
                }
                else {
                    targetAngle = JointEditorHelpers.AngleFromAnchor(center, targetPosition,
                        JointEditorHelpers.GetTargetRotation(hingeJoint2D, bias));
                }

                float distanceFromCenter = (handleSize + (10*HandleUtility.GetHandleSize(center)/64));

                using (new DisposableHandleColor(editorSettings.angleAreaColor)) {
                    Handles.DrawSolidArc(center, Vector3.forward, GUIHelpers.Rotated2DVector(targetAngle - maxLimit),
                        maxLimit - minLimit, distanceFromCenter);
                }
                using (new DisposableHandleColor(editorSettings.angleLimitColor)) {
                    float minAngle = targetAngle - minLimit;
                    float maxAngle = targetAngle - maxLimit;

                    Vector3 minEnd = center + GUIHelpers.Rotated2DVector(minAngle)*distanceFromCenter;
                    Handles.DrawLine(center, minEnd);

                    Vector3 maxEnd = center + GUIHelpers.Rotated2DVector(targetAngle - maxLimit)*distanceFromCenter;
                    Handles.DrawLine(center, maxEnd);

                    if ((minLimit < 0 && maxLimit < 0) || (minLimit > 0 && maxLimit > 0)) {
                        using (new DisposableHandleColor(Color.red)) {
                            Handles.DrawWireArc(center, Vector3.forward,
                                GUIHelpers.Rotated2DVector(targetAngle - maxLimit),
                                maxLimit - minLimit, distanceFromCenter);
                        }
                    }
                    else {
                        Handles.DrawWireArc(center, Vector3.forward, GUIHelpers.Rotated2DVector(targetAngle - maxLimit),
                            maxLimit - minLimit, distanceFromCenter);
                    }


                    EditorGUI.BeginChangeCheck();
                    using (DisposableHandleDrawerBase drawer = new DisposableRectangleDrawer(Color.white, Color.black)) {
                        minAngle = GUIHelpers.AngleSlider(anchorInfo.lowerAngleID, drawer, center, minAngle,
                            distanceFromCenter, 10*HandleUtility.GetHandleSize(minEnd)/64);
                        maxAngle = GUIHelpers.AngleSlider(anchorInfo.upperAngleID, drawer, center, maxAngle,
                            distanceFromCenter, 10*HandleUtility.GetHandleSize(maxEnd)/64);
                    }


                    if (EditorGUI.EndChangeCheck()) {
                        GUIHelpers.RecordUndo("Change Angle Limits", hingeJoint2D);
                        limits.min = targetAngle - minAngle;
                        limits.max = targetAngle - maxAngle;
                        hingeJoint2D.limits = limits;
                        changed = true;
                    }

                }
            }
        }

        return changed;
    }

    private static void DiscGui(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, JointEditorHelpers.AnchorBias bias)
    {
        Vector3 center = JointEditorHelpers.GetAnchorPosition(hingeJoint2D, bias);

        float handleSize = HandleUtility.GetHandleSize(center)*editorSettings.orbitRangeScale;
        float distance = HandleUtility.DistanceToCircle(center, handleSize*.5f);
        bool inZone = distance <= AnchorEpsilon;

        Vector2 bodyPosition = hingeJoint2D.transform.position;
        using (new DisposableHandleColor(editorSettings.mainDiscColor))
        {
            Handles.DrawLine(bodyPosition, center);
        }
        if (hingeJoint2D.connectedBody)
        {
            using (new DisposableHandleColor(editorSettings.connectedDiscColor))
            {
                Handles.DrawLine(hingeJoint2D.connectedBody.transform.position, center);
            }
        }
        if (editorSettings.ringDisplayMode == JointEditorSettings.RingDisplayMode.Always ||
            (editorSettings.ringDisplayMode == JointEditorSettings.RingDisplayMode.Hover &&
             (anchorInfo.showRadius && (inZone || anchorInfo.IsActive()))))
        {
            using (new DisposableHandleColor(editorSettings.mainDiscColor))
            {
                Handles.DrawWireDisc(center, Vector3.forward, Vector2.Distance(center, bodyPosition));
            }

            if (hingeJoint2D.connectedBody)
            {
                using (new DisposableHandleColor(editorSettings.connectedDiscColor))
                {
                    Handles.DrawWireDisc(center, Vector3.forward,
                        Vector2.Distance(center,
                            hingeJoint2D.connectedBody.transform.position));
                }
            }
        }
    }

    private static bool RadiusGUI(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, JointEditorHelpers.AnchorBias bias)
    {
        Vector3 center = JointEditorHelpers.GetAnchorPosition(hingeJoint2D, bias);

        List<Transform> transforms = new List<Transform>();
        List<Transform> rightTransforms = new List<Transform>();
        switch (bias)
        {
            case JointEditorHelpers.AnchorBias.Connected:
                if (hingeJoint2D.connectedBody)
                {
                    transforms.Add(hingeJoint2D.connectedBody.transform);
                    rightTransforms.Add(hingeJoint2D.transform);
                    if (Event.current.shift)
                    {
                        transforms.Add(hingeJoint2D.transform);
                    }
                }
                else
                {
                    transforms.Add(hingeJoint2D.transform);
                }
                break;
            default:
                transforms.Add(hingeJoint2D.transform);
                if (hingeJoint2D.connectedBody)
                {
                    rightTransforms.Add(hingeJoint2D.connectedBody.transform);
                    if (Event.current.shift)
                    {
                        transforms.Add(hingeJoint2D.connectedBody.transform);
                    }
                }
                break;
        }
        if (Event.current.shift)
        {
            rightTransforms = transforms;
        }

        EditorGUI.BeginChangeCheck();
        DrawRadiusHandle(anchorInfo.radiusID, transforms, rightTransforms, center);

        return EditorGUI.EndChangeCheck();
    }

    private static bool SliderGUI(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, IEnumerable<Vector2> otherAnchors,
        JointEditorHelpers.AnchorBias bias)
    {
        int sliderID = anchorInfo.sliderID;
        List<Vector2> snapPositions = new List<Vector2> {hingeJoint2D.transform.position};

        if (hingeJoint2D.connectedBody)
        {
            snapPositions.Add(hingeJoint2D.connectedBody.transform.position);
        }

        switch (bias)
        {
            case JointEditorHelpers.AnchorBias.Main:
                snapPositions.Add(JointEditorHelpers.GetAnchorPosition(hingeJoint2D,
                    JointEditorHelpers.AnchorBias.Connected));
                break;
            case JointEditorHelpers.AnchorBias.Connected:
                snapPositions.Add(JointEditorHelpers.GetAnchorPosition(hingeJoint2D, JointEditorHelpers.AnchorBias.Main));
                break;
        }

        snapPositions.AddRange(otherAnchors);

        EditorGUI.BeginChangeCheck();
        Vector2 position = AnchorSlider(sliderID, editorSettings.anchorScale, snapPositions, bias, hingeJoint2D);

        bool changed = false;
        if (EditorGUI.EndChangeCheck())
        {
            GUIHelpers.RecordUndo("Anchor Move", hingeJoint2D);
            changed = true;

            JointEditorHelpers.SetAnchorPosition(hingeJoint2D, position, bias);
        }
        return changed;
    }


    private static void DrawRadiusHandle(int controlID, IEnumerable<Transform> transforms,
        IEnumerable<Transform> rightTransforms,
        Vector2 midPoint)
    {
        RadiusHandle(controlID,
            transforms,
            rightTransforms,
            midPoint,
            HandleUtility.GetHandleSize(midPoint)*editorSettings.anchorScale*0.5f,
            HandleUtility.GetHandleSize(midPoint)*editorSettings.orbitRangeScale*0.5f);
    }


    private static bool ToggleLockButton(int controlID, HingeJoint2D hingeJoint2D, JointEditorHelpers.AnchorBias bias)
    {
        Vector3 center = JointEditorHelpers.GetAnchorPosition(hingeJoint2D, bias);

        bool lockPressed = GUIHelpers.CustomHandleButton(controlID,
            center,
            HandleUtility.GetHandleSize(center)*editorSettings.lockButtonScale,
            editorSettings.unlockButtonTexture, editorSettings.lockButtonTexture);

        if (lockPressed)
        {
            HingeJoint2DSettings hingeSettings = HingeJoint2DSettingsEditor.GetOrCreate(hingeJoint2D);

            GUIHelpers.RecordUndo("Lock Anchors", hingeSettings, hingeJoint2D);
            hingeSettings.lockAnchors = true;
            EditorUtility.SetDirty(hingeSettings);

            ReAlignAnchors(hingeJoint2D, bias);
        }

        return lockPressed;
    }

    private static bool ToggleUnlockButton(int controlID, HingeJoint2D hingeJoint2D, JointEditorHelpers.AnchorBias bias)
    {
        Vector3 center = JointEditorHelpers.GetAnchorPosition(hingeJoint2D, bias);

        bool lockPressed = GUIHelpers.CustomHandleButton(controlID,
            center,
            HandleUtility.GetHandleSize(center)*editorSettings.lockButtonScale,
            editorSettings.lockButtonTexture, editorSettings.unlockButtonTexture);

        if (lockPressed)
        {
            HingeJoint2DSettings hingeSettings = HingeJoint2DSettingsEditor.GetOrCreate(hingeJoint2D);

            GUIHelpers.RecordUndo("Unlock Anchors", hingeSettings);
            hingeSettings.lockAnchors = false;
            EditorUtility.SetDirty(hingeSettings);
        }

        return lockPressed;
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        bool foldout = EditorGUILayout.Foldout(editorSettings.foldout, "Advanced Options");
        if (EditorGUI.EndChangeCheck())
        {
            editorSettings.foldout = foldout;
            EditorUtility.SetDirty(editorSettings);
        }
        int grp = Undo.GetCurrentGroup();
        EditorGUI.BeginChangeCheck();
        if (foldout)
        {
            using (new DisposableEditorGUIIndent())
            {
                List<Object> allSettings =
                    targets.Cast<HingeJoint2D>().Select(hingeJoint2D => HingeJoint2DSettingsEditor.Get(hingeJoint2D))
                        .Where(hingeSettings => hingeSettings != null).Cast<Object>().ToList();

                SerializedObject serializedSettings = null;
                if (allSettings.Count > 0)
                {
                    serializedSettings = new SerializedObject(allSettings.ToArray());
                }
                ToggleShowGizmos(serializedSettings);
                ToggleAnchorLock(serializedSettings);
                ToggleShowAngleLimits(serializedSettings);
            }
        }


        /*SerializedProperty propertyIterator = serializedObject.GetIterator();
        do
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField(propertyIterator.propertyPath);
            EditorGUILayout.LabelField(propertyIterator.type);
            EditorGUILayout.EndHorizontal();
        } while (propertyIterator.Next(true));*/

        Vector2 originalAnchor = serializedObject.FindProperty("m_Anchor").vector2Value;
        Vector2 originalConnectedAnchor = serializedObject.FindProperty("m_ConnectedAnchor").vector2Value;
        Object connectedRigidBody = serializedObject.FindProperty("m_ConnectedRigidBody").objectReferenceValue;

        Dictionary<HingeJoint2D, Vector2> worldConnectedAnchors =
            targets.Cast<HingeJoint2D>()
                .ToDictionary(hingeJoint2D => hingeJoint2D,
                    hingeJoint2D => JointEditorHelpers.GetConnectedAnchorPosition(hingeJoint2D));


        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            Vector2 curAnchor = serializedObject.FindProperty("m_Anchor").vector2Value;
            Vector2 curConnectedAnchor = serializedObject.FindProperty("m_ConnectedAnchor").vector2Value;

            bool mainAnchorChanged = Vector2.Distance(curAnchor, originalAnchor) > AnchorEpsilon;
            bool connectedAnchorChanged = Vector2.Distance(curConnectedAnchor, originalConnectedAnchor) >
                                          AnchorEpsilon;

            if (mainAnchorChanged || connectedAnchorChanged)
            {
                JointEditorHelpers.AnchorBias bias;

                if (mainAnchorChanged)
                {
                    bias = connectedAnchorChanged
                        ? JointEditorHelpers.AnchorBias.Either
                        : JointEditorHelpers.AnchorBias.Main;
                }
                else
                {
                    bias = JointEditorHelpers.AnchorBias.Connected;
                }
                foreach (HingeJoint2D hingeJoint2D in targets)
                {
                    HingeJoint2DSettings hingeSettings = HingeJoint2DSettingsEditor.Get(hingeJoint2D);
                    bool wantsLock = hingeSettings != null && hingeSettings.lockAnchors;

                    if (wantsLock)
                    {
                        GUIHelpers.RecordUndo("Inspector", hingeJoint2D);
                        ReAlignAnchors(hingeJoint2D, bias);
                        EditorUtility.SetDirty(hingeJoint2D);
                    }
                }
            }

            if (connectedRigidBody != serializedObject.FindProperty("m_ConnectedRigidBody").objectReferenceValue)
            {
                foreach (HingeJoint2D hingeJoint2D in targets)
                {
                    GUIHelpers.RecordUndo("Inspector", hingeJoint2D);
                    JointEditorHelpers.SetWorldConnectedAnchorPosition(hingeJoint2D, worldConnectedAnchors[hingeJoint2D]);

                    EditorUtility.SetDirty(hingeJoint2D);
                }
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.CollapseUndoOperations(grp);
            //Debug.Log("!!!");
            //hinge angle changed...
        }
    }

    private void ToggleShowGizmos(SerializedObject serializedSettings)
    {
        EditorGUI.BeginChangeCheck();
        bool value;
        if (serializedSettings != null)
        {
            SerializedProperty showJointGizmos = serializedSettings.FindProperty("showJointGizmos");
            EditorGUILayout.PropertyField(showJointGizmos);
            value = showJointGizmos.boolValue;
        }
        else
        {
            value = EditorGUILayout.Toggle("Show Joint Gizmos", true);
        }
        if (EditorGUI.EndChangeCheck())
        {
            foreach (HingeJoint2D hingeJoint2D in targets)
            {
                HingeJoint2DSettings hingeSettings = HingeJoint2DSettingsEditor.GetOrCreate(hingeJoint2D);

                GUIHelpers.RecordUndo("toggle gizmo display", hingeSettings);
                hingeSettings.showJointGizmos = value;
                EditorUtility.SetDirty(hingeSettings);
            }
        }
    }

    private void ToggleShowAngleLimits(SerializedObject serializedSettings)
    {
        EditorGUI.BeginChangeCheck();
        bool value;
        if (serializedSettings != null)
        {
            SerializedProperty showAngleLimits = serializedSettings.FindProperty("showAngleLimits");
            EditorGUILayout.PropertyField(showAngleLimits);
            value = showAngleLimits.boolValue;
        }
        else
        {
            value = EditorGUILayout.Toggle("Show Angle Limits", true);
        }
        if (EditorGUI.EndChangeCheck())
        {
            foreach (HingeJoint2D hingeJoint2D in targets)
            {
                HingeJoint2DSettings hingeSettings = HingeJoint2DSettingsEditor.GetOrCreate(hingeJoint2D);

                GUIHelpers.RecordUndo("toggle angle limits display", hingeSettings);
                hingeSettings.showAngleLimits = value;
                EditorUtility.SetDirty(hingeSettings);
            }
        }
    }

    private void ToggleAnchorLock(SerializedObject serializedSettings)
    {
        EditorGUI.BeginChangeCheck();
        bool value;
        if (serializedSettings != null)
        {
            SerializedProperty lockAnchors = serializedSettings.FindProperty("lockAnchors");
            EditorGUILayout.PropertyField(lockAnchors);
            value = lockAnchors.boolValue;
        }
        else
        {
            value = EditorGUILayout.Toggle("Lock Anchors", false);
        }

        if (EditorGUI.EndChangeCheck())
        {
            bool wantsContinue = true;
            int choice = 1;

            if (value)
            {
                bool farAway = targets.Cast<HingeJoint2D>().Any(hingeJoint2D =>
                    Vector2.Distance(
                        JointEditorHelpers.GetMainAnchorPosition(hingeJoint2D),
                        JointEditorHelpers.GetConnectedAnchorPosition(hingeJoint2D)
                        ) > AnchorEpsilon);
                if (farAway)
                {
                    choice = EditorUtility.DisplayDialogComplex("Enable Anchor Lock",
                        "Which anchor would you like to lock to?",
                        "Main",
                        "Connected",
                        "Cancel");

                    if (choice == 2) //cancel
                    {
                        wantsContinue = false;
                    }
                }
            }
            if (wantsContinue)
            {
                foreach (HingeJoint2D hingeJoint2D in targets)
                {
                    HingeJoint2DSettings hingeSettings = HingeJoint2DSettingsEditor.GetOrCreate(hingeJoint2D);

                    GUIHelpers.RecordUndo("toggle anchor locking", hingeSettings);
                    hingeSettings.lockAnchors = value;
                    EditorUtility.SetDirty(hingeSettings);

                    if (value)
                    {
                        JointEditorHelpers.AnchorBias bias = choice == 0
                            ? JointEditorHelpers.AnchorBias.Main
                            : JointEditorHelpers.AnchorBias.Connected;

                        GUIHelpers.RecordUndo("toggle anchor locking", hingeJoint2D);
                        ReAlignAnchors(hingeJoint2D, bias);
                        EditorUtility.SetDirty(hingeJoint2D);
                    }
                }
            }
        }
    }

    private static void ReAlignAnchors(HingeJoint2D hingeJoint2D,
        JointEditorHelpers.AnchorBias bias = JointEditorHelpers.AnchorBias.Either)
    {
        Transform transform = hingeJoint2D.transform;

        Vector2 connectedAnchor = hingeJoint2D.connectedAnchor;
        Vector2 worldAnchor = GUIHelpers.Transform2DPoint(transform, hingeJoint2D.anchor);

        if (hingeJoint2D.connectedBody)
        {
            Rigidbody2D connectedBody = hingeJoint2D.connectedBody;
            Transform connectedTransform = connectedBody.transform;

            if (bias != JointEditorHelpers.AnchorBias.Main
                && (bias == JointEditorHelpers.AnchorBias.Connected
                    || (!transform.rigidbody2D.isKinematic && connectedBody.isKinematic)))
            {
                //other body is static or there is a bias
                Vector2 worldConnectedAnchor = GUIHelpers.Transform2DPoint(connectedTransform, connectedAnchor);
                hingeJoint2D.anchor = GUIHelpers.InverseTransform2DPoint(transform, worldConnectedAnchor);
            }
            else if (bias == JointEditorHelpers.AnchorBias.Main
                     || (transform.rigidbody2D.isKinematic && !connectedBody.isKinematic))
            {
                //this body is static or there is a bias
                hingeJoint2D.connectedAnchor = GUIHelpers.InverseTransform2DPoint(connectedTransform,
                    worldAnchor);
            }
            else
            {
                Vector2 midPoint = (GUIHelpers.Transform2DPoint(connectedTransform, connectedAnchor) +
                                    worldAnchor)*.5f;
                hingeJoint2D.anchor = GUIHelpers.InverseTransform2DPoint(transform, midPoint);
                hingeJoint2D.connectedAnchor = GUIHelpers.InverseTransform2DPoint(connectedTransform, midPoint);
            }
        }
        else
        {
            if (bias == JointEditorHelpers.AnchorBias.Main)
            {
                hingeJoint2D.connectedAnchor = worldAnchor;
            }
            else
            {
                hingeJoint2D.anchor = GUIHelpers.InverseTransform2DPoint(transform, connectedAnchor);
            }
        }
    }
}