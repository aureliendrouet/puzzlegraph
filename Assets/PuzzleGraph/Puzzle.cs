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

public class Puzzle {
	public string name = string.Empty;
	
	public List<PuzzleNode> nodes = new List<PuzzleNode> ();
	public List<PuzzleEdge> edges = new List<PuzzleEdge> ();
	public List<PuzzleElement> staticElements = new List<PuzzleElement> ();
	public List<PuzzleElement> dynamicElements = new List<PuzzleElement> ();
	public List<PuzzleItem> items = new List<PuzzleItem> ();
	
	// Serialization
	
	public static void Serialize (Puzzle puzzle, TextWriter writer) {
		SerializationTool tool = new SerializationTool (puzzle);
		writer.WriteLine (puzzle.name);
		
		foreach (PuzzleNode node in puzzle.nodes)
			writer.WriteLine (tool.Serialize (node));
		foreach (PuzzleEdge edge in puzzle.edges)
			writer.WriteLine (tool.Serialize (edge));
		foreach (PuzzleElement element in puzzle.staticElements)
			writer.WriteLine (tool.Serialize (element));
		foreach (PuzzleElement element in puzzle.dynamicElements)
			writer.WriteLine (tool.Serialize (element));
		foreach (PuzzleItem item in puzzle.items)
			writer.WriteLine (tool.Serialize (item));	
	}

	public static void Deserialize (Puzzle emptyPuzzle, TextReader reader) {
		SerializationTool tool = new SerializationTool (emptyPuzzle);
		emptyPuzzle.name = reader.ReadLine ();
		
		while (reader.Peek () != -1) {
			string line = reader.ReadLine ();
			PuzzleSerializable obj = tool.DeserializePartly (line);
			if (obj is PuzzleNode)
				emptyPuzzle.AddNode (obj as PuzzleNode);
			else if (obj is PuzzleEdge)
				emptyPuzzle.AddEdge (obj as PuzzleEdge);
			else if (obj is PuzzleElement)
				emptyPuzzle.AddElement (obj as PuzzleElement);
			else if (obj is PuzzleItem)
				emptyPuzzle.AddItem (obj as PuzzleItem);
		}

		tool.Final ();
	}
	
	// Running API
	
	public PuzzleStateNode GetStartState () {
		PuzzleState state = new PuzzleState ();
		state.puzzle = this;
		
		state.elementValues = new object[dynamicElements.Count];
		for (int i=0; i<state.elementValues.Length; i++)
			state.elementValues[i] = dynamicElements[i].defaultValue;
		
		state.itemValues = new int[items.Count];
		for (int i=0; i<state.itemValues.Length; i++)
			state.itemValues[i] = nodes.IndexOf (items[i].defaultNode);
		
		return new PuzzleStateNode (state);
	}
	
	public object GetElementValue (PuzzleState state, PuzzleElement element) {
		int index = dynamicElements.IndexOf (element);
		if (index < 0)
			return null;
		return state.elementValues[index];
	}
	
	public void SetElementValue (PuzzleState state, PuzzleElement element, object value) {
		int index = dynamicElements.IndexOf (element);
		if (index < 0)
			return;
		state.elementValues[index] = value;
	}
	
	public PuzzleNode GetItemNode (PuzzleState state, PuzzleItem item) {
		int index = items.IndexOf (item);
		if (index < 0)
			return null;
		return nodes[state.itemValues[index]];
	}
	
	public void SetItemNode (PuzzleState state, PuzzleItem item, PuzzleNode node) {
		int itemIndex = items.IndexOf (item);
		int nodeIndex = nodes.IndexOf (node);
		if (itemIndex < 0 || nodeIndex < 0)
			return;
		state.itemValues[itemIndex] = nodeIndex;
	}
	
	public List<PuzzleItem> GetItemsInNode (PuzzleState state, PuzzleNode node) {
		List<PuzzleItem> itemList = new List<PuzzleItem> ();
		foreach (PuzzleItem item in items)
			if (item.GetNode (this, state) == node)
				itemList.Add (item);
		return itemList;
	}
	public List<T> GetItemsInNode<T> (PuzzleState state, PuzzleNode node) where T : PuzzleItem {
		List<T> itemList = new List<T> ();
		foreach (PuzzleItem item in items)
			if (item is T && item.GetNode (this, state) == node)
				itemList.Add (item as T);
		return itemList;
	}
	public bool NodeHasItem<T> (PuzzleState state, PuzzleNode node) {
		foreach (PuzzleItem item in items)
			if (item is T && item.GetNode (this, state) == node)
				return true;
		return false;
	}
	
	public Dictionary<PuzzleState, PuzzleStateNode> stateMap = null;
	public PuzzleStateNode startNode = null;
	public List<PuzzleStateNode> stateNodes = null;
	public bool exploredAll = true;
	public int longestPathSteps = 0;
	public int goalPathSteps = 0;
	public int goalStates = 0;
	
	public bool ReplaceIfSameAsExistingNode (PuzzleStateEdge edge) {
		PuzzleStateNode sameState = null;
		if (stateMap.TryGetValue (edge.toNode.state, out sameState)) {
			edge.ReplaceToNode (sameState);
			return true;
		}
		return false;
	}

	public void CheckIsGoal (PuzzleStateNode node) {
		bool anyPlayers = false;
		bool allPlayersInGoal = true;
		foreach (PuzzleItem item in items) {
			PuzzlePlayer player = item as PuzzlePlayer;
			if (player == null)
				continue;
			anyPlayers = true;
			PuzzleElement element = player.GetNode (this, node.state).element;
			if (element == null || !(element is PuzzleGoal))
				allPlayersInGoal = false;
		}
		node.goal = false;
		if (anyPlayers && allPlayersInGoal)
		{
			node.goal = true;
			if (goalPathSteps > 0)
				goalPathSteps = Math.Min (goalPathSteps, node.step);
			else
				goalPathSteps = node.step;
			goalStates++;
		}
	}
	
	public void EvaluatePossibleStates (PuzzleStateNode stateNode) {
		//stateNode.children = new List<PuzzleStateEdge> ();
		
		// Get states from nodes
		foreach (PuzzleNode node in nodes)
			 node.AddStates (this, stateNode);
		
		// Get states from edges
		foreach (PuzzleEdge e in edges)
			e.AddStates (this, stateNode);
		
		// Post-process states
		foreach (PuzzleStateEdge stateEdge in stateNode.outgoing) {
			foreach (PuzzleNode node in nodes) {
				if (node.element != null) {
					node.element.UpdateState (this, stateEdge.toNode.state, node);
				}
			}
			
			// Do last
			if (!ReplaceIfSameAsExistingNode (stateEdge)) {
				stateEdge.directPath = true;

				PuzzleStateNode newNode = stateEdge.toNode;
				CheckIsGoal (newNode);
				longestPathSteps = System.Math.Max (longestPathSteps, newNode.step);
				stateMap[newNode.state] = newNode;
				newNode.id = stateMap.Count - 1;
			}
		}
	}
	
	public void EvaluateTree (int maxDepth) {
		startNode = GetStartState ();
		exploredAll = true;
		longestPathSteps = 0;
		goalPathSteps = 0;
		goalStates = 0;
		stateMap = new Dictionary<PuzzleState, PuzzleStateNode> ();
		stateMap[startNode.state] = startNode;

		List<PuzzleStateNode> goalNodes = new List<PuzzleStateNode> ();
		Queue<PuzzleStateNode> queue = new Queue<PuzzleStateNode> ();
		queue.Enqueue (startNode);

		while (queue.Count > 0) {
			PuzzleStateNode node = queue.Dequeue ();
			if (node.goal) {
				goalNodes.Add (node);
			}
			else if (node.step >= maxDepth) {
				exploredAll = false;
			}
			else {
				EvaluatePossibleStates (node);
				foreach (PuzzleStateEdge edge in node.outgoing)
					if (edge.directPath)
						queue.Enqueue (edge.toNode);
			}
		}
			
		for (int i = goalNodes.Count - 1; i >= 0; i--) {
			PuzzleStateNode node = goalNodes[i];
			while (node != null) {
				if (node.goalPath)
					break;
				node.goalPath = true;
				node.stuck = false;

				PuzzleStateNode newNode = null;
				foreach (var edge in node.ingoing) {
					if (edge.directPath)
						newNode = edge.fromNode;
					else
						queue.Enqueue (edge.fromNode);
				}
				node = newNode;
			}
		}

		while (queue.Count > 0) {
			PuzzleStateNode node = queue.Dequeue ();
			if (node.stuck == false)
				continue;
			node.stuck = false;
			foreach (PuzzleStateEdge edge in node.ingoing)
				queue.Enqueue (edge.fromNode);
		}

		stateNodes = stateMap.Values.OrderBy (e => e.id).ToList ();
	}
	
	// Editing API
	
	public PuzzleContainer GetElementContainer (PuzzleElement element) {
		foreach (PuzzleNode node in nodes)
			if (node.element == element)
				return node;
		foreach (PuzzleEdge edge in edges)
			if (edge.element == element)
				return edge;
		return null;
	}
	
	public delegate void NodeFunction (PuzzleNode node);
	public delegate void EdgeFunction (PuzzleEdge edge);
	public delegate void ElementFunction (PuzzleElement element);
	public delegate void ItemFunction (PuzzleItem item);
	public delegate void ReceiverFunction (PuzzleNode node, PuzzleReceiverElement receiver);
	public NodeFunction addNode;
	public NodeFunction removeNode;
	public EdgeFunction addEdge;
	public EdgeFunction removeEdge;
	public ElementFunction addElement;
	public ElementFunction removeElement;
	public ItemFunction addItem;
	public ItemFunction removeItem;
	public ReceiverFunction addReceiver;
	public ReceiverFunction removeReceiver;
	
	public void AddNode (PuzzleNode node) {
		nodes.Add (node);
		if (string.IsNullOrEmpty (node.name)) {
			char testChar = 'A';
			while (true) {
				bool unique = true;
				foreach (PuzzleNode n in nodes) {
					if (n.name.Length > 0 && n.name[0] == testChar) {
						unique = false;
						break;
					}
				}
				if (unique)
					break;
				else
					testChar++;
			}
			node.name = string.Empty + testChar;
		}
		addNode (node);
	}
	public void RemoveNode (PuzzleNode node) {
		// Remove edges referencing node
		for (int i=edges.Count-1; i>=0; i--)
			if (edges[i].nodeA == node || edges[i].nodeB == node)
				RemoveEdge (edges[i]);
		// Remove element
		if (node.element != null)
			RemoveElement (node.element);
		// Remove items
		for (int i=0; i<items.Count; i++)
			if (items[i].defaultNode == node)
				RemoveItem (items[i]);
		// Remove node
		nodes.Remove (node);
		removeNode (node);
	}
	
	public void AddEdge (PuzzleEdge edge) {
		edges.Add (edge);
		addEdge (edge);
	}
	public void RemoveEdge (PuzzleEdge edge) {
		// Remove element
		if (edge.element != null)
			RemoveElement (edge.element);
		// Remove edge
		edges.Remove (edge);
		removeEdge (edge);
	}
	
	public void AddElement (PuzzleElement element) {
		if (element.dynamic)
			dynamicElements.Add (element);
		else
			staticElements.Add (element);
		addElement (element);
	}
	public void RemoveElement (PuzzleElement element) {
		// Remove receivers from this node.
		PuzzleNode node = GetElementContainer (element) as PuzzleNode;
		if (node != null) {
			for (int i = node.receiverElements.Count - 1; i >= 0; i--) {
				RemoveReceiver (node, node.receiverElements[i]);
			}
		}

		// Do callback first so it can perform proper lookup in puzzle
		removeElement (element);
		
		// Remove element from edges and nodes
		for (int i=0; i<edges.Count; i++)
			if (edges[i].element == element)
				edges[i].element = null;
		for (int i=0; i<nodes.Count; i++) {
			if (nodes[i].element == element)
				nodes[i].element = null;
		}
		PuzzleReceiverElement receiverElement = element as PuzzleReceiverElement;
		if (receiverElement != null) {
			for (int i=0; i<nodes.Count; i++) {
				if (nodes[i].receiverElements.Contains (receiverElement))
					RemoveReceiver (nodes[i], receiverElement);
			}
		}
		
		// Remove element
		if (element.dynamic)
			dynamicElements.Remove (element);
		else
			staticElements.Remove (element);
	}
	
	public List<PuzzleItem> GetDefaultItemsInNode (PuzzleNode node) {
		List<PuzzleItem> nodeItems = new List<PuzzleItem> ();
		foreach (PuzzleItem item in items)
			if (item.defaultNode == node)
				nodeItems.Add (item);
		return nodeItems;
	}
	
	public void AddItem (PuzzleItem item) {
		items.Add (item);
		addItem (item);
	}
	public void RemoveItem (PuzzleItem item) {
		items.Remove (item);
		removeItem (item);
	}

	public void AddReceiver (PuzzleNode node, PuzzleReceiverElement receiver) {
		node.AddReceiver (receiver);
		if (addReceiver != null)
			addReceiver (node, receiver);
	}
	public void RemoveReceiver (PuzzleNode node, PuzzleReceiverElement receiver) {
		if (removeReceiver != null)
			removeReceiver (node, receiver);
		node.RemoveReceiver (receiver);
	}
}
