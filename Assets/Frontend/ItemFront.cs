/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using UnityEngine;
using System.Collections;

[System.Serializable]
public class ItemData {
	public string name;
	public Sprite image;
	
	private System.Type m_Type = null;
	public System.Type type {
		get {
			if (m_Type == null)
				m_Type = typeof (Puzzle).Assembly.GetType ("Puzzle"+name);
			if (m_Type == null)
				Debug.Log ("Couldn't find type with name "+name);
			return m_Type;
		}
	}
}

public class ItemFront : MonoBehaviour {
	PuzzleItem m_Item;
	public PuzzleItem item { get { return m_Item; } }
	
	public SpriteRenderer staticSprite;
	public ItemData[] items;
	
	private PuzzleFront m_Puzzle = null;
	public PuzzleFront puzzle { get { return m_Puzzle; } }
	
	public void Set (PuzzleFront puzzle, PuzzleItem item) {
		m_Puzzle = puzzle;
		m_Item = item;
		
		transform.parent = puzzle.transform;
		transform.position = puzzle.GetNode (item.defaultNode).transform.position;
		SetItem (item);
	}
	
	public void SetNodePos (PuzzleNode node, PuzzleStateNode state) {
		transform.position = GetPositionInNode (node, state);
	}
	
	public IEnumerator AnimateNode (PuzzleNode oldNode, PuzzleNode newNode, PuzzleStateNode state, bool animate) {
		Vector3 newPos = GetPositionInNode (newNode, state);
		if (animate) {
			Vector3 oldPos = transform.position;
			float startTime = Time.time;
			while (Time.time < startTime + PuzzleFront.moveDuration) {
				transform.position = Vector3.Lerp (oldPos, newPos, (Time.time - startTime) / PuzzleFront.moveDuration);
				yield return 0;
			}
		}
		transform.position = newPos;
	}

	private Vector3 GetPositionInNode (PuzzleNode node, PuzzleStateNode state) {
		Vector3 newPos = puzzle.GetNode (node).transform.position;

		if (state != null) {
			var items = puzzle.puzzle.GetItemsInNode (state.state, node);

			if (items.Count > 1) {
				int index = items.IndexOf (item);
				float offset = (index + 0.5f) / items.Count * 2 - 1;
				newPos += Vector3.right * offset * 0.3f;
			}
		}

		return newPos;
	}
	
	private void SetItem (PuzzleItem puzzleItem) {
		string name = puzzleItem.GetType ().Name.Replace ("Puzzle", "");
		foreach (var item in items) {
			if (item.name == name) {
				staticSprite.sprite = item.image;
			}
		}
	}
	
	private void SetTint (Color color) {
		staticSprite.color = color;
	}
}

