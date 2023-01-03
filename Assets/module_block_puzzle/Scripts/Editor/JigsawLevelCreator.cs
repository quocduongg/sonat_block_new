#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorPrefs;
using Event = UnityEngine.Event;


namespace BlockPuzzle
{

    public class JigsawLevelCreator : EditorWindow
    {
        [MenuItem("Tools/DuongPham/" + nameof(JigsawLevelCreator))]
        public static void ShowWindow()
        {
            GetWindow(typeof(JigsawLevelCreator));
        }

        private Vector2 _scrollPosition;

        private string path => "Assets/module_block_puzzle/jigsaw_collection.json";

        int _currentColor = 0;
        private EditorAssets _editorAssets;
        Rect _lastRect;

        int _currentLevel = 0;
        private JigsawCollection _levelCollection;
        private Vector3Int _defaultSize;
        private JigsawLevel _tempLevel;
        private Texture2D _jigsawTex;

        public Vector3Int DefaultSize
        {
            get
            {
                if (_defaultSize.x <= 0 || _defaultSize.y <= 0)
                {
                    if (HasKey(nameof(DefaultSize)))
                        _defaultSize = JsonUtility.FromJson<Vector3Int>(GetString(nameof(DefaultSize)));
                    else
                        _defaultSize = new Vector3Int(10, 10, 10);
                }

                return _defaultSize;
            }
            set
            {
                _defaultSize = value;
                SetString(nameof(DefaultSize), JsonUtility.ToJson(_defaultSize));
            }
        }

        void Load()
        {
            _currentLevel = Mathf.Clamp(_currentLevel, 0, _levelCollection.levels.Count);
            if (_levelCollection.levels.Count == _currentLevel)
            {
                _tempLevel = new JigsawLevel();
                _jigsawTex = Resources.Load<Texture2D>(_tempLevel.file);
                Repaint();
                return;
            }

            _tempLevel = _levelCollection.levels[_currentLevel].Clone();
            _jigsawTex = Resources.Load<Texture2D>(_tempLevel.file);
            Repaint();
        }


        public void OnGUI()
        {
            if (ReferenceEquals(_editorAssets, null))
                _editorAssets = Resources.Load<EditorAssets>("EditorAssets");

            Event current = Event.current;
            var color = GUI.backgroundColor;
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (GUILayout.Button("Test"))
            {
                var data = DuongExtensions.LoadTextAtPath(path);
                _levelCollection = JsonUtility.FromJson<JigsawCollection>(data);
                if (_levelCollection == null)
                    _levelCollection = new JigsawCollection();
                
                for (var i = 0; i < _levelCollection.levels.Count; i++)
                {
                    _levelCollection.levels[i].file = (i + 1)+"a";
                    _levelCollection.levels[i].fileBg = (i + 1)+"";
                    _levelCollection.levels[i].size = new Vector3Int(10, 10, 10);
                }
                File.WriteAllText(path, JsonUtility.ToJson(_levelCollection));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            
            if (GUILayout.Button("Test2"))
            {
                var data = DuongExtensions.LoadTextAtPath(path);
                _levelCollection = JsonUtility.FromJson<JigsawCollection>(data);
                if (_levelCollection == null)
                    _levelCollection = new JigsawCollection();
                
                for (var i = 6; i < _levelCollection.levels.Count; i++)
                {
                    _levelCollection.levels[i].parts2 = new List<ListPoint>();
                }
                File.WriteAllText(path, JsonUtility.ToJson(_levelCollection));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            
            if (current.keyCode == KeyCode.LeftArrow && current.type == EventType.KeyDown)
            {
                if (_currentColor > 0)
                    _currentColor--;
                this.Repaint();
                
            }

            if (current.keyCode == KeyCode.RightArrow && current.type == EventType.KeyDown)
            {
                if (_currentColor < _editorAssets.colors.Count - 1)
                    _currentColor++;
                this.Repaint();
            }


            StartHorizon(() =>
            {
                _currentLevel =
                    EditorGUILayout.IntField(
                        $"level (total:{(_levelCollection != null ? _levelCollection.levels.Count : 0)})", _currentLevel);
                if (GUILayout.Button("<<"))
                {
                    _currentLevel--;
                    Load();
                }

                if (GUILayout.Button(">>"))
                {
                    _currentLevel++;
                    Load();
                }

                if (GUILayout.Button("load from path"))
                {
                    var data = DuongExtensions.LoadTextAtPath(path);
                    _levelCollection = JsonUtility.FromJson<JigsawCollection>(data);
                    if (_levelCollection == null)
                        _levelCollection = new JigsawCollection();
                    Load();
                    Debug.Log(_levelCollection);
                }

                if (GUILayout.Button("save file"))
                {
                    File.WriteAllText(path, JsonUtility.ToJson(_levelCollection));
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Debug.LogError("save to path:" + path);
                }

                if (GUILayout.Button("save level"))
                {
                    if (_levelCollection.levels.Count == _currentLevel)
                        _levelCollection.levels.Add(_tempLevel.Clone());
                    else
                        _levelCollection.levels[_currentLevel] = _tempLevel.Clone();
                }
               
            });
            if (GUILayout.Button("Convert Data"))
            {
                for (var i = 0; i < _levelCollection.levels.Count; i++)
                {
                    _levelCollection.levels[i].ConvertData();
                }
            }
           

            StartHorizon(() =>
            {
                if (GUILayout.Button("SaveAsNew"))
                {
                    _levelCollection.levels.Add(_tempLevel.Clone());
                    _currentLevel = _levelCollection.levels.Count-1;
                }
                if (GUILayout.Button("Delete"))
                {
                    _levelCollection.levels.RemoveAt(_currentLevel);
                    Load();
                }
                if (GUILayout.Button("Clear"))
                {
                    _tempLevel = new JigsawLevel();
                }
            });

            var df = DefaultSize;
            df = EditorGUILayout.Vector3IntField("default", df, GUILayout.Width(300));
            if (DefaultSize != df)
                DefaultSize = df;


            EditorGUILayout.LabelField("Select :", _level2Title, _baseLayout);
            _lastRect = GUILayoutUtility.GetLastRect();
            int count1 = 0;
            for (int i = 0; i < _editorAssets.colors.Count; i++)
            {
                Vector2 rootPos = new Vector2(50 + count1 * (_editorAssets.tileSize.x),
                    _lastRect.yMax + 10);
                Rect rect = new Rect(rootPos, _editorAssets.tileSize);

                if (i < _editorAssets.numberTextures.Length)
                {
                    DrawTexture(rect, _editorAssets.numberTextures[i], _editorAssets.colors[i]);
                }
                else
                {
                    DrawBgTile(rect, _editorAssets.colors[i]);
                }

                //Draw Indicator
                if (i == _currentColor)
                {
                    rootPos.y += _editorAssets.tileSize.y + 2;

                    var size = new Vector2(_editorAssets.tileSize.x, _editorAssets.tileSize.y / 4);
                    rect = new Rect(rootPos, size);
                    DrawBgTile(rect, Color.black);
                }

                //Catch Click
                if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition))
                {
                    _currentColor = i;
                    this.Repaint();
                }
                

                count1++;
            }

            GUILayout.Space(_editorAssets.tileSize.y  + 20);
            GUI.color = color;

            EditorGUILayout.LabelField(path);
            if (_tempLevel != null)
            {
                _tempLevel.size = EditorGUILayout.Vector3IntField("size", _tempLevel.size, GUILayout.Width(300));
                _tempLevel.file = EditorGUILayout.TextField("file", _tempLevel.file, GUILayout.Width(300));
                _tempLevel.fileBg = EditorGUILayout.TextField("fileBg", _tempLevel.fileBg, GUILayout.Width(300));
                
                if (GUILayout.Button("Load Texture"))
                {
                    JigsawBoard obj = (JigsawBoard) FindObjectOfType(typeof(JigsawBoard));
                    if (obj != null)
                        _jigsawTex = obj.mapTextures[_currentLevel];
//                    _jigsawTex = Resources.Load<Texture2D>(_tempLevel.file);
                    Debug.Log(_jigsawTex,_jigsawTex);
                }
                
         
                
                _lastRect = GUILayoutUtility.GetLastRect();
                var col = _tempLevel.size.x;
                var row = _tempLevel.size.y;
                for (int i = 0; i < col; i++)
                for (int j = 0; j < row; j++)
                {
                    var point = new Point(i, j);
                    Vector2 rootPos = new Vector2(50 + point.col * (_editorAssets.tileSize.x),
                        _lastRect.yMax + 10 + (row - 1 - point.row) *
                        (_editorAssets.tileSize.y));
                    
                    
                    Rect rect = new Rect(rootPos, _editorAssets.tileSize);
                    if ((i + j) % 2 == 0)
                        DrawBgTile(rect, _editorAssets.blankColor);
                    else
                        DrawBgTile(rect, _editorAssets.blankColor2);
                    
                    // Mouse down
                    if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition))
                    {
                        
                        if (current.button == 0)
                        {
                            _tempLevel.TryAdd(point,_currentColor,current.shift,current.control);
                        }

                        if (current.button == 1)
                            _tempLevel.TryRemove(point,_currentColor);
                        
                        Repaint();
                    }
                }
                
                if (_jigsawTex != null)
                {
                    Vector2 rootPos = new Vector2(50 + 0 * (_editorAssets.tileSize.x),
                        _lastRect.yMax + 10 + (row - 1 -(row-1)) *
                        (_editorAssets.tileSize.y));
                    
                    Rect rt = new Rect(rootPos,new Vector2( _editorAssets.tileSize.x * col, _editorAssets.tileSize.y * row));
                    DrawTexture(rt, _jigsawTex,new Color(1,1,1,0.5f));
                }


                foreach (var part in _tempLevel.parts2)
                {
                    for (var i = 0; i < part.Count; i++)
                    {
                        var point = part.points[i];
                        Vector2 rootPos = new Vector2(50 + point.col * (_editorAssets.tileSize.x),
                            _lastRect.yMax + 10 + (row - 1 - point.row) *
                            (_editorAssets.tileSize.y));
                        Rect rect = new Rect(rootPos, _editorAssets.tileSize);
                        if((point.value & 1) != 0)
                            DrawBgTile(rect, new Color(1,0.5f,0.5f));
                        if((point.value & 2) != 0)
                            DrawTexture(rect,StaticTexture2.IndicateTexture, Color.white);
                        DrawTexture(rect, _editorAssets.numberTextures[i], _editorAssets.colors[part.customParameter.intValue3]);
                    }
                   
                }
                
                

                GUILayout.Space(row * (_editorAssets.tileSize.y) + 20);
            }

            
            GUILayout.Space(15);
            EditorGUILayout.EndScrollView();
        }

        private Texture2D Bgtexture2D => SquareTexture;

        void DrawBgTile(Rect rect, Color bgColor)
        {
            GUI.color = bgColor;
            GUI.DrawTexture(rect, Bgtexture2D, ScaleMode.StretchToFill);
        }

        void DrawTexture(Rect rect, Texture2D text, Color bgColor)
        {
            GUI.color = bgColor;
            GUI.DrawTexture(rect, text, ScaleMode.StretchToFill);
        }

        void StartHorizon(Action action)
        {
            //*
            EditorGUILayout.BeginHorizontal();
            action.Invoke();
            EditorGUILayout.EndHorizontal();
            //*
        }

        GUIStyle _level1 => new GUIStyle()
        {
            padding = new RectOffset(10, 0, 0, 0),
            fontStyle = FontStyle.Bold,
        };

        GUIStyle _level2Title => new GUIStyle()
        {
            padding = new RectOffset(20, 0, 5, 0),
            fontStyle = FontStyle.Bold
        };

        GUIStyle _level2 => new GUIStyle()
        {
            padding = new RectOffset(30, 0, 5, 0),
            //fontStyle = FontStyle.Bold
        };

        GUILayoutOption[] _baseLayout => new GUILayoutOption[] {GUILayout.Width(100f)};


        private static string FilePath => string.Format("{0}/{1}", Application.persistentDataPath, "PlayerData.json");
        
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
    }
    
    
}

public static class StaticTexture2
{
    
    private const string Indicate =
        "iVBORw0KGgoAAAANSUhEUgAAAIAAAACACAYAAADDPmHLAAADVElEQVR4Ae2cQY7iUAxEm1Hfq7lZZ242c7JMsrGoBS2MBPPl9yJFsj9GcVU9wS6Xfd+vHx8f573yta283A+7Lb/35QDgXPL7BxErfHRZYYkndtif+M5bv/LrrU/zYcs5IADLRfLehQTgvX4v97TPOxv9Oc7/3vnM48cd+P346Fsmv46nXG+fdA+AM/ztdtD6KQdW8/Dc53rcdfkXUFYwCwFg5l6qBaCsYBYCwMy9VAtAWcEsBICZe6kWgLKCWQgAM/dSLQBlBbMQAGbupVoAygpmIQDM3Eu1AJQVzEIAmLmXagEoK5iFADBzL9UCUFYwCwFg5l6qBaCsYBYCwMy9VAtAWcEsBICZe6kWgLKCWQgAM/dSLQBlBbMQAGbupVoAygpmIQDM3Eu1AJQVzEIAmLmXagEoK5iFADBzL9UCUFYwCwFg5l6qz/cEVmPBc8BfAF7moVgAwg5eIwC8zEOxAIQdvEYAeJmHYgEIO3iNAPAyD8UCEHbwGgHgZR6KBSDs4DUCwMs8FAtA2MFrBICXeSgWgLCD1wgAL/NQLABhB68RAF7moVgAwg5eIwC8zEOxAIQdvEYAeJmHYgEIO3iNAPAyD8UCEHbwGgHgZR6KBSDs4DUCwMs8FAtA2MFrBICXeSgWgLCD1wgAL/NQLABhB68RAF7moVgAwg5eIwC8zEOxAIQdvEYAeJmHYgEIO3iNAPAyD8UCEHbwGgHgZR6KBSDs4DUCwMs8FAtA2MFrBICXeSg+AdiO+3xl+O19nnnNc2A7JN3mvPsLMC/kliIBaNk1b1gA5mXaUiQALbvmDQvAvExbigSgZde8YQGYl2lLkQC07Jo3LADzMm0pEoCWXfOGBWBepi1FAtCya96wAMzLtKVIAFp2zRsWgHmZthQJQMuuecMCMC/TliIBaNk1b1gA5mXaUiQALbvmDQvAvExbigSgZde8YQGYl2lLkQC07Jo3LADzMm0pEoCWXfOGBWBepi1FAtCya96wAMzLtKVIAFp2zRsWgHmZthQJQMuuecMCMC/TliIBaNk1b1gA5mXaUvR5Z/rrON/ufPY/jlfapaN/tb3PXOO67Pu+HSffcbpec1lvpYc2Ol/ItPTlX8DS8bx+OQF4vcdLP0EAlo7n9cv9A9rXIu1zGh9YAAAAAElFTkSuQmCC";


    public static Texture2D IndicateTexture
    {
        get
        {
            if(_indicateTexture == null)
            {
                _indicateTexture = Base64ToTexture2D(Indicate);
            }

            return _indicateTexture;
        }
    }

    private static Texture2D _indicateTexture;
  
    
    public static Texture2D Base64ToTexture2D(string encodedData)
    {
        byte[] imageData = Convert.FromBase64String(encodedData);
			
        int width, height;
        GetImageSize(imageData, out width, out height);
			
        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Point;
        texture.LoadImage(imageData);
			
        return texture;
    }
    
    private static void GetImageSize(byte[] imageData, out int width, out int height)
    {
        width = ReadInt(imageData, 3 + 15);
        height = ReadInt(imageData, 3 + 15 + 2 + 2);
    }
		
    private static int ReadInt(byte[] imageData, int offset)
    {
        return (imageData[offset] << 8) | imageData[offset + 1];
    }
}

#endif