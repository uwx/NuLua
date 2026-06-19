namespace NuLua;

public enum LuaValueType : byte
{
    Nil,
    Boolean,
    LightUserData,
    Number,
    Vector,
    String,
    Table,
    Function,
    UserData,
    Thread,
    Buffer,
}
