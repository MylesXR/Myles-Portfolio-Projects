using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameObjectArray
{
    public GameObject[] PuzzlePieces;
}

public class ListManager : MonoBehaviour
{
    public List<GameObjectArray> PuzzleGroups;

    private void Spawn()
    {
        // On Start, loop through all the GameObjects in all the arrays and set them inactive.
        foreach (GameObjectArray array in PuzzleGroups)
        {
            foreach (GameObject obj in array.PuzzlePieces)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }
    }

    // Method to activate all GameObjects in a specific array
    public void ActivatePuzzleGroup(int index)
    {
        // Check if the index is valid
        if (index < 0 || index >= PuzzleGroups.Count)
        {
            Debug.LogWarning("Invalid array index.");
            return;
        }

        // Loop through the specified array and set all GameObjects to active.
        foreach (GameObject obj in PuzzleGroups[index].PuzzlePieces)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
    }
}
