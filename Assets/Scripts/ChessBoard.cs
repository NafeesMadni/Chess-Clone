using UnityEngine;

public class ChessBoard : MonoBehaviour
{
    #region Variables
    [SerializeField] private Material whiteTile;
    [SerializeField] private Material blackTile;
    [SerializeField] public Camera curCamera;
    [HideInInspector] public GameObject[,] board;
    #endregion

    private void Awake()
    {
        DrawAllTiles(1f, 8, 8);
        transform.position = new Vector3(-3.3f, -3.3f, 1);
        transform.localScale = new Vector3(0.95f, 0.95f, 1);
    }

    #region Get Tiles Index
    public Vector2Int GetTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y] == hitInfo){
                    Debug.Log(string.Format("{0},{1}", x, y)); 
                    return new Vector2Int(x, y); 
                }
            }
        }
    
        return -Vector2Int.one; // never hit
    }
    #endregion

    #region Drawing Tile
    private void DrawAllTiles(float tileSize, int Rows, int Columns)
    {
        board = new GameObject[Rows, Columns];

        for (int x = 0; x < Rows; x++)
            for (int y = 0; y < Columns; y++)
                board[x, y] = DrawSingleTile(1, x, y);

    }

    private GameObject DrawSingleTile(float tileSize, int x, int y)  
    {
        GameObject tile = new GameObject(string.Format("{0}, {1}", x, y));

        Mesh mesh = new Mesh();
        tile.AddComponent<MeshFilter>().mesh = mesh;

        MeshRenderer renderer = tile.AddComponent<MeshRenderer>();

        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0)
        };

        int[] triangles = new int[]
        {
            0, 2, 1, // First triangle
            2, 3, 1  // Second triangle
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        if ((x + y) % 2 == 0)
            renderer.material = blackTile;
        else
            renderer.material = whiteTile;
        
        tile.transform.position = new Vector3(y, x, this.transform.position.z);

        
        tile.transform.parent = this.transform;

        tile.AddComponent<BoxCollider>();

        return tile;

    }
    #endregion

}
