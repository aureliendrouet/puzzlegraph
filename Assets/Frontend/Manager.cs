/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using UnityEngine;
using System.Collections;

public class Manager : MonoBehaviour {

	public PuzzleTester puzzleTester;
	public GraphDrawer graphDrawer;
	public GUISkin skin;

	Camera testerCam;
	Camera graphCam;
	bool showTools = true;
	bool showGraph = false;
	float showToolsValue = 1;
	float showGraphValue = 0;
	float graphFraction = 0.4f;

	const int kMenuBarHeight = 40;
	const int kToolsWidth = 156;
	const int kSplitterWidth = 2;
	const int kSpacing = 0;
	const int kButtonWidth = 110;

	void Start () {
		testerCam = puzzleTester.GetComponent<Camera> ();
		graphCam = graphDrawer.GetComponent<Camera> ();
	}

	void Update () {
		showToolsValue = Mathf.Lerp (showToolsValue, showTools ? 1 : 0, Time.deltaTime * 10);
		showGraphValue = Mathf.Lerp (showGraphValue, showGraph ? 1 : 0, Time.deltaTime * 10);

		// toolsWidth and graphWidth are each including splitterWidth.
		// puzzleWidth is not.
		float toolingWidth = (kToolsWidth - kSplitterWidth) * showToolsValue + kSplitterWidth;
		float mainWidth = Screen.width - toolingWidth - kSplitterWidth;
		float graphWidth = (mainWidth * graphFraction + kSplitterWidth * 0.5f) * showGraphValue;
		float puzzleWidth = mainWidth - graphWidth;
		float mainHeight = Screen.height - kMenuBarHeight - kSplitterWidth;

		/*Rect toolsRect = new Rect (
			0,
			0,
			toolingWidth - splitterWidth,
			mainHeight);*/
		Rect puzzleRect = new Rect (
			toolingWidth,
			kSplitterWidth,
			puzzleWidth,
			mainHeight);
		Rect graphRect = new Rect (
			toolingWidth + puzzleWidth + kSplitterWidth,
			kSplitterWidth,
			graphWidth - kSplitterWidth,
			mainHeight);

		testerCam.pixelRect = puzzleRect;
		graphCam.pixelRect = graphRect;
	}

	void OnGUI () {
		GUI.skin = skin;

		// Meny bar background
		GUI.Box (
			new Rect (
				0,
				0,
				Screen.width,
				kMenuBarHeight
			),
			string.Empty
		);

		// Tools background
		GUI.Box (
			new Rect (
				- (kToolsWidth - kSplitterWidth) * (1 - showToolsValue),
				kMenuBarHeight - kSplitterWidth,
				kToolsWidth,
				Screen.height - (kMenuBarHeight - kSplitterWidth)
			),
			string.Empty
		);

		// Menu
		Rect rect;

		rect = new Rect (kSpacing, 0, kButtonWidth, kMenuBarHeight);
		if (GUI.Button (rect, "Menu"))
			puzzleTester.ShowMenu ();

		rect = new Rect ((Screen.width - kButtonWidth) * 0.5f, 0, kButtonWidth, kMenuBarHeight);
		if (GUI.Button (rect, puzzleTester.playing ? "Stop" : "Play"))
			puzzleTester.playing = !puzzleTester.playing;

		rect = new Rect (Screen.width - kSpacing - kButtonWidth, 0, kButtonWidth, kMenuBarHeight);
		if (GUI.Button (rect, showGraph ? "Hide States" : "Show States"))
			showGraph = !showGraph;

		showTools = !puzzleTester.playing;
	}
}
