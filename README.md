# Gesture Recognition Script for Unity

## What is this exactly?
I wrote this script for my game project, Maestro. The premise of the game, in brief, is that the player is a musical conductor liberating famous composers throughout history from their own music. As such, it involves making gestures like those a conductor would make for an orchestra. This presented one of the main technical problems for scripting the gameplay, the others being creating a custom 2D physics, scripting enemies, and potentially interpreting music files to generate the levels.

The goal for this script: recognize the player's gestures.

The result: a script that allows for gestures to be recorded and for recognition to be performed using recorded gestures as targets. Almost everything about the recording and recognition is customizable. Read the manual below for details.

To read about my thoughts and process in writing this script, read about it on my website: [Andy De La Fuente](https://andydelafuente.com/AD_Projects.html)

## Manual
### Importing to Your Project
I made this as streamlined as possible by wrapping everything up into one c# script for Unity. All you have to do to test this out is make a 2D scene, add a camera, and add this script to the camera as a component! You will see all of the controls right away.

### The Controls
#### **Recording**
Have this checked to record gestures and save them to a JSON file called ".../StreamingAssets/gestures.json". Make sure to write a name in the Template Save Name field so the gesture is saved with a label of your choice.

When recording, you can simplpy click and drag your mouse to make any gesture you want. Unless Limit Samples is turned on, you can take as long as you want; recording is finished and your gesture is processed once you lift the mouse button. 

When recording is turned off, any gesture you make will be processed and then compared to recorded gestures. In the Unity console, a message is printed when you start and end a gesture, as well as the name of the matched gesture if recognition is run. 
#### **Anomalies Testing**
Checking this will turn on an extra feature when comparing your gesture to recorded gestures that weights certain differences more than others. Specifically, anomalies testing computes a standard deviation for the difference between gestures and weights differences that deviate from the average by a number of deviations of your choice (Dev Tightness). You an also set how much to weight these differences (Anomalies Factor).

*This feature adds some computation and uses more memory than the standard, but does not affect the time complexity of the script. Also, it takes effect after gestures have been reduced to a set number of points (Points Per Gesture), meaning that it is only adding a few traversals of data that is limited by you. Going through 30 to 100 points a few more times will be trivial on almost any device.*
#### **Template Save Name**
This is the name the next gesture you make will be recorded as, if recording is turned on.
#### **Points Per Gesture**
This integer value will determine how many points gestures will be reduced (or increased) to. Recording measures points at short intervals (Sampling Rate), so the data has to be mapped onto a standard number of points to allow reasonable comparison.

This value has the strongest effect on performance, as the mapping of data is one of the more computationally heavy parts of the script, and it will determine how many points must be compared when checking differences to each recorded gesture. Read about performance in the farther down this document.

**Avoid setting this number to extremes, 100 and below is plenty to capture the data of the movement, and 30 an up is enough to avoid disruptive data loss when mapping (there is always some loss unless you managed to record less points)**

**Recommended Range: 30 to 100**

