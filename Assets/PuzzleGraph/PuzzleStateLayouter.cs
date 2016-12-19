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

public class PuzzleStateLayouter {
	public Puzzle puzzle;

	Pointf[] vectors;
	Pointf[] oldVectors;
	double[,] distances;

	Pointf centerTarget;
	double angleTarget;
	double diameterTarget;

	static Pointf center;
	static double angle;
	public static double diameter;

	static bool initialized = false;
	bool stabilized = false;

	double segmentLength = 1;

	public PuzzleStateLayouter (Puzzle puzzle) {
		this.puzzle = puzzle;
		LayoutStates (puzzle.startNode, Pointf.zero, 0);
		InternalCompute ();
		vectors = new Pointf[puzzle.stateNodes.Count];
		oldVectors = new Pointf[puzzle.stateNodes.Count];
		Update (0.0001);
		if (!initialized) {
			center = centerTarget;
			angle = angleTarget;
			diameter = diameterTarget;
		}
	}

	public Pointf GetPoint (int index) {
		Pointf p = puzzle.stateNodes[index].point - center;
		double s = Math.Sin (angle);
		double c = Math.Cos (angle);
		double xnew = p.x * c + p.y * s;
		double ynew = -p.x * s + p.y * c;
		return new Pointf (xnew, ynew) / segmentLength;
	}

	void LayoutStates (PuzzleStateNode state, Pointf point, double angle) {
		if (state == null)
			return;
		state.point = point;

		int count = state.outgoing.Where (e => e.directPath).Count ();
		int child = 0;
		for (int i = 0; i < state.outgoing.Count; i++) {
			if (state.outgoing[i].directPath) {
				double childAngle = angle - Math.PI + Math.PI * 2 * ((child + 1f) / (count + 1f));
				Pointf childDir = new Pointf (Math.Cos (childAngle), Math.Sin (childAngle));

				LayoutStates (state.outgoing[i].toNode, point + childDir, childAngle);
				child++;
			}
		}
	}

	public void Update (double timeDelta) {
		DateTime time = System.DateTime.Now;
		while ((System.DateTime.Now - time).TotalMilliseconds < 10)
			UpdateInternal ();

		if (initialized && !stabilized)
			return;

		double t = Math.Min (timeDelta * 5, 1);

		center = Pointf.Lerp (center, centerTarget, t);

		double targetAngle = angleTarget;
		if (targetAngle > angle + Math.PI)
			angle += Math.PI * 2;
		if (targetAngle < angle - Math.PI)
			angle -= Math.PI * 2;
		if (targetAngle > angle + Math.PI * 0.5)
			targetAngle -= Math.PI;
		if (targetAngle < angle - Math.PI * 0.5)
			targetAngle += Math.PI;
		angle = angle * (1 - t) + targetAngle * t;

		diameter = diameter * (1 - t) + diameterTarget * t;
	}

	void UpdateInternal () {
		int n = puzzle.stateNodes.Count;
		for (int i = 0; i < n; i++)
			vectors[i] = Pointf.zero;

		for (int i = 0; i < n; i++) {
			PuzzleStateNode node1 = puzzle.stateNodes[i];
			for (int j = 0; j < i; j++) {
				PuzzleStateNode node2 = puzzle.stateNodes[j];
				Pointf vector = node2.point - node1.point;
				double len = vector.magnitude;

				if (len == 0)
					continue;

				double dist = distances[i, j];

				// Calculate the adjustment length as the difference between
				// the current and ideal distance, divided by the squared ideal distance.
				// The division by ideal distance is because it's less important
				// to maintain ideal distance to far off nodes than to close by one.
				// And also because there are more far off nodes than close by ones,
				// so they often have a large aggregate force.
				// The reason to use exactly the squared ideal distance to divide with
				// is largely experimentally arrived at.
				double adjust = (dist - len) / (dist * dist);

				// Multiply the normalized adjustment vector with the adjustment length.
				vector = (vector / len) * adjust;
				vectors[node1.id] += -vector;
				vectors[node2.id] += vector;
			}
		}

		double multiplier = 0.1;
		double maxForce = 0;
		for (int i = 0; i < n; i++) {
			int id = puzzle.stateNodes[i].id;
			puzzle.stateNodes[i].point += vectors[id] * multiplier;
			maxForce = Math.Max (maxForce, vectors[id].sqrMagnitude);
		}
		if (maxForce < 0.0001) {
			stabilized = true;
			initialized = true;
		}

		CalcGraphStats ();

		Pointf[] temp = oldVectors;
		oldVectors = vectors;
		vectors = temp;
	}

	void CalcGraphStats () {
		double segmentLengthSum = 0;
		int segmentCount = 0;

		double extremeLength = 0;
		Pointf extreme1 = Pointf.zero;
		Pointf extreme2 = Pointf.zero;

		int n = puzzle.stateNodes.Count;
		for (int i = 0; i < n; i++) {
			PuzzleStateNode node1 = puzzle.stateNodes[i];
			for (int j = 0; j < i; j++) {
				PuzzleStateNode node2 = puzzle.stateNodes[j];
				Pointf vector = node2.point - node1.point;
				double len = vector.magnitude;

				double dist = distances[i, j];
				if (dist == 1) {
					segmentCount++;
					segmentLengthSum += len;
				}

				if (len > extremeLength) {
					extremeLength = len;
					extreme1 = node1.point;
					extreme2 = node2.point;
				}
			}
		}

		if (segmentCount == 0) {
			centerTarget = Pointf.zero;
			diameterTarget = 0;
			angleTarget = 0;
		}
		else {
			segmentLength = segmentLengthSum / segmentCount;
			centerTarget = (extreme2 + extreme1) * 0.5;
			Pointf mainAxis = (extreme2 - extreme1);
			diameterTarget = mainAxis.magnitude;
			mainAxis = mainAxis / diameterTarget;
			Pointf axis2 = new Pointf (-mainAxis.y, mainAxis.x);

			double projectedMin = double.MaxValue;
			double projectedMax = double.MinValue;
			for (int i = 0; i < n; i++) {
				Pointf p = puzzle.stateNodes[i].point;
				Pointf rel = p - centerTarget;
				double relOnAxis2 = Pointf.Dot (rel, axis2);
				projectedMin = Math.Min (projectedMin, relOnAxis2);
				projectedMax = Math.Max (projectedMax, relOnAxis2);
			}

			angleTarget = Math.Atan2 (mainAxis.x, -mainAxis.y);
			centerTarget += axis2 * (projectedMin + projectedMax) * 0.5;

			diameterTarget /= segmentLength;
		}
	}

	void InternalCompute () {
		int n = puzzle.stateNodes.Count;

		distances = new double[n, n];
		CalculateDistances (distances);

		// calculating the ideal distance between the nodes
		double diameter = 200;
		for (int i = 0; i < n - 1; i++) {
			for (int j = i + 1; j < n; j++) {
				// Distance between disconnected vertices
				double dist = diameter;

				// Calculating the minimal distance between the vertices
				if (distances[i, j] != double.MaxValue)
					dist = Math.Min (distances[i, j], dist);
				if (distances[j, i] != double.MaxValue)
					dist = Math.Min (distances[j, i], dist);
				distances[i, j] = distances[j, i] = dist;
			}
		}
	}

	void CalculateDistances (double[,] distances) {
		int n = puzzle.stateNodes.Count;
		for (int i = 0; i < n; i++)
			for (int j = 0; j < n; j++)
				distances[i, j] = double.MaxValue;

		for (int i = 0; i < n; i++)
			CalculateDistancesToNode (puzzle.stateNodes[i], puzzle.stateNodes[i], 0, distances);
	}

	void CalculateDistancesToNode (PuzzleStateNode origin, PuzzleStateNode current, int dist, double[,] distances) {
		if (distances[origin.id, current.id] <= dist)
			return;
		distances[origin.id, current.id] = dist;

		for (int i = 0; i < current.outgoing.Count; i++)
			CalculateDistancesToNode (origin, current.outgoing[i].toNode, dist + 1, distances);
		for (int i = 0; i < current.ingoing.Count; i++)
			CalculateDistancesToNode (origin, current.ingoing[i].fromNode, dist + 1, distances);
	}
}
