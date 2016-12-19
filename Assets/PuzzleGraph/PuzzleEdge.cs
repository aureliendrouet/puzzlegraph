/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;

// Allows walk, ball, vision both ways.
public class PuzzleEdge : PuzzleContainer, PuzzleSerializable {
	public PuzzleNode nodeA;
	public PuzzleNode nodeB;

	private PuzzleElement defaultEdgeElement = new PuzzleDefaultEdge ();
	public override PuzzleElement defaultElement { get { return defaultEdgeElement; } }
	
	public PuzzleEdge () { }
	public PuzzleEdge (PuzzleNode nodeA, PuzzleNode nodeB) {
		this.nodeA = nodeA;
		this.nodeB = nodeB;
	}
	
	public override Point GetPosition () {
		return (nodeA.position + nodeB.position) / 2;
	}
	
	public override string GetName (SerializationTool tool) { return nodeA.name + "-" + nodeB.name;; }
	public override void SetName (SerializationTool tool, string name) {
		string[] nodeNames = name.Split ('-');
		nodeA = tool.puzzle.nodes.Find (e => e.GetName (tool) == nodeNames[0]);
		nodeB = tool.puzzle.nodes.Find (e => e.GetName (tool) == nodeNames[1]);
	}
	
	public void AddStates (Puzzle puzzle, PuzzleStateNode parent) {
		AddStates (puzzle, parent, nodeA, nodeB, true);
		AddStates (puzzle, parent, nodeB, nodeA, false);
	}
	
	public void AddStates (Puzzle puzzle, PuzzleStateNode parent, PuzzleNode nodeA, PuzzleNode nodeB, bool forward) {
		PuzzleState state = parent.state;
		PuzzleState next;
		PuzzleElement e = elementNotNull;
		PuzzleElement n = nodeB.elementNotNull;
		foreach (PuzzleItem player in puzzle.GetItemsInNode<PuzzlePlayer> (state, nodeA)) {
			// Walk
			if (e.CanWalk (puzzle, state, nodeA, nodeB, forward) && n.CanWalk (puzzle, state, nodeA, nodeB, forward)) {
				next = parent.CloneAndAddChild ("Go here", nodeB).state;
				player.SetNode (puzzle, next, nodeB);
			}
			
			// Take or push ball
			List<PuzzleBall> balls = puzzle.GetItemsInNode<PuzzleBall> (state, nodeA);
			if (balls.Count > 0) {
				PuzzleBall ball = balls[0];
				if (e.CanTakeBall (puzzle, state, nodeA, nodeB, forward) && n.CanTakeBall (puzzle, state, nodeA, nodeB, forward)) {
					next = parent.CloneAndAddChild ("Bring ball here", nodeB).state;
					player.SetNode (puzzle, next, nodeB);
					ball.SetNode (puzzle, next, nodeB);
				}
				if (e.CanPushBall (puzzle, state, nodeA, nodeB, forward) && n.CanPushBall (puzzle, state, nodeA, nodeB, forward)) {
					next = parent.CloneAndAddChild ("Push ball here", nodeB).state;
					ball.SetNode (puzzle, next, nodeB);
				}
			}
			
			// Hit toggle at other end
			if (e.CanSee (puzzle, state, nodeA, nodeB, forward) && n.CanSee (puzzle, state, nodeA, nodeB, forward)) {
				PuzzleToggle toggle = nodeB.element as PuzzleToggle;
				if (toggle != null) {
					next = parent.CloneAndAddChild ("Shoot toggle", nodeB).state;
					nodeB.TriggerToggle (puzzle, next);
				}
			}
		}
	}
}

