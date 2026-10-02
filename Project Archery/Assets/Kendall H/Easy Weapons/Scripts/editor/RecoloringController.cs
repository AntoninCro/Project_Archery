using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyWeapon.Recoloring
{

    [CustomEditor(typeof(RecoloringTool))]
    public class RecoloringController : Editor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            RecoloringTool coloringTool = (RecoloringTool)target;

            if (GUILayout.Button("Recolor Mesh(s)"))
            {
                coloringTool.ReColor();
            }
            if (GUILayout.Button("Save as New Prefab"))
            {
                coloringTool.LockInAndCreateNewPrefab();
            }

        }
    }
}

