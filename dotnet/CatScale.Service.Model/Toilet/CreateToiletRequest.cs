using JetBrains.Annotations;

namespace CatScale.Service.Model.Toilet;

[PublicAPI]
public record CreateToiletRequest
(
    string Name,
    string Description
);