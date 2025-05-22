using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class GameVisualManager : NetworkBehaviour
{
    private const float GRID_SIZE = 3.1f;

    [SerializeField] private Transform crossPrefab;
    [SerializeField] private Transform circlePrefab;
    [SerializeField] private Transform lineCompletePrefab;

    private List<GameObject> visualGameObjectList;

    private void Awake()
    {
        visualGameObjectList = new List<GameObject>();
    }

    private void Start()
    {
        GameManager.instance.OnClickedOnGridPosition += GameManager_OnClickedOnGridPosition;
        GameManager.instance.OnGameWin += GameManager_OnGameWin;
        GameManager.instance.OnRematch += GameManager_OnRematch;
    }

    private void GameManager_OnRematch(object sender, System.EventArgs e)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;


        foreach (GameObject go in visualGameObjectList)
        {
            Destroy(go);
        }

        visualGameObjectList.Clear();
    }

    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
        if(!NetworkManager.Singleton.IsServer)
            return;


        float eulerZ = 0f;
        switch(e.line.orientation)
        {
            default:
            case GameManager.Orientation.Vertical:      eulerZ = 0f;    break; 
            case GameManager.Orientation.Horizontal:    eulerZ = 90f;   break; 
            case GameManager.Orientation.DiagonalA:     eulerZ = 45f;   break; 
            case GameManager.Orientation.DiagonalB:     eulerZ = -45f;  break; 
        }

        Transform lineCompleteTransform = Instantiate(lineCompletePrefab, GetGridPosition(e.line.centerGridPosition.x, e.line.centerGridPosition.y), Quaternion.Euler(0, 0, eulerZ));
        lineCompleteTransform.GetComponent<NetworkObject>().Spawn(true);

        visualGameObjectList.Add(lineCompleteTransform.gameObject);
    }

    private void GameManager_OnClickedOnGridPosition(object sender, GameManager.OnClickedOnGridPositionEventArgs e)
    {
        SpawnObjectRpc(e._x, e._y, e._playerType);
    }

    [Rpc(SendTo.Server)]
    private void SpawnObjectRpc(int x, int y, GameManager.PlayerType playerType)
    {
        Transform prefab;
        switch(playerType)
        {
            default:
            case GameManager.PlayerType.Cross:
                prefab = crossPrefab;
                break;
            case GameManager.PlayerType.Circle:
                prefab = circlePrefab;
                break;

        }

        Transform spawnedCrossTransform = Instantiate(prefab, GetGridPosition(x, y), Quaternion.identity);
        spawnedCrossTransform.GetComponent<NetworkObject>().Spawn(true);

        visualGameObjectList.Add(spawnedCrossTransform.gameObject);
    }

    private Vector2 GetGridPosition(int x, int y)
    {
        return new Vector2(-GRID_SIZE + x * GRID_SIZE, -GRID_SIZE + y * GRID_SIZE);
    }
}
