using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/* Code Reviewed Tuesday, May 15th by Parker Neufeld, pdn844   */
/* Primary purpose is to be attached to the camera for the classic super hexagon orientation, but it can be used generically also */
public class ConstantWorldRotation : MonoBehaviour
{
    public static ConstantWorldRotation Instance { get; private set; }
    public RotationStartMode StartMode;

    [Range(0f, 30f)]
    [Tooltip("How many degrees the script rotates per frame")]
    public float DefaultRotationRate = 0f;
    public float CurrentRotationRate;
    [Tooltip("Checked = clockwise, Unchecked = counter-clockwise")]
    private bool _right;




    public float FlipMaxCd = 5f;
    public float CurFlipCd;

    public RotationMode Mode;

    [Header("Systematic Mode")]
    public float SystematicTime;

    [Header("Random Modes")]
    [Tooltip("Changes will not take effect until you restart the game!")]
    public float RandBaseProb;

    [Header("Random Ramp Mode Only")]
    [Tooltip("The probability of the rotation change will multiply this value after each attempt when using RAND_RAMP")]
    public float RampAmt = 1.03f;
    public float RampedProb;



    public enum RotationMode
    {
        Unchanging,
        Random,
        RandRamp,
        Systematic
    }

    public enum RotationStartMode
    {
        Right,
        Left,
        Randomize
    }


    void RotateCoolDown()
    {

    }

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
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
        RampedProb = RandBaseProb;
        CurFlipCd = FlipMaxCd;
        if (Mode == RotationMode.Random || Mode == RotationMode.RandRamp)
        {
            InvokeRepeating("FlipRoll", 0f, .5f);
        }
    }

    public void FlipRoll()
    {
        //Is the rotation cooldown still on? Then abort computation!
        if (CurFlipCd > 0f)
        {
            return;
        }
        float roll = Random.Range(0f, 1f);
        if (Mode == RotationMode.Random)
        {
            if (RandBaseProb > roll)
            {
                _right = !_right;
                RampedProb = RandBaseProb;
                CurFlipCd = FlipMaxCd;
            }
            else
            {
                RampedProb *= RampAmt;
            }
        }
        else if (Mode == RotationMode.RandRamp)
        {
            if (RampedProb > roll)
            {
                _right = !_right;
                RampedProb = RandBaseProb;
                CurFlipCd = FlipMaxCd;
            }
            else
            {
                RampedProb *= RampAmt;
            }
        }

    }

    public void Clockwise()
    {
        if (_right)
        {
            return;
        }

        LevelEnducedFlip();
    }

    public void CounterClockwise()
    {
        if (!_right)
        {
            return;
        }

        LevelEnducedFlip();
    }

    public void LevelEnducedFlip()
    {
        _right = !_right;
        RampedProb = RandBaseProb;
        CurFlipCd = FlipMaxCd;
    }

    void Update()
    {
        CurFlipCd -= Time.deltaTime;

        if (Time.deltaTime == 0)
        {
            return;
        }

        // This used to be in FixedUpdate, so 0.02f is the old time between FixedUpdate calls. I thought it might be smoother in Update.
        Vector3 rotationVect = new Vector3(0, 0, CurrentRotationRate * (Time.deltaTime / 0.02f));
        if (_right)
        {
            rotationVect *= -1;
        }
        transform.Rotate(rotationVect);
    }

    public void Reset()
    {
        CancelInvoke();
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        CurFlipCd = FlipMaxCd;
        _right = true;
        CurrentRotationRate = DefaultRotationRate;
    }
}
