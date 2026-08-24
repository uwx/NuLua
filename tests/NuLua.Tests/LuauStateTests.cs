using System.Numerics;
using NuLua.Luau;

namespace NuLua.Tests;

public class LuauStateTests
{
    [Test]
    public async Task PushVectorAndToVectorRoundTripsComponents()
    {
        using var state = LuauState.Create();

        var input = new Vector3(1.5f, -2.25f, 3.75f);
        state.PushVector(input);

        await Assert.That(state.GetType(-1)).IsEqualTo(LuaValueType.Vector);
        var output = state.ToVector(-1);
        await Assert.That(output).IsEqualTo(input);
    }

    [Test]
    public async Task ToVectorThrowsWhenValueIsNotAVector()
    {
        using var state = LuauState.Create();
        state.PushNumber(1.0);
        await Assert.That(() => state.ToVector(-1)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task GetTypeReportsVectorAndBufferForLuauValues()
    {
        using var state = LuauState.Create();

        state.PushVector(new Vector3(1f, 2f, 3f));
        await Assert.That(state.GetType(-1)).IsEqualTo(LuaValueType.Vector);

        using var buffer = state.NewBuffer(4);
        state.PushValue(buffer.Reference);
        await Assert.That(state.GetType(-1)).IsEqualTo(LuaValueType.Buffer);
        state.Pop(1);
    }

    [Test]
    public async Task NewBufferReturnsBufferOfRequestedSize()
    {
        using var state = LuauState.Create();

        using var buffer = state.NewBuffer(16);

        await Assert.That(buffer.Length).IsEqualTo(16);
    }

    [Test]
    public async Task LuauBufferIndexerWritesAndReadsBytes()
    {
        using var state = LuauState.Create();

        using var buffer = state.NewBuffer(4);
        buffer[0] = 10;
        buffer[1] = 20;
        buffer[2] = 30;
        buffer[3] = 40;

        await Assert.That(buffer[0]).IsEqualTo((byte)10);
        await Assert.That(buffer[3]).IsEqualTo((byte)40);
    }

    [Test]
    public async Task LuauBufferCopyFromAndToArrayRoundTripsBytes()
    {
        using var state = LuauState.Create();

        using var buffer = state.NewBuffer(5);
        buffer.CopyFrom(new byte[] { 1, 2, 3, 4, 5 });

        var copy = buffer.ToArray();
        await Assert.That(copy.Length).IsEqualTo(5);
        await Assert.That(copy[0]).IsEqualTo((byte)1);
        await Assert.That(copy[4]).IsEqualTo((byte)5);
    }

    [Test]
    public async Task ToLuauBufferWrapsExistingBufferOnStack()
    {
        using var state = LuauState.Create();

        using var original = state.NewBuffer(3);
        original[0] = 7;
        original[1] = 8;
        original[2] = 9;

        state.PushValue(original.Reference);
        using var wrapped = state.ToBuffer(-1);
        state.Pop(1);

        await Assert.That(wrapped.Length).IsEqualTo(3);
        await Assert.That(wrapped[0]).IsEqualTo((byte)7);
        await Assert.That(wrapped[2]).IsEqualTo((byte)9);
    }

    [Test]
    public async Task LuauToLuaValueReturnsVectorTypeForVectors()
    {
        using var state = LuauState.Create();

        var input = new Vector3(4f, 5f, 6f);
        state.PushVector(input);

        var value = state.ToLuaValue(-1);
        await Assert.That(value.Type).IsEqualTo(LuaValueType.Vector);
        await Assert.That(value.Read<Vector3>()).IsEqualTo(input);
    }

    [Test]
    public async Task LuauToLuaValueReturnsBufferTypeForBuffers()
    {
        using var state = LuauState.Create();

        using var buffer = state.NewBuffer(2);
        buffer[0] = 100;
        buffer[1] = 200;

        state.PushValue(buffer.Reference);
        var value = state.ToLuaValue(-1);
        state.Pop(1);

        await Assert.That(value.Type).IsEqualTo(LuaValueType.Buffer);
        using var read = value.Read<LuauBuffer>();
        await Assert.That(read.Length).IsEqualTo(2);
        await Assert.That(read[0]).IsEqualTo((byte)100);
    }

    [Test]
    public async Task LuauPushHandlesVectorLuaValue()
    {
        using var state = LuauState.Create();

        var input = new Vector3(11f, 22f, 33f);
        state.Push(LuaValue.FromVector(input));

        await Assert.That(state.GetType(-1)).IsEqualTo(LuaValueType.Vector);
        await Assert.That(state.ToVector(-1)).IsEqualTo(input);
    }

    [Test]
    public async Task GetTypeReportsNonClassForOrdinaryValues()
    {
        // Luau's class/object types are gated behind the FFlag::DebugLuauUserDefinedClasses
        // compiler flag, which we cannot toggle from C#, so we only assert that ordinary
        // values report their own type here.
        using var state = LuauState.Create();

        state.PushNumber(1.0);
        await Assert.That(state.GetType(-1)).IsNotEqualTo(LuaValueType.Class);
        await Assert.That(state.GetType(-1)).IsNotEqualTo(LuaValueType.Object);

        state.PushString("hello");
        await Assert.That(state.GetType(-1)).IsNotEqualTo(LuaValueType.Class);
        await Assert.That(state.GetType(-1)).IsNotEqualTo(LuaValueType.Object);
    }

    [Test]
    public async Task ToClassThrowsWhenValueIsNotAClass()
    {
        using var state = LuauState.Create();
        state.PushNumber(1.0);
        await Assert.That(() => state.ToClass(-1)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ToObjectThrowsWhenValueIsNotAnObject()
    {
        using var state = LuauState.Create();
        state.PushNumber(1.0);
        await Assert.That(() => state.ToObject(-1)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task LuaValueFromClassAndFromObjectSetCorrectType()
    {
        var dummy = new DummyLuaObject();
        await Assert.That(LuaValue.FromClass(dummy).Type).IsEqualTo(LuaValueType.Class);
        await Assert.That(LuaValue.FromObject(dummy).Type).IsEqualTo(LuaValueType.Object);
    }

    [Test]
    public async Task PushPrimitiveAndToPrimitiveRoundTripsIdAndPayload()
    {
        using var state = LuauState.Create();

        byte[] payload = [1, 2, 3, 4, 5];
        state.PushPrimitive(3, payload);

        await Assert.That(state.GetType(-1)).IsEqualTo(LuaValueType.Primitive);

        var read = state.ToPrimitive(-1, out var id);
        // Materialize the span before any await (spans cannot cross await boundaries).
        var bytes = read.ToArray();
        await Assert.That(id).IsEqualTo(3);
        // The native API zero-pads the payload to LUA_PRIMITIVE_SIZE; only the prefix is meaningful.
        await Assert.That(bytes.Length).IsEqualTo(LuaValue.PrimitivePayloadSize);
        await Assert.That(bytes.AsSpan(0, payload.Length).SequenceEqual(payload)).IsTrue();
    }

    [Test]
    public async Task ToPrimitiveThrowsWhenValueIsNotAPrimitive()
    {
        using var state = LuauState.Create();
        state.PushNumber(1.0);
        await Assert.That(() => state.ToPrimitive(-1, out _)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task LuaValueFromPrimitivePushAndPopRoundTrips()
    {
        using var state = LuauState.Create();

        byte[] payload = [10, 20, 30, 40];
        var value = LuaValue.FromPrimitive(5, payload);

        await Assert.That(value.Type).IsEqualTo(LuaValueType.Primitive);
        await Assert.That(value.Read<PrimitiveValue>().Id).IsEqualTo(5);
        var primitiveValueValue = value.Read<PrimitiveValue>().Primitive;
        await Assert.That(((ReadOnlySpan<byte>)primitiveValueValue)[..payload.Length].SequenceEqual(payload)).IsTrue();
        await Assert.That(value.Read<ReadOnlyMemory<byte>>().Span[..payload.Length].SequenceEqual(payload)).IsTrue();

        state.Push(value);
        await Assert.That(state.GetType(-1)).IsEqualTo(LuaValueType.Primitive);

        var popped = state.Pop();
        await Assert.That(popped.Type).IsEqualTo(LuaValueType.Primitive);
        await Assert.That(popped.Read<PrimitiveValue>().Id).IsEqualTo(5);
        // Reading back from the VM returns the zero-padded 24-byte payload.
        var read = popped.Read<PrimitiveValue>().Primitive;
        var readSpan = (ReadOnlySpan<byte>)read;
        await Assert.That(readSpan.Length).IsEqualTo(LuaValue.PrimitivePayloadSize);
        var readSpan1 = (ReadOnlySpan<byte>)read;
        await Assert.That(readSpan1[..payload.Length].SequenceEqual(payload)).IsTrue();
    }

    [Test]
    public async Task PrimitiveIdThrowsForNonPrimitiveValue()
    {
        var value = LuaValue.FromNumber(1.0);
        await Assert.That(() => value.Read<PrimitiveValue>().Id).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task FromPrimitiveRejectsOversizedPayload()
    {
        var payload = new byte[LuaValue.PrimitivePayloadSize + 1];
        await Assert.That(() => LuaValue.FromPrimitive(0, payload))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task FromPrimitiveRejectsInvalidId()
    {
        await Assert.That(() => LuaValue.FromPrimitive(-1, []))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => LuaValue.FromPrimitive(LuaValue.PrimitiveIdLimit, []))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task GetPrimitiveMetatableReturnsFalseWhenUnset()
    {
        using var state = LuauState.Create();
        await Assert.That(state.GetPrimitiveMetatable(0, out _)).IsFalse();
    }

    [Test]
    public async Task SetPrimitiveMetatableThenGetRoundTrips()
    {
        using var state = LuauState.Create();
        using var metatable = state.CreateTable();
        metatable["__index"] = 42;

        state.SetPrimitiveMetatable(2, metatable);

        await Assert.That(state.GetPrimitiveMetatable(2, out var retrieved)).IsTrue();
        await Assert.That(retrieved).IsNotNull();
        retrieved!.Dispose();
    }

    [Test]
    public async Task SetPrimitiveMetatableThrowsWhenSetTwice()
    {
        using var state = LuauState.Create();
        using var metatable = state.CreateTable();

        state.SetPrimitiveMetatable(1, metatable);
        await Assert.That(() => state.SetPrimitiveMetatable(1, metatable))
            .Throws<InvalidOperationException>();
    }

    sealed class DummyLuaObject : ILuaObject
    {
        public LuaReference Reference => default;

        public void Dispose() { }
    }
}
