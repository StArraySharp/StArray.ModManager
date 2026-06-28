using System.Text.Json.Serialization;

namespace StArray.ModManager.Manager;

[JsonSerializable(typeof(ModManagerConfig))]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(Dictionary<string, System.Text.Json.JsonElement>))]
/// <summary>System.Text.Json 源生成上下文，为 AOT 提供预编译序列化代码</summary>
[JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true)]
public partial class ModManagerJsonContext : JsonSerializerContext
{ }
