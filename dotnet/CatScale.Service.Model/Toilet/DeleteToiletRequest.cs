using JetBrains.Annotations;

namespace CatScale.Service.Model.Toilet;

[PublicAPI]
public record DeleteToiletRequest
(
    int Id
);