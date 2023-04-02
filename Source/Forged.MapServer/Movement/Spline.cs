// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using Forged.MapServer.Maps;

namespace Forged.MapServer.Movement;

public class Spline<T>
{
    public EvaluationMode MMode;

    private static readonly Matrix4x4 SBezier3Coeffs = new(-1.0f, 3.0f, -3.0f, 1.0f, 3.0f, -6.0f, 3.0f, 0.0f, -3.0f, 3.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f);
    private static readonly Matrix4x4 SCatmullRomCoeffs = new(-0.5f, 1.5f, -1.5f, 0.5f, 1.0f, -2.5f, 2.0f, -0.5f, -0.5f, 0.0f, 0.5f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f);
    private bool _cyclic;
    private int _indexHi;
    private int _indexLo;
    private float _initialOrientation;
    private T[] _lengths = Array.Empty<T>();
    private Vector3[] _points = Array.Empty<Vector3>();

    // could be modified, affects segment length evaluation precision
    // lesser value saves more performance in cost of lover precision
    // minimal value is 1
    // client's value is 20, blizzs use 2-3 steps to compute length
    private int _stepsPerSegment = 3;

    public void Clear()
    {
        Array.Clear(_points, 0, _points.Length);
    }

    public void ComputeIndex(float t, ref int index, ref float u)
    {
        //ASSERT(t >= 0.f && t <= 1.f);
        var length = (T)(t * Length());
        index = ComputeIndexInBounds(length);
        //ASSERT(index < index_hi);
        u = (float)(length - Length(index)) / (float)Length(index, index + 1);
    }

    public bool Empty()
    {
        return _indexLo == _indexHi;
    }

    public int First()
    {
        return _indexLo;
    }

    public Vector3 GetPoint(int i)
    {
        return _points[i];
    }

    public int GetPointCount()
    {
        return _points.Length;
    }

    public Vector3[] GetPoints()
    {
        return _points;
    }

    public void InitLengths(IInitializer<T> cacher)
    {
        var i = _indexLo;
        Array.Resize(ref _lengths, _indexHi + 1);

        while (i < _indexHi)
        {
            T newLength = (dynamic)cacher.Invoke(this, i);

            if ((dynamic)newLength < 0) // todo fix me this is a ulgy hack.
                newLength = (dynamic)(Type.GetTypeCode(typeof(T)) == TypeCode.Int32 ? int.MaxValue : double.MaxValue);

            _lengths[++i] = newLength;
        }
    }

    public void InitLengths()
    {
        var i = _indexLo;
        dynamic length = default(T);
        Array.Resize(ref _lengths, _indexHi + 1);

        while (i < _indexHi)
        {
            length += (int)SegLength(i);
            _lengths[++i] = length;
        }
    }

    public bool IsCyclic()
    {
        return _cyclic;
    }

    public int Last()
    {
        return _indexHi;
    }

    public dynamic Length()
    {
        if (_lengths.Length == 0)
            return default;

        return _lengths[_indexHi];
    }

    public dynamic Length(int first, int last)
    {
        return _lengths[last] - (dynamic)_lengths[first];
    }

    public dynamic Length(int idx)
    {
        return _lengths[idx];
    }

    public void Set_length(int i, T length)
    {
        _lengths[i] = length;
    }

    public void set_steps_per_segment(int newStepsPerSegment)
    {
        _stepsPerSegment = newStepsPerSegment;
    }

    private void C_Evaluate(Span<Vector3> vertice, float t, Matrix4x4 matr, out Vector3 result)
    {
        Vector4 tvec = new(t * t * t, t * t, t, 1.0f);
        var weights = Vector4.Transform(tvec, matr);

        result = vertice[0] * weights.X + vertice[1] * weights.Y + vertice[2] * weights.Z + vertice[3] * weights.W;
    }

    private void C_Evaluate_Derivative(Span<Vector3> vertice, float t, Matrix4x4 matr, out Vector3 result)
    {
        Vector4 tvec = new(3.0f * t * t, 2.0f * t, 1.0f, 0.0f);
        var weights = Vector4.Transform(tvec, matr);

        result = vertice[0] * weights.X + vertice[1] * weights.Y + vertice[2] * weights.Z + vertice[3] * weights.W;
    }

    private int ComputeIndexInBounds(T length)
    {
        // Temporary disabled: causes infinite loop with t = 1.f
        /*
            index_type hi = index_hi;
            index_type lo = index_lo;

            index_type i = lo + (float)(hi - lo) * t;

            while ((lengths[i] > length) || (lengths[i + 1] <= length))
            {
                if (lengths[i] > length)
                    hi = i - 1; // too big
                else if (lengths[i + 1] <= length)
                    lo = i + 1; // too small

                i = (hi + lo) / 2;
            }*/

        var i = _indexLo;
        var n = _indexHi;

        while (i + 1 < n && (dynamic)_lengths[i + 1] < length)
            ++i;

        return i;
    }

    #region Evaluate

    public void Evaluate_Percent(int idx, float u, out Vector3 c)
    {
        switch (MMode)
        {
            case EvaluationMode.Linear:
                EvaluateLinear(idx, u, out c);

                break;

            case EvaluationMode.Catmullrom:
                EvaluateCatmullRom(idx, u, out c);

                break;

            case EvaluationMode.Bezier3Unused:
                EvaluateBezier3(idx, u, out c);

                break;

            default:
                c = new Vector3();

                break;
        }
    }

    private void EvaluateBezier3(int index, float t, out Vector3 result)
    {
        index *= (int)3u;
        Span<Vector3> span = _points;
        C_Evaluate(span[index..], t, SBezier3Coeffs, out result);
    }

    private void EvaluateCatmullRom(int index, float t, out Vector3 result)
    {
        Span<Vector3> span = _points;
        C_Evaluate(span[(index - 1)..], t, SCatmullRomCoeffs, out result);
    }

    private void EvaluateLinear(int index, float u, out Vector3 result)
    {
        result = _points[index] + (_points[index + 1] - _points[index]) * u;
    }

    #endregion Evaluate

    #region Init

    public void InitCyclicSpline(Vector3[] controls, int count, EvaluationMode m, int cyclicPoint, float orientation = 0f)
    {
        MMode = m;
        _cyclic = true;

        InitSpline(controls, count, m, orientation);
    }

    public void InitSpline(Span<Vector3> controls, int count, EvaluationMode m, float orientation = 0f)
    {
        MMode = m;
        _cyclic = false;
        _initialOrientation = orientation;

        switch (MMode)
        {
            case EvaluationMode.Linear:
            case EvaluationMode.Catmullrom:
                InitCatmullRom(controls, count, _cyclic, 0);

                break;

            case EvaluationMode.Bezier3Unused:
                InitBezier3(controls, count);

                break;
        }
    }

    public void InitSplineCustom(SplineRawInitializer initializer)
    {
        initializer.Initialize(ref MMode, ref _cyclic, ref _points, ref _indexLo, ref _indexHi);
    }

    private void InitBezier3(Span<Vector3> controls, int count)
    {
        var c = (int)(count / 3u * 3u);
        var t = (int)(c / 3u);

        Array.Resize(ref _points, c);
        Array.Copy(controls.ToArray(), _points, c);

        _indexLo = 0;
        _indexHi = t - 1;
    }

    private void InitCatmullRom(Span<Vector3> controls, int count, bool cyclic, int cyclicPoint)
    {
        var realSize = count + (cyclic ? (1 + 2) : (1 + 1));

        _points = new Vector3[realSize];

        var loIndex = 1;
        var highIndex = loIndex + count - 1;

        Array.Copy(controls.ToArray(), 0, _points, loIndex, count);

        // first and last two indexes are space for special 'virtual points'
        // these points are required for proper C_Evaluate and C_Evaluate_Derivative methtod work
        if (cyclic)
        {
            if (cyclicPoint == 0)
                _points[0] = controls[count - 1];
            else
                _points[0] = controls[0] - new Vector3(MathF.Cos(_initialOrientation), MathF.Sin(_initialOrientation), 0.0f);

            _points[highIndex + 1] = controls[cyclicPoint];
            _points[highIndex + 2] = controls[cyclicPoint + 1];
        }
        else
        {
            _points[0] = controls[0] - new Vector3(MathF.Cos(_initialOrientation), MathF.Sin(_initialOrientation), 0.0f);
            _points[highIndex + 1] = controls[count - 1];
        }

        _indexLo = loIndex;
        _indexHi = highIndex + (cyclic ? 1 : 0);
    }

    #endregion Init

    #region EvaluateDerivative

    public void Evaluate_Derivative(int idx, float u, out Vector3 hermite)
    {
        switch (MMode)
        {
            case EvaluationMode.Linear:
                EvaluateDerivativeLinear(idx, out hermite);

                break;

            case EvaluationMode.Catmullrom:
                EvaluateDerivativeCatmullRom(idx, u, out hermite);

                break;

            case EvaluationMode.Bezier3Unused:
                EvaluateDerivativeBezier3(idx, u, out hermite);

                break;

            default:
                hermite = new Vector3();

                break;
        }
    }

    private void EvaluateDerivativeBezier3(int index, float t, out Vector3 result)
    {
        index *= (int)3u;
        Span<Vector3> span = _points;
        C_Evaluate_Derivative(span[index..], t, SBezier3Coeffs, out result);
    }

    private void EvaluateDerivativeCatmullRom(int index, float t, out Vector3 result)
    {
        Span<Vector3> span = _points;
        C_Evaluate_Derivative(span[(index - 1)..], t, SCatmullRomCoeffs, out result);
    }

    private void EvaluateDerivativeLinear(int index, out Vector3 result)
    {
        result = _points[index + 1] - _points[index];
    }

    #endregion EvaluateDerivative

    #region SegLength

    public float SegLength(int i)
    {
        switch (MMode)
        {
            case EvaluationMode.Linear:
                return SegLengthLinear(i);

            case EvaluationMode.Catmullrom:
                return SegLengthCatmullRom(i);

            case EvaluationMode.Bezier3Unused:
                return SegLengthBezier3(i);

            default:
                return 0;
        }
    }

    private float SegLengthBezier3(int index)
    {
        index *= (int)3u;

        var p = _points.AsSpan(index);

        C_Evaluate(p, 0.0f, SBezier3Coeffs, out var nextPos);
        var curPos = nextPos;

        var i = 1;
        double length = 0;

        while (i <= _stepsPerSegment)
        {
            C_Evaluate(p, i / (float)_stepsPerSegment, SBezier3Coeffs, out nextPos);
            length += (nextPos - curPos).Length();
            curPos = nextPos;
            ++i;
        }

        return (float)length;
    }

    private float SegLengthCatmullRom(int index)
    {
        var p = _points.AsSpan(index - 1);
        var curPos = p[1];

        var i = 1;
        double length = 0;

        while (i <= _stepsPerSegment)
        {
            C_Evaluate(p, i / (float)_stepsPerSegment, SCatmullRomCoeffs, out var nextPos);
            length += (nextPos - curPos).Length();
            curPos = nextPos;
            ++i;
        }

        return (float)length;
    }

    private float SegLengthLinear(int index)
    {
        return (_points[index] - _points[index + 1]).Length();
    }

    #endregion SegLength
}