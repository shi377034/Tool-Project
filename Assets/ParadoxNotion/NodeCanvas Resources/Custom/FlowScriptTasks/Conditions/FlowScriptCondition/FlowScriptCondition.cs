using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;
using FlowCanvas;
using System.Collections.Generic;
using FlowCanvas.Functions;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NodeCanvas.Tasks.Conditions
{
    [Name("FlowScript")]
    [Category("✫ FlowScript")]
    public class FlowScriptCondition : ConditionTask
    {
        [System.Serializable]
        class InternalParameter
        {
            public string ID;
            public string Name;
            public BBParameter Parameter;
        }
        [SerializeField]
        [Name("Function")]
        public BBParameter<ConditionalFunction> Reference;
        [SerializeField]
        private string title = "Check FlowScript";
        private ConditionalFunction Instance = null;
        [SerializeField]
        private List<InternalParameter> ParameterList = new List<InternalParameter>();
        [SerializeField]
        private List<InternalParameter> OutList = new List<InternalParameter>();
        [System.NonSerialized]
        private bool result = false;
        protected override string info
        {
            get
            {
                return title;
            }
        }
        public Graph graph
        {
            get
            {
                if (ownerSystem.contextObject is Graph)
                {
                    return (Graph)ownerSystem.contextObject;
                }
                if (ownerSystem.contextObject is GraphOwner)
                {
                    return (ownerSystem.contextObject as GraphOwner).graph;
                }
                return null;
            }
        }
        public void CheckInstance()
        {
            if (Reference.value == Instance) return;//引用对象和实例相同，返回实例
            //否则从引用对象copy实例
            Instance = Graph.Clone(Reference.value);
            Instance.agent = agent;
            Instance.blackboard = blackboard;
            Instance.UpdateReferences();
            foreach(var param in ParameterList)
            {
                param.Parameter.bb = blackboard;
                Instance.entryFunctionMap[param.ID] = () => { return param.Parameter.value; };
            }
            Instance.exitAction = (f) =>
            {
                for(int i=0;i<OutList.Count;++i)
                {
                    var p = OutList[i];
                    if (p.Parameter == null) continue;
                    p.Parameter.value = Instance.exitFunctionMap[p.ID]();
                }
                result = Instance.exitFunction();
            };
            Instance.StartGraph(agent, blackboard, false);     
        }
        protected override bool OnCheck()
        {
            if (Instance == null)
            {
                CheckInstance();
            }
            if (Instance != null && Instance.isRunning)
            {
                Instance.entryAction(Flow.New);
            }
            return result;         
        }
#if UNITY_EDITOR
        protected override void OnTaskInspectorGUI()
        {
            EditorUtils.BBParameterField(new GUIContent("Function"), Reference);
            title = EditorGUILayout.TextField("Title", title);
            if (Reference.value == null)
            {
                if (!Application.isPlaying && GUILayout.Button("CREATE NEW"))
                {
                    Reference.value = EditorUtils.CreateAsset<ConditionalFunction>(true);
                }
            }
            else
            {
                GUI.backgroundColor = EditorUtils.lightBlue;
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("EDIT"))
                {
                    if(graph != null)
                    {
                        graph.currentChildGraph = Application.isPlaying ? Instance : Reference.value;
                    }              
                }
                if(GUILayout.Button("REGISTER"))
                {
                    RegisterParameter();
                }
                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;
                var target = Application.isPlaying ? Instance : Reference.value;
                var options = new EditorUtils.ReorderableListOptions();
                options.allowRemove = false;
                EditorUtils.TitledSeparator("In");
                EditorUtils.ReorderableList(ParameterList, options, (i, r) =>
                {
                    var def = ParameterList[i];
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(def.Name, GUILayout.ExpandWidth(true));
                    EditorUtils.BBParameterField(GUIContent.none, def.Parameter);
                    GUILayout.EndHorizontal();
                });
                EditorUtils.TitledSeparator("Out");
                EditorUtils.ReorderableList(OutList, options, (i, r) =>
                {
                    var def = OutList[i];
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(def.Name, GUILayout.ExpandWidth(true));
                    EditorUtils.BBParameterField(GUIContent.none, def.Parameter);
                    GUILayout.EndHorizontal();
                });
            }

        }
#endif
      
        /// <summary>
        /// 刷新节点
        /// </summary>
        /// <param name="ownerSystem"></param>
        public override void OnValidate(ITaskSystem ownerSystem)
        {       
            base.OnValidate(ownerSystem);
            RegisterParameter();
        }
        void RegisterParameter()
        {
            var target = Application.isPlaying ? Instance : Reference.value;
            if (target == null) return;
            for (int i = 0; i < target.inputDefinitions.Count; ++i)
            {
                var def = target.inputDefinitions[i];
                if (def.type == typeof(Flow)) continue;
                InternalParameter param = ParameterList.Find(x => x.ID == def.ID);
                if (param == null)
                {
                    param = new InternalParameter();
                    param.ID = def.ID;
                    param.Name = def.name;
                    param.Parameter = BBParameter.CreateInstance(def.type, blackboard);
                    ParameterList.Add(param);
                }
                else
                {
                    param.Name = def.name;
                    param.Parameter.bb = blackboard;
                }
            }
            for (int i = 0; i < target.outputDefinitions.Count; ++i)
            {
                var def = target.outputDefinitions[i];
                if (def.type == typeof(Flow)) continue;
                InternalParameter param = OutList.Find(x => x.ID == def.ID);
                if (param == null)
                {
                    param = new InternalParameter();
                    param.ID = def.ID;
                    param.Name = def.name;
                    param.Parameter = BBParameter.CreateInstance(def.type, blackboard);
                    OutList.Add(param);
                }
                else
                {
                    param.Name = def.name;
                    param.Parameter.bb = blackboard;
                }
            }
            ParameterList.RemoveAll(x =>
            {
                var def = target.inputDefinitions.Find(d => d.type != typeof(Flow) && d.ID == x.ID);
                if (def == null)
                {
                    return true;
                }
                return false;
            });
            OutList.RemoveAll(x =>
            {
                var def = target.outputDefinitions.Find(d => d.type != typeof(Flow) && d.ID == x.ID);
                if (def == null)
                {
                    return true;
                }
                return false;
            });
        }
    }
}