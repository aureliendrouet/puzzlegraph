/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Linq;

public interface PuzzleSerializable {
	string Serialize (SerializationTool tool);
	void Deserialize (SerializationTool tool, string str);
	string GetName (SerializationTool tool);
	void SetName (SerializationTool tool, string name);
}

public class SerializationTool {
	public Puzzle puzzle;
	public Dictionary<PuzzleSerializable, string> pending = new Dictionary<PuzzleSerializable, string> ();
	
	public SerializationTool (Puzzle puzzle) {
		this.puzzle = puzzle;
	}
	
	public string Serialize (PuzzleSerializable obj) {
		return obj.GetType ().Name + " " + obj.GetName (this) + " { " + obj.Serialize (this) + " }";
	}
	
	public string GetToken (ref string text, char separator) {
		int index = text.IndexOf (separator);
		string token = text.Substring (0, index);
		text = text.Substring (index + 1).Trim ();
		return token.Trim ();
	}
	
	public PuzzleSerializable DeserializePartly (string str) {
		string t = GetToken (ref str, ' ');
		string name = GetToken (ref str, '{');
		string content = str.Substring (0, str.Length-1).Trim ();
		System.Type typ = typeof (Puzzle).Assembly.GetType (t);
		object o = System.Activator.CreateInstance (typ);
		PuzzleSerializable obj = o as PuzzleSerializable;
		obj.SetName (this, name);
		pending.Add (obj, content);
		return obj;
	}
	
	public void Final () {
		foreach (KeyValuePair<PuzzleSerializable, string> kvp in pending) {
			kvp.Key.Deserialize (this, kvp.Value);
		}
	}
}
