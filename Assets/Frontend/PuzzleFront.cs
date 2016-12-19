/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using UnityEngine;
using System.Collections.Generic;

public class PuzzleFront : MonoBehaviour {

	// Public member variables and properties

	public bool playing = false;
	public float stateChangeTime = -1;
	public float itemChangeTime = -1;
	public float doneTime = -1;

	private PuzzleStateNode m_State;
	public PuzzleStateNode state { get { return m_State; } }

	public NodeFront nodePrefab;
	public EdgeFront edgePrefab;
	public ItemFront itemPrefab;

	public List<NodeFront> nodes;
	public List<EdgeFront> edges;
	public List<ItemFront> items;

	private Puzzle m_Puzzle;
	public Puzzle puzzle { get { return m_Puzzle; } }

	// Public static variables and properties
	
	public static float moveDuration = 0.5f;
	public static float itemAndEffectDuration = 1.0f;
	public static float itemDelay = 0.2f;
	public static float effectDelay = 0.2f;
	
	public static Color selectionColor = new Color (0.6f, 0.8f, 1.0f);
	public static Color normalColor = new Color (0.9f, 0.9f, 0.9f);
	public static Color hoverColor = Color.Lerp (normalColor, selectionColor, 0.4f);
	
	public enum MouseTool {
		Connect,
		Move,
		Trigger
	}
	
	private static MouseTool s_Tool = MouseTool.Connect;
	public static MouseTool tool { get { return s_Tool; } set { s_Tool = value; } }
	
	private static ContainerFront s_Selected = null;
	public static ContainerFront selected { get { return s_Selected; } set { s_Selected = value; } }

	private static ContainerFront s_Hovered = null;
	public static ContainerFront hovered { get { return s_Hovered; } }

	// Private member variables and properties

	Stack<PuzzleStateNode> undoStack = new Stack<PuzzleStateNode> ();

	// Private static variables and properties

	static NodeFront s_DragStartNode = null;
	static Vector2 s_MouseDownWorldPos = Vector3.zero;
	static Vector2 s_MouseDownNodePos = Vector3.zero;
	static bool s_DraggingThresholdPassed = false;

	static EdgeFront s_TempDragEdge;
	static NodeFront s_TempDragNode;
	static PuzzleGate s_TempDragReceiver;

	// Methods
	
	public void CreatePuzzle (Puzzle puzzle) {
		m_Puzzle = puzzle;
		
		puzzle.addNode += AddNode;
		puzzle.removeNode += RemoveNode;
		puzzle.addEdge += AddEdge;
		puzzle.removeEdge += RemoveEdge;
		puzzle.addElement += AddElement;
		puzzle.removeElement += RemoveElement;
		puzzle.addItem += AddItem;
		puzzle.removeItem += RemoveItem;
		puzzle.addReceiver += AddReceiver;
		puzzle.removeReceiver += RemoveReceiver;
		
		if (s_TempDragEdge)
			Destroy (s_TempDragEdge.gameObject);
		s_TempDragEdge = Instantiate (edgePrefab) as EdgeFront;
		s_TempDragEdge.Set (this, null);
		s_TempDragEdge.gameObject.SetActive (false);
		s_TempDragEdge.transform.parent = transform;

		if (s_TempDragNode)
			Destroy (s_TempDragNode.gameObject);
		s_TempDragNode = Instantiate (nodePrefab) as NodeFront;
		s_TempDragNode.Set (this, null);
		s_TempDragNode.gameObject.SetActive (false);
		s_TempDragNode.transform.parent = transform;
		
		s_TempDragReceiver = new PuzzleGate ();
	}
	
	public Bounds GetBounds () {
		if (nodes.Count == 0)
			return new Bounds ();
		Bounds bounds = new Bounds (nodes[0].transform.position, Vector3.one);
		for (int i=1; i<nodes.Count; i++) {
			bounds.Encapsulate (new Bounds (nodes[i].transform.position, Vector3.one));
		}
		return bounds;
	}
	
	public static Vector2 PointToVector (Point p) {
		return new Vector2 (p.x, p.y);
	}
	
	public static Point VectorToPoint (Vector2 p) {
		return new Point (Mathf.FloorToInt (p.x + 0.5f), Mathf.FloorToInt (p.y + 0.5f));
	}
	
	Vector2 GetMousePos () {
		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		float dist = 0;
		new Plane (-Vector3.forward, Vector3.zero).Raycast (ray, out dist);
		return ray.GetPoint (dist);
	}
	
	public static Vector2 Snap (Vector2 pos) {
		pos.x = Mathf.Round (pos.x / 2);
		pos.y = Mathf.Round (pos.y / 2);
		return pos;
	}
	
	public void Update () {
		if (!playing)
			UpdateEditing ();
	}
	
	public void Reset () {
		undoStack.Clear ();
		m_State = puzzle.startNode;

		foreach (var element in puzzle.dynamicElements) {
			PuzzleContainer container = puzzle.GetElementContainer (element);
			ContainerFront front = GetContainer (container);
			front.SetElement (element);
		}

		foreach (var item in puzzle.items) {
			PuzzleNode newNode = item.defaultNode;
			ItemFront front = GetItem (item);
			front.SetNodePos (newNode, state);
		}
	}

	public bool HasUndo () {
		return undoStack.Count > 0;
	}

	public void Undo () {
		PuzzleStateNode state = undoStack.Pop ();
		SetState (state, true);
	}

	public void GoToState (PuzzleStateNode newState) {
		undoStack.Push (state);
		SetState (newState, true);
	}

	private void SetState (PuzzleStateNode newState, bool animate) {
		PuzzleStateNode lastState = state;
		m_State = newState;
		
		stateChangeTime = Time.time;
		if (animate) {
			itemChangeTime = stateChangeTime + itemDelay;
			doneTime = stateChangeTime;
		}
		else {
			itemChangeTime = stateChangeTime;
			doneTime = stateChangeTime;
		}

		bool itemChange = false;
		foreach (var element in puzzle.dynamicElements) {
			object oldVal = puzzle.GetElementValue (lastState.state, element);
			object newVal = puzzle.GetElementValue (newState.state, element);
			if (!newVal.Equals (oldVal)) {
				PuzzleContainer container = puzzle.GetElementContainer (element);
				ContainerFront front = GetContainer (container);
				StartCoroutine (front.AnimateValue ((bool)oldVal, (bool)newVal, animate));
				itemChange = true;
			}
		}
		if (itemChange && animate) {
			doneTime += itemAndEffectDuration;
		}

		bool movement = false;
		foreach (var item in puzzle.items) {
			PuzzleNode oldNode = puzzle.GetItemNode (lastState.state, item);
			PuzzleNode newNode = puzzle.GetItemNode (newState.state, item);
			if (newNode != oldNode)
				movement = true;
			ItemFront front = GetItem (item);
			// Animate regardless of whether old and new node are the same.
			// Movement of other items may cause static item to change offset.
			StartCoroutine (front.AnimateNode (oldNode, newNode, state, animate));
		}
		if (movement && animate) {
			itemChangeTime += moveDuration;
			doneTime += moveDuration;
		}
	}
	
	public void UpdateEditing () {
		// Drag
		if (s_DragStartNode != null) {
			if (!s_DraggingThresholdPassed) {
				if (Vector2.Distance (GetMousePos (), s_MouseDownWorldPos) > 0.1f) {
					s_DraggingThresholdPassed = true;
				}
			}
			switch (tool) {
				case MouseTool.Move: HandleMoveDrag (); break;
				case MouseTool.Connect: HandleEdgeDrag (); break;
				case MouseTool.Trigger: HandleTriggerDrag (); break;
			}
		}
		
		// Delete
		if (selected != null && Input.GetKeyDown (KeyCode.Delete) || Input.GetKeyDown (KeyCode.Backspace)) {
			if (selected is NodeFront && puzzle.nodes.Count > 1)
				puzzle.RemoveNode (selected.container as PuzzleNode);
			else if (selected is EdgeFront)
				puzzle.RemoveEdge (selected.container as PuzzleEdge);
		}
	}
	
	void HandleEdgeDrag () {
		if (Input.GetMouseButtonUp (0)) {
			s_TempDragEdge.gameObject.SetActive (false);
			if (hovered is NodeFront && hovered != s_DragStartNode) {
				NodeFront hoveredNode = (hovered as NodeFront);

				// Add new node and edge to it.
				if (hovered == s_TempDragNode) {
					PuzzleNode newNode = new PuzzleNode ();
					newNode.SetPosition (VectorToPoint (Snap (hoveredNode.transform.position)));
					puzzle.AddNode (newNode);

					foreach (NodeFront node in nodes) {
						if (node.node == newNode) {
							s_Selected = node;
						}
					}

					PuzzleEdge edge = new PuzzleEdge (s_DragStartNode.node, newNode);
					puzzle.AddEdge (edge);
				}

				// Add edge to existing node.
				else {
					PuzzleEdge edge = new PuzzleEdge (s_DragStartNode.node, hoveredNode.node);
					puzzle.AddEdge (edge);
					foreach (EdgeFront edgeFront in edges) {
						if (edgeFront.edge == edge) {
							s_Selected = edgeFront;
						}
					}
				}
			}
			s_DragStartNode = null;
			s_TempDragNode.gameObject.SetActive (false);
		}
		else {
			Vector2 pos = GetMousePos ();
			if (hovered is NodeFront)
				pos = hovered.transform.position;
			s_TempDragEdge.SetPositions (s_TempDragEdge.transform.position, pos);

			Vector2 snapped = Snap (pos);
			Point position = VectorToPoint (snapped);
			bool validNew = true;
			if (Vector2.Distance (snapped, pos * 0.5f) > 0.25f) {
				validNew = false;
			}
			else {
				foreach (PuzzleNode node in puzzle.nodes) {
					if (node.position == position) {
						validNew = false;
						break;
					}
				}
			}

			s_TempDragNode.gameObject.SetActive (validNew);
			s_TempDragNode.SetPoint (position);
		}
	}
		
	void HandleTriggerDrag () {
		if (Input.GetMouseButtonUp (0)) {
			s_DragStartNode.RemoveReceiver (s_TempDragReceiver);
			if (hovered != null && hovered.container.element is PuzzleReceiverElement) {
				puzzle.AddReceiver (s_DragStartNode.node, hovered.container.element as PuzzleReceiverElement);
			}
			s_DragStartNode = null;
		}
		else {
			Vector2 pos = GetMousePos ();
			if (hovered != null && hovered.container.element is PuzzleReceiverElement) {
				pos = hovered.GetElementPosition ();
				s_DragStartNode.UpdateReceiver (s_TempDragReceiver, pos, false);
			}
			else {
				s_DragStartNode.UpdateReceiver (s_TempDragReceiver, pos, true);
			}
		}
	}
	
	void HandleMoveDrag () {
		if (Input.GetMouseButtonUp (0)) {
			s_DragStartNode.node.SetPosition (VectorToPoint (Snap (GetMousePos ())));
			s_DragStartNode = null;
		}
		else {
			if (s_DraggingThresholdPassed)
				s_DragStartNode.SetPosition (GetMousePos () - s_MouseDownWorldPos + s_MouseDownNodePos);
		}
	}
	
	bool IsValidHover (ContainerFront container) {
		if (playing)
			return false;
		if (s_DragStartNode == null)
			return true;
		if (tool == MouseTool.Move)
			return false;
		if (tool == MouseTool.Connect)
			return (container is NodeFront && container != selected);
		if (tool == MouseTool.Trigger)
			return ((container.container.element as PuzzleReceiverElement) != null);
		return false;
	}
	
	public void OnContainerMouseEnter (ContainerFront container) {
		if (playing)
			return;
		if (IsValidHover (container))
			s_Hovered = container;
	}
	
	public void OnContainerMouseExit (ContainerFront container) {
		if (playing)
			return;
		if (s_Hovered == container)
			s_Hovered = null;
	}
	
	public void OnContainerMouseOver (ContainerFront container) {
		
	}
	
	public void OnContainerMouseDown (ContainerFront container) {
		if (playing)
			return;
		s_Selected = container;
		s_MouseDownWorldPos = GetMousePos ();
		s_MouseDownNodePos = container.transform.position;
		s_DraggingThresholdPassed = false;
		
		NodeFront nodeFront = container as NodeFront;
		
		if (tool == MouseTool.Move) {
			s_DragStartNode = nodeFront;
		}
		
		if (tool == MouseTool.Connect) {
			if (nodeFront != null) {
				s_DragStartNode = nodeFront;
				s_TempDragEdge.gameObject.SetActive (true);
				s_TempDragEdge.transform.position = container.transform.position;
				s_TempDragEdge.transform.localScale = new Vector3 (0.1f,1,1);
			}
		}
		
		if (tool == MouseTool.Trigger) {
			if (nodeFront != null && (nodeFront.container.element as PuzzleTriggerElement) != null) {
				s_DragStartNode = nodeFront;
				nodeFront.AddReceiver (s_TempDragReceiver);
			}
		}
	}
	
	public void OnContainerMouseUp (ContainerFront container) {
		
	}
	
	public ContainerFront GetContainer (PuzzleContainer container) {
		if (container is PuzzleEdge)
			return GetEdge (container as PuzzleEdge);
		else
			return GetNode (container as PuzzleNode);
	}
	
	public NodeFront GetNode (PuzzleNode node) {
		foreach (NodeFront nodeFront in nodes)
			if (nodeFront.node == node)
				return nodeFront;
		return null;
	}
	
	public EdgeFront GetEdge (PuzzleEdge edge) {
		foreach (EdgeFront edgeFront in edges)
			if (edgeFront.edge == edge)
				return edgeFront;
		return null;
	}
	
	public ItemFront GetItem (PuzzleItem item) {
		foreach (ItemFront itemFront in items)
			if (itemFront.item == item)
				return itemFront;
		return null;
	}
	
	// Callbacks
	
	public void AddNode (PuzzleNode node) {
		NodeFront nodeFront = Instantiate (nodePrefab) as NodeFront;
		nodeFront.transform.parent = transform;
		nodeFront.gameObject.name = "Node "+node.name;
		nodeFront.Set (this, node);
		nodes.Add (nodeFront);
	}
	
	public void RemoveNode (PuzzleNode node) {
		foreach (NodeFront nodeFront in nodes) {
			if (nodeFront.node == node) {
				Destroy (nodeFront.gameObject);
				nodes.Remove (nodeFront);
				break;
			}
		}
	}
	
	public void AddEdge (PuzzleEdge edge) {
		EdgeFront edgeFront = Instantiate (edgePrefab) as EdgeFront;
		edgeFront.transform.parent = transform;
		edgeFront.gameObject.name = "Edge "+edge.nodeA.name+" - "+edge.nodeB.name;
		edgeFront.Set (this, edge);
		edges.Add (edgeFront);
	}
	
	public void RemoveEdge (PuzzleEdge edge) {
		foreach (EdgeFront edgeFront in edges) {
			if (edgeFront.edge == edge) {
				Destroy (edgeFront.gameObject);
				edges.Remove (edgeFront);
				break;
			}
		}
	}
	
	public void AddElement (PuzzleElement element) {
		PuzzleContainer container = puzzle.GetElementContainer (element);
		ContainerFront containerFront = null;
		foreach (ContainerFront current in nodes)
			if (current.container == container)
				containerFront = current;
		foreach (ContainerFront current in edges)
			if (current.container == container)
				containerFront = current;
		if (containerFront == null)
			return;
		
		containerFront.SetElement (element);
	}
	
	public void RemoveElement (PuzzleElement element) {
		PuzzleContainer container = puzzle.GetElementContainer (element);
		ContainerFront containerFront = null;
		foreach (ContainerFront current in nodes)
			if (current.container == container)
				containerFront = current;
		foreach (ContainerFront current in edges)
			if (current.container == container)
				containerFront = current;
		if (containerFront == null)
			return;
		
		containerFront.SetElement (null);
	}
	
	public void AddItem (PuzzleItem item) {
		ItemFront itemFront = Instantiate (itemPrefab) as ItemFront;
		itemFront.transform.parent = transform;
		itemFront.gameObject.name = "Item "+item.GetType ().Name.Replace ("Puzzle", "");
		itemFront.Set (this, item);
		items.Add (itemFront);
	}
	
	public void RemoveItem (PuzzleItem item) {
		foreach (ItemFront itemFront in items) {
			if (itemFront.item == item) {
				Destroy (itemFront.gameObject);
				items.Remove (itemFront);
				break;
			}
		}
	}

	public void AddReceiver (PuzzleNode node, PuzzleReceiverElement receiver) {
		GetNode (node).AddReceiver (receiver);
	}

	public void RemoveReceiver (PuzzleNode node, PuzzleReceiverElement receiver) {
		GetNode (node).RemoveReceiver (receiver);
	}
	
	// Play interface
	
	
}


