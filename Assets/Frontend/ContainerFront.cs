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
public class ElementData {
	public string name;
	[Multiline]
	public string tooltip;
	public Sprite imageStatic;
	public Sprite imageOff;
	public Sprite imageOn;
	public Sprite imageLine;
	public Sprite imageFill;
	
	private System.Type m_Type = null;
	public System.Type type {
		get {
			if (m_Type == null)
				m_Type = typeof (Puzzle).Assembly.GetType ("Puzzle" + name.Replace (" ", ""));
			if (m_Type == null)
				Debug.Log ("Couldn't find type with name "+name);
			return m_Type;
		}
	}
}

public class ContainerFront : MonoBehaviour {
	PuzzleContainer m_Container;
	public PuzzleContainer container { get { return m_Container; } }
	
	public SpriteRenderer lineSprite;
	public SpriteRenderer fillSprite;
	public SpriteRenderer staticSprite;
	public SpriteRenderer dynamicSprite;
	
	public Sprite defaultLineImage;
	public Sprite defaultFillImage;
	public ElementData[] elements;
	
	private PuzzleFront m_Puzzle = null;
	public PuzzleFront puzzle { get { return m_Puzzle; } }
	
	public virtual void Set (PuzzleFront puzzle, PuzzleContainer container) {
		m_Puzzle = puzzle;
		m_Container = container;
	}
	
	public virtual void Update () {
		if (PuzzleFront.selected == this)
			SetTint (PuzzleFront.selectionColor);
		else if (PuzzleFront.hovered == this)
			SetTint (PuzzleFront.hoverColor);
		else
			SetTint (PuzzleFront.normalColor);
	}
	
	public void OnMouseEnter () {
		m_Puzzle.OnContainerMouseEnter (this);
	}
	
	public void OnMouseExit () {
		m_Puzzle.OnContainerMouseExit (this);
	}
	
	public void OnMouseOver () {
		m_Puzzle.OnContainerMouseOver (this);
	}
	
	public void OnMouseDown () {
		m_Puzzle.OnContainerMouseDown (this);
	}
	
	public void OnMouseUp () {
		m_Puzzle.OnContainerMouseUp (this);
	}
	
	private ElementData GetElementData () {
		PuzzleElement element = container.element;
		if (element == null)
			return null;
		System.Type type = element.GetType ();
		foreach (var elem in elements)
			if (elem.type == type)
				return elem;
		return null;
	}
	
	public void SetElement (PuzzleElement element) {
		PuzzleBoolElement boolElement = element as PuzzleBoolElement;
		if (boolElement != null) {
			boolElement.updateState -= UpdateState;
			boolElement.updateState += UpdateState;
		}
		if (element == null) {
			staticSprite.sprite = null;
			dynamicSprite.sprite = null;
			lineSprite.sprite = defaultLineImage;
			fillSprite.sprite = defaultFillImage;
			
			if (staticSprite.GetComponent<Collider>())
				staticSprite.GetComponent<Collider>().enabled = false;
		}
		else {
			ElementData elem = GetElementData ();
			staticSprite.sprite = elem.imageStatic;
			bool state = false;
			if (boolElement != null)
				state = boolElement.defaultOn;
			SetState (state);
			lineSprite.sprite = (elem.imageLine != null ? elem.imageLine : defaultLineImage);
			fillSprite.sprite = (elem.imageFill != null? elem.imageFill : defaultFillImage);
			
			if (staticSprite.GetComponent<Collider>())
				staticSprite.GetComponent<Collider>().enabled = (boolElement != null);
		}
	}
	
	private void SetState (bool state) {
		ElementData elem = GetElementData ();
		dynamicSprite.sprite = (state ? elem.imageOn : elem.imageOff);
	}
	
	public IEnumerator AnimateValue (bool oldVal, bool newVal, bool animate) {
		if (animate) {
			while (Time.time < puzzle.itemChangeTime)
				yield return null;
			if (!(container.element is PuzzleTriggerElement))
				yield return new WaitForSeconds (PuzzleFront.effectDelay);
		}
		SetState (newVal);
	}
	
	public void UpdateState () {
		SetElement (container.element);
	}
	
	private void SetTint (Color color) {
		fillSprite.color = color;
		staticSprite.color = color;
		dynamicSprite.color = color;
	}
	
	public virtual Vector3 GetElementPosition () {
		return transform.position;
	}
}

