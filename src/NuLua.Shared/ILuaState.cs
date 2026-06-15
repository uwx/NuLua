namespace NuLua;

public interface ILuaState : ILuaObject, IDisposable
{
    nint AsPointer();

    void OpenLibraries();
    void OpenBaseLibrary();
    void OpenTableLibrary();
    void OpenStringLibrary();
    void OpenMathLibrary();
    void OpenCoroutineLibrary();
    void OpenPackageLibrary();
    void OpenDebugLibrary();

    int GetTop();
    void SetTop(int index);
    int GetAbsIndex(int index);
    LuaType GetType(int index);

    void PushNil();
    void PushBoolean(bool value);
    void PushInteger(long value);
    void PushNumber(double value);
    void PushString(ReadOnlySpan<char> str);
    void PushString(ReadOnlySpan<byte> utf8Str);
    void PushLightUserData(nint data);
    void PushValue(int index);

    bool ToBoolean(int index);
    double ToNumber(int index);
    string ToString(int index);
    nint ToLightUserData(int index);

    LuaValue GetGlobal(ReadOnlySpan<char> name);
    void SetGlobal(ReadOnlySpan<char> name, LuaValue value);

    LuaTable CreateTable(int initialArraySize = 0, int initialRecordsSize = 0);
    void GetTable(int index);
    void SetTable(int index);

    void Arith(LuaArithmeticOperator op);
    void Compare(LuaComparisonOperator op);
    void Concat(int count);
    void Len(int index);
    void Call(int argCount, int returnCount);

    void LoadString(ReadOnlySpan<char> chunk, ReadOnlySpan<char> chunkName = default);
    void LoadString(ReadOnlySpan<byte> utf8Chunk, ReadOnlySpan<byte> utf8ChunkName = default);
}

public interface ILuaState<T> : ILuaState
    where T : ILuaState<T>
{
    void XMove(T target, int count);
}
