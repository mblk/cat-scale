using JetBrains.Annotations;

namespace CatScale.Service.Model.Toilet;

[PublicAPI]
public record UpdateToiletRequest
(
    string Name,
    string Description
);