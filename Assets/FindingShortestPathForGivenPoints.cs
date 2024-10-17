using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;
using System.Diagnostics;
using System.Collections;
using UnityEngine.Rendering;
using System.Threading;
using System.Threading.Tasks;

public class FindingShortestPathForGivenPoints : MonoBehaviour
{
    [SerializeField] int numberOfPoints = 10;
    int factorial = 0;
    int calculatedFactorial = 0;
    int counter = 0;
    int[] shortestCombination;

    float shortestDistance = 0;
    float distance = 0;
    float updateInterval = 1.0f; // Update interval in seconds
    float nextUpdateTime = 0.0f;
    float[,] distanceMatrix;

    [SerializeField] GameObject pointPrefab;
    [SerializeField] GameObject linePrefab;
    [SerializeField] TextMeshProUGUI text;
    GameObject StartEndNode;
    GameObject[] points;
    Stopwatch stopwatch = new Stopwatch();
    List<GameObject> lines = new List<GameObject>();

    Thread newThread;

    private void Start()
    {
        points = new GameObject[numberOfPoints];
        List<int[]> results = new List<int[]>();
        int[] indices = new int[points.Length];
        shortestCombination = new int[points.Length];

        SpawnPoints(pointPrefab);
        factorial = CalculateFactorial(numberOfPoints - 1);

        for (int i = 0; i < points.Length; i++)
        {
            indices[i] = i;
        }

        // Initialize distance matrix
        distanceMatrix = new float[numberOfPoints, numberOfPoints];
        for (int i = 0; i < numberOfPoints; i++)
        {
            for (int j = 0; j < numberOfPoints; j++)
            {
                distanceMatrix[i, j] = GetDistance(points[i], points[j]);
            }
        }
        stopwatch.Start();
        newThread = new Thread(() => BackTrackNoCor(indices, 0, results));
        newThread.Start();

    }
    private void Update()
    {
        if (newThread.IsAlive)
        {
            DestroyLines();
            DrawLine(shortestCombination);
            UpdateUI();
        }
        else
        {
            stopwatch.Stop();
        }
    }
    void BackTrackNoCor(int[] indices, int index, List<int[]> results)
    {
        if (index == indices.Length - 1)
        {
            results.Add((int[])indices.Clone());
            calculatedFactorial = results.Count; // Update the calculated factorial count
        }
        if (results.Count - 1 == counter)
        {
            FindShortestPathForCurrentCombination(results[counter], counter);
            counter++;
        }
        // Recursively generate permutations with the last index fixed
        // (should reduce the number of permutations - skips identical permutations with different starting points?)
        for (int i = index; i < indices.Length - 1; i++)
        {
            Swap(indices, index, i); // makes a choice
            BackTrackNoCor(indices, index + 1, results);
            Swap(indices, index, i); // backtracks a choice            
        }
    }
    void Swap(int[] array, int i, int j)
    {
        int temp = array[i];
        array[i] = array[j];
        array[j] = temp;
    }
    void DrawLine(int[] shortestCombination)
    {
        for (int j = 0; j < shortestCombination.Length; j++)
        {
            GameObject line = Instantiate(linePrefab);
            lines.Add(line);
            LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, points[shortestCombination[j]].transform.position);
            if (j == shortestCombination.Length - 1)
                lineRenderer.SetPosition(1, points[shortestCombination[0]].transform.position);
            else
                lineRenderer.SetPosition(1, points[shortestCombination[j + 1]].transform.position);
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

    void FindShortestPathForCurrentCombination(int[] results, int counter)
    {
        for (int j = 0; j < results.Length; j++)
        {
            int nextIndex = (j + 1) % results.Length; // if j is the last index, nextIndex will be 0
            distance += distanceMatrix[results[j], results[nextIndex]];
        }
        if (shortestDistance == 0)
        {
            shortestDistance = distance;
        }
        if (distance < shortestDistance)
        {
            shortestDistance = distance;
            shortestCombination = results;
        }
        distance = 0;
        calculatedFactorial = counter + 1;
    }

    //Connects Points from point 1 to point 2 to point 3 and so on, redundant
    void FindPathWithIndexes()
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (i == points.Length - 1)
            {
                distance += GetDistance(points[i], StartEndNode);
                UnityEngine.Debug.DrawLine(points[i].transform.position, StartEndNode.transform.position, Color.black, 1000);
            }
            else
            {
                distance += GetDistance(points[i], points[i + 1]);
                UnityEngine.Debug.DrawLine(points[i].transform.position, points[i + 1].transform.position, Color.black, 1000);
            }
        }
        UnityEngine.Debug.Log("Distance: " + distance);
    }
    int CalculateFactorial(int n)
    {
        if (n <= 1)
            return 1;
        return n * CalculateFactorial(n - 1);
    }
    GameObject[] SpawnPoints(GameObject pointPrefab)
    {
        List<Vector2> positions = new List<Vector2>();
        for (int i = 0; i < numberOfPoints; i++)
        {
            int x = UnityEngine.Random.Range(-11, 11);
            int y = UnityEngine.Random.Range(-4, 4);
            Vector2 position = new Vector2(x, y);
            if (positions.Contains(position))
            {
                i--;
                continue;
            }
            positions.Add(position);
            GameObject point = Instantiate(pointPrefab, position, Quaternion.identity);
            points[i] = point;
        }
        return points;
    }
    public float GetDistance(GameObject point1, GameObject point2)
    {
        return Vector2.Distance(point1.transform.position, point2.transform.position);
    }
    private void Reset()
    {
        foreach (var point in points)
        {
            Destroy(point);
        }
        shortestDistance = 0;
        stopwatch.Reset();
        calculatedFactorial = 0;
        counter = 0;
        
        StopAllCoroutines();
        Start();
    }
    void UpdateUI()
    {
        nextUpdateTime = Time.time + updateInterval;

        text.text = numberOfPoints + " point problem\n" +
        "Searched " + calculatedFactorial + " / " + factorial + " possible combinations\n" +
        "Finished " + GetPercent(calculatedFactorial, factorial) + "%\n" +
        stopwatch.ElapsedMilliseconds + " time\n" +
        "Shortest Distance: " + shortestDistance;
    }
    float GetPercent(int a, int b)
    {
        float c = (float)calculatedFactorial / factorial * 100;
        return Mathf.Round(c);
    }
    public void NewCalculation()
    {
       Reset();
    }
}