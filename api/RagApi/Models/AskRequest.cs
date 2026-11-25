using System.ComponentModel.DataAnnotations;

namespace RagApi.Models;

public record AskRequest(
    [Required, StringLength(2000, MinimumLength = 1)] string Question);