# Gesture Recognition Script for Unity

## What is this exactly?
I wrote this script for a game which involved making gestures like those a conductor would make for an orchestra. This is one of the main technical problems for scripting the game's mechanics.

The goal for this script: recognize player gestures made with a mouse.

The result: a script that allows for gestures to be recorded and for recognition to be performed using previously recorded gestures as targets. Almost everything about the process is customizable. Read the manual below for details.

## Manual
### Importing to Your Project
I made this as streamlined as possible by wrapping everything up into one c# script for Unity. All you have to do to test this out is make a 2D scene, add a camera, and add this script to the camera as a component! You will see all of the controls right away.

### The Controls
#### **Recording**
Have this checked to record gestures and save them to a JSON file called ".../StreamingAssets/gestures.json". Make sure to write a name in the Template Save Name field so the gesture is saved with a label of your choice.

When recording, click and drag your mouse to make the gesture. Unless Limit Samples is turned on, you can take as long as you want; recording is finished and your gesture is processed once you lift the mouse button. 

When recording is turned off, any gesture you make will be processed and then compared to recorded gestures. In the Unity console, a message is printed when you start and end a gesture, as well as the name of the matched gesture if recognition is run. 

#### **Anomalies Testing**
Checking this will turn on an extra feature when comparing your gesture to recorded gestures that weights certain differences more than others. Specifically, anomalies testing computes a standard deviation for the difference between gestures and weights differences that deviate from the average by a number of deviations of your choice (Dev Tightness). You can also set how much to weight these differences (Anomalies Factor).

> *This feature adds some computation and uses more memory than the standard, but does not affect the time complexity of the script. Also, it takes effect after gestures have been reduced to a set number of points (Points Per Gesture), meaning that it is only adding a few traversals of data that is limited by you. Going through 30 to 100 points a few more times will be trivial on almost any device.*

#### **Template Save Name**
The name your next gesture will be recorded as.

#### **Points Per Gesture**
This integer value will determine how many coordinate points each gesture will be turned into.

This value has the strongest effect on performance, as the mapping of data is one of the more computationally heavy parts of the script, and it will determine how many points must be compared when checking differences to each recorded gesture. Read about performance farther down this document.
> *Recording samples points at intervals of time, so gestures will have different amounts of recorded data depending on how long they took to make. To allow for reasonable comparison, they must be represented in a standard number of points.*

> *AVOID setting this number to extremes, 100 and below is plenty to capture the data of the movement, and 30 and up is enough to avoid disruptive data loss when mapping (there is always some loss unless you managed to record less points)*

> *Recommended Range: 30 to 100*
> 
#### Sampling Rate
This floating point value is the interval in seconds after which your mouse position will be sampled when recording.
> *You will want to set this to something much smaller than a second, since whatever motion you make with your mouse will almost certainly take only fractions of a second. Also keep in mind this will affect how many samples are taken, so if you set it too low, you might collect way more data than is necessary. The default value of 10 milliseconds is enough to record around 100 times per second, which is enough to collect what you need. Going below this might be pointless and behave unexpectedly, as you will be nearing the amount of time it actually takes to do the sampling (single milliseconds)*
> *Recommended Range: 0.01 to 0.10 seconds*
> 
#### Limit Samples
Turn this on to limit the amount of samples that will be taken before a gesture is processed automatically. When this is enabled, it is likely that your entire movement will not be captured.
This can be useful for experimenting with smaller Sampling Rate values and potentially if you want gestures to be made within a certain time frame.

#### Max Points Allowed
This integer value is the number of samples that will be allowed before a gesture is processed if Limit Samples is turned on. Keep in mind this interacts with the Sampling Rate, since the amount of samples you allow multiplied by the sampling rate will give you the approximate time frame you have to make a gesture before it is processed.

#### Standard Ratio
This represents the size of square that gestures will be scaled to. Gestures are not stretched or squashed, they are scaled down maintaining their aspect ratio to fit inside this square. 
> *This is necessary to allow for comparsion, by keeping the gestures at the same scale. Setting this too low has the possibility of making recognition a bit less reliable as the values being dealt with are smaller and will differ by smaller margins.*

> *Recommended Range: 100 to 500*

#### Dev Tightness
This floating point value is the amount of deviations before a difference between two gestures is weighted extra, if Anomalies Testing is turned on.
> *There is only a small range for this value that will actually have an effect. Below 1.0, Anomalies Testing will likely be weighing a majority of of points (assuming a close to normal random distribution), and above 2.0 it will barely be capturing anything.*

> *Recommended Range: 1.0 to 2.0*

#### Anomalies Factor
This floating point value is the amount that Anomalies Testing will weight differences that exceed the Dev Tightness threshold, if Anomalies Testing is turned on.
> *You can experiment alot with this value, but keep in mind that if you set it too high, there is a chance results will be less predictable, as an actually accidental 'anomaly' (say your hand shifted suddenly when drawing a line) could cause a gesture's difference to be distorted. Of course, what you set this to should take into account the Dev Tightness you set.*

## Performance
There are three major parts of the script to look at for performance:
* Sampling Data
* Processing Into Gestures
* Recognition

### Sampling Data
The collecting of the data as you draw is very streamlined. It inolves some conditional checks and some simple arithmetic. The location of the initial click is used as the point (0,0) from which to base each subsequent point from. The time complexity of this piece is O(1), as it does not change, and the actual performance is very quick.

### Processing Into Gestures
The set of points collected when drawing must be processed into a standard size and number of points to allow for comparison afterwards. This part is much more intensive than the actual sampling, as it traverses the list of points to scale them and then also to map them. The mapping requires a bit more computation, as there is interpolation going on, but this is still just arithmetic. The time complexity of this section is O(2n) = O(n) where n is the number of points sampled.

As such, the growth is not bad, but is dependent on the number of samples taken, which can potentially increase a lot if you lower the Sampling Rate value too much. The recommended range in the manual above avoids this, and the runtime is still very fast.

### Recognition
Points are compared between gestures and the recorded 'template gestures'. This means traversal of points will happen once for each template, as there is no way to pre order them and perform a greedy search or a dynamic programming algorithm because the difference cannot be predicted beforehand.

The time complexity of this part is O(n * m), where n is the number of points per gesture and m is the number of templates, which doesn't look exceptional, the intended use for this involves a relatively small amount of templates, and the number of points per gesture should be kept at the range recommended in the manual above (because more is actually not helpful past a certain amount). Because of all this, the runtime in the end is not bad, taking only about 100 to 200 milliseconds in some informal tests.


## Possible Additions
- [ ] Test for confidence of match and add a threshold to allow a 'no match' return even if there are templates
- [ ] Add a feature to allow grouping of recorded gestures, so that recognition can be run on specific groups
