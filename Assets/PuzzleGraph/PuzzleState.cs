/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System;

public class PuzzleStateNode {
	public PuzzleState state;
	public List<PuzzleStateEdge> ingoing = new List<PuzzleStateEdge> ();
	public List<PuzzleStateEdge> outgoing = new List<PuzzleStateEdge> ();
	public bool goal = false;
	public bool goalPath = false;
	public bool stuck = true;
	public int step = 0;
	public int id = 0;

	public Pointf point;

	public PuzzleStateNode (PuzzleState state) {
		this.state = state;
	}

	public PuzzleStateNode CloneAndAddChild (string name, PuzzleNode node) {
		PuzzleStateNode n = new PuzzleStateNode (state.Clone ());
		n.step = step + 1;
		new PuzzleStateEdge (name, node, this, n);
		return n;
	}
}

public class PuzzleStateEdge {
	public string name;
	public PuzzleNode actionNode = null;
	public PuzzleStateNode fromNode = null;
	public PuzzleStateNode toNode = null;
	public bool directPath = false;

	public PuzzleStateEdge (
		string name,
		PuzzleNode actionNode,
		PuzzleStateNode fromNode,
		PuzzleStateNode toNode
	) {
		this.name = name;
		this.actionNode = actionNode;
		this.fromNode = fromNode;
		this.toNode = toNode;
		fromNode.outgoing.Add (this);
		toNode.ingoing.Add (this);
	}

	public void ReplaceToNode (PuzzleStateNode newToNode) {
		toNode.ingoing.Remove (this);
		toNode = newToNode;
		toNode.ingoing.Add (this);
	}
}

[System.Serializable]
public class PuzzleState {
	
	public object[] elementValues;
	public int[] itemValues;

	[System.NonSerialized]
	public Puzzle puzzle;
	
	// Deep copy in separeate memory space
	public PuzzleState Clone () {
		MemoryStream ms = new MemoryStream ();
		BinaryFormatter bf = new BinaryFormatter ();
		bf.Serialize (ms, this);
		ms.Position = 0;
		PuzzleState state = (PuzzleState)bf.Deserialize (ms);
		ms.Close ();
		state.puzzle = puzzle;
		return state;
	}
	
	public override int GetHashCode () {
		unchecked { // Overflow is fine, just wrap
			int hash = 17;
			
			// Elements need to contribute in fixed order.
			// Toggle in Node 1 on, toggle in Node 2 off is not the same as
			// toggle in Node 1 off, toggle in Node 2 on.
			foreach (object e in elementValues)
				hash = hash * 23 + e.GetHashCode ();
			
			// Items need to contribute in sorted order.
			// Ball in Node 1 and ball in Node 2 is the same as
			// ball in Node 2 and ball in Node 1.
			List<Point> itemTypeValuePairs = new List<Point> ();
			for (int i = 0; i < puzzle.items.Count; i++) {
				itemTypeValuePairs.Add (
					new Point (
						puzzle.items[i].GetType ().GetHashCode (),
						itemValues[i]
					)
				);
			}
			itemTypeValuePairs = itemTypeValuePairs.OrderBy (e => e.x).ThenBy (e => e.y).ToList ();
			foreach (Point p in itemTypeValuePairs)
				hash = hash * 23 + (p.x * 100 + p.y);
			
			return hash;
		}
	}

	public override bool Equals (object obj) {
		return obj.GetHashCode () == GetHashCode ();
	}
	public static bool operator == (PuzzleState a, PuzzleState b) {
		if (System.Object.ReferenceEquals (a, b))
		    return true;
		
		if (((object)a == null) || ((object)b == null))
		    return false;
		
		return a.Equals (b);
	}
	public static bool operator != (PuzzleState a, PuzzleState b) { return !(a == b); }
}
