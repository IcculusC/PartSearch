# Part Search

Part Search is an addon for Kerbal Space Program. Part Search adds a quick search window to the Vehicle Assembly Building (VAB) and the Space Plane Hangar (SPH). When entering text in the search window, the addon searches for (partial) matches in the names and descriptions of vehicle parts. The parts window is filtered dynamically to only contain the matching parts.

## Installation

Copy all the files to the GameData directory in your KSP installation.

## Usage

After successful installation the toolbars in the VAB and SPH display a new button named 'Find'. This button will toggle the search window on and off.

The search window can be moved around freely. The addon remembers the last position of the window. The window contains a text field and two buttons. Clicking in the text field allows entering text. While typing the part window displays only the parts matching the search term. For example typing 'tank' will show all parts whose name or description contain the string tank (this includes partial matches).

The button marked 'C' clears the contents of the textfield.

When the '∞' button is clicked searches include all parts in all categories. Otherwise only the current category is searched.

## Notes

* If a search is started, while the part window is not on the first page, the search results might not be visible. Use the 'Previous Page' button to switch to the first page.
* If the search window is not visible when clicking the find button, try deleting the file PartSearch\PluginData\PartSearch\config.xml. This resets the window position. The game must be restarted to see the effect.
* Part Search might interfere with other addons, that manipulate the part window.