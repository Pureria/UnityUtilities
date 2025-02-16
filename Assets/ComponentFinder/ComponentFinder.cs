using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace UnityUtilities.ComponentFinder
{
    public class ComponentFinder : EditorWindow
    {
        private Dictionary<string, List<Type>> componentTypes = new Dictionary<string, List<Type>>();
        //private List<Type> componentTypes = new List<Type>();
        private int selectedIndex = 0;
        private List<GameObject> foundObjects = new List<GameObject>();

        [MenuItem("Tools/Component Finder")]
        public static void ShowWindow()
        {
            GetWindow<ComponentFinder>("Component Finder");
        }

        private void OnEnable()
        {
            LoadComponentType();
        }

        private void LoadComponentType()
        {
            componentTypes.Clear();
            
            //アセンブリからコンポーネントの型を取得
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                //コンポーネントの型を取得
                foreach(var type in assembly.GetTypes())
                {
                    //Componentを継承していて、抽象クラスでない場合
                    if (typeof(Component).IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        string key = type.Namespace ?? "Non Namespace";
                        if (!componentTypes.ContainsKey(key))
                        {
                            componentTypes.Add(key, new List<Type> { type });
                        }
                        else
                        {
                            componentTypes[key].Add(type);
                        }
                    }
                }
            }
            
            //namespaceでソート
            componentTypes = componentTypes.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            foreach (var kvp in componentTypes)
            {
                kvp.Value.Sort((a, b) => a.Name.CompareTo(b.Name));
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("シーン内のコンポーネント検索", EditorStyles.boldLabel);
            
            //ドロップダウンで検索対象のコンポーネント選択
            string[] typeNames = componentTypes.SelectMany(x => x.Value.Select(t => x.Key + "/" + t.Name)).ToArray();
            selectedIndex = EditorGUILayout.Popup("コンポーネントを選択", selectedIndex, typeNames);

            if (GUILayout.Button("検索"))
            {
                //選択されたコンポーネントの型を取得
                var selectedType = GetSelectedType(selectedIndex, componentTypes);
                FindComponents(selectedType);
            }
            
            EditorGUILayout.Space();
            GUILayout.Label("検索結果：", EditorStyles.boldLabel);

            if (foundObjects.Count == 0)
            {
                //見つからなかった場合
                EditorGUILayout.LabelField("見つかりませんでした");
            }
            else
            {
                //検索結果をボタンで表示
                //ボタンを押すとそのオブジェクトが選択される
                foreach (var obj in foundObjects)
                {
                    if(GUILayout.Button(obj.name)) Selection.activeGameObject = obj;
                }
            }
        }
        
        // 選択されたインデックスに基づいて型を取得するメソッド
        private Type GetSelectedType(int index, Dictionary<string, List<Type>> componentTypes)
        {
            // インデックスを走査して、正しい型を取得する
            int currentIndex = 0;
            foreach (var kvp in componentTypes)
            {
                foreach (var type in kvp.Value)
                {
                    if (currentIndex == index)
                    {
                        return type; // 選択された型を返す
                    }
                    currentIndex++;
                }
            }
            return null; // 見つからなかった場合
        }

        //選択されたコンポーネントの型を持つコンポーネントを検索
        private void FindComponents(Type targetType)
        {
            foundObjects.Clear();

            //コンポーネントが選択されていない場合終了
            if (targetType == null)
            {
                Debug.LogError("検索対象のコンポーネントが選択されていません");
                return;
            }
            
            //Componentを継承しているかチェックする
            if (!typeof(Component).IsAssignableFrom(targetType))
            {
                Debug.LogError($"指定された型 {targetType} は Component を継承していません");
                return;
            }
            
            //動的に型を取得しているためリフレクションで動的にメソッドを取得しジェネリックメソッドを呼び出す
            //FindObjectsByTypeメソッドをリフレクションで取得
            var method = typeof(UnityEngine.Object)
                //UnityEngine.ObjectのPublicかつStaticなメソッドを取得
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                //メソッド名がFindObjectsByTypeでジェネリックメソッドであるものを取得
                .FirstOrDefault(x => x.Name == "FindObjectsByType" && x.IsGenericMethod);
            
            //ジェネリックメソッドを取得したのでtargetTypeに適用したメソッドを作成
            var genericMethod = method.MakeGenericMethod(targetType);
            
            //Invokeの戻り値はobjectなので、配列にキャストする
            var objects = (Array)genericMethod.Invoke(null, new object[] { FindObjectsSortMode.None });
            
            //検索結果をリストに追加
            foreach (var obj in objects)
            {
                //Component型にキャストしてGameObjectを取得
                if (obj is Component component)
                {
                    foundObjects.Add(component.gameObject);
                }
            }
        }
    }
}
