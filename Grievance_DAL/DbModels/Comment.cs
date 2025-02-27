namespace Grievance_DAL.DbModels
{
    public class Comment : BaseEntity
    {
        public int Id { get; set; }
        public string CommentText { get; set; }

        public string CommentedBy { get; set; }  // Reference from external system

        public DateTime CommentDate { get; set; }
        public string CommentType { get; set; }
        public int ReferenceId { get; set; }
    }

}
