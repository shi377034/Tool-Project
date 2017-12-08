using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;
namespace FlowCanvas.Functions
{
    [DoNotList]
    [Name("Input")]
    [Icon("MacroIn")]
    [Description("Defines the Input ports of the Function")]
    [Protected]
    public class ConditionalFunctionInputNode : FlowNode
    {

        private ConditionalFunction function
        {
            get { return (ConditionalFunction)graph; }
        }

        protected override void RegisterPorts()
        {
            for (var i = 0; i < function.inputDefinitions.Count; i++)
            {
                var def = function.inputDefinitions[i];
                if (def.type == typeof(Flow))
                {
                    function.entryAction = AddFlowOutput(def.name, def.ID).Call;
                }
                else
                {
                    AddValueOutput(def.name, def.type, () => { return function.entryFunctionMap[def.ID](); }, def.ID);
                }
            }
        }


        ////////////////////////////////////////
        ///////////GUI AND EDITOR STUFF/////////
        ////////////////////////////////////////
#if UNITY_EDITOR

        protected override UnityEditor.GenericMenu OnContextMenu(UnityEditor.GenericMenu menu)
        {
            return null;
        }

        protected override UnityEditor.GenericMenu OnDragAndDropPortContextMenu(UnityEditor.GenericMenu menu, Port port)
        {
            if (port is ValueInput)
            {
                menu.AddItem(new GUIContent(string.Format("Promote to new Input '{0}'", port.name)), false, () => {
                    var def = new DynamicPortDefinition(port.name, port.type);
                    if (function.AddInputDefinition(def))
                    {
                        GatherPorts();
                        BinderConnection.Create(GetOutputPort(def.ID), port);
                    }
                });
            }
            return menu;
        }

        protected override void OnNodeInspectorGUI()
        {

            if (GUILayout.Button("Add Value Input"))
            {
                EditorUtils.ShowPreferedTypesSelectionMenu(typeof(object), (t) =>
                {
                    function.AddInputDefinition(new DynamicPortDefinition(string.Format("{0} Input", t.FriendlyName()), t));
                    GatherPorts();
                });
            }
            var options = new EditorUtils.ReorderableListOptions();
            options.allowRemove = false;
            EditorUtils.ReorderableList(function.inputDefinitions, options, (i, r) =>
            {
                var def = function.inputDefinitions[i];
                GUILayout.BeginHorizontal();
                def.name = UnityEditor.EditorGUILayout.TextField(def.name, GUILayout.Width(0), GUILayout.ExpandWidth(true));
                GUILayout.Label(def.type.FriendlyName(), GUILayout.Width(0), GUILayout.ExpandWidth(true));
                if(def.type != typeof(Flow))
                {
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        function.inputDefinitions.RemoveAt(i);
                        function.PortChange();
                    }
                }    
                GUILayout.EndHorizontal();
            });

            if (GUI.changed)
            {
                GatherPorts();
            }
        }

#endif
    }
}