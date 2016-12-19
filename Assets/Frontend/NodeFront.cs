/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using UnityEngine;
using System.Collections.Generic;

public class NodeFront : ContainerFront {
	public Material receiverMaterial;
	
	private Dictionary<PuzzleReceiverElement, LineRenderer> m_Lines =
		new Dictionary<PuzzleReceiverElement, LineRenderer> ();
	
	public PuzzleNode node { get { return container as PuzzleNode; } }
	
	public override void Set (PuzzleFront puzzle, PuzzleContainer container) {
		base.Set (puzzle, container);
		PuzzleNode node = container as PuzzleNode;
		if (node != null) {
			node.setPosition += SetPoint;
			SetPoint (node.position);
		}
	}
	
	public override void Update () {
		base.Update ();
		foreach (var receiver in m_Lines.Keys)
			UpdateReceiver (receiver);
	}
	
	public override Vector3 GetElementPosition () {
		return transform.position - Vector3.up * 0.1f;
	}
	
	public void SetPoint (Point position) {
		SetPosition (new Vector3 (position.x, position.y) * 2);
	}
	
	public void SetPosition (Vector3 position) {
		transform.position = position;
		var items = puzzle.puzzle.GetDefaultItemsInNode (node);
		foreach (var item in items) {
			puzzle.GetItem (item).transform.position = position;
		}
	}
	
	public void AddReceiver (PuzzleReceiverElement receiver) {
		GameObject go = new GameObject ();
		go.transform.parent = transform;
		LineRenderer line = go.AddComponent<LineRenderer> ();
		line.material = receiverMaterial;
		line.sortingOrder = 5;
		line.SetWidth (0.25f, 0.25f);
		
		m_Lines[receiver] = line;
		UpdateReceiver (receiver);
	}
	
	public void RemoveReceiver (PuzzleReceiverElement receiver) {
		if (m_Lines.ContainsKey (receiver)) {
			Destroy (m_Lines[receiver].gameObject);
			m_Lines.Remove (receiver);
		}
	}
	
	public void UpdateReceiver (PuzzleReceiverElement receiver) {
		PuzzleContainer c = puzzle.puzzle.GetElementContainer (receiver);
		ContainerFront containerFront = puzzle.GetContainer (c);
		if (containerFront == null)
			return;
		Vector3 end = containerFront.GetElementPosition ();
		LineRenderer line = m_Lines[receiver];
		UpdateReceiver (line, end, false);
	}
	
	public void UpdateReceiver (PuzzleReceiverElement receiver, Vector3 end, bool preciseEnd) {
		LineRenderer line = m_Lines[receiver];
		UpdateReceiver (line, end, preciseEnd);
	}
	
	const int kPositionCount = 9;
	private void UpdateReceiver (LineRenderer line, Vector3 end, bool preciseEnd) {
		Vector3 start = GetElementPosition ();
		
		Vector3 dir = (end - start).normalized;
		float shortening = Mathf.Min (0.25f, Vector3.Distance (start, end) * 0.25f);
		start += dir * shortening;
		if (!preciseEnd)
			end -= dir * shortening;
		float dist = Vector3.Distance (start, end);
		Vector3 sideDir = new Vector3 (dir.y, -dir.x, 0);
		float bulge = Mathf.Log (dist+1) * 0.4f;
		
		line.SetVertexCount (kPositionCount);
		for (int i=0; i<kPositionCount; i++) {
			float fraction = (float)i / (kPositionCount-1);
			float offset = Mathf.Sin (fraction * Mathf.PI) * bulge;
			Vector3 pos = Vector3.Lerp (start, end, fraction) + sideDir * offset;
			line.SetPosition (i, pos);
		}
		
		line.material.mainTextureScale = new Vector2 (dist * 4, 1);
		line.material.mainTextureOffset = -Vector3.right * Time.time * 0.5f;
	}
}

