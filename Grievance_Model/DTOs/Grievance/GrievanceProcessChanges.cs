namespace Grievance_Model.DTOs.Grievance
{
    public class GrievanceProcessChanges
    {
        public int GrievanceId { get; set; }
        public List<GrievanceChange>? ChangeList { get; set; }

        public List<CommentDetailsModel>? CommentDetails { get; set; }
        public string? CaseName { get; set; }
        public string? ChangeBy { get; set; }
        public DateTime? ModifyDate { get; set; }
    }

    public class CommentDetailsModel
    {
        public string? Comment { get; set; }
        public string? CommentType { get; set; }

        public int? CommentedById { get; set; }
        public string? CommentedByName { get; set; }
        public DateTime? CommentedDate { get; set; }

        public List<string>? Attachment { get; set; }

    }

    public class GrievanceChange
    {
        public string? Column { get; set; }
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public string? ProcessCount { get; set; }
    }
}
