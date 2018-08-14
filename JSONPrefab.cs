using FullSerializer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class JSONPrefab : EditorWindow {
    public static Dictionary<string, UnityEngine.Object> guids;
    public static Dictionary<Action<UnityEngine.Object>, string> fields;

    [Serializable]
    public struct JGameObject
    {
        public string name;
        public Vector3 positon;
        public Vector3 rotation;
        public Vector3 scale;
        public JGameObject[] children;
        public JComponent[] components;
        public string guid;

        public GameObject Instantiate(Transform parent)
        {
            GameObject go = new GameObject(name);

            go.transform.parent = parent;
            go.transform.localPosition = positon;
            go.transform.localEulerAngles = rotation;
            go.transform.localScale = scale;

            guids.Add(guid, go);

            if (children != null)
                foreach (JGameObject jgo in children)
                    jgo.Instantiate(go.transform);

            foreach(JComponent jc in components)
            {
                Type type = Type.GetType(jc.type);
                if (type != null)
                {
                    if (go.GetComponent(type) != null)
                    {
                        guids.Add(jc.guid, go.GetComponent(type));
                    }
                    else
                    {
                        Component c = go.AddComponent(type);
                        guids.Add(jc.guid, c);

                        foreach (JField field in jc.fields)
                        {
                            try
                            {
                                if (field.value is string && ((string)field.value).StartsWith("{{ ") && ((string)field.value).EndsWith(" }}"))
                                {
                                    SetObjectValue((string)field.value, (UnityEngine.Object obj) => type.GetField(field.name).SetValue(c, obj));
                                } else
                                {
                                    type.GetField(field.name).SetValue(c, field.value);
                                }
                            } catch(Exception e)
                            {
                                Debug.Log("Failed to set field " + type.Name + "." + field.name + " to " + field.value);
                            }
                        }
                        foreach (JProperty prop in jc.properties)
                        {
                            try
                            {
                                System.Reflection.PropertyInfo pi = type.GetProperty(prop.name);
                                if (pi.GetSetMethod() == null) continue;

                                if (prop.value is string && ((string)prop.value).StartsWith("{{ ") && ((string)prop.value).EndsWith(" }}"))
                                {
                                    SetObjectValue((string)prop.value, (UnityEngine.Object obj) => pi.SetValue(c, Convert.ChangeType(obj, pi.PropertyType), null));
                                }
                                else
                                {
                                    pi.SetValue(c, Convert.ChangeType(prop.value, pi.PropertyType), null);
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.Log("Failed to set prop " + type.Name + "." + prop.name + " to " + prop.value);
                            }
                        }
                    }
                } else
                {
                    Debug.Log("Skipping type " + jc.type + "!");
                }
            }

            return go;
        }

        public void SetObjectValue(string guid, Action<UnityEngine.Object> setter)
        {
            fields.Add(setter, guid);
        }
    }

    [Serializable]
    public struct JField
    {
        public string name;
        public object value;
    }

    [Serializable]
    public struct JProperty
    {
        public string name;
        public object value;
    }

    [Serializable]
    public struct JComponent
    {
        public string type;
        public JField[] fields;
        public JProperty[] properties;
        public string guid;
    }

    [MenuItem("JSONPrefab/Instantiate")]
    public static void CreateWindow()
    {
        ((JSONPrefab)ScriptableObject.CreateInstance(typeof(JSONPrefab))).ShowUtility();
    }

    //string text = "Paste JSON here";

    void OnGUI()
    {
        if (GUILayout.Button("Instantiate Ragdoll"))
        {
            guids = new Dictionary<string, UnityEngine.Object>();
            fields = new Dictionary<Action<UnityEngine.Object>, string>();

            string text = File.ReadAllText(@"C:\Program Files (x86)\Steam\SteamApps\common\Human Fall Flat\prefab.json");
            var s = new fsSerializer();
            fsData data = fsJsonParser.Parse(text);
            JGameObject instance = new JGameObject();
            s.TryDeserialize<JGameObject>(data, ref instance);
            instance.Instantiate(null);

            foreach(var a in fields.Keys)
            {
                string g = fields[a];
                if(guids.ContainsKey(g))
                {
                    a.Invoke(guids[g]);
                } else
                {
                    Debug.Log("Missing GUID " + g);
                }
            }
        }

        if (GUILayout.Button("Instantiate Player"))
        {
            guids = new Dictionary<string, UnityEngine.Object>();
            fields = new Dictionary<Action<UnityEngine.Object>, string>();

            string text = File.ReadAllText(@"C:\Program Files (x86)\Steam\SteamApps\common\Human Fall Flat\playerPrefab.json");
            var s = new fsSerializer();
            fsData data = fsJsonParser.Parse(text);
            JGameObject instance = new JGameObject();
            s.TryDeserialize<JGameObject>(data, ref instance);
            instance.Instantiate(null);

            foreach (var a in fields.Keys)
            {
                string g = fields[a];
                if (guids.ContainsKey(g))
                {
                    a.Invoke(guids[g]);
                }
                else
                {
                    Debug.Log("Missing GUID " + g);
                }
            }
        }

        //text = EditorGUILayout.TextArea(text);
    }
}
