/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;

public class PuzzleItem : PuzzleSerializable {
	public PuzzleNode defaultNode;
	public override string ToString () {
		 return this.GetType ().Name.Replace ("Puzzle", "");
	}
	
	public PuzzleNode GetNode (Puzzle puzzle, PuzzleState state) {
		return puzzle.GetItemNode (state, this);
	}
	public void SetNode (Puzzle puzzle, PuzzleState state, PuzzleNode node) {
		puzzle.SetItemNode (state, this, node);
	}
	
	public string Serialize (SerializationTool tool) {
		return string.Empty;
	}
	public void Deserialize (SerializationTool tool, string str) {
		
	}
	public string GetName (SerializationTool tool) {
		return defaultNode.name;
	}
	public void SetName (SerializationTool tool, string name) {
		defaultNode = tool.puzzle.nodes.Find (e => e.GetName (tool) == name);
	}
}

public class PuzzlePlayer : PuzzleItem {}

public class PuzzleBall : PuzzleItem {}

