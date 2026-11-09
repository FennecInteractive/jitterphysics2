using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

public enum GLDebugMessageSeverity : uint
{
    Notification = GLC.DEBUG_SEVERITY_NOTIFICATION,
    Low = GLC.DEBUG_SEVERITY_LOW,
    Medium = GLC.DEBUG_SEVERITY_MEDIUM,
    High = GLC.DEBUG_SEVERITY_HIGH
}

public enum GLDebugMessageSource : uint
{
    API = GLC.DEBUG_SOURCE_API,
    WindowSystem = GLC.DEBUG_SOURCE_WINDOW_SYSTEM,
    ShaderCompiler = GLC.DEBUG_SOURCE_SHADER_COMPILER,
    ThirdParty = GLC.DEBUG_SOURCE_THIRD_PARTY,
    Application = GLC.DEBUG_SOURCE_APPLICATION,
    Other = GLC.DEBUG_SOURCE_OTHER
}

public enum GLDebugMessageType : uint
{
    Error = GLC.DEBUG_TYPE_ERROR,
    DeprecatedBehavior = GLC.DEBUG_TYPE_DEPRECATED_BEHAVIOR,
    UndefinedBehavior = GLC.DEBUG_TYPE_UNDEFINED_BEHAVIOR,
    Portability = GLC.DEBUG_TYPE_PORTABILITY,
    Performance = GLC.DEBUG_TYPE_PERFORMANCE,
    Marker = GLC.DEBUG_TYPE_MARKER,
    PushGroup = GLC.DEBUG_TYPE_PUSH_GROUP,
    PopGroup = GLC.DEBUG_TYPE_POP_GROUP,
    Other = GLC.DEBUG_TYPE_OTHER
}

public struct GLDebugMessage
{
    public GLDebugMessageSeverity Severity;
    public GLDebugMessageSource Source;
    public GLDebugMessageType Type;
    public string Message;
    public uint Id;
}
