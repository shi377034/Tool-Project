using System;
using System.Linq;
using System.Collections.Generic;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FlowCanvas.Functions
{
    public class ConditionalFunction : FlowGraph
    {
        FlowGraph flowGraph
        {
            get
            {
                return this;
            }
        }
        [System.Serializable]
        struct DerivedSerializationData
        {
            public bool LocalBlackboard;
            public List<DynamicPortDefinition> inputDefinitions;
            public List<DynamicPortDefinition> outputDefinitions;
        }
        public override object OnDerivedDataSerialization()
        {
            var data = new DerivedSerializationData();
            data.LocalBlackboard = _LocalBlackboard;
            data.inputDefinitions = this.inputDefinitions;
            data.outputDefinitions = this.outputDefinitions;
            return data;
        }

        public override void OnDerivedDataDeserialization(object data)
        {
            if (data is DerivedSerializationData)
            {
                this.inputDefinitions = ((DerivedSerializationData)data).inputDefinitions;
                this.outputDefinitions = ((DerivedSerializationData)data).outputDefinitions;
                this._LocalBlackboard = ((DerivedSerializationData)data).LocalBlackboard;
            }
        }
        ///The list of input port definition of the macro
        [SerializeField]
        public List<DynamicPortDefinition> inputDefinitions = new List<DynamicPortDefinition>();
        ///The list of output port definition of the macro
        [SerializeField]
        public List<DynamicPortDefinition> outputDefinitions = new List<DynamicPortDefinition>();
        [NonSerialized]
        public Action<Flow> entryAction;
        [NonSerialized]
        public Action<Flow> exitAction;
        [NonSerialized]
        public Func<bool> exitFunction;
        [NonSerialized]
        public Dictionary<string, Func<object>> entryFunctionMap = new Dictionary<string, Func<object>>(StringComparer.Ordinal);
        [NonSerialized]
        public Dictionary<string, Func<object>> exitFunctionMap = new Dictionary<string, Func<object>>(StringComparer.Ordinal);
 
        private ConditionalFunctionInputNode _entry;
        private ConditionalFunctionOutputNode _exit;
        [SerializeField]
        private bool _LocalBlackboard;
        public override bool useLocalBlackboard
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    _LocalBlackboard = EditorUtility.IsPersistent(this);
                }
#endif
                return _LocalBlackboard;
            }
        }

        ///The entry node of the Macro (input ports)
        public ConditionalFunctionInputNode entry
        {
            get
            {
                if (_entry == null)
                {
                    _entry = allNodes.OfType<ConditionalFunctionInputNode>().FirstOrDefault();
                    if (_entry == null)
                    {
                        _entry = AddNode<ConditionalFunctionInputNode>(new Vector2(-translation.x + 200, -translation.y + 200));
                    }
                }
                return _entry;
            }
        }
        ///The exit node of the Macro (output ports)
        public ConditionalFunctionOutputNode exit
        {
            get
            {
                if (_exit == null)
                {
                    _exit = allNodes.OfType<ConditionalFunctionOutputNode>().FirstOrDefault();
                    if (_exit == null)
                    {
                        _exit = AddNode<ConditionalFunctionOutputNode>(new Vector2(-translation.x + 600, -translation.y + 200));
                    }
                }
                return _exit;
            }
        }
        ///validates the entry & exit references
        protected override void OnGraphValidate()
        {
            base.OnGraphValidate();
            _entry = null;
            _exit = null;
            _entry = entry;
            _exit = exit;
            //create initial ports in case there are none in both entry and exit
            if (inputDefinitions.Count == 0 && outputDefinitions.Count == 0)
            {
                var defIn = new DynamicPortDefinition("OnCheck", typeof(Flow));
                var defOut = new DynamicPortDefinition("YieldReturn", typeof(Flow));
                inputDefinitions.Add(defIn);
                outputDefinitions.Add(defOut);
                entry.GatherPorts();
                exit.GatherPorts();
                var source = entry.GetOutputPort(defIn.ID);
                var target = exit.GetInputPort(defOut.ID);
                BinderConnection.Create(source, target);
            }
        }
        ///Adds a new input port definition to the Macro
		public bool AddInputDefinition(DynamicPortDefinition def)
        {
            if (inputDefinitions.Find(d => d.ID == def.ID) == null)
            {
                inputDefinitions.Add(def);
                return true;
            }
            return false;
        }

        ///Adds a new output port definition to the Macro
        public bool AddOutputDefinition(DynamicPortDefinition def)
        {
            if (outputDefinitions.Find(d => d.ID == def.ID) == null)
            {
                outputDefinitions.Add(def);
                return true;
            }
            return false;
        }
        public void PortChange()
        {
            var changes = allNodes.OfType<IPortChange>().ToList();
            foreach (var node in changes)
            {
                node.PortsChnage();
            }
        }
#if UNITY_EDITOR
        protected override GenericMenu OnCanvasContextMenu(GenericMenu menu, Vector2 mousePos)
        {
            menu = base.OnCanvasContextMenu(menu, mousePos);
            foreach (DynamicPortDefinition port in inputDefinitions)
            {
                menu.AddItem(new GUIContent("Input Port/" + port.name), false, () =>
                {
                    flowGraph.AddFlowNode<ConditionalFunctionInputPortNode>(mousePos, null, null).Register(port.ID);
                });
            }
            return menu;
        }
#endif
    }
}