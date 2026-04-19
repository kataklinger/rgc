Introduction
------------

The repository implements an algorithm for recrating a game world from video capture of gameplay, so it is universal as it does not depend on game's internal representation of its world. To achieve this, we scan each frame of gameplay and extract features that we are using to find our position in the world. We keep database of identified features, called sketch, which we update with each new frame we process. Once the frame's location is determined and sketch is updated, we store frame's image data combined with motion information. We use motion information to remove moving objects from the final map.

Since it operates on video output, certain restrictions apply to gameplays that can be supported:

*   Video has limited number of colors (up to 16)
*   Gameplay have to continuously scroll over the game's world
    *   Example of supported gameplay: [CJ's Elephant Antics](https://www.youtube.com/watch?v=IZD5fKEDxdY)
    *   Example of gameplay that is not supported: [Cybernoid](https://www.youtube.com/watch?v=k6xhfDLaVtU)
*   Each part of the world needs to have enough distinguishable features and their layout has to be unique

We can see some example of generated maps:

1.  CJ's Elephant Antics: [Level 1](https://github.com/user-attachments/assets/a401757d-79af-4c02-95b0-ddb1cf9dd6eb)
2.  Combat Crazy: Warbringer: [Level 1](https://github.com/user-attachments/assets/b5025672-47c8-4688-a3be-7389860f05bd), [Level 2](https://github.com/user-attachments/assets/15fde5b8-9aa2-42d2-8685-1bee898d914e), [Level 3](https://github.com/user-attachments/assets/9849c778-2715-41cc-9cd0-5682ff35c2e2)

Note that the algorithm and its implementation is not optimized enough to support real-time mapping, as it needs more time to process gameplay video than to record it.

Since this is only a demonstration of the algorithm, not fully fledged "product", user-friendliness of the final application was not considered. Application operates on ZIP files that are storing individual frames as separate files. Frames stored in archived files has no specific format, they are just raw captures of pixels from the video output of predefined size. Pixels stored in fame file are not in RGB format, but platform specific color values.

In this implementation, everything (size screen, color palette, etc.) is fixed to support Commodore 64 video output. All gameplay files provided are grabbed playing games on C++ port of [the emulator from this repository implemented in C#](https://github.com/kataklinger/c64net).

Determining Active Window
-------------------------

The first thing we need to figure out is the active area of the screen which displays part of game world. We operate under the assumption that there is a single such area and that it is the largest one on the screen. The rest of the screen we ignore as it, most probably, displays HUD.

To determine active window, we scan changes between frames and keep a matrix that stores detected changes for each pixel on screen. Each time we detect change of a pixel color between frames, we mark them in this matrix. After each update, we are extracting islands (contours) of changed pixels, searching the largest one. Contour extraction is explained in [this section](#contour).

We are looking for a contour that covers at least a third of total screen pixels, but we will keep scanning frames as long as this area keeps increasing. We stop scanning only after we have not detected size increase of the largest contour for 100 consecutive frames. When that happens, bounding rectangle of the contour is declared as active window.

`Window` module implements logic for determining active window. The module defines the following entities:

*   `Pending` record – that keeps the state of the window scanning process: change-of-pixels matrix, size of the largest contour and its bounding rectangle...
*   `Window` record – represents finalized active window: bounding rectangle with the list of frames that had been scanned.
*   `WindowState` discriminated union – result of `update` function.
*   `update` function – that processes current frame and updates the state of windows scanning process. If the criteria for declaring active window has been reached, function will return `Complete Window`, otherwise the result will be `Incomplete Pending` to indicate failure.

Creating Map
------------

Once the window into our game world has be determined, the map stitching can begin.

The basic idea is to identify features in the current frame and match them against the database of already identified features, called sketch, to determine the location of the current frame in the game world. We deal with two kinds of features: corners and contours.

Extracting Features
-------------------

Original images are not suitable for feature extraction as they contain too many information. Game worlds are made to be pretty and colorful, but that unfortunately introduces a lot of "noise" for our needs. So in order to reduce amount of information we are dealing with and number of extracted features, we apply median filter to source image. We use 5x5px window to do the filtering:

| <img width="285" height="145" alt="image" src="https://github.com/user-attachments/assets/18c723be-1724-4480-9826-1c6978d3f8ad" /> | &gt;   | <img width="285" height="145" alt="image" src="https://github.com/user-attachments/assets/7581f97e-fa6d-4452-9051-417309205918" /> |
|------------------------------------------------------------|--------|------------------------------------------------------------|
| <br>                    Original image<br>                 | &nbsp; | <br>                    Filtered image<br>                 |

`Filter` module contains `median` function that implements median filter.

`Feature` module defines few data types that are used for representation of features for different purposes:

*   `Feature` record - stores feature's position and area
*   `Fid` alias - description of the feature (e.g., for corners, it represents color of all surrounding pixels)
*   `Handle` alias - unique definition of the feature: its description, position and area

Corners
-------

In computer vision, corners are usually the most interesting parts of the image. We are going to use them as landmarks on our maps to identify position of a frame.

For extraction of corners, we are not going to use any of the fancy algorithms, but a simple implementation that only inspects neighboring pixels. For each pixel in the frame, we take 3x3px window and inspect pixels in it. Only following patterns are recognized as corners:

<img width="447" height="100" alt="image" src="https://github.com/user-attachments/assets/496bc275-1c52-4bf1-a63a-086f3149ae8f" />

Patterns recognized as corners - X marks same color, ? marks any other color

Please note that corner identification operates on frames that have been processed by median filter.

<img width="285" height="145" alt="image" src="https://github.com/user-attachments/assets/81183369-be1e-4479-958d-da58c77eac15" />

Detected corners - corner pixels are marked by bright red color

Once a corner has been identified, we are switching to non-filtered image and take 5x5pixels window at corner's position to create its description. We are converting color values in this window to array of bytes and from there to an integer number. That number will be feature description. We combine the description with corner's position to uniquely identify a corner. Area is added to the mix to satisfy `Feature` contract, but that information is not that useful to us, because all corners will have the same area that corresponds to window size.

`Corner` module defines `extract` function that will extract corners from the image and return them as list of features.

Contours
--------

Contours represents connected pixels of the uniform color. In our algorithm, we are using them for identifying moving parts of the frame, its application is described [here](#location). Another application is identifying active window, except we are not processing real frames, but 1-bit pixels that identify whether pixels of the original image had any changes in color during scanning period. That application is described in [this section](#window).

Simple 4-way flood fill algorithm with queue is used to identify contours.

<img width="284" height="144" alt="image" src="https://github.com/user-attachments/assets/57cb1e3d-10d7-4566-ba31-98170fa73613" />

Example of extracted contours (with area > 10px)

`Contour` module defines functions and data types that are dealing with contours:

*   `Contour` record - stores information about contour: position, area, bounding rectangle, color, ID (contour number in the image) and list of the edge pixels.
*   `Buffers` record - represents buffers used by contour detection algorithm. It also provides some useful information such as map of edge pixels and map of contour IDs.
*   `buffer` function - creates `Buffers` instance for that can handle specified image.
*   `single` function - taking coordinate within image to extract single contour from the image to which specified pixel belongs.
*   `extract` function - extract all contours in the image. It returns all extracted contours as list of `Contour`.
*   `recover` function - recreates image from supplied list of contours.
*   `encode` function - creates `Feature` object from `Contour` instance.

Building Feature Map
--------------------

We are keeping all discovered features in a database called sketch. The database has two main parts: pending features and confirmed features. Only confirmed features are used for locating frames. When the feature is first discovered and added to the database, it goes to the pending list. Only after it is determined that the same feature exists at the same location for certain number of successive frames (25), it goes into confirmed list. This is done in an effort to remove features belonging to moving objects.

These two parts are represented by:

1.  map of all detected features, which maps unique feature to its confirmation state
2.  multi-map of confirmed features, which maps feature descriptions to list of confirmed locations

The database has two operations:

1.  find operation that takes a list of features as input and returns the list of matches.
2.  update operation that updates database with the list of features. All features go through a confirmation process.

List of matches contains feature descriptions found in the confirmed list of the database and their known global locations. If some feature description from the input list is not found, it will not be present in the result set.

As feature extraction process returns feature locations that are local to the processed frame, we need to provide adjustment to sketch update operation. Adjustment is actual global location of the frame to which new features belong. Determining frame location is described in the next section.

The update loop looks like this:

1.  Extract features from a frame
2.  Match extracted features against current sketch
3.  Use matches to determine global location of the frame
4.  Update sketch with frame features adjusted to actual frame position: we add new features to pending list and move features with enough confirmations to confirmed list

<img width="401" height="321" alt="image" src="https://github.com/user-attachments/assets/8f8d4011-d019-4a57-93c9-9891d7e2ae8f" />

Sketch update loop

`Sketch` module defines sketch structure and two of the mentioned operations:

*   `Sketch` record - represents sketch (feature database)
*   `Matches` record - feature description matches
*   `find` function - find operation
*   `update` record - update operation

Determining Location
--------------------

To determine position of the frame, we use matches that find operation of feature databases has returned. As it was noted, match contains feature description and list of known positions. For each matched feature, we take all known positions and do the adjustment so we get position of top-left corner of the frame. We declare all these positions as candidates for possible position of the frame.

![](https://cloudfront.codeproject.com/recipes/5274896/locate.png)

<img width="600" height="222" alt="image" src="https://github.com/user-attachments/assets/9c161cb8-af68-4541-a3e2-b5865e8f5541" />

We count number of times each position appears among the candidates and use this count as an input to the formula that scores viability of position. The second parameter in this formula is position's distance from the previous frame's position. We select position with the largest value. The formula we use to calculate position's score looks like this:

$distance=\\sqrt{(x\_c-x\_p)^2+(y\_c-y\_p)^2}$

$\\delta={\\min(\\log(\\max(distance,1)),3)\*0.8 \\over 3}$

$score=count\*(1-\\delta)$

After we select a position the best score, we need to verify whether it is acceptable. In order for position to be verified, at least 50% of confirmed features in frame's region must be among the matches. If we get fewer matches in the region, we inform the caller that we failed to determine the frame's position.

<img width="450" height="423" alt="image" src="https://github.com/user-attachments/assets/27e99999-ea4b-4a77-af38-c578476c9e74" />

Example of criteria for accepting frame's position

`Locate` module has `byFeatureCount` function that is responsible for determining position of the frame based on the feature matches.

Detecting Moving Objects
------------------------

Moving object are making all kind of artifacts on our map, so we need to filter them as much as possible. To do so, we need to detect movement between two consecutive frames. Since two frames can have different global positions, we need to adjust positions and find overlapping area and then do movement detection only in that region.

Once we have made the adjustments, we have two more steps to do. In the first step, we find all contours whose pixels had any changes since the previous frame. In the second step, we create matrix which marks all pixels within bounding rectangles of such contours.

|                         Frame #1                            |                         Frame #2                     |
|-------------------------------------------------------------|------------------------------------------------------|
| <img width="285" height="145" alt="image" src="https://github.com/user-attachments/assets/b74a5e34-5f6c-44bc-b583-ae801ccfb658" /> | <img width="285" height="145" alt="image" src="https://github.com/user-attachments/assets/ef95b76a-e07b-4dde-8761-76914bb90ac9" /> |
| <img width="285" height="145" alt="image" src="https://github.com/user-attachments/assets/8fd5e3ed-e003-46d2-8571-809f9a965d6b" /> |
|                         Detected motion                     |

**Example of motion detection**

`Motion` module defines `mark` function that takes two consecutive frames and returns matrix that has the regions with detected movement marked.

Plotting Map
------------

So far, we have dealt with stuff needed by algorithm to make sense of the game's world and not it's actual visual representation. This is the final step in our effort to create unified view of the game's world. To do so, we keep a single buffer which stores content of each frame for which we determined location.

Size of the buffer occasionally needs to extended, we do it in increments of active window's height/weight - depending on which dimension needs to be extended. As our first frame has position _(0, 0)_, we also keep coordinates of the real top-left corner of the map. Each time we extend map to the left at the top, we need to update these coordinates.

Content of the current frame cannot just be pasted at determined location as moving objects would leave artifacts all over the map. So instead of keeping just the latest pixel value in map print, we keep history of all possible colors for each pixel. As we operate with the limited number of colors, we keep these values in a simple array. The idea is that each element of the array keeps the weight of a color. Weight is calculated based on the number of times certain color appeared on that location. Each time color is observer, its weight is increased, but only if no movement was detected in the region to which the pixel belongs. If movement was detected, weight of that color will reset.

Two options to generate actual pixel values are available: fast and smoothed. Fast just select color with highest weight, while smoothed option uses values in neighboring cells to perform blur-like operation.

`Print` module has `update` function that updates map print with current frame and `plot` and `quickPlot` that converts actual map print to an image.

Stitching It All Together
-------------------------

Our last effort is to combine all the stuff discussed previously. The implementation is two modules:

1.  `World` module - defines `World` record which holds world's sketch (feature database) and map print. It also tracks position of the last located frame. `update` function is responsible for updating world with each new frame: it locates frame, updates sketch and map print.
2.  `Stitcher` module - defines `loop` function. It takes sequence of frames and runs algorithm's main loop, creating world representation. It has three steps:
    1.  Identifying active window
    2.  Creating representation of game world
    3.  Plotting map print to actual image

Auxiliary Stuff
---------------

The following modules define auxiliary data types and functions:

*   `Primitive` module - defines `Point` and `Rect` records that are used for representing positions and regions in the world
*   `Palette` module - defines set of functions for converting pixel colors from different pallets and formats
*   `Matrix` module - defines `Matrix` class, that represents matrix used for representing image data and offers basic operation like initialization, resizing…
*   `Image` module - defines set of functions for converting image data (stored as matrix) to different pallets and formats
*   `Filter` module - provides implementation of median and blur filters
*   `Stitcher` module - has `IStitchingObserver` that can be used to notify external code about progress of the algorithm

Future Work and Improvements
----------------------------

As it was said at the beginning of the text, this is just a proof of concept, it needs more work to be practical and usable for real-time mapping while the game is being played. This is a short list of required improvements:

1.  Code optimization - as it is obvious, we cannot process world mapping in real-time
2.  Handling non-overlapping segments - the current implementation cannot handle different segments of the map that are not overlapping. If we are suddenly "teleported" to a different part of the world that has not been mapped yet, we would be "lost". We would not be able to patch our map with that part of the world.
3.  Inclusion of moving object - finding approach to track moving object and include them into final map print
4.  Improved filtering of artifacts - as the results show, we are not able to filter all the traces moving objects leave on our maps
