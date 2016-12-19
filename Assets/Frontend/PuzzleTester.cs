/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public enum GraphEditAction {
	Select,
	Node,
	Edge
}

public class PuzzleTester : MonoBehaviour {

	// Public member variables and properties

	public GUISkin skin;
	public PuzzleFront puzzleFrontPrefab;
	public Texture2D icon;

	public GraphDrawer graphDrawer;
	
	public bool playing {
		get { return puzzleFront.playing; }
		set {
			if (puzzleFront.playing == value)
				return;
			puzzleFront.playing = value;
			puzzleFront.Reset ();
		}
	}

	// Private member variables and properties

	Camera cam;
	PuzzleStateLayouter layouter = null;
	PuzzleFront puzzleFront = null;

	Puzzle puzzle { get { return puzzleFront.puzzle; } }
	PuzzleStateNode state { get { return puzzleFront.state; } }
	PuzzleContainer selected {
		get { return PuzzleFront.selected ? PuzzleFront.selected.container : null; }
	}

	Vector2 puzzleListScroll;
	Vector2 statesScroll;
	Vector2 helpScroll;
	bool showHints = false;
	float hideAboutTime = 2;
	float hideHelpTime = -1;

	List<PuzzleStateEdge>[] actionsPerNode;
	PuzzleStateNode lastCalculatedState = null;

	string[] puzzleNames = null;
	int openPuzzleIndex = -1;
	bool dirtyPuzzle = false;

	Rect toolsWindowRect;
	Rect containerWindowRect;
	Rect menuWindowRect;
	Rect modalWindowRect;

	string folder { get { return Application.persistentDataPath + "/"; } }

	string tempSaveName = string.Empty;
	string[] toolNames = new string[] { "draw", "move", "trigger" };

	string tooltip = null;
	string lastTooltip = null;
	float newTooltipTime = 0;

	enum MenuOption { None, Menu, Open, Save }
	MenuOption menu = MenuOption.None;

	// Const variables

	const int kMaxLevel = 200;
	const int kWindowSpacing = 10;
	const int kLargeButtonSize = 64 + 6;
	const int kTopLevelButtonHeight = 30;
	const int kButtonHeight = 24;
	const int kSpacing = 4;	
	const int kBorder = 2;
	const int kSeparator = 10;

	// Methods
	
	void Start () {
		cam = GetComponent<Camera> ();
		CreatePuzzle (true);
		PreparePuzzle ();
	}

	void FixedUpdate () {
		if (dirtyPuzzle)
			RefreshData ();
		if (layouter != null)
			layouter.Update (Time.deltaTime);
	}

	void OnGUI () {
		GUI.skin = skin;
		if (Event.current.type != EventType.Layout)
			tooltip = null;

		if (menu != MenuOption.None) {
			if (GUI.Button (new Rect (0, 0, Screen.width, Screen.height), string.Empty, GUIStyle.none))
				menu = MenuOption.None;
		}

		float menuWidth = kLargeButtonSize * 2 + kSpacing * 3 + kBorder * 2;
		Rect position = new Rect (cam.pixelRect.xMin - menuWidth, 0, menuWidth, Screen.height);

		GetRect (ref position, 40, kWindowSpacing);

		if (playing) {
			StatesGUI ();
		}

		toolsWindowRect = GetRect (ref position, 60, kWindowSpacing);
		GUI.Window (1, toolsWindowRect, ToolsWindow, "Tools");

		if (selected != null) {
			containerWindowRect = position;
			GUI.Window (2, containerWindowRect, ContainerWindow, selected is PuzzleNode ? "Node" : "Edge");
		}

		if (menu == MenuOption.Menu) {
			menuWindowRect = new Rect (0, 40 - 2, 200, kButtonHeight * 8 + kSeparator * 2 + 12);
			GUI.Window (0, menuWindowRect, MenuWindow, string.Empty);
		}
		if (menu == MenuOption.Open) {
			Vector2 size = new Vector2 (Mathf.Min (Screen.width, 300), Mathf.Min (Screen.height, 300));
			modalWindowRect = new Rect (
				Mathf.RoundToInt ((Screen.width - size.x) / 2), Mathf.RoundToInt ((Screen.height - size.y) / 2),
				size.x, size.y);
			GUI.Window (4, modalWindowRect, OpenWindow, "Open Puzzle");
		}
		if (menu == MenuOption.Save) {
			Vector2 size = new Vector2 (Mathf.Min (Screen.width, 300), Mathf.Min (Screen.height, 80));
			modalWindowRect = new Rect (
				Mathf.RoundToInt ((Screen.width - size.x) / 2), Mathf.RoundToInt ((Screen.height - size.y) / 2),
				size.x, size.y);
			GUI.Window (5, modalWindowRect, SaveWindow, "Save Puzzle");
		}

		if (Time.time < hideHelpTime + 1) {
			modalWindowRect = new Rect (0, 0, Screen.width, Screen.height);
			modalWindowRect.y -= modalWindowRect.height * Mathf.Max (0, Time.time - hideHelpTime) * 4;
			GUI.Window (7, modalWindowRect, HelpWindow, "Help");
			GUI.BringWindowToFront (7);
		}

		if (Time.time < hideAboutTime + 1) {
			modalWindowRect = new Rect (0, 0, Screen.width, Screen.height);
			modalWindowRect.y -= modalWindowRect.height * Mathf.Max (0, Time.time - hideAboutTime) * 4;
			GUI.Window (3, modalWindowRect, AboutWindow, string.Empty);
			GUI.BringWindowToFront (3);
		}

		Rect tooltipRect = new Rect (
			Event.current.mousePosition.x,
			Event.current.mousePosition.y + 20,
			200, 200);
		GUI.Window (6, tooltipRect, TooltipWindow, string.Empty, GUIStyle.none);
		GUI.BringWindowToFront (6);
	}

	private void DirtyPuzzle () {
		dirtyPuzzle = true;
	}

	public void ShowMenu () {
		if (menu == MenuOption.None)
			menu = MenuOption.Menu;
		else
			menu = MenuOption.None;
	}

	string[] GetPuzzles () {
		string[] files = Directory.GetFiles (folder, "*.txt");

		// If no saved files, copy built-in files to save files folder.
		if (files.Length == 0) {
			var puzzles = Resources.LoadAll ("Puzzles");
			foreach (var obj in puzzles) {
				TextAsset puzzleText = obj as TextAsset;
				string filename = folder + obj.name + ".txt";
				TextWriter writer = new StreamWriter (filename);
				writer.Write (puzzleText.text);
				writer.Close ();
			}
			files = System.IO.Directory.GetFiles (folder, "*.txt");
		}

		for (int i=0; i<files.Length; i++)
			files[i] = Path.GetFileNameWithoutExtension (files[i]);

		return files;
	}

	void CreatePuzzle (bool addInitialElements = false) {
		Puzzle puzzle = new Puzzle ();
		if (puzzleFront)
			Destroy (puzzleFront.gameObject);
		puzzleFront = Instantiate (puzzleFrontPrefab) as PuzzleFront;
		puzzleFront.CreatePuzzle (puzzle);
		puzzle.addEdge += e => DirtyPuzzle ();
		puzzle.removeEdge += e => DirtyPuzzle ();
		puzzle.addElement += e => DirtyPuzzle ();
		puzzle.removeElement += e => DirtyPuzzle ();
		puzzle.addItem += e => DirtyPuzzle ();
		puzzle.removeItem += e => DirtyPuzzle ();
		puzzle.addNode += e => DirtyPuzzle ();
		puzzle.removeNode += e => DirtyPuzzle ();
		puzzle.addReceiver += (e, f) => DirtyPuzzle ();
		puzzle.removeReceiver += (e, f) => DirtyPuzzle ();

		if (addInitialElements) {
			PuzzleNode node = new PuzzleNode ();
			PuzzlePlayer player = new PuzzlePlayer ();
			player.defaultNode = node;
			puzzle.AddNode (node);
			puzzle.AddItem (player);
		}

		RefreshData ();
	}
	
	void RefreshData () {
		puzzle.EvaluateTree (kMaxLevel);

		puzzleFront.Reset ();
		layouter = new PuzzleStateLayouter (puzzle);
		graphDrawer.puzzleFront = puzzleFront;
		graphDrawer.layouter = layouter;

		dirtyPuzzle = false;
	}
	
	Rect GetRect (ref Rect total, float height, float spacingAfter) {
		Rect rect = total;
		rect.height = height;
		total.yMin += height + spacingAfter;
		return rect;
	}

	Rect GetRectHNeg (ref Rect total, float width, float spacingAfter) {
		Rect rect = total;
		rect.xMin = rect.xMax - width;
		total.xMax -= width + spacingAfter;
		return rect;
	}
	
	Rect GetInnerWindowRect (Rect rect) {
		Rect inner = new Rect (0, 0, rect.width, rect.height);
		inner.xMin += kBorder + kSpacing;
		inner.xMax -= kBorder + kSpacing;
		inner.yMin += 22;
		inner.yMax -= kBorder + kSpacing;
		return inner;
	}

	void RefreshStatesGUI () {
		if (state == lastCalculatedState)
			return;
		actionsPerNode = new List<PuzzleStateEdge>[puzzle.nodes.Count];
		for (int i = 0; i < state.outgoing.Count; i++) {
			PuzzleStateEdge child = state.outgoing[i];
			PuzzleNode actionNode = child.actionNode;
			int nodeIndex = puzzle.nodes.IndexOf (actionNode);
			if (actionsPerNode[nodeIndex] == null)
				actionsPerNode[nodeIndex] = new List<PuzzleStateEdge> ();
			actionsPerNode[nodeIndex].Add (child);
		}
		lastCalculatedState = state;
	}

	void StatesGUI () {
		Rect area = cam.pixelRect.FlipY ();
		GUILayout.BeginArea (area);
		area.position = Vector2.zero;
		int spacing = kSpacing;
		Rect optionsRect = new Rect (area.width - 150 - spacing, spacing, 150, kButtonHeight);

		GUI.enabled = puzzleFront.HasUndo ();
		if (GUI.Button (GetRectHNeg (ref optionsRect, 70, spacing), "Undo"))
			puzzleFront.Undo ();

		if (GUI.Button (GetRectHNeg (ref optionsRect, 70, spacing), "Restart"))
			puzzleFront.Reset ();
		GUI.enabled = true;

		showHints = GUI.Toggle (GetRectHNeg (ref optionsRect, 150, spacing), showHints, "Show Hints");

		RefreshStatesGUI ();

		if (Time.time > puzzleFront.doneTime) {
			for (int i = 0; i < puzzleFront.nodes.Count; i++) {
				NodeFront nodeFront = puzzleFront.nodes[i];
				PuzzleNode actionNode = nodeFront.node;
				int nodeIndex = puzzle.nodes.IndexOf (actionNode);
				if (actionsPerNode[nodeIndex] == null)
					continue;

				int actionCount = actionsPerNode[nodeIndex].Count;

				Vector3 nodePosition = nodeFront.transform.position - Vector3.up * 0.2f;
				Vector2 pos = cam.WorldToScreenPoint (nodePosition);
				pos.y = cam.pixelRect.height - pos.y;
				pos -= cam.pixelRect.min;

				GUI.color = new Color (1, 1, 1, 0.7f);
				Rect rect = new Rect (pos.x - 60, pos.y, 120, 24 + kButtonHeight * actionCount);
				GUI.Box (rect, string.Empty, "OptionBox");

				for (int j = 0; j < actionsPerNode[i].Count; j++) {
					PuzzleStateEdge childAction = actionsPerNode[i][j];
					GUI.color = Color.white;
					GUI.contentColor = Color.green;
					if (showHints) {
						// Show as green if goal path that's either
						// further along or we are not currently on goal path.
						if (childAction.toNode.goalPath && (childAction.directPath || !state.goalPath))
							GUI.color = Color.green;
						else if (childAction.toNode.stuck)
							GUI.color = Color.red;
					}

					rect = new Rect (pos.x - 58, pos.y + 22 + kButtonHeight * j, 116, kButtonHeight);
					if (GUI.Button (rect, childAction.name, "MenuOption"))
						puzzleFront.GoToState (childAction.toNode);
				}
			}
			GUI.color = Color.white;
		}

		GUILayout.EndArea ();
	}
	
	void PreparePuzzle () {
		Bounds bounds = puzzleFront.GetBounds ();
		cam.transform.position = bounds.center - Vector3.forward * 10;
		RefreshData ();
	}

	void MenuWindow (int id) {
		Rect position = menuWindowRect;
		position.position = Vector2.zero;
		position = new RectOffset (2, 2, 2, 2).Remove (position);
		GetRect (ref position, kSpacing, 0);
		
		if (menu == MenuOption.Menu) {
			if (GUI.Button (GetRect (ref position, kButtonHeight, 0), "New Puzzle", "MenuOption")) {
				CreatePuzzle (true);
				PreparePuzzle ();
				menu = MenuOption.None;
			}

			if (GUI.Button (GetRect (ref position, kButtonHeight, 0), "Open Puzzle...", "MenuOption")) {
				puzzleNames = GetPuzzles ();
				openPuzzleIndex = -1;
				menu = MenuOption.Open;
			}

			GUI.enabled = (puzzle.name != string.Empty);
			if (GUI.Button (GetRect (ref position, kButtonHeight, 0), "Save Puzzle", "MenuOption")) {
				string filename = folder + puzzle.name + ".txt";
				TextWriter writer = new StreamWriter (filename);
				Puzzle.Serialize (puzzle, writer);
				writer.Close ();

				menu = MenuOption.None;
			}

			GUI.enabled = true;
			if (GUI.Button (GetRect (ref position, kButtonHeight, 0), "Save Puzzle As...", "MenuOption")) {
				tempSaveName = puzzle.name;
				if (tempSaveName == string.Empty)
					tempSaveName = "Untitled Puzzle";
				menu = MenuOption.Save;
			}

			GUI.Box (GetRect (ref position, kSeparator, 0), string.Empty, "MenuSep");

			if (GUI.Button (GetRect (ref position, kButtonHeight, 0), "Open Puzzle Folder", "MenuOption")) {
				// Make sure there are puzzles in the folder.
				GetPuzzles ();

				bool windows = (Application.platform == RuntimePlatform.WindowsPlayer);
				string path = (windows ? "file:///" : "file://") + folder;
				path = path.Replace (" ", "%20");
				Application.OpenURL (path);
				Debug.Log ("Opening folder: " + path);

				menu = MenuOption.None;
			}

			GUI.Box (GetRect (ref position, kSeparator, 0), string.Empty, "MenuSep");

			if (GUI.Button (GetRect (ref position, kButtonHeight, 0), "Help...", "MenuOption")) {
				hideHelpTime = Mathf.Infinity;
				menu = MenuOption.None;
			}

			if (GUI.Button (GetRect (ref position, kButtonHeight, 0), "About...", "MenuOption")) {
				hideAboutTime = Time.time + 60;
				menu = MenuOption.None;
			}

			if (GUI.Button (GetRect (ref position, kButtonHeight, 0), "Quit", "MenuOption")) {
				Application.Quit ();
			}
		}
	}

	void OpenWindow (int id) {
		Rect position = GetInnerWindowRect (modalWindowRect);

		Rect scrollviewRect = GetRect (ref position, position.height - kSpacing - kButtonHeight, kSpacing);

		GUI.Box (scrollviewRect, string.Empty);
		scrollviewRect = new RectOffset (2, 2, 2, 2).Remove (scrollviewRect);
		Rect viewRect = new Rect (0, 0, scrollviewRect.width, puzzleNames.Length * (kButtonHeight + 0) - kSpacing);
		if (viewRect.height > scrollviewRect.height)
			viewRect.width -= 15;
		puzzleListScroll = GUI.BeginScrollView (scrollviewRect, puzzleListScroll, viewRect);
		for (int i = 0; i < puzzleNames.Length; i++) {
			Rect rect = GetRect (ref viewRect, kButtonHeight, 0);
			bool selected = (GUI.Toggle (rect, openPuzzleIndex == i, puzzleNames[i], "MenuOption"));
			if (selected)
				openPuzzleIndex = i;
		}
		GUI.EndScrollView ();

		Rect buttonsRect = GetRect (ref position, kButtonHeight, 0);

		if (GUI.Button (new Rect (buttonsRect.xMax - 204, buttonsRect.y, 100, buttonsRect.height), "Cancel")) {
			menu = MenuOption.None;
		}

		GUI.enabled = (openPuzzleIndex >= 0);
		if (GUI.Button (new Rect (buttonsRect.xMax - 100, buttonsRect.y, 100, buttonsRect.height), "Open")) {
			CreatePuzzle ();

			string filename = folder + puzzleNames[openPuzzleIndex] + ".txt";
			TextReader reader = new StreamReader (filename);
			Puzzle.Deserialize (puzzle, reader);
			reader.Close ();

			PreparePuzzle ();
			menu = MenuOption.None;
		}
		GUI.enabled = true;
	}

	void SaveWindow (int id) {
		Rect position = GetInnerWindowRect (modalWindowRect);
		tempSaveName = GUI.TextField (GetRect (ref position, kButtonHeight, kSpacing), tempSaveName);
		
		Rect buttonsRect = GetRect (ref position, kButtonHeight, 0);

		if (GUI.Button (new Rect (buttonsRect.xMax - 204, buttonsRect.y, 100, buttonsRect.height), "Cancel")) {
			menu = MenuOption.None;
		}

		if (GUI.Button (new Rect (buttonsRect.xMax - 100, buttonsRect.y, 100, buttonsRect.height), "Save")) {
			puzzle.name = tempSaveName;

			string filename = folder + puzzle.name + ".txt";
			TextWriter writer = new StreamWriter (filename);
			Puzzle.Serialize (puzzle, writer);
			writer.Close ();

			menu = MenuOption.None;
		}
	}

	void AboutWindow (int id) {
		GUILayout.FlexibleSpace ();
		GUILayout.Label (icon, "Title");
		GUILayout.Label ("PuzzleGraph", "Title");
		GUILayout.FlexibleSpace ();
		GUILayout.Label ("By Rune Skovbo Johansen", "Copyright");
		if (GUILayout.Button ("runevision.com", "Url"))
			Application.OpenURL ("http://runevision.com");
		GUILayout.FlexibleSpace ();
		if (Event.current.type == EventType.MouseDown)
			hideAboutTime = Mathf.Min (hideAboutTime, Time.time);
	}

	void HelpWindow (int id) {
		helpScroll = GUILayout.BeginScrollView (helpScroll);
		{
			GUILayout.Label (@"<b>Tools</b>

<b>Draw</b>: Click on nodes and drag to other nodes or new positions to create edges.
<b>Move</b>: Click on nodes and drag to move them.
<b>Trigger</b>: Click on nodes with trigger elements and drag to dynamic elements to create a trigger connection.
", "HelpText");
			GUILayout.BeginHorizontal ();
			{
				GUILayout.BeginVertical (GUILayout.MaxWidth (Screen.width));
				{
					GUILayout.Label ("Node Elements");
					ShowElements (puzzleFrontPrefab.nodePrefab.elements);
					GUILayout.FlexibleSpace ();
				}
				GUILayout.EndVertical ();

				GUILayout.Space (20);

				GUILayout.BeginVertical (GUILayout.MaxWidth (Screen.width));
				{
					GUILayout.Label ("Edge Elements");
					ShowElements (puzzleFrontPrefab.edgePrefab.elements);
					GUILayout.FlexibleSpace ();
				}
				GUILayout.EndVertical ();

				GUILayout.Space (20);
			}
			GUILayout.EndHorizontal ();
		}
		GUILayout.EndScrollView ();

		if (Event.current.type == EventType.MouseDown)
			hideHelpTime = Mathf.Min (hideHelpTime, Time.time);
	}

	void ShowElements (ElementData[] elements) {
		// Container options
		for (int i=0; i<elements.Length; i++) {
			GUILayout.BeginHorizontal ();
			ElementData data = elements[i];
			Rect rect = GUILayoutUtility.GetRect (kLargeButtonSize, kLargeButtonSize, GUILayout.ExpandWidth (false));
			DrawElement (rect, data);
			string text = string.Format ("<b>{0}</b>\n{1} ", data.name, data.tooltip);
			GUILayout.Label (text, "HelpText", GUILayout.ExpandWidth (true));
			GUILayout.EndHorizontal ();
		}
	}

	void TooltipWindow (int id) {
		if (Event.current.type != EventType.Layout && tooltip != lastTooltip) {
			lastTooltip = tooltip;
			newTooltipTime = Time.time;
		}

		// Just always call the tooltip label to avoid control ID issues,
		// but make it invisible when there's no tooltip.
		if (tooltip == null || Time.time < newTooltipTime + 0.5f)
			GUI.color = new Color (0, 0, 0, 0);

		GUILayout.Label (tooltip, "Box", GUILayout.ExpandWidth (false));
	}

	void ToolsWindow (int id) {
		Rect position = GetInnerWindowRect (toolsWindowRect);
		PuzzleFront.tool = (PuzzleFront.MouseTool)GUI.Toolbar (position, (int)PuzzleFront.tool, toolNames);
	}
	
	void ContainerWindow (int id) {
		Rect position = GetInnerWindowRect (containerWindowRect);
		position.width = kLargeButtonSize;
		position.height = kLargeButtonSize;
		
		PuzzleNode node = selected as PuzzleNode;
		
		ElementData[] elements = node != null ?
			puzzleFrontPrefab.nodePrefab.elements :
			puzzleFrontPrefab.edgePrefab.elements;
		
		// Container options
		for (int i=0; i<elements.Length; i++) {
			ElementData elementData = elements[i];
			bool chosenElement = (selected.element != null && selected.element.GetType () == elementData.type);
			
			if (i % 2 == 0)
				position.x = kBorder+kSpacing;
			else
				position.x += kLargeButtonSize+kSpacing;
			
			if (ElementButton (position, elementData, chosenElement)) {
				if (!chosenElement) {
					if (selected.element != null)
						puzzle.RemoveElement (selected.element);
					PuzzleElement element = (PuzzleElement)Activator.CreateInstance (elementData.type);
					selected.element = element;
					puzzle.AddElement (element);
				}
				else {
					puzzle.RemoveElement (selected.element);
				}
			}
			
			if (i % 2 == 1 || i == elements.Length-1)
				position.y += kLargeButtonSize+kSpacing;
		}

		position.y += 10;

		if (selected.element != null) {
			var boolElement = selected.element as PuzzleBoolElement;
			if (boolElement != null) {
				position.x = kBorder+kSpacing;
				position.width = GetInnerWindowRect (containerWindowRect).width;
				position.height = 24;
				Rect pos = new Rect (position.x, position.y, position.width, 20);
				boolElement.defaultOn = GUI.Toggle (pos, boolElement.defaultOn, "Enabled");
				position.y += 24 + kSpacing + 10;
			}
		}
		
		// Item options
		if (node != null) {
			List<PuzzleItem> itemsInNode = puzzle.GetDefaultItemsInNode (node);
			
			ItemData[] itemDatas = puzzleFront.itemPrefab.items;
			foreach (var data in itemDatas) {
				position = new Rect (kBorder+kSpacing, position.y, 32, 48);
				
				DrawSprite (position, data.image);
				
				List<PuzzleItem> itemsOfType = itemsInNode.Where (e => e.GetType () == data.type).ToList ();
				
				position.x += position.width + kSpacing + 1;
				position.width = 33;
				GUI.Label (position, itemsOfType.Count.ToString ());
				
				Rect buttonRect = position;
				buttonRect.y += 8;
				buttonRect.height -= 16;
				buttonRect.x += buttonRect.width + kSpacing;
				GUI.enabled = (itemsOfType.Count > 0);
				if (GUI.Button (buttonRect, "-")) {
					puzzle.RemoveItem (itemsOfType[0]);
				}
				GUI.enabled = true;
				
				buttonRect.x += buttonRect.width + kSpacing;
				if (GUI.Button (buttonRect, "+")) {
					PuzzleItem item = (PuzzleItem)Activator.CreateInstance (data.type);
					item.defaultNode = node;
					puzzle.AddItem (item);
				}
				
				position.y += 48  - 8;
			}
		}
	}

	private bool ElementButton (Rect rect, ElementData data, bool selected) {
		bool after = GUI.Toggle (rect, selected, GUIContent.none, "Button");
		if (rect.Contains (Event.current.mousePosition))
			tooltip = string.Format ("<b>{0}</b>\n{1} ", data.name, data.tooltip);
		
		DrawElement (rect, data);
		return (after != selected);
	}

	private void DrawElement (Rect rect, ElementData data) {
		rect.x += (rect.width - 64) * 0.5f;
		rect.y += (rect.height - 64) * 0.5f;
		rect.width = 64;
		rect.height = 64;
		if (typeof (PuzzleNodeElement).IsAssignableFrom (data.type)) {
			DrawSprite (rect, data.imageLine ? data.imageLine : puzzleFrontPrefab.nodePrefab.defaultLineImage);
			DrawSprite (rect, data.imageFill ? data.imageFill : puzzleFrontPrefab.nodePrefab.defaultFillImage);
		}
		else {
			Rect edgeRect = new RectOffset (2,2,16,16).Remove (rect);
			DrawSprite (edgeRect, data.imageLine ? data.imageLine : puzzleFrontPrefab.edgePrefab.defaultLineImage);
			DrawSprite (edgeRect, data.imageFill ? data.imageFill : puzzleFrontPrefab.edgePrefab.defaultFillImage);
			rect = new RectOffset (8,8,8,8).Remove (rect);
		}
		if (data.imageStatic)
			DrawSprite (rect, data.imageStatic);
		if (data.imageOff)
			DrawSprite (rect, data.imageOff);
	}
	
	private void DrawSprite (Rect position, Sprite sprite) {
		Rect coords = sprite.rect;
		coords.x /= sprite.texture.width;
		coords.y /= sprite.texture.height;
		coords.width /= sprite.texture.width;
		coords.height /= sprite.texture.height;
		GUI.DrawTextureWithTexCoords (position, sprite.texture, coords);
	}
}
