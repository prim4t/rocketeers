﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using InControl;

public class SpotlightPause : MonoBehaviour {
	
	private class SpotlightData {
		public SpotlightData (float s, float a) { spread = s; alpha = a; }
		public float	spread;
		public float	alpha;
	}

	public static SpotlightPause	S = null;
	public bool						spotlightOn = false;

	public bool					animating = false;
	private GameObject				rightPanel;
	private GameObject				leftPanel;

	void Awake () {
		S = this;
	}

	void Start () {
		gameObject.SetActive (false);

		rightPanel = transform.Find ("Canvas").Find("Covers").Find("FadeRight").gameObject;
		leftPanel = transform.Find ("Canvas").Find("Covers").Find("FadeLeft").gameObject;

		SetTransparency (0f);
		SetSpread (0f);
		SetPosition (0f);
	}

	/**************** Public Interface ****************/

	public bool CreateSpotlight(float spread) {
		if (!animating && !spotlightOn) {
			gameObject.SetActive (true);

			SetPosition (0f);
			SpotlightData dat = new SpotlightData (spread, 0.75f);
			StartCoroutine ("ExtendSpotlight", dat);
			return true;
		} else {
			return false;
		}
	}

	public bool CreateSpotlight(float spread, float x_pos) {
		if (!animating && !spotlightOn) {
			gameObject.SetActive (true);

			SetPosition (x_pos);
			SpotlightData dat = new SpotlightData (spread, 0.75f);
			StartCoroutine ("ExtendSpotlight", dat);
			return true;
		} else {
			return false;
		}
	}

	public bool CreateSpotlight(float spread, float x_pos, float alpha) {
		if (!animating && !spotlightOn) {
			gameObject.SetActive (true);

			SetPosition (x_pos);
			SpotlightData dat = new SpotlightData (spread, alpha);
			StartCoroutine ("ExtendSpotlight", dat);
			return true;
		} else {
			return false;
		}
	}

	public bool DestroySpotlight() {
		if (!animating && spotlightOn) {
			StartCoroutine ("RetractSpotlight");
			return true;
		} else {
			return false;
		}
	}

	/**************** Utility ****************/

	void SetTransparency (float trans) {
		Color col = rightPanel.GetComponent<Image> ().color;
		col.a = trans;
		rightPanel.GetComponent<Image> ().color = col;

		col = leftPanel.GetComponent<Image> ().color;
		col.a = trans;
		leftPanel.GetComponent<Image> ().color = col;
	}

	void SetSpread (float spread) {
		Vector3 rot = rightPanel.transform.eulerAngles;
		rot.z = spread;
		rightPanel.transform.eulerAngles = rot;

		rot = leftPanel.transform.eulerAngles;
		rot.z = -spread;
		leftPanel.transform.eulerAngles = rot;
	}

	void SetPosition (float x_pos) {
		Transform covers = transform.Find ("Canvas").Find ("Covers");
		Vector3 pos = covers.position;
		pos.x = x_pos;
		covers.position = pos;
	}

	IEnumerator ExtendSpotlight (SpotlightData data) {
		animating = true;
		Time.timeScale = 0;

		// Reset the spotlight values just in case
		SetTransparency (0f);
		SetSpread (0f);

		// Darken screen
		Image panel = rightPanel.GetComponent<Image>();
		while (panel.color.a < data.alpha - 0.01f) {
			SetTransparency (Mathf.Lerp (panel.color.a, data.alpha, 0.25f));
			yield return null;
		}
		SetTransparency (data.alpha);


		// Expand spotlight
		while (rightPanel.transform.eulerAngles.z < data.spread - 0.01f) {
			SetSpread (Mathf.Lerp(rightPanel.transform.eulerAngles.z, data.spread, 0.25f));
			yield return null;
		}
		SetSpread (data.spread);

		animating = false;
		spotlightOn = true;
	}

	IEnumerator RetractSpotlight () {
		animating = true;

		// Retract spotlight
		while (rightPanel.transform.rotation.z > 0.01f) {
			SetSpread (Mathf.Lerp(rightPanel.transform.eulerAngles.z, 0f, 0.25f));
			yield return null;
		}
		SetSpread (0f);

		// Brighten screen
		Image panel = rightPanel.GetComponent<Image>();
		while (panel.color.a > 0.01f) {
			SetTransparency (Mathf.Lerp (panel.color.a, 0f, 0.25f));
			yield return null;
		}
		SetTransparency (0f);

		SetPosition (0f);

		animating = false;
		spotlightOn = false;
		Time.timeScale = 1;
		gameObject.SetActive (false);
	}
}
