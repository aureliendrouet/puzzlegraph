/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using UnityEngine;

public class EdgeFront : ContainerFront {
	public PuzzleEdge edge { get { return container as PuzzleEdge; } }
	
	public override void Set (PuzzleFront puzzle, PuzzleContainer container) {
		base.Set (puzzle, container);
		UpdatePosition ();
	}
	
	public override void Update () {
		base.Update ();
		UpdatePosition ();
	}
	
	public override Vector3 GetElementPosition () {
		if (edge == null)
			return Vector3.zero;
		
		NodeFront a = puzzle.GetNode (edge.nodeA);
		NodeFront b = puzzle.GetNode (edge.nodeB);
		return Vector3.Lerp (a.transform.position, b.transform.position, 0.5f);
	}
	
	void UpdatePosition () {
		if (edge != null) {
			NodeFront a = puzzle.GetNode (edge.nodeA);
			NodeFront b = puzzle.GetNode (edge.nodeB);
			SetPositions (
				a.transform.position,
				b.transform.position
			);
		}
	}
	
	public void SetPositions (Vector2 a, Vector2 b) {
		if (b == a)
			b = a + Vector2.right * 0.01f;
		transform.position = a;
		Vector2 dir = b - a;
		transform.localScale = new Vector3 (dir.magnitude * 0.5f, 1, 1);
		transform.localEulerAngles = Vector3.forward * Mathf.Atan2 (dir.y, dir.x) * Mathf.Rad2Deg;
		staticSprite.transform.localScale = new Vector3 (1 / transform.localScale.x, 1, 1);
		dynamicSprite.transform.localScale = new Vector3 (1 / transform.localScale.x, 1, 1);
	}
}

