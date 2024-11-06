using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Schema;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(VoronoiMosaic))]
[RequireComponent(typeof(CaveGenerator))]

public class CaveVoronoiMap : MonoBehaviour
{
    
    private VoronoiMosaic voronoiDiagram;
    private CaveGenerator caveGenerator;

    // Treasure parameters
    public int numberOfTreasures = 10;
    public int crossSize = 5;

    // Delay for displaying intermediate steps (in seconds)
    public float stepDelay = 0.2f;
    public string saveFileName = "CaveMosaicMap.png";

    private Texture2D caveTexture;
    private Texture2D mosaicTexture;

    private void Start()
    {
        voronoiDiagram = GetComponent<VoronoiMosaic>();
        caveGenerator = GetComponent<CaveGenerator>();
        StartCoroutine(GenerateCaveVoronoiMap());
    }

    private IEnumerator GenerateCaveVoronoiMap()
    {
        // Step 1: Generate the cave map
        StartCoroutine(caveGenerator.GenerateCave());
        while (caveGenerator.finalTexture == null)
        {
            yield return new WaitForSeconds(stepDelay);
        }
        caveTexture = caveGenerator.finalTexture;
        yield return new WaitForSeconds(stepDelay);

        // Step 2: Generate the Voronoi mosaic based on the cave texture
        voronoiDiagram.originalImage = caveTexture;
        mosaicTexture = voronoiDiagram.GenerateMosaic(new Vector2Int(caveTexture.width, caveTexture.height));
        yield return new WaitForSeconds(stepDelay);

        // Step 3: Find treasure spots and mark them on the mosaic texture
        List<Vector2Int> treasureSpots = FindTreasureSpots(caveTexture);
        foreach (Vector2Int spot in treasureSpots)
        {
            DrawTreasureCross(mosaicTexture, spot, crossSize, Color.red);
        }

        // Apply the updated texture
        mosaicTexture.Apply();

        // Display the final texture as a sprite
        GetComponent<SpriteRenderer>().sprite = Sprite.Create(mosaicTexture, new Rect(0, 0, mosaicTexture.width, mosaicTexture.height), Vector2.one * 0.5f);

        // Save the result to disk
        SaveTextureToDisk(mosaicTexture, saveFileName);
    }

    // Find empty spots suitable for treasures
    private List<Vector2Int> FindTreasureSpots(Texture2D caveMap)
    {
        List<Vector2Int> spots = new List<Vector2Int>();
        int width = caveMap.width;
        int height = caveMap.height;

        for (int x = crossSize; x < width - crossSize; x++)
        {
            for (int y = crossSize; y < height - crossSize; y++)
            {
                if (caveMap.GetPixel(x, y) == Color.black && caveMap.GetPixel(x, y - 1) == Color.white)
                {
                    spots.Add(new Vector2Int(x, y));
                }
            }
        }
        
        List<Vector2Int> chosenSpots = new List<Vector2Int>();
        System.Random rnd = new System.Random();
        
        for (int i = 0; i < numberOfTreasures; i++)
        {
            var chosenSpot = spots[rnd.Next(spots.Count - 1)];
            while (chosenSpots.Contains(chosenSpot))
            {
                chosenSpot = spots[rnd.Next(spots.Count - 1)];
            }
            chosenSpots.Add(chosenSpot);
        }
        return chosenSpots;
    }

    // Draw a cross at the given position
    private void DrawTreasureCross(Texture2D texture, Vector2Int position, int size, Color color)
    {
        for (int i = -size; i <= size; i++)
        {
            texture.SetPixel(position.x + i, position.y + i, color);
            texture.SetPixel(position.x - i, position.y + i, color);
        }
    }

    // Save the texture to disk
    private void SaveTextureToDisk(Texture2D texture, string fileName)
    {
        byte[] bytes = texture.EncodeToPNG();
        string path = Path.Combine(Application.dataPath, fileName);
        File.WriteAllBytes(path, bytes);
    }
}
