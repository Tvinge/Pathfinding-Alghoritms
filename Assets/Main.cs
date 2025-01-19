using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System;
using Unity.VisualScripting;

public class Main : MonoBehaviour
{
    UIManager uiManager;
    public Action<int[]> OnPathChange;

    [SerializeField] int numberOfPoints = 10;
    [SerializeField] int antCount = 10;
    int antCounter = 0;
    int factorial = 0;
    int currentCombination = 0;
    int typeOfCalculation = 0;    
    int[] shortestCombination;
    int[][] antPaths;


    float shortestDistance = 0;
    float updateInterval = 1.0f; // Update interval in seconds
    float nextUpdateTime = 0.0f;
    //float antSpeed = 0.1f;


    GameObject StartEndNode;
    GameObject[] points;
    Stopwatch stopwatch = new Stopwatch();
    List<GameObject> lines = new List<GameObject>();

    Thread newThread;
    System.Random random;


    [Header("Prefabs")]
    [SerializeField] GameObject pointPrefab;
    [SerializeField] GameObject linePrefab;
    [SerializeField] GameObject pheromoneLinePrefab;
    [SerializeField] TextMeshProUGUI text;

    [Header("Ant Colony opt parameters")]
    [SerializeField] float powerOfDistance = 4;
    [SerializeField] float powerOfPheromone = 0.1f;
    [SerializeField] float evaporationRate = 0.5f;
    [SerializeField] int iterationsPerSecond = 2;



    [Header("Analyze output of alghorithm")]
    [SerializeField] string desString;
    [SerializeField] string pheromoneMatrixString;
    [SerializeField] string distanceMatrixString;


    [SerializeField] float[,] des;
    [SerializeField] float[,] pheromoneMatrix;
    [SerializeField] float[,] distanceMatrix;



    GameObject[,] pheromoneLines;
    UnityEngine.Color[,] colorLines;
    GameObject lineParentContainer;
    GameObject circleParentContainer;
    GameObject pheromoneLineParentContainer;





    private void Awake()
    {
        uiManager = FindFirstObjectByType<UIManager>();
        uiManager.onValueChangedTMP.AddListener(NewCalculation);        //uiManager.onValueChanged += NewCalculation;
        OnPathChange += DrawNewPath;
        random = new System.Random();
        antPaths = new int[numberOfPoints][];
        

        lineParentContainer = new GameObject();
        lineParentContainer.name = "LineParentContainer";
        circleParentContainer = new GameObject();
        circleParentContainer.name = "CircleParentContainer";
        pheromoneLineParentContainer = new GameObject();
        pheromoneLineParentContainer.name = "PheromoneLineParentContainer";

        SetupNewPoints();
    }




    private void Update()
    {
        if (newThread != null)
        {
            if (Input.GetMouseButtonDown(0))
                newThread.Abort();
            if (Input.GetMouseButtonDown(1))
                ResetData(false);

            if (newThread.IsAlive)
            {
                //if (antCounter >= antCount)
                {
                    antCounter = 0;
                    DestroyLines();
                    DrawLines(shortestCombination);
                }
            }
            else
                stopwatch.Stop();
        }

        for (int i = 0; i < points.Length; i++)
        {
            for (int j = 0; j < points.Length; j++)
            { 
                if (i != j)
                    UpdatePheromones(i,j);
            }
        }
                UpdateUI();

    }
    void DrawNewPath(int[] pathToDraw)
    {
        DestroyLines();
        DrawLines(pathToDraw);
    }
    public void AnotherCalculation()
    {
        NewCalculation(typeOfCalculation);
    }
    void NewCalculation(int type)
    {
        typeOfCalculation = type;
        ResetData(false);
        List<int[]> results = new List<int[]>();
        shortestCombination = new int[points.Length];
        int[] indices = new int[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            indices[i] = i;
        }


        stopwatch.Start();

        switch (type)
        {
            case 0:
                FindPathWithIndexes();
                break;
            case 1:
                newThread = new Thread(() => BackTrackAllPossibleConfigurations(indices, 0, results));
                newThread.Start();
                break;
            case 2:
                newThread = new Thread(() => SendSwarmOfAnts());
                newThread.Start();

                break;
            default:
                Console.WriteLine("Invalid option selected");
                break;
        }

    }
    #region solutions
    void SendSwarmOfAnts() //run ant algorithm multiple times on different threads, think about doing it via gpu
    {
        //antPaths = new int[timesToRun][];
        while (0 < 1)
        {
            List<int[]> combinations = new List<int[]>();

            for (int i = 0; i < antCount; i++)
            {
                combinations.Add(Ant());
                float distance = shortestDistance;
                if (combinations.Last().Length == points.Length)
                {
                    shortestDistance = FindShortestPathForCurrentCombination(combinations.Last(), shortestDistance);
                }
                else
                {
                    UnityEngine.Debug.Log("Error in ant path");
                }

            }
            antCounter++;

            float[,] newPheromoneMatrix = new float[numberOfPoints, numberOfPoints];

            for (int i = 0; i < shortestCombination.Length; i++)
            {
                int x = shortestCombination[i];
                int y = shortestCombination[(i + 1) % shortestCombination.Length]; // if i is the last index, nextIndex will be 0
                newPheromoneMatrix[x, y] += 1;
            }


            for (int i = 0; i < points.Length; i++)
            {
                for (int j = 0; j < points.Length; j++)
                {

                    pheromoneMatrix[i, j] = pheromoneMatrix[i, j] * evaporationRate + newPheromoneMatrix[i,j] ;
                }
            }

            System.Threading.Thread.Sleep(1000 / iterationsPerSecond);
        }
    }

    float GetPathTotalDistance(int[] path)
    {
        float totalDistance = 0;
        for (int i = 0; i < path.Length; i++)
        {
            totalDistance += GetDistance(points[path[i]], points[path[(i + 1) % path.Length]]);
        }
        return totalDistance;
    }

    int[] Ant()
    {
        int[] antPath = new int[points.Length];
        List<int> leftoverPointIndices = new List<int>(points.Length);
        for (int i = 0; i < points.Length; i++)
        {
            leftoverPointIndices.Add(i);
        }
        //int startEndIndex = UnityEngine.Random.Range(0, points.Length);//0 inclusive, points.Length exclusive
        int startEndIndex = random.Next(0, points.Length);
        antPath[0] = startEndIndex;
        leftoverPointIndices.Remove(antPath[0]);

        for (int i = 1; i < points.Length; i++)
        {
            antPath[i] = GetNextNode(antPath[i-1], leftoverPointIndices, startEndIndex);
            leftoverPointIndices.Remove(antPath[i]);
        }


        return antPath;
    }


    int GetNextNode(int currentNode, List<int> leftoverPoints, int starEndIndex)
    {
        if (leftoverPoints.Count == 0)
            return starEndIndex;

        float[] desirabilityOfPoints = new float[points.Length];



        for (int i = 0; i < points.Length; i++)
        {
            float dst = distanceMatrix[currentNode, i];
            float pheromoneStr = pheromoneMatrix[currentNode, i];

            if (leftoverPoints.Contains(i))
                desirabilityOfPoints[i] = pheromoneMatrix[currentNode, i] * Mathf.Pow(1 / dst, powerOfDistance);
            else
                desirabilityOfPoints[i] = 0;
        }

        int nextIndex = 0;


        if (desirabilityOfPoints.Sum() == 0)
        {
            float closestDistance = float.MaxValue;

            for (int i = 0; i < leftoverPoints.Count; i++)
            {
                if (closestDistance > distanceMatrix[currentNode, i] && distanceMatrix[currentNode, i] != 0)
                {
                    closestDistance = distanceMatrix[currentNode, i];
                    nextIndex = i;
                }
            }
        }
        else
        {

            nextIndex = GetWeighedValue(desirabilityOfPoints, leftoverPoints);
        }
        leftoverPoints.Remove(nextIndex);

        //nextIndex = GetWeighedValue(desirabilityOfPoints, leftoverPoints);

        pheromoneMatrix[currentNode, nextIndex] += 1 / distanceMatrix[currentNode, nextIndex];  //leave pheromones

        

        return nextIndex;
    }

    //gets random value based on desirability of points,
    //higher desirability means higher chance of getting that index
    int GetWeighedValue(float[] desirabilityOfPoints, List<int> leftoverPoints) 
    {
        float sumOfDesirability = desirabilityOfPoints.Sum();




        float randomValue = (float)random.NextDouble() * sumOfDesirability;//random value between 0 and sumOfDesirability,
                                                                           //nextdouble returns value between 0 and 1
        float value = 0;
        int index = 0;

        //searches for threshold, if value is higher than threshold, it chooses that index
        for (int i = 0; i < desirabilityOfPoints.Length; i++)
        {
            if (randomValue > value)
            {
               value += desirabilityOfPoints[i];
               index = i; 
            }
            else
                break;
        }
        return index;
    }


    //returns all posible permutations of the array, and finds the shorstest path
    void BackTrackAllPossibleConfigurations(int[] indices, int index, List<int[]> results)
    {
        if (index == indices.Length - 1)
        {
            results.Add((int[])indices.Clone());
            //calculatedFactorial = results.Count; // Update the calculated factorial count
        }
        if (results.Count - 1 == currentCombination)
        {
            shortestDistance = FindShortestPathForCurrentCombination(results[currentCombination], shortestDistance);
            currentCombination++;
        }
        // Recursively generate permutations with the last index fixed
        // (should reduce the number of permutations - skips identical permutations with different starting points?)
        for (int i = index; i < indices.Length - 1; i++)
        {
            Swap(indices, index, i); // makes a choice
            BackTrackAllPossibleConfigurations(indices, index + 1, results);
            Swap(indices, index, i); // backtracks a choice            
        }
    }
    void Swap(int[] array, int i, int j)
    {
        int temp = array[i];
        array[i] = array[j];
        array[j] = temp;
    }

    //Connects Points from point 1 to point 2 to point 3 and so on, redundant
    void FindPathWithIndexes()
    {
        float distance = 0;
        StartEndNode = points[0];
        for (int i = 0; i < points.Length; i++)
        {
            if (i == points.Length - 1)
            {
                distance += GetDistance(points[i], StartEndNode);
                UnityEngine.Debug.DrawLine(points[i].transform.position, StartEndNode.transform.position, UnityEngine.Color.black, 100);
            }
            else
            {
                distance += GetDistance(points[i], points[i + 1]);
                UnityEngine.Debug.DrawLine(points[i].transform.position, points[i + 1].transform.position, UnityEngine.Color.black, 100);
            }
        }
        UnityEngine.Debug.Log("Distance: " + distance);
    }



    #endregion
   

    void DrawLines(int[] shortestCombination)
    {

        for (int j = 0; j < shortestCombination.Length; j++)
        {
            GameObject line = Instantiate(linePrefab);
            lines.Add(line);
            LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, points[shortestCombination[j]].transform.position);
            if (j == shortestCombination.Length - 1)
            {
                lineRenderer.SetPosition(1, points[shortestCombination[0]].transform.position);
                line.name = points[shortestCombination[j]].name + " - " + points[shortestCombination[0]].name;
            }
            else
            {
                lineRenderer.SetPosition(1, points[shortestCombination[j + 1]].transform.position);
                line.name = points[shortestCombination[j]].name + " - " + points[shortestCombination[j + 1]].name;
            }

            for (int i = 0; i < 100; i++)
            {
                UnityEngine.Color color = new UnityEngine.Color(0, i, 0);

                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
            }

            line.transform.SetParent(lineParentContainer.transform);
        }
        UnityEngine.Debug.Log(("shortest combination: " + string.Join(", ", shortestCombination)));
    }
    void DestroyLines()
    {
        foreach (GameObject line in lines)
        {
            Destroy(line);
        }
        lines.Clear();
    }

    float FindShortestPathForCurrentCombination(int[] results, float sDistance)
    {
        float distance = 0;
        for (int j = 0; j < results.Length; j++)
        {
            int nextIndex = (j + 1) % results.Length; // if j is the last index, nextIndex will be 0
            distance += distanceMatrix[results[j], results[nextIndex]];
        }
        if (distance < sDistance)
        {
            sDistance = distance;
            shortestCombination = results;
            return sDistance;
        }
        if (sDistance == 0)
        {
            sDistance = distance;
            return sDistance;
        }
        return sDistance;
    }


    int CalculateFactorial(int n)
    {
        if (n <= 1)
            return 1;
        return n * CalculateFactorial(n - 1);
    }


    public void GetSetupNewPoints() // display in inspector
    {
        SetupNewPoints();
    }
    void SetupNewPoints()
    {
        ResetData(true);
        points = new GameObject[numberOfPoints];
        SpawnPoints(pointPrefab);
        factorial = CalculateFactorial(numberOfPoints - 1);
        distanceMatrix = InitializeDistanceMatrix();
        pheromoneMatrix = InitializePheromoneMatrix();
        colorLines = InitializeColorLines();
        pheromoneLines = InitializePheromoneLines();
        des = InitializeDes();

    }
    public GameObject[] SpawnPoints(GameObject pointPrefab)
    {
        List<Vector2> positions = new List<Vector2>();
        for (int i = 0; i < numberOfPoints; i++)
        {
            int x = UnityEngine.Random.Range(-5, 11);
            int y = UnityEngine.Random.Range(-4, 4);
            Vector2 position = new Vector2(x, y);
            if (positions.Contains(position))
            {
                i--;
                continue;
            }
            positions.Add(position);
            GameObject point = Instantiate(pointPrefab, position, Quaternion.identity);
            point.GetComponent<TextMeshPro>().text = i.ToString();
            point.name = i.ToString();
            point.transform.SetParent(circleParentContainer.transform);
            points[i] = point;

        }

        return points;
    }


    float[,] InitializeDes()
    {
        float[,] des = new float[numberOfPoints, numberOfPoints];
        for (int i = 0; i < numberOfPoints; i++)
        {
            for (int j = 0; j < numberOfPoints; j++)
            {
                if (i != j)
                {
                    des[i, j] = 0;
                }
            }
        }
        return des;
    }
    float[,] InitializePheromoneMatrix()
    {
        float[,] pheromoneMatrix = new float[numberOfPoints, numberOfPoints];
        for (int i = 0; i < numberOfPoints; i++)
        {
            for (int j = 0; j < numberOfPoints; j++)
            {
                if (i != j)
                {
                    pheromoneMatrix[i, j] = 1;// / distanceMatrix[i, j];
                }
                else
                    {
                    pheromoneMatrix[i, j] = 0;
                }
            }
        }
        return pheromoneMatrix;
    }
    float[,] InitializeDistanceMatrix()
    {
        float[,] distanceMatrix = new float[numberOfPoints, numberOfPoints];
        for (int i = 0; i < numberOfPoints; i++)
        {
            for (int j = 0; j < numberOfPoints; j++)
            {
                if (i != j)
                {
                    distanceMatrix[i, j] = GetDistance(points[i], points[j]);
                }
            }
        }
        return distanceMatrix;
    }
    UnityEngine.Color[,] InitializeColorLines()
    {
       UnityEngine.Color[,] colorLines = new UnityEngine.Color[numberOfPoints, numberOfPoints];
        for (int i = 0; i < points.Length; i++)
        {
            for (int j = 0; j < points.Length; j++)
            {
                if (i != j) 
                {
                    colorLines[i, j] = new UnityEngine.Color(1, 0, 0, 0);
                }
            }
        }
        return colorLines;
    }

    GameObject[,] InitializePheromoneLines()
    {
        if (pheromoneLines != null)
        {
            foreach (var line in pheromoneLines)
            {
                Destroy(line);
            }
        }

        GameObject[,] pheromoneLinesTemp = new GameObject[numberOfPoints, numberOfPoints];
        for (int i = 0; i < points.Length; i++)
        {
            for (int j = 0; j < points.Length; j++)
            {
                if (i != j) // Avoid drawing lines from a point to itself
                {
                    GameObject line = Instantiate(pheromoneLinePrefab);
                    LineRenderer lineRenderer = line.GetComponent<LineRenderer>();

                    // Set the positions of the line
                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPosition(0, points[i].transform.position);
                    lineRenderer.SetPosition(1, points[j].transform.position);
                    lineRenderer.startWidth = 0.01f;
                    lineRenderer.endWidth = 0.01f;
                    line.transform.SetParent(pheromoneLineParentContainer.transform);
                    pheromoneLinesTemp[i, j] = line;
                }
            }
        }
        return pheromoneLinesTemp;
    }


    void UpdatePheromones(int i, int j)
    {
        
        float opacity = Mathf.Clamp(pheromoneMatrix[i, j], 0, 1);
        colorLines[i, j] = new UnityEngine.Color(255, 0, 0, opacity);

        pheromoneLines[i, j].GetComponent<LineRenderer>().material.color = colorLines[i, j];

        //pheromoneLines[i, j].GetComponent<LineRenderer>().startColor = colorLines[i, j];
        //pheromoneLines[i, j].GetComponent<LineRenderer>().endColor = colorLines[i, j];

    }

    public float GetDistance(GameObject point1, GameObject point2)
    {
        return Vector2.Distance(point1.transform.position, point2.transform.position);
    }
    public void ResetData(bool resetPoints)
    {
        if (points != null && resetPoints == true)
        {
            foreach (var point in points)
            {
                Destroy(point);
            }
        }

        shortestDistance = 0;
        stopwatch.Reset();
        currentCombination = 0;
        antCounter = 0;

        antPaths = new int[numberOfPoints][];
        if (newThread != null)
        {
            newThread.Abort();
        }

    }
    void UpdateUI()
    {
        nextUpdateTime = Time.time + updateInterval;

        text.text = numberOfPoints + " point problem\n" +
        "Searched " + currentCombination + " / " + factorial + " possible combinations\n" +
        "Finished " + GetPercent(currentCombination, factorial) + "%\n" +
        stopwatch.ElapsedMilliseconds + " time\n" +
        "Shortest Distance: " + shortestDistance;
    }
    float GetPercent(int a, int b)
    {
        float c = (float)currentCombination / factorial * 100;
        return Mathf.Round(c);
    }

}