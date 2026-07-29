using StArray.ModManager.Il2Cpp;
using StArray.ModManager.RuntimeAbstractions;

namespace StArray.ModManager.Tests;

[TestFixture]
[NonParallelizable]
public sealed class Il2CppRuntimeRegressionTests
{
    private RuntimeBackend _originalBackend;

    [SetUp]
    public void SetUp()
    {
        _originalBackend = RuntimeManager.Backend;
        RuntimeManager.SetBackend(RuntimeBackend.Il2Cpp);
    }

    [TearDown]
    public void TearDown()
    {
        RuntimeManager.SetBackend(_originalBackend);
    }

    [Test]
    public void CurrentCachesDomainAndBalancesNestedAttachment()
    {
        var fake = new FakeIl2CppRuntimeApi();
        using var scope = Il2CppRuntimeApi.OverrideForTesting(fake);

        var first = Il2CppDomain.Current;
        var second = Il2CppDomain.Current;

        Assert.That(first, Is.Not.Null);
        Assert.That(second, Is.SameAs(first));

        first!.ThreadAttach();
        second!.ThreadAttach();
        first.ThreadDetach();

        Assert.Multiple(() =>
        {
            Assert.That(fake.DomainGetCalls, Is.EqualTo(1));
            Assert.That(fake.ThreadAttachCalls, Is.EqualTo(1));
            Assert.That(fake.ThreadDetachCalls, Is.Zero);
        });

        second.ThreadDetach();
        Assert.That(fake.ThreadDetachCalls, Is.EqualTo(1));
    }

    [Test]
    public void ExistingIl2CppThreadIsNeverDetachedByWrapper()
    {
        var fake = new FakeIl2CppRuntimeApi { CurrentThread = 0x4444 };
        using var scope = Il2CppRuntimeApi.OverrideForTesting(fake);
        var domain = Il2CppDomain.Current!;

        domain.ThreadAttach();
        domain.ThreadDetach();

        Assert.Multiple(() =>
        {
            Assert.That(fake.ThreadAttachCalls, Is.Zero);
            Assert.That(fake.ThreadDetachCalls, Is.Zero);
        });
    }

    [Test]
    public void CurrentRetriesWhenDomainIsNotReadyYet()
    {
        var fake = new FakeIl2CppRuntimeApi { Domain = 0 };
        using var scope = Il2CppRuntimeApi.OverrideForTesting(fake);

        Assert.That(Il2CppDomain.Current, Is.Null);
        fake.Domain = 0x100;

        Assert.Multiple(() =>
        {
            Assert.That(Il2CppDomain.Current, Is.Not.Null);
            Assert.That(fake.DomainGetCalls, Is.EqualTo(2));
        });
    }

    [Test]
    public void Il2CppArrayDataStartsAfterBoundsAndLength()
    {
        var fake = new FakeIl2CppRuntimeApi { UnboxedObject = 0x1000 };
        using var scope = Il2CppRuntimeApi.OverrideForTesting(fake);

        var array = new RuntimeArray((nint)0x2000);

        Assert.That(array.DataPtr, Is.EqualTo((nint)(0x1000 + nint.Size * 2)));
    }

    [Test]
    public void Il2CppReferenceFieldUsesWriteBarrierAwareSetter()
    {
        var fake = new FakeIl2CppRuntimeApi { ReferenceField = true };
        using var scope = Il2CppRuntimeApi.OverrideForTesting(fake);
        var field = new Il2CppField(0x3000);

        field.SetValue(0x4000, (nint)0x5000);

        Assert.Multiple(() =>
        {
            Assert.That(fake.ObjectFieldSetCalls, Is.EqualTo(1));
            Assert.That(fake.ValueFieldSetCalls, Is.Zero);
            Assert.That(fake.LastObject, Is.EqualTo((nint)0x4000));
            Assert.That(fake.LastField, Is.EqualTo((nint)0x3000));
            Assert.That(fake.LastObjectValue, Is.EqualTo((nint)0x5000));
        });
    }

    [TestCase(0x0e, true)]
    [TestCase(0x12, true)]
    [TestCase(0x14, true)]
    [TestCase(0x1c, true)]
    [TestCase(0x1d, true)]
    [TestCase(0x0f, false)]
    [TestCase(0x11, false)]
    public void Il2CppFieldTypeClassificationDoesNotTreatPointersAsObjects(int typeCode, bool expected)
    {
        Assert.That(NativeIl2CppRuntimeApi.IsDirectReferenceType(typeCode), Is.EqualTo(expected));
    }

    [Test]
    public void Il2CppValueFieldUsesMetadataAwareFieldApi()
    {
        var fake = new FakeIl2CppRuntimeApi();
        using var scope = Il2CppRuntimeApi.OverrideForTesting(fake);
        var field = new Il2CppField(0x3000);

        field.SetValue(0x4000, 123);

        Assert.Multiple(() =>
        {
            Assert.That(fake.ValueFieldSetCalls, Is.EqualTo(1));
            Assert.That(fake.ObjectFieldSetCalls, Is.Zero);
        });
    }

    [Test]
    public void RuntimeInvokePropagatesFormattedIl2CppException()
    {
        var fake = new FakeIl2CppRuntimeApi
        {
            InvokeException = 0x6000,
            ExceptionMessage = "System.InvalidOperationException: failed",
            ExceptionStackTrace = "at Game.Run()",
        };
        using var scope = Il2CppRuntimeApi.OverrideForTesting(fake);
        var method = new Il2CppMethod(0x7000);

        var exception = Assert.Throws<Il2CppInvocationException>(() => method.Invoke(0x8000));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.NativeException, Is.EqualTo((nint)0x6000));
            Assert.That(exception.Message, Does.Contain("System.InvalidOperationException: failed"));
            Assert.That(exception.NativeStackTrace, Is.EqualTo("at Game.Run()"));
            Assert.That(exception.ToString(), Does.Contain("at Game.Run()"));
        });
    }

    [Test]
    public void UnixProbeUsesNoLoadAndBalancesReturnedHandle()
    {
        var closed = nint.Zero;

        var loaded = RuntimeManager.ProbeUnixLibrary(
            "libil2cpp.so",
            (_, flags) =>
            {
                Assert.That(flags, Is.EqualTo(RuntimeManager.RtldNow | RuntimeManager.RtldNoLoad));
                return 0x9000;
            },
            handle => closed = handle);

        Assert.Multiple(() =>
        {
            Assert.That(RuntimeManager.RtldNoLoad, Is.EqualTo(0x4));
            Assert.That(loaded, Is.True);
            Assert.That(closed, Is.EqualTo((nint)0x9000));
        });
    }

    [Test]
    public void UnixProbeDoesNotCloseMissingLibrary()
    {
        var closeCalls = 0;

        var loaded = RuntimeManager.ProbeUnixLibrary(
            "missing.so",
            (_, _) => 0,
            _ => closeCalls++);

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.False);
            Assert.That(closeCalls, Is.Zero);
        });
    }

    private sealed class FakeIl2CppRuntimeApi : IIl2CppRuntimeApi
    {
        public nint Domain { get; set; } = 0x100;
        public nint CurrentThread { get; init; }
        public nint UnboxedObject { get; init; }
        public bool ReferenceField { get; init; }
        public nint InvokeResult { get; init; }
        public nint InvokeException { get; init; }
        public string ExceptionMessage { get; init; } = "native exception";
        public string ExceptionStackTrace { get; init; } = "native stack";

        public int ThreadAttachCalls { get; private set; }
        public int ThreadDetachCalls { get; private set; }
        public int DomainGetCalls { get; private set; }
        public int ObjectFieldSetCalls { get; private set; }
        public int ValueFieldSetCalls { get; private set; }
        public nint LastObject { get; private set; }
        public nint LastField { get; private set; }
        public nint LastObjectValue { get; private set; }

        public nint DomainGet()
        {
            DomainGetCalls++;
            return Domain;
        }
        public nint ThreadCurrent() => CurrentThread;

        public nint ThreadAttach(nint domain)
        {
            ThreadAttachCalls++;
            return 0x200;
        }

        public void ThreadDetach(nint thread) => ThreadDetachCalls++;
        public nint ObjectUnbox(nint obj) => UnboxedObject;
        public int FieldGetFlags(nint field) => 0;
        public bool IsReferenceField(nint field) => ReferenceField;
        public T GetFieldValue<T>(nint obj, nint field, bool isStatic) where T : unmanaged => default;

        public void SetFieldValue<T>(nint obj, nint field, bool isStatic, T value) where T : unmanaged
        {
            ValueFieldSetCalls++;
        }

        public void SetObjectFieldValue(nint obj, nint field, nint value)
        {
            ObjectFieldSetCalls++;
            LastObject = obj;
            LastField = field;
            LastObjectValue = value;
        }

        public nint RuntimeInvoke(nint method, nint obj, nint[]? args, out nint exception)
        {
            exception = InvokeException;
            return InvokeResult;
        }

        public string FormatException(nint exception) => ExceptionMessage;
        public string FormatStackTrace(nint exception) => ExceptionStackTrace;
    }
}
