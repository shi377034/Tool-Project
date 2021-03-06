using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using ParadoxNotion;
using ParadoxNotion.Design;
using NodeCanvas.Framework;

using FlowCanvas.Macros;
using FlowCanvas.Nodes;


namespace FlowCanvas{

	///Base class for flow graphs.
	[GraphInfo(
		packageName = "FlowCanvas",
		docsURL = "http://flowcanvas.paradoxnotion.com/documentation/",
		resourcesURL = "http://flowcanvas.paradoxnotion.com/downloads/",
		forumsURL = "http://flowcanvas.paradoxnotion.com/forums-page/"
		)]
	[System.Serializable]
	abstract public class FlowGraph : Graph {

		private bool hasInitialized;
		private List<IUpdatable> updatableNodes;
		private Dictionary<string, CustomFunctionEvent> functions;
		private Dictionary<System.Type, Component> cachedAgentComponents = new Dictionary<System.Type, Component>();

		public override System.Type baseNodeType{ get {return typeof(FlowNode);} }
		public override bool useLocalBlackboard{ get {return false;} }
		sealed public override bool requiresAgent{	get {return false;} }
		sealed public override bool requiresPrimeNode { get {return false;} }
		sealed public override bool autoSort{ get {return false;} }

		///Calls and returns a value of a custom function in the flowgraph
		public T CallFunction<T>(string name, params object[] args){
			return (T)CallFunction(name, args);
		}

		///Calls and returns a value of a custom function in the flowgraph
		public object CallFunction(string name, params object[] args){
			CustomFunctionEvent func = null;
			if (functions.TryGetValue(name, out func)){
				return func.Invoke(new Flow(), args);
			}
			return null;
		}

		///Returns cached component type from graph agent
		public UnityEngine.Object GetAgentComponent(System.Type type){
			if (agent == null){
				return null;
			}

			if (type == typeof(GameObject)){
				return agent.gameObject;
			}

			if (type == typeof(Transform)){
				return agent.transform;
			}

			if (type == typeof(Component)){
				return agent;
			}

			Component component = null;
			if (cachedAgentComponents.TryGetValue(type, out component)){
				return component;
			}

			if (typeof(Component).RTIsAssignableFrom(type)){
				component = agent.GetComponent(type);
			}

			return cachedAgentComponents[type] = component;
		}

		//...
		protected override void OnGraphStarted(){

			if (!hasInitialized){
				updatableNodes = new List<IUpdatable>();
				functions = new Dictionary<string, CustomFunctionEvent>(System.StringComparer.Ordinal);
			}

			for (var i = 0; i < allNodes.Count; i++){
				var node = allNodes[i];
				if (node is MacroNodeWrapper){
					var macroNode = (MacroNodeWrapper)node;
					if (macroNode.macro != null){
						macroNode.CheckInstance();
						macroNode.macro.StartGraph(agent, blackboard, false, null);
					}
				}

				if (!hasInitialized){
					if (node is IUpdatable){
						updatableNodes.Add( (IUpdatable)node );
					}
					if (node is CustomFunctionEvent){
						var func = (CustomFunctionEvent)node;
						functions[func.identifier] = func;
					}
				}
			}


			if (!hasInitialized){
				//2nd pass after macros have been init
				for (var i = 0; i < allNodes.Count; i++){
					if (allNodes[i] is FlowNode){
						var flowNode = (FlowNode)allNodes[i];
						flowNode.AssignSelfInstancePort();
						flowNode.BindPorts();
					}
				}
			}

			hasInitialized = true;
		}

		//Update IUpdatable nodes. Basicaly for events like Input, Update etc
		//This is the only thing that udpates per-frame
		protected override void OnGraphUpdate(){
			if (updatableNodes != null && updatableNodes.Count > 0){
				for (var i = 0; i < updatableNodes.Count; i++){
					updatableNodes[i].Update();
				}
			}
		}

		//...
		protected override void OnGraphStoped(){
			for (var i = 0; i < allNodes.Count; i++){
				var node = allNodes[i];
				if (node is MacroNodeWrapper){
					var macroNode = (MacroNodeWrapper)node;
					if (macroNode.macro != null){
						macroNode.macro.Stop();
					}
				}
			}
		}


		//Returns whether there is a CustomObjectWrapper implemented for target type
		static System.Type[] FindCustomObjectWrappers(System.Type targetType){
			var results = new List<System.Type>();
			foreach(var type in ReflectionTools.GetAllTypes()){
				if (type.IsSubclassOf(typeof(CustomObjectWrapper))){
					var args = type.BaseType.GetGenericArguments();
					if (args.Length == 1 && args[0] == targetType ){
						results.Add(type);
					}
				}
			}
			return results.ToArray();
		}


		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		protected override bool canAcceptVariableDrops{get{return true;}}

		//Append menu items in canvas right click context menu
		protected override UnityEditor.GenericMenu OnCanvasContextMenu(UnityEditor.GenericMenu menu, Vector2 mousePos){
			menu = this.AppendSimplexNodesMenu(menu, "Functions/Implemented", mousePos, null, null);
			menu = this.AppendVariableNodesMenu(menu, "Variables", mousePos, null, null);
			menu = this.AppendAllReflectionNodesMenu(menu, "Functions/Reflected", mousePos, null, null);
			menu = this.AppendMacroNodesMenu(menu, "MACROS", mousePos, null, null);
			menu = this.AppendMenuCallbackReceivers(menu, "", mousePos, null, null);
			return menu;
		}

		//Create and set a UnityObject variable node on drop
		protected override void OnDropAccepted(Object o, Vector2 mousePos){
			
			if (o == null){
				return;
			}

			if ( UnityEditor.EditorUtility.IsPersistent(this) && !UnityEditor.EditorUtility.IsPersistent(o) ){
				Debug.LogError("This Graph is an asset. The dragged object is a scene reference. The reference will not persist");
			}

			//macro?
			if (o is Macro){
				this.AddMacroNode( (Macro)o, mousePos, null, null );
				return;
			}

			//wrappable object?
			var wrapperTypes = FindCustomObjectWrappers(o.GetType());
			var firstWrapperType = wrapperTypes.FirstOrDefault( t => t.IsDefined(typeof(Nodes.CustomObjectWrapper.ExplusiveWrapperAttribute), true ));

			//show reflection
			UnityEditor.GenericMenu menu = new UnityEditor.GenericMenu();
			var type = o.GetType();
			if (firstWrapperType != null){
				this.AddObjectWrapper(firstWrapperType, mousePos, null, o);
				return;
			}
			menu.AddItem( new GUIContent( string.Format("Make Variable ({0})", o.GetType().FriendlyName()) ), false, (x)=>{ this.AddVariableGet(type, null, mousePos, null, x);}, o );
			foreach(var _wrapperType in wrapperTypes){
				var wrapperType = _wrapperType;
				menu.AddItem( new GUIContent( string.Format("Add Wrapper ({0})", wrapperType.FriendlyName()) ), false, (x)=>{ this.AddObjectWrapper(wrapperType, mousePos, null, (UnityEngine.Object)x);}, o );
			}
			menu.AddSeparator("/");
			menu = this.AppendTypeReflectionNodesMenu(menu, type, "", mousePos, null, o);

			if (o is GameObject){
				var go = (GameObject)o;
				foreach(var component in go.GetComponents<Component>().Where(c => c.hideFlags == 0)){
					var cType = component.GetType();
					var category = cType.Name + "/";
					menu.AddItem(new GUIContent(string.Format(category + "Make Variable ({0})", cType.Name)), false, (x)=>{ this.AddVariableGet(cType, null, mousePos, null, x); }, component );
					foreach(var _wrapperType in wrapperTypes){
						var wrapperType = _wrapperType;
						menu.AddItem( new GUIContent( string.Format("Add Wrapper ({0})", wrapperType.FriendlyName()) ), false, (x)=>{ this.AddObjectWrapper(wrapperType, mousePos, null, (UnityEngine.Object)x);}, o );
					}
					menu = this.AppendTypeReflectionNodesMenu(menu, cType, "", mousePos, null, component);
				}
			}

			menu.ShowAsContext();
			Event.current.Use();
		}


		protected override void OnVariableDropInGraph(Variable variable, Event e, Vector2 mousePos){
			if (variable != null){
				var menu = new UnityEditor.GenericMenu();
				menu.AddItem(new GUIContent("Get " + variable.name), false, ()=>{ this.AddVariableGet(variable.varType, variable.name, mousePos, null, null); });
				menu.AddItem(new GUIContent("Set " + variable.name), false, ()=>{ this.AddVariableSet(variable.varType, variable.name, mousePos, null, null); });
				menu.ShowAsContext();
				e.Use();
			}
		}

		#endif
	}
}