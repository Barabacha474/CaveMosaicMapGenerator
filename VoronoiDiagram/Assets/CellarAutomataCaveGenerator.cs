using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]

public class CaveGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    public bool generateFromStart = true;
    public int seed;
    public int width = 128;
    public int height = 128;
    public float chanceToStartAlive = 0.45f;
    public int birthLimit = 4;
    public int deathLimit = 3;
    public int numberOfSteps = 5;
    public bool showIntermediateSteps = false;
    public int stepDelay = 1;
    public string saveFileName = "CaveMap.png";

    private bool[][] cellmap;
    
    [HideInInspector]
    public Texture2D finalTexture;

    private void Start()
    {
        if (generateFromStart)
        {
            StartCoroutine(GenerateCave());
        }
    }
    
    public IEnumerator GenerateCave()
    {
        cellmap = InitializeMap();
        
        // Run the simulation for the specified number of steps
        for (int i = 0; i < numberOfSteps; i++)
        {
            cellmap = DoSimulationStep(cellmap);

            // Display intermediate steps if enabled
            if (showIntermediateSteps)
            {
                Texture2D texture = ConvertMapToTexture(cellmap);
                GetComponent<SpriteRenderer>().sprite = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * 0.5f);
                
                // Wait for the specified delay before showing the next step
                yield return new WaitForSeconds(stepDelay);
            }
        }

        // Display final result and save to file
        finalTexture = ConvertMapToTexture(cellmap);
        GetComponent<SpriteRenderer>().sprite = Sprite.Create(finalTexture, new Rect(0, 0, width, height), Vector2.one * 0.5f);
        SaveTextureAsPNG(finalTexture, saveFileName);
    }

    bool[][] InitializeMap()
    {
        Random.InitState(seed);
        
        // Initialize map
        bool[][] map = new bool[width][];
        for (int x = 0; x < width; x++)
        {
            map[x] = new bool[height];
            for (int y = 0; y < height; y++)
            {
                // Fill it with alive cells according to chance
                map[x][y] = Random.value < chanceToStartAlive;
            }
        }
        return map;
    }

    int CountAliveNeighbours(bool[][] map, int x, int y)
    {
        int count = 0;
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                int neighbourX = x + i;
                int neighbourY = y + j;

                // Skip the current cell itself
                if (i == 0 && j == 0) continue;

                // Count cells outside the map as alive (creates solid borders)
                if (neighbourX < 0 || neighbourY < 0 || neighbourX >= width || neighbourY >= height)
                {
                    count++;
                }
                else if (map[neighbourX][neighbourY])
                {
                    count++;
                }
            }
        }
        return count;
    }

    bool[][] DoSimulationStep(bool[][] oldMap)
    {
        //Create new grid map
        bool[][] newMap = new bool[width][];
        for (int x = 0; x < width; x++)
        {
            newMap[x] = new bool[height];
            for (int y = 0; y < height; y++)
            {
                int aliveNeighbours = CountAliveNeighbours(oldMap, x, y);

                // Apply rules based on the modified cellular automata logic
                if (oldMap[x][y])
                {
                    newMap[x][y] = aliveNeighbours >= deathLimit;
                }
                else
                {
                    newMap[x][y] = aliveNeighbours > birthLimit;
                }
            }
        }
        return newMap;
    }

    public Texture2D ConvertMapToTexture(bool[][] map)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                texture.SetPixel(x, y, map[x][y] ? Color.black : Color.white);
            }
        }
        texture.Apply();
        return texture;
    }

    void SaveTextureAsPNG(Texture2D texture, string fileName)
    {
        byte[] bytes = texture.EncodeToPNG();
        string path = Path.Combine(Application.dataPath, fileName);
        File.WriteAllBytes(path, bytes);
        Debug.Log("Saved cave map to: " + path);
    }
}
