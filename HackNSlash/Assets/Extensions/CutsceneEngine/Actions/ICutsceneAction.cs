namespace Extensions.CutsceneEngine
{
    /**
     * <summary>
     * The ICutsceneAction interface defines a contract for cutscene actions, which are executed on cutscene actors during a cutscene.
     * Implementing this interface allows an object to specify how it should respond to cutscene actions, such as moving, playing animations, or triggering events.
     * This interface can be used by the cutscene system to invoke the appropriate actions on objects that implement it, enabling dynamic and context-specific behavior during cutscenes.
     * </summary>
     */
    public interface ICutsceneAction
    {
        /**
        * <summary>
        * Executes the cutscene action on the specified actor within the given cutscene context. This method should be implemented to define the specific behavior of the cutscene action when invoked by the cutscene system.
        * The cutscene system will call this method at the appropriate time during a cutscene, passing in the relevant actor and context information to allow for dynamic execution of the action based on the cutscene's requirements.
        * </summary>
        * <param name="actor">The cutscene actor on which the action should be executed. This parameter provides access to the actor's properties and methods, allowing for interaction with the actor during the execution of the action.</param>
        * <param name="context">The cutscene context that provides additional information about the current state of the cutscene, such as timing, parameters, and other relevant data. This parameter allows for context-specific behavior during the execution of the action.</param>
        */
        void Execute(ICutsceneActor actor, CutsceneContext context);
    }
}