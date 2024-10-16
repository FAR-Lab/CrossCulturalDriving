using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node_AccelWithBreak : SO_FSMNodeAction
{ 
    public float Max_accel = 0.7418f; 
    public float Max_speed = 6.3637f;
    public float Delta = 0.8403f;
    public float BreakTime = 1f; // Time in seconds to fully stop

    private bool _isStopping = false; 
    private float _decelRate; 
    private readonly float _targetSpeed = 0f;

    public override void OnEnter(SC_AVContext context)
    {
        _decelRate = context.GetSpeed() / BreakTime;
    }

    public override void OnExit(SC_AVContext context)
    {
        _isStopping = false;
    }

    public override void OnUpdate(SC_AVContext context) 
    {
        bool isFrontClear = context.IsFrontClear();
        float currentSpeed = context.GetSpeed();

        if (!isFrontClear)
        {
            if (!_isStopping)
            {
                _isStopping = true;
                _decelRate = currentSpeed / BreakTime;
            }

            float newSpeed = Mathf.Max(currentSpeed - _decelRate * Time.deltaTime, _targetSpeed);
            context.SetSpeed(newSpeed);

            if (newSpeed <= 0)
            {
                context.SetSpeed(0);
            }
        }
        else
        {
            if (_isStopping && currentSpeed == 0)
            {
                _isStopping = false;
            }

            if (!_isStopping)
            {
                float acceleration = Max_accel * (1 - Mathf.Pow(currentSpeed / Max_speed, Delta));
                float newSpeed = Mathf.Min(currentSpeed + acceleration * Time.deltaTime, Max_speed);
                context.SetSpeed(newSpeed);
            }
        }
    }
}
