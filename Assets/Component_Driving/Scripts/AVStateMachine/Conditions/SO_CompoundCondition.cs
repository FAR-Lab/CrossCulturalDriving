using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LogicalOperator
{
    And,
    Or,
    Nand,
    Nor,
    Not
}

public class SO_CompoundCondition : SO_TransitionCondition
{
    public LogicalOperator logicalOperator;
    public SO_TransitionCondition[] conditions;

    public override bool IsConditionMet(SC_AVContext context)
    {
        switch (logicalOperator)
        {
            case LogicalOperator.And:
                foreach (var condition in conditions)
                {
                    if (!condition.IsConditionMet(context))
                    {
                        return false;
                    }
                }
                return true;

            case LogicalOperator.Or:
                foreach (var condition in conditions)
                {
                    if (condition.IsConditionMet(context))
                    {
                        return true;
                    }
                }
                return false;

            case LogicalOperator.Nand:
                foreach (var condition in conditions)
                {
                    if (!condition.IsConditionMet(context))
                    {
                        return true;
                    }
                }
                return false;

            case LogicalOperator.Nor:
                foreach (var condition in conditions)
                {
                    if (condition.IsConditionMet(context))
                    {
                        return false;
                    }
                }
                return true;

            case LogicalOperator.Not:
                if (conditions.Length != 1)
                {
                    throw new InvalidOperationException("NOT operator requires exactly one condition.");
                }
                return !conditions[0].IsConditionMet(context);

            default:
                throw new NotSupportedException("Unsupported logical operator.");
        }
    }
}
