﻿using ParadoxNotion;
using ParadoxNotion.Design;
using Logger = ParadoxNotion.Services.Logger;

namespace FlowCanvas.Nodes
{
	[Description("Should always be used to return out of a Custom Function. The return value is only required if the Custom Function returns a value as well.")]
	[Category("Functions/Custom")]
	[ContextDefinedInputs(typeof(object))]
	public class Return : FlowControlNode{
		protected override void RegisterPorts(){
			var returnPort = AddValueInput<object>("Value");
			AddFlowInput(" ", (f)=>
			{

#if UNITY_EDITOR				
				if (f.Return == null){
					Fail("The 'Return' node should be called as part of a Custom Function node.");
					return;
				}
				var returnValue = returnPort.value;
				var returnType = returnValue != null? returnValue.GetType() : null;
				if ( f.ReturnType != null && !f.ReturnType.RTIsAssignableFrom(returnType) ){
					Fail(string.Format("Return Value is not of expected type '{0}'.", f.ReturnType.FriendlyName() ) );
					return;
				}

				if ( f.ReturnType == null && returnValue != null ){
					Logger.LogWarning("Function Returns a value, but no value is required", null, this);
				}
#endif

				f.Return(returnPort.value);
			});
		}
	}
}