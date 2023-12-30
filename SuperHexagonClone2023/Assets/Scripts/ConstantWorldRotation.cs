using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/* Code Reviewed Tuesday, May 15th by Parker Neufeld, pdn844   */
/* Primary purpose is to be attached to the camera for the classic super hexagon orientation, but it can be used generically also */
public class ConstantWorldRotation : MonoBehaviour
{
    public static ConstantWorldRotation Instance { get; private set; }
    public RotationStartMode startMode;

    [Range(0f, 30f)]
    [Tooltip("How many degrees the script rotates per frame")]
    public float defaultRotationRate = 2f;
    public float currentRotationRate;
    [Tooltip("Checked = clockwise, Unchecked = counter-clockwise")]
    private bool right;




    public float flipMaxCd = 5f;
    public float curFlipCd;

    public RotationMode mode;

    [Header("Systematic Mode")]
    public float systematicTime;

    [Header("Random Modes")]
    [Tooltip("Changes will not take effect until you restart the game!")]
    public float randBaseProb;

    [Header("Random Ramp Mode Only")]
    [Tooltip("The probability of the rotation change will multiply this value after each attempt when using RAND_RAMP")]
    public float rampAmt = 1.03f;
    public float rampedProb;



    public enum RotationMode
    {
        UNCHANGING,
        RANDOM,
        RAND_RAMP,
        SYSTEMATIC
    }

    public enum RotationStartMode
    {
        RIGHT,
        LEFT,
        RANDOMIZE
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
        switch (startMode)
        {
            case RotationStartMode.LEFT:
                right = false;
                break;
            case RotationStartMode.RIGHT:
                right = true;
                break;
            case RotationStartMode.RANDOMIZE:
                if (Random.Range(0f, 1f) > 5f)
                {
                    right = !right;
                }
                break;
        }

        currentRotationRate = defaultRotationRate;
        rampedProb = randBaseProb;
        curFlipCd = flipMaxCd;
        if (mode == RotationMode.RANDOM || mode == RotationMode.RAND_RAMP)
        {
            InvokeRepeating("FlipRoll", 0f, .5f);
        }
    }

    public void FlipRoll()
    {
        //Is the rotation cooldown still on? Then abort computation!
        if (curFlipCd > 0f)
        {
            return;
        }
        float roll = Random.Range(0f, 1f);
        if (mode == RotationMode.RANDOM)
        {
            if (randBaseProb > roll)
            {
                right = !right;
                rampedProb = randBaseProb;
                curFlipCd = flipMaxCd;
            }
            else
            {
                rampedProb *= rampAmt;
            }
        }
        else if (mode == RotationMode.RAND_RAMP)
        {
            if (rampedProb > roll)
            {
                right = !right;
                rampedProb = randBaseProb;
                curFlipCd = flipMaxCd;
            }
            else
            {
                rampedProb *= rampAmt;
            }
        }

    }

    public void Clockwise()
    {
        if (right)
            return;
        LevelEnducedFlip();
    }

    public void CounterClockwise()
    {
        if (!right)
            return;
        LevelEnducedFlip();
    }

    public void LevelEnducedFlip()
    {
        right = !right;
        rampedProb = randBaseProb;
        curFlipCd = flipMaxCd;
    }

    void Update()
    {
        curFlipCd -= Time.deltaTime;

        if (Time.deltaTime == 0)
            return;

        // This used to be in FixedUpdate, so 0.02f is the old time between FixedUpdate calls. I thought it might be smoother in Update.
        Vector3 rotationVect = new Vector3(0, 0, currentRotationRate * (Time.deltaTime / 0.02f));
        if (right)
        {
            rotationVect *= -1;
        }
        transform.Rotate(rotationVect);
    }

    public void Reset()
    {
        CancelInvoke();
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        curFlipCd = flipMaxCd;
        right = true;
        currentRotationRate = defaultRotationRate;
    }
}
