using UnityEngine;
using System.Collections.Generic;

public class PinCollisionResetterVERIFIER : MonoBehaviour
{
    [SerializeField] private List<BowlingPinControllerVERIFIER> pins;

    public void ResetPins()
    {
        foreach(BowlingPinControllerVERIFIER pin in pins)
        {
            pin.ResetPin();
        }
    }
}