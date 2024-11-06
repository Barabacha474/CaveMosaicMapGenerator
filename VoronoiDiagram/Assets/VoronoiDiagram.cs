using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]

public class VoronoiMosaic : MonoBehaviour
{
    public bool generateFromStart = true;
    public int regionAmount;
    public int seed;
    public Texture2D originalImage;
    public bool useMeanColor = true;
    public string saveFileName = "VoronoiMosaicResult.png";

    private void Start()
    {
        if (generateFromStart)
        {
            if (originalImage == null)
            {
                Debug.LogError("Original Image not assigned!");
                return;
            }
            
            Vector2Int imageDim = new Vector2Int(originalImage.width, originalImage.height);
            GetComponent<SpriteRenderer>().sprite = Sprite.Create(GenerateMosaic(imageDim), new Rect(0, 0, imageDim.x, imageDim.y), Vector2.one * 0.5f);
        }
        
    }

    
    public Texture2D GenerateMosaic(Vector2Int imageDim)
    {
        // fool protection   
        if (originalImage == null)
        {
            Debug.LogError("Original Image not assigned!");
            return null;
        }
        
        Random.InitState(seed);
        
        // Generate centroids for Voronoi regions
        Vector2Int[] centroids = new Vector2Int[regionAmount];
        for (int i = 0; i < regionAmount; i++)
        {
            centroids[i] = new Vector2Int(Random.Range(0, imageDim.x), Random.Range(0, imageDim.y));
        }

        // Array to store region index for each pixel
        int[] closestCentroids = new int[imageDim.x * imageDim.y];
        for (int x = 0; x < imageDim.x; x++)
        {
            for (int y = 0; y < imageDim.y; y++)
            {
                int index = x + y * imageDim.x;
                closestCentroids[index] = GetClosestCentroidIndex(new Vector2Int(x, y), centroids);
            }
        }

        // Collect colors for each region based on closest centroids
        List<Color>[] regionColors = new List<Color>[regionAmount];
        for (int i = 0; i < regionAmount; i++)
        {
            regionColors[i] = new List<Color>();
        }

        // Fill regionColors with colors from original image pixels
        for (int x = 0; x < imageDim.x; x++)
        {
            for (int y = 0; y < imageDim.y; y++)
            {
                int index = x + y * imageDim.x;
                int closestCentroidIndex = closestCentroids[index];
                regionColors[closestCentroidIndex].Add(originalImage.GetPixel(x, y));
            }
        }

        // Determine fill color for each region
        Color[] regionFillColors = new Color[regionAmount];
        for (int i = 0; i < regionAmount; i++)
        {
            regionFillColors[i] = useMeanColor ? GetMeanColor(regionColors[i]) : GetModeColor(regionColors[i]);
        }

        // Create pixel array for the final texture
        Color[] pixelColors = new Color[imageDim.x * imageDim.y];
        for (int x = 0; x < imageDim.x; x++)
        {
            for (int y = 0; y < imageDim.y; y++)
            {
                int index = x + y * imageDim.x;
                int closestCentroidIndex = closestCentroids[index];
                pixelColors[index] = regionFillColors[closestCentroidIndex];
            }
        }

        // Create and save the texture
        Texture2D finalTexture = GetImageFromColorArray(pixelColors, imageDim);
        SaveTextureAsPNG(finalTexture, saveFileName);

        return finalTexture;
    }

    int GetClosestCentroidIndex(Vector2Int pixelPos, Vector2Int[] centroids)
    {
        float smallestDist = float.MaxValue;
        int index = 0;
        for (int i = 0; i < centroids.Length; i++)
        {
            float dist = Vector2.Distance(pixelPos, centroids[i]);
            if (dist < smallestDist)
            {
                smallestDist = dist;
                index = i;
            }
        }
        return index;
    }

    Color GetMeanColor(List<Color> colors)
    {
        float r = 0, g = 0, b = 0;
        foreach (Color color in colors)
        {
            r += color.r;
            g += color.g;
            b += color.b;
        }
        int colorCount = colors.Count;
        return new Color(r / colorCount, g / colorCount, b / colorCount, 1f);
    }

    Color GetModeColor(List<Color> colors)
    {
        Dictionary<Color, int> colorFrequency = new Dictionary<Color, int>();
        foreach (Color color in colors)
        {
            if (colorFrequency.ContainsKey(color))
                colorFrequency[color]++;
            else
                colorFrequency[color] = 1;
        }

        Color modeColor = Color.black;
        int maxCount = 0;
        foreach (var kvp in colorFrequency)
        {
            if (kvp.Value > maxCount)
            {
                maxCount = kvp.Value;
                modeColor = kvp.Key;
            }
        }
        return modeColor;
    }

    Texture2D GetImageFromColorArray(Color[] pixelColors, Vector2Int imageDim)
    {
        Texture2D tex = new Texture2D(imageDim.x, imageDim.y);
        tex.filterMode = FilterMode.Point;
        tex.SetPixels(pixelColors);
        tex.Apply();
        return tex;
    }

    void SaveTextureAsPNG(Texture2D texture, string fileName)
    {
        byte[] bytes = texture.EncodeToPNG();
        string path = Path.Combine(Application.dataPath, fileName);
        File.WriteAllBytes(path, bytes);
        Debug.Log("Saved Voronoi mosaic to: " + path);
    }
}
