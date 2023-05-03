using System.Runtime.InteropServices;

namespace CatScale.ReprocessTool;

public static class NativeFilterLib
{
    public delegate void StartOfEventHandler();
    public delegate void StablePhaseHandler(double length, double value);
    public delegate void EndOfEventHandler();
    
    [DllImport("filter_lib.so", EntryPoint = "register_handlers")]
    public static extern void RegisterHandlers(StartOfEventHandler startOfEvent, StablePhaseHandler stablePhase, EndOfEventHandler endOfEvent);
    
    [DllImport("filter_lib.so", EntryPoint = "filter_cascade_init")]
    public static extern void InitFilterCascade();
    
    [DllImport("filter_lib.so", EntryPoint = "filter_cascade_process")]
    public static extern double ProcessValueInFilterCascade(double input, double dt);
}