using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
public class PattemEditor : EditorWindow
{
    public Bricks_PattemTableObj pattemTableObj;
    private static PattemEditor window;
    PattemInfor[] listPattemsInfor;
    LevelData[] levelData;
    Vector2 scrollViewVector;
    int scrollHeightArea;

    int scrollHeight = 300;
    int scrollWidth = 250;

    int space = 12;
    bool showPattem = false;

    [MenuItem("Tools/Sonat/Pattem Editor")]
    public static void Init()
    {
        // Get existing open window or if none, make a new one:
        window = (PattemEditor) EditorWindow.GetWindow(typeof(PattemEditor));
        window.Show();
//        window.Close();
    }

    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(PattemEditor));
    }

    void OnSelectionChange()
    {
        if (Selection.objects.Length > 0)
        {
            Debug.Log(Selection.objects[0]);
            if (Selection.objects[0] is Bricks_PattemTableObj)
            {

                pattemTableObj = Selection.objects[0] as Bricks_PattemTableObj;

                listPattemsInfor = (PattemInfor[]) pattemTableObj.listPattemsInfor.Clone();
                levelData = pattemTableObj.levelData;

                for (int i = 0; i < listPattemsInfor.Length; i++)
                {
                    if (listPattemsInfor[i].grid == null || listPattemsInfor[i].grid.Length == 0)
                    {
                        listPattemsInfor[i].grid = new bool[25];
                    }
                }


                UpdateScrollHeight();
            }
        }

        this.Repaint();
    }


    private void Apply()
    {
        if (Application.isPlaying)
        {
            Debug.Log("Editer is running - Stop and try again");
            return;
        }

        pattemTableObj.listPattemsInfor = listPattemsInfor;
        pattemTableObj.levelData = levelData;
        EditorUtility.SetDirty(pattemTableObj);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();

        Debug.Log("Apply Success! ");
    }

//    void Initialize()
//    {
//        string objectPath = "Assets/_Bricks/Scripts/Editor/PattemData.asset";
//        pattemTableObj = AssetDatabase.LoadAssetAtPath(objectPath, typeof(Bricks_PattemTableObj)) as Bricks_PattemTableObj;
//
//        listPattemsInfor = (PattemInfor[])pattemTableObj.listPattemsInfor.Clone();
//        levelData = pattemTableObj.levelData;
//
//        for (int i = 0; i < listPattemsInfor.Length; i++)
//        {
//            if (listPattemsInfor[i].grid == null || listPattemsInfor[i].grid.Length == 0)
//            {
//                listPattemsInfor[i].grid = new bool[25];
//            }
//        }
//
//
//
//        UpdateScrollHeight();
//    }

    void UpdateScrollHeight()
    {
        scrollHeightArea = Mathf.FloorToInt(listPattemsInfor.Length / 4) * (scrollHeight + space) + 390;
    }

    int x = 0;
    int y = 0;

    void OnGUI()
    {
        EditorGUIUtility.labelWidth = 90;

        if (listPattemsInfor == null)
        {
            GUILayout.Label("Select a PatternInfo");
        }
        else
        {
            x = 0;
            y = 0;

            GUILayout.BeginArea(new Rect(300, 15, 150, 30));
            GUILayout.Label("Total Item : " + listPattemsInfor.Length);
            GUILayout.EndArea();


            GUILayout.BeginHorizontal();


            if (GUILayout.Button("Apply", new GUILayoutOption[] {GUILayout.Width(50), GUILayout.Height(30)}))
            {
                Apply();
            }

            showPattem = EditorGUILayout.Toggle("Show Pattem", showPattem);

            GUILayout.EndHorizontal();

            if (listPattemsInfor.Length == 0)
            {
                return;
            }

            scrollViewVector = GUI.BeginScrollView(new Rect(25, 40, position.width - 30, position.height),
                scrollViewVector, new Rect(0, 0, 400, scrollHeightArea + 2100));

            GUILayout.BeginVertical();
            //return;


            for (int i = 0; i < listPattemsInfor.Length; i++)
            {
                if (!showPattem)
                {
                    continue;
                }

                if (i == 0)
                {
                    GUILayout.BeginHorizontal();
                }

                GUILayout.BeginArea(
                    new Rect(x * (scrollWidth + space), 10 + y * (scrollHeight + space), scrollWidth, scrollHeight),
                    GUI.skin.button);
                x++;

                GUILayout.BeginHorizontal();
                GUILayout.Label(listPattemsInfor[i].type.ToString(), EditorStyles.boldLabel,
                    new GUILayoutOption[] {GUILayout.Width(150)});
                GUILayout.EndHorizontal();

                listPattemsInfor[i].type = (Types) EditorGUILayout.EnumPopup("", listPattemsInfor[i].type,
                    new GUILayoutOption[] {GUILayout.Width(70)});

                if (listPattemsInfor[i].grid == null || listPattemsInfor[i].grid.Length == 0)
                {
                    listPattemsInfor[i].grid = new bool[25];
                }

                GUILayout.BeginArea(new Rect(25, 60, 200, 200));

                for (int j = 0; j < 25; j++)
                {
                    if (j == 0 || j == 5 || j == 10 || j == 15 || j == 20)
                    {
                        GUILayout.BeginHorizontal();
                    }

                    GUI.color = listPattemsInfor[i].grid[j] ? Color.green : Color.black;
                    if (GUILayout.Button("", GUILayout.Width(37), GUILayout.Height(37)))
                    {
                        listPattemsInfor[i].grid[j] = !listPattemsInfor[i].grid[j];
                    }

                    if (j == 4 || j == 9 || j == 14 || j == 19 || j == 24)
                    {
                        GUILayout.EndHorizontal();
                    }
                }


                GUILayout.EndArea();


                GUI.color = Color.white;


                GUILayout.BeginArea(new Rect(3, 280, 60, 50));
                if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    Remove(i);
                }

                GUILayout.EndArea();

                GUILayout.EndArea();

                if (i != 0 && (i + 1) % 4 == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(space);
                    x = 0;
                    y++;
                }
            }

            int h = Mathf.FloorToInt(listPattemsInfor.Length / 4);
            float addPosX = (listPattemsInfor.Length - h * 4) * (scrollWidth + space);
            float addPosY = 10 + h * (scrollHeight + space);

            GUILayout.BeginArea(new Rect(addPosX, addPosY, scrollWidth, scrollHeight));
            if (GUILayout.Button("Add New Item", GUILayout.Width(scrollWidth), GUILayout.Height(scrollHeight)))
            {
                Add();
            }

            GUILayout.EndArea();


            GUILayout.EndVertical();


            GUILayout.BeginArea(new Rect(0, 2300, 1500, 2500));

            GUILayout.BeginVertical();

            y = 0;
            for (int i = 0; i < listPattemsInfor.Length; i++)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label(listPattemsInfor[i].type.ToString(), EditorStyles.boldLabel,
                    new GUILayoutOption[] {GUILayout.Width(50)});

                GUILayout.BeginVertical();
                for (int j = 0; j < 25; j++)
                {
                    if (j == 0 || j == 5 || j == 10 || j == 15 || j == 20)
                    {
                        GUILayout.BeginHorizontal();
                    }

                    GUI.color = listPattemsInfor[i].grid[j] ? Color.green : Color.clear;
                    GUILayout.Box("",GUILayout.Width(9), GUILayout.Height(9));

                    if (j == 4 || j == 9 || j == 14 || j == 19 || j == 24)
                    {
                        GUILayout.EndHorizontal();
                    }
                }

                GUI.color = Color.white;
                GUILayout.EndVertical();

                GUILayout.Space(20);

                x = 0;


                GUILayout.BeginHorizontal();
                for (int j = 0; j < 9; j++)
                {
                    if (levelData[j].weight == null || levelData[j].weight.Length < listPattemsInfor.Length)
                    {
                        levelData[j].weight = new int[listPattemsInfor.Length];
                    }


                    levelData[j].weight[i] =
                        EditorGUI.IntSlider(new Rect(x + 150, y + 20, 115, 15), levelData[j].weight[i], 0, 12);
                    x += 125;
                }

                GUILayout.EndHorizontal();


                GUILayout.EndHorizontal();
                GUILayout.Space(12);
                y += 73;

                GUI.color = Color.white;
            }

            GUILayout.BeginArea(new Rect(150, 0, 1500, 50));
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 9; i++)
            {
                levelData[i].Score =
                    EditorGUILayout.IntField("", levelData[i].Score, new GUILayoutOption[] {GUILayout.Width(100)});
                GUILayout.Space(21);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            GUILayout.EndVertical();

            GUILayout.EndArea();


            GUI.EndScrollView();
        }
    }
    
    void DrawBgTile(Rect rect, Color bgColor)
    {
        GUI.color = bgColor;
        GUI.DrawTexture(rect, Bgtexture2D, ScaleMode.StretchToFill);
    }
    private Texture2D Bgtexture2D => SquareTexture;
    static Texture2D _squareTexture;
    public static Texture2D SquareTexture
    {
        get
        {
            if (_squareTexture == null)
            {
                _squareTexture = new Texture2D(1, 1, TextureFormat.ARGB4444, false);
                _squareTexture.SetPixel(0, 0, Color.white);
                _squareTexture.Apply();
            }

            return _squareTexture;
        }
    }
    void Remove(int index)
    {
        PattemInfor[] pattemInforSave = (PattemInfor[]) listPattemsInfor.Clone();
        int size = listPattemsInfor.Length;
        listPattemsInfor = new PattemInfor[size - 1];

        int t = 0;
        for (int i = 0; i < size - 1; i++)
        {
            if (i == index)
            {
                t = 1;
            }

            listPattemsInfor[i] = pattemInforSave[i + t];
        }

        Debug.Log("Remove Item : " + index);
    }

    void Add()
    {
        PattemInfor[] pattemInforSave = (PattemInfor[]) listPattemsInfor.Clone();
        int size = listPattemsInfor.Length;
        listPattemsInfor = new PattemInfor[size + 1];

        for (int i = 0; i < size; i++)
        {
            listPattemsInfor[i] = pattemInforSave[i];
        }

        PattemInfor t = new PattemInfor();

        t.grid = new bool[25];
        UpdateScrollHeight();
    }
}
#endif