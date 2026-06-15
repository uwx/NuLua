namespace NuLua;

public interface ILuaState : ILuaObject, IDisposable
{
    bool IsYieldable { get; }
    LuaThreadStatus Status { get; }

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

    void Copy(int fromIndex, int toIndex);
    void Rotate(int index, int n);

    void PushNil();
    void PushBoolean(bool value);
    void PushInteger(long value);
    void PushNumber(double value);
    void PushString(ReadOnlySpan<byte> utf8Str);
    void PushLightUserData(nint data);
    bool PushThread();
    void PushValue(int index);

    bool ToBoolean(int index);
    double ToNumber(int index);
    string ToString(int index);
    nint ToLightUserData(int index);
    ILuaState ToThread(int index);

    LuaValue GetGlobal(ReadOnlySpan<char> name);
    void SetGlobal(ReadOnlySpan<char> name, LuaValue value);

    LuaTable CreateTable(int initialArraySize = 0, int initialRecordsSize = 0);
    void GetTable(int index);
    void SetTable(int index);
    void Next(int index);

    void Arith(LuaArithmeticOperator op);
    void Compare(LuaComparisonOperator op);
    void Concat(int count);
    void Len(int index);
    void Call(int argCount, int returnCount);
    void Resume(int argCount);

    void LoadString(ReadOnlySpan<char> chunk, ReadOnlySpan<char> chunkName = default);
    void LoadString(ReadOnlySpan<byte> utf8Chunk, ReadOnlySpan<byte> utf8ChunkName = default);
}

public interface ILuaState<T> : ILuaState
    where T : ILuaState<T>
{
    T? From { get; }

    void XMove(T target, int count);
    new T ToThread(int index);
}
