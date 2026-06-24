using System.Text.Json.Serialization;

record TagChanges(
    [property: JsonPropertyName("tenantId")]        int TenantId,
    [property: JsonPropertyName("consolidations")]  List<Consolidation> Consolidations,
    [property: JsonPropertyName("deletions")]       List<string> Deletions,
    [property: JsonPropertyName("additions")]       List<Addition> Additions
);

record Consolidation(
    [property: JsonPropertyName("from")] string From,
    [property: JsonPropertyName("to")]   string To
);

record Addition(
    [property: JsonPropertyName("tag")]     string Tag,
    [property: JsonPropertyName("postIds")] List<int> PostIds
);
