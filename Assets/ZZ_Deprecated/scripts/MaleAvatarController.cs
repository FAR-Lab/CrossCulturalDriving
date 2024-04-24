/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class MaleAvatarController : MonoBehaviour {
	[SerializeField] Animator animator;
	// Use this for initialization
	void Start () {
		animator.SetBool("Idle", true);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	//Call ChooseAnimation(animChoice) where animChoice is an integer to enter an animation state
	//If 0 or 1 is chosen it sets the controller boolean to either idle = 0 or walk = 1
	public void ChooseAnimation (int animChoice) {
		if (animChoice == 0) {
			animator.SetBool("Idle", true);
			animator.SetBool("Walk", false);
		} else if (animChoice == 1) {
			animator.SetBool("Walk", true);
			animator.SetBool("Idle", false);
		}
	}

}
