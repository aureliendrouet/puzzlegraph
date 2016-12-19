/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;

public class PuzzleElement : PuzzleSerializable {
	public virtual void OnEvent (bool state) {}
	public override string ToString () {
		 return this.GetType ().Name.Replace ("Puzzle", "");
	}
	public virtual string ToString (Puzzle puzzle, PuzzleState state) {
		 return ToString ();
	}
	
	public virtual void Serialize (Puzzle puzzle, System.IO.TextWriter writer) { }
	
	public virtual bool CanWalk (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return true; }
	public virtual bool CanTakeBall (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return true; }
	public virtual bool CanPushBall (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) {
		// A ball can be pushed from one node to another if both the edge and the receiving node allow it.
		// We want nodes to allow it by default if they allow bringing a ball.
		// For edges we want to now allow it by default in order to reduce state space edge explosion.
		// But it can be allowed by overriding for certain edge types where it can make a vital difference.
		return this is PuzzleEdgeElement ? false : CanTakeBall (puzzle, state, nodeA, nodeB, forward);
	}
	public virtual bool CanSee (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return true; }
	
	public virtual bool dynamic { get { return false; } }
	public virtual object defaultValue { get { return null; } }
	
	public virtual void UpdateState (Puzzle puzzle, PuzzleState state, PuzzleNode node) { }
	
	public virtual string Serialize (SerializationTool tool) {
		return string.Empty;
	}
	public virtual void Deserialize (SerializationTool tool, string str) {
		
	}
	public string GetName (SerializationTool tool) {
		return tool.puzzle.GetElementContainer (this).GetName (tool);
	}
	public void SetName (SerializationTool tool, string name) {
		if (name.Contains ("-"))
			tool.puzzle.edges.Find (e => e.GetName (tool) == name).element = this;
		else
			tool.puzzle.nodes.Find (e => e.GetName (tool) == name).element = this;
	}
}

public interface PuzzleEdgeElement {}
public interface PuzzleNodeElement {}

public abstract class PuzzleBoolElement : PuzzleElement {
	public delegate void StateNotification ();
	public StateNotification updateState;
	
	private bool m_DefaultOn = false;
	public bool defaultOn {
		get { return m_DefaultOn; }
		set {
			if (value == m_DefaultOn)
				return;
			m_DefaultOn = value;
			updateState ();
		}
	}
	
	public override bool dynamic { get { return true; } }
	public override object defaultValue { get { return defaultOn; } }
	
	public bool GetValue (Puzzle puzzle, PuzzleState state) {
		return (bool)puzzle.GetElementValue (state, this);
	}
	public void SetValue (Puzzle puzzle, PuzzleState state, bool value) {
		puzzle.SetElementValue (state, this, value);
	}
	public void ToggleValue (Puzzle puzzle, PuzzleState state) {
		SetValue (puzzle, state, !GetValue (puzzle, state));
	}
	
	public override string Serialize (SerializationTool tool) {
		return defaultOn ? "on" : "off";
	}
	public override void Deserialize (SerializationTool tool, string str) {
		defaultOn = (str == "on");
	}
}

public class PuzzleReceiverElement : PuzzleBoolElement {
	
}

public class PuzzleTriggerElement : PuzzleBoolElement, PuzzleNodeElement {
	public override string ToString (Puzzle puzzle, PuzzleState state) {
		 return base.ToString () + "\n" + (GetValue (puzzle, state) ? "On" : "Off");
	}
}


// Nodes

public class PuzzleDefaultNode : PuzzleElement, PuzzleNodeElement { }

// The puzzle is solved when all players are on a goal location. Balls can't be brought here.
public class PuzzleGoal : PuzzleElement, PuzzleNodeElement {
	public override bool CanTakeBall (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return false; }
}

// Can be pressed by player or ball standing on it. Remains active once pressed.
public class PuzzleButton : PuzzleTriggerElement {
	public override void UpdateState (Puzzle puzzle, PuzzleState state, PuzzleNode node) {
		bool hasPlayer = puzzle.NodeHasItem<PuzzlePlayer> (state, node);
		bool hasBall = puzzle.NodeHasItem<PuzzleBall> (state, node);
		if (hasPlayer || hasBall)
			node.TriggerValue (puzzle, state, true);
	}
}

// Activates when ball is on it. Deactivates when ball is moved away.
public class PuzzlePlate : PuzzleTriggerElement {
	public override void UpdateState (Puzzle puzzle, PuzzleState state, PuzzleNode node) {
		bool hasBall = puzzle.NodeHasItem<PuzzleBall> (state, node);
		node.TriggerValue (puzzle, state, hasBall);
	}
}

// Can be toggled by player hitting it up close or in adjecent node if edge allows vision.
public class PuzzleToggle : PuzzleTriggerElement { }

// Player can only go here when bridge is passable. Balls can't be brought here.
public class PuzzleBridge : PuzzleReceiverElement, PuzzleNodeElement {
	public override bool CanWalk (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return GetValue (puzzle, state); }
	public override bool CanTakeBall (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return false; }
	public override string ToString (Puzzle puzzle, PuzzleState state) {
		 return base.ToString () + "\n" + (GetValue (puzzle, state) ? "Safe" : "Hole");
	}
}


// Edges

public class PuzzleDefaultEdge : PuzzleElement, PuzzleEdgeElement { }

// Player can bring or push a ball one way here, but not the other. No vision.
public class PuzzleStep : PuzzleElement, PuzzleEdgeElement {
	public override bool CanTakeBall (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return forward; }
	public override bool CanPushBall (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return forward; }
	public override bool CanSee (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return false; }
}

// Player can jump down the steep side or push a ball down.
// Player may only go the other way if a ball is present (to climb on), leaving the ball behind. No vision.
public class PuzzleWall : PuzzleElement, PuzzleEdgeElement {
	public override bool CanWalk (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) {
		return forward || (puzzle.GetItemsInNode<PuzzleBall> (state, nodeA).Count > 0);
	}
	public override bool CanTakeBall (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return forward; }
	public override bool CanPushBall (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return forward; }
	public override bool CanSee (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return false; }
}

// Player can only go one way or push a ball one way. No vision.
public class PuzzleOneWay : PuzzleElement, PuzzleEdgeElement {
	public override bool CanWalk (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return forward; }
	public override bool CanTakeBall (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return forward; }
	public override bool CanPushBall (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return forward; }
	public override bool CanSee (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return false; }
}

// Balls can't go through here. No vision.
public class PuzzleNoBall : PuzzleElement, PuzzleEdgeElement {
	public override bool CanTakeBall (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return false; }
	public override bool CanSee (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return false; }
}

// Allows vision, but player and balls can't go through here.
public class PuzzleVision : PuzzleElement, PuzzleEdgeElement {
	public override bool CanWalk (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return false; }
	public override bool CanTakeBall (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return false; }
	public override bool CanSee (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return true; }
}

// When passable, allows player and ball to go trough, as well as vision.
public class PuzzleGate : PuzzleReceiverElement, PuzzleEdgeElement {
	public override bool CanWalk (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) {  return GetValue (puzzle, state); }
	public override bool CanTakeBall (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return GetValue (puzzle, state); }
	public override bool CanSee (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return GetValue (puzzle, state); }
	public override string ToString (Puzzle puzzle, PuzzleState state) {
		 return base.ToString () + "\n" + (GetValue (puzzle, state) ? "Open" : "Closed");
	}
}

// When hazard is active, player can only go here when bringing a ball (for protection).
public class PuzzleBlockableHazard : PuzzleReceiverElement, PuzzleEdgeElement {
	public override bool CanWalk (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return GetValue (puzzle, state); }
	public override bool CanTakeBall (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return true; }
	public override string ToString (Puzzle puzzle, PuzzleState state) {
		 return base.ToString () + "\n" + (GetValue (puzzle, state) ? "Safe" : "Hazard");
	}
}

// Player can push a ball through but not go self.
public class PuzzleBallTrack : PuzzleElement, PuzzleEdgeElement {
	public override bool CanWalk (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return false; }
	public override bool CanTakeBall (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return false; }
	public override bool CanPushBall (Puzzle puzzle, PuzzleState state, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) { return true; }
}

