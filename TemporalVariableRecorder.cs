using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using System.Reflection;
using System;
using System.IO;
using System.Text;

public class TemporalVariableRecorder : MonoBehaviour
{
    public int insertionIndex;
    public List<ComponentPropertySelection> selections;

    private List<string> records;
    private float startTime = 0f;
    string s = "";

    private void Start()
    {
        RecordStart();
    }
    private void Update()
    {
        AddOneRecord();
    }

    private void OnApplicationQuit()
    {
        RecordEnd();
    }

    public void RecordStart()
    {
        records = new List<string>();
        int hour, minute, second, year, month, day;
        hour = DateTime.Now.Hour;
        minute = DateTime.Now.Minute;
        second = DateTime.Now.Second;
        year = DateTime.Now.Year;
        month = DateTime.Now.Month;
        day = DateTime.Now.Day;
        records.Add("Start at" + "," + string.Format("{0:D2}:{1:D2}:{2:D2} {3:D4}/{4:D2}/{5:D2}", hour, minute, second, year, month, day));

        startTime = Time.realtimeSinceStartup;
        s += "Time" + ",";
        //for (int i = 0; i < selections.Count; i++)
        //{
        //    var subProperties = ReflectionVariable.GetSubProperties(selections[i].component, selections[i].propertyName, selections[i].propertyInnerVariableNames);
        //    foreach (var subProperty in subProperties)
        //    {
        //        if(selections[i].customName !="")
        //        s += selections[i].customName + "." + subProperty + ',';
        //        else
        //        s += selections[i].gameObject.name+"_"+ selections[i].component.GetType().Name + "_" + selections[i].propertyName + "." + subProperty + ',';
        //    }
        //}
        for (int i = 0; i < selections.Count; i++)
        {
            (object propertyValue, System.Type propertyType) = ReflectionVariable.GetPropertyValueWithType(selections[i].component, selections[i].propertyName, selections[i].propertyInnerVariableNames); 

            if(selections[i].propertyInnerVariableNames[0] == "")
            {
                if (selections[i].customName != "")
                    s += selections[i].customName + "." + selections[i].propertyName + ',';
                else
                    s += selections[i].gameObject.name + "_" + selections[i].component.GetType().Name + "." + selections[i].propertyName + ',';
            }
            else
            {
                if (selections[i].customName != "")
                    s += selections[i].customName + "." + selections[i].propertyInnerVariableNames[0] + ',';
                else
                    s += selections[i].gameObject.name + "_" + selections[i].component.GetType().Name + "_" + selections[i].propertyName + "." + selections[i].propertyInnerVariableNames[0] + ',';
            }

            
        }
        records.Add(s);
    }

    public void AddOneRecord()
    {

        try
        {
            s = (Time.realtimeSinceStartup-startTime).ToString() + ",";
            for (int i = 0; i < selections.Count; i++)
            {
                //var subProperties = ReflectionVariable.GetSubProperties(selections[i].component, selections[i].propertyName, selections[i].propertyInnerVariableNames);
                //if(subProperties != null&& subProperties.Count!= 0) 
                //{
                //    foreach (var subProperty in subProperties)
                //    {
                //        (object propertyValue, System.Type propertyType) = ReflectionVariable.GetPropertyValueWithType(selections[i].component, selections[i].propertyName, selections[i].propertyInnerVariableNames);
                //        var subPropertyValue = ReflectionVariable.GetSubPropertyValue(propertyValue, subProperty);
                //       // s += subPropertyValue?.ToString() + ',';
                //          s += $"\"{subPropertyValue?.ToString()}\",";
                //    }
                //}
                //else
                //{
                    (object propertyValue, System.Type propertyType) = ReflectionVariable.GetPropertyValueWithType(selections[i].component, selections[i].propertyName, selections[i].propertyInnerVariableNames);
                    s += $"\"{propertyValue?.ToString()}\",";
                //}

            }
            records.Add(s);
        }
        catch (Exception e) { Debug.Log(e); }
    }
    public void RecordEnd()
    {
        records.Add("End");
        csvController.GetInstance().WriteCsv(records.ToArray(), Application.dataPath+"/record.csv");
        Debug.Log("Write success on : " + Application.dataPath + "/record.csv");
    }
}

public static class ReflectionVariable
{
    public static (object, System.Type) GetPropertyValueWithType(Component component, string propertyName, List<string> innerVariableNames)
    {
        object value = null;
        System.Type type = null;

        var propertyInfo = component.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (propertyInfo != null)
        {
            value = propertyInfo.GetValue(component, null);
            type = propertyInfo.PropertyType;
        }
        else
        {
            var fieldInfo = component.GetType().GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (fieldInfo != null)
            {
                value = fieldInfo.GetValue(component);
                type = fieldInfo.FieldType;
            }
        }

        foreach (var innerVariableName in innerVariableNames)
        {
            if (value != null)
            {
                var innerPropertyInfo = type.GetProperty(innerVariableName, BindingFlags.Public | BindingFlags.Instance);
                if (innerPropertyInfo != null)
                {
                    value = innerPropertyInfo.GetValue(value, null);
                    type = innerPropertyInfo.PropertyType;
                }
                else
                {
                    var innerFieldInfo = type.GetField(innerVariableName, BindingFlags.Public | BindingFlags.Instance);
                    if (innerFieldInfo != null)
                    {
                        value = innerFieldInfo.GetValue(value);
                        type = innerFieldInfo.FieldType;
                    }
                }
            }
        }

        return (value, type);
    }

    public static List<string> GetSubProperties(Component component, string propertyName, List<string> innerVariableNames)
    {
        var subProperties = new List<string>();
        var (propertyValue, propertyType) = GetPropertyValueWithType(component, propertyName, innerVariableNames);

        if (propertyValue != null && propertyType != null)
        {
            var fields = propertyType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var props = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

            subProperties.AddRange(fields.Select(field => field.Name));
            subProperties.AddRange(props.Select(prop => prop.Name));
        }

        return subProperties;
    }

    public static object GetSubPropertyValue(object propertyValue, string subPropertyName)
    {
        if (propertyValue == null) return null;

        var propertyType = propertyValue.GetType();
        var propertyInfo = propertyType.GetProperty(subPropertyName, BindingFlags.Public | BindingFlags.Instance);
        if (propertyInfo != null)
        {
            return propertyInfo.GetValue(propertyValue, null);
        }
        else
        {
            var fieldInfo = propertyType.GetField(subPropertyName, BindingFlags.Public | BindingFlags.Instance);
            if (fieldInfo != null)
            {
                return fieldInfo.GetValue(propertyValue);
            }
        }

        return null;
    }
}

[System.Serializable]
public class ComponentPropertySelection
{
    public GameObject gameObject;
    public Component component;
    public string propertyName;
    public List<string> propertyInnerVariableNames = new List<string>();
    public string customName;
}

[CustomEditor(typeof(TemporalVariableRecorder))]
public class CustomScriptEditor : Editor
{
    private ReorderableList reorderableList;
    private const float baseElementSpacing = 10f;
    private const int maxDepth = 3;

    private void OnEnable()
    {
        reorderableList = new ReorderableList(serializedObject,
            serializedObject.FindProperty("selections"),
            true, true, true, true);

        reorderableList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Component Property Selections");
        };

        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            var element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;

            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("gameObject"), GUIContent.none);

            var gameObject = element.FindPropertyRelative("gameObject").objectReferenceValue as GameObject;
            if (gameObject != null)
            {
                var componentProperty = element.FindPropertyRelative("component");
                var propertyNameProperty = element.FindPropertyRelative("propertyName");
                var propertyInnerVariableNamesProperty = element.FindPropertyRelative("propertyInnerVariableNames");
                var customNameProperty = element.FindPropertyRelative("customName");

                Component[] components = gameObject.GetComponents<Component>();
                string[] componentNames = components.Select(comp => comp.GetType().Name).ToArray();
                int currentIndex = Array.IndexOf(components, componentProperty.objectReferenceValue);

                int selectedIndex = EditorGUI.Popup(
                    new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + 2, rect.width, EditorGUIUtility.singleLineHeight),
                    "Component", currentIndex, componentNames);
                if (selectedIndex >= 0 && selectedIndex < components.Length)
                {
                    componentProperty.objectReferenceValue = components[selectedIndex];
                }

                if (componentProperty.objectReferenceValue != null)
                {
                    string[] propertyNames = GetComponentPropertyNames((Component)componentProperty.objectReferenceValue).ToArray();
                    int selectedPropertyIndex = Array.IndexOf(propertyNames, propertyNameProperty.stringValue);
                    int newSelectedPropertyIndex = EditorGUI.Popup(
                        new Rect(rect.x, rect.y + 2 * (EditorGUIUtility.singleLineHeight + 2), rect.width, EditorGUIUtility.singleLineHeight),
                        "Variable Name", selectedPropertyIndex, propertyNames);

                    if (newSelectedPropertyIndex >= 0 && newSelectedPropertyIndex < propertyNames.Length)
                    {
                        propertyNameProperty.stringValue = propertyNames[newSelectedPropertyIndex];
                    }

                    DrawInnerVariables(rect, propertyInnerVariableNamesProperty, componentProperty.objectReferenceValue.GetType(), propertyNameProperty.stringValue, 3);
                }

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y + (3 + propertyInnerVariableNamesProperty.arraySize) * (EditorGUIUtility.singleLineHeight + 2), rect.width, EditorGUIUtility.singleLineHeight),
                    customNameProperty, new GUIContent("Custom Variable Name", "The name recorded in csv"));
            }
        };

        reorderableList.elementHeightCallback = (int index) =>
        {
            var element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            var propertyInnerVariableNamesProperty = element.FindPropertyRelative("propertyInnerVariableNames");
            int depth = propertyInnerVariableNamesProperty.arraySize;
            return (4 + depth) * (EditorGUIUtility.singleLineHeight + 2) + baseElementSpacing; 
        };

        reorderableList.onAddCallback = (ReorderableList list) =>
        {
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("gameObject").objectReferenceValue = null;
            element.FindPropertyRelative("component").objectReferenceValue = null;
            element.FindPropertyRelative("propertyName").stringValue = "";
            element.FindPropertyRelative("propertyInnerVariableNames").ClearArray();
            element.FindPropertyRelative("customName").stringValue = "";
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        reorderableList.DoLayoutList();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawInnerVariables(Rect rect, SerializedProperty propertyInnerVariableNamesProperty, Type type, string propertyName, int depth)
    {
        if (string.IsNullOrEmpty(propertyName) || depth > maxDepth) return;

        FieldInfo selectedField = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
        PropertyInfo selectedProperty = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

        List<string> innerVariableNames = GetPropertyInnerVariableNames(selectedField, selectedProperty);

        for (int i = 0; i < propertyInnerVariableNamesProperty.arraySize; i++)
        {
            var innerVariableNameProperty = propertyInnerVariableNamesProperty.GetArrayElementAtIndex(i);
            int selectedInnerVariableIndex = innerVariableNames.IndexOf(innerVariableNameProperty.stringValue);
            int newSelectedInnerVariableIndex = EditorGUI.Popup(
                new Rect(rect.x, rect.y + depth * (EditorGUIUtility.singleLineHeight + 2), rect.width, EditorGUIUtility.singleLineHeight),
                "Inner Variable", selectedInnerVariableIndex, innerVariableNames.ToArray());

            if (newSelectedInnerVariableIndex >= 0 && newSelectedInnerVariableIndex < innerVariableNames.Count)
            {
                innerVariableNameProperty.stringValue = innerVariableNames[newSelectedInnerVariableIndex];

                if (i == propertyInnerVariableNamesProperty.arraySize - 1 && !string.IsNullOrEmpty(innerVariableNameProperty.stringValue) && depth < maxDepth)
                {
                    propertyInnerVariableNamesProperty.arraySize++;
                }
            }

            depth++;
        }

        if (propertyInnerVariableNamesProperty.arraySize == 0 || (!string.IsNullOrEmpty(propertyInnerVariableNamesProperty.GetArrayElementAtIndex(propertyInnerVariableNamesProperty.arraySize - 1).stringValue) && depth < maxDepth))
        {
            propertyInnerVariableNamesProperty.arraySize++;
        }
    }

    private List<string> GetPropertyInnerVariableNames(FieldInfo fieldInfo, PropertyInfo propertyInfo)
    {
        var variableNames = new List<string>();
        Type type = null;

        if (fieldInfo != null)
        {
            type = fieldInfo.FieldType;
        }
        else if (propertyInfo != null)
        {
            type = propertyInfo.PropertyType;
        }

        if (type != null)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            variableNames.AddRange(fields.Select(field => field.Name));

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.GetIndexParameters().Length == 0);
            variableNames.AddRange(props.Select(prop => prop.Name));
        }

        return variableNames;
    }

    private List<string> GetComponentPropertyNames(Component component)
    {
        var propertyNames = new List<string>();
        var type = component.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

        propertyNames.AddRange(fields.Select(field => field.Name));
        propertyNames.AddRange(props.Select(prop => prop.Name));

        return propertyNames;
    }
}

#region csv editing
public class csvController
{
    static csvController csv;
    public List<string[]> arrayData;

    private csvController()
    {
        arrayData = new List<string[]>();
    }

    public static csvController GetInstance()
    {
        if (csv == null)
        {
            csv = new csvController();
        }
        return csv;
    }

    public int loadFile(string fileName)
    {
        arrayData.Clear();
        StreamReader sr = null;
        try
        {
            string file_url = fileName;
            sr = File.OpenText(file_url);
            Debug.Log("File Find in " + file_url);
        }
        catch
        {
            Debug.Log("File cannot find!");
            return 0;
        }

        string line;
        int count = 0;
        while ((line = sr.ReadLine()) != null)
        {
            count++;
            arrayData.Add(line.Split(','));
        }
        sr.Close();
        sr.Dispose();
        return count;
    }

    public string getString(int row, int col)
    {
        return arrayData[row][col];
    }

    public int getInt(int row, int col)
    {
        return int.Parse(arrayData[row][col]);
    }

    public float getFloat(int row, int col)
    {
        return float.Parse(arrayData[row][col]);
    }

    public Vector3[] StartLoad(string filename)
    {
        int count = csvController.GetInstance().loadFile(filename);
        Vector3[] vs = new Vector3[count];
        for (int i = 1; i < count; i++)
        {
            vs[i - 1] = new Vector3(csvController.GetInstance().getFloat(i, 1), csvController.GetInstance().getFloat(i, 2), csvController.GetInstance().getFloat(i, 3));
        }
        return vs;
    }

    public void WriteCsv(string[] strs, string path, char delimiter = ',') 
    {
        if (!File.Exists(path))
        {
            File.Create(path).Dispose();
        }
        using (StreamWriter stream = new StreamWriter(path, false, Encoding.UTF8))
        {
            foreach (string str in strs)
            {
                string modifiedStr = str.Replace(',', delimiter);  
                if (modifiedStr != null)
                    stream.WriteLine(modifiedStr);
            }
        }
    }
}
#endregion