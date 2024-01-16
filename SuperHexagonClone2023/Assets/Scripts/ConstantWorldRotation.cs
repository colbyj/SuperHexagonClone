using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/* Code Reviewed Tuesday, May 15th by Parker Neufeld, pdn844   */
/* Primary purpose is to be attached to the camera for the classic super hexagon orientation, but it can be used generically also */
public class ConstantWorldRotation : MonoBehaviour
{
    public static ConstantWorldRotation Instance { get; private set; }
    public RotationStartMode StartMode;

    [Range(0f, 360f)]
    [Tooltip("How many degrees the script rotates per frame")]
    public float DefaultRotationRate = 0f;
    public float CurrentRotationRate;

    private bool _right;

    public enum RotationStartMode
    {
        Right,
        Left,
        Randomize
    }

    void Awake()
    {
        Instance = this;

        switch (StartMode)
        {
            case RotationStartMode.Left:
                _right = false;
                break;
            case RotationStartMode.Right:
                _right = true;
                break;
            case RotationStartMode.Randomize:
                if (Random.Range(0f, 1f) > 5f)
                {
                    _right = !_right;
                }
                break;
        }

        CurrentRotationRate = DefaultRotationRate;
    }

    public void Clockwise()
    {
        if (_right)
        {
            return;
        }

        LevelInitiatedFlip();
    }

    public void CounterClockwise()
    {
        if (!_right)
        {
            return;
        }

        LevelInitiatedFlip();
    }

    public void LevelInitiatedFlip()
    {
        _right = !_right;
    }

    void Update()
    {
        if (Time.deltaTime == 0)
        {
            return;
        }

        float rotationAmount = CurrentRotationRate * Time.deltaTime;
        
        if (_right)
        {
            rotationAmount *= -1;
        }

        transform.Rotate(Vector3.forward, rotationAmount);
    }

    public void Reset()
    {
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));

        _right = true;
        CurrentRotationRate = DefaultRotationRate;
    }
}
