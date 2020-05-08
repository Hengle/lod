﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace XEditor
{
    public class LODAsset
    {
        public GameObject go;
        public SkinnedMeshRenderer[] renders;
        public Mesh[] meshes;

        public float screenRelativeHeight;
        private int vertCnt, triCnt;
        private Vector2 scroll;
        
        public void Drop(GameObject g)
        {
            go = Root(g);
            renders = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            int cnt = renders.Length;
            meshes = new Mesh[cnt];
            vertCnt = 0;
            triCnt = 0;
            for (int i = 0; i < cnt; i++)
            {
                meshes[i] = renders[i].sharedMesh;
                vertCnt += meshes[i].vertexCount;
                triCnt += meshes[i].triangles.Length;
            }
            triCnt /= 3;
        }

        private GameObject Root(GameObject go)
        {
            bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(go);
            if (isPrefab)
            {
                Transform ret = go.transform;
                while (ret.parent != null && PrefabUtility.IsPartOfAnyPrefab(ret.parent.gameObject))
                {
                    ret = ret.parent;
                }
                return ret.gameObject;
            }
            return go;
        }

        public void GUI()
        {
            if (meshes != null && go != null)
            {
                scroll = GUILayout.BeginScrollView(scroll);
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Label(go.name, LODGUI.totalStyle);
                if (GUILayout.Button("Visualize Bounds", GUILayout.MaxWidth(110)))
                {
                    LodUtil.AttachCollider(go);
                }
                GUILayout.EndHorizontal();
                GUILayout.Label("total verts: " + vertCnt + " tris: " + triCnt, LODGUI.totalStyle);
                foreach (var mesh in meshes)
                {
                    GUILayout.BeginHorizontal();
                    DrawMesh(mesh);
                    GUILayout.BeginVertical();
                    GUILayout.Space(24);
                    GUILayout.Label(mesh.name);
                    GUILayout.Label("verts: " + mesh.vertexCount);
                    GUILayout.Label("tris:  " + mesh.triangles.Length / 3);
                    GUILayout.Label("bounds: " + mesh.bounds);
                    string desc = "skin ";
                    if (has(mesh.uv)) desc += "uv ";
                    if (has(mesh.uv2)) desc += "uv2 ";
                    if (has(mesh.uv3)) desc += "uv3 ";
                    if (has(mesh.uv4)) desc += "uv4 ";
                    if (has(mesh.normals)) desc += "normal ";
                    if (has(mesh.tangents)) desc += "tangent ";
                    if (has(mesh.colors)) desc += "color ";
                    GUILayout.Label(desc);
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("no gameobject attached");
            }
        }

        private void DrawMesh(Mesh mesh)
        {
            var prev = AssetPreview.GetAssetPreview(mesh);
            GUIContent content = new GUIContent(prev, mesh.name);
            if(GUILayout.Button(content, LODGUI.Styles.m_LODRendererButton))
            {
                Selection.activeObject = mesh;
            }
        }

        private bool has(System.Array arr)
        {
            return arr != null && arr.Length > 1;
        }
    }

    public class LODAction
    {
        private readonly float m_Percentage;
        private readonly List<LODGUI.LODInfo> m_LODs;
        private readonly Vector2 m_ClickedPosition;
        private readonly List<LODAsset> m_LODsProperty;

        public delegate void Callback();
        private readonly Callback m_Callback;

        public LODAction(List<LODGUI.LODInfo> lods, float percentage, Vector2 clickedPosition, List<LODAsset> propLODs, Callback callback)
        {
            m_LODs = lods;
            m_Percentage = percentage;
            m_ClickedPosition = clickedPosition;
            m_LODsProperty = propLODs;
            m_Callback = callback;
        }

        public void InsertLOD()
        {
            int insertIndex = -1;
            float screenHeight = 0.1f;
            foreach (var lod in m_LODs)
            {
                if (m_Percentage > lod.RawScreenPercent)
                {
                    insertIndex = lod.LODLevel;
                    screenHeight = lod.ScreenPercent;
                    break;
                }
            }

            LODAsset asset = new LODAsset();
            asset.screenRelativeHeight = Mathf.Max(0.1f, screenHeight - 0.1f);
            if (insertIndex < 0)
            {
                m_LODsProperty.Add(asset);
                insertIndex = m_LODs.Count;
            }
            else
            {
                m_LODsProperty.Insert(insertIndex, asset);
            }

            asset.screenRelativeHeight = m_Percentage;
            m_Callback?.Invoke();
        }

        public void DeleteLOD()
        {
            if (m_LODs.Count <= 0) return;
            
            foreach (var lod in m_LODs)
            {
                string name = string.Format("lod", lod.LODLevel);
                if (lod.m_RangePosition.Contains(m_ClickedPosition) &&
                    EditorUtility.DisplayDialog("Delete LOD", "Are you sure you wish to delete this LOD?", "Yes", "No"))
                {
                    m_LODsProperty.RemoveAt(lod.LODLevel);
                    m_LODs.Remove(lod);
                    m_Callback?.Invoke();
                    break;
                }
            }
        }
    }
}