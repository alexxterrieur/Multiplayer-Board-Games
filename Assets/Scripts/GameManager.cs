using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static GameManager;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance { get; private set; }

    public EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition;
    public class OnClickedOnGridPositionEventArgs : EventArgs
    {
        public int _x;
        public int _y;
        public PlayerType _playerType;
    }

    public EventHandler OnGameStarted;
    public EventHandler OnCurrentPlayablePlayerTypeChanged;

    public EventHandler OnRematch;
    public EventHandler OnGameTied;
    public EventHandler OnScoreChanged;
    public EventHandler<OnGameWinEventArgs> OnGameWin;
    public class OnGameWinEventArgs : EventArgs
    {
        public Line line;
        public PlayerType winPlayerType;
    }


    public enum PlayerType
    {
        None,
        Cross,
        Circle,
    }

    public enum Orientation
    {
        Horizontal,
        Vertical,
        DiagonalA,
        DiagonalB,
    }

    public struct Line
    {
        public List<Vector2Int> gridVector2IntList;
        public Vector2Int centerGridPosition;
        public Orientation orientation;
    }

    private PlayerType localPlayerType;
    private NetworkVariable<PlayerType> currentPlayablePlayerType = new NetworkVariable<PlayerType>();
    private PlayerType[,] playerTypeArray;
    private List<Line> lineList;

    private NetworkVariable<int> playerCrossScrore = new NetworkVariable<int>();
    private NetworkVariable<int> playerCircleScore = new NetworkVariable<int>();

    private void Awake()
    {
        if(instance != null)
        {
            Debug.LogError("More than one GameManager Instance");
        }
        instance = this;
        
        playerTypeArray = new PlayerType[3, 3];

        lineList = new List<Line>
        {
            //Horizontal
            new Line
            {
                gridVector2IntList = new List<Vector2Int> {new Vector2Int(0, 0),new Vector2Int(1, 0), new Vector2Int(2, 0), },
                centerGridPosition = new Vector2Int(1, 0),
                orientation = Orientation.Vertical,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int> {new Vector2Int(0, 1),new Vector2Int(1, 1), new Vector2Int(2, 1), },
                centerGridPosition = new Vector2Int(1, 1),
                orientation = Orientation.Vertical,

            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int> {new Vector2Int(0, 2),new Vector2Int(1, 2), new Vector2Int(2, 2), },
                centerGridPosition = new Vector2Int(1, 2),
                orientation = Orientation.Vertical,
            },

            //Vertical
            new Line
            {
                gridVector2IntList = new List<Vector2Int> {new Vector2Int(0, 0),new Vector2Int(0, 1), new Vector2Int(0, 2), },
                centerGridPosition = new Vector2Int(0, 1),
                orientation = Orientation.Horizontal,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int> {new Vector2Int(1, 0),new Vector2Int(1, 1), new Vector2Int(1, 2), },
                centerGridPosition = new Vector2Int(1, 1),
                orientation = Orientation.Horizontal,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int> {new Vector2Int(2, 0),new Vector2Int(2, 1), new Vector2Int(2, 2), },
                centerGridPosition = new Vector2Int(2, 1),
                orientation = Orientation.Horizontal,
            },

            //Diagonals
            new Line
            {
                gridVector2IntList = new List<Vector2Int> {new Vector2Int(0, 0),new Vector2Int(1, 1), new Vector2Int(2, 2), },
                centerGridPosition = new Vector2Int(1, 1),
                orientation = Orientation.DiagonalA,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int> {new Vector2Int(0, 2),new Vector2Int(1, 1), new Vector2Int(2, 0), },
                centerGridPosition = new Vector2Int(1, 1),
                orientation = Orientation.DiagonalB,
            },
        };
    }

    public override void OnNetworkSpawn()
    {
        if(NetworkManager.Singleton.LocalClientId == 0)
        {
            localPlayerType = PlayerType.Cross;
        }
        else
        {
            localPlayerType = PlayerType.Circle;
        }

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }

        currentPlayablePlayerType.OnValueChanged += (PlayerType oldPlayerType, PlayerType newPlayerType) => {
            OnCurrentPlayablePlayerTypeChanged?.Invoke(this, EventArgs.Empty);
        };

        //Update Score
        playerCrossScrore.OnValueChanged += (int prevScore, int newScore) =>
        {
            OnScoreChanged?.Invoke(this, EventArgs.Empty);
        };

        playerCircleScore.OnValueChanged += (int prevScore, int newScore) =>
        {
            OnScoreChanged?.Invoke(this, EventArgs.Empty);
        };

    }

    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        if(NetworkManager.Singleton.ConnectedClientsList.Count == 2)
        {
            currentPlayablePlayerType.Value = PlayerType.Cross;
            TriggerOnGameStartedRpc();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameStartedRpc()
    {
        OnGameStarted?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.Server)]
    public void ClickedOnGridRpc(int x, int y, PlayerType playerType)
    {
        if(playerType != currentPlayablePlayerType.Value)
            return;

        if (playerTypeArray[x, y] != PlayerType.None)
            return; //already occupied

        playerTypeArray[x, y] = playerType;


        OnClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs { 
            _x = x,
            _y = y,
            _playerType = playerType,
        });

        switch(currentPlayablePlayerType.Value)
        {
            default:
            case PlayerType.Cross:
                currentPlayablePlayerType.Value = PlayerType.Circle;
                break;
            case PlayerType.Circle:
                currentPlayablePlayerType.Value = PlayerType.Cross;
                break;
        }

        TestWinner();
    }


    //win
    private bool TestWinnerLine(Line line)
    {
        return TestWinnerLine(
            playerTypeArray[line.gridVector2IntList[0].x, line.gridVector2IntList[0].y],
            playerTypeArray[line.gridVector2IntList[1].x, line.gridVector2IntList[1].y],
            playerTypeArray[line.gridVector2IntList[2].x, line.gridVector2IntList[2].y]
        );
    }

    private bool TestWinnerLine(PlayerType aPlayerType, PlayerType bPlayerType, PlayerType cPlayerType)
    {
        return
            aPlayerType != PlayerType.None &&
            aPlayerType == bPlayerType &&
            bPlayerType == cPlayerType;
    }

    private void TestWinner()
    {
        for(int i = 0; i < lineList.Count; i++)
        {
            Line line = lineList[i];

            if(TestWinnerLine(line))
            {
                Debug.Log("win");
                currentPlayablePlayerType.Value = PlayerType.None;
                PlayerType winPlayerType = playerTypeArray[line.centerGridPosition.x, line.centerGridPosition.y];

                switch(winPlayerType)
                {
                    case PlayerType.Cross:
                        playerCrossScrore.Value++;
                        break;
                    case PlayerType.Circle:
                        playerCircleScore.Value++;
                        break;
                }

                TriggerOnGameWinRpc(i, winPlayerType);
                return;
            }
        }


        bool isBoardFull = true;
        for (int x = 0; x < playerTypeArray.GetLength(0); x++)
        {
            for (int y = 0; y < playerTypeArray.GetLength(1); y++)
            {
                if (playerTypeArray[x, y] == PlayerType.None)
                {
                    isBoardFull = false;
                    break;
                }
            }
        }

        if (isBoardFull)
        {
            Debug.Log("tie");
            currentPlayablePlayerType.Value = PlayerType.None;
            TriggerOnGameTiedRpc();
        }


    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameTiedRpc()
    {
        OnGameTied?.Invoke(this, EventArgs.Empty);
    }


    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameWinRpc(int lineIndex, PlayerType winPlayerType)
    {
        Line line = lineList[lineIndex];
        OnGameWin?.Invoke(this, new OnGameWinEventArgs
        {
            line = line,
            winPlayerType = winPlayerType,
        });
    }

    [Rpc(SendTo.Server)]
    public void RematchRpc()
    {
        for(int x = 0; x < playerTypeArray.GetLength(0); x++)
        {
            for(int y = 0; y < playerTypeArray.GetLength(1);  y++)
            {
                playerTypeArray[x, y] = PlayerType.None;
            }
        }

        currentPlayablePlayerType.Value = PlayerType.Cross;
        TriggerOnRematchRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnRematchRpc()
    {
        OnRematch?.Invoke(this, EventArgs.Empty);
    }

    public PlayerType GetLocalPlayerType()
    { 
        return localPlayerType; 
    }

    public PlayerType GetCurrentPlayablePlayerType()
    {
        return currentPlayablePlayerType.Value;
    }

    public void GetScores(out int playerCrossScore, out int playerCircleScore)
    {
        playerCrossScore = this.playerCrossScrore.Value;
        playerCircleScore = this.playerCircleScore.Value;
    }
}
