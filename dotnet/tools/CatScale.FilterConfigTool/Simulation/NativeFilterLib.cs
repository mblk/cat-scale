using System.Runtime.InteropServices;

namespace CatScale.FilterConfigTool.Simulation;

public static class NativeFilterLib
{
    public delegate void StartOfEventHandler();
    public delegate void StablePhaseHandler(double length, double value);
    public delegate void EndOfEventHandler();
    public delegate void DebugHandler(string id, double value);
    
    [DllImport("filter_lib", EntryPoint = "register_handlers")]
    public static extern void RegisterHandlers(StartOfEventHandler startOfEvent, StablePhaseHandler stablePhase,
        EndOfEventHandler endOfEvent, DebugHandler debugHandler);
    
    [DllImport("filter_lib", EntryPoint = "filter_cascade_init")]
    public static extern void InitFilterCascade();
    
    [DllImport("filter_lib", EntryPoint = "filter_cascade_cleanup")]
    public static extern void CleanupFilterCascade();
    
    [DllImport("filter_lib", EntryPoint = "filter_cascade_process")]
    public static extern double ProcessValueInFilterCascade(double input, double dt);
}