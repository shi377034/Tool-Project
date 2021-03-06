﻿using NodeCanvas.Framework;

namespace FlowCanvas
{

    ///Add this component on a game object to be controlled by a Flow Graph script (a FlowScript)
    public class FlowScriptController : GraphOwner<FlowScript> {

		///Calls and returns a value of a custom function in the flowgraph
		public object CallFunction(string name, params object[] args){
			return behaviour.CallFunction(name, args);
		}
	}
}