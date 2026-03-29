namespace StateKernel.NodeSet;

/// <summary>
/// Defines baseline options for importing an OPC UA NodeSet into StateKernel.
/// </summary>
/// <param name="PreserveNamespaceUris">
/// Indicates whether original namespace URIs should be preserved during import.
/// </param>
/// <param name="IncludeTypeDefinitions">
/// Indicates whether type definitions should be included alongside instantiated nodes.
/// </param>
public sealed record NodeSetImportOptions(bool PreserveNamespaceUris, bool IncludeTypeDefinitions);
