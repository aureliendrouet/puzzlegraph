/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

public class GraphDrawer : MonoBehaviour {
	public Material graphMaterial;
	public PuzzleFront puzzleFront;
	private PuzzleStateLayouter m_Layouter = null;
	public PuzzleStateLayouter layouter {
		get { return m_Layouter; }
		set { m_Layouter = value; Activate (); }
	}
	private Camera cam;
	private float lastOrthographicSize = 1;
	private bool manualZoom = false;

	void Activate () {
		cam = GetComponent<Camera> ();
		lastOrthographicSize = cam.orthographicSize;
		manualZoom = false;
	}

	void OnPostRender () {
		if (layouter == null)
			return;

		if (!manualZoom) {
			if (cam.orthographicSize != lastOrthographicSize) {
				manualZoom = false;
			}
			else {
				lastOrthographicSize = ((float)PuzzleStateLayouter.diameter + 6) * 0.5f;
				cam.orthographicSize = lastOrthographicSize;
			}
		}

		GL.PushMatrix ();
		graphMaterial.SetPass (0);
		GL.LoadProjectionMatrix (Camera.current.projectionMatrix);

		GL.Begin (GL.QUADS);

		// Draw outer rings for special nodes
		for (int i = 0; i < layouter.puzzle.stateNodes.Count; i++) {
			PuzzleStateNode node = layouter.puzzle.stateNodes[i];
			if (node.goal || node == layouter.puzzle.startNode)
				DrawNode (node, true);
		}

		// Draw edges
		for (int i = 0; i < layouter.puzzle.stateNodes.Count; i++) {
			if (i % 100 == 0) {
				GL.End ();
				GL.Begin (GL.QUADS);
			}

			PuzzleStateNode node = layouter.puzzle.stateNodes[i];
			Pointf point1 = layouter.GetPoint (i);
			Vector2 p1 = new Vector2 ((float)point1.x, (float)point1.y);
			for (int j = 0; j < node.outgoing.Count; j++) {
				PuzzleStateNode other = node.outgoing[j].toNode;
				Pointf point2 = layouter.GetPoint (other.id);
				Vector2 p2 = new Vector2 ((float)point2.x, (float)point2.y);
				Vector2 dir = p2 - p1;
				float length = dir.magnitude;
				if (length < 0.01f)
					continue;
				float angle = Mathf.Atan2 (dir.y, dir.x) * Mathf.Rad2Deg;

				DrawGLQuad (
					(p1 + p2) * 0.5f,
					new Vector2 (length, 0.8f),
					angle,
					new Rect (66/512f, 400/512f, 4/512f, 32/512f),
					Color.black);
			}
		}

		// Draw nodes
		for (int i = 0; i < layouter.puzzle.stateNodes.Count; i++) {
			if (i % 100 == 0) {
				GL.End ();
				GL.Begin (GL.QUADS);
			}

			DrawNode (layouter.puzzle.stateNodes[i], false);
		}

		GL.End ();
		GL.PopMatrix ();
	}

	void DrawNode (PuzzleStateNode node, bool outer) {
		Color color = Color.white;

		Pointf point = layouter.GetPoint (node.id);
		Vector2 p = new Vector2 ((float)point.x, (float)point.y);
		float size = 0.5f;

		if (outer) {
			size += 0.4f;

			if (node.goal)
				color = Color.green;
		}
		else {
			if (node.goalPath)
				color = Color.green;
			else if (node.stuck)
				color = Color.red;

			if (node == puzzleFront.state) {
				float pulse01 = 0.5f + 0.5f * Mathf.Sin (Time.unscaledTime * Mathf.PI * 2);
				pulse01 *= pulse01;
				color = Color.Lerp (color, Color.black, pulse01 * 0.5f);
			}
		}

		DrawGLQuad (
			p, Vector2.one * size, 0,
			new Rect (0/512f, 384/512f, 64/512f, 64/512f), Color.black);

		DrawGLQuad (
			p, Vector2.one * (size - 0.1f), 0,
			new Rect (0/512f, 384/512f, 64/512f, 64/512f), color);
	}

	void DrawGLQuad (Vector2 center, Vector2 size, float rotation, Rect uvs, Color color) {
		GL.Color (color);

		Vector2 pAA = new Vector2 (-size.x, -size.y) * 0.5f;
		Vector2 pAB = new Vector2 (-size.x,  size.y) * 0.5f;
		Vector2 pBB = new Vector2 ( size.x,  size.y) * 0.5f;
		Vector2 pBA = new Vector2 ( size.x, -size.y) * 0.5f;

		Quaternion rot = Quaternion.Euler (0, 0, rotation);
		pAA = (Vector2)(rot * pAA) + center;
		pAB = (Vector2)(rot * pAB) + center;
		pBB = (Vector2)(rot * pBB) + center;
		pBA = (Vector2)(rot * pBA) + center;

		GL.TexCoord2 (uvs.xMin, uvs.yMin);
		GL.Vertex3 (pAA.x, pAA.y, 0);

		GL.TexCoord2 (uvs.xMin, uvs.yMax);
		GL.Vertex3 (pAB.x, pAB.y, 0);

		GL.TexCoord2 (uvs.xMax, uvs.yMax);
		GL.Vertex3 (pBB.x, pBB.y, 0);

		GL.TexCoord2 (uvs.xMax, uvs.yMin);
		GL.Vertex3 (pBA.x, pBA.y, 0);
	}

	void OnGUI () {
		if (layouter == null)
			return;

		GUILayout.BeginArea (GetComponent<Camera> ().pixelRect.FlipY ());

		Puzzle puzzle = layouter.puzzle;
		GUI.Label (new Rect (10, 5, 100, 100),
			"Explored All:\nFound States:\nGoal States:\nGoal Path:\nLongest Path:");
		GUI.Label (new Rect (110, 5, 50, 100),
			string.Format ("{0:5}\n{1,5}\n{2,5}\n{3,5}\n{4,5}",
			puzzle.exploredAll,
			puzzle.stateMap.Count,
			puzzle.goalStates,
			puzzle.goalPathSteps,
			puzzle.longestPathSteps));

		GUILayout.EndArea ();
	}
}
