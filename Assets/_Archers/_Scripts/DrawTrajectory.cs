using System.Collections.Generic;
using QFSW.MOP2;
using UnityEngine;

public class DrawTrajectory : MonoBehaviour
{
    public readonly float kMinPowerFactor = 0.8f;
    public readonly float kMaxPowerFactor = 2.3f;
    public readonly float kMinHorizonAngle = -20;
    public readonly float kMaxHorizonAngle = 80;
    private const float kStandartPower = 8f;
    private const float kG = 9.81f; // Ускорение свободного падения
    private const int   kTrajectoryResolution = 22; // per second
    private const float kPartToDraw = 0.12f; // per second
    private const float kMaxFlyTime = 4f; // per second
    
    [SerializeField] private float _scaleFrom;
    [SerializeField] private float _scaleTo;

    private List<GameObject> _activeDots;
    private List<GameObject> _prevDots;
    private Vector3[]        _trajectory;

    #region Init

    private void Awake()
    {
        _activeDots = new List<GameObject>();
        _prevDots = new List<GameObject>();
    }

    #endregion

    #region Public methods

    public void CalculateAndDrawDotsTrajectory(Vector3 startPoint, Vector3 direction, float shootPowerNorm, float shootAngleNorm)
    {
        _trajectory = GeneratePath(startPoint, direction, shootPowerNorm, shootAngleNorm);

        if (_activeDots.Count > 0)
        {
            // Точки уже сделали, только подвинуть
            for (int i = 0; i < _activeDots.Count; i++)
            {
                if (i < _trajectory.Length)
                    _activeDots[i].transform.position = _trajectory[i];
            }
        }
        else
        {
            // Точки делаются впервые (новое прицеливание)
            for (int i = 0; i < _trajectory.Length; i++)
            {
                var procent = (float)i / _trajectory.Length;

                var newDot = MasterObjectPooler.Instance.GetObject("Dots");
                newDot.name = "Dot_" + i;
                newDot.transform.position = _trajectory[i];
                var scale = Mathf.Lerp(_scaleFrom, _scaleTo, procent);
                newDot.transform.localScale = Vector3.one * scale;
                if (i == 0 || procent > kPartToDraw)
                    newDot.GetComponent<SpriteRenderer>().enabled = false;
                else
                    newDot.GetComponent<SpriteRenderer>().enabled = true;
                
                _activeDots.Add(newDot);
            }
        }
    }


    /*public void Draw(Vector3 startPoint, Vector3 endPoint, float height, float partToDraw)
    {
        _height = height;
        _endPoint = endPoint;
        
        var tweenPath = new Vector3[2];
        tweenPath[0] = Vector3.Lerp(startPoint, endPoint, 0.5f);
        tweenPath[0].y += height;
        tweenPath[1] = endPoint;
        
        transform.position = startPoint;
        var s = transform.DOPath(tweenPath, 1f, PathType.CatmullRom);
        s.ForceInit();
        _dotsPath = s.PathGetDrawPoints(_resolution);
        s.Kill();

        if (_currentDots.Count > 0)
        {
            // Точки уже сделали, только подвинуть
            for (int i = 0; i < _currentDots.Count; i++)
            {
                if (i < _dotsPath.Length)
                    _currentDots[i].transform.position = _dotsPath[i];
            }
        }
        else
        {
            // Точки делаются впервые (новое прицеливание)
            for (int i = 0; i < _dotsPath.Length; i++)
            {
                var procent = (float)i / _dotsPath.Length;
                /*if(procent > partToDraw)
                    continue;#1#
                
                var newDot = MasterObjectPooler.Instance.GetObject("Dots");
                newDot.name = "Dot_" + i;
                newDot.transform.position = _dotsPath[i];
                var scale = Mathf.Lerp(_scaleFrom, _scaleTo, procent);
                newDot.transform.localScale = Vector3.one * scale;
                if (i == 0 || i == 1 || i == 2 || procent > partToDraw)
                    newDot.GetComponent<SpriteRenderer>().enabled = false;
                else
                    newDot.GetComponent<SpriteRenderer>().enabled = true;
                
                //newDot.transform.localScale = (i == 0 || i == 1 || i == 2) ? Vector3.zero : new Vector3(scale, scale, scale);
                _currentDots.Add(newDot);
            }
        }
    }*/

    public void MakeCurrentsDotsAsPrevsAndHide()
    {
        ClearPrevDots();
        
        if(_activeDots.Count == 0)
            return;

        for (int i = 0; i < _activeDots.Count; i++)
        {
            var newPrevDot = MasterObjectPooler.Instance.GetObject("PrevDots");
            newPrevDot.name = "PrevDot_" + i;
            newPrevDot.transform.position = _activeDots[i].transform.position;
            newPrevDot.transform.localScale = _activeDots[i].transform.localScale;
            newPrevDot.GetComponent<SpriteRenderer>().enabled = false;
            _prevDots.Add(newPrevDot);
        }
        
        ClearCurrentDots();
    }

    public void ShowPrevDots()
    {
        for (int i = 1; i < _prevDots.Count; i++)
        {
            _prevDots[i].GetComponent<SpriteRenderer>().enabled = true;
        }
    }

    public Vector3[] GetCurrentPath()
    {
        return _trajectory;
    }

    public void ClearCurrentDots()
    {
        for (int i = 0; i < _activeDots.Count; i++)
        {
            _activeDots[i].GetComponent<PoolableMonoBehaviour>().Release();
        }
        
        _activeDots.Clear();
    }

    public void ClearPrevDots()
    {
        for (int i = 0; i < _prevDots.Count; i++)
        {
            _prevDots[i].GetComponent<PoolableMonoBehaviour>().Release();
        }
        
        _prevDots.Clear();
    }

    public Vector3[] GetNewTrajectoryWithOffset(float spreading)
    {
        var offset = Random.insideUnitSphere * spreading;
        var newTrajectory = new Vector3[_trajectory.Length];
        _trajectory.CopyTo(newTrajectory, 0);
        
        for (int i = 0; i < newTrajectory.Length; i++)
            newTrajectory[i] += offset;
        
        return newTrajectory;
    }

    public Vector3[] GeneratePath(Vector3 startPoint, Vector3 direction, float shootPowerNorm, float shootAngleNorm)
    {
        var pointsNumber = Mathf.RoundToInt(kMaxFlyTime * kTrajectoryResolution);
        var path = new Vector3[pointsNumber];

        float time = 0;
        for (int i = 0; i < pointsNumber; i++)
        {
            var pos = CalculateTrajectoryPosByTime(time, shootPowerNorm, shootAngleNorm);
            var newPos = startPoint + pos.x * direction + Vector3.up * pos.y;
            path[i] = newPos;
            time += 1f / kTrajectoryResolution;
        }

        return path;
    }

    // Вернуть позицию в траетории, где она уходит под землю - для примерного расчета дальности выстрела
    public (Vector3, float) GetPosWithSignChanged()
    {
        for (int i = 0; i < _trajectory.Length; i++)
        {
            if (_trajectory[i].y < 0)
                return (_trajectory[i], (float)i / _trajectory.Length);
        }

        return (_trajectory[_trajectory.Length-1], 1f);
    }
    
    #endregion

    #region Private methods

    private Vector2 CalculateTrajectoryPosByTime(float time, float shootPowerNorm, float shootAngleNorm)
    {
        var power = Mathf.Lerp(kMinPowerFactor, kMaxPowerFactor, shootPowerNorm);
        var angle = Mathf.Lerp(kMinHorizonAngle, kMaxHorizonAngle, shootAngleNorm);
        var v = kStandartPower * power; // Скорость броска (тестовая пока)
        
        var x = v * Mathf.Cos(angle * Mathf.Deg2Rad) * time;
        var y = v * Mathf.Sin(angle * Mathf.Deg2Rad) * time - 0.5 * kG * time * time;

        return new Vector2(x, (float)y);
    }
    
    #endregion
}
