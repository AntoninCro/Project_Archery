using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyWeapon
{
    namespace Recoloring
    {
        public class RecoloringTool : MonoBehaviour
        {
            [Header("vvv Use The RGBA Prefabs Here vvv")]
            [Tooltip("All child objects with mesh componants will have to be set active")]
            [SerializeField] GameObject prefabToRecolor;

            GameObject currentReColored;
            List<GameObject> newObjects = new List<GameObject>();

            [Tooltip("Alpha channel determines smoothness")]
            [SerializeField]
            Color bladeColor = new Color(1, 0, 0, 0), hiltColor = new Color(0, 1, 0, 0),
                handleColor = new Color(0, 0, 1, 0), enchantmentColor = new Color(0, 0, 0, 1);

            [Tooltip("2 is good for URP, HDRP may need 1000000+ depending on volume and lighting settings")]
            [SerializeField] float enchantmentGlowMult = 2f;

            List<MeshFilter> filtersToReColor = new List<MeshFilter>();

            bool inPlayMode = false;

            private void Start()
            {
                inPlayMode = true;
            }

            public void ReColor()
            {
                if (!inPlayMode)
                {
                    Debug.Log("MUST BE IN PLAY MODE");
                    return;
                }


                filtersToReColor.Clear();
                if (newObjects != null)
                {
                    if (newObjects.Count > 0)
                    {
                        //destroy them in reverse order
                        for (int i = newObjects.Count - 1; i >= 0; i--)
                        {
                            Destroy(newObjects[i]);
                        }

                    }
                }

                newObjects.Clear();

                if (currentReColored != null)
                {
                    Destroy(currentReColored);
                }

                currentReColored = Instantiate(prefabToRecolor, transform);
                newObjects.Add(currentReColored);
                MeshFilter[] mf = currentReColored.GetComponentsInChildren<MeshFilter>();
                filtersToReColor.AddRange(mf);





                for (int i = 0; i < filtersToReColor.Count; i++)
                {

                    Color[] currentCols;
                    Color[] newCols;


                    //if (inPlayMode)
                    //{

                    currentCols = filtersToReColor[i].mesh.colors;
                    newCols = new Color[currentCols.Length];

                    //}
                    //else
                    //{

                    //    currentCols = filtersToReColor[i].sharedMesh.colors;
                    //    newCols = new Color[currentCols.Length];

                    //}


                    for (int j = 0; j < newCols.Length; j++)
                    {
                        //newCols[j] = currentCols[j];

                        if (currentCols[j].r == 1f)
                        {
                            newCols[j] = bladeColor;
                        }
                        if (currentCols[j].g == 1f)
                        {
                            newCols[j] = hiltColor;

                        }
                        if (currentCols[j].b == 1f)
                        {
                            newCols[j] = handleColor;

                        }
                        if (currentCols[j].a == 1f)
                        {
                            newCols[j] = enchantmentColor * enchantmentGlowMult;

                        }

                    }

                    filtersToReColor[i].mesh.colors = newCols;

                }


            }

            public void LockInAndCreateNewPrefab()
            {

                if (!inPlayMode)
                {
                    Debug.Log("MUST BE IN PLAY MODE");
                    return;
                }

                if (filtersToReColor != null && filtersToReColor.Count > 0)
                {


                    if (currentReColored != null)
                    {

                        string objectName = currentReColored.name + ".RECOLORED";

                        MeshFilter[] filts = currentReColored.GetComponentsInChildren<MeshFilter>();
                        SkinnedMeshRenderer[] skins = currentReColored.GetComponentsInChildren<SkinnedMeshRenderer>();


                        List<Mesh> meshes = new List<Mesh>();
                        foreach (MeshFilter filter in filts)
                        {
                            filter.gameObject.SetActive(true);
                            meshes.Add(filter.mesh);
                        }
                        foreach (SkinnedMeshRenderer sk in skins)
                        {
                            sk.gameObject.SetActive(true);

                            meshes.Add(sk.sharedMesh);
                        }

                        string filePath = EditorUtility.SaveFilePanelInProject("Save " + objectName, objectName, "asset", "");
                        if (filePath == "") { return; }

                        string minus1Path = "";

                        string[] splitPath = filePath.Split(char.Parse("/"));

                        for (int i = 0; i < splitPath.Length - 1; i++)
                        {
                            minus1Path += splitPath[i];
                            minus1Path += "/";
                        }

                        minus1Path += objectName;

                        int indexA = 1;
                        foreach (Mesh mesh in meshes)
                        {

                            string usedPath = minus1Path;


                            usedPath += indexA + "_Mesh.asset";

                            AssetDatabase.CreateAsset(mesh, usedPath);


                            indexA++;
                        }

                        string newPath = minus1Path + ".prefab";

                        bool prefabSuccess;
                        PrefabUtility.SaveAsPrefabAssetAndConnect(currentReColored, newPath, InteractionMode.UserAction, out prefabSuccess);

                        PrefabUtility.ApplyPrefabInstance(currentReColored, InteractionMode.UserAction);

                        if (prefabSuccess)
                        {
                            Debug.Log("Asset saved to " + newPath);
                        }
                        else
                        {
                            Debug.Log("Asset failed to save");
                        }

                    }
                    else
                    {
                        Debug.Log("prefabToReColor is not valid");
                    }

                }
                else
                {
                    Debug.Log("prefab Creation failed. Be sure you have already recolored the mesh");
                }

            }
        }
    }
}

