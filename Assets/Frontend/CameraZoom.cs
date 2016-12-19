/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using UnityEngine;
using System.Collections;

public class CameraZoom : MonoBehaviour {

	public float min = 2;
	public float max = 20;

	private Camera cam;

	private Vector2 mouseDown;
	private Vector3 mouseDownPos;
	
	void Start () {
		cam = GetComponent<Camera> ();
		GUI.depth = -100;
	}

	void OnGUI () {
		Event evt = Event.current;
		bool inside = cam.pixelRect.Contains (evt.mousePosition);

		int id = GUIUtility.GetControlID (FocusType.Passive);
		if (evt.type == EventType.MouseDown && (evt.button == 2 || PuzzleFront.hovered == null) && inside) {
			mouseDown = evt.mousePosition;
			mouseDownPos = transform.position;
			GUIUtility.hotControl = id;
			evt.Use ();
			PuzzleFront.selected = null;
		}
		else if (evt.type == EventType.MouseDrag && GUIUtility.hotControl == id) {
			Vector2 delta = evt.mousePosition - mouseDown;
			delta.y = -delta.y;
			float mult = (cam.orthographicSize * 2 / Screen.height);
			transform.position = mouseDownPos -	(Vector3)delta * mult;
			evt.Use ();
		}
		else if (evt.type == EventType.MouseUp && GUIUtility.hotControl == id) {
			GUIUtility.hotControl = 0;
			evt.Use ();
		}
		else if (evt.type == EventType.ScrollWheel && inside) {
			cam.orthographicSize *= (1 + evt.delta.y * 0.05f);
			cam.orthographicSize = Mathf.Clamp (cam.orthographicSize, min, max);
			evt.Use ();
		}
	}
}
