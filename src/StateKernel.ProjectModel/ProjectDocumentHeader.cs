using StateKernel.Domain.Projects;

namespace StateKernel.ProjectModel;

/// <summary>
/// Captures the stable identity and schema version of a persisted project document.
/// </summary>
/// <param name="ProjectId">The persisted project identifier.</param>
/// <param name="Name">The human-readable project name.</param>
/// <param name="FormatVersion">The schema version used by the persisted document.</param>
public sealed record ProjectDocumentHeader(ProjectId ProjectId, string Name, int FormatVersion);
