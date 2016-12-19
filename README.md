# PuzzleGraph #

PuzzleGraph is a tool for creating simple typical computer game puzzles and then analyzing and visualizing their state space.

Setup and connect puzzle elements like gates, toggles, pressure plates and boulders. See the state space of the puzzle visualized, including solution paths, dead ends and fail states.

* [Compiled binaries for Windows, Mac, and Linux on itch.io](https://runevision.itch.io/puzzlegraph)
* [YouTube video demonstrating the tool](https://youtu.be/NeTjbfAbNyA)

The repository is a Unity project. The code base consists of a back-end that does not rely on Unity API and a front-end that's developed on top of Unity's features. (The back-end does not rely on the front-end, so it should be possible to develop an alternative front-end if desired.)

## Contribution guidelines ##

I'll be happy to consider pull requests, particularly if they are of features mentioned in the todo-list, or bug fixes.

For features not part of the todo-list, I'll consider it on a case by case basis. I would suggest reaching out to me early to discuss if and how new features could be integrated.

## License ##

Copyright (c) 2016 Rune Skovbo Johansen

This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

## Todo-list ##

### Loose ends ###

- [x] Test puzzles with multiple players.
- [x] Make sure all edge and node types make sense.
- [ ] Take an extra pass looking into consequences of complicated boolean trigger/effect logic.

### State graph ###

- [x] Make state graph update while editing.
- [ ] Visualize one-way connections with arrows.
- [ ] Make it possible to click on nodes in state graph to go to that state.
- [ ] Highlight (somehow) adjacent states that can be reached from current state.

### Graph reconnecting ###

- [ ] Allow reversing direction of directional edge elements via option in left sidebar.

Triggers are currently drawn with LineRenderers and are not selectable at all.
Not sure yet of best way to solve it.

- [ ] Allow selecting trigger connections and deleting them.

Edges cannot be reconnected to other nodes. Would suggest a workflow where
if the edge is clicked near one end or the other and then dragged,
it gets detached in that end and can be dragged onto a different node.

- [ ] Allow re-connecting edges to other nodes.
- [ ] Allow re-connecting triggers to other nodes.

### Graph authoring without tools ###

Currently authoring is done by using three different tools for drawing
(creating/connecting nodes), moving nodes, and adding triggers.
It would be more intuitive and quick if there's no tool switching,
and instead small widgets over the hovered node allow dragging edges
out and adding triggers. The widgets would appear for one node at a
time only, when the cursor is within a given radius of the node.

Mockup:  
![PuzzleGraphMockup.png](https://bitbucket.org/repo/7o5q86/images/2094539977-PuzzleGraphMockup.png)

- [ ] Make widgets for dragging edges out - at edge, pointing in direction of mouse cursor.
- [ ] Make widgets for dragging triggers - inside node, in lower half.
- [ ] Remove tools.

### Menus ###

- [x] Make save and load dialogs look proper.
- [x] Make About dialog.
- [x] Make splash screen.