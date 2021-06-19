using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class PathfinderScript : MonoBehaviour
{

    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] Slabs;
    public KMSelectable Reset;
    public TextMesh[] Texts;
    public MeshRenderer Lava;

    private List<string> AnswerList = new List<string> { };
    private List<string> RecievedList = new List<string> { };
    private int[,] UnchangingGrid = new int[4, 4];
    private int[,] Grid = new int[4, 4];
    private int CurrentRow;
    private int CurrentColumn;
    private int PrevSlab = 0;
    private int Facing = 2;
    private int PrevFacing = 2;
    private string[][] GridToString = { new string[4], new string[4], new string[4], new string[4] };
    private bool Pressing;
    private bool Solved;

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        for (int i = 0; i < 16; i++)
        {
            int x = i;
            Slabs[i].OnInteract += delegate { if (!Solved) StartCoroutine(SlabPress(x)); return false; };
        }
        Reset.OnInteract += delegate { if (!Solved) StartCoroutine(SlabPress(16)); return false; };
        Calculate();
        Slabs[0].transform.localPosition -= new Vector3(0, 0.01f, 0);
        Texts[0].color = new Color32(255, 111, 0, 255);
    }

    void FixedUpdate()
    {
        Lava.material.SetTextureOffset("_MainTex", new Vector2(Lava.material.GetTextureOffset("_MainTex").x - 0.0002f, Lava.material.GetTextureOffset("_MainTex").y + 0.0002f));
    }

    void Calculate()
    {
        int Direction = 1;
        bool Continue;
        CalcSteps:
        {
            AnswerList = new List<string> { };
            for (int i = 0; i < 30; i++)
            {
                Continue = false;
                while (!Continue)
                {
                    int Movement = Rnd.Range(0, 4);
                    if (Movement == 0 && CurrentRow != 0 && Direction != 2)
                    {
                        CurrentRow--;
                        AnswerList.Add("u");
                        Grid[CurrentRow, CurrentColumn]++;
                        UnchangingGrid[CurrentRow, CurrentColumn]++;
                        Direction = Movement;
                        Continue = true;
                    }
                    else if (Movement == 1 && CurrentColumn != 3 && Direction != 3)
                    {
                        CurrentColumn++;
                        AnswerList.Add("r");
                        Grid[CurrentRow, CurrentColumn]++;
                        UnchangingGrid[CurrentRow, CurrentColumn]++;
                        Direction = Movement;
                        Continue = true;
                    }
                    else if (Movement == 2 && CurrentRow != 3 && Direction != 0)
                    {
                        CurrentRow++;
                        AnswerList.Add("d");
                        Grid[CurrentRow, CurrentColumn]++;
                        UnchangingGrid[CurrentRow, CurrentColumn]++;
                        Direction = Movement;
                        Continue = true;
                    }
                    else if (Movement == 3 && CurrentColumn != 0 && Direction != 1)
                    {
                        CurrentColumn--;
                        AnswerList.Add("l");
                        Grid[CurrentRow, CurrentColumn]++;
                        UnchangingGrid[CurrentRow, CurrentColumn]++;
                        Direction = Movement;
                        Continue = true;
                    }
                }
            }
            if (CurrentRow != 3 || CurrentColumn != 3)
            {
                Grid = new int[4, 4];
                UnchangingGrid = new int[4, 4];
                CurrentRow = 0;
                CurrentColumn = 0;
                goto CalcSteps;
            }
            for (int i = 0; i < 16; i++)
                GridToString[Mathf.FloorToInt(i / 4f)][i % 4] = Grid[Mathf.FloorToInt(i / 4f), i % 4].ToString();
            Debug.LogFormat("[Pathfinder #{0}] The grid:\n{1}", _moduleID, GridToString[0].Join() + "\n" + GridToString[1].Join() + "\n" + GridToString[2].Join() + "\n" + GridToString[3].Join());
            Debug.LogFormat("[Pathfinder #{0}] One possible solution: \"{1}\".", _moduleID, AnswerList.Join(", "));
            for (int i = 0; i < 16; i++)
            {
                Texts[i].text = Grid[Mathf.FloorToInt(i / 4f), i % 4].ToString();
                if (Texts[i].text == "0" && i != 0)
                    Slabs[i].transform.localScale = new Vector3();
            }
        }
    }

    private IEnumerator SlabPress(int pos)
    {
        if (pos == 16)
        {
            for (int i = 0; i < 5; i++)
            {
                Reset.transform.localPosition -= new Vector3(0, 0.01f / 5, 0);
                yield return null;
            }
            bool[] Zeroes = new bool[16];
            for (int i = 0; i < 16; i++)
            {
                if (Grid[Mathf.FloorToInt(i / 4), i % 4] == 0 && i != PrevSlab)
                    Zeroes[i] = true;
            }
            for (int i = 0; i < 16; i++)
            {
                Texts[i].text = UnchangingGrid[Mathf.FloorToInt(i / 4), i % 4].ToString();
                Grid[Mathf.FloorToInt(i / 4), i % 4] = UnchangingGrid[Mathf.FloorToInt(i / 4), i % 4];
            }
            Texts[PrevSlab].color = new Color32(255, 58, 0, 255);
            int PrevSlabCache = PrevSlab;
            PrevSlab = 0;
            Texts[0].color = new Color32(255, 111, 0, 255);
            Facing = 2;
            PrevFacing = 2;
            RecievedList = new List<string> { };
            for (int i = 0; i < 5; i++)
            {
                Reset.transform.localPosition += new Vector3(0, 0.01f / 5, 0);
                Slabs[0].transform.localPosition -= new Vector3(0, 0.01f / 5, 0);
                Slabs[PrevSlabCache].transform.localPosition += new Vector3(0, 0.01f / 5, 0);
                for (int j = 0; j < 16; j++)
                {
                    if (Zeroes[j])
                    {
                        Slabs[j].transform.localPosition += new Vector3(0, 0.05f / 5, 0);
                    }
                }
                yield return null;
            }
        }
        else
        {
            if ((PrevSlab == pos + 4 || (PrevSlab == pos - 1 && PrevSlab % 4 != 3) || PrevSlab == pos - 4 || (PrevSlab == pos + 1 && PrevSlab % 4 != 0)) && !Pressing && Grid[Mathf.FloorToInt(pos / 4), pos % 4] != 0)
            {
                string MovementCache = "";
                if (PrevSlab == pos + 4)
                {
                    Facing = 0;
                    MovementCache = "u";
                }
                else if (PrevSlab == pos - 1 && pos % 4 != 4)
                {
                    Facing = 1;
                    MovementCache = "r";
                }
                else if (PrevSlab == pos - 4)
                {
                    Facing = 2;
                    MovementCache = "d";
                }
                else
                {
                    Facing = 3;
                    MovementCache = "l";
                }
                if ((PrevFacing + 2) % 4 == Facing)
                    Facing = PrevFacing;
                else
                {
                    RecievedList.Add(MovementCache);
                    PrevFacing = Facing;
                    Pressing = true;
                    Audio.PlaySoundAtTransform("footstep", Slabs[pos].transform);
                    Grid[Mathf.FloorToInt(pos / 4), pos % 4]--;
                    Texts[pos].text = Grid[Mathf.FloorToInt(pos / 4), pos % 4].ToString();
                    Texts[pos].color = new Color32(255, 111, 0, 255);
                    Texts[PrevSlab].color = new Color32(255, 58, 0, 255);
                    for (int i = 0; i < 5; i++)
                    {
                        Slabs[pos].transform.localPosition -= new Vector3(0, 0.01f / 5, 0);
                        if (Grid[Mathf.FloorToInt(PrevSlab / 4), PrevSlab % 4] != 0)
                            Slabs[PrevSlab].transform.localPosition += new Vector3(0, 0.01f / 5, 0);
                        else
                            Slabs[PrevSlab].transform.localPosition -= new Vector3(0, 0.02f / 5, 0);
                        yield return null;
                    }
                    if (Grid[Mathf.FloorToInt(PrevSlab / 4), PrevSlab % 4] == 0)
                        StartCoroutine(SlabSink(PrevSlab));
                    PrevSlab = pos;
                    bool Solve = true;
                    for (int i = 0; i < 16; i++)
                    {
                        if (Grid[Mathf.FloorToInt(i / 4), i % 4] != 0)
                            Solve = false;
                    }
                    if (Solve && pos == 15)
                    {
                        Module.HandlePass();
                        Debug.LogFormat("[Pathfinder #{0}] You gave a correct solution: \"{1}\". Module solved!", _moduleID, RecievedList.Join(", "));
                        Solved = true;
                        for (int i = 0; i < 5; i++)
                        {
                            Slabs[15].transform.localPosition -= new Vector3(0, 0.02f / 5, 0);
                            yield return null;
                        }
                        Audio.PlaySoundAtTransform("solve", Slabs[15].transform);
                        StartCoroutine(SlabSink(15));
                    }
                }
            }
        }
        Pressing = false;
        yield return null;
    }

    private IEnumerator SlabSink(int pos)
    {
        for (int i = 0; i < 120; i++)
        {
            Slabs[pos].transform.localPosition -= new Vector3(0, 0.02f / 120, 0);
            yield return null;
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} l u r d' to move left, then up, then right, then down. Use '!{0} reset' to press the reset pillar.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        string[] CommandArray = command.Split(' ');
        string[] ValidCommands = { "u", "r", "d", "l" };
        yield return null;
        for (int i = 0; i < CommandArray.Length; i++)
        {
            if (!ValidCommands.Contains(CommandArray[i]) && command != "reset")
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
            else if (command == "reset")
                Reset.OnInteract();
            else
            {
                if (CommandArray[i] == "u" && PrevSlab > 3)
                    if (Texts[PrevSlab - 4].text != "0")
                        Slabs[PrevSlab - 4].OnInteract();
                    else
                    {
                        yield return "sendtochaterror Cannot move there.";
                        yield break;
                    }
                else if (CommandArray[i] == "r" && PrevSlab % 4 != 3)
                    if (Texts[PrevSlab + 1].text != "0")
                        Slabs[PrevSlab + 1].OnInteract();
                    else
                    {
                        yield return "sendtochaterror Cannot move there.";
                        yield break;
                    }
                else if (CommandArray[i] == "d" && PrevSlab < 12)
                    if (Texts[PrevSlab + 4].text != "0")
                        Slabs[PrevSlab + 4].OnInteract();
                    else
                    {
                        yield return "sendtochaterror Cannot move there.";
                        yield break;
                    }
                else if (CommandArray[i] == "l" && PrevSlab % 4 != 0)
                    if (Texts[PrevSlab - 1].text != "0")
                        Slabs[PrevSlab - 1].OnInteract();
                    else
                    {
                        yield return "sendtochaterror Cannot move there.";
                        yield break;
                    }
                else
                {
                    yield return "sendtochaterror Cannot move there.";
                    yield break;
                }
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        for (int i = 0; i < 30; i++)
        {
            yield return true;
            switch (AnswerList[i])
            {
                case "u":
                    Slabs[PrevSlab - 4].OnInteract();
                    break;
                case "r":
                    Slabs[PrevSlab + 1].OnInteract();
                    break;
                case "d":
                    Slabs[PrevSlab + 4].OnInteract();
                    break;
                default:
                    Slabs[PrevSlab - 1].OnInteract();
                    break;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
}
