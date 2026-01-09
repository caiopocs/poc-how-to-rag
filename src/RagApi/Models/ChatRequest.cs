using System.ComponentModel.DataAnnotations;

public class ChatRequest
{
    [Required(ErrorMessage = "Question is required")]
    public string Question { get; set; } = string.Empty;
}