using System.ComponentModel.DataAnnotations;

public class ChatRequest
{
    [Required(ErrorMessage = "Question is required")]
    public string Question { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional: Specific document ID to search within. If not provided, searches all documents.
    /// </summary>
    public string? DocumentId { get; set; }
}