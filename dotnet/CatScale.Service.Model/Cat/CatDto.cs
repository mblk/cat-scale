using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace CatScale.Service.Model.Cat;

[PublicAPI]
public record CatDto
(
    int Id,
    [Required(AllowEmptyStrings = false)] string Name,
    DateOnly DateOfBirth
);

[PublicAPI]
public record CreateCatRequest
(
    [Required(AllowEmptyStrings = false)] string Name,
    DateOnly DateOfBirth
);

[PublicAPI]
public record DeleteCatRequest
(
    int Id
);

[PublicAPI]
public record UpdateCatRequest
(
    [Required(AllowEmptyStrings = false)] string Name,
    DateOnly DateOfBirth
);
