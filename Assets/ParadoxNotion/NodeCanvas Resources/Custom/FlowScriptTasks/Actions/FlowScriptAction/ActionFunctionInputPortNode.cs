using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;
namespace FlowCanvas.Functions
{
    [DoNotList]
    [Name("Port")]
    [Icon("MacroIn")]
    [Description("Defines the Input ports of the Macro")]
    public class ActionFunctionInputPortNode : FlowNode, IPortChange
    {

        [SerializeField]
        protected string entryID;
        private ActionFunction function
        {
            get { return (ActionFunction)graph; }
        }

        protected override void RegisterPorts()
        {
            if (!string.IsNullOrEmpty(entryID))
            {
                var def = function.inputDefinitions.Find(x => x.ID == entryID);
                if (def != null)
                {
                    AddValueOutput(def.name, def.type, () => { return function.entryFunctionMap[def.ID](); }, def.ID);
                }
            }
        }
        public void Register(string id)
        {
            entryID = id;
            GatherPorts();
        }

        ////////////////////////////////////////
        ///////////GUI AND EDITOR STUFF/////////
        ////////////////////////////////////////
#if UNITY_EDITOR
        protected override void OnNodeInspectorGUI()
        {
            var def = function.inputDefinitions.Find(x => x.ID == entryID);
            if (def != null)
            {
                GUILayout.BeginHorizontal();
                def.name = UnityEditor.EditorGUILayout.TextField(def.name, GUILayout.Width(0), GUILayout.ExpandWidth(true));
                GUILayout.Label(def.type.FriendlyName(), GUILayout.Width(0), GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
            }
        }

        void IPortChange.PortsChnage()
        {
            var def = function.inputDefinitions.Find(x => x.ID == entryID);
            if (def == null)
            {
                function.RemoveNode(this);
            }
        }

#endif
    }
}