using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

//******************************************* Gesture Recognizer *********************************************//
//
//      Author:         Andres De La Fuente Duran
//
//      Use:            This script can simply be attached to a camera to function.
//                      It allows for recording of 2D 'gestures', in this case meaning a set of points
//                      representing a continuous motion of the mouse (or touch input).
//                      It also allows for testing 'gestures' against a collection of templates created
//                      with this very same script.
//
//                      Template gestures are saved in a JSON file and loaded into a list at start.
//                      
//                      The way the recognition works is that a list of points is recorded (relative to
//                      the location of the initial click as (0,0)), scaled to a square resolution of 
//                      choice, reduced by interpolation to a set number of points, and then compared
//                      to gestures already processed by this script.
//
//                      This is built for maximum customizability, so you can change the number of points
//                      allowed when recording, the number of points once reduced, the rate of sampling,
//                      and the square ratio gestures are scaled to. Recording the gestures and testing
//                      can be done easily by swithching the 'recording' boolean variable.
//
//                      Some additional notes:      Because the origin of each gesture is the initial
//                                                  point, and comparison follows points in order of
//                                                  recording, directionality is captured by this
//                                                  solution. The gestures do not have to be wildly 
//                                                  different for the recognition to be reliable.
//
//                                                  However, you can turn on 'anomaliesTesting' to
//                                                  weight more heavily sudden differences in gestures
//                                                  than constant differences to allow for similar
//                                                  gestures with small modifications or flares.




//****************************************** Recognizer Class ****************************************************//
// 
//      Use:        Stores all information for the current gesture being recorded, the existing gestures,
//                  the conditions selected by an editor user, and variables needed to perform recognition.
//                  This is the central class with most of the functionality in the script.
//
//      Fields:     
//                  Editor Controlled............................................................................
//
//                  recording:          boolean to control whether to save a gesture or try to recognize it
//                  
//                  anomaliesTesting:   boolean to control whether to weight sudden differences during
//                                      comparison more than other differences
//
//                  pointsPerGesture:   the size of the array of points stored for each gesture
//                   
//                  templateSaveName:   the string name of the gesture to be saved when recording
//
//                  samplingRate:       time interval between samples while recording
//
//                  maxPointsAllowed:   the maximum number of points that will be recorded
//
//                  standardRatio:      the size of one side of the square that points will be scaled to
//
//                  devTightness:       the number of deviations from the average difference between the points
//                                      of two gestures that are allowed before they are weighted more
//
//                  anomaliesFactor:    how much extra to weight the differences that surpass the devTightness
//
//                  Control Flow................................................................................
//                  
//                  gestureStarted:     boolean to execute code to start gesture and to avoid starting anew
//                  
//                  gestureComplete:    boolean to execute recording of gesture until complete
//
//                  inputReady:         boolean to prevent execution of anything until input is lifted
//                                      so as not to start gestures immediately after one is complete
//
//                  Recording and Recognizing...................................................................
//
//                  gestureFileName:    JSON file to load saved gestures from as templates for recognition
//          
//                  startPoint:         the initial point from which to calculate every other point
//
//                  currentPoint:       the last point recorded
//                  
//                  currentGesture:     the object containing the recorded gesture for current execution
//      
//                  currentPointList:   list of points as they are recorded
//
//                  reducedPoints:      array of points for after scaling and mapping of currentPointList
//
//                  templates:          object to store list of template gestures
//
//                  tempTime:           time since last sample
//
//      Methods:    Documentation is above each significant function

public class GestureRecognizer : MonoBehaviour {

    public bool recording = true;
    public bool anomaliesTesting = false;
    public string templateSaveName;
    public int pointsPerGesture = 30;
    public float samplingRate = 0.01f;
    public bool limitSamples = false;
    public int maxPointsAllowed = 100;
    public float standardRatio = 100f;
    public float devTightness = 1f;
    public float anomaliesFactor = 5f;

    private bool gestureStarted;
    private bool gestureComplete;
    private bool inputReady;

    private string gestureFileName = "gestures.json";
    private TwoDPoint startPoint;
    private TwoDPoint currentPoint;
    private DrawnGesture currentGesture;
    private List<TwoDPoint> currentPointList;
    private TwoDPoint[] reducedPoints;
    private GestureTemplates templates;
    private float tempTime = 0f;

    


    private void Awake()
    {
        
    }

    void Start () {
        LoadTemplates();
        varInitialization();
    }
    
    #region variable initialization and reset
    private void varInitialization()
    {
        currentPoint = new TwoDPoint(0, 0);
        startPoint = new TwoDPoint(0, 0);
        currentPointList = new List<TwoDPoint>();
        currentPointList.Add(new TwoDPoint(0, 0));
        reducedPoints = new TwoDPoint[pointsPerGesture];
        for (int i = 0; i < pointsPerGesture; i++)
        {
            reducedPoints[i] = new TwoDPoint(0, 0);
        }
        gestureStarted = false;
        gestureComplete = false;
        inputReady = false;
        currentGesture = new DrawnGesture("currentGesture", pointsPerGesture);
    }


    private void varReset()
    {
        for (int i = 0; i < pointsPerGesture; i++)
        {
            reducedPoints[i].SetX(0);
            reducedPoints[i].SetY(0);
        }
        currentPointList.Clear();
        currentPointList.Add(new TwoDPoint(0,0));
        gestureStarted = false;
        gestureComplete = false;
    }

    #endregion

    void Update() {
        tempTime += Time.deltaTime;
        if (Input.GetMouseButton(0))
        {
            if (inputReady)
            {
                if (!gestureStarted)
                {
                    gestureStarted = true;
                    StartGesture();
                }
                if ((!gestureComplete) && (tempTime > samplingRate))
                {
                    tempTime = 0f;
                    ContinueGesture();
                }
                if (gestureComplete)
                {
                    EndGesture();
                }
            }
        } else
        {
            if (gestureStarted)
            {
                EndGesture();
            }
            inputReady = true;
        }
    }


    //******************************************
    //      Save and Load Gestures
    //
    //      SaveTemplates
    //      use:                writes templates to json file
    //      LoadTemplates
    //      use:                called on start to read json templates
    //                          object from file if it's there
    private void SaveTemplates()
    {
        string filePath = Application.dataPath + "/StreamingAssets/" + gestureFileName;
        string saveData = JsonUtility.ToJson(templates);
        File.WriteAllText(filePath, saveData);
    }

    private void LoadTemplates()
    {
        templates = new GestureTemplates();
        string filePath = Path.Combine(Application.streamingAssetsPath, gestureFileName);
        if (File.Exists(filePath))
        {
            string data = File.ReadAllText(filePath);
            templates = JsonUtility.FromJson<GestureTemplates>(data);
        }
    }


    //***************************************
    //      StartGesture
    //
    //      use:            Set up recording of gesture by
    //                      setting the start point and control bool.
    //                      Called when player first clicks.
    private void StartGesture()
    {
        Debug.Log("gesture started");
        startPoint.SetX(Input.mousePosition.x);
        startPoint.SetY(Input.mousePosition.y);
        gestureComplete = false;
    }


    //***************************************
    //      ContinueGesture
    //
    //      use:            Update min and max x and y values for
    //                      the current gesture being recorded
    //                      and add the new point to the list.
    //                      Called while player holds input down.
    private void ContinueGesture()
    {
        currentPoint.SetX(Input.mousePosition.x - startPoint.GetX());
        currentPoint.SetY(Input.mousePosition.y - startPoint.GetY());
        currentPointList.Add(new TwoDPoint(currentPoint.GetX(), currentPoint.GetY()));
        if (currentPoint.GetX() > currentGesture.GetMaxX())
        {
            currentGesture.SetMaxX(currentPoint.GetX());
        }
        if (currentPoint.GetX() < currentGesture.GetMinX())
        {
            currentGesture.SetMinX(currentPoint.GetX());
        }
        if (currentPoint.GetY() > currentGesture.GetMaxY())
        {
            currentGesture.SetMaxY(currentPoint.GetY());
        }
        if (currentPoint.GetY() < currentGesture.GetMinY())
        {
            currentGesture.SetMinY(currentPoint.GetY());
        }
        if (limitSamples && currentPointList.Count >= maxPointsAllowed)
        {
            gestureComplete = true;
            Debug.Log(message: "Gesture Complete!");
        }
    }


    //***************************************
    //      EndGesture
    //
    //      use:            Resets control bools and other variables
    //                      records gesture to the templates object
    //                      or calls recognition.
    //                      Called when max recording points reached.
    private void EndGesture()
    {
        if (inputReady) inputReady = false;
        gestureStarted = false;
        gestureComplete = true;
        Rescale(currentGesture);
        MapPoints(currentGesture);
        if (recording)
        {
            currentGesture.SetName(templateSaveName);
            templates.templates.Add(new DrawnGesture(currentGesture.GetName(), pointsPerGesture, currentGesture.GetMaxX(), currentGesture.GetMaxY(),
                currentGesture.GetMinX(), currentGesture.GetMinY(), currentGesture.GetPoints()));
        } else
        {
            DrawnGesture m = FindMatch(currentGesture, templates);
            Debug.Log(m.GetName());
        }
        varReset();
    }


    //***************************************
    //      Rescale
    //
    //      use:        scales recorded list of points to a square field
    //                  of a chosen size by multiplication of the factor
    //                  of the desired size it already is
    //                  Called on every gesture after recording
    private void Rescale(DrawnGesture gesture)
    {
        float scale = 1f;
        float xrange = gesture.GetMaxX() - gesture.GetMinX();
        float yrange = gesture.GetMaxY() - gesture.GetMinY();
        if (xrange >= yrange)
        {
            scale = standardRatio / (gesture.GetMaxX() - gesture.GetMinX());
        } else
        {
            scale = standardRatio / (gesture.GetMaxY() - gesture.GetMinY());
        }
        if (scale != 1)
        {
            foreach (TwoDPoint point in currentPointList)
            {
                point.SetX(point.GetX() * scale);
                point.SetY(point.GetY() * scale);
            }
        }
    }


    //***************************************
    //      MapPoints
    //
    //      use:        maps the list of recorded points to a desired
    //                  number of points by calculating an even distance
    //                  between such a number of points and interpolating
    //                  when that distance is reached upon traversal of the
    //                  list
    //                  Called after scaling on every gesture
    //
    //      param:      gesture:    the object to store the new array
    private void MapPoints(DrawnGesture gesture)
    {
        reducedPoints[0].SetX(currentPointList[0].GetX());
        reducedPoints[0].SetY(currentPointList[0].GetY());
        int newIndex = 1;
        float totalDistance = TotalDistance();
        float coveredDistance = 0;
        float thisDistance = 0;
        float idealInterval = totalDistance / pointsPerGesture;
        for (int i = 0; i < currentPointList.Count - 1; i++)
        {
            thisDistance = PointDistance(currentPointList[i], currentPointList[i + 1]);
            bool passedIdeal = (coveredDistance + thisDistance) >= idealInterval;
            if (passedIdeal)
            {
                TwoDPoint reference = currentPointList[i];
                while (passedIdeal && newIndex < reducedPoints.Length)
                {
                    float percentNeeded = (idealInterval - coveredDistance) / thisDistance;
                    if (percentNeeded > 1f) percentNeeded = 1f;
                    if (percentNeeded < 0f) percentNeeded = 0f;
                    float new_x = (((1f - percentNeeded) * reference.GetX()) + (percentNeeded * currentPointList[i + 1].GetX()));
                    float new_y = (((1f - percentNeeded) * reference.GetY()) + (percentNeeded * currentPointList[i + 1].GetY()));
                    reducedPoints[newIndex] = new TwoDPoint(new_x, new_y);
                    reference = reducedPoints[newIndex];
                    newIndex++;
                    thisDistance = (coveredDistance + thisDistance) - idealInterval;
                    coveredDistance = 0;
                    passedIdeal = (coveredDistance + thisDistance) >= idealInterval;
                }
                coveredDistance = thisDistance;
            } else
            {
                coveredDistance += thisDistance;
            }
            gesture.SetPoints(reducedPoints);
        }

    }


    //***************************************
    //      FindMatch
    //
    //      use:        determines template gesture with the minimum
    //                  average distance between points to the 
    //                  currently recorded gesture
    //                  Called after finishing a gesture when not
    //                  recording
    //
    //      param:      playerGesture:  current gesture to be matched
    //                  templates:      object containting list of 
    //                                  gestures to compare against
    //
    //      return:     returns gesture object of the minimum 
    //                  difference template
    private DrawnGesture FindMatch(DrawnGesture playerGesture, GestureTemplates templates)
    {
        float minAvgDifference = float.MaxValue;
        DrawnGesture match = new DrawnGesture("no match", pointsPerGesture);
        foreach(DrawnGesture template in templates.templates)
        {
            Debug.Log(template.GetName());
            float d = AverageDifference(playerGesture, template);
            Debug.Log(d.ToString());
            if (d < minAvgDifference)
            {
                minAvgDifference = d;
                match = template;               
            }
        }
        return match;
    }


    //***************************************
    //      AverageDifference
    //
    //      use:        caluclates the average distance between 
    //                  the points of two gestures
    //
    //      param:      playerGesture:  first to be compared
    //                  template:       gesture to be compared against
    //
    //      return:     returns float value of the average distance
    //                  between points of two parameter gestures
    private float AverageDifference(DrawnGesture playerGesture, DrawnGesture template)  
    {
        int numPoints = playerGesture.GetNumPoints();

        if (numPoints != template.GetNumPoints())
        {
            Debug.Log("Number of points differs from templates");
            return -1f;
        }

        float totalDifference = 0;

        for (int i = 0; i < numPoints; i++)
        {
            totalDifference += PointDistance(playerGesture.GetPoints()[i], template.GetPoints()[i]);
        }

        return (totalDifference / numPoints);
    }


    //***************************************
    //      AverageDistanceWithAnomalies
    //
    //      use:        calculates the average difference between 
    //                  the points of two gestures but weighing
    //                  those which deviate significantly by 
    //                  multiplying them
    //                  Both the tightness of this and the factor
    //                  of multiplication are customizable
    //                  above
    //
    //      param:      playerGesture:  first to be compared
    //                  template:       gesture to be compared against
    //
    //      return:     returns float value of the average distance
    //                  between points of two parameter gestures
    //                  with weights
    private float AverageDifferenceWithAnomalies(DrawnGesture playerGesture, DrawnGesture template)
    {
        int numPoints = playerGesture.GetNumPoints();

        if (numPoints != template.GetNumPoints())
        {
            Debug.Log("Number of points differs from templates");
            return -1f;
        }

        float totalDifference = 0;
        float[] sampleDifferences = new float[numPoints];
        float[] sampleDeviations = new float[numPoints];
        float standardDev = 0;

        for (int i = 0; i < numPoints; i++)
        {
            float thisDistance = PointDistance(playerGesture.GetPoints()[i], template.GetPoints()[i]);
            sampleDifferences[i] = thisDistance;
            totalDifference += thisDistance;
        }

        float average = totalDifference / numPoints;

        for (int i = 0; i < numPoints; i++)
        {
            sampleDeviations[i] = Math.Abs(sampleDifferences[i] - average);
            standardDev += sampleDifferences[i];
        }

        standardDev = standardDev / numPoints;

        for (int i = 0; i < numPoints; i++)
        {
            if (Math.Abs(sampleDeviations[i]) > devTightness * standardDev)
            {
                totalDifference -= sampleDifferences[i];
                totalDifference += anomaliesFactor * sampleDifferences[i];
            }
        }

        average = totalDifference / numPoints;

        return (average);
    }

    //***************************************
    //      TotalDistance
    //
    //      use:        calculates the total distance covered
    //                  when traversing the current list of recorded
    //                  points in order of recording
    //                  Called when determining ideal intervals
    //                  for mapping onto desired number of points
    private float TotalDistance()
    {
        float totalDistance = 0;
        for(int i = 0; i < currentPointList.Count - 1; i++)
        {
            totalDistance += PointDistance(currentPointList[i], currentPointList[i + 1]);
        }
        Debug.Log("total distance: " + totalDistance);
        return totalDistance;
    }


    //***************************************
    //      PointDistance
    //
    //      use:        calculates the absolute value of the distance
    //                  between two points using pythagorean theorem
    private float PointDistance(TwoDPoint a, TwoDPoint b)
    {
        float xDif = a.GetX() - b.GetX();
        float yDif = a.GetY() - b.GetY();
        return Mathf.Sqrt((xDif * xDif) + (yDif * yDif));
    }
}





//******************************************************* Templates ******************************************************//
//
//      Use:    Groups gestures to be used for comparison to a player's attempts

[Serializable]
public class GestureTemplates
{
    public List<DrawnGesture> templates;

    public GestureTemplates()
    {
        templates = new List<DrawnGesture>();
    }

}





//******************************************************** Gestures ******************************************************//
//
//      Use:    Groups all information pertinent to a 'gesture'
//              which is essentially a single stroke drawing represented by points
//
//      Fields:     points:     list of points representing the gesture, only populated once a hand drawn gesture is 
//                              reduced by the MapPoints method
//
//                  min/max:    these are the minimum and maximum x and y values of the points (starting point 
//                              is used as the origin)
//
//                  numPoints:  the size of the points array (set to a variable of the GestureRecognizer class to 
//                              keep control there)
//
//                  name:       string that will be returned when matched with a non-recorded gesture
//
//      Methods:    Initializer(2 parameters):  use when creating a new gesture for later use
//
//                  Initializer(7 parameters):  use when copying data from another gesture
//
//                  Reset:                      for use in clearing the gesture used for each player gesture attempt

[Serializable]
public class DrawnGesture
{
    private TwoDPoint[] points;
    private string name;
    private float maxX;
    private float minX;
    private float maxY;
    private float minY;
    private int numPoints;

    public DrawnGesture(string newName, int pointsPerGesture)
    {
        numPoints = pointsPerGesture;
        points = new TwoDPoint[numPoints];
        name = newName;
        maxX = 0;
        maxY = 0;
    }
    public DrawnGesture(string newName, int pointsPerGesture, float max_x, float max_y, float min_x, float min_y, TwoDPoint[] newPoints)
    {
        numPoints = pointsPerGesture;
        points = new TwoDPoint[numPoints];
        SetPoints(newPoints);
        name = newName;
        maxX = max_x;
        minX = min_x;
        maxY = max_y;
        minY = min_y;
    }
    public void Reset()
    {
        maxX = 0;
        minX = 0;
        maxY = 0;
        minY = 0;
        name = "";
        Array.Clear(points, 0, numPoints);
    }

    public TwoDPoint[] GetPoints()
    {
        return points;
    }
    public void SetPoints(TwoDPoint[] new_points)
    {
        for(int i = 0; i < numPoints; i++)
        {
            points[i] = new TwoDPoint(new_points[i].GetX(), new_points[i].GetY());
        }
    }
    public string GetName()
    {
        return name;
    }
    public void SetName(string n)
    {
        name = n;
    }
    public float GetMaxX()
    {
        return maxX;
    }
    public void SetMaxX(float x)
    {
        maxX = x;
    }
    public float GetMaxY()
    {
        return maxY;
    }
    public void SetMaxY(float y)
    {
        maxY = y;
    }
    public float GetMinY()
    {
        return minY;
    }
    public void SetMinY(float y)
    {
        minY = y;
    }
    public float GetMinX()
    {
        return minX;
    }
    public void SetMinX(float x)
    {
        minX = x;
    }
    public int GetNumPoints()
    {
        return numPoints;
    }
    public void SetNumPoints(int n)
    {
        numPoints = n;
    }
}






//******************************************************** Points ********************************************************//
//
//      Use:    This is a class to maintain 2D coordinates
//      
//      Fields:     x:  the x coordinate (relative to the first point when recorded)
//                  y:  the y coordinate (also relative to first point)

public class TwoDPoint
{
    private float x;
    private float y;

    public TwoDPoint(float startx, float starty)
    {
        x = startx;
        y = starty;
    }

    public float GetX()
    {
        return x;
    }
    public void SetX(float new_x)
    {
        x = new_x;
    }
    public float GetY()
    {
        return y;
    }
    public void SetY(float new_y)
    {
        y = new_y;
    }

} 
