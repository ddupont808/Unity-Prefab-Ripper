using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using FullSerializer;

namespace ExampleAssembly
{
    public class Loader
    {
        static UnityEngine.GameObject gameObject;

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

        public static Dictionary<UnityEngine.Object, string> guids = new Dictionary<UnityEngine.Object, string>();

        public static void Load()
        {
            gameObject = new UnityEngine.GameObject();
            gameObject.AddComponent<Cheat>();
            UnityEngine.Object.DontDestroyOnLoad(gameObject);

            UnityEngine.Debug.Log("swag");
            UnityEngine.Debug.Log("wew");

            try
            {
                Game game = GameObject.FindObjectOfType<Game>();
                GameObject playerPrefab = game.ragdollPrefab.gameObject;

                JGameObject jgo = SerializeGameObject(playerPrefab);
                var s = new fsSerializer();
                fsData data;
                s.TrySerialize<JGameObject>(jgo, out data);

                File.WriteAllText("prefab.json", fsJsonPrinter.PrettyJson(data));
                Debug.Log("saved ragdoll prefab");
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }


            try
            {
                Game game = GameObject.FindObjectOfType<Game>();
                GameObject playerPrefab = game.playerPrefab.gameObject;

                JGameObject jgo = SerializeGameObject(playerPrefab);
                var s = new fsSerializer();
                fsData data;
                s.TrySerialize<JGameObject>(jgo, out data);

                File.WriteAllText("playerPrefab.json", fsJsonPrinter.PrettyJson(data));
                Debug.Log("saved player prefab");
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        public static string GetGUID(UnityEngine.Object go)
        {
            if (guids.ContainsKey(go))
                return guids[go];
            string guid = "{{ " + Guid.NewGuid().ToString() + " }}";
            guids.Add(go, guid);
            return guid;
        }

        private static JGameObject SerializeGameObject(GameObject go)
        {
            JGameObject jgo;

            jgo.name = go.name;
            jgo.positon = go.transform.localPosition;
            jgo.rotation = go.transform.localEulerAngles;
            jgo.scale = go.transform.localScale;
            int nchildren = go.transform.childCount;
            JGameObject[] jgos = new JGameObject[nchildren];
            for (int i = 0; i < nchildren; i++)
                jgos[i] = SerializeGameObject(go.transform.GetChild(i).gameObject);
            Component[] c = go.GetComponents<Component>();
            JComponent[] comps = new JComponent[c.Length];
            for(int i = 0; i < c.Length; i++)
            {
                JComponent jc;
                Component co = c[i];
                Type type = co.GetType();

                jc.type = type.AssemblyQualifiedName;
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField | BindingFlags.SetField);
                List<JField> jfields = new List<JField>();
                foreach(FieldInfo fi in fields)
                {
                    bool flag = false;
                    foreach(Attribute attr in fi.GetCustomAttributes(true))
                    {
                        if (attr is HideInInspector || attr is ObsoleteAttribute)
                            flag = true;
                    }

                    if (flag) continue;
                    JField jfield;
                    jfield.name = fi.Name;
                    object field = fi.GetValue(co);
                    if(field != null && typeof(UnityEngine.Object).IsAssignableFrom(fi.FieldType))
                    {
                        field = GetGUID((UnityEngine.Object)field);
                    }
                    jfield.value = field;
                    jfields.Add(jfield);
                }
                jc.fields = jfields.ToArray();


                PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Instance);
                List<JProperty> jprops = new List<JProperty>();
                foreach (PropertyInfo fi in props)
                {
                    bool flag = false;
                    foreach (Attribute attr in fi.GetCustomAttributes(true))
                    {
                        if (attr is HideInInspector || attr is ObsoleteAttribute)
                            flag = true;
                    }

                    if (flag) continue;
                    try
                    {
                        Type t = fi.PropertyType;
                        //if (t == typeof(string) || t == typeof(int) || t == typeof(float) || t == typeof(double) || t == typeof(Vector3) || t == typeof(Vector2))
                        object value = fi.GetValue(co, null);

                        var s = new fsSerializer();
                        fsData data;
                        s.TrySerialize(value, out data);
                        fsJsonPrinter.PrettyJson(data);

                        if (value != null && typeof(UnityEngine.Object).IsAssignableFrom(fi.PropertyType))
                        {
                            value = GetGUID((UnityEngine.Object)value);
                        }

                        JProperty jprop;
                        jprop.name = fi.Name;
                        jprop.value = value;
                        jprops.Add(jprop);
                    } catch(Exception ex)
                    {

                    }
                }
                jc.properties = jprops.ToArray();
                jc.guid = GetGUID(co);

                comps[i] = jc;
            }
            jgo.components = comps;
            jgo.children = jgos;
            jgo.guid = GetGUID(go);

            return jgo;
        }

        public static void Unload()
        {
            UnityEngine.Object.Destroy(gameObject);
        }
    }
}
