using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace flexington.PathGenerator
{
    [CustomEditor(typeof(PathGeneratorComponent))]
    public class PathGeneratorInspector : Editor
    {
        private Path _path;

        private PathGeneratorComponent _target;

        private void OnEnable()
        {
            if (_target == null) _target = (PathGeneratorComponent)target;
            if (_path == null)
            {
                _target.GeneratePath();
                _path = _target.Path;
            }
        }

        private void OnSceneGUI()
        {
            Input();
            Draw();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();


            EditorGUI.BeginChangeCheck();
            bool autoSet = GUILayout.Toggle(_path.AutoSet, "Auto Set Control Points");
            if (autoSet != _path.AutoSet)
            {
                Undo.RecordObject(_target, "Toggle Auto Set");
                _path.AutoSet = autoSet;
            }

            bool isClosed = GUILayout.Toggle(_path.IsClosed, "Toggle closed");
            if (isClosed != _path.IsClosed)
            {
                Undo.RecordObject(_target, "Toggle Closed");
                _path.ToggleClosed();
            }

            if (GUILayout.Button("Reset Path"))
            {
                _target.GeneratePath();
                _path = _target.Path;
            }

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        private void Input()
        {
            Event guiEvent = Event.current;
            Vector2 mousePosition = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;

            if (IsLeftClick() && guiEvent.shift)
            {
                Undo.RecordObject(_target, "Add Segment");
                _path.AddSegment(mousePosition);
            }
        }

        private void Draw()
        {
            for (int i = 0; i < _path.SegmentCount; i++)
            {
                Vector2[] points = _path.GetPointsInSegmnet(i);
                Handles.color = Color.grey;
                Handles.DrawLine(points[1], points[0]);
                Handles.DrawLine(points[2], points[3]);

                Handles.DrawBezier(points[0], points[3], points[1], points[2], Color.green, null, 2);
            }

            Handles.color = Color.red;
            for (int i = 0; i < _path.PointCount; i++)
            {
                Vector2 position = _path[i];
                Vector2 newPosition = Handles.FreeMoveHandle(position, Quaternion.identity, .1f, Vector3.zero, Handles.CylinderHandleCap);
                if (position == newPosition) continue;
                Undo.RecordObject(_target, "Move Point");
                _path.MovePoint(i, newPosition);
            }
        }

        private bool IsLeftClick()
        {
            Event guiEvent = Event.current;
            return guiEvent.type == EventType.MouseDown && guiEvent.button == 0;
        }
    }
}
