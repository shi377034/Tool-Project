using System;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace FlowCanvas.Functions
{
    [DoNotList]
    [Name("Return")]
    [Icon("MacroOut")]
    [Description("Defines the Output ports of the Macro")]
    [Protected]
    public class ConditionalFunctionOutputNode : FlowNode,IPortChange
    {
        private ConditionalFunction function
        {
            get { return (ConditionalFunction)graph; }
        }
        ValueInput<bool> value;
        protected override void RegisterPorts()
        {
            value = AddValueInput<bool>("Result");
            for (var i = 0; i < function.outputDefinitions.Count; i++)
            {
                var def = function.outputDefinitions[i];
                if (def.type == typeof(Flow))
                {
                    AddFlowInput(def.name, (f) =>
                    {
                        function.exitAction(f);
                    }
                    , def.ID);
                }else
                {
                    function.exitFunctionMap[def.ID] = AddValueInput(def.name, def.type, def.ID).GetValue;
                }
            }       
            function.exitFunction = () => { return value.value; };
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
            if (port is ValueOutput)
            {
                menu.AddItem(new GUIContent(string.Format("Promote to new Output '{0}'", port.name)), false, () =>
                {
                    var def = new DynamicPortDefinition(port.name, port.type);
                    if (function.AddOutputDefinition(def))
                    {
                        function.PortChange();
                        BinderConnection.Create(port, GetInputPort(def.ID));
                    }
                });
            }
            return menu;
        }
        protected override void OnNodeGUI()
        {
            base.OnNodeGUI();
        }
        protected override void OnNodeInspectorGUI()
        {
            var options = new EditorUtils.ReorderableListOptions();
            options.allowRemove = false;
            EditorUtils.ReorderableList(function.outputDefinitions, options, (i, r) =>
            {
                var def = function.outputDefinitions[i];
                GUILayout.BeginHorizontal();
                def.name = UnityEditor.EditorGUILayout.TextField(def.name, GUILayout.Width(0), GUILayout.ExpandWidth(true));
                GUILayout.Label(def.type.FriendlyName(), GUILayout.Width(0), GUILayout.ExpandWidth(true));
                if (def.type != typeof(Flow))
                {
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        function.outputDefinitions.RemoveAt(i);
                        function.PortChange();
                    }
                }
                GUILayout.EndHorizontal();
            });
        }

        void IPortChange.PortsChnage()
        {
            GatherPorts();
        }
#endif
    }
}