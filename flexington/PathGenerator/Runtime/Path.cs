using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace flexington.PathGenerator
{
    [Serializable]
    public class Path
    {
        [SerializeField, HideInInspector] private List<Vector2> _points;

        [SerializeField, HideInInspector] private bool _isClosed;
        public bool IsClosed
        {
            get { return _isClosed; }
            private set { _isClosed = value; }
        }

        [SerializeField, HideInInspector] private bool _autoSet;
        /// <summary>
        /// Determins whether the control points should be automatically set 
        /// </summary>
        /// <value></value>
        public bool AutoSet
        {
            get { return _autoSet; }
            set
            {
                if (value != _autoSet)
                {
                    _autoSet = value;
                    if (_autoSet) AutoSetAllControlPoints();
                }
            }
        }

        public Vector2 this[int i] { get { return _points[i]; } }

        public int SegmentCount { get { return _points.Count / 3; } }

        public int PointCount { get { return _points.Count; } }

        /// <summary>
        /// Creates a new empty path
        /// </summary>
        public Path() { }

        /// <summary>
        /// Creates a new path between anchorA and anchorb
        /// </summary>
        public Path(Vector2 anchorA, Vector2 anchorB)
        {
            _points = new List<Vector2>{
            anchorA,
            anchorA + (Vector2.right + Vector2.up) * 0.5f,
            anchorB + (Vector2.left + Vector2.down) * 0.5f,
            anchorB
        };
        }

        /// <summary>
        /// Adds a new segment between the last anchor point and the new point
        /// </summary>
        public void AddSegment(Vector2 anchor)
        {
            _points.Add(_points[_points.Count - 1] * 2 - _points[_points.Count - 2]);
            _points.Add((_points[_points.Count - 1] + anchor) * 0.5f);
            _points.Add(anchor);

            if (_autoSet) AutoSetChangedControlPoints(_points.Count - 1);
        }

        /// <summary>
        /// Return the points that belong to the given segment
        /// </summary>
        public Vector2[] GetPointsInSegmnet(int i)
        {
            i *= 3;
            return new Vector2[] { _points[i], _points[i + 1], _points[i + 2], _points[LoopIndex(i + 3)] };
        }

        public void MovePoint(int index, Vector2 position)
        {
            if (index % 3 != 0 && _autoSet) return;
            Vector2 moveDelta = position - _points[index];
            _points[index] = position;

            if (_autoSet)
            {
                AutoSetChangedControlPoints(index);
                return;
            }

            if (index % 3 == 0)
            {
                if (IsInRange(index + 1, _isClosed)) _points[LoopIndex(index + 1)] += moveDelta;
                if (IsInRange(index - 1, _isClosed)) _points[LoopIndex(index - 1)] += moveDelta;
            }
            else
            {
                bool nextIsAnchor = (index + 1) % 3 == 0;
                int other = nextIsAnchor ? index + 2 : index - 2;
                int anchor = nextIsAnchor ? index + 1 : index - 1;
                if (IsInRange(other, _isClosed))
                {
                    float distance = (_points[LoopIndex(anchor)] - _points[LoopIndex(other)]).magnitude;
                    Vector2 direction = (_points[LoopIndex(anchor)] - position).normalized;
                    _points[LoopIndex(other)] = _points[LoopIndex(anchor)] + direction * distance;
                }
            }
        }

        public void ToggleClosed()
        {
            _isClosed = !_isClosed;

            if (_isClosed)
            {
                _points.Add(_points[_points.Count - 1] * 2 - _points[_points.Count - 2]);
                _points.Add(_points[0] * 2 - _points[1]);
                if (_autoSet)
                {
                    AutoSetControlPoints(0);
                    AutoSetControlPoints(_points.Count - 3);
                }
            }
            else if (!_isClosed)
            {
                _points.RemoveRange(_points.Count - 2, 2);
                if (_autoSet) AutoSetControlPoints();
            }
        }

        /// <summary>
        /// Automatically sets the control points of the given anchor.
        /// </summary>
        public void AutoSetControlPoints(int index)
        {
            Vector2 anchor = _points[index];
            Vector2 direction = Vector2.zero;
            float[] neighbourDistance = new float[2];

            if (IsInRange(index - 3, _isClosed))
            {
                Vector2 offset = _points[LoopIndex(index - 3)] - anchor;
                direction += offset.normalized;
                neighbourDistance[0] = offset.magnitude;
            }
            if (IsInRange(index + 3, _isClosed))
            {
                Vector2 offset = _points[LoopIndex(index + 3)] - anchor;
                direction -= offset.normalized;
                neighbourDistance[1] = -offset.magnitude;
            }

            direction.Normalize();

            for (int i = 0; i < neighbourDistance.Length; i++)
            {
                int control = index + i * 2 - 1;
                if (IsInRange(control, _isClosed))
                {
                    _points[LoopIndex(control)] = anchor + direction * neighbourDistance[i] * .5f;
                }
            }
        }

        /// <summary>
        /// Automatically sets the control points of the start and end anchor.
        /// </summary>
        public void AutoSetControlPoints()
        {
            if (_isClosed) return;

            _points[1] = (_points[0] + _points[2]) * .5f;
            _points[_points.Count - 2] = (_points[_points.Count - 1] + _points[_points.Count - 3]) * .5f;
        }

        /// <summary>
        /// Automatically sets the control points for all anchor points
        /// </summary>
        public void AutoSetAllControlPoints()
        {
            for (int i = 0; i < _points.Count; i += 3) AutoSetControlPoints(i);
            AutoSetControlPoints();

        }

        /// <summary>
        /// Automatically sets the control points of the given anchor and its neighbours
        /// </summary>
        public void AutoSetChangedControlPoints(int index)
        {
            for (int i = index - 3; i <= index + 3; i += 3)
            {
                if (!IsInRange(i, _isClosed)) continue;
                AutoSetControlPoints(LoopIndex(i));
            }
            AutoSetControlPoints();
        }

        public Vector2[] CalculateEvenlySpacedPoints(float spacing, float resultion = 1)
        {

            List<Vector2> points = new List<Vector2>();
            points.Add(_points[0]);
            Vector2 previousPoint = _points[0];
            float distance = 0;

            for (int i = 0; i < SegmentCount; i++)
            {
                Vector2[] p = GetPointsInSegmnet(i);

                float offset = Vector2.Distance(p[0], p[1]) + Vector2.Distance(p[1], p[2]) + Vector2.Distance(p[2], p[3]);
                float curveLength = Vector2.Distance(p[0], p[3]) + (offset * .5f);
                int devisions = Mathf.CeilToInt(curveLength * resultion * 10);

                float t = 0;
                while (t <= 1)
                {
                    t += 1f / devisions;
                    Vector2 pointOnCurve = Bezier.EvaluateCubic(p[0], p[1], p[2], p[3], t);
                    distance += Vector2.Distance(previousPoint, pointOnCurve);

                    while (distance >= spacing)
                    {
                        float overshootDistance = distance - spacing;
                        Vector2 newPoint = pointOnCurve + (previousPoint - pointOnCurve).normalized * overshootDistance;
                        points.Add(newPoint);
                        distance = overshootDistance;
                        previousPoint = newPoint;
                    }
                    previousPoint = pointOnCurve;
                }
            }
            return points.ToArray();
        }

        private bool IsInRange(int i, bool isClosed)
        {
            if (isClosed) return true;
            else return i >= 0 && i < _points.Count;
        }

        private int LoopIndex(int index)
        {
            return (index + _points.Count) % _points.Count;
        }
    }
}

