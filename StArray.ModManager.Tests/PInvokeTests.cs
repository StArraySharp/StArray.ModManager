using StArray.ModManager.Il2Cpp;

namespace StArray.ModManager.Tests;


public class PInvokeTests2
{
    [Test]
    public void Test1()
    {
        Il2CppFunctions.SetIl2CppLibraryPath("kernel32.dll");
        Assert.That(!Il2CppDomain.Current.IsValid);
    }
}