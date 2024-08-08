using UnityEngine;

[System.Serializable]
public class PID {
	public float pFactor, iFactor, dFactor;
		
	float integral;
	float lastError;
	float minOutput;
	float maxOutput;


	public PID(float pFactor, float iFactor, float dFactor, float minOutput = float.MinValue, float maxOutput = float.MaxValue) {
		this.pFactor = pFactor;
		this.iFactor = iFactor;
		this.dFactor = dFactor;
		this.minOutput = minOutput;
		this.maxOutput = maxOutput;
	}
	
	
	public float Update(float setpoint, float actual, float timeFrame) {
		var present = setpoint - actual;
		integral += present * timeFrame;
		integral = Mathf.Clamp(integral, minOutput, maxOutput);
		var deriv = (present - lastError) / timeFrame;
		lastError = present;
		return Mathf.Clamp(present * pFactor + integral * iFactor + deriv * dFactor, minOutput, maxOutput);
	}
}
