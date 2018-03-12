using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CheckersBoard : MonoBehaviour
{
    public static CheckersBoard Instance { set; get; }

    public Piece[,] pieces = new Piece[8, 8];
    public GameObject whitePiecePrefab;
    public GameObject blackPiecePrefab;

    private bool gameIsOver;
    private float winTime;

    private Vector3 boardOffset = new Vector3(-4.0f, 0, -4.0f);
    private Vector3 pieceOffset = new Vector3(0.5f, 0.125f, 0.5f);

    public bool isRed;
    private bool isRedTurn;

    private Piece selectedPiece;

    private Vector2 mouseOver;
    private Vector2 startDrag;
    private Vector2 endDrag;

    private Client client;

    public Vector2 getStartDrag()
    {
        return startDrag;
    }

    public Vector2 getEndDrag()
    {
        return endDrag;
    }

    public Piece getSelectedPiece()
    {
        return selectedPiece;
    }

    private void Start()
    {
        Instance = this;
        client = FindObjectOfType<Client>();

        if (client)
			isRed = client.isHost;

		isRedTurn = true;
        GenerateBoard();
    }

    private void Update()
    {
        if (gameIsOver)
        {
            if (Time.time - winTime > 3.0f)
            {
                Server server = FindObjectOfType<Server>();
                Client client = FindObjectOfType<Client>();

                if (server)
                    Destroy(server.gameObject);

                if (client)
                    Destroy(client.gameObject);

                SceneManager.LoadScene("Menu");
            }

            return;
        }
			
        UpdateMouseOver();

		if((isRed)?isRedTurn:!isRedTurn)
        {
            int x = (int)mouseOver.x;
            int y = (int)mouseOver.y;

            if (selectedPiece != null)
                UpdatePieceDrag(selectedPiece);

            if (Input.GetMouseButtonDown(0))
                SelectPiece(x, y);

            if (Input.GetMouseButtonUp(0))
                TryMove((int)startDrag.x, (int)startDrag.y, x, y);
        }
    }

    private void UpdateMouseOver()
    {
        if (!Camera.main)
        {
            Debug.Log("Unable to find main camera");
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board")))
        {
            mouseOver.x = (int)(hit.point.x - boardOffset.x);
            mouseOver.y = (int)(hit.point.z - boardOffset.z);
        }
        else
        {
            mouseOver.x = -1;
            mouseOver.y = -1;
        }
    }

    private void UpdatePieceDrag(Piece p)
    {
        if (!Camera.main)
        {
            Debug.Log("Unable to find main camera");
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board")))
        {
            p.transform.position = hit.point + Vector3.up;
        }
    }

    private void SelectPiece(int x, int y)
    {
        // Out of bounds
        if (x < 0 || x >= 8 || y < 0 || y >= 8)
            return;

        Piece p = pieces[x, y];
		if (p != null && p.isRed == isRed)
        {
			selectedPiece = p;
			startDrag = mouseOver;
        }
    }

    public void TryMove(int x1, int y1,int x2,int y2)
    {
        // Multiplayer Support
        startDrag = new Vector2(x1, y1);
        endDrag = new Vector2(x2, y2);
        selectedPiece = pieces[x1, y1];

        // Out of bounds
        if (x2 < 0 || x2 >= 8 || y2 < 0 || y2 >= 8)
        {
            if (selectedPiece != null)
                MovePiece(selectedPiece, x1, y1);

            startDrag = Vector2.zero;
            selectedPiece = null;
            return;
        }

        if (selectedPiece != null)
        {
            // If it has not moved
            if (endDrag == startDrag)
            {
                MovePiece(selectedPiece, x1, y1);
                startDrag = Vector2.zero;
                selectedPiece = null;
                return;
            }

            // Check if its a valid move
            if (selectedPiece.ValidMove(pieces, x1, y1, x2, y2))
            {
                // Did we kill anything
                // If this is a jump
                if (Mathf.Abs(x2 - x1) == 2)
                {
                    Piece p = pieces[(x1 + x2) / 2, (y1 + y2) / 2];
                    if (p != null)
                    {
                        pieces[(x1 + x2) / 2, (y1 + y2) / 2] = null;
                        DestroyImmediate(p.gameObject);
                    }
                }

                pieces[x2, y2] = selectedPiece;
                pieces[x1, y1] = null;
                MovePiece(selectedPiece, x2, y2);

                EndTurn();
            }
            else
            {
                MovePiece(selectedPiece, x1, y1);
                startDrag = Vector2.zero;
                selectedPiece = null;
                return;
            }
        }
    }

    private void EndTurn()
    {
        int x = (int)endDrag.x;
        int y = (int)endDrag.y;

        // Promotions
        if (selectedPiece != null)
        {
			if (selectedPiece.isRed && !selectedPiece.isKing && y == 7)
            {
                selectedPiece.isKing = true;
                selectedPiece.transform.Rotate(Vector3.right * 180);
            }
			else if (!selectedPiece.isRed && !selectedPiece.isKing && y == 0)
            {
                selectedPiece.isKing = true;
                selectedPiece.transform.Rotate(Vector3.right * 180);
            }
        }

        if(client)
        {
            string msg = "CMOV|";
            msg += startDrag.x.ToString() + "|";
            msg += startDrag.y.ToString() + "|";
            msg += endDrag.x.ToString() + "|";
            msg += endDrag.y.ToString();

            client.Send(msg);
        }

        selectedPiece = null;
        startDrag = Vector2.zero;
		isRedTurn = !isRedTurn;
        CheckVictory();
    }

    private void CheckVictory()
    {
        var ps = FindObjectsOfType<Piece>();
        bool red = false, black = false;
        for (int i = 0; i < ps.Length; i++)
        {
			if (ps[i].isRed)
                red = true;
            else
                black = true;
        }

		if (!red || !black) 
		{
			winTime = Time.time;
			gameIsOver = true;
		}
    }

    private void GenerateBoard()
    {
        // Generate White team
        for (int y = 0; y < 3; y++)
        {
            bool oddRow = (y % 2 == 0);
            for (int x = 0; x < 8; x += 2)
            {
                // Generate our Piece
                GeneratePiece((oddRow) ? x : x + 1, y);
            }
        }

        // Generate Black team
        for (int y = 7; y > 4; y--)
        {
            bool oddRow = (y % 2 == 0);
            for (int x = 0; x < 8; x += 2)
            {
                // Generate our Piece
                GeneratePiece((oddRow) ? x : x + 1, y);
            }
        }
    }

    private void GeneratePiece(int x, int y)
    {
        bool red = (y > 3) ? false : true;
        GameObject go = Instantiate((red)?whitePiecePrefab:blackPiecePrefab) as GameObject;
        go.transform.SetParent(transform);
        Piece p = go.GetComponent<Piece>();
        pieces[x, y] = p;
        MovePiece(p, x, y);
    }

    private void MovePiece(Piece p, int x, int y)
    {
        p.transform.position = (Vector3.right * x) + (Vector3.forward * y) + boardOffset + pieceOffset;
    }   
}
