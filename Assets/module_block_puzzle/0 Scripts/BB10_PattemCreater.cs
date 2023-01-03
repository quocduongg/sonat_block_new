using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BB10_PattemCreater : MonoBehaviour
{
    public GameObject cubePrefab;
    public List<BB10_BrickCubeUnit> listCube;

    void Awake()
    {
        listPattemsInfor = pattemDataSave.listPattemsInfor;

        for(int i = 0; i < listPattemsInfor.Length; i++)
        {
            bool[] grid = listPattemsInfor[i].grid;


            List<Vec2> listVec = new List<Vec2>();


            for(int j = 0; j < grid.Length; j++)
            {
                if(grid[j])
                {
                    listVec.Add(new Vec2(j % 5, j / 5));
                }
            }

            dataInfor.Add(listPattemsInfor[i].type, listVec.ToArray());
        }
    }

    public Bricks_PattemTableObj pattemDataSave;
    PattemInfor[] listPattemsInfor;
    Dictionary<Types, Vec2[]> dataInfor = new Dictionary<Types, Vec2[]>();
                 
    public List<BB10_BrickCubeUnit> CreatePattem(Types type, Vector2 pos, float scale)
    {
        return Create(type, pos, scale);
    }

    List<BB10_BrickCubeUnit> Create(Types thisType, Vector2 pos, float scale)
    {
        Vec2[] pattemList = dataInfor[thisType];

        List<BB10_BrickCubeUnit> listCubeUnit = new List<BB10_BrickCubeUnit>();

//        BB10_ColorData data = BB10_MainObjControl.Instant.colorControl.GetRandSpriteData();

        for(int i = 0; i < pattemList.Length; i++)
        {
            BB10_BrickCubeUnit thisCubeUnit = GetCube();
            listCubeUnit.Add(thisCubeUnit);
            thisCubeUnit.thisType = thisType;
//            thisCubeUnit.SetSprite(data);
            //thisCubeUnit.SetSprite(colorCtr.GetSpriteData(1), 1);
            thisCubeUnit.transform.localScale = new Vector3(scale, scale, scale);

            float posX = pos.x + pattemList[i].R * scale;
            float posY = pos.y + pattemList[i].C * scale;

            thisCubeUnit.transform.position = new Vector2(posX, posY);

            thisCubeUnit.scale = scale;
            thisCubeUnit.indexRow = pattemList[i].C;
            thisCubeUnit.indexCol = pattemList[i].R;

//            thisCubeUnit.SetShadowDropBlock(false);
        }
        return listCubeUnit;
    }               

    public BB10_BrickCubeUnit CreateABlock(Vector2 pos, float scale)
    {
        BB10_BrickCubeUnit newCubeUnit = GetCube();
        newCubeUnit.transform.localScale = new Vector3(scale, scale, scale);
        newCubeUnit.transform.position = pos;
        return newCubeUnit;
    }       

    Vec2[] listO0 = new Vec2[]
    {
        new Vec2(0, 0),
    };

    Vec2[] listO1 = new Vec2[]
    {
        new Vec2(0, 0),
        new Vec2(1, 0),
        new Vec2(1, 1),
        new Vec2(0, 1)
    };

    Vec2[] listO2 = new Vec2[]
    {
        new Vec2(0, 0),
        new Vec2(1, 0),
        new Vec2(-1, 0),
        new Vec2(0, 1),
        new Vec2(1, 1),
        new Vec2(-1, 1),
        new Vec2(0, -1),
        new Vec2(1, -1),
        new Vec2(-1, -1)
    };

    Vec2[] listL0 = new Vec2[]
    {
        new Vec2(1, 0),
        new Vec2(1, 1),
        new Vec2(0, 1)
    };

    Vec2[] listL1 = new Vec2[]
    {
        new Vec2(-1, 0),
        new Vec2(0, 1),
        new Vec2(1, 1),
        new Vec2(-1, 1),
        new Vec2(-1, -1)
    };

    Vec2[] listI0 = new Vec2[]
    {
        new Vec2(0, 0),
        new Vec2(1, 0),
    };

    Vec2[] listI1 = new Vec2[]
    {
        new Vec2(-1, 0),
        new Vec2(0, 0),
        new Vec2(1, 0)
    };

    Vec2[] listI2 = new Vec2[]
    {
        new Vec2(-1, 0),
        new Vec2(0, 0),
        new Vec2(1, 0),
        new Vec2(2, 0)
    };

    Vec2[] listI3 = new Vec2[]
    {
        new Vec2(-2, 0),
        new Vec2(-1, 0),
        new Vec2(0, 0),
        new Vec2(1, 0),
        new Vec2(2, 0)
    };

    Vec2[] listT0 = new Vec2[]
    {
        new Vec2(0, 1),
        new Vec2(0, 0),
        new Vec2(0, -1),
        new Vec2(-1, 0)
    };

    Vec2[] listT1 = new Vec2[]
    {
        new Vec2(-1, 0),
        new Vec2(0, 0),
        new Vec2(1, 0),
        new Vec2(1, 1),
        new Vec2(1, -1)
    };

    Vec2[] listLB0 = new Vec2[]
    {
        new Vec2(0, 1),
        new Vec2(0, 0),
        new Vec2(0, -1),
        new Vec2(1, -1)
    };

    Vec2[] listZ0 = new Vec2[]
    {
        new Vec2(0, 1),
        new Vec2(0, 0),
        new Vec2(-1, 0),
        new Vec2(-1, -1)
    };

    BB10_BrickCubeUnit GetCube()
    {
        BB10_BrickCubeUnit cube;
        if(listCube.Count == 0)
        {
            cube = Instantiate(cubePrefab).GetComponent<BB10_BrickCubeUnit>();
        }
        else
        {
            cube = listCube[listCube.Count - 1];
            listCube.RemoveAt(listCube.Count - 1);
//            cube.SetAlpha(GameDefine.pattemLightAlpha);
            cube.transform.eulerAngles = Vector3.zero;
        }

//        cube.SetShadowDropBlock(false);

        return cube;
    }

    public void SetCube(BB10_BrickCubeUnit cube)
    {
        listCube.Add(cube);
        cube.transform.position = new Vector2(0, -100);
    }

    public Types GetTypeFromString(string text)
    {
        for(int i = 0; i < listPattemsInfor.Length; i++)
        {
            if(listPattemsInfor[i].type.ToString().Equals(text))
            {
                return listPattemsInfor[i].type;
            }
        }

        return Types.O0;
    }
}