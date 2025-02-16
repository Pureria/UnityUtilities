using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityUtilities.DynamicComponentInspector
{    
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class CustomInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            
            //targetはUnityのインスペクタで選択されているオブジェクトを示す
            //targetはObject型なので、MonoBehaviour型にキャストする
            MonoBehaviour targetScript = (MonoBehaviour)target;
            
            Type type = targetScript.GetType(); //targetScriptの型を取得
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); //targetScriptのフィールドを取得(インスタンスフィールド、パブリックフィールド、非パブリックフィールド)

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("リフレクションで取得したフィールド", EditorStyles.boldLabel);

            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(targetScript); //targetScriptのフィールドの値を取得

                //フィールドの型によって適切なGUIを表示
                if (field.FieldType == typeof(int))
                {
                    int newValue = EditorGUILayout.IntField(field.Name, (int)value);
                    if (newValue != (int)value) field.SetValue(targetScript, newValue);
                }
                else if (field.FieldType == typeof(float))
                {
                    float newValue = EditorGUILayout.FloatField(field.Name, (float)value);
                    if (newValue != (float)value) field.SetValue(targetScript, newValue);
                }
                else if (field.FieldType == typeof(string))
                {
                    string newValue = EditorGUILayout.TextField(field.Name, (string)value);
                    if (newValue != (string)value) field.SetValue(targetScript, newValue);
                }
                else
                {
                    EditorGUILayout.LabelField($"{field.Name} ({field.FieldType.Name}): {value}");
                }
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(targetScript);
            }
        }
    }
}
