/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Linq;

public abstract class PuzzleContainer : PuzzleSerializable {
	public abstract PuzzleElement defaultElement { get; }
	
	public PuzzleElement element = null;
	public PuzzleElement elementNotNull { get { return element ?? defaultElement; } }
	
	// Editor
	public override string ToString () {
		if (element != null)
			return element.ToString ();
		return string.Empty;
	}
	public abstract Point GetPosition ();
	
	public virtual string Serialize (SerializationTool tool) { return string.Empty; }
	public virtual void Deserialize (SerializationTool tool, string str) { }
	public virtual string GetName (SerializationTool tool) { return string.Empty; }
	public virtual void SetName (SerializationTool tool, string name) { }
}

