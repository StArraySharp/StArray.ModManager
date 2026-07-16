set -e
cd $(dirname $0)/..
dotnet build StArray.ModManager.Windows/StArray.ModManager.Windows.csproj
cp StArray.ModManager.Windows/bin/Debug/net10.0/StArray.ModManager*.dll /c/Users/StArray/Desktop/3.1.1_Il2Cpp/Corehold/managed
/c/Users/StArray/Desktop/3.1.1_Il2Cpp/ADOFAI.exe