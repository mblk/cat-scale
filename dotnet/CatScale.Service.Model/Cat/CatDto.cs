using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace CatScale.Service.Model.Cat;

[PublicAPI]
public enum CatTypeDto
{
    Active,
    Inactive,
    Test,
}

[PublicAPI]
public record CatDto
(
    int Id,
    CatTypeDto Type,
    [Required(AllowEmptyStrings = false)] string Name,
    DateOnly DateOfBirth
);

[PublicAPI]
public record CreateCatRequest
(
    CatTypeDto Type,
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
    CatTypeDto Type,
    [Required(AllowEmptyStrings = false)] string Name,
    DateOnly DateOfBirth
);
