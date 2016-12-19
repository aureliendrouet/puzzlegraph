/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Linq;

public sealed class PuzzleNode : PuzzleContainer, PuzzleSerializable {
	public string name = "";

	private PuzzleElement defaultNodeElement = new PuzzleDefaultNode ();
	public override PuzzleElement defaultElement { get { return defaultNodeElement; } }
	
	public delegate void PositionSetter (Point position);
	public PositionSetter setPosition;
	
	private Point m_Position;
	public Point position { get { return m_Position; } }
	public List<PuzzleReceiverElement> receiverElements = new List<PuzzleReceiverElement> ();
	
	public void SetPosition (Point position) {
		m_Position = position;
		if (setPosition != null)
			setPosition (position);
	}
	
	public void AddReceiver (PuzzleReceiverElement receiver) {
		receiverElements.Add (receiver);
	}
	
	public void RemoveReceiver (PuzzleReceiverElement receiver) {
		receiverElements.Remove (receiver);
	}
	
	public void TriggerValue (Puzzle puzzle, PuzzleState state, bool value) {
		PuzzleTriggerElement e = element as PuzzleTriggerElement;
		if (e.GetValue (puzzle, state) == value)
			return;
		e.SetValue (puzzle, state, value);
		foreach (PuzzleReceiverElement receiver in receiverElements)
			receiver.SetValue (puzzle, state, value);
	}
	
	public void TriggerToggle (Puzzle puzzle, PuzzleState state) {
		PuzzleTriggerElement e = element as PuzzleTriggerElement;
		e.SetValue (puzzle, state, !(bool)e.GetValue (puzzle, state));
		foreach (PuzzleReceiverElement receiver in receiverElements)
			receiver.ToggleValue (puzzle, state);
	}
	
	public override Point GetPosition () { return position; }
	
	public void AddStates (Puzzle puzzle, PuzzleStateNode parent) {
		PuzzleState state = parent.state;
		PuzzleState next;
		if (puzzle.GetItemsInNode<PuzzlePlayer> (state, this).Count > 0) {
			// Activate toggle
			PuzzleToggle toggle = element as PuzzleToggle;
			if (toggle != null) {
				next = parent.CloneAndAddChild ("Press toggle", this).state;
				TriggerToggle (puzzle, next);
			}
		}
	}
	
	public override string Serialize (SerializationTool tool) {
		string str = string.Empty;
		str += "pos:"+position.x+","+position.y;
		if (receiverElements.Count > 0) {
			string[] names = receiverElements.Select (e => tool.puzzle.GetElementContainer (e).GetName (tool)).ToArray ();
			str += " notify:"+string.Join (",", names);
		}
		return str;
	}
	public override void Deserialize (SerializationTool tool, string str) {
		string[] tokens = str.Split (new char[] {' '}, System.StringSplitOptions.RemoveEmptyEntries);
		int pos = 0;
		string[] coords = tokens[pos].Substring (4).Split (',');
		SetPosition (new Point (int.Parse (coords[0]), int.Parse (coords[1])));
		pos++;
		while (pos < tokens.Length) {
			if (tokens[pos].StartsWith ("notify:")) {
				string[] receiverNames = tokens[pos].Substring (7).Split (',');
				foreach (string name in receiverNames) {
					PuzzleElement receiver = tool.puzzle.dynamicElements.Find (e => e.GetName (tool) == name);
					tool.puzzle.AddReceiver (this, receiver as PuzzleReceiverElement);
				}
			}
			pos++;
		}
	}
	public override string GetName (SerializationTool tool) { return name; }
	public override void SetName (SerializationTool tool, string name) { this.name = name; }
}
