using StateKernel.ProjectModel;

namespace StateKernel.ProjectBundles;

/// <summary>
/// Describes the identity of a portable project bundle artifact.
/// </summary>
/// <param name="Project">The project document contained by the bundle.</param>
/// <param name="CreatedAtUtc">The UTC timestamp when the bundle was created.</param>
public sealed record ProjectBundleManifest(ProjectDocumentHeader Project, DateTimeOffset CreatedAtUtc);
