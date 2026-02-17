using System;

namespace Extensions.CutsceneEngine
{
    /**
     * <summary>
     * The ActorMethodAction class represents a cutscene action that invokes a method on a cutscene actor.
     * It allows for dynamic method invocation on actors during cutscenes, enabling designers to trigger specific behaviors or animations on actors by calling their methods with specified parameters.
     * This class uses reflection to invoke the method, so it can call any public method on the actor's adapter as long as the method name and parameters are correctly specified.
     * </summary>
     */
    [Serializable]
    public class ActorMethodAction : ICutsceneAction
    {
        public string MethodName;
        public SerializedCutsceneParameter[] Parameters;

        public void Execute(ICutsceneActor actor, CutsceneContext context)
        {
            var adapter = actor.GetCutsceneAdapter();
            object[] args = Array.ConvertAll(Parameters, p => p.GetValue());
            adapter.Invoke(MethodName, args);
        }
    }
}