﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoopingAnimation : MonoBehaviour {

    public  Sprite[]        sprites;
    public int              current = 0;
    float            animationSpeed = .07f;
    float startTime;
    

	// Use this for initialization
	void Start () {
        startTime = Time.time;
	}
	
	// Update is called once per frame
	void Update () {
        // Hides sprite while in build phase
        if(PhaseManager.S.inBuildPhase) {
            GetComponent<SpriteRenderer>().enabled = false;
            return;
        }
        GetComponent<SpriteRenderer>().enabled = true;
        if (Time.time - startTime >= animationSpeed) {
            startTime = Time.time;
            current++;
            if(current == sprites.Length) {
                current = 0;
            }
            GetComponent<SpriteRenderer>().sprite = sprites[current];
        }
	}
}
