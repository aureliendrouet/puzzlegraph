/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;

public struct Pointf {
	public double x;
	public double y;

	public static Pointf zero {
		get {
			return new Pointf (0, 0);
		}
	}

	public double magnitude {
		get {
			return Math.Sqrt (this.x * this.x + this.y * this.y);
		}
	}

	public Pointf normalized {
		get {
			Pointf result = new Pointf (this.x, this.y);
			result.Normalize ();
			return result;
		}
	}

	public double sqrMagnitude {
		get {
			return this.x * this.x + this.y * this.y;
		}
	}

	//
	// Indexer
	//
	public double this[int index] {
		get {
			if (index == 0) {
				return this.x;
			}
			if (index != 1) {
				throw new IndexOutOfRangeException ("Invalid Pointf index!");
			}
			return this.y;
		}
		set {
			if (index != 0) {
				if (index != 1) {
					throw new IndexOutOfRangeException ("Invalid Pointf index!");
				}
				this.y = value;
			}
			else {
				this.x = value;
			}
		}
	}

	public Pointf (double x, double y) {
		this.x = x;
		this.y = y;
	}

	public static Pointf ClampMagnitude (Pointf vector, double maxLength) {
		if (vector.sqrMagnitude > maxLength * maxLength) {
			return vector.normalized * maxLength;
		}
		return vector;
	}

	public static double Distance (Pointf a, Pointf b) {
		return (a - b).magnitude;
	}

	public static double Dot (Pointf lhs, Pointf rhs) {
		return lhs.x * rhs.x + lhs.y * rhs.y;
	}

	public static Pointf Lerp (Pointf from, Pointf to, double t) {
		return new Pointf (from.x + (to.x - from.x) * t, from.y + (to.y - from.y) * t);
	}

	public static Pointf Max (Pointf lhs, Pointf rhs) {
		return new Pointf (Math.Max (lhs.x, rhs.x), Math.Max (lhs.y, rhs.y));
	}

	public static Pointf Min (Pointf lhs, Pointf rhs) {
		return new Pointf (Math.Min (lhs.x, rhs.x), Math.Min (lhs.y, rhs.y));
	}

	public static Pointf MoveTowards (Pointf current, Pointf target, double maxDistanceDelta) {
		Pointf a = target - current;
		double magnitude = a.magnitude;
		if (magnitude <= maxDistanceDelta || magnitude == 0) {
			return target;
		}
		return current + a / magnitude * maxDistanceDelta;
	}

	public static Pointf Reflect (Pointf inDirection, Pointf inNormal) {
		return -2 * Pointf.Dot (inNormal, inDirection) * inNormal + inDirection;
	}

	public static Pointf Scale (Pointf a, Pointf b) {
		return new Pointf (a.x * b.x, a.y * b.y);
	}

	public static double SqrMagnitude (Pointf a) {
		return a.x * a.x + a.y * a.y;
	}

	public override bool Equals (object other) {
		if (!(other is Pointf)) {
			return false;
		}
		Pointf vector = (Pointf)other;
		return this.x.Equals (vector.x) && this.y.Equals (vector.y);
	}

	public override int GetHashCode () {
		return this.x.GetHashCode () ^ this.y.GetHashCode () << 2;
	}

	public void Normalize () {
		double magnitude = this.magnitude;
		if (magnitude > 1E-05) {
			this /= magnitude;
		}
		else {
			this = Pointf.zero;
		}
	}

	public void Scale (Pointf scale) {
		this.x *= scale.x;
		this.y *= scale.y;
	}

	public void Set (double new_x, double new_y) {
		this.x = new_x;
		this.y = new_y;
	}

	public double SqrMagnitude () {
		return this.x * this.x + this.y * this.y;
	}

	public override string ToString () {
		return string.Format ("({0:F1}, {1:F1})", new object[] {
			this.x,
			this.y
		});
	}

	public string ToString (string format) {
		return string.Format ("({0}, {1})", new object[] {
			this.x.ToString (format),
			this.y.ToString (format)
		});
	}

	public static Pointf operator + (Pointf a, Pointf b) {
		return new Pointf (a.x + b.x, a.y + b.y);
	}

	public static Pointf operator / (Pointf a, double d) {
		return new Pointf (a.x / d, a.y / d);
	}

	public static bool operator == (Pointf lhs, Pointf rhs) {
		return Pointf.SqrMagnitude (lhs - rhs) < 9.999999E-11;
	}

	public static bool operator != (Pointf lhs, Pointf rhs) {
		return Pointf.SqrMagnitude (lhs - rhs) >= 9.999999E-11;
	}

	public static Pointf operator * (Pointf a, double d) {
		return new Pointf (a.x * d, a.y * d);
	}

	public static Pointf operator * (double d, Pointf a) {
		return new Pointf (a.x * d, a.y * d);
	}

	public static Pointf operator - (Pointf a, Pointf b) {
		return new Pointf (a.x - b.x, a.y - b.y);
	}

	public static Pointf operator - (Pointf a) {
		return new Pointf (-a.x, -a.y);
	}
}

