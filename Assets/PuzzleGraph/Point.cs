/*
 * Copyright (c) 2016 Rune Skovbo Johansen
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;

public struct Point {
	public int x, y;
	
	public int X { get { return x; } set { x = value; } }
	public int Y { get { return y; } set { y = value; } }
	
	public Point (int x, int y) {
		this.x = x;
		this.y = y;
	}
	
	public int this[int index] {
		get {
			switch (index) {
				case 0: return x;
				case 1: return y;
				default: throw new System.IndexOutOfRangeException ();
			}
		}
		set {
			switch (index) {
				case 0: x = value; break;
				case 1: y = value; break;
				default: throw new System.IndexOutOfRangeException ();
			}
		}
	}
	
	public int[] array { get { return new int[] {x, y}; } }
	
	public static Point zero = new Point (0, 0);
	public static Point right = new Point (1, 0);
	public static Point up = new Point (0, 1);
	public static Point one = new Point (1, 1);
	
	public static Point operator + (Point a, Point b) {
		return new Point (a.x + b.x, a.y + b.y);
	}
	
	public static Point operator - (Point a, Point b) {
		return new Point (a.x - b.x, a.y - b.y);
	}
	
	public static Point operator - (Point a) {
		return new Point (-a.x, -a.y);
	}
	
	public static Point operator * (Point a, int f) {
		return new Point (a.x * f, a.y * f);
	}
	
	public static Point operator / (Point a, int f) {
		return new Point (a.x / f, a.y / f);
	}
	
	public static Point operator * (Point a, Point b) {
		return new Point (a.x * b.x, a.y * b.y);
	}
	
	public static bool operator == (Point a, Point b) {
		return a.Equals (b);
	}
	
	public static bool operator != (Point a, Point b) {
		return !a.Equals (b);
	}
	
	public override bool Equals (System.Object obj) {
		//Check for null and compare run-time types.
		if (obj == null || GetType () != obj.GetType ()) return false;
		Point p = (Point)obj;
		return (x == p.x) && (y == p.y);
	}
	
	public override string ToString () {
		return "Point ("+x+","+y+")";
	}
	
	public override int GetHashCode () {
		int result = x;
		result = 46309 * result + y;
		return result;
	}
	
	public static Point Min (Point a, Point b) {
		return new Point (Math.Min (a.x, b.x), Math.Min (a.y, b.y));
	}
	
	public static Point Max (Point a, Point b) {
		return new Point (Math.Max (a.x, b.x), Math.Max (a.y, b.y));
	}
}

