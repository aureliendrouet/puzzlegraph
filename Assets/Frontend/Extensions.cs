/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using UnityEngine;
using System.Collections;

public static class Extensions {

	public static Rect FlipY (this Rect rect) {
		return new Rect (rect.x, Screen.height - rect.yMax, rect.width, rect.height);
	}
}
