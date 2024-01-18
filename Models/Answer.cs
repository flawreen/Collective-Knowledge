using System.ComponentModel.DataAnnotations;

namespace QueueUnderflow.Models
{
    public class Answer
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "The content of the answer is required")]
        public string Content { get; set; }

        public DateTime Date { get; set; }

        public int DiscussionId { get; set; }

        public virtual Discussion? Discussion { get; set; }

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
    }
}
